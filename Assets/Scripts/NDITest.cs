using UnityEngine;
using Klak.Ndi;
using System.Linq;

public class NDITest : MonoBehaviour
{
    [Header("NDI Test Configuration")]
    public NdiReceiver ndiReceiver;
    public Material displayMaterial;
    
    private void Start()
    {
        Debug.Log("NDI Test component started - verifying NDI functionality");
        
        // Check if NDI libraries are available
        if (System.IntPtr.Size == 8) // 64-bit check
        {
            Debug.Log("Running on 64-bit system - NDI should be available");
        }
        else
        {
            Debug.LogWarning("Running on 32-bit system - NDI may not be available");
        }
        
        // Test NDI finder
        TestNDIFinder();
    }
    
    private void TestNDIFinder()
    {
        try
        {
            Debug.Log("Testing NDI Finder static API");
            var sources = NdiFinder.sourceNames.ToArray();
            Debug.Log($"Available NDI sources: {sources.Length}");
            
            for (int i = 0; i < sources.Length; i++)
            {
                Debug.Log($"NDI Source {i}: {sources[i]}");
            }
            
            if (sources.Length == 0)
            {
                Debug.Log("No NDI sources found - ensure NDI cameras are on the network");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error testing NDI Finder: {e.Message}");
        }
    }
    
    private void Update()
    {
        // Log NDI receiver status if available
        if (ndiReceiver != null && Time.frameCount % 60 == 0) // Log once per second at 60fps
        {
            bool hasTexture = ndiReceiver.texture != null;
            Debug.Log($"NDI Receiver status - Connected: {hasTexture}, Source: {ndiReceiver.ndiName}");
        }
    }
}