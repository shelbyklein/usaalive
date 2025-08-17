using UnityEngine;

/// <summary>
/// Helper script to clean up old TCP VISCACommunicator components and GameObjects
/// </summary>
public class VISCACleanup : MonoBehaviour
{
    [ContextMenu("Clean Old VISCA Components")]
    public void CleanOldVISCAComponents()
    {
        Debug.Log("[CLEANUP] Starting cleanup of old TCP VISCA components...");
        
        // Find all old VISCACommunicator components (TCP-based)
        var oldCommunicators = FindObjectsOfType<VISCACommunicator>();
        
        Debug.Log($"[CLEANUP] Found {oldCommunicators.Length} old TCP VISCACommunicator components");
        
        foreach (var oldComm in oldCommunicators)
        {
            if (oldComm != null && oldComm.gameObject != null)
            {
                string objectName = oldComm.gameObject.name;
                Debug.Log($"[CLEANUP] Destroying old TCP communicator: {objectName}");
                
                if (Application.isPlaying)
                {
                    Destroy(oldComm.gameObject);
                }
                else
                {
                    DestroyImmediate(oldComm.gameObject);
                }
            }
        }
        
        // Also clean up any orphaned VISCA_Camera_* objects
        var allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.StartsWith("VISCA_Camera_") && obj.GetComponent<VISCAUDPCommunicator>() == null)
            {
                Debug.Log($"[CLEANUP] Destroying orphaned VISCA object: {obj.name}");
                
                if (Application.isPlaying)
                {
                    Destroy(obj);
                }
                else
                {
                    DestroyImmediate(obj);
                }
            }
        }
        
        Debug.Log("[CLEANUP] Cleanup complete! Old TCP components removed.");
    }
    
    [ContextMenu("Force Restart VISCA System")]
    public void ForceRestartVISCASystem()
    {
        Debug.Log("[RESTART] Force restarting VISCA system...");
        
        // Clean old components first
        CleanOldVISCAComponents();
        
        // Find VISCAController and restart it
        var viscaController = FindObjectOfType<VISCAController>();
        if (viscaController != null)
        {
            Debug.Log("[RESTART] Restarting VISCAController...");
            
            // Clear existing communicators list
            viscaController.GetType().GetField("cameraCommunicators", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(viscaController, new System.Collections.Generic.List<VISCAUDPCommunicator>());
            
            // Trigger restart by disabling and re-enabling
            viscaController.enabled = false;
            viscaController.enabled = true;
        }
        
        Debug.Log("[RESTART] VISCA system restart complete!");
    }
}