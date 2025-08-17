using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

public class PTZInputHandler : MonoBehaviour
{
    [Header("VISCA Control")]
    [SerializeField] private VISCAController viscaController;
    
    [Header("Input Settings")]
    [SerializeField] private InputActionAsset ptzInputActions;
    [SerializeField] private bool enableKeyboardControls = true;
    [SerializeField] private bool enableGamepadControls = false;
    
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
    private InputAction[] presetActions;
    
    // State tracking
    private bool isMoving = false;
    private bool isZooming = false;
    private bool isFocusing = false;
    private Vector2 currentPanTilt = Vector2.zero;
    private float currentZoom = 0f;
    private float currentFocus = 0f;
    
    private void Awake()
    {
        if (viscaController == null)
        {
            viscaController = FindObjectOfType<VISCAController>();
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
            Debug.LogError("PTZ Input Actions not assigned!");
            return;
        }
        
        // Get action maps
        ptzMovementMap = ptzInputActions.FindActionMap("PTZMovement");
        zoomControlMap = ptzInputActions.FindActionMap("ZoomControl");
        focusControlMap = ptzInputActions.FindActionMap("FocusControl");
        presetControlMap = ptzInputActions.FindActionMap("PresetControl");
        
        if (ptzMovementMap == null)
        {
            Debug.LogWarning("PTZMovement action map not found, creating keyboard fallback");
            CreateKeyboardFallback();
            return;
        }
        
        // Setup actions
        SetupPTZActions();
        SetupZoomActions();
        SetupFocusActions();
        SetupPresetActions();
        SetupModifierActions();
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
        speedModifierAction = ptzInputActions.FindAction("SpeedModifier");
        precisionModifierAction = ptzInputActions.FindAction("PrecisionModifier");
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
    
    // Keyboard fallback for when no input actions are configured
    private void Update()
    {
        if (ptzInputActions != null) return; // Use input system if available
        
        HandleKeyboardInput();
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
            HandleStopAllCommand();
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
    }
    
    private async void HandleAutoFocusCommand()
    {
        if (viscaController == null) return;
        
        Debug.Log("[PTZ Input] Auto focus command");
        await viscaController.OnePushAutoFocus();
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
        
        if (precisionModifier) return slowSpeed;
        if (speedModifier) return fastSpeed;
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