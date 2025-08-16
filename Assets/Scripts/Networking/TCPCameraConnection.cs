using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using USAALive.VISCA;

namespace USAALive.Networking
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Reconnecting
    }

    public class TCPCameraConnection : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string _cameraIP = "192.168.1.100";
        [SerializeField] private int _cameraPort = 52381;
        [SerializeField] private byte _cameraAddress = 0x81;
        [SerializeField] private float _connectionTimeout = 5.0f;
        [SerializeField] private float _responseTimeout = 2.0f;
        [SerializeField] private float _reconnectDelay = 3.0f;
        [SerializeField] private int _maxReconnectAttempts = 5;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = true;

        public event Action<ConnectionState> OnConnectionStateChanged;
        public event Action<VISCAResponse> OnResponseReceived;
        public event Action<string> OnError;

        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public string CameraIP => _cameraIP;
        public int CameraPort => _cameraPort;
        public byte CameraAddress => _cameraAddress;
        public bool IsConnected => State == ConnectionState.Connected;

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private byte[] _receiveBuffer = new byte[1024];
        private Queue<byte[]> _commandQueue = new Queue<byte[]>();
        private Queue<TaskCompletionSource<VISCAResponse>> _responseQueue = new Queue<TaskCompletionSource<VISCAResponse>>();
        private bool _isProcessingCommands = false;
        private int _reconnectAttempts = 0;
        private bool _shouldReconnect = true;

        #region Unity Lifecycle

        private void Start()
        {
            if (_enableDebugLogging)
                Debug.Log($"TCPCameraConnection initialized for {_cameraIP}:{_cameraPort}");
        }

        private void OnDestroy()
        {
            _shouldReconnect = false;
            DisconnectAsync().ConfigureAwait(false);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _shouldReconnect = false;
                DisconnectAsync().ConfigureAwait(false);
            }
            else if (State == ConnectionState.Disconnected)
            {
                _shouldReconnect = true;
                ConnectAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region Connection Management

        public async Task<bool> ConnectAsync()
        {
            if (State == ConnectionState.Connected || State == ConnectionState.Connecting)
            {
                LogDebug("Already connected or connecting");
                return State == ConnectionState.Connected;
            }

            SetConnectionState(ConnectionState.Connecting);
            LogDebug($"Attempting to connect to {_cameraIP}:{_cameraPort}");

            try
            {
                _tcpClient = new TcpClient();
                
                var connectTask = _tcpClient.ConnectAsync(_cameraIP, _cameraPort);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_connectionTimeout));
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"Connection timeout after {_connectionTimeout} seconds");
                }

                if (!_tcpClient.Connected)
                {
                    throw new InvalidOperationException("Failed to establish connection");
                }

                _networkStream = _tcpClient.GetStream();
                SetConnectionState(ConnectionState.Connected);
                _reconnectAttempts = 0;

                StartReceiving();
                StartCommandProcessing();

                LogDebug("Successfully connected to camera");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Connection failed: {ex.Message}");
                SetConnectionState(ConnectionState.Error);
                
                if (_shouldReconnect && _reconnectAttempts < _maxReconnectAttempts)
                {
                    ScheduleReconnect();
                }
                
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            LogDebug("Disconnecting from camera");
            _shouldReconnect = false;
            _isProcessingCommands = false;

            try
            {
                _networkStream?.Close();
                _tcpClient?.Close();
            }
            catch (Exception ex)
            {
                LogError($"Error during disconnect: {ex.Message}");
            }
            finally
            {
                _networkStream?.Dispose();
                _tcpClient?.Dispose();
                _networkStream = null;
                _tcpClient = null;

                SetConnectionState(ConnectionState.Disconnected);
                ClearQueues();
            }

            await Task.CompletedTask;
        }

        private void ScheduleReconnect()
        {
            if (!_shouldReconnect) return;

            _reconnectAttempts++;
            SetConnectionState(ConnectionState.Reconnecting);
            
            LogDebug($"Scheduling reconnect attempt {_reconnectAttempts}/{_maxReconnectAttempts} in {_reconnectDelay} seconds");
            
            Task.Delay(TimeSpan.FromSeconds(_reconnectDelay)).ContinueWith(async _ =>
            {
                if (_shouldReconnect && State == ConnectionState.Reconnecting)
                {
                    await ConnectAsync();
                }
            });
        }

        #endregion

        #region Command Sending

        public async Task<VISCAResponse> SendCommandAsync(VISCACommand command, Dictionary<string, object> parameters = null)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Camera is not connected");
            }

            var commandBytes = command.BuildCommand(_cameraAddress, parameters);
            return await SendCommandBytesAsync(commandBytes);
        }

        public async Task<VISCAResponse> SendCommandBytesAsync(byte[] commandBytes)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Camera is not connected");
            }

            var responseTask = new TaskCompletionSource<VISCAResponse>();
            
            lock (_commandQueue)
            {
                _commandQueue.Enqueue(commandBytes);
                _responseQueue.Enqueue(responseTask);
            }

            LogDebug($"Queued command: {BitConverter.ToString(commandBytes)}");

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_responseTimeout));
            var completedTask = await Task.WhenAny(responseTask.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                responseTask.TrySetException(new TimeoutException("Command response timeout"));
                throw new TimeoutException($"Command response timeout after {_responseTimeout} seconds");
            }

            return await responseTask.Task;
        }

        public void SendCommandFireAndForget(VISCACommand command, Dictionary<string, object> parameters = null)
        {
            if (!IsConnected)
            {
                LogError("Cannot send command: Camera is not connected");
                return;
            }

            var commandBytes = command.BuildCommand(_cameraAddress, parameters);
            SendCommandBytesFireAndForget(commandBytes);
        }

        public void SendCommandBytesFireAndForget(byte[] commandBytes)
        {
            if (!IsConnected)
            {
                LogError("Cannot send command: Camera is not connected");
                return;
            }

            lock (_commandQueue)
            {
                _commandQueue.Enqueue(commandBytes);
            }

            LogDebug($"Queued command (no response): {BitConverter.ToString(commandBytes)}");
        }

        #endregion

        #region Command Processing

        private async void StartCommandProcessing()
        {
            _isProcessingCommands = true;
            
            while (_isProcessingCommands && IsConnected)
            {
                try
                {
                    byte[] commandToSend = null;
                    
                    lock (_commandQueue)
                    {
                        if (_commandQueue.Count > 0)
                        {
                            commandToSend = _commandQueue.Dequeue();
                        }
                    }

                    if (commandToSend != null)
                    {
                        await _networkStream.WriteAsync(commandToSend, 0, commandToSend.Length);
                        LogDebug($"Sent command: {BitConverter.ToString(commandToSend)}");
                        
                        await Task.Delay(50);
                    }
                    else
                    {
                        await Task.Delay(10);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error processing commands: {ex.Message}");
                    
                    if (IsConnected)
                    {
                        SetConnectionState(ConnectionState.Error);
                        if (_shouldReconnect)
                        {
                            ScheduleReconnect();
                        }
                    }
                    break;
                }
            }
        }

        #endregion

        #region Data Receiving

        private async void StartReceiving()
        {
            try
            {
                while (IsConnected && _networkStream != null)
                {
                    int bytesRead = await _networkStream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        var responseBytes = new byte[bytesRead];
                        Array.Copy(_receiveBuffer, 0, responseBytes, 0, bytesRead);
                        
                        ProcessReceivedData(responseBytes);
                    }
                    else
                    {
                        LogDebug("Received 0 bytes - connection may be closed");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error receiving data: {ex.Message}");
                
                if (IsConnected)
                {
                    SetConnectionState(ConnectionState.Error);
                    if (_shouldReconnect)
                    {
                        ScheduleReconnect();
                    }
                }
            }
        }

        private void ProcessReceivedData(byte[] data)
        {
            LogDebug($"Received data: {BitConverter.ToString(data)}");

            var response = new VISCAResponse(data);
            
            TaskCompletionSource<VISCAResponse> responseTask = null;
            lock (_responseQueue)
            {
                if (_responseQueue.Count > 0)
                {
                    responseTask = _responseQueue.Dequeue();
                }
            }

            responseTask?.TrySetResult(response);
            OnResponseReceived?.Invoke(response);

            if (response.IsError)
            {
                LogError($"Camera response error: {response.ErrorMessage}");
            }
        }

        #endregion

        #region Helper Methods

        private void SetConnectionState(ConnectionState newState)
        {
            if (State != newState)
            {
                State = newState;
                LogDebug($"Connection state changed to: {newState}");
                OnConnectionStateChanged?.Invoke(newState);
            }
        }

        private void ClearQueues()
        {
            lock (_commandQueue)
            {
                _commandQueue.Clear();
            }

            lock (_responseQueue)
            {
                while (_responseQueue.Count > 0)
                {
                    var task = _responseQueue.Dequeue();
                    task.TrySetCanceled();
                }
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[TCPCameraConnection] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[TCPCameraConnection] {message}");
            OnError?.Invoke(message);
        }

        #endregion

        #region Public Configuration

        public void SetCameraAddress(byte address)
        {
            _cameraAddress = address;
            LogDebug($"Camera address set to: 0x{address:X2}");
        }

        public void SetConnectionSettings(string ip, int port)
        {
            if (IsConnected)
            {
                LogError("Cannot change connection settings while connected");
                return;
            }

            _cameraIP = ip;
            _cameraPort = port;
            LogDebug($"Connection settings updated: {ip}:{port}");
        }

        public void SetTimeouts(float connectionTimeout, float responseTimeout)
        {
            _connectionTimeout = connectionTimeout;
            _responseTimeout = responseTimeout;
            LogDebug($"Timeouts updated - Connection: {connectionTimeout}s, Response: {responseTimeout}s");
        }

        public void SetReconnectionSettings(float reconnectDelay, int maxAttempts)
        {
            _reconnectDelay = reconnectDelay;
            _maxReconnectAttempts = maxAttempts;
            LogDebug($"Reconnection settings updated - Delay: {reconnectDelay}s, Max attempts: {maxAttempts}");
        }

        #endregion
    }
}