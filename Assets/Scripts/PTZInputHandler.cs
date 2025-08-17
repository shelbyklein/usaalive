using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading.Tasks;
using System.Reflection;

public class PTZInputHandler : MonoBehaviour
{
    [Header("VISCA Control")]
    [SerializeField] private VISCAController viscaController;
    
    [Header("Camera Display")]
    [SerializeField] private NDICameraGridManager cameraGridManager;
    
    [Header("Input Settings")]
    [SerializeField] private InputActionAsset ptzInputActions;
    [SerializeField] private bool enableKeyboardControls = true;
    [SerializeField] private bool enableGamepadControls = true;
    
    [Header("Speed Settings")]
    [SerializeField] private int slowSpeed = 3;
    [SerializeField] private int normalSpeed = 8;
    [SerializeField] private int fastSpeed = 15;
    
    [Header("Control Behavior")]
    [SerializeField] private bool holdToMove = true; // If false, toggle mode
    [SerializeField] private float repeatCommandInterval = 0.1f;
    
    // Input Action Maps
    private InputActionMap ptzMovementMap;
    private InputActionMap zoomControlMap;
    private InputActionMap focusControlMap;
    private InputActionMap presetControlMap;
    
    // Input Actions
    private InputAction panTiltAction;
    private InputAction zoomAction;
    private InputAction focusAction;
    private InputAction speedModifierAction;
    private InputAction precisionModifierAction;
    private InputAction stopAllAction;
    private InputAction homeAction;
    private InputAction autoFocusAction;
    private InputAction cycleCameraNextAction;
    private InputAction cycleCameraPreviousAction;
    private InputAction fullScreenToggleAction;
    private InputAction[] presetActions;
    
    // State tracking
    private bool isMoving = false;
    private bool isZooming = false;
    private bool isFocusing = false;
    private Vector2 currentPanTilt = Vector2.zero;
    private float currentZoom = 0f;
    private float currentFocus = 0f;
    
    // Speed tracking for dynamic updates
    private int lastPanTiltSpeed = -1;
    private int lastZoomSpeed = -1;
    private int lastFocusSpeed = -1;
    private VISCAProtocol.PTZCommand lastPTZCommand = VISCAProtocol.PTZCommand.Stop;
    private bool lastSpeedModifierState = false;
    private bool lastPrecisionModifierState = false;
    
    private void Awake()
    {
        if (viscaController == null)
        {
            viscaController = FindObjectOfType<VISCAController>();
        }
        
        if (cameraGridManager == null)
        {
            cameraGridManager = FindObjectOfType<NDICameraGridManager>();
        }
        
        InitializeInputActions();
    }
    
    private void OnEnable()
    {
        EnableInputActions();
    }
    
    private void OnDisable()
    {
        DisableInputActions();
    }
    
    private void InitializeInputActions()
    {
        if (ptzInputActions == null)
        {
            Debug.LogError("[PTZ Input] PTZ Input Actions not assigned!");
            return;
        }
        
        Debug.Log($"[PTZ Input] Initializing with input actions: {ptzInputActions.name}");
        
        // Get action maps
        ptzMovementMap = ptzInputActions.FindActionMap("PTZMovement");
        zoomControlMap = ptzInputActions.FindActionMap("ZoomControl");
        focusControlMap = ptzInputActions.FindActionMap("FocusControl");
        presetControlMap = ptzInputActions.FindActionMap("PresetControl");
        
        if (ptzMovementMap == null)
        {
            Debug.LogWarning("[PTZ Input] PTZMovement action map not found, creating keyboard fallback");
            CreateKeyboardFallback();
            return;
        }
        
        Debug.Log("[PTZ Input] Action maps found successfully, setting up actions...");
        
        // Setup actions
        SetupPTZActions();
        SetupZoomActions();
        SetupFocusActions();
        SetupPresetActions();
        SetupModifierActions();
        
        Debug.Log("[PTZ Input] Input actions initialization complete");
    }
    
    private void CreateKeyboardFallback()
    {
        // If no input actions asset, create keyboard controls directly
        Debug.Log("[PTZ Input] Using keyboard fallback controls");
        enabled = true;
    }
    
    private void SetupPTZActions()
    {
        panTiltAction = ptzMovementMap?.FindAction("PanTilt");
        homeAction = ptzMovementMap?.FindAction("Home");
        stopAllAction = ptzMovementMap?.FindAction("StopAll");
        cycleCameraNextAction = ptzMovementMap?.FindAction("CycleCameraNext");
        cycleCameraPreviousAction = ptzMovementMap?.FindAction("CycleCameraPrevious");
        fullScreenToggleAction = ptzMovementMap?.FindAction("FullScreenToggle");
        
        Debug.Log($"[PTZ Input] Found actions - PanTilt: {panTiltAction != null}, Home: {homeAction != null}, StopAll: {stopAllAction != null}");
        Debug.Log($"[PTZ Input] Found camera cycling actions - Next: {cycleCameraNextAction != null}, Previous: {cycleCameraPreviousAction != null}");
        Debug.Log($"[PTZ Input] Found full screen action: {fullScreenToggleAction != null}");
        
        if (panTiltAction != null)
        {
            panTiltAction.performed += OnPanTiltPerformed;
            panTiltAction.canceled += OnPanTiltCanceled;
        }
        
        if (homeAction != null)
        {
            homeAction.performed += OnHomePerformed;
        }
        
        if (stopAllAction != null)
        {
            stopAllAction.performed += OnStopAllPerformed;
        }
        
        if (cycleCameraNextAction != null)
        {
            cycleCameraNextAction.performed += OnCycleCameraNextPerformed;
            Debug.Log("[PTZ Input] CycleCameraNext action event registered");
        }
        else
        {
            Debug.LogError("[PTZ Input] CycleCameraNext action not found!");
        }
        
        if (cycleCameraPreviousAction != null)
        {
            cycleCameraPreviousAction.performed += OnCycleCameraPreviousPerformed;
            Debug.Log("[PTZ Input] CycleCameraPrevious action event registered");
        }
        else
        {
            Debug.LogError("[PTZ Input] CycleCameraPrevious action not found!");
        }
        
        if (fullScreenToggleAction != null)
        {
            fullScreenToggleAction.performed += OnFullScreenTogglePerformed;
            Debug.Log("[PTZ Input] FullScreenToggle action event registered");
        }
        else
        {
            Debug.LogError("[PTZ Input] FullScreenToggle action not found!");
        }
    }
    
    private void SetupZoomActions()
    {
        zoomAction = zoomControlMap?.FindAction("Zoom");
        
        if (zoomAction != null)
        {
            zoomAction.performed += OnZoomPerformed;
            zoomAction.canceled += OnZoomCanceled;
        }
    }
    
    private void SetupFocusActions()
    {
        focusAction = focusControlMap?.FindAction("Focus");
        autoFocusAction = focusControlMap?.FindAction("AutoFocus");
        
        if (focusAction != null)
        {
            focusAction.performed += OnFocusPerformed;
            focusAction.canceled += OnFocusCanceled;
        }
        
        if (autoFocusAction != null)
        {
            autoFocusAction.performed += OnAutoFocusPerformed;
        }
    }
    
    private void SetupPresetActions()
    {
        // Preset controls disabled - number keys reserved for camera selection
        if (presetControlMap == null) return;
        
        // Skip setting up preset actions to avoid conflicts with camera selection
        Debug.Log("[PTZ Input] Preset controls disabled - use camera selection keys instead");
    }
    
    private void SetupModifierActions()
    {
        speedModifierAction = ptzMovementMap?.FindAction("SpeedModifier");
        precisionModifierAction = ptzMovementMap?.FindAction("PrecisionModifier");
        
        Debug.Log($"[PTZ Input] Speed modifier actions - Speed: {speedModifierAction != null}, Precision: {precisionModifierAction != null}");
    }
    
    private void EnableInputActions()
    {
        ptzMovementMap?.Enable();
        zoomControlMap?.Enable();
        focusControlMap?.Enable();
        // presetControlMap disabled to avoid conflicts with camera selection
    }
    
    private void DisableInputActions()
    {
        ptzMovementMap?.Disable();
        zoomControlMap?.Disable();
        focusControlMap?.Disable();
        // presetControlMap disabled to avoid conflicts with camera selection
    }
    
    // Update method for continuous monitoring
    private void Update()
    {
        // Always monitor for speed changes during movement
        UpdateMovementSpeeds();
        
        // Handle keyboard fallback if no input actions are configured
        if (ptzInputActions == null)
        {
            HandleKeyboardInput();
        }
    }
    
    private void HandleKeyboardInput()
    {
        if (!enableKeyboardControls) return;
        
        // PTZ Movement
        Vector2 panTilt = Vector2.zero;
        
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            panTilt.y = 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            panTilt.y = -1f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            panTilt.x = -1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            panTilt.x = 1f;
            
        if (panTilt != currentPanTilt)
        {
            currentPanTilt = panTilt;
            HandlePanTiltMovement(panTilt);
        }
        
        // Zoom
        float zoom = 0f;
        if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.E))
            zoom = 1f;
        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.Q))
            zoom = -1f;
            
        if (zoom != currentZoom)
        {
            currentZoom = zoom;
            HandleZoomMovement(zoom);
        }
        
        // Focus
        float focus = 0f;
        if (Input.GetKey(KeyCode.F))
            focus = 1f;
        if (Input.GetKey(KeyCode.V))
            focus = -1f;
            
        if (focus != currentFocus)
        {
            currentFocus = focus;
            HandleFocusMovement(focus);
        }
        
        // Presets removed - number keys now only used for camera selection
        
        // Special commands
        if (Input.GetKeyDown(KeyCode.H))
            HandleHomeCommand();
        if (Input.GetKeyDown(KeyCode.Space))
            HandleFullScreenToggle();
        if (Input.GetKeyDown(KeyCode.Tab))
            HandleAutoFocusCommand();
    }
    
    // Input System Event Handlers
    private void OnPanTiltPerformed(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        currentPanTilt = input;
        HandlePanTiltMovement(input);
    }
    
    private void OnPanTiltCanceled(InputAction.CallbackContext context)
    {
        currentPanTilt = Vector2.zero;
        HandlePanTiltMovement(Vector2.zero);
    }
    
    private void OnZoomPerformed(InputAction.CallbackContext context)
    {
        float input = context.ReadValue<float>();
        currentZoom = input;
        HandleZoomMovement(input);
    }
    
    private void OnZoomCanceled(InputAction.CallbackContext context)
    {
        currentZoom = 0f;
        HandleZoomMovement(0f);
    }
    
    private void OnFocusPerformed(InputAction.CallbackContext context)
    {
        float input = context.ReadValue<float>();
        currentFocus = input;
        HandleFocusMovement(input);
    }
    
    private void OnFocusCanceled(InputAction.CallbackContext context)
    {
        currentFocus = 0f;
        HandleFocusMovement(0f);
    }
    
    private void OnHomePerformed(InputAction.CallbackContext context)
    {
        HandleHomeCommand();
    }
    
    private void OnStopAllPerformed(InputAction.CallbackContext context)
    {
        HandleStopAllCommand();
    }
    
    private void OnAutoFocusPerformed(InputAction.CallbackContext context)
    {
        HandleAutoFocusCommand();
    }
    
    private void OnCycleCameraNextPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("[PTZ Input] R1 (CycleCameraNext) button pressed!");
        HandleCycleCameraNext();
    }
    
    private void OnCycleCameraPreviousPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("[PTZ Input] L1 (CycleCameraPrevious) button pressed!");
        HandleCycleCameraPrevious();
    }
    
    private void OnFullScreenTogglePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("[PTZ Input] North button (FullScreenToggle) pressed!");
        HandleFullScreenToggle();
    }
    
    private void OnPresetPerformed(int presetNumber)
    {
        HandlePresetCommand(presetNumber);
    }
    
    // Movement Handlers
    private async void HandlePanTiltMovement(Vector2 input)
    {
        if (viscaController == null) return;
        
        if (input.magnitude < 0.1f)
        {
            // Stop movement
            if (isMoving)
            {
                await viscaController.StopPanTilt();
                isMoving = false;
                lastPanTiltSpeed = -1;
                lastPTZCommand = VISCAProtocol.PTZCommand.Stop;
            }
            return;
        }
        
        // Determine movement direction
        var command = GetPTZCommandFromInput(input);
        int speed = GetCurrentSpeed();
        
        if (command != VISCAProtocol.PTZCommand.Stop)
        {
            await viscaController.MovePanTilt(command, speed, speed);
            isMoving = true;
            
            // Store current movement data for dynamic speed updates
            lastPTZCommand = command;
            lastPanTiltSpeed = speed;
            lastSpeedModifierState = speedModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
            lastPrecisionModifierState = precisionModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftControl);
        }
    }
    
    private async void HandleZoomMovement(float input)
    {
        if (viscaController == null) return;
        
        if (Mathf.Abs(input) < 0.1f)
        {
            // Stop zoom
            if (isZooming)
            {
                await viscaController.ZoomStop();
                isZooming = false;
                lastZoomSpeed = -1;
            }
            return;
        }
        
        int speed = GetCurrentZoomSpeed();
        
        if (input > 0)
        {
            await viscaController.ZoomTele(speed);
        }
        else
        {
            await viscaController.ZoomWide(speed);
        }
        
        isZooming = true;
        
        // Store current zoom data for dynamic speed updates
        lastZoomSpeed = speed;
        lastSpeedModifierState = speedModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
        lastPrecisionModifierState = precisionModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftControl);
    }
    
    private async void HandleFocusMovement(float input)
    {
        if (viscaController == null) return;
        
        if (Mathf.Abs(input) < 0.1f)
        {
            // Stop focus
            if (isFocusing)
            {
                await viscaController.FocusStop();
                isFocusing = false;
                lastFocusSpeed = -1;
            }
            return;
        }
        
        int speed = GetCurrentFocusSpeed();
        
        if (input > 0)
        {
            await viscaController.FocusFar(speed);
        }
        else
        {
            await viscaController.FocusNear(speed);
        }
        
        isFocusing = true;
        
        // Store current focus data for dynamic speed updates
        lastFocusSpeed = speed;
        lastSpeedModifierState = speedModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
        lastPrecisionModifierState = precisionModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftControl);
    }
    
    // Command Handlers
    private async void HandleHomeCommand()
    {
        if (viscaController == null) return;
        
        Debug.Log("[PTZ Input] Home command");
        await viscaController.PanTiltHome();
    }
    
    private async void HandleStopAllCommand()
    {
        if (viscaController == null) return;
        
        Debug.Log("[PTZ Input] Stop all command");
        await viscaController.EmergencyStopAll();
        isMoving = false;
        isZooming = false;
        isFocusing = false;
        
        // Reset speed tracking
        lastPanTiltSpeed = -1;
        lastZoomSpeed = -1;
        lastFocusSpeed = -1;
        lastPTZCommand = VISCAProtocol.PTZCommand.Stop;
    }
    
    private async void HandleAutoFocusCommand()
    {
        if (viscaController == null) return;
        
        Debug.Log("[PTZ Input] Auto focus command");
        await viscaController.OnePushAutoFocus();
    }
    
    private void HandleFullScreenToggle()
    {
        if (cameraGridManager == null)
        {
            Debug.LogWarning("[PTZ Input] Camera Grid Manager not found - cannot toggle full screen");
            return;
        }
        
        // Get currently selected camera from input manager
        var cameraInputManager = FindObjectOfType<CameraInputManager>();
        if (cameraInputManager == null || !cameraInputManager.IsCameraSelected())
        {
            Debug.LogWarning("[PTZ Input] No camera selected - cannot toggle full screen");
            return;
        }
        
        int selectedCameraIndex = cameraInputManager.GetCurrentlySelectedCamera();
        Debug.Log($"[PTZ Input] Toggling full screen for camera {selectedCameraIndex + 1}");
        
        cameraGridManager.ToggleFullScreen(selectedCameraIndex);
    }
    
    private void HandleCycleCameraNext()
    {
        var cameraInputManager = FindObjectOfType<CameraInputManager>();
        if (cameraInputManager == null)
        {
            Debug.LogWarning("[PTZ Input] Camera Input Manager not found - cannot cycle cameras");
            return;
        }
        
        if (cameraGridManager == null)
        {
            Debug.LogWarning("[PTZ Input] Camera Grid Manager not found - cannot cycle cameras");
            return;
        }
        
        int totalCameras = cameraGridManager.GetCameraCount();
        if (totalCameras == 0)
        {
            Debug.LogWarning("[PTZ Input] No cameras available for cycling");
            return;
        }
        
        int currentCamera = cameraInputManager.GetCurrentlySelectedCamera();
        int nextCamera;
        
        if (currentCamera < 0)
        {
            // No camera currently selected, start with camera 0
            nextCamera = 0;
            Debug.Log($"[PTZ Input] No camera selected, selecting first camera: {nextCamera + 1}");
        }
        else
        {
            nextCamera = (currentCamera + 1) % totalCameras;
            Debug.Log($"[PTZ Input] Cycling to next camera: {currentCamera + 1} → {nextCamera + 1}");
        }
        
        // Stop movement on current camera before cycling if different from next
        if (currentCamera >= 0 && currentCamera != nextCamera)
        {
            if (viscaController != null)
            {
                viscaController.StopCameraMovement(currentCamera);
                Debug.Log($"[PTZ Input] Stopped Camera {currentCamera + 1} before cycling to {nextCamera + 1}");
            }
        }
        
        // Use the existing camera selection system through reflection or direct method call
        var selectCameraMethod = typeof(CameraInputManager).GetMethod("SelectCamera", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (selectCameraMethod != null)
        {
            selectCameraMethod.Invoke(cameraInputManager, new object[] { nextCamera });
        }
        else
        {
            Debug.LogError("[PTZ Input] Could not access SelectCamera method");
        }
    }
    
    private void HandleCycleCameraPrevious()
    {
        var cameraInputManager = FindObjectOfType<CameraInputManager>();
        if (cameraInputManager == null)
        {
            Debug.LogWarning("[PTZ Input] Camera Input Manager not found - cannot cycle cameras");
            return;
        }
        
        if (cameraGridManager == null)
        {
            Debug.LogWarning("[PTZ Input] Camera Grid Manager not found - cannot cycle cameras");
            return;
        }
        
        int totalCameras = cameraGridManager.GetCameraCount();
        if (totalCameras == 0)
        {
            Debug.LogWarning("[PTZ Input] No cameras available for cycling");
            return;
        }
        
        int currentCamera = cameraInputManager.GetCurrentlySelectedCamera();
        int previousCamera;
        
        if (currentCamera < 0)
        {
            // No camera currently selected, start with last camera
            previousCamera = totalCameras - 1;
            Debug.Log($"[PTZ Input] No camera selected, selecting last camera: {previousCamera + 1}");
        }
        else
        {
            previousCamera = (currentCamera - 1 + totalCameras) % totalCameras;
            Debug.Log($"[PTZ Input] Cycling to previous camera: {currentCamera + 1} → {previousCamera + 1}");
        }
        
        // Stop movement on current camera before cycling if different from previous
        if (currentCamera >= 0 && currentCamera != previousCamera)
        {
            if (viscaController != null)
            {
                viscaController.StopCameraMovement(currentCamera);
                Debug.Log($"[PTZ Input] Stopped Camera {currentCamera + 1} before cycling to {previousCamera + 1}");
            }
        }
        
        // Use the existing camera selection system through reflection or direct method call
        var selectCameraMethod = typeof(CameraInputManager).GetMethod("SelectCamera", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (selectCameraMethod != null)
        {
            selectCameraMethod.Invoke(cameraInputManager, new object[] { previousCamera });
        }
        else
        {
            Debug.LogError("[PTZ Input] Could not access SelectCamera method");
        }
    }
    
    private async void HandlePresetCommand(int presetNumber)
    {
        if (viscaController == null) return;
        
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        if (isShiftHeld)
        {
            // Save preset
            Debug.Log($"[PTZ Input] Saving preset {presetNumber}");
            await viscaController.SavePreset(presetNumber);
        }
        else
        {
            // Recall preset
            Debug.Log($"[PTZ Input] Recalling preset {presetNumber}");
            await viscaController.RecallPreset(presetNumber);
        }
    }
    
    // Helper Methods
    private VISCAProtocol.PTZCommand GetPTZCommandFromInput(Vector2 input)
    {
        float threshold = 0.5f;
        
        bool up = input.y > threshold;
        bool down = input.y < -threshold;
        bool left = input.x < -threshold;
        bool right = input.x > threshold;
        
        if (up && left) return VISCAProtocol.PTZCommand.UpLeft;
        if (up && right) return VISCAProtocol.PTZCommand.UpRight;
        if (down && left) return VISCAProtocol.PTZCommand.DownLeft;
        if (down && right) return VISCAProtocol.PTZCommand.DownRight;
        if (up) return VISCAProtocol.PTZCommand.Up;
        if (down) return VISCAProtocol.PTZCommand.Down;
        if (left) return VISCAProtocol.PTZCommand.Left;
        if (right) return VISCAProtocol.PTZCommand.Right;
        
        return VISCAProtocol.PTZCommand.Stop;
    }
    
    private int GetCurrentSpeed()
    {
        bool speedModifier = speedModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
        bool precisionModifier = precisionModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftControl);
        
        if (precisionModifier) 
        {
            return slowSpeed;
        }
        if (speedModifier) 
        {
            return fastSpeed;
        }
        return normalSpeed;
    }
    
    private int GetCurrentZoomSpeed()
    {
        bool speedModifier = speedModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
        bool precisionModifier = precisionModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftControl);
        
        if (precisionModifier) return 1;
        if (speedModifier) return 7;
        return 4;
    }
    
    private int GetCurrentFocusSpeed()
    {
        bool speedModifier = speedModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
        bool precisionModifier = precisionModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftControl);
        
        if (precisionModifier) return 1;
        if (speedModifier) return 7;
        return 3;
    }
    
    // Dynamic speed update system
    private async void UpdateMovementSpeeds()
    {
        if (viscaController == null) return;
        
        // Check current modifier states
        bool currentSpeedModifier = speedModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftShift);
        bool currentPrecisionModifier = precisionModifierAction?.IsPressed() ?? Input.GetKey(KeyCode.LeftControl);
        
        // Check if modifier states have changed
        bool modifiersChanged = (currentSpeedModifier != lastSpeedModifierState) || 
                               (currentPrecisionModifier != lastPrecisionModifierState);
        
        if (modifiersChanged)
        {
            // Update PTZ movement speed if currently moving
            if (isMoving && currentPanTilt.magnitude > 0.1f)
            {
                int newSpeed = GetCurrentSpeed();
                if (newSpeed != lastPanTiltSpeed)
                {
                    Debug.Log($"[PTZ Input] Speed changed during movement: {lastPanTiltSpeed} → {newSpeed}");
                    await viscaController.MovePanTilt(lastPTZCommand, newSpeed, newSpeed);
                    lastPanTiltSpeed = newSpeed;
                }
            }
            
            // Update zoom speed if currently zooming
            if (isZooming && Mathf.Abs(currentZoom) > 0.1f)
            {
                int newZoomSpeed = GetCurrentZoomSpeed();
                if (newZoomSpeed != lastZoomSpeed)
                {
                    Debug.Log($"[PTZ Input] Zoom speed changed during movement: {lastZoomSpeed} → {newZoomSpeed}");
                    if (currentZoom > 0)
                    {
                        await viscaController.ZoomTele(newZoomSpeed);
                    }
                    else
                    {
                        await viscaController.ZoomWide(newZoomSpeed);
                    }
                    lastZoomSpeed = newZoomSpeed;
                }
            }
            
            // Update focus speed if currently focusing
            if (isFocusing && Mathf.Abs(currentFocus) > 0.1f)
            {
                int newFocusSpeed = GetCurrentFocusSpeed();
                if (newFocusSpeed != lastFocusSpeed)
                {
                    Debug.Log($"[PTZ Input] Focus speed changed during movement: {lastFocusSpeed} → {newFocusSpeed}");
                    if (currentFocus > 0)
                    {
                        await viscaController.FocusFar(newFocusSpeed);
                    }
                    else
                    {
                        await viscaController.FocusNear(newFocusSpeed);
                    }
                    lastFocusSpeed = newFocusSpeed;
                }
            }
            
            // Update the stored modifier states
            lastSpeedModifierState = currentSpeedModifier;
            lastPrecisionModifierState = currentPrecisionModifier;
        }
    }
    
    // Public methods for external control
    public void SetVISCAController(VISCAController controller)
    {
        viscaController = controller;
    }
    
    public void EnableControls(bool enable)
    {
        enableKeyboardControls = enable;
        
        if (enable)
        {
            EnableInputActions();
        }
        else
        {
            DisableInputActions();
            // Stop any ongoing movement
            HandleStopAllCommand();
        }
    }
    
    public bool AreControlsEnabled()
    {
        return enableKeyboardControls;
    }
}