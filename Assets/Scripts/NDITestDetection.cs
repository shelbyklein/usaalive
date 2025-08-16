using UnityEngine;
using Klak.Ndi;
using System.Linq;

public class NDITestDetection : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== NDI Test Detection Started ===");
        InvokeRepeating(nameof(CheckNDISources), 0f, 3f);
    }
    
    private void CheckNDISources()
    {
        try
        {
            var sources = NdiFinder.sourceNames.ToArray();
            Debug.Log($"[NDI Test] Found {sources.Length} NDI sources:");
            
            if (sources.Length > 0)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    Debug.Log($"[NDI Test]   {i + 1}. {sources[i]}");
                }
            }
            else
            {
                Debug.Log("[NDI Test] No NDI sources detected. Check network and NDI setup.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NDI Test] Error checking sources: {e.Message}");
        }
    }
    
    private void OnDestroy()
    {
        CancelInvoke();
    }
}