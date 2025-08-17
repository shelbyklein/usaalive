using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the mapping between camera indices and their actual IP addresses
/// Bridges NDI detection with VISCA control
/// </summary>
public class CameraIPMapper : MonoBehaviour
{
    [Header("Camera IP Mapping")]
    [SerializeField] private bool debugLogging = true;
    
    private Dictionary<int, string> cameraIPMap = new Dictionary<int, string>();
    private NDICameraGridManager gridManager;
    
    public static CameraIPMapper Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Find the NDI camera grid manager
        gridManager = FindObjectOfType<NDICameraGridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning("[Camera IP Mapper] NDICameraGridManager not found!");
        }
        
        // Start monitoring for IP changes
        InvokeRepeating(nameof(UpdateIPMapping), 1f, 2f);
    }
    
    private void UpdateIPMapping()
    {
        if (gridManager == null || gridManager.cameraCells == null)
            return;
        
        bool hasChanges = false;
        
        for (int i = 0; i < gridManager.cameraCells.Count; i++)
        {
            var cell = gridManager.cameraCells[i];
            if (cell.ndiReceiver != null && !string.IsNullOrEmpty(cell.ndiReceiver.ndiName))
            {
                string detectedIP = NDIIPExtractor.ExtractIPFromNDISource(cell.ndiReceiver.ndiName);
                
                if (!string.IsNullOrEmpty(detectedIP) && NDIIPExtractor.IsValidIPAddress(detectedIP))
                {
                    if (!cameraIPMap.ContainsKey(i) || cameraIPMap[i] != detectedIP)
                    {
                        cameraIPMap[i] = detectedIP;
                        hasChanges = true;
                        
                        if (debugLogging)
                        {
                            Debug.Log($"[Camera IP Mapper] Updated Camera {i + 1} → IP: {detectedIP}");
                        }
                    }
                }
            }
        }
        
        if (hasChanges)
        {
            NotifyVISCAControllerOfIPChanges();
        }
    }
    
    private void NotifyVISCAControllerOfIPChanges()
    {
        var viscaController = FindObjectOfType<VISCAController>();
        if (viscaController != null)
        {
            // Trigger VISCA controller to update with new IPs
            viscaController.ForceRestartVISCASystem();
            
            if (debugLogging)
            {
                Debug.Log("[Camera IP Mapper] Notified VISCA controller of IP changes");
            }
        }
    }
    
    /// <summary>
    /// Get the actual IP address for a camera index
    /// </summary>
    public string GetCameraIP(int cameraIndex)
    {
        if (cameraIPMap.TryGetValue(cameraIndex, out string ip))
        {
            return ip;
        }
        
        // Fallback to direct NDI query if not in cache
        if (gridManager?.cameraCells != null && cameraIndex < gridManager.cameraCells.Count)
        {
            var cell = gridManager.cameraCells[cameraIndex];
            if (cell.ndiReceiver != null && !string.IsNullOrEmpty(cell.ndiReceiver.ndiName))
            {
                string detectedIP = NDIIPExtractor.ExtractIPFromNDISource(cell.ndiReceiver.ndiName);
                if (!string.IsNullOrEmpty(detectedIP) && NDIIPExtractor.IsValidIPAddress(detectedIP))
                {
                    cameraIPMap[cameraIndex] = detectedIP;
                    return detectedIP;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get all mapped camera IPs
    /// </summary>
    public Dictionary<int, string> GetAllCameraIPs()
    {
        return new Dictionary<int, string>(cameraIPMap);
    }
    
    /// <summary>
    /// Get camera info for debugging
    /// </summary>
    public string GetCameraInfo(int cameraIndex)
    {
        string ip = GetCameraIP(cameraIndex);
        if (!string.IsNullOrEmpty(ip))
        {
            return $"Camera {cameraIndex + 1}: {ip}";
        }
        
        return $"Camera {cameraIndex + 1}: No IP detected";
    }
    
    /// <summary>
    /// Force an immediate update of the IP mapping
    /// </summary>
    public void ForceUpdate()
    {
        UpdateIPMapping();
    }
    
    /// <summary>
    /// Get the number of cameras with detected IPs
    /// </summary>
    public int GetDetectedCameraCount()
    {
        return cameraIPMap.Count;
    }
    
    [ContextMenu("Debug Print All Camera IPs")]
    public void DebugPrintAllCameraIPs()
    {
        Debug.Log($"[Camera IP Mapper] Total cameras detected: {cameraIPMap.Count}");
        foreach (var kvp in cameraIPMap)
        {
            Debug.Log($"[Camera IP Mapper] Camera {kvp.Key + 1}: {kvp.Value}");
        }
    }
}