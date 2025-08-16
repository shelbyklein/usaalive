using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using USAALive.VISCA;
using USAALive.Networking;

namespace USAALive.Input
{
    public class CameraInputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private float _deadZone = 0.1f;
        [SerializeField] private float _maxSpeed = 1.0f;
        [SerializeField] private float _speedSensitivity = 2.0f;
        [SerializeField] private bool _invertPanAxis = false;
        [SerializeField] private bool _invertTiltAxis = false;

        [Header("Command Buffering")]
        [SerializeField] private float _commandInterval = 0.1f;
        [SerializeField] private int _maxQueuedCommands = 10;
        [SerializeField] private bool _enableCommandBuffering = true;

        [Header("Speed Control")]
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _minCameraSpeed = 0x01;
        [SerializeField] private float _maxCameraSpeed = 0x18;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = true;

        public event Action<VISCACommand, Dictionary<string, object>> OnCommandGenerated;

        private InputActionAsset _inputActions;
        private CameraConnectionManager _connectionManager;
        private Queue<CommandBufferEntry> _commandBuffer = new Queue<CommandBufferEntry>();
        private Coroutine _commandProcessingCoroutine;
        private bool _isProcessingCommands = false;

        // Input state tracking
        private Vector2 _currentPanTiltInput = Vector2.zero;
        private float _currentZoomInput = 0f;
        private float _currentFocusInput = 0f;
        private float _speedModifier = 1.0f;
        private bool _isPTZMoving = false;
        private bool _isZooming = false;
        private bool _isFocusing = false;

        // Preset handling
        private bool _isSettingPreset = false;

        private struct CommandBufferEntry
        {
            public VISCACommand command;
            public Dictionary<string, object> parameters;
            public float timestamp;
        }

        #region Unity Lifecycle

        private void Awake()
        {
            // Load the input actions asset
            _inputActions = Resources.Load<InputActionAsset>("CameraControls");
            if (_inputActions == null)
            {
                LogError("CameraControls InputActionAsset not found in Resources folder!");
            }
            
            _connectionManager = FindObjectOfType<CameraConnectionManager>();

            if (_connectionManager == null)
            {
                LogError("CameraConnectionManager not found! Input handler will not function properly.");
            }
        }

        private void OnEnable()
        {
            EnableInputActions();
            StartCommandProcessing();
        }

        private void OnDisable()
        {
            DisableInputActions();
            StopCommandProcessing();
        }

        private void OnDestroy()
        {
            // InputActionAsset doesn't need explicit disposal
            // Cleanup is handled by DisableInputActions() in OnDisable()
        }

        #endregion

        #region Input Action Setup

        private void EnableInputActions()
        {
            if (_inputActions == null) return;
            
            _inputActions.Enable();
            
            // PTZ Actions
            var ptzMap = _inputActions.FindActionMap("PTZ");
            if (ptzMap != null)
            {
                ptzMap.FindAction("PanTilt").performed += OnPanTiltPerformed;
                ptzMap.FindAction("PanTilt").canceled += OnPanTiltCanceled;
                ptzMap.FindAction("Up").performed += OnPTZButtonPressed;
                ptzMap.FindAction("Down").performed += OnPTZButtonPressed;
                ptzMap.FindAction("Left").performed += OnPTZButtonPressed;
                ptzMap.FindAction("Right").performed += OnPTZButtonPressed;
                ptzMap.FindAction("Home").performed += OnHomePressed;
                ptzMap.FindAction("Stop").performed += OnStopPressed;
                ptzMap.FindAction("SpeedModifier").performed += OnSpeedModifierChanged;
                ptzMap.FindAction("SpeedModifier").canceled += OnSpeedModifierCanceled;
            }

            // Zoom Actions
            var zoomMap = _inputActions.FindActionMap("Zoom");
            if (zoomMap != null)
            {
                zoomMap.FindAction("ZoomIn").performed += OnZoomInPressed;
                zoomMap.FindAction("ZoomIn").canceled += OnZoomInCanceled;
                zoomMap.FindAction("ZoomOut").performed += OnZoomOutPressed;
                zoomMap.FindAction("ZoomOut").canceled += OnZoomOutCanceled;
                zoomMap.FindAction("ZoomAxis").performed += OnZoomAxisPerformed;
                zoomMap.FindAction("ZoomAxis").canceled += OnZoomAxisCanceled;
                zoomMap.FindAction("ZoomSpeed").performed += OnZoomSpeedChanged;
            }

            // Focus Actions
            var focusMap = _inputActions.FindActionMap("Focus");
            if (focusMap != null)
            {
                focusMap.FindAction("FocusNear").performed += OnFocusNearPressed;
                focusMap.FindAction("FocusNear").canceled += OnFocusNearCanceled;
                focusMap.FindAction("FocusFar").performed += OnFocusFarPressed;
                focusMap.FindAction("FocusFar").canceled += OnFocusFarCanceled;
                focusMap.FindAction("FocusAuto").performed += OnFocusAutoPressed;
                focusMap.FindAction("FocusManual").performed += OnFocusManualPressed;
                focusMap.FindAction("OnePushAF").performed += OnOnePushAFPressed;
                focusMap.FindAction("FocusAxis").performed += OnFocusAxisPerformed;
                focusMap.FindAction("FocusAxis").canceled += OnFocusAxisCanceled;
            }

            // Image Actions
            var imageMap = _inputActions.FindActionMap("Image");
            if (imageMap != null)
            {
                imageMap.FindAction("ExposureMode").performed += OnExposureModeChanged;
                imageMap.FindAction("WhiteBalance").performed += OnWhiteBalanceChanged;
                imageMap.FindAction("IrisUp").performed += OnIrisUpPressed;
                imageMap.FindAction("IrisDown").performed += OnIrisDownPressed;
                imageMap.FindAction("ShutterUp").performed += OnShutterUpPressed;
                imageMap.FindAction("ShutterDown").performed += OnShutterDownPressed;
                imageMap.FindAction("GainUp").performed += OnGainUpPressed;
                imageMap.FindAction("GainDown").performed += OnGainDownPressed;
            }

            // System Actions
            var systemMap = _inputActions.FindActionMap("System");
            if (systemMap != null)
            {
                systemMap.FindAction("Preset1").performed += ctx => OnPresetPressed(1);
                systemMap.FindAction("Preset2").performed += ctx => OnPresetPressed(2);
                systemMap.FindAction("Preset3").performed += ctx => OnPresetPressed(3);
                systemMap.FindAction("SetPreset").performed += OnSetPresetPressed;
                systemMap.FindAction("SetPreset").canceled += OnSetPresetReleased;
                systemMap.FindAction("PowerToggle").performed += OnPowerTogglePressed;
                systemMap.FindAction("NextCamera").performed += OnNextCameraPressed;
                systemMap.FindAction("PrevCamera").performed += OnPrevCameraPressed;
            }

            LogDebug("Input actions enabled");
        }

        private void DisableInputActions()
        {
            _inputActions?.Disable();
            LogDebug("Input actions disabled");
        }

        #endregion

        #region PTZ Input Handlers

        private void OnPanTiltPerformed(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            input = ApplyDeadZone(input);
            
            if (_invertPanAxis) input.x = -input.x;
            if (_invertTiltAxis) input.y = -input.y;

            _currentPanTiltInput = input;

            if (input.magnitude > 0)
            {
                StartPTZMovement(input);
            }
            else
            {
                StopPTZMovement();
            }
        }

        private void OnPanTiltCanceled(InputAction.CallbackContext context)
        {
            _currentPanTiltInput = Vector2.zero;
            StopPTZMovement();
        }

        private void OnPTZButtonPressed(InputAction.CallbackContext context)
        {
            string actionName = context.action.name;
            Vector2 direction = Vector2.zero;

            switch (actionName)
            {
                case "Up": direction = Vector2.up; break;
                case "Down": direction = Vector2.down; break;
                case "Left": direction = Vector2.left; break;
                case "Right": direction = Vector2.right; break;
            }

            if (_invertPanAxis && (direction == Vector2.left || direction == Vector2.right))
                direction.x = -direction.x;
            if (_invertTiltAxis && (direction == Vector2.up || direction == Vector2.down))
                direction.y = -direction.y;

            StartPTZMovement(direction);
        }

        private void StartPTZMovement(Vector2 direction)
        {
            if (direction.magnitude < _deadZone)
            {
                StopPTZMovement();
                return;
            }

            _isPTZMoving = true;
            
            var speeds = CalculatePTZSpeeds(direction);
            var parameters = new Dictionary<string, object>
            {
                ["panspeed"] = speeds.panSpeed,
                ["tiltspeed"] = speeds.tiltSpeed
            };

            VISCACommand command = GetPTZDirectionCommand(direction);
            if (command != null)
            {
                QueueCommand(command, parameters);
            }
        }

        private void StopPTZMovement()
        {
            if (!_isPTZMoving) return;
            
            _isPTZMoving = false;
            var command = VISCACommandLibrary.GetCommand("PTZ_Stop");
            if (command != null)
            {
                QueueCommand(command, null);
            }
        }

        private VISCACommand GetPTZDirectionCommand(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;

            // Determine direction based on angle
            if (angle >= 337.5f || angle < 22.5f)
                return VISCACommandLibrary.GetCommand("PTZ_Right");
            else if (angle >= 22.5f && angle < 67.5f)
                return VISCACommandLibrary.GetCommand("PTZ_UpRight");
            else if (angle >= 67.5f && angle < 112.5f)
                return VISCACommandLibrary.GetCommand("PTZ_Up");
            else if (angle >= 112.5f && angle < 157.5f)
                return VISCACommandLibrary.GetCommand("PTZ_UpLeft");
            else if (angle >= 157.5f && angle < 202.5f)
                return VISCACommandLibrary.GetCommand("PTZ_Left");
            else if (angle >= 202.5f && angle < 247.5f)
                return VISCACommandLibrary.GetCommand("PTZ_DownLeft");
            else if (angle >= 247.5f && angle < 292.5f)
                return VISCACommandLibrary.GetCommand("PTZ_Down");
            else if (angle >= 292.5f && angle < 337.5f)
                return VISCACommandLibrary.GetCommand("PTZ_DownRight");

            return null;
        }

        private (byte panSpeed, byte tiltSpeed) CalculatePTZSpeeds(Vector2 direction)
        {
            float magnitude = Mathf.Clamp01(direction.magnitude * _speedModifier * _maxSpeed);
            float curveValue = _speedCurve.Evaluate(magnitude);
            
            float speedRange = _maxCameraSpeed - _minCameraSpeed;
            byte baseSpeed = (byte)(_minCameraSpeed + (curveValue * speedRange));
            
            byte panSpeed = (byte)(baseSpeed * Mathf.Abs(direction.x));
            byte tiltSpeed = (byte)(baseSpeed * Mathf.Abs(direction.y));
            
            panSpeed = (byte)Mathf.Clamp(panSpeed, _minCameraSpeed, _maxCameraSpeed);
            tiltSpeed = (byte)Mathf.Clamp(tiltSpeed, _minCameraSpeed, _maxCameraSpeed);
            
            return (panSpeed, tiltSpeed);
        }

        private void OnHomePressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("PTZ_Home");
            QueueCommand(command, null);
        }

        private void OnStopPressed(InputAction.CallbackContext context)
        {
            StopPTZMovement();
            StopZooming();
            StopFocusing();
        }

        private void OnSpeedModifierChanged(InputAction.CallbackContext context)
        {
            _speedModifier = 1.0f + context.ReadValue<float>();
        }

        private void OnSpeedModifierCanceled(InputAction.CallbackContext context)
        {
            _speedModifier = 1.0f;
        }

        #endregion

        #region Zoom Input Handlers

        private void OnZoomInPressed(InputAction.CallbackContext context)
        {
            StartZooming(1.0f);
        }

        private void OnZoomInCanceled(InputAction.CallbackContext context)
        {
            StopZooming();
        }

        private void OnZoomOutPressed(InputAction.CallbackContext context)
        {
            StartZooming(-1.0f);
        }

        private void OnZoomOutCanceled(InputAction.CallbackContext context)
        {
            StopZooming();
        }

        private void OnZoomAxisPerformed(InputAction.CallbackContext context)
        {
            _currentZoomInput = context.ReadValue<float>();
            
            if (Mathf.Abs(_currentZoomInput) > _deadZone)
            {
                StartZooming(_currentZoomInput);
            }
            else
            {
                StopZooming();
            }
        }

        private void OnZoomAxisCanceled(InputAction.CallbackContext context)
        {
            _currentZoomInput = 0f;
            StopZooming();
        }

        private void StartZooming(float direction)
        {
            _isZooming = true;
            
            var command = direction > 0 
                ? VISCACommandLibrary.GetCommand("Zoom_TeleVariable")
                : VISCACommandLibrary.GetCommand("Zoom_WideVariable");
            
            var parameters = new Dictionary<string, object>
            {
                ["zoomspeed"] = CalculateZoomSpeed(Mathf.Abs(direction))
            };
            
            QueueCommand(command, parameters);
        }

        private void StopZooming()
        {
            if (!_isZooming) return;
            
            _isZooming = false;
            var command = VISCACommandLibrary.GetCommand("Zoom_Stop");
            QueueCommand(command, null);
        }

        private byte CalculateZoomSpeed(float inputMagnitude)
        {
            float magnitude = Mathf.Clamp01(inputMagnitude * _speedModifier);
            return (byte)Mathf.Clamp(Mathf.RoundToInt(magnitude * 7), 0, 7);
        }

        private void OnZoomSpeedChanged(InputAction.CallbackContext context)
        {
            // Zoom speed modifier handled in CalculateZoomSpeed
        }

        #endregion

        #region Focus Input Handlers

        private void OnFocusNearPressed(InputAction.CallbackContext context)
        {
            StartFocusing(-1.0f);
        }

        private void OnFocusNearCanceled(InputAction.CallbackContext context)
        {
            StopFocusing();
        }

        private void OnFocusFarPressed(InputAction.CallbackContext context)
        {
            StartFocusing(1.0f);
        }

        private void OnFocusFarCanceled(InputAction.CallbackContext context)
        {
            StopFocusing();
        }

        private void OnFocusAxisPerformed(InputAction.CallbackContext context)
        {
            _currentFocusInput = context.ReadValue<float>();
            
            if (Mathf.Abs(_currentFocusInput) > _deadZone)
            {
                StartFocusing(_currentFocusInput);
            }
            else
            {
                StopFocusing();
            }
        }

        private void OnFocusAxisCanceled(InputAction.CallbackContext context)
        {
            _currentFocusInput = 0f;
            StopFocusing();
        }

        private void StartFocusing(float direction)
        {
            _isFocusing = true;
            
            var command = direction > 0 
                ? VISCACommandLibrary.GetCommand("Focus_FarVariable")
                : VISCACommandLibrary.GetCommand("Focus_NearVariable");
            
            var parameters = new Dictionary<string, object>
            {
                ["p"] = CalculateFocusSpeed(Mathf.Abs(direction))
            };
            
            QueueCommand(command, parameters);
        }

        private void StopFocusing()
        {
            if (!_isFocusing) return;
            
            _isFocusing = false;
            var command = VISCACommandLibrary.GetCommand("Focus_Stop");
            QueueCommand(command, null);
        }

        private byte CalculateFocusSpeed(float inputMagnitude)
        {
            float magnitude = Mathf.Clamp01(inputMagnitude * _speedModifier);
            return (byte)Mathf.Clamp(Mathf.RoundToInt(magnitude * 7), 0, 7);
        }

        private void OnFocusAutoPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Focus_Auto");
            QueueCommand(command, null);
        }

        private void OnFocusManualPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Focus_Manual");
            QueueCommand(command, null);
        }

        private void OnOnePushAFPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Focus_OnePushAF");
            QueueCommand(command, null);
        }

        #endregion

        #region Image Control Handlers

        private void OnExposureModeChanged(InputAction.CallbackContext context)
        {
            int mode = Mathf.RoundToInt(context.ReadValue<float>());
            VISCACommand command = mode switch
            {
                1 => VISCACommandLibrary.GetCommand("AE_Manual"),
                2 => VISCACommandLibrary.GetCommand("AE_ShutterPriority"),
                3 => VISCACommandLibrary.GetCommand("AE_IrisPriority"),
                4 => VISCACommandLibrary.GetCommand("AE_Bright"),
                _ => null
            };
            
            if (command != null)
                QueueCommand(command, null);
        }

        private void OnWhiteBalanceChanged(InputAction.CallbackContext context)
        {
            int mode = Mathf.RoundToInt(context.ReadValue<float>());
            VISCACommand command = mode switch
            {
                1 => VISCACommandLibrary.GetCommand("WB_Indoor"),
                2 => VISCACommandLibrary.GetCommand("WB_Outdoor"),
                3 => VISCACommandLibrary.GetCommand("WB_OnePush"),
                4 => VISCACommandLibrary.GetCommand("WB_Auto"),
                _ => null
            };
            
            if (command != null)
                QueueCommand(command, null);
        }

        private void OnIrisUpPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Iris_Up");
            QueueCommand(command, null);
        }

        private void OnIrisDownPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Iris_Down");
            QueueCommand(command, null);
        }

        private void OnShutterUpPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Shutter_Up");
            QueueCommand(command, null);
        }

        private void OnShutterDownPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Shutter_Down");
            QueueCommand(command, null);
        }

        private void OnGainUpPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Gain_Up");
            QueueCommand(command, null);
        }

        private void OnGainDownPressed(InputAction.CallbackContext context)
        {
            var command = VISCACommandLibrary.GetCommand("Gain_Down");
            QueueCommand(command, null);
        }

        #endregion

        #region System Control Handlers

        private void OnPresetPressed(int presetNumber)
        {
            if (_isSettingPreset)
            {
                // Set preset
                var setCommand = VISCACommandLibrary.GetCommand("Memory_Set");
                var parameters = new Dictionary<string, object> { ["p"] = (byte)presetNumber };
                QueueCommand(setCommand, parameters);
                LogDebug($"Setting preset {presetNumber}");
            }
            else
            {
                // Recall preset
                var recallCommand = VISCACommandLibrary.GetCommand("Memory_Recall");
                var parameters = new Dictionary<string, object> { ["p"] = (byte)presetNumber };
                QueueCommand(recallCommand, parameters);
                LogDebug($"Recalling preset {presetNumber}");
            }
        }

        private void OnSetPresetPressed(InputAction.CallbackContext context)
        {
            _isSettingPreset = true;
            LogDebug("Preset setting mode enabled - press preset key to set");
        }

        private void OnSetPresetReleased(InputAction.CallbackContext context)
        {
            _isSettingPreset = false;
            LogDebug("Preset setting mode disabled");
        }

        private void OnPowerTogglePressed(InputAction.CallbackContext context)
        {
            // Toggle between power on and off commands
            var command = VISCACommandLibrary.GetCommand("Power_On");
            QueueCommand(command, null);
        }

        private void OnNextCameraPressed(InputAction.CallbackContext context)
        {
            if (_connectionManager != null)
            {
                int nextCamera = _connectionManager.GetNextConnectedCamera();
                if (nextCamera >= 0)
                {
                    _connectionManager.SetActiveCamera(nextCamera);
                    LogDebug($"Switched to next camera: {nextCamera}");
                }
            }
        }

        private void OnPrevCameraPressed(InputAction.CallbackContext context)
        {
            if (_connectionManager != null)
            {
                int prevCamera = _connectionManager.GetPreviousConnectedCamera();
                if (prevCamera >= 0)
                {
                    _connectionManager.SetActiveCamera(prevCamera);
                    LogDebug($"Switched to previous camera: {prevCamera}");
                }
            }
        }

        #endregion

        #region Command Buffering

        private void QueueCommand(VISCACommand command, Dictionary<string, object> parameters)
        {
            if (command == null) return;

            if (_enableCommandBuffering)
            {
                var entry = new CommandBufferEntry
                {
                    command = command,
                    parameters = parameters,
                    timestamp = Time.time
                };

                lock (_commandBuffer)
                {
                    if (_commandBuffer.Count >= _maxQueuedCommands)
                    {
                        _commandBuffer.Dequeue();
                    }
                    _commandBuffer.Enqueue(entry);
                }
            }
            else
            {
                ExecuteCommand(command, parameters);
            }

            OnCommandGenerated?.Invoke(command, parameters);
        }

        private void StartCommandProcessing()
        {
            if (_commandProcessingCoroutine == null && _enableCommandBuffering)
            {
                _isProcessingCommands = true;
                _commandProcessingCoroutine = StartCoroutine(ProcessCommandBuffer());
            }
        }

        private void StopCommandProcessing()
        {
            _isProcessingCommands = false;
            if (_commandProcessingCoroutine != null)
            {
                StopCoroutine(_commandProcessingCoroutine);
                _commandProcessingCoroutine = null;
            }
        }

        private IEnumerator ProcessCommandBuffer()
        {
            while (_isProcessingCommands)
            {
                CommandBufferEntry? entry = null;
                
                lock (_commandBuffer)
                {
                    if (_commandBuffer.Count > 0)
                    {
                        entry = _commandBuffer.Dequeue();
                    }
                }

                if (entry.HasValue)
                {
                    ExecuteCommand(entry.Value.command, entry.Value.parameters);
                }

                yield return new WaitForSeconds(_commandInterval);
            }
        }

        private void ExecuteCommand(VISCACommand command, Dictionary<string, object> parameters)
        {
            if (_connectionManager != null && _connectionManager.HasActiveCameraConnected)
            {
                _connectionManager.SendCommandToActiveCamera(command, parameters);
                LogDebug($"Executed command: {command.Action}");
            }
            else
            {
                LogDebug($"Cannot execute command - no active camera: {command.Action}");
            }
        }

        #endregion

        #region Helper Methods

        private Vector2 ApplyDeadZone(Vector2 input)
        {
            if (input.magnitude < _deadZone)
                return Vector2.zero;
            
            // Apply dead zone and rescale
            float magnitude = (input.magnitude - _deadZone) / (1f - _deadZone);
            return input.normalized * magnitude;
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[CameraInputHandler] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CameraInputHandler] {message}");
        }

        #endregion

        #region Public Configuration

        public void SetDeadZone(float deadZone)
        {
            _deadZone = Mathf.Clamp01(deadZone);
        }

        public void SetMaxSpeed(float maxSpeed)
        {
            _maxSpeed = Mathf.Clamp(maxSpeed, 0.1f, 2.0f);
        }

        public void SetSpeedSensitivity(float sensitivity)
        {
            _speedSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5.0f);
        }

        public void SetAxisInversion(bool invertPan, bool invertTilt)
        {
            _invertPanAxis = invertPan;
            _invertTiltAxis = invertTilt;
        }

        public void SetCommandBuffering(bool enabled, float interval = 0.1f)
        {
            _enableCommandBuffering = enabled;
            _commandInterval = interval;
            
            if (enabled && !_isProcessingCommands)
                StartCommandProcessing();
            else if (!enabled && _isProcessingCommands)
                StopCommandProcessing();
        }

        #endregion
    }
}