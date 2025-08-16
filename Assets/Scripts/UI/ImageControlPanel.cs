using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using USAALive.VISCA;
using USAALive.Networking;

namespace USAALive.UI
{
    public class ImageControlPanel : MonoBehaviour
    {
        private VisualElement _root;
        private CameraConnectionManager _connectionManager;

        // Exposure Mode Buttons
        private Button _aeManualButton;
        private Button _aeShutterButton;
        private Button _aeIrisButton;
        private Button _aeBrightButton;

        // White Balance Buttons
        private Button _wbIndoorButton;
        private Button _wbOutdoorButton;
        private Button _wbAutoButton;
        private Button _wbManualButton;
        private Button _wbOnePushButton;

        // Iris Controls
        private Button _irisUpButton;
        private Button _irisDownButton;
        private Slider _irisSlider;
        private Label _irisValueLabel;

        // Shutter Controls
        private Button _shutterUpButton;
        private Button _shutterDownButton;
        private Slider _shutterSlider;
        private Label _shutterValueLabel;

        // Gain Controls
        private Button _gainUpButton;
        private Button _gainDownButton;
        private Slider _gainSlider;
        private Label _gainValueLabel;

        // Color Controls
        private Button _redGainUpButton;
        private Button _redGainDownButton;
        private Slider _redGainSlider;
        private Label _redGainValue;

        private Button _blueGainUpButton;
        private Button _blueGainDownButton;
        private Slider _blueGainSlider;
        private Label _blueGainValue;

        // Brightness Controls
        private Button _brightnessUpButton;
        private Button _brightnessDownButton;
        private Slider _brightnessSlider;
        private Label _brightnessValueLabel;

        // Reset Controls
        private Button _resetImageButton;
        private Button _autoAdjustButton;

        // State tracking
        private string _currentExposureMode = "Bright";
        private string _currentWhiteBalanceMode = "Outdoor";
        private Dictionary<string, float> _imageValues = new Dictionary<string, float>();

        public void Initialize(VisualElement root, CameraConnectionManager connectionManager)
        {
            _root = root;
            _connectionManager = connectionManager;

            CacheUIElements();
            SetupEventHandlers();
            InitializeValues();

            Debug.Log("[ImageControlPanel] Initialized successfully");
        }

        private void OnDestroy()
        {
            CleanupEventHandlers();
        }

        #region UI Element Caching

        private void CacheUIElements()
        {
            // Exposure Mode Buttons
            _aeManualButton = _root.Q<Button>("ae-manual");
            _aeShutterButton = _root.Q<Button>("ae-shutter");
            _aeIrisButton = _root.Q<Button>("ae-iris");
            _aeBrightButton = _root.Q<Button>("ae-bright");

            // White Balance Buttons
            _wbIndoorButton = _root.Q<Button>("wb-indoor");
            _wbOutdoorButton = _root.Q<Button>("wb-outdoor");
            _wbAutoButton = _root.Q<Button>("wb-auto");
            _wbManualButton = _root.Q<Button>("wb-manual");
            _wbOnePushButton = _root.Q<Button>("wb-one-push");

            // Iris Controls
            _irisUpButton = _root.Q<Button>("iris-up");
            _irisDownButton = _root.Q<Button>("iris-down");
            _irisSlider = _root.Q<Slider>("iris-slider");
            _irisValueLabel = _root.Q<Label>("iris-value-label");

            // Shutter Controls
            _shutterUpButton = _root.Q<Button>("shutter-up");
            _shutterDownButton = _root.Q<Button>("shutter-down");
            _shutterSlider = _root.Q<Slider>("shutter-slider");
            _shutterValueLabel = _root.Q<Label>("shutter-value-label");

            // Gain Controls
            _gainUpButton = _root.Q<Button>("gain-up");
            _gainDownButton = _root.Q<Button>("gain-down");
            _gainSlider = _root.Q<Slider>("gain-slider");
            _gainValueLabel = _root.Q<Label>("gain-value-label");

            // Color Controls
            _redGainUpButton = _root.Q<Button>("red-gain-up");
            _redGainDownButton = _root.Q<Button>("red-gain-down");
            _redGainSlider = _root.Q<Slider>("red-gain-slider");
            _redGainValue = _root.Q<Label>("red-gain-value");

            _blueGainUpButton = _root.Q<Button>("blue-gain-up");
            _blueGainDownButton = _root.Q<Button>("blue-gain-down");
            _blueGainSlider = _root.Q<Slider>("blue-gain-slider");
            _blueGainValue = _root.Q<Label>("blue-gain-value");

            // Brightness Controls
            _brightnessUpButton = _root.Q<Button>("brightness-up");
            _brightnessDownButton = _root.Q<Button>("brightness-down");
            _brightnessSlider = _root.Q<Slider>("brightness-slider");
            _brightnessValueLabel = _root.Q<Label>("brightness-value-label");

            // Reset Controls
            _resetImageButton = _root.Q<Button>("reset-image-button");
            _autoAdjustButton = _root.Q<Button>("auto-adjust-button");
        }

        #endregion

        #region Event Handler Setup

        private void SetupEventHandlers()
        {
            // Exposure Mode Events
            if (_aeManualButton != null)
                _aeManualButton.clicked += () => SetExposureMode("Manual");
            if (_aeShutterButton != null)
                _aeShutterButton.clicked += () => SetExposureMode("Shutter");
            if (_aeIrisButton != null)
                _aeIrisButton.clicked += () => SetExposureMode("Iris");
            if (_aeBrightButton != null)
                _aeBrightButton.clicked += () => SetExposureMode("Bright");

            // White Balance Events
            if (_wbIndoorButton != null)
                _wbIndoorButton.clicked += () => SetWhiteBalanceMode("Indoor");
            if (_wbOutdoorButton != null)
                _wbOutdoorButton.clicked += () => SetWhiteBalanceMode("Outdoor");
            if (_wbAutoButton != null)
                _wbAutoButton.clicked += () => SetWhiteBalanceMode("Auto");
            if (_wbManualButton != null)
                _wbManualButton.clicked += () => SetWhiteBalanceMode("Manual");
            if (_wbOnePushButton != null)
                _wbOnePushButton.clicked += OnWhiteBalanceOnePush;

            // Iris Control Events
            SetupAdjustmentControls(_irisUpButton, _irisDownButton, _irisSlider, "Iris");

            // Shutter Control Events
            SetupAdjustmentControls(_shutterUpButton, _shutterDownButton, _shutterSlider, "Shutter");

            // Gain Control Events
            SetupAdjustmentControls(_gainUpButton, _gainDownButton, _gainSlider, "Gain");

            // Color Control Events
            SetupColorControls(_redGainUpButton, _redGainDownButton, _redGainSlider, "RGain");
            SetupColorControls(_blueGainUpButton, _blueGainDownButton, _blueGainSlider, "BGain");

            // Brightness Control Events
            SetupAdjustmentControls(_brightnessUpButton, _brightnessDownButton, _brightnessSlider, "Bright");

            // Reset Control Events
            if (_resetImageButton != null)
                _resetImageButton.clicked += OnResetImageClicked;
            if (_autoAdjustButton != null)
                _autoAdjustButton.clicked += OnAutoAdjustClicked;

            // Connection Manager Events
            if (_connectionManager != null)
            {
                _connectionManager.OnCameraResponseReceived += OnCameraResponseReceived;
                _connectionManager.OnCameraConnectionStateChanged += OnConnectionStateChanged;
            }
        }

        private void SetupAdjustmentControls(Button upButton, Button downButton, Slider slider, string controlName)
        {
            if (upButton != null)
                upButton.clicked += () => AdjustValue(controlName, true);

            if (downButton != null)
                downButton.clicked += () => AdjustValue(controlName, false);

            if (slider != null)
                slider.RegisterValueChangedCallback(evt => OnSliderChanged(controlName, evt.newValue));
        }

        private void SetupColorControls(Button upButton, Button downButton, Slider slider, string controlName)
        {
            if (upButton != null)
                upButton.clicked += () => AdjustColorValue(controlName, true);

            if (downButton != null)
                downButton.clicked += () => AdjustColorValue(controlName, false);

            if (slider != null)
                slider.RegisterValueChangedCallback(evt => OnColorSliderChanged(controlName, evt.newValue));
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
            // Initialize image values
            _imageValues["Iris"] = 128f;
            _imageValues["Shutter"] = 128f;
            _imageValues["Gain"] = 64f;
            _imageValues["RGain"] = 128f;
            _imageValues["BGain"] = 128f;
            _imageValues["Bright"] = 128f;

            // Set initial slider values
            SetSliderValue(_irisSlider, _imageValues["Iris"]);
            SetSliderValue(_shutterSlider, _imageValues["Shutter"]);
            SetSliderValue(_gainSlider, _imageValues["Gain"]);
            SetSliderValue(_redGainSlider, _imageValues["RGain"]);
            SetSliderValue(_blueGainSlider, _imageValues["BGain"]);
            SetSliderValue(_brightnessSlider, _imageValues["Bright"]);

            // Update display values
            UpdateAllDisplayValues();

            // Set initial mode states
            SetExposureMode("Bright", false);
            SetWhiteBalanceMode("Outdoor", false);
        }

        #endregion

        #region Mode Control

        private void SetExposureMode(string mode, bool sendCommand = true)
        {
            _currentExposureMode = mode;

            // Update button states
            var buttons = new[] { _aeManualButton, _aeShutterButton, _aeIrisButton, _aeBrightButton };
            foreach (var button in buttons)
            {
                button?.RemoveFromClassList("active");
            }

            Button activeButton = mode switch
            {
                "Manual" => _aeManualButton,
                "Shutter" => _aeShutterButton,
                "Iris" => _aeIrisButton,
                "Bright" => _aeBrightButton,
                _ => _aeBrightButton
            };

            activeButton?.AddToClassList("active");

            if (sendCommand)
            {
                var command = mode switch
                {
                    "Manual" => VISCACommandLibrary.GetCommand("AE_Manual"),
                    "Shutter" => VISCACommandLibrary.GetCommand("AE_ShutterPriority"),
                    "Iris" => VISCACommandLibrary.GetCommand("AE_IrisPriority"),
                    "Bright" => VISCACommandLibrary.GetCommand("AE_Bright"),
                    _ => null
                };

                SendCommand(command, null);
                Debug.Log($"[ImageControlPanel] Exposure mode set to: {mode}");
            }
        }

        private void SetWhiteBalanceMode(string mode, bool sendCommand = true)
        {
            _currentWhiteBalanceMode = mode;

            // Update button states
            var buttons = new[] { _wbIndoorButton, _wbOutdoorButton, _wbAutoButton, _wbManualButton };
            foreach (var button in buttons)
            {
                button?.RemoveFromClassList("active");
            }

            Button activeButton = mode switch
            {
                "Indoor" => _wbIndoorButton,
                "Outdoor" => _wbOutdoorButton,
                "Auto" => _wbAutoButton,
                "Manual" => _wbManualButton,
                _ => _wbOutdoorButton
            };

            activeButton?.AddToClassList("active");

            if (sendCommand)
            {
                var command = mode switch
                {
                    "Indoor" => VISCACommandLibrary.GetCommand("WB_Indoor"),
                    "Outdoor" => VISCACommandLibrary.GetCommand("WB_Outdoor"),
                    "Auto" => VISCACommandLibrary.GetCommand("WB_Auto"),
                    "Manual" => VISCACommandLibrary.GetCommand("WB_Manual"),
                    _ => null
                };

                SendCommand(command, null);
                Debug.Log($"[ImageControlPanel] White balance mode set to: {mode}");
            }
        }

        #endregion

        #region Event Handlers

        private void AdjustValue(string controlName, bool increase)
        {
            var command = increase switch
            {
                true => controlName switch
                {
                    "Iris" => VISCACommandLibrary.GetCommand("Iris_Up"),
                    "Shutter" => VISCACommandLibrary.GetCommand("Shutter_Up"),
                    "Gain" => VISCACommandLibrary.GetCommand("Gain_Up"),
                    "Bright" => VISCACommandLibrary.GetCommand("Bright_Up"),
                    _ => null
                },
                false => controlName switch
                {
                    "Iris" => VISCACommandLibrary.GetCommand("Iris_Down"),
                    "Shutter" => VISCACommandLibrary.GetCommand("Shutter_Down"),
                    "Gain" => VISCACommandLibrary.GetCommand("Gain_Down"),
                    "Bright" => VISCACommandLibrary.GetCommand("Bright_Down"),
                    _ => null
                }
            };

            SendCommand(command, null);

            // Update local value for UI feedback
            if (_imageValues.ContainsKey(controlName))
            {
                float delta = increase ? 5f : -5f;
                _imageValues[controlName] = Mathf.Clamp(_imageValues[controlName] + delta, 0f, 255f);
                UpdateDisplayValue(controlName);
            }

            Debug.Log($"[ImageControlPanel] {controlName} adjusted {(increase ? "up" : "down")}");
        }

        private void AdjustColorValue(string controlName, bool increase)
        {
            var command = increase switch
            {
                true => controlName switch
                {
                    "RGain" => VISCACommandLibrary.GetCommand("RGain_Up"),
                    "BGain" => VISCACommandLibrary.GetCommand("BGain_Up"),
                    _ => null
                },
                false => controlName switch
                {
                    "RGain" => VISCACommandLibrary.GetCommand("RGain_Down"),
                    "BGain" => VISCACommandLibrary.GetCommand("BGain_Down"),
                    _ => null
                }
            };

            SendCommand(command, null);

            // Update local value for UI feedback
            if (_imageValues.ContainsKey(controlName))
            {
                float delta = increase ? 5f : -5f;
                _imageValues[controlName] = Mathf.Clamp(_imageValues[controlName] + delta, 0f, 255f);
                UpdateColorDisplayValue(controlName);
            }

            Debug.Log($"[ImageControlPanel] {controlName} adjusted {(increase ? "up" : "down")}");
        }

        private void OnSliderChanged(string controlName, float value)
        {
            _imageValues[controlName] = value;

            var command = controlName switch
            {
                "Iris" => VISCACommandLibrary.GetCommand("Iris_Direct"),
                "Shutter" => VISCACommandLibrary.GetCommand("Shutter_Direct"),
                "Gain" => VISCACommandLibrary.GetCommand("Gain_Direct"),
                "Bright" => VISCACommandLibrary.GetCommand("Bright_Direct"),
                _ => null
            };

            if (command != null)
            {
                var parameters = new Dictionary<string, object>
                {
                    ["pq"] = (byte)value
                };
                SendCommand(command, parameters);
            }

            UpdateDisplayValue(controlName);
        }

        private void OnColorSliderChanged(string controlName, float value)
        {
            _imageValues[controlName] = value;

            var command = controlName switch
            {
                "RGain" => VISCACommandLibrary.GetCommand("RGain_Direct"),
                "BGain" => VISCACommandLibrary.GetCommand("BGain_Direct"),
                _ => null
            };

            if (command != null)
            {
                var parameters = new Dictionary<string, object>
                {
                    ["pq"] = (byte)value
                };
                SendCommand(command, parameters);
            }

            UpdateColorDisplayValue(controlName);
        }

        private void OnWhiteBalanceOnePush()
        {
            var command = VISCACommandLibrary.GetCommand("WB_OnePush");
            SendCommand(command, null);
            Debug.Log("[ImageControlPanel] White balance one push executed");
        }

        private void OnResetImageClicked()
        {
            // Reset all image values to defaults
            _imageValues["Iris"] = 128f;
            _imageValues["Shutter"] = 128f;
            _imageValues["Gain"] = 64f;
            _imageValues["RGain"] = 128f;
            _imageValues["BGain"] = 128f;
            _imageValues["Bright"] = 128f;

            // Update sliders
            SetSliderValue(_irisSlider, _imageValues["Iris"]);
            SetSliderValue(_shutterSlider, _imageValues["Shutter"]);
            SetSliderValue(_gainSlider, _imageValues["Gain"]);
            SetSliderValue(_redGainSlider, _imageValues["RGain"]);
            SetSliderValue(_blueGainSlider, _imageValues["BGain"]);
            SetSliderValue(_brightnessSlider, _imageValues["Bright"]);

            // Update displays
            UpdateAllDisplayValues();

            // Reset modes
            SetExposureMode("Bright");
            SetWhiteBalanceMode("Outdoor");

            Debug.Log("[ImageControlPanel] Image settings reset to defaults");
        }

        private void OnAutoAdjustClicked()
        {
            // Trigger auto exposure and white balance
            var aeCommand = VISCACommandLibrary.GetCommand("AE_Bright");
            var wbCommand = VISCACommandLibrary.GetCommand("WB_Auto");
            var afCommand = VISCACommandLibrary.GetCommand("Focus_OnePushAF");

            SendCommand(aeCommand, null);
            SendCommand(wbCommand, null);
            SendCommand(afCommand, null);

            SetExposureMode("Bright", false);
            SetWhiteBalanceMode("Auto", false);

            Debug.Log("[ImageControlPanel] Auto adjust executed");
        }

        private void OnCameraResponseReceived(int cameraIndex, VISCAResponse response)
        {
            if (cameraIndex != _connectionManager?.ActiveCameraIndex) return;

            // Parse image data from camera responses
            if (response.Type == VISCAResponseType.Data && response.Data.ContainsKey("raw"))
            {
                var data = response.Data["raw"] as byte[];
                if (data != null)
                {
                    ParseImageData(data);
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

        private void UpdateDisplayValue(string controlName)
        {
            if (!_imageValues.ContainsKey(controlName)) return;

            float value = _imageValues[controlName];
            string displayText = controlName switch
            {
                "Iris" => FormatIrisValue(value),
                "Shutter" => FormatShutterValue(value),
                "Gain" => FormatGainValue(value),
                "Bright" => FormatBrightnessValue(value),
                _ => value.ToString("F0")
            };

            Label label = controlName switch
            {
                "Iris" => _irisValueLabel,
                "Shutter" => _shutterValueLabel,
                "Gain" => _gainValueLabel,
                "Bright" => _brightnessValueLabel,
                _ => null
            };

            if (label != null)
                label.text = displayText;
        }

        private void UpdateColorDisplayValue(string controlName)
        {
            if (!_imageValues.ContainsKey(controlName)) return;

            float value = _imageValues[controlName];
            Label label = controlName switch
            {
                "RGain" => _redGainValue,
                "BGain" => _blueGainValue,
                _ => null
            };

            if (label != null)
                label.text = value.ToString("F0");
        }

        private void UpdateAllDisplayValues()
        {
            UpdateDisplayValue("Iris");
            UpdateDisplayValue("Shutter");
            UpdateDisplayValue("Gain");
            UpdateDisplayValue("Bright");
            UpdateColorDisplayValue("RGain");
            UpdateColorDisplayValue("BGain");
        }

        private void SetSliderValue(Slider slider, float value)
        {
            if (slider != null)
                slider.value = value;
        }

        private void EnableControls(bool enabled)
        {
            var allButtons = new[] { 
                _aeManualButton, _aeShutterButton, _aeIrisButton, _aeBrightButton,
                _wbIndoorButton, _wbOutdoorButton, _wbAutoButton, _wbManualButton, _wbOnePushButton,
                _irisUpButton, _irisDownButton, _shutterUpButton, _shutterDownButton,
                _gainUpButton, _gainDownButton, _redGainUpButton, _redGainDownButton,
                _blueGainUpButton, _blueGainDownButton, _brightnessUpButton, _brightnessDownButton,
                _resetImageButton, _autoAdjustButton
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

            var allSliders = new[] { 
                _irisSlider, _shutterSlider, _gainSlider, 
                _redGainSlider, _blueGainSlider, _brightnessSlider
            };

            foreach (var slider in allSliders)
            {
                if (slider != null)
                {
                    slider.SetEnabled(enabled);
                }
            }
        }

        #endregion

        #region Helper Methods

        private string FormatIrisValue(float value)
        {
            // Convert iris value to F-stop representation
            // This is a simplified conversion - real cameras have specific mappings
            float fStop = 1.4f + (value / 255f) * 10f;
            return $"F{fStop:F1}";
        }

        private string FormatShutterValue(float value)
        {
            // Convert shutter value to speed representation
            // This is a simplified conversion - real cameras have specific mappings
            int speed = Mathf.RoundToInt(30 + (value / 255f) * 1000);
            return $"1/{speed}";
        }

        private string FormatGainValue(float value)
        {
            // Convert gain value to dB representation
            float db = (value / 255f) * 30f;
            return $"{db:F0}dB";
        }

        private string FormatBrightnessValue(float value)
        {
            // Convert brightness to signed value
            int brightness = Mathf.RoundToInt(value - 128);
            return brightness >= 0 ? $"+{brightness}" : brightness.ToString();
        }

        private void ParseImageData(byte[] data)
        {
            try
            {
                // Parse image setting data from camera responses
                // This would need to be implemented based on specific camera response formats
                // Different cameras may have different response structures
                Debug.Log("[ImageControlPanel] Parsing image data from camera response");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ImageControlPanel] Failed to parse image data: {ex.Message}");
            }
        }

        private void SendCommand(VISCACommand command, Dictionary<string, object> parameters)
        {
            if (command != null && _connectionManager != null)
            {
                _connectionManager.SendCommandToActiveCamera(command, parameters);
            }
        }

        #endregion

        #region Public Interface

        public void SetImageValue(string controlName, float value)
        {
            if (_imageValues.ContainsKey(controlName))
            {
                _imageValues[controlName] = Mathf.Clamp(value, 0f, 255f);
                
                Slider slider = controlName switch
                {
                    "Iris" => _irisSlider,
                    "Shutter" => _shutterSlider,
                    "Gain" => _gainSlider,
                    "RGain" => _redGainSlider,
                    "BGain" => _blueGainSlider,
                    "Bright" => _brightnessSlider,
                    _ => null
                };

                SetSliderValue(slider, value);
                
                if (controlName == "RGain" || controlName == "BGain")
                    UpdateColorDisplayValue(controlName);
                else
                    UpdateDisplayValue(controlName);
            }
        }

        public float GetImageValue(string controlName)
        {
            return _imageValues.ContainsKey(controlName) ? _imageValues[controlName] : 0f;
        }

        public void SetExposureMode(string mode)
        {
            SetExposureMode(mode, true);
        }

        public void SetWhiteBalanceMode(string mode)
        {
            SetWhiteBalanceMode(mode, true);
        }

        public string GetCurrentExposureMode()
        {
            return _currentExposureMode;
        }

        public string GetCurrentWhiteBalanceMode()
        {
            return _currentWhiteBalanceMode;
        }

        public void ResetToDefaults()
        {
            OnResetImageClicked();
        }

        public void AutoAdjust()
        {
            OnAutoAdjustClicked();
        }

        #endregion
    }
}