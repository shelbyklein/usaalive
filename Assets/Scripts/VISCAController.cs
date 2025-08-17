using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class VISCAController : MonoBehaviour
{
    [Header("Camera Controllers")]
    [SerializeField] private List<VISCAUDPCommunicator> cameraCommunicators = new List<VISCAUDPCommunicator>();
    
    [Header("Control Settings")]
    [SerializeField] private int defaultPanSpeed = 8;
    [SerializeField] private int defaultTiltSpeed = 8;
    [SerializeField] private int defaultZoomSpeed = 4;
    [SerializeField] private int defaultFocusSpeed = 4;
    
    [Header("Input Settings")]
    [SerializeField] private bool enableContinuousMovement = true;
    [SerializeField] private float commandRepeatInterval = 0.1f; // Seconds between repeated commands
    
    private CameraInputManager cameraInputManager;
    private int currentlySelectedCamera = -1;
    private bool isInitialized = false;
    
    // Movement state tracking
    private bool isPanTiltActive = false;
    private bool isZoomActive = false;
    private bool isFocusActive = false;
    private VISCAProtocol.PTZCommand currentPTZCommand;
    private VISCAProtocol.ZoomCommand currentZoomCommand;
    private VISCAProtocol.FocusCommand currentFocusCommand;
    
    // Event delegates
    public event Action<int, bool> OnCameraConnectionChanged;
    public event Action<int, string> OnCameraError;
    public event Action<int, VISCAProtocol.PTZCommand> OnPTZCommandExecuted;
    public event Action<int, VISCAProtocol.ZoomCommand> OnZoomCommandExecuted;
    public event Action<int, VISCAProtocol.FocusCommand> OnFocusCommandExecuted;

    private void Start()
    {
        if (isInitialized)
        {
            Debug.LogWarning("[VISCA] Controller already initialized, skipping...");
            return;
        }
        
        // Clear any existing communicators first
        ClearExistingCommunicators();

        try
        {
            // Find the camera input manager
            cameraInputManager = FindObjectOfType<CameraInputManager>();
            if (cameraInputManager == null)
            {
                Debug.LogError("CameraInputManager not found! VISCA control requires camera selection.");
                return;
            }
            
            // Start delayed initialization to wait for NDI sources
            StartCoroutine(DelayedInitialization());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[VISCA] Error during initialization: {ex.Message}");
            Debug.LogError($"[VISCA] Stack trace: {ex.StackTrace}");
        }
    }
    
    private System.Collections.IEnumerator DelayedInitialization()
    {
        Debug.Log("[VISCA] Waiting for NDI sources to be detected before creating communicators...");
        
        var gridManager = FindObjectOfType<NDICameraGridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning("[VISCA] NDI Camera Grid Manager not found, creating default cameras");
            CreateDefaultCameras();
            yield break;
        }
        
        // Wait up to 10 seconds for NDI sources to be detected
        float waitTime = 0f;
        const float maxWaitTime = 10f;
        
        while (waitTime < maxWaitTime)
        {
            if (gridManager.cameraCells != null && gridManager.cameraCells.Count > 0)
            {
                // Check if the cells have NDI receivers with actual source names
                bool hasValidSources = false;
                foreach (var cell in gridManager.cameraCells)
                {
                    if (cell.ndiReceiver != null && !string.IsNullOrEmpty(cell.ndiReceiver.ndiName))
                    {
                        hasValidSources = true;
                        break;
                    }
                }
                
                if (hasValidSources)
                {
                    Debug.Log($"[VISCA] NDI sources detected after {waitTime:F1}s, creating communicators...");
                    CreateCommunicatorsFromNDISources(gridManager);
                    break;
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }
        
        // If no NDI sources found after waiting, create default cameras
        if (cameraCommunicators.Count == 0)
        {
            Debug.LogWarning($"[VISCA] No NDI sources detected after {maxWaitTime}s, creating default cameras");
            CreateDefaultCameras();
        }
        
        InitializeCameraCommunicators();
        
        Debug.Log($"[VISCA] Controller initialized with {cameraCommunicators.Count} cameras");
        
        if (cameraCommunicators.Count == 0)
        {
            Debug.LogWarning("[VISCA] No camera communicators found. Add VISCACommunicator components manually or configure camera IP addresses.");
        }
        
        isInitialized = true;
    }

    private void ClearExistingCommunicators()
    {
        // Clear the list and destroy any existing communicator GameObjects
        if (cameraCommunicators != null)
        {
            foreach (var comm in cameraCommunicators)
            {
                if (comm != null && comm.gameObject != null)
                {
                    Debug.Log($"[VISCA] Clearing old communicator: {comm.gameObject.name}");
                    if (Application.isPlaying)
                    {
                        Destroy(comm.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(comm.gameObject);
                    }
                }
            }
            cameraCommunicators.Clear();
        }
    }

    private void CreateDefaultCameras()
    {
        Debug.LogWarning("[VISCA] Creating 6 default camera communicators with fallback IPs");
        for (int i = 0; i < 6; i++)
        {
            CreateCameraCommunicator(i + 1, $"192.168.1.{100 + i}", $"Default Camera {i + 1}");
        }
    }

    private void CreateCommunicatorsFromNDISources(NDICameraGridManager gridManager)
    {
        // Access the camera cells to get NDI source information
        var cameraCells = gridManager.cameraCells;
        
        if (cameraCells == null || cameraCells.Count == 0)
        {
            Debug.LogWarning("[VISCA] No NDI camera cells found, using default setup");
            // Create default cameras without recursion
            for (int i = 0; i < 6; i++)
            {
                CreateCameraCommunicator(i + 1, $"192.168.1.{100 + i}", $"Default Camera {i + 1}");
            }
            return;
        }

        Debug.Log($"[VISCA] Found {cameraCells.Count} NDI cameras, using their detected IP addresses...");

        // Safety limit to prevent too many cameras
        int maxCameras = Mathf.Min(cameraCells.Count, 10);
        
        for (int i = 0; i < maxCameras; i++)
        {
            var cell = cameraCells[i];
            string ndiSourceName = null;
            string detectedIP = null;
            
            // Try to get NDI source name from the receiver
            if (cell.ndiReceiver != null && !string.IsNullOrEmpty(cell.ndiReceiver.ndiName))
            {
                ndiSourceName = cell.ndiReceiver.ndiName;
                // Extract IP from the NDI source name that was already processed by NDI manager
                detectedIP = NDIIPExtractor.ExtractIPFromNDISource(ndiSourceName);
            }
            
            if (!string.IsNullOrEmpty(detectedIP) && NDIIPExtractor.IsValidIPAddress(detectedIP))
            {
                // Use the actual detected IP address
                CreateCameraCommunicator(i + 1, detectedIP, ndiSourceName ?? $"Camera {i + 1}");
                Debug.Log($"[VISCA] Using detected IP {detectedIP} for camera {i + 1} from NDI source '{ndiSourceName}'");
            }
            else
            {
                // Fallback only if we really can't get the IP
                Debug.LogWarning($"[VISCA] Could not extract valid IP from '{ndiSourceName}', using fallback");
                CreateCameraCommunicator(i + 1, $"192.168.1.{100 + i}", ndiSourceName ?? $"Camera {i + 1}");
            }
        }
    }

    private void CreateCameraCommunicator(int cameraNumber, string ipAddress, string sourceName)
    {
        try
        {
            // Validate inputs
            if (cameraNumber <= 0 || string.IsNullOrEmpty(ipAddress))
            {
                Debug.LogError($"[VISCA] Invalid camera parameters: number={cameraNumber}, ip='{ipAddress}'");
                return;
            }

            // Check if we already have too many cameras
            if (cameraCommunicators.Count >= 10)
            {
                Debug.LogWarning($"[VISCA] Maximum camera limit reached (10), skipping camera {cameraNumber}");
                return;
            }

            // Create a new GameObject for this camera's communicator
            var cameraObj = new GameObject($"VISCA_Camera_{cameraNumber}");
            if (transform != null)
            {
                cameraObj.transform.SetParent(transform);
            }
            
            // Add VISCAUDPCommunicator component
            var communicator = cameraObj.AddComponent<VISCAUDPCommunicator>();
            
            // Configure UDP communicator
            communicator.cameraIP = ipAddress;
            communicator.cameraPort = 52381; // Confirmed: AIDA cameras use port 52381
            communicator.cameraAddress = cameraCommunicators.Count + 1; // Set address based on list position (1-based)
            
            // Add to our list
            cameraCommunicators.Add(communicator);
            
            Debug.Log($"[VISCA] Created communicator for Camera {cameraNumber}: {ipAddress}:52381 (NDI: {sourceName})");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[VISCA] Error creating camera communicator: {ex.Message}");
        }
    }

    private void InitializeCameraCommunicators()
    {
        // Subscribe to communicator events
        for (int i = 0; i < cameraCommunicators.Count; i++)
        {
            int cameraIndex = i; // Capture for closure
            var communicator = cameraCommunicators[i];
            
            if (communicator != null)
            {
                communicator.OnConnectionChanged += (connected) => OnCameraConnectionChanged?.Invoke(cameraIndex, connected);
                communicator.OnError += (error) => OnCameraError?.Invoke(cameraIndex, error);
                
                Debug.Log($"[VISCA] Camera {cameraIndex + 1} communicator initialized");
            }
        }
    }

    public void SetSelectedCamera(int cameraIndex)
    {
        if (cameraCommunicators.Count == 0)
        {
            Debug.LogWarning($"[VISCA] No camera communicators configured. Please add VISCACommunicator components to enable PTZ control.");
            currentlySelectedCamera = -1;
            return;
        }
        
        if (cameraIndex >= 0 && cameraIndex < cameraCommunicators.Count)
        {
            currentlySelectedCamera = cameraIndex;
            var selectedComm = cameraCommunicators[cameraIndex];
            Debug.Log($"[VISCA] Selected camera {cameraIndex + 1} for control → Camera Address {selectedComm?.cameraAddress} at {selectedComm?.cameraIP}");
        }
        else
        {
            currentlySelectedCamera = -1;
            Debug.LogWarning($"[VISCA] Invalid camera index: {cameraIndex}. Available cameras: 0-{cameraCommunicators.Count - 1}");
        }
    }

    public async void StopCameraMovement(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= cameraCommunicators.Count)
        {
            Debug.LogWarning($"[VISCA] Cannot stop camera {cameraIndex + 1}: Invalid camera index");
            return;
        }

        var comm = cameraCommunicators[cameraIndex];
        if (comm == null)
        {
            Debug.LogWarning($"[VISCA] Cannot stop camera {cameraIndex + 1}: Communicator is null");
            return;
        }

        try
        {
            // Send stop commands for all movement types using the communicator's direct methods
            await comm.Stop();        // Stop PTZ movement
            await comm.ZoomStop();    // Stop zoom movement
            await comm.FocusStop();   // Stop focus movement
            Debug.Log($"[VISCA] Stopped all movement on Camera {cameraIndex + 1}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VISCA] Failed to stop Camera {cameraIndex + 1}: {ex.Message}");
        }
    }

    private VISCAUDPCommunicator GetSelectedCommunicator()
    {
        if (currentlySelectedCamera < 0 || currentlySelectedCamera >= cameraCommunicators.Count)
        {
            // Try to get from input manager
            if (cameraInputManager != null && cameraInputManager.IsCameraSelected())
            {
                currentlySelectedCamera = cameraInputManager.GetCurrentlySelectedCamera();
            }
        }
        
        if (currentlySelectedCamera >= 0 && currentlySelectedCamera < cameraCommunicators.Count)
        {
            return cameraCommunicators[currentlySelectedCamera];
        }
        
        return null;
    }

    // PTZ Movement Methods
    public async Task<bool> MovePanTilt(VISCAProtocol.PTZCommand command, int panSpeed = -1, int tiltSpeed = -1)
    {
        var communicator = GetSelectedCommunicator();
        if (communicator == null)
        {
            Debug.LogWarning("[VISCA] No camera selected for PTZ control");
            return false;
        }

        byte actualPanSpeed = (byte)Mathf.Clamp(panSpeed > 0 ? panSpeed : defaultPanSpeed, 1, 24);
        byte actualTiltSpeed = (byte)Mathf.Clamp(tiltSpeed > 0 ? tiltSpeed : defaultTiltSpeed, 1, 24);
        
        try
        {
            // Map VISCA commands to UDP communicator methods
            switch (command)
            {
                case VISCAProtocol.PTZCommand.Left:
                    await communicator.PanLeft(actualPanSpeed);
                    break;
                case VISCAProtocol.PTZCommand.Right:
                    await communicator.PanRight(actualPanSpeed);
                    break;
                case VISCAProtocol.PTZCommand.Up:
                    await communicator.TiltUp(actualTiltSpeed);
                    break;
                case VISCAProtocol.PTZCommand.Down:
                    await communicator.TiltDown(actualTiltSpeed);
                    break;
                case VISCAProtocol.PTZCommand.UpLeft:
                    await communicator.PanTiltUpLeft(actualPanSpeed, actualTiltSpeed);
                    break;
                case VISCAProtocol.PTZCommand.UpRight:
                    await communicator.PanTiltUpRight(actualPanSpeed, actualTiltSpeed);
                    break;
                case VISCAProtocol.PTZCommand.DownLeft:
                    await communicator.PanTiltDownLeft(actualPanSpeed, actualTiltSpeed);
                    break;
                case VISCAProtocol.PTZCommand.DownRight:
                    await communicator.PanTiltDownRight(actualPanSpeed, actualTiltSpeed);
                    break;
                case VISCAProtocol.PTZCommand.Stop:
                    await communicator.Stop();
                    break;
                case VISCAProtocol.PTZCommand.Home:
                    await communicator.Home();
                    break;
            }
            
            OnPTZCommandExecuted?.Invoke(currentlySelectedCamera, command);
            Debug.Log($"[VISCA] PTZ command {command} executed on camera {currentlySelectedCamera + 1}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VISCA] PTZ command failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopPanTilt()
    {
        return await MovePanTilt(VISCAProtocol.PTZCommand.Stop);
    }

    public async Task<bool> PanTiltHome()
    {
        return await MovePanTilt(VISCAProtocol.PTZCommand.Home);
    }

    public async Task<bool> SetPanTiltPosition(int panPosition, int tiltPosition, int panSpeed = -1, int tiltSpeed = -1)
    {
        var communicator = GetSelectedCommunicator();
        if (communicator == null)
        {
            Debug.LogWarning("[VISCA] No camera selected for position control");
            return false;
        }

        int actualPanSpeed = panSpeed > 0 ? panSpeed : defaultPanSpeed;
        int actualTiltSpeed = tiltSpeed > 0 ? tiltSpeed : defaultTiltSpeed;
        
        // For UDP communicator, we don't need to build complex position commands
        // The working reference doesn't support absolute positioning, so skip this for now
        Debug.LogWarning("[VISCA] Absolute position commands not implemented in UDP communicator");
        return false;
    }

    // Zoom Control Methods
    public async Task<bool> Zoom(VISCAProtocol.ZoomCommand command, int speed = -1)
    {
        var communicator = GetSelectedCommunicator();
        if (communicator == null)
        {
            Debug.LogWarning("[VISCA] No camera selected for zoom control");
            return false;
        }

        byte actualSpeed = (byte)Mathf.Clamp(speed > 0 ? speed : defaultZoomSpeed, 0, 7);
        
        try
        {
            switch (command)
            {
                case VISCAProtocol.ZoomCommand.TeleVariable:
                    await communicator.ZoomIn(actualSpeed);
                    break;
                case VISCAProtocol.ZoomCommand.WideVariable:
                    await communicator.ZoomOut(actualSpeed);
                    break;
                case VISCAProtocol.ZoomCommand.Stop:
                    await communicator.ZoomStop();
                    break;
            }
            
            OnZoomCommandExecuted?.Invoke(currentlySelectedCamera, command);
            Debug.Log($"[VISCA] Zoom command {command} executed on camera {currentlySelectedCamera + 1}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VISCA] Zoom command failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ZoomTele(int speed = -1)
    {
        return await Zoom(VISCAProtocol.ZoomCommand.TeleVariable, speed);
    }

    public async Task<bool> ZoomWide(int speed = -1)
    {
        return await Zoom(VISCAProtocol.ZoomCommand.WideVariable, speed);
    }

    public async Task<bool> ZoomStop()
    {
        return await Zoom(VISCAProtocol.ZoomCommand.Stop);
    }

    public async Task<bool> SetZoomPosition(int position, int speed = -1)
    {
        var communicator = GetSelectedCommunicator();
        if (communicator == null)
        {
            Debug.LogWarning("[VISCA] No camera selected for zoom position control");
            return false;
        }

        // For UDP communicator, direct zoom positioning not implemented in working reference
        Debug.LogWarning("[VISCA] Direct zoom position commands not implemented in UDP communicator");
        return false;
    }

    // Focus Control Methods
    public async Task<bool> Focus(VISCAProtocol.FocusCommand command, int speed = -1)
    {
        var communicator = GetSelectedCommunicator();
        if (communicator == null)
        {
            Debug.LogWarning("[VISCA] No camera selected for focus control");
            return false;
        }

        try
        {
            switch (command)
            {
                case VISCAProtocol.FocusCommand.Auto:
                    await communicator.FocusAuto();
                    break;
                case VISCAProtocol.FocusCommand.Manual:
                    await communicator.FocusManual();
                    break;
                case VISCAProtocol.FocusCommand.NearVariable:
                    await communicator.FocusNear();
                    break;
                case VISCAProtocol.FocusCommand.FarVariable:
                    await communicator.FocusFar();
                    break;
                case VISCAProtocol.FocusCommand.Stop:
                    await communicator.FocusStop();
                    break;
                case VISCAProtocol.FocusCommand.OnePushAF:
                    await communicator.FocusOnePush();
                    break;
            }
            
            OnFocusCommandExecuted?.Invoke(currentlySelectedCamera, command);
            Debug.Log($"[VISCA] Focus command {command} executed on camera {currentlySelectedCamera + 1}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VISCA] Focus command failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetAutoFocus()
    {
        return await Focus(VISCAProtocol.FocusCommand.Auto);
    }

    public async Task<bool> SetManualFocus()
    {
        return await Focus(VISCAProtocol.FocusCommand.Manual);
    }

    public async Task<bool> FocusNear(int speed = -1)
    {
        return await Focus(VISCAProtocol.FocusCommand.NearVariable, speed);
    }

    public async Task<bool> FocusFar(int speed = -1)
    {
        return await Focus(VISCAProtocol.FocusCommand.FarVariable, speed);
    }

    public async Task<bool> FocusStop()
    {
        return await Focus(VISCAProtocol.FocusCommand.Stop);
    }

    public async Task<bool> OnePushAutoFocus()
    {
        return await Focus(VISCAProtocol.FocusCommand.OnePushAF);
    }

    // Preset Methods
    public async Task<bool> SavePreset(int presetNumber)
    {
        var communicator = GetSelectedCommunicator();
        if (communicator == null)
        {
            Debug.LogWarning("[VISCA] No camera selected for preset save");
            return false;
        }

        try
        {
            await communicator.PresetSet((byte)Mathf.Clamp(presetNumber, 0, 7));
            Debug.Log($"[VISCA] Preset {presetNumber} saved on camera {currentlySelectedCamera + 1}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VISCA] Preset save failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RecallPreset(int presetNumber)
    {
        var communicator = GetSelectedCommunicator();
        if (communicator == null)
        {
            Debug.LogWarning("[VISCA] No camera selected for preset recall");
            return false;
        }

        try
        {
            await communicator.PresetRecall((byte)Mathf.Clamp(presetNumber, 0, 7));
            Debug.Log($"[VISCA] Preset {presetNumber} recalled on camera {currentlySelectedCamera + 1}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VISCA] Preset recall failed: {ex.Message}");
            return false;
        }
    }

    // Status Methods
    public bool IsCameraConnected(int cameraIndex)
    {
        if (cameraIndex >= 0 && cameraIndex < cameraCommunicators.Count)
        {
            return cameraCommunicators[cameraIndex]?.IsConnected ?? false;
        }
        return false;
    }

    public bool IsSelectedCameraConnected()
    {
        var communicator = GetSelectedCommunicator();
        return communicator?.IsConnected ?? false;
    }

    public int GetConnectedCameraCount()
    {
        int count = 0;
        foreach (var communicator in cameraCommunicators)
        {
            if (communicator?.IsConnected == true)
                count++;
        }
        return count;
    }

    public string GetCameraInfo(int cameraIndex)
    {
        if (cameraIndex >= 0 && cameraIndex < cameraCommunicators.Count)
        {
            var comm = cameraCommunicators[cameraIndex];
            return $"{comm?.cameraIP}:{comm?.cameraPort} (Address: {comm?.cameraAddress})";
        }
        return null;
    }

    // Add new camera communicator
    public void AddCamera(VISCAUDPCommunicator communicator)
    {
        if (communicator != null)
        {
            int cameraIndex = cameraCommunicators.Count;
            cameraCommunicators.Add(communicator);
            
            // Subscribe to events
            communicator.OnConnectionChanged += (connected) => OnCameraConnectionChanged?.Invoke(cameraIndex, connected);
            communicator.OnError += (error) => OnCameraError?.Invoke(cameraIndex, error);
            
            Debug.Log($"[VISCA] Added camera {cameraIndex + 1} communicator");
        }
    }

    // Emergency stop all movement
    public async Task<bool> EmergencyStopAll()
    {
        bool allSuccess = true;
        
        var tasks = new List<Task>();
        
        foreach (var communicator in cameraCommunicators)
        {
            if (communicator?.IsConnected == true)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await communicator.Stop();
                        await communicator.ZoomStop();
                        await communicator.FocusStop();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[VISCA] Emergency stop failed for camera: {ex.Message}");
                        allSuccess = false;
                    }
                }));
            }
        }
        
        await Task.WhenAll(tasks);
        
        Debug.Log($"[VISCA] Emergency stop executed on all cameras. Success: {allSuccess}");
        return allSuccess;
    }
    
    [ContextMenu("Force Restart VISCA System")]
    public void ForceRestartVISCASystem()
    {
        Debug.Log("[VISCA] Force restarting VISCA system to pick up new IP addresses...");
        
        // Clear existing communicators
        ClearExistingCommunicators();
        
        // Reset initialization flag
        isInitialized = false;
        
        // Restart the system
        if (Application.isPlaying)
        {
            Start();
        }
        
        Debug.Log("[VISCA] VISCA system restart complete!");
    }
}