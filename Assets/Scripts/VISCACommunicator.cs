using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class VISCACommunicator : MonoBehaviour
{
    [Header("Connection Settings")]
    public VISCACameraConfig cameraConfig;
    
    [Header("Communication Settings")]
    public int commandTimeoutMs = 5000;
    public int maxRetryAttempts = 3;
    public bool enableLogging = true;
    
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private bool isConnected = false;
    private readonly object lockObject = new object();
    
    // Command queue with priority support
    private readonly Queue<VISCACommandRequest> commandQueue = new Queue<VISCACommandRequest>();
    private bool isProcessingQueue = false;
    
    // Response tracking
    private readonly Dictionary<byte, TaskCompletionSource<VISCAResponse>> pendingCommands = 
        new Dictionary<byte, TaskCompletionSource<VISCAResponse>>();
    private byte sequenceNumber = 1;

    public event Action<bool> OnConnectionChanged;
    public event Action<VISCAResponse> OnResponseReceived;
    public event Action<string> OnError;

    private struct VISCACommandRequest
    {
        public byte[] Command;
        public VISCAProtocol.CommandPriority Priority;
        public TaskCompletionSource<VISCAResponse> CompletionSource;
        public int RetryCount;
        public byte SequenceNumber;
    }

    public struct VISCAResponse
    {
        public bool IsSuccess;
        public byte[] Data;
        public VISCAProtocol.VISCAError ErrorCode;
        public string ErrorMessage;
        public byte SequenceNumber;
    }

    private void Start()
    {
        if (cameraConfig == null)
        {
            Debug.LogError("Camera config not assigned!");
            return;
        }
        
        ConnectAsync();
    }

    private void OnDestroy()
    {
        DisconnectAsync();
    }

    public async void ConnectAsync()
    {
        if (isConnected)
        {
            Debug.LogWarning("Already connected to camera");
            return;
        }

        try
        {
            Log($"Connecting to camera at {cameraConfig.IPAddress}:{cameraConfig.Port}");
            
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(cameraConfig.IPAddress, cameraConfig.Port);
            networkStream = tcpClient.GetStream();
            isConnected = true;
            
            Log("Connected to camera successfully");
            OnConnectionChanged?.Invoke(true);
            
            // Start response listening
            _ = Task.Run(ListenForResponses);
            
            // Start command queue processing
            _ = Task.Run(ProcessCommandQueue);
        }
        catch (Exception ex)
        {
            LogError($"Failed to connect to camera: {ex.Message}");
            OnError?.Invoke($"Connection failed: {ex.Message}");
            isConnected = false;
            OnConnectionChanged?.Invoke(false);
        }
    }

    public async void DisconnectAsync()
    {
        if (!isConnected)
            return;

        try
        {
            isConnected = false;
            
            // Cancel all pending commands
            lock (lockObject)
            {
                foreach (var pending in pendingCommands.Values)
                {
                    pending.SetCanceled();
                }
                pendingCommands.Clear();
                commandQueue.Clear();
            }
            
            networkStream?.Close();
            tcpClient?.Close();
            
            Log("Disconnected from camera");
            OnConnectionChanged?.Invoke(false);
        }
        catch (Exception ex)
        {
            LogError($"Error during disconnect: {ex.Message}");
        }
    }

    public async Task<VISCAResponse> SendCommandAsync(byte[] command, VISCAProtocol.CommandPriority priority = VISCAProtocol.CommandPriority.Other)
    {
        if (!isConnected)
        {
            return new VISCAResponse
            {
                IsSuccess = false,
                ErrorCode = VISCAProtocol.VISCAError.NoSocket,
                ErrorMessage = "Not connected to camera"
            };
        }

        if (!VISCACommandBuilder.ValidatePacket(command))
        {
            return new VISCAResponse
            {
                IsSuccess = false,
                ErrorCode = VISCAProtocol.VISCAError.Syntax,
                ErrorMessage = "Invalid command packet"
            };
        }

        var completionSource = new TaskCompletionSource<VISCAResponse>();
        var sequenceNum = GetNextSequenceNumber();
        
        var request = new VISCACommandRequest
        {
            Command = command,
            Priority = priority,
            CompletionSource = completionSource,
            RetryCount = 0,
            SequenceNumber = sequenceNum
        };

        lock (lockObject)
        {
            // Insert command based on priority (higher priority = lower number = front of queue)
            if (priority <= VISCAProtocol.CommandPriority.PTZPosition)
            {
                // High priority - add to front
                var tempQueue = new Queue<VISCACommandRequest>();
                tempQueue.Enqueue(request);
                while (commandQueue.Count > 0)
                {
                    tempQueue.Enqueue(commandQueue.Dequeue());
                }
                commandQueue.Clear();
                while (tempQueue.Count > 0)
                {
                    commandQueue.Enqueue(tempQueue.Dequeue());
                }
            }
            else
            {
                // Normal priority - add to back
                commandQueue.Enqueue(request);
            }
            
            pendingCommands[sequenceNum] = completionSource;
        }

        try
        {
            return await completionSource.Task;
        }
        catch (OperationCanceledException)
        {
            return new VISCAResponse
            {
                IsSuccess = false,
                ErrorCode = VISCAProtocol.VISCAError.CommandCancelled,
                ErrorMessage = "Command was cancelled"
            };
        }
    }

    private async Task ProcessCommandQueue()
    {
        while (isConnected)
        {
            VISCACommandRequest? request = null;
            
            lock (lockObject)
            {
                if (commandQueue.Count > 0 && !isProcessingQueue)
                {
                    request = commandQueue.Dequeue();
                    isProcessingQueue = true;
                }
            }

            if (request.HasValue)
            {
                await ProcessSingleCommand(request.Value);
                lock (lockObject)
                {
                    isProcessingQueue = false;
                }
            }
            else
            {
                await Task.Delay(10); // Small delay when queue is empty
            }
        }
    }

    private async Task ProcessSingleCommand(VISCACommandRequest request)
    {
        try
        {
            if (enableLogging)
            {
                Log($"Sending command: {VISCACommandBuilder.BytesToHexString(request.Command)}");
            }

            await networkStream.WriteAsync(request.Command, 0, request.Command.Length);
            
            // Wait for ACK with timeout
            var ackReceived = await WaitForAck(request.SequenceNumber);
            
            if (!ackReceived && request.RetryCount < maxRetryAttempts)
            {
                // Retry command
                request.RetryCount++;
                lock (lockObject)
                {
                    commandQueue.Enqueue(request);
                }
                Log($"Retrying command (attempt {request.RetryCount + 1})");
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to send command: {ex.Message}");
            
            lock (lockObject)
            {
                if (pendingCommands.TryGetValue(request.SequenceNumber, out var completionSource))
                {
                    completionSource.SetResult(new VISCAResponse
                    {
                        IsSuccess = false,
                        ErrorCode = VISCAProtocol.VISCAError.CommandNotExecutable,
                        ErrorMessage = ex.Message,
                        SequenceNumber = request.SequenceNumber
                    });
                    pendingCommands.Remove(request.SequenceNumber);
                }
            }
        }
    }

    private async Task<bool> WaitForAck(byte sequenceNumber)
    {
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < commandTimeoutMs)
        {
            lock (lockObject)
            {
                if (!pendingCommands.ContainsKey(sequenceNumber))
                {
                    return true; // Command completed
                }
            }
            
            await Task.Delay(10);
        }
        
        return false; // Timeout
    }

    private async Task ListenForResponses()
    {
        var buffer = new byte[256];
        
        while (isConnected)
        {
            try
            {
                if (networkStream.DataAvailable)
                {
                    int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        var responseData = new byte[bytesRead];
                        Array.Copy(buffer, responseData, bytesRead);
                        
                        ProcessResponse(responseData);
                    }
                }
                else
                {
                    await Task.Delay(10);
                }
            }
            catch (Exception ex)
            {
                if (isConnected)
                {
                    LogError($"Error reading response: {ex.Message}");
                }
                break;
            }
        }
    }

    private void ProcessResponse(byte[] responseData)
    {
        if (enableLogging)
        {
            Log($"Received response: {VISCACommandBuilder.BytesToHexString(responseData)}");
        }

        var response = ParseResponse(responseData);
        OnResponseReceived?.Invoke(response);
        
        // Complete pending command if this is a completion response
        if (response.IsSuccess || response.ErrorCode != VISCAProtocol.VISCAError.None)
        {
            lock (lockObject)
            {
                if (pendingCommands.TryGetValue(response.SequenceNumber, out var completionSource))
                {
                    completionSource.SetResult(response);
                    pendingCommands.Remove(response.SequenceNumber);
                }
            }
        }
    }

    private VISCAResponse ParseResponse(byte[] data)
    {
        if (data == null || data.Length < 3)
        {
            return new VISCAResponse
            {
                IsSuccess = false,
                ErrorCode = VISCAProtocol.VISCAError.MessageLength,
                ErrorMessage = "Response too short"
            };
        }

        var response = new VISCAResponse
        {
            Data = data,
            SequenceNumber = (byte)(data[0] & 0x0F) // Extract camera/sequence info
        };

        // Check response type
        if ((data[1] & 0xF0) == VISCAProtocol.ACK_CODE)
        {
            response.IsSuccess = true;
            response.ErrorMessage = "ACK received";
        }
        else if ((data[1] & 0xF0) == VISCAProtocol.COMPLETION_CODE)
        {
            response.IsSuccess = true;
            response.ErrorMessage = "Command completed";
        }
        else if ((data[1] & 0xF0) == VISCAProtocol.ERROR_CODE)
        {
            response.IsSuccess = false;
            response.ErrorCode = (VISCAProtocol.VISCAError)(data[2]);
            response.ErrorMessage = $"VISCA Error: {response.ErrorCode}";
        }
        else
        {
            response.IsSuccess = false;
            response.ErrorCode = VISCAProtocol.VISCAError.Syntax;
            response.ErrorMessage = "Unknown response type";
        }

        return response;
    }

    private byte GetNextSequenceNumber()
    {
        lock (lockObject)
        {
            sequenceNumber = (byte)((sequenceNumber % 7) + 1); // VISCA sequence 1-7
            return sequenceNumber;
        }
    }

    public bool IsConnected => isConnected;

    public int QueuedCommandCount
    {
        get
        {
            lock (lockObject)
            {
                return commandQueue.Count;
            }
        }
    }

    private void Log(string message)
    {
        if (enableLogging)
        {
            Debug.Log($"[VISCA] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[VISCA ERROR] {message}");
    }

    // Convenience methods for common operations
    public async Task<VISCAResponse> SendPTZCommandAsync(VISCAProtocol.PTZCommand command, int panSpeed = 5, int tiltSpeed = 5)
    {
        var commandBytes = VISCACommandBuilder.BuildPTZCommand(cameraConfig.CameraAddress, command, panSpeed, tiltSpeed);
        if (commandBytes == null) return new VISCAResponse { IsSuccess = false, ErrorMessage = "Failed to build command" };
        
        return await SendCommandAsync(commandBytes, VISCAProtocol.CommandPriority.PTZMovement);
    }

    public async Task<VISCAResponse> SendZoomCommandAsync(VISCAProtocol.ZoomCommand command, int speed = 3)
    {
        var commandBytes = VISCACommandBuilder.BuildZoomCommand(cameraConfig.CameraAddress, command, speed);
        if (commandBytes == null) return new VISCAResponse { IsSuccess = false, ErrorMessage = "Failed to build command" };
        
        return await SendCommandAsync(commandBytes, VISCAProtocol.CommandPriority.ZoomMovement);
    }

    public async Task<VISCAResponse> SendFocusCommandAsync(VISCAProtocol.FocusCommand command, int speed = 3)
    {
        var commandBytes = VISCACommandBuilder.BuildFocusCommand(cameraConfig.CameraAddress, command, speed);
        if (commandBytes == null) return new VISCAResponse { IsSuccess = false, ErrorMessage = "Failed to build command" };
        
        return await SendCommandAsync(commandBytes, VISCAProtocol.CommandPriority.FocusControl);
    }

    public async Task<VISCAResponse> SendPresetCommandAsync(VISCAProtocol.PresetCommand command, int presetNumber)
    {
        var commandBytes = VISCACommandBuilder.BuildPresetCommand(cameraConfig.CameraAddress, command, presetNumber);
        if (commandBytes == null) return new VISCAResponse { IsSuccess = false, ErrorMessage = "Failed to build command" };
        
        return await SendCommandAsync(commandBytes, VISCAProtocol.CommandPriority.CameraControl);
    }
}