using UnityEngine;
using Klak.Ndi;
using System.Linq;

public class NDISourceFinder : MonoBehaviour
{
    private void Start()
    {
        InvokeRepeating(nameof(CheckForNDISources), 1f, 5f); // Check every 5 seconds
        Debug.Log("NDI Source Finder started - scanning for NDI sources every 5 seconds");
    }
    
    private void CheckForNDISources()
    {
        // Use NdiFinder static API to check for sources
        var sources = NdiFinder.sourceNames.ToArray();
        Debug.Log($"NDI Source scan - Found {sources.Length} sources:");
        
        for (int i = 0; i < sources.Length; i++)
        {
            Debug.Log($"  Source {i + 1}: {sources[i]}");
        }
        
        if (sources.Length == 0)
        {
            Debug.Log("  No NDI sources detected. Ensure NDI cameras are on the network.");
        }
    }
    
    private void OnDestroy()
    {
        CancelInvoke();
    }
}