using UnityEngine;
using Klak.Ndi;
using System.Linq;

public class SimpleNDITest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== SIMPLE NDI TEST STARTED ===");
        
        // Immediate test
        TestNDISources();
        
        // Repeat every 3 seconds
        InvokeRepeating(nameof(TestNDISources), 3f, 3f);
    }
    
    private void TestNDISources()
    {
        Debug.Log("--- NDI SOURCE CHECK ---");
        
        try
        {
            var sources = NdiFinder.sourceNames.ToArray();
            Debug.Log($"Found {sources.Length} NDI sources:");
            
            if (sources.Length > 0)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    Debug.Log($"  {i + 1}. '{sources[i]}'");
                }
            }
            else
            {
                Debug.Log("  No NDI sources detected");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking NDI sources: {e.Message}");
        }
        
        Debug.Log("--- END NDI CHECK ---");
    }
    
    private void OnDestroy()
    {
        CancelInvoke();
    }
}