using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using USAALive.VISCA;
using USAALive.Networking;
using USAALive.Input;

namespace USAALive.UI
{
    public class MainUIController : MonoBehaviour
    {
        [Header("UI Documents")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private VisualTreeAsset _mainControllerAsset;

        [Header("Component References")]
        [SerializeField] private CameraConnectionManager _connectionManager;
        [SerializeField] private CameraInputHandler _inputHandler;

        [Header("Update Settings")]
        [SerializeField] private float _statusUpdateInterval = 0.1f;
        [SerializeField] private float _positionUpdateInterval = 0.2f;

        // UI Element References
        private VisualElement _root;
        private Label _statusIndicator;
        private Label _statusText;
        private Label _cameraIPLabel;
        private Button _connectButton;
        private Label _connectionStatusLabel;
        private Label _ptzPositionLabel;
        private Label _zoomLevelLabel;
        private Label _focusModeLabel;
        private Label _commandsCounter;
        private Label _fpsCounter;
        private Label _cameraOverlayLabel;

        // Camera Button References
        private List<Button> _cameraButtons = new List<Button>();
        private List<Button> _presetButtons = new List<Button>();
        private Button _setPresetButton;
        private Button _clearPresetButton;

        // Quick Control References
        private Button _homeButton;
        private Button _stopButton;
        private Button _autofocusButton;
        private Button _powerButton;

        // Video Control References
        private Button _videoPauseButton;
        private Button _videoSnapshotButton;
        private Button _videoFullscreenButton;

        // Focus and Zoom Control References
        private DropdownField _focusModeDropdown;
        private Button _focusNearButton;
        private Button _focusFarButton;
        private Slider _focusSlider;
        private Button _zoomWideButton;
        private Button _zoomTeleButton;
        private Slider _zoomSlider;
        private SliderInt _zoomSpeedSlider;

        // State tracking
        private int _currentActiveCamera = -1;
        private bool _isSettingPreset = false;
        private int _commandsSent = 0;
        private float _lastUpdateTime = 0f;
        private float _lastPositionUpdateTime = 0f;

        // PTZ and Image control panels
        private PTZControlPanel _ptzControlPanel;
        private ImageControlPanel _imageControlPanel;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            FindComponentReferences();
        }

        private void Start()
        {
            InitializeUI();
            SetupEventHandlers();
            UpdateConnectionStatus();
        }

        private void Update()
        {
            UpdateStatusDisplay();
            UpdatePositionDisplay();
            UpdateFPSCounter();
        }

        private void OnDestroy()
        {
            CleanupEventHandlers();
        }

        #endregion

        #region Initialization

        private void FindComponentReferences()
        {
            if (_connectionManager == null)
                _connectionManager = FindObjectOfType<CameraConnectionManager>();

            if (_inputHandler == null)
                _inputHandler = FindObjectOfType<CameraInputHandler>();

            if (_connectionManager == null)
                Debug.LogWarning("[MainUIController] CameraConnectionManager not found!");

            if (_inputHandler == null)
                Debug.LogWarning("[MainUIController] CameraInputHandler not found!");
        }

        private void InitializeUI()
        {
            _root = _uiDocument.rootVisualElement;

            if (_root == null)
            {
                Debug.LogError("[MainUIController] Root visual element not found!");
                return;
            }

            // Get UI element references
            CacheUIElements();
            InitializeCameraButtons();
            InitializePresetButtons();
            InitializeQuickControls();
            InitializeVideoControls();
            InitializeFocusAndZoomControls();
            InitializeControlPanels();
            SetupDropdowns();

            Debug.Log("[MainUIController] UI initialized successfully");
        }

        private void CacheUIElements()
        {
            // Header elements
            _statusIndicator = _root.Q<Label>("status-indicator");
            _statusText = _root.Q<Label>("status-text");

            // Camera info elements
            _cameraIPLabel = _root.Q<Label>("camera-ip-label");
            _connectButton = _root.Q<Button>("connect-button");

            // Status elements
            _connectionStatusLabel = _root.Q<Label>("connection-status-label");
            _ptzPositionLabel = _root.Q<Label>("ptz-position-label");
            _zoomLevelLabel = _root.Q<Label>("zoom-level-label");
            _focusModeLabel = _root.Q<Label>("focus-mode-label");

            // Footer elements
            _commandsCounter = _root.Q<Label>("commands-counter");
            _fpsCounter = _root.Q<Label>("fps-counter");

            // Video overlay
            _cameraOverlayLabel = _root.Q<Label>("camera-overlay-label");
        }

        private void InitializeCameraButtons()
        {
            _cameraButtons.Clear();
            for (int i = 0; i < 4; i++)
            {
                var button = _root.Q<Button>($"camera-btn-{i}");
                if (button != null)
                {
                    _cameraButtons.Add(button);
                    int cameraIndex = i; // Capture for closure
                    button.clicked += () => OnCameraButtonClicked(cameraIndex);
                }
            }
        }

        private void InitializePresetButtons()
        {
            _presetButtons.Clear();
            for (int i = 1; i <= 6; i++)
            {
                var button = _root.Q<Button>($"preset-{i}");
                if (button != null)
                {
                    _presetButtons.Add(button);
                    int presetNumber = i; // Capture for closure
                    button.clicked += () => OnPresetButtonClicked(presetNumber);
                }
            }

            _setPresetButton = _root.Q<Button>("set-preset-button");
            _clearPresetButton = _root.Q<Button>("clear-preset-button");

            if (_setPresetButton != null)
                _setPresetButton.clicked += OnSetPresetClicked;

            if (_clearPresetButton != null)
                _clearPresetButton.clicked += OnClearPresetClicked;
        }

        private void InitializeQuickControls()
        {
            _homeButton = _root.Q<Button>("home-button");
            _stopButton = _root.Q<Button>("stop-button");
            _autofocusButton = _root.Q<Button>("autofocus-button");
            _powerButton = _root.Q<Button>("power-button");

            if (_homeButton != null)
                _homeButton.clicked += OnHomeButtonClicked;

            if (_stopButton != null)
                _stopButton.clicked += OnStopButtonClicked;

            if (_autofocusButton != null)
                _autofocusButton.clicked += OnAutofocusButtonClicked;

            if (_powerButton != null)
                _powerButton.clicked += OnPowerButtonClicked;
        }

        private void InitializeVideoControls()
        {
            _videoPauseButton = _root.Q<Button>("video-pause-button");
            _videoSnapshotButton = _root.Q<Button>("video-snapshot-button");
            _videoFullscreenButton = _root.Q<Button>("video-fullscreen-button");

            if (_videoPauseButton != null)
                _videoPauseButton.clicked += OnVideoPauseClicked;

            if (_videoSnapshotButton != null)
                _videoSnapshotButton.clicked += OnVideoSnapshotClicked;

            if (_videoFullscreenButton != null)
                _videoFullscreenButton.clicked += OnVideoFullscreenClicked;
        }

        private void InitializeFocusAndZoomControls()
        {
            // Focus controls
            _focusModeDropdown = _root.Q<DropdownField>("focus-mode-dropdown");
            _focusNearButton = _root.Q<Button>("focus-near-button");
            _focusFarButton = _root.Q<Button>("focus-far-button");
            _focusSlider = _root.Q<Slider>("focus-slider");

            // Zoom controls
            _zoomWideButton = _root.Q<Button>("zoom-wide-button");
            _zoomTeleButton = _root.Q<Button>("zoom-tele-button");
            _zoomSlider = _root.Q<Slider>("zoom-slider");
            _zoomSpeedSlider = _root.Q<SliderInt>("zoom-speed-slider");

            // Focus button events
            if (_focusNearButton != null)
                _focusNearButton.clicked += OnFocusNearClicked;

            if (_focusFarButton != null)
                _focusFarButton.clicked += OnFocusFarClicked;

            // Zoom button events
            if (_zoomWideButton != null)
                _zoomWideButton.clicked += OnZoomWideClicked;

            if (_zoomTeleButton != null)
                _zoomTeleButton.clicked += OnZoomTeleClicked;

            // Slider events
            if (_focusSlider != null)
                _focusSlider.RegisterValueChangedCallback(OnFocusSliderChanged);

            if (_zoomSlider != null)
                _zoomSlider.RegisterValueChangedCallback(OnZoomSliderChanged);

            if (_zoomSpeedSlider != null)
                _zoomSpeedSlider.RegisterValueChangedCallback(OnZoomSpeedChanged);
        }

        private void InitializeControlPanels()
        {
            // Initialize PTZ control panel
            var ptzInstance = _root.Q<VisualElement>("ptz-control-instance");
            if (ptzInstance != null)
            {
                var ptzPanelGO = new GameObject("PTZControlPanel");
                ptzPanelGO.transform.SetParent(transform);
                _ptzControlPanel = ptzPanelGO.AddComponent<PTZControlPanel>();
                _ptzControlPanel.Initialize(ptzInstance, _connectionManager);
            }

            // Initialize Image control panel
            var imageInstance = _root.Q<VisualElement>("image-control-instance");
            if (imageInstance != null)
            {
                var imagePanelGO = new GameObject("ImageControlPanel");
                imagePanelGO.transform.SetParent(transform);
                _imageControlPanel = imagePanelGO.AddComponent<ImageControlPanel>();
                _imageControlPanel.Initialize(imageInstance, _connectionManager);
            }
        }

        private void SetupDropdowns()
        {
            if (_focusModeDropdown != null)
            {
                _focusModeDropdown.choices = new List<string> { "Auto", "Manual" };
                _focusModeDropdown.value = "Auto";
                _focusModeDropdown.RegisterValueChangedCallback(OnFocusModeChanged);
            }
        }

        #endregion

        #region Event Handlers Setup

        private void SetupEventHandlers()
        {
            if (_connectionManager != null)
            {
                _connectionManager.OnCameraConnectionStateChanged += OnCameraConnectionStateChanged;
                _connectionManager.OnActiveCameraChanged += OnActiveCameraChanged;
                _connectionManager.OnCameraResponseReceived += OnCameraResponseReceived;
                _connectionManager.OnCameraError += OnCameraError;
            }

            if (_inputHandler != null)
            {
                _inputHandler.OnCommandGenerated += OnCommandGenerated;
            }

            if (_connectButton != null)
                _connectButton.clicked += OnConnectButtonClicked;
        }

        private void CleanupEventHandlers()
        {
            if (_connectionManager != null)
            {
                _connectionManager.OnCameraConnectionStateChanged -= OnCameraConnectionStateChanged;
                _connectionManager.OnActiveCameraChanged -= OnActiveCameraChanged;
                _connectionManager.OnCameraResponseReceived -= OnCameraResponseReceived;
                _connectionManager.OnCameraError -= OnCameraError;
            }

            if (_inputHandler != null)
            {
                _inputHandler.OnCommandGenerated -= OnCommandGenerated;
            }
        }

        #endregion

        #region UI Event Handlers

        private void OnCameraButtonClicked(int cameraIndex)
        {
            if (_connectionManager != null && _connectionManager.SetActiveCamera(cameraIndex))
            {
                UpdateCameraButtonStates();
                UpdateCameraInfo(cameraIndex);
                Debug.Log($"[MainUIController] Switched to camera {cameraIndex}");
            }
        }

        private void OnConnectButtonClicked()
        {
            if (_connectionManager != null)
            {
                int activeCamera = _connectionManager.ActiveCameraIndex;
                if (activeCamera >= 0)
                {
                    var state = _connectionManager.GetCameraConnectionState(activeCamera);
                    if (state == ConnectionState.Connected)
                    {
                        _ = _connectionManager.DisconnectCameraAsync(activeCamera);
                    }
                    else
                    {
                        _ = _connectionManager.ConnectCameraAsync(activeCamera);
                    }
                }
            }
        }

        private void OnPresetButtonClicked(int presetNumber)
        {
            if (_isSettingPreset)
            {
                // Set preset
                var command = VISCACommandLibrary.GetCommand("Memory_Set");
                var parameters = new Dictionary<string, object> { ["p"] = (byte)presetNumber };
                SendCommand(command, parameters);
                _isSettingPreset = false;
                UpdatePresetButtonStates();
                Debug.Log($"[MainUIController] Set preset {presetNumber}");
            }
            else
            {
                // Recall preset
                var command = VISCACommandLibrary.GetCommand("Memory_Recall");
                var parameters = new Dictionary<string, object> { ["p"] = (byte)presetNumber };
                SendCommand(command, parameters);
                Debug.Log($"[MainUIController] Recalled preset {presetNumber}");
            }
        }

        private void OnSetPresetClicked()
        {
            _isSettingPreset = !_isSettingPreset;
            UpdatePresetButtonStates();
            Debug.Log($"[MainUIController] Preset setting mode: {_isSettingPreset}");
        }

        private void OnClearPresetClicked()
        {
            _isSettingPreset = false;
            UpdatePresetButtonStates();
            Debug.Log("[MainUIController] Cleared preset setting mode");
        }

        private void OnHomeButtonClicked()
        {
            var command = VISCACommandLibrary.GetCommand("PTZ_Home");
            SendCommand(command, null);
        }

        private void OnStopButtonClicked()
        {
            var command = VISCACommandLibrary.GetCommand("PTZ_Stop");
            SendCommand(command, null);
        }

        private void OnAutofocusButtonClicked()
        {
            var command = VISCACommandLibrary.GetCommand("Focus_OnePushAF");
            SendCommand(command, null);
        }

        private void OnPowerButtonClicked()
        {
            var command = VISCACommandLibrary.GetCommand("Power_On");
            SendCommand(command, null);
        }

        private void OnFocusNearClicked()
        {
            var command = VISCACommandLibrary.GetCommand("Focus_NearStandard");
            SendCommand(command, null);
        }

        private void OnFocusFarClicked()
        {
            var command = VISCACommandLibrary.GetCommand("Focus_FarStandard");
            SendCommand(command, null);
        }

        private void OnZoomWideClicked()
        {
            var command = VISCACommandLibrary.GetCommand("Zoom_WideStandard");
            SendCommand(command, null);
        }

        private void OnZoomTeleClicked()
        {
            var command = VISCACommandLibrary.GetCommand("Zoom_TeleStandard");
            SendCommand(command, null);
        }

        private void OnFocusSliderChanged(ChangeEvent<float> evt)
        {
            // Convert slider value to focus position
            var command = VISCACommandLibrary.GetCommand("Focus_Direct");
            var focusPosition = (ushort)(evt.newValue * 0xFFFF / 100f);
            var parameters = new Dictionary<string, object> { ["pqrs"] = focusPosition };
            SendCommand(command, parameters);
        }

        private void OnZoomSliderChanged(ChangeEvent<float> evt)
        {
            // Convert slider value to zoom position
            var command = VISCACommandLibrary.GetCommand("Zoom_Direct");
            var zoomPosition = (ushort)(evt.newValue * 0x4000 / 100f);
            var parameters = new Dictionary<string, object> { ["pqrs"] = zoomPosition };
            SendCommand(command, parameters);
        }

        private void OnZoomSpeedChanged(ChangeEvent<int> evt)
        {
            // Zoom speed is handled by the input system
            Debug.Log($"[MainUIController] Zoom speed changed to: {evt.newValue}");
        }

        private void OnFocusModeChanged(ChangeEvent<string> evt)
        {
            var command = evt.newValue == "Auto" 
                ? VISCACommandLibrary.GetCommand("Focus_Auto")
                : VISCACommandLibrary.GetCommand("Focus_Manual");
            SendCommand(command, null);
        }

        private void OnVideoPauseClicked()
        {
            Debug.Log("[MainUIController] Video pause clicked - NDI implementation needed");
        }

        private void OnVideoSnapshotClicked()
        {
            Debug.Log("[MainUIController] Video snapshot clicked - Implementation needed");
        }

        private void OnVideoFullscreenClicked()
        {
            Debug.Log("[MainUIController] Video fullscreen clicked - Implementation needed");
        }

        #endregion

        #region Connection Event Handlers

        private void OnCameraConnectionStateChanged(int cameraIndex, ConnectionState state)
        {
            if (cameraIndex == _connectionManager?.ActiveCameraIndex)
            {
                UpdateConnectionStatus();
                UpdateCameraButtonStates();
                UpdateConnectButton(state);
            }
        }

        private void OnActiveCameraChanged(int cameraIndex)
        {
            _currentActiveCamera = cameraIndex;
            UpdateCameraButtonStates();
            UpdateCameraInfo(cameraIndex);
            UpdateConnectionStatus();
        }

        private void OnCameraResponseReceived(int cameraIndex, VISCAResponse response)
        {
            if (cameraIndex == _connectionManager?.ActiveCameraIndex)
            {
                // Handle response data for UI updates
                Debug.Log($"[MainUIController] Response from camera {cameraIndex}: {response}");
            }
        }

        private void OnCameraError(int cameraIndex, string error)
        {
            if (cameraIndex == _connectionManager?.ActiveCameraIndex)
            {
                Debug.LogError($"[MainUIController] Camera {cameraIndex} error: {error}");
            }
        }

        private void OnCommandGenerated(VISCACommand command, Dictionary<string, object> parameters)
        {
            _commandsSent++;
        }

        #endregion

        #region UI Updates

        private void UpdateConnectionStatus()
        {
            if (_connectionManager == null) return;

            int activeCamera = _connectionManager.ActiveCameraIndex;
            if (activeCamera >= 0)
            {
                var state = _connectionManager.GetCameraConnectionState(activeCamera);
                UpdateStatusIndicator(state);
                UpdateConnectionStatusLabel(state);
            }
            else
            {
                UpdateStatusIndicator(ConnectionState.Disconnected);
                UpdateConnectionStatusLabel(ConnectionState.Disconnected);
            }
        }

        private void UpdateStatusIndicator(ConnectionState state)
        {
            if (_statusIndicator == null || _statusText == null) return;

            _statusIndicator.RemoveFromClassList("connected");
            _statusIndicator.RemoveFromClassList("connecting");
            _statusIndicator.RemoveFromClassList("disconnected");

            switch (state)
            {
                case ConnectionState.Connected:
                    _statusIndicator.AddToClassList("connected");
                    _statusText.text = "Connected";
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Reconnecting:
                    _statusIndicator.AddToClassList("connecting");
                    _statusText.text = "Connecting...";
                    break;
                default:
                    _statusIndicator.AddToClassList("disconnected");
                    _statusText.text = "Disconnected";
                    break;
            }
        }

        private void UpdateConnectionStatusLabel(ConnectionState state)
        {
            if (_connectionStatusLabel == null) return;

            _connectionStatusLabel.RemoveFromClassList("connected");
            _connectionStatusLabel.RemoveFromClassList("disconnected");

            switch (state)
            {
                case ConnectionState.Connected:
                    _connectionStatusLabel.AddToClassList("connected");
                    _connectionStatusLabel.text = "Connected";
                    break;
                default:
                    _connectionStatusLabel.AddToClassList("disconnected");
                    _connectionStatusLabel.text = "Disconnected";
                    break;
            }
        }

        private void UpdateCameraButtonStates()
        {
            int activeCamera = _connectionManager?.ActiveCameraIndex ?? -1;
            
            for (int i = 0; i < _cameraButtons.Count; i++)
            {
                if (_cameraButtons[i] != null)
                {
                    _cameraButtons[i].RemoveFromClassList("active");
                    if (i == activeCamera)
                    {
                        _cameraButtons[i].AddToClassList("active");
                    }
                }
            }
        }

        private void UpdateCameraInfo(int cameraIndex)
        {
            if (_connectionManager == null || _cameraIPLabel == null || _cameraOverlayLabel == null) return;

            var config = _connectionManager.GetCameraConfig(cameraIndex);
            if (config != null)
            {
                _cameraIPLabel.text = $"{config.ipAddress}:{config.port}";
                _cameraOverlayLabel.text = config.name;
            }
        }

        private void UpdateConnectButton(ConnectionState state)
        {
            if (_connectButton == null) return;

            switch (state)
            {
                case ConnectionState.Connected:
                    _connectButton.text = "Disconnect";
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Reconnecting:
                    _connectButton.text = "Connecting...";
                    break;
                default:
                    _connectButton.text = "Connect";
                    break;
            }
        }

        private void UpdatePresetButtonStates()
        {
            string buttonClass = _isSettingPreset ? "highlight" : "";
            
            foreach (var button in _presetButtons)
            {
                button?.RemoveFromClassList("highlight");
                if (_isSettingPreset)
                {
                    button?.AddToClassList("highlight");
                }
            }

            if (_setPresetButton != null)
            {
                _setPresetButton.RemoveFromClassList("active");
                if (_isSettingPreset)
                {
                    _setPresetButton.AddToClassList("active");
                }
            }
        }

        private void UpdateStatusDisplay()
        {
            if (Time.time - _lastUpdateTime < _statusUpdateInterval) return;
            _lastUpdateTime = Time.time;

            // Update commands counter
            if (_commandsCounter != null)
            {
                _commandsCounter.text = $"Commands Sent: {_commandsSent}";
            }
        }

        private void UpdatePositionDisplay()
        {
            if (Time.time - _lastPositionUpdateTime < _positionUpdateInterval) return;
            _lastPositionUpdateTime = Time.time;

            // TODO: Query actual camera position and update labels
            // This will be implemented when we add position inquiry commands
        }

        private void UpdateFPSCounter()
        {
            if (_fpsCounter != null)
            {
                float fps = 1.0f / Time.deltaTime;
                _fpsCounter.text = $"FPS: {fps:F0}";
            }
        }

        #endregion

        #region Helper Methods

        private void SendCommand(VISCACommand command, Dictionary<string, object> parameters)
        {
            if (_connectionManager != null && command != null)
            {
                _connectionManager.SendCommandToActiveCamera(command, parameters);
                _commandsSent++;
            }
        }

        #endregion

        #region Public Interface

        public void SetConnectionManager(CameraConnectionManager connectionManager)
        {
            if (_connectionManager != null)
                CleanupEventHandlers();

            _connectionManager = connectionManager;
            SetupEventHandlers();
            UpdateConnectionStatus();
        }

        public void SetInputHandler(CameraInputHandler inputHandler)
        {
            if (_inputHandler != null)
                _inputHandler.OnCommandGenerated -= OnCommandGenerated;

            _inputHandler = inputHandler;
            
            if (_inputHandler != null)
                _inputHandler.OnCommandGenerated += OnCommandGenerated;
        }

        public void ShowNotification(string message, float duration = 3.0f)
        {
            Debug.Log($"[MainUIController] Notification: {message}");
            // TODO: Implement notification UI system
        }

        #endregion
    }
}