using UnityEngine;

[System.Serializable]
public class VISCASetupHelper : MonoBehaviour
{
    [Header("Setup Configuration")]
    [SerializeField] private int numberOfCameras = 6;
    [SerializeField] private string baseIPAddress = "192.168.1.";
    [SerializeField] private int startingIPSuffix = 100;
    [SerializeField] private int viscaPort = 52381;
    
    [Header("Manual Setup")]
    [SerializeField] private bool createCamerasOnStart = false;
    
    private void Start()
    {
        if (createCamerasOnStart)
        {
            SetupVISCACameras();
        }
    }
    
    [ContextMenu("Setup VISCA Cameras")]
    public void SetupVISCACameras()
    {
        var viscaController = FindObjectOfType<VISCAController>();
        if (viscaController == null)
        {
            Debug.LogError("VISCAController not found in scene!");
            return;
        }
        
        Debug.Log($"[VISCA Setup] Creating {numberOfCameras} camera communicators...");
        
        for (int i = 0; i < numberOfCameras; i++)
        {
            CreateCameraCommunicator(i, viscaController.transform);
        }
        
        Debug.Log($"[VISCA Setup] Setup complete! Created {numberOfCameras} camera communicators.");
        Debug.Log("[VISCA Setup] Configure IP addresses in the Inspector if needed, then test camera selection.");
    }
    
    private void CreateCameraCommunicator(int cameraIndex, Transform parent)
    {
        // Create GameObject
        var cameraObj = new GameObject($"VISCA_Camera_{cameraIndex + 1}");
        cameraObj.transform.SetParent(parent);
        
        // Add VISCACommunicator component
        var communicator = cameraObj.AddComponent<VISCACommunicator>();
        
        // Create and assign camera config
        var config = new VISCACameraConfig
        {
            CameraAddress = cameraIndex + 1,
            IPAddress = $"{baseIPAddress}{startingIPSuffix + cameraIndex}",
            Port = viscaPort,
            UseSerial = false
        };
        
        communicator.cameraConfig = config;
        
        Debug.Log($"[VISCA Setup] Created Camera {cameraIndex + 1} - IP: {config.IPAddress}:{config.Port}");
    }
    
    [ContextMenu("Clear VISCA Cameras")]
    public void ClearVISCACameras()
    {
        var viscaController = FindObjectOfType<VISCAController>();
        if (viscaController == null)
        {
            Debug.LogWarning("VISCAController not found in scene!");
            return;
        }
        
        // Find and destroy existing camera objects
        for (int i = viscaController.transform.childCount - 1; i >= 0; i--)
        {
            var child = viscaController.transform.GetChild(i);
            if (child.name.StartsWith("VISCA_Camera_"))
            {
                Debug.Log($"[VISCA Setup] Removing {child.name}");
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        Debug.Log("[VISCA Setup] All VISCA cameras cleared.");
    }
    
    [ContextMenu("Test Camera Connection")]
    public void TestCameraConnections()
    {
        var communicators = FindObjectsOfType<VISCACommunicator>();
        
        if (communicators.Length == 0)
        {
            Debug.LogWarning("[VISCA Setup] No camera communicators found. Run 'Setup VISCA Cameras' first.");
            return;
        }
        
        Debug.Log($"[VISCA Setup] Testing {communicators.Length} camera connections...");
        
        foreach (var communicator in communicators)
        {
            if (communicator.cameraConfig != null)
            {
                Debug.Log($"[VISCA Setup] Camera {communicator.cameraConfig.CameraAddress}: {communicator.cameraConfig.IPAddress}:{communicator.cameraConfig.Port} - Connected: {communicator.IsConnected}");
            }
        }
    }
}