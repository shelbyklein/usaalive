using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using USAALive.VISCA;

namespace USAALive.Networking
{
    [Serializable]
    public class CameraConfiguration
    {
        public string name = "Camera 1";
        public string ipAddress = "192.168.1.100";
        public int port = 52381;
        public byte address = 0x81;
        public bool autoConnect = true;
        public bool enabled = true;
    }

    public class CameraConnectionManager : MonoBehaviour
    {
        [Header("Camera Configurations")]
        [SerializeField] private List<CameraConfiguration> _cameraConfigs = new List<CameraConfiguration>();

        [Header("Connection Settings")]
        [SerializeField] private bool _autoConnectOnStart = true;
        [SerializeField] private float _connectionRetryDelay = 2.0f;
        [SerializeField] private int _maxConnectionRetries = 3;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = true;

        public event Action<int, ConnectionState> OnCameraConnectionStateChanged;
        public event Action<int, VISCAResponse> OnCameraResponseReceived;
        public event Action<int, string> OnCameraError;
        public event Action<int> OnActiveCameraChanged;

        public int ActiveCameraIndex { get; private set; } = 0;
        public int CameraCount => _cameraConnections.Count;
        public bool HasActiveCameraConnected => ActiveCameraIndex >= 0 && 
                                               ActiveCameraIndex < _cameraConnections.Count && 
                                               _cameraConnections[ActiveCameraIndex]?.IsConnected == true;

        private List<TCPCameraConnection> _cameraConnections = new List<TCPCameraConnection>();
        private Dictionary<int, int> _connectionRetryCount = new Dictionary<int, int>();

        #region Unity Lifecycle

        private void Start()
        {
            LogDebug("CameraConnectionManager starting up");
            InitializeCameraConnections();

            if (_autoConnectOnStart)
            {
                ConnectAllCamerasAsync().ConfigureAwait(false);
            }
        }

        private void OnDestroy()
        {
            DisconnectAllCamerasAsync().ConfigureAwait(false);
        }

        #endregion

        #region Initialization

        private void InitializeCameraConnections()
        {
            ClearExistingConnections();

            for (int i = 0; i < _cameraConfigs.Count; i++)
            {
                if (_cameraConfigs[i].enabled)
                {
                    CreateCameraConnection(i);
                }
            }

            LogDebug($"Initialized {_cameraConnections.Count} camera connections");
        }

        private void ClearExistingConnections()
        {
            foreach (var connection in _cameraConnections)
            {
                if (connection != null)
                {
                    connection.OnConnectionStateChanged -= (state) => HandleConnectionStateChanged(GetCameraIndex(connection), state);
                    connection.OnResponseReceived -= (response) => HandleResponseReceived(GetCameraIndex(connection), response);
                    connection.OnError -= (error) => HandleError(GetCameraIndex(connection), error);
                    
                    if (connection.gameObject != null)
                    {
                        DestroyImmediate(connection.gameObject);
                    }
                }
            }

            _cameraConnections.Clear();
            _connectionRetryCount.Clear();
        }

        private void CreateCameraConnection(int configIndex)
        {
            var config = _cameraConfigs[configIndex];
            var connectionGO = new GameObject($"CameraConnection_{config.name}");
            connectionGO.transform.SetParent(transform);

            var connection = connectionGO.AddComponent<TCPCameraConnection>();
            connection.SetConnectionSettings(config.ipAddress, config.port);
            connection.SetCameraAddress(config.address);

            int cameraIndex = _cameraConnections.Count;
            connection.OnConnectionStateChanged += (state) => HandleConnectionStateChanged(cameraIndex, state);
            connection.OnResponseReceived += (response) => HandleResponseReceived(cameraIndex, response);
            connection.OnError += (error) => HandleError(cameraIndex, error);

            _cameraConnections.Add(connection);
            _connectionRetryCount[cameraIndex] = 0;

            LogDebug($"Created camera connection {cameraIndex}: {config.name} ({config.ipAddress}:{config.port})");
        }

        #endregion

        #region Connection Management

        public async Task<bool> ConnectAllCamerasAsync()
        {
            LogDebug("Connecting all cameras");
            var connectionTasks = new List<Task<bool>>();

            for (int i = 0; i < _cameraConnections.Count; i++)
            {
                if (_cameraConfigs[i].autoConnect && _cameraConfigs[i].enabled)
                {
                    connectionTasks.Add(ConnectCameraAsync(i));
                }
            }

            var results = await Task.WhenAll(connectionTasks);
            int connectedCount = results.Count(r => r);

            LogDebug($"Connected {connectedCount}/{connectionTasks.Count} cameras");
            return connectedCount > 0;
        }

        public async Task<bool> ConnectCameraAsync(int cameraIndex)
        {
            if (!IsValidCameraIndex(cameraIndex))
            {
                LogError($"Invalid camera index: {cameraIndex}");
                return false;
            }

            var connection = _cameraConnections[cameraIndex];
            var config = _cameraConfigs[cameraIndex];

            LogDebug($"Connecting camera {cameraIndex}: {config.name}");

            try
            {
                bool success = await connection.ConnectAsync();
                if (success)
                {
                    _connectionRetryCount[cameraIndex] = 0;
                    LogDebug($"Successfully connected camera {cameraIndex}");
                }
                else
                {
                    HandleConnectionRetry(cameraIndex);
                }
                return success;
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect camera {cameraIndex}: {ex.Message}");
                HandleConnectionRetry(cameraIndex);
                return false;
            }
        }

        public async Task DisconnectCameraAsync(int cameraIndex)
        {
            if (!IsValidCameraIndex(cameraIndex))
            {
                LogError($"Invalid camera index: {cameraIndex}");
                return;
            }

            var connection = _cameraConnections[cameraIndex];
            var config = _cameraConfigs[cameraIndex];

            LogDebug($"Disconnecting camera {cameraIndex}: {config.name}");
            await connection.DisconnectAsync();
        }

        public async Task DisconnectAllCamerasAsync()
        {
            LogDebug("Disconnecting all cameras");
            var disconnectionTasks = new List<Task>();

            for (int i = 0; i < _cameraConnections.Count; i++)
            {
                disconnectionTasks.Add(DisconnectCameraAsync(i));
            }

            await Task.WhenAll(disconnectionTasks);
            LogDebug("All cameras disconnected");
        }

        private void HandleConnectionRetry(int cameraIndex)
        {
            _connectionRetryCount[cameraIndex]++;
            
            if (_connectionRetryCount[cameraIndex] <= _maxConnectionRetries)
            {
                LogDebug($"Scheduling retry {_connectionRetryCount[cameraIndex]}/{_maxConnectionRetries} for camera {cameraIndex}");
                
                Task.Delay(TimeSpan.FromSeconds(_connectionRetryDelay)).ContinueWith(async _ =>
                {
                    await ConnectCameraAsync(cameraIndex);
                });
            }
            else
            {
                LogError($"Max connection retries exceeded for camera {cameraIndex}");
            }
        }

        #endregion

        #region Camera Selection

        public bool SetActiveCamera(int cameraIndex)
        {
            if (!IsValidCameraIndex(cameraIndex))
            {
                LogError($"Invalid camera index: {cameraIndex}");
                return false;
            }

            if (!_cameraConnections[cameraIndex].IsConnected)
            {
                LogError($"Camera {cameraIndex} is not connected");
                return false;
            }

            int previousIndex = ActiveCameraIndex;
            ActiveCameraIndex = cameraIndex;

            LogDebug($"Active camera changed from {previousIndex} to {cameraIndex}");
            OnActiveCameraChanged?.Invoke(cameraIndex);
            return true;
        }

        public bool SetActiveCameraByName(string cameraName)
        {
            int index = _cameraConfigs.FindIndex(c => c.name.Equals(cameraName, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                return SetActiveCamera(index);
            }

            LogError($"Camera with name '{cameraName}' not found");
            return false;
        }

        public int GetNextConnectedCamera()
        {
            int startIndex = (ActiveCameraIndex + 1) % _cameraConnections.Count;
            
            for (int i = 0; i < _cameraConnections.Count; i++)
            {
                int checkIndex = (startIndex + i) % _cameraConnections.Count;
                if (_cameraConnections[checkIndex].IsConnected)
                {
                    return checkIndex;
                }
            }

            return -1;
        }

        public int GetPreviousConnectedCamera()
        {
            int startIndex = ActiveCameraIndex - 1;
            if (startIndex < 0) startIndex = _cameraConnections.Count - 1;
            
            for (int i = 0; i < _cameraConnections.Count; i++)
            {
                int checkIndex = startIndex - i;
                if (checkIndex < 0) checkIndex += _cameraConnections.Count;
                
                if (_cameraConnections[checkIndex].IsConnected)
                {
                    return checkIndex;
                }
            }

            return -1;
        }

        #endregion

        #region Command Sending

        public async Task<VISCAResponse> SendCommandToActiveCameraAsync(VISCACommand command, Dictionary<string, object> parameters = null)
        {
            if (!HasActiveCameraConnected)
            {
                throw new InvalidOperationException("No active camera connected");
            }

            return await _cameraConnections[ActiveCameraIndex].SendCommandAsync(command, parameters);
        }

        public async Task<VISCAResponse> SendCommandToCameraAsync(int cameraIndex, VISCACommand command, Dictionary<string, object> parameters = null)
        {
            if (!IsValidCameraIndex(cameraIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(cameraIndex));
            }

            if (!_cameraConnections[cameraIndex].IsConnected)
            {
                throw new InvalidOperationException($"Camera {cameraIndex} is not connected");
            }

            return await _cameraConnections[cameraIndex].SendCommandAsync(command, parameters);
        }

        public void SendCommandToActiveCamera(VISCACommand command, Dictionary<string, object> parameters = null)
        {
            if (!HasActiveCameraConnected)
            {
                LogError("No active camera connected");
                return;
            }

            _cameraConnections[ActiveCameraIndex].SendCommandFireAndForget(command, parameters);
        }

        public void SendCommandToCamera(int cameraIndex, VISCACommand command, Dictionary<string, object> parameters = null)
        {
            if (!IsValidCameraIndex(cameraIndex))
            {
                LogError($"Invalid camera index: {cameraIndex}");
                return;
            }

            if (!_cameraConnections[cameraIndex].IsConnected)
            {
                LogError($"Camera {cameraIndex} is not connected");
                return;
            }

            _cameraConnections[cameraIndex].SendCommandFireAndForget(command, parameters);
        }

        #endregion

        #region Event Handlers

        private void HandleConnectionStateChanged(int cameraIndex, ConnectionState state)
        {
            LogDebug($"Camera {cameraIndex} connection state: {state}");
            OnCameraConnectionStateChanged?.Invoke(cameraIndex, state);

            if (state == ConnectionState.Connected && GetConnectedCameraCount() == 1)
            {
                SetActiveCamera(cameraIndex);
            }
            else if (state == ConnectionState.Disconnected && cameraIndex == ActiveCameraIndex)
            {
                int nextCamera = GetNextConnectedCamera();
                if (nextCamera >= 0)
                {
                    SetActiveCamera(nextCamera);
                }
            }
        }

        private void HandleResponseReceived(int cameraIndex, VISCAResponse response)
        {
            OnCameraResponseReceived?.Invoke(cameraIndex, response);
        }

        private void HandleError(int cameraIndex, string error)
        {
            LogError($"Camera {cameraIndex} error: {error}");
            OnCameraError?.Invoke(cameraIndex, error);
        }

        #endregion

        #region Query Methods

        public CameraConfiguration GetCameraConfig(int cameraIndex)
        {
            return IsValidCameraIndex(cameraIndex) ? _cameraConfigs[cameraIndex] : null;
        }

        public TCPCameraConnection GetCameraConnection(int cameraIndex)
        {
            return IsValidCameraIndex(cameraIndex) ? _cameraConnections[cameraIndex] : null;
        }

        public ConnectionState GetCameraConnectionState(int cameraIndex)
        {
            return IsValidCameraIndex(cameraIndex) ? _cameraConnections[cameraIndex].State : ConnectionState.Disconnected;
        }

        public int GetConnectedCameraCount()
        {
            return _cameraConnections.Count(c => c.IsConnected);
        }

        public List<int> GetConnectedCameraIndices()
        {
            var indices = new List<int>();
            for (int i = 0; i < _cameraConnections.Count; i++)
            {
                if (_cameraConnections[i].IsConnected)
                {
                    indices.Add(i);
                }
            }
            return indices;
        }

        public List<string> GetConnectedCameraNames()
        {
            var names = new List<string>();
            for (int i = 0; i < _cameraConnections.Count; i++)
            {
                if (_cameraConnections[i].IsConnected)
                {
                    names.Add(_cameraConfigs[i].name);
                }
            }
            return names;
        }

        #endregion

        #region Configuration Management

        public void AddCameraConfiguration(CameraConfiguration config)
        {
            _cameraConfigs.Add(config);
            LogDebug($"Added camera configuration: {config.name}");
            
            if (Application.isPlaying)
            {
                InitializeCameraConnections();
            }
        }

        public bool RemoveCameraConfiguration(int cameraIndex)
        {
            if (!IsValidCameraIndex(cameraIndex))
            {
                return false;
            }

            if (Application.isPlaying)
            {
                DisconnectCameraAsync(cameraIndex).ConfigureAwait(false);
            }

            _cameraConfigs.RemoveAt(cameraIndex);
            
            if (Application.isPlaying)
            {
                InitializeCameraConnections();
            }

            LogDebug($"Removed camera configuration at index {cameraIndex}");
            return true;
        }

        public void UpdateCameraConfiguration(int cameraIndex, CameraConfiguration newConfig)
        {
            if (!IsValidCameraIndex(cameraIndex))
            {
                return;
            }

            bool wasConnected = Application.isPlaying && _cameraConnections[cameraIndex].IsConnected;
            
            if (wasConnected)
            {
                DisconnectCameraAsync(cameraIndex).ConfigureAwait(false);
            }

            _cameraConfigs[cameraIndex] = newConfig;
            
            if (Application.isPlaying)
            {
                InitializeCameraConnections();
                
                if (wasConnected && newConfig.autoConnect)
                {
                    ConnectCameraAsync(cameraIndex).ConfigureAwait(false);
                }
            }

            LogDebug($"Updated camera configuration at index {cameraIndex}");
        }

        #endregion

        #region Helper Methods

        private bool IsValidCameraIndex(int index)
        {
            return index >= 0 && index < _cameraConnections.Count;
        }

        private int GetCameraIndex(TCPCameraConnection connection)
        {
            return _cameraConnections.IndexOf(connection);
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[CameraConnectionManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CameraConnectionManager] {message}");
        }

        #endregion
    }
}