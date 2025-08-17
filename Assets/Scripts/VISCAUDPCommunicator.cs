using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// UDP-based VISCA communicator based on the working reference implementation
/// Uses UDP instead of TCP for better compatibility with VISCA over IP cameras
/// </summary>
public class VISCAUDPCommunicator : MonoBehaviour, IDisposable
{
    [Header("Camera Configuration")]
    public string cameraIP = "192.168.1.100";
    public int cameraPort = 52381;
    public int cameraAddress = 1;
    
    [Header("Settings")]
    public bool enableLogging = true;
    
    // Network objects
    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private bool isConnected = false;
    
    // Events
    public event Action<bool> OnConnectionChanged;
    public event Action<string> OnError;
    
    public bool IsConnected => isConnected;
    
    private void Start()
    {
        ConnectToCamera();
    }
    
    private void OnDestroy()
    {
        Dispose();
    }
    
    public void ConnectToCamera()
    {
        try
        {
            if (udpClient != null)
            {
                udpClient.Dispose();
            }
            
            endPoint = new IPEndPoint(IPAddress.Parse(cameraIP), cameraPort);
            udpClient = new UdpClient();
            isConnected = true;
            
            Log($"VISCA UDP communicator initialized for {cameraIP}:{cameraPort} (Camera {cameraAddress})");
            OnConnectionChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize camera connection: {ex.Message}");
            OnError?.Invoke($"Connection failed: {ex.Message}");
            isConnected = false;
            OnConnectionChanged?.Invoke(false);
        }
    }
    
    public void UpdateConnection(string newIP, int newPort)
    {
        if (cameraIP == newIP && cameraPort == newPort)
        {
            return; // No change needed
        }
        
        Log($"Updating VISCA connection: {cameraIP}:{cameraPort} → {newIP}:{newPort}");
        cameraIP = newIP;
        cameraPort = newPort;
        
        ConnectToCamera();
    }
    
    // Pan/Tilt Commands
    public async Task PanLeft(byte speed = ViscaCommands.DEFAULT_PAN_SPEED)
    {
        await SendCommandAsync(ViscaCommands.PanTiltCommand(true, false, false, false, speed, 0x00));
    }
    
    public async Task PanRight(byte speed = ViscaCommands.DEFAULT_PAN_SPEED)
    {
        await SendCommandAsync(ViscaCommands.PanTiltCommand(false, true, false, false, speed, 0x00));
    }
    
    public async Task TiltUp(byte speed = ViscaCommands.DEFAULT_PAN_SPEED)
    {
        await SendCommandAsync(ViscaCommands.PanTiltCommand(false, false, true, false, 0x00, speed));
    }
    
    public async Task TiltDown(byte speed = ViscaCommands.DEFAULT_PAN_SPEED)
    {
        await SendCommandAsync(ViscaCommands.PanTiltCommand(false, false, false, true, 0x00, speed));
    }
    
    public async Task PanTiltUpLeft(byte panSpeed = ViscaCommands.DEFAULT_PAN_SPEED, byte tiltSpeed = ViscaCommands.DEFAULT_PAN_SPEED)
    {
        await SendCommandAsync(ViscaCommands.PanTiltUpLeftCommand(panSpeed, tiltSpeed));
    }
    
    public async Task PanTiltUpRight(byte panSpeed = ViscaCommands.DEFAULT_PAN_SPEED, byte tiltSpeed = ViscaCommands.DEFAULT_PAN_SPEED)
    {
        await SendCommandAsync(ViscaCommands.PanTiltUpRightCommand(panSpeed, tiltSpeed));
    }
    
    public async Task PanTiltDownLeft(byte panSpeed = ViscaCommands.DEFAULT_PAN_SPEED, byte tiltSpeed = ViscaCommands.DEFAULT_PAN_SPEED)
    {
        await SendCommandAsync(ViscaCommands.PanTiltDownLeftCommand(panSpeed, tiltSpeed));
    }
    
    public async Task PanTiltDownRight(byte panSpeed = ViscaCommands.DEFAULT_PAN_SPEED, byte tiltSpeed = ViscaCommands.DEFAULT_PAN_SPEED)
    {
        await SendCommandAsync(ViscaCommands.PanTiltDownRightCommand(panSpeed, tiltSpeed));
    }
    
    public async Task Stop()
    {
        await SendCommandAsync(ViscaCommands.StopCommand());
    }
    
    public async Task Home()
    {
        await SendCommandAsync(ViscaCommands.HomeCommand());
    }
    
    // Zoom Commands
    public async Task ZoomIn(byte speed = ViscaCommands.DEFAULT_ZOOM_SPEED)
    {
        await SendCommandAsync(ViscaCommands.ZoomCommand(true, false, speed));
    }
    
    public async Task ZoomOut(byte speed = ViscaCommands.DEFAULT_ZOOM_SPEED)
    {
        await SendCommandAsync(ViscaCommands.ZoomCommand(false, true, speed));
    }
    
    public async Task ZoomStop()
    {
        await SendCommandAsync(ViscaCommands.ZoomStopCommand());
    }
    
    // Focus Commands
    public async Task FocusNear()
    {
        await SendCommandAsync(ViscaCommands.FocusNearCommand());
    }
    
    public async Task FocusFar()
    {
        await SendCommandAsync(ViscaCommands.FocusFarCommand());
    }
    
    public async Task FocusStop()
    {
        await SendCommandAsync(ViscaCommands.FocusStopCommand());
    }
    
    public async Task FocusAuto()
    {
        await SendCommandAsync(ViscaCommands.FocusAutoCommand());
    }
    
    public async Task FocusManual()
    {
        await SendCommandAsync(ViscaCommands.FocusManualCommand());
    }
    
    public async Task FocusOnePush()
    {
        await SendCommandAsync(ViscaCommands.FocusOnePushCommand());
    }
    
    // Preset Commands
    public async Task PresetRecall(byte presetNumber)
    {
        await SendCommandAsync(ViscaCommands.PresetRecallCommand(presetNumber));
    }
    
    public async Task PresetSet(byte presetNumber)
    {
        await SendCommandAsync(ViscaCommands.PresetSetCommand(presetNumber));
    }
    
    public async Task PresetReset(byte presetNumber)
    {
        await SendCommandAsync(ViscaCommands.PresetResetCommand(presetNumber));
    }
    
    // White Balance Commands
    public async Task WhiteBalanceAuto()
    {
        await SendCommandAsync(ViscaCommands.WhiteBalanceAutoCommand());
    }
    
    public async Task WhiteBalanceIndoor()
    {
        await SendCommandAsync(ViscaCommands.WhiteBalanceIndoorCommand());
    }
    
    public async Task WhiteBalanceOutdoor()
    {
        await SendCommandAsync(ViscaCommands.WhiteBalanceOutdoorCommand());
    }
    
    // Exposure Commands
    public async Task ExposureFullAuto()
    {
        await SendCommandAsync(ViscaCommands.ExposureFullAutoCommand());
    }
    
    public async Task ExposureManual()
    {
        await SendCommandAsync(ViscaCommands.ExposureManualCommand());
    }
    
    // Core send method
    public async Task SendCommandAsync(byte[] command)
    {
        if (!isConnected || udpClient == null || endPoint == null)
        {
            LogError("Cannot send command: Not connected to camera");
            return;
        }
        
        try
        {
            // Update camera address in command if needed
            if (command.Length > 0 && (command[0] & 0xF0) == 0x80)
            {
                command[0] = (byte)(0x80 | (cameraAddress & 0x0F));
            }
            
            await udpClient.SendAsync(command, command.Length, endPoint);
            
            if (enableLogging)
            {
                string hexString = BitConverter.ToString(command).Replace("-", " ");
                Log($"Sent VISCA command: {hexString}");
            }
        }
        catch (Exception ex)
        {
            // Reduce spam for expected network errors when no physical cameras present
            if (ex.Message.Contains("No route to host") || ex.Message.Contains("Host is down"))
            {
                Log($"Camera at {cameraIP}:{cameraPort} not reachable (expected if no physical camera)");
            }
            else
            {
                LogError($"Failed to send VISCA command: {ex.Message}");
                OnError?.Invoke($"Send error: {ex.Message}");
            }
        }
    }
    
    private void Log(string message)
    {
        if (enableLogging)
        {
            Debug.Log($"[VISCA UDP] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[VISCA UDP] {message}");
    }
    
    public void Dispose()
    {
        if (udpClient != null)
        {
            udpClient.Dispose();
            udpClient = null;
        }
        
        isConnected = false;
        OnConnectionChanged?.Invoke(false);
    }
}