using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using USAALive.VISCA;
using USAALive.Networking;

namespace USAALive.UI
{
    public class PTZControlPanel : MonoBehaviour
    {
        private VisualElement _root;
        private CameraConnectionManager _connectionManager;

        // PTZ Direction Buttons
        private Button _ptzUpLeft;
        private Button _ptzUp;
        private Button _ptzUpRight;
        private Button _ptzLeft;
        private Button _ptzStop;
        private Button _ptzRight;
        private Button _ptzDownLeft;
        private Button _ptzDown;
        private Button _ptzDownRight;

        // Speed Controls
        private SliderInt _panSpeedSlider;
        private SliderInt _tiltSpeedSlider;
        private Label _panSpeedValue;
        private Label _tiltSpeedValue;

        // Position Controls
        private Label _panPositionValue;
        private Label _tiltPositionValue;
        private Button _homePositionButton;
        private Button _resetPositionButton;

        // Advanced Controls
        private Toggle _continuousModeToggle;
        private Toggle _positionFeedbackToggle;
        private IntegerField _gotoPanField;
        private IntegerField _gotoTiltField;
        private Button _gotoButton;

        // State tracking
        private bool _isPTZMoving = false;
        private Vector2 _currentPanTiltSpeeds = new Vector2(12, 12);

        public void Initialize(VisualElement root, CameraConnectionManager connectionManager)
        {
            _root = root;
            _connectionManager = connectionManager;

            CacheUIElements();
            SetupEventHandlers();
            InitializeValues();

            Debug.Log("[PTZControlPanel] Initialized successfully");
        }

        private void OnDestroy()
        {
            CleanupEventHandlers();
        }

        #region UI Element Caching

        private void CacheUIElements()
        {
            // PTZ Direction Buttons
            _ptzUpLeft = _root.Q<Button>("ptz-up-left");
            _ptzUp = _root.Q<Button>("ptz-up");
            _ptzUpRight = _root.Q<Button>("ptz-up-right");
            _ptzLeft = _root.Q<Button>("ptz-left");
            _ptzStop = _root.Q<Button>("ptz-stop");
            _ptzRight = _root.Q<Button>("ptz-right");
            _ptzDownLeft = _root.Q<Button>("ptz-down-left");
            _ptzDown = _root.Q<Button>("ptz-down");
            _ptzDownRight = _root.Q<Button>("ptz-down-right");

            // Speed Controls
            _panSpeedSlider = _root.Q<SliderInt>("pan-speed-slider");
            _tiltSpeedSlider = _root.Q<SliderInt>("tilt-speed-slider");
            _panSpeedValue = _root.Q<Label>("pan-speed-value");
            _tiltSpeedValue = _root.Q<Label>("tilt-speed-value");

            // Position Controls
            _panPositionValue = _root.Q<Label>("pan-position-value");
            _tiltPositionValue = _root.Q<Label>("tilt-position-value");
            _homePositionButton = _root.Q<Button>("home-position-button");
            _resetPositionButton = _root.Q<Button>("reset-position-button");

            // Advanced Controls
            _continuousModeToggle = _root.Q<Toggle>("continuous-mode-toggle");
            _positionFeedbackToggle = _root.Q<Toggle>("position-feedback-toggle");
            _gotoPanField = _root.Q<IntegerField>("goto-pan-field");
            _gotoTiltField = _root.Q<IntegerField>("goto-tilt-field");
            _gotoButton = _root.Q<Button>("goto-button");
        }

        #endregion

        #region Event Handler Setup

        private void SetupEventHandlers()
        {
            // PTZ Direction Button Events
            SetupPTZDirectionEvents();

            // Speed Control Events
            if (_panSpeedSlider != null)
            {
                _panSpeedSlider.RegisterValueChangedCallback(OnPanSpeedChanged);
            }

            if (_tiltSpeedSlider != null)
            {
                _tiltSpeedSlider.RegisterValueChangedCallback(OnTiltSpeedChanged);
            }

            // Position Control Events
            if (_homePositionButton != null)
                _homePositionButton.clicked += OnHomePositionClicked;

            if (_resetPositionButton != null)
                _resetPositionButton.clicked += OnResetPositionClicked;

            // Advanced Control Events
            if (_continuousModeToggle != null)
                _continuousModeToggle.RegisterValueChangedCallback(OnContinuousModeChanged);

            if (_positionFeedbackToggle != null)
                _positionFeedbackToggle.RegisterValueChangedCallback(OnPositionFeedbackChanged);

            if (_gotoButton != null)
                _gotoButton.clicked += OnGotoButtonClicked;

            // Connection Manager Events
            if (_connectionManager != null)
            {
                _connectionManager.OnCameraResponseReceived += OnCameraResponseReceived;
                _connectionManager.OnCameraConnectionStateChanged += OnConnectionStateChanged;
            }
        }

        private void SetupPTZDirectionEvents()
        {
            // Use mousedown/mouseup events for press and hold functionality
            SetupPTZButton(_ptzUpLeft, "PTZ_UpLeft");
            SetupPTZButton(_ptzUp, "PTZ_Up");
            SetupPTZButton(_ptzUpRight, "PTZ_UpRight");
            SetupPTZButton(_ptzLeft, "PTZ_Left");
            SetupPTZButton(_ptzRight, "PTZ_Right");
            SetupPTZButton(_ptzDownLeft, "PTZ_DownLeft");
            SetupPTZButton(_ptzDown, "PTZ_Down");
            SetupPTZButton(_ptzDownRight, "PTZ_DownRight");

            if (_ptzStop != null)
                _ptzStop.clicked += OnPTZStopClicked;
        }

        private void SetupPTZButton(Button button, string commandKey)
        {
            if (button == null) return;

            button.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) // Left mouse button
                {
                    StartPTZMovement(commandKey);
                }
            });

            button.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0) // Left mouse button
                {
                    StopPTZMovement();
                }
            });

            // Also handle mouse leave to stop movement
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                StopPTZMovement();
            });
        }

        private void CleanupEventHandlers()
        {
            if (_connectionManager != null)
            {
                _connectionManager.OnCameraResponseReceived -= OnCameraResponseReceived;
                _connectionManager.OnCameraConnectionStateChanged -= OnConnectionStateChanged;
            }
        }

        #endregion

        #region Initialization

        private void InitializeValues()
        {
            // Set default speed values
            if (_panSpeedSlider != null)
            {
                _panSpeedSlider.value = 12;
                _currentPanTiltSpeeds.x = 12;
            }

            if (_tiltSpeedSlider != null)
            {
                _tiltSpeedSlider.value = 12;
                _currentPanTiltSpeeds.y = 12;
            }

            UpdateSpeedDisplays();

            // Set default toggle states
            if (_continuousModeToggle != null)
                _continuousModeToggle.value = false;

            if (_positionFeedbackToggle != null)
                _positionFeedbackToggle.value = true;

            // Set default goto position values
            if (_gotoPanField != null)
                _gotoPanField.value = 0;

            if (_gotoTiltField != null)
                _gotoTiltField.value = 0;

            UpdatePositionDisplay(0, 0);
        }

        #endregion

        #region PTZ Movement Control

        private void StartPTZMovement(string commandKey)
        {
            if (_isPTZMoving) return;

            var command = VISCACommandLibrary.GetCommand(commandKey);
            if (command != null && _connectionManager != null)
            {
                var parameters = new Dictionary<string, object>
                {
                    ["panspeed"] = (byte)_currentPanTiltSpeeds.x,
                    ["tiltspeed"] = (byte)_currentPanTiltSpeeds.y
                };

                _connectionManager.SendCommandToActiveCamera(command, parameters);
                _isPTZMoving = true;

                // Visual feedback
                HighlightActiveMovement(true);

                Debug.Log($"[PTZControlPanel] Started PTZ movement: {commandKey}");
            }
        }

        private void StopPTZMovement()
        {
            if (!_isPTZMoving) return;

            var command = VISCACommandLibrary.GetCommand("PTZ_Stop");
            if (command != null && _connectionManager != null)
            {
                var parameters = new Dictionary<string, object>
                {
                    ["panspeed"] = (byte)_currentPanTiltSpeeds.x,
                    ["tiltspeed"] = (byte)_currentPanTiltSpeeds.y
                };

                _connectionManager.SendCommandToActiveCamera(command, parameters);
                _isPTZMoving = false;

                // Visual feedback
                HighlightActiveMovement(false);

                Debug.Log("[PTZControlPanel] Stopped PTZ movement");
            }
        }

        private void HighlightActiveMovement(bool active)
        {
            // Add visual feedback to indicate active movement
            var buttons = new[] { _ptzUpLeft, _ptzUp, _ptzUpRight, _ptzLeft, _ptzRight, _ptzDownLeft, _ptzDown, _ptzDownRight };
            
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.RemoveFromClassList("active");
                    if (active)
                    {
                        // You could add logic to highlight only the active direction button
                        // For now, we'll just remove the highlight on stop
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnPanSpeedChanged(ChangeEvent<int> evt)
        {
            _currentPanTiltSpeeds.x = evt.newValue;
            UpdateSpeedDisplays();
            Debug.Log($"[PTZControlPanel] Pan speed changed to: {evt.newValue}");
        }

        private void OnTiltSpeedChanged(ChangeEvent<int> evt)
        {
            _currentPanTiltSpeeds.y = evt.newValue;
            UpdateSpeedDisplays();
            Debug.Log($"[PTZControlPanel] Tilt speed changed to: {evt.newValue}");
        }

        private void OnPTZStopClicked()
        {
            StopPTZMovement();
        }

        private void OnHomePositionClicked()
        {
            var command = VISCACommandLibrary.GetCommand("PTZ_Home");
            if (command != null && _connectionManager != null)
            {
                _connectionManager.SendCommandToActiveCamera(command, null);
                Debug.Log("[PTZControlPanel] PTZ Home command sent");
            }
        }

        private void OnResetPositionClicked()
        {
            var command = VISCACommandLibrary.GetCommand("PTZ_Reset");
            if (command != null && _connectionManager != null)
            {
                _connectionManager.SendCommandToActiveCamera(command, null);
                Debug.Log("[PTZControlPanel] PTZ Reset command sent");
            }
        }

        private void OnContinuousModeChanged(ChangeEvent<bool> evt)
        {
            Debug.Log($"[PTZControlPanel] Continuous mode: {evt.newValue}");
            // TODO: Implement continuous movement mode logic
        }

        private void OnPositionFeedbackChanged(ChangeEvent<bool> evt)
        {
            Debug.Log($"[PTZControlPanel] Position feedback: {evt.newValue}");
            // TODO: Enable/disable position inquiry based on this setting
        }

        private void OnGotoButtonClicked()
        {
            if (_gotoPanField != null && _gotoTiltField != null)
            {
                int panPosition = _gotoPanField.value;
                int tiltPosition = _gotoTiltField.value;

                var command = VISCACommandLibrary.GetCommand("PTZ_AbsolutePosition");
                if (command != null && _connectionManager != null)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["panspeed"] = (byte)_currentPanTiltSpeeds.x,
                        ["tiltspeed"] = (byte)_currentPanTiltSpeeds.y,
                        ["panposition"] = (ushort)panPosition,
                        ["tiltposition"] = (ushort)tiltPosition
                    };

                    _connectionManager.SendCommandToActiveCamera(command, parameters);
                    Debug.Log($"[PTZControlPanel] Goto position: Pan={panPosition}, Tilt={tiltPosition}");
                }
            }
        }

        private void OnCameraResponseReceived(int cameraIndex, VISCAResponse response)
        {
            if (cameraIndex != _connectionManager?.ActiveCameraIndex) return;

            // Parse position data from camera responses
            if (response.Type == VISCAResponseType.Data && response.Data.ContainsKey("raw"))
            {
                var data = response.Data["raw"] as byte[];
                if (data != null && data.Length >= 8)
                {
                    // Parse PTZ position data (example format)
                    // This would need to be adjusted based on actual camera response format
                    ParsePositionData(data);
                }
            }
        }

        private void OnConnectionStateChanged(int cameraIndex, ConnectionState state)
        {
            if (cameraIndex != _connectionManager?.ActiveCameraIndex) return;

            bool isConnected = state == ConnectionState.Connected;
            EnableControls(isConnected);
        }

        #endregion

        #region UI Updates

        private void UpdateSpeedDisplays()
        {
            if (_panSpeedValue != null)
                _panSpeedValue.text = _currentPanTiltSpeeds.x.ToString();

            if (_tiltSpeedValue != null)
                _tiltSpeedValue.text = _currentPanTiltSpeeds.y.ToString();
        }

        private void UpdatePositionDisplay(float panDegrees, float tiltDegrees)
        {
            if (_panPositionValue != null)
                _panPositionValue.text = $"{panDegrees:F1}°";

            if (_tiltPositionValue != null)
                _tiltPositionValue.text = $"{tiltDegrees:F1}°";
        }

        private void EnableControls(bool enabled)
        {
            var allButtons = new[] { 
                _ptzUpLeft, _ptzUp, _ptzUpRight, _ptzLeft, _ptzStop, _ptzRight, 
                _ptzDownLeft, _ptzDown, _ptzDownRight, _homePositionButton, 
                _resetPositionButton, _gotoButton 
            };

            foreach (var button in allButtons)
            {
                if (button != null)
                {
                    button.SetEnabled(enabled);
                    if (!enabled)
                    {
                        button.AddToClassList("disabled");
                    }
                    else
                    {
                        button.RemoveFromClassList("disabled");
                    }
                }
            }

            var allSliders = new[] { _panSpeedSlider, _tiltSpeedSlider };
            foreach (var slider in allSliders)
            {
                if (slider != null)
                {
                    slider.SetEnabled(enabled);
                }
            }

            var allFields = new[] { _gotoPanField, _gotoTiltField };
            foreach (var field in allFields)
            {
                if (field != null)
                {
                    field.SetEnabled(enabled);
                }
            }

            var allToggles = new[] { _continuousModeToggle, _positionFeedbackToggle };
            foreach (var toggle in allToggles)
            {
                if (toggle != null)
                {
                    toggle.SetEnabled(enabled);
                }
            }
        }

        #endregion

        #region Helper Methods

        private void ParsePositionData(byte[] data)
        {
            try
            {
                // Example parsing - adjust based on actual camera response format
                // VISCA position inquiry responses typically contain position data in specific byte positions
                
                // This is a placeholder implementation
                // Real implementation would parse the actual VISCA response format
                float panDegrees = 0; // Parse from data
                float tiltDegrees = 0; // Parse from data

                UpdatePositionDisplay(panDegrees, tiltDegrees);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PTZControlPanel] Failed to parse position data: {ex.Message}");
            }
        }

        #endregion

        #region Public Interface

        public void SetPanTiltSpeeds(int panSpeed, int tiltSpeed)
        {
            panSpeed = Mathf.Clamp(panSpeed, 1, 24);
            tiltSpeed = Mathf.Clamp(tiltSpeed, 1, 23);

            if (_panSpeedSlider != null)
                _panSpeedSlider.value = panSpeed;

            if (_tiltSpeedSlider != null)
                _tiltSpeedSlider.value = tiltSpeed;

            _currentPanTiltSpeeds = new Vector2(panSpeed, tiltSpeed);
            UpdateSpeedDisplays();
        }

        public Vector2 GetPanTiltSpeeds()
        {
            return _currentPanTiltSpeeds;
        }

        public void StopAllMovement()
        {
            StopPTZMovement();
        }

        public void GoHome()
        {
            OnHomePositionClicked();
        }

        public void GoToPosition(int panDegrees, int tiltDegrees)
        {
            if (_gotoPanField != null)
                _gotoPanField.value = panDegrees;

            if (_gotoTiltField != null)
                _gotoTiltField.value = tiltDegrees;

            OnGotoButtonClicked();
        }

        #endregion
    }
}