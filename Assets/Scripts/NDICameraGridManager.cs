using UnityEngine;
using UnityEngine.UIElements;
using Klak.Ndi;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class NDICameraGridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private int maxCameras = 4;
    [SerializeField] private int gridColumns = 2;
    [SerializeField] private int gridRows = 2;
    
    [Header("Video Configuration")]
    [SerializeField] private int targetWidth = 1920;
    [SerializeField] private int targetHeight = 1080;
    
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("NDI Resources")]
    [SerializeField] private NdiResources ndiResources;
    
    private VisualElement gridContainer;
    private List<NDICameraCell> cameraCells = new List<NDICameraCell>();
    private List<string> lastKnownSources = new List<string>();
    
    private void Start()
    {
        // Load NDI Resources if not assigned
        if (ndiResources == null)
        {
            // Try to load from the KlakNDI package
            ndiResources = Resources.Load<NdiResources>("NdiResources");
            if (ndiResources == null)
            {
                // Try to find it in the project
                var foundResources = Resources.FindObjectsOfTypeAll<NdiResources>();
                if (foundResources.Length > 0)
                {
                    ndiResources = foundResources[0];
                    Debug.Log($"Found NDI Resources: {ndiResources.name}");
                }
                else
                {
                    Debug.LogWarning("NDI Resources not found - will try to continue without it");
                }
            }
        }
        
        InitializeUI();
        
        // Immediate NDI source check
        var sources = NdiFinder.sourceNames.ToList();
        Debug.Log($"Immediate NDI source check found {sources.Count} sources:");
        foreach (var source in sources)
        {
            Debug.Log($"  - {source}");
        }
        
        StartCoroutine(NDISourceDetectionLoop());
    }
    
    private void InitializeUI()
    {
        if (uiDocument == null)
        {
            uiDocument = FindObjectOfType<UIDocument>();
            Debug.Log($"Found UIDocument: {uiDocument?.name}");
        }
            
        if (uiDocument?.rootVisualElement != null)
        {
            var root = uiDocument.rootVisualElement;
            Debug.Log($"Root element found: {root.name}");
            
            gridContainer = root.Q<VisualElement>("camera-display");
            Debug.Log($"Grid container found: {gridContainer != null}");
            
            if (gridContainer != null)
            {
                SetupGridLayout();
                Debug.Log("NDI Camera Grid Manager initialized successfully");
                
                // Force an initial update to show the grid even without sources
                UpdateCameraGrid(new List<string>());
            }
            else
            {
                Debug.LogError("Grid container 'camera-display' not found in UI");
                // List all available elements for debugging
                DebugUIElements(root, 0);
            }
        }
        else
        {
            Debug.LogError("UIDocument or root element is null");
        }
    }
    
    private void DebugUIElements(VisualElement element, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}Element: {element.name} (class: {element.GetClasses().FirstOrDefault()})");
        
        if (depth < 3) // Limit depth to avoid spam
        {
            foreach (var child in element.Children())
            {
                DebugUIElements(child, depth + 1);
            }
        }
    }
    
    private void SetupGridLayout()
    {
        // Clear existing content
        gridContainer.Clear();
        
        // Create CSS for grid layout
        gridContainer.style.display = DisplayStyle.Flex;
        gridContainer.style.flexDirection = FlexDirection.Column;
        
        // Create rows
        for (int row = 0; row < gridRows; row++)
        {
            var rowElement = new VisualElement();
            rowElement.style.flexDirection = FlexDirection.Row;
            rowElement.style.flexGrow = 1;
            
            // Create columns in this row
            for (int col = 0; col < gridColumns; col++)
            {
                int cellIndex = row * gridColumns + col;
                if (cellIndex >= maxCameras) break;
                
                var cellElement = CreateCameraCell(cellIndex);
                rowElement.Add(cellElement);
            }
            
            gridContainer.Add(rowElement);
        }
        
        Debug.Log($"Grid layout created: {gridRows}x{gridColumns} for {maxCameras} cameras");
    }
    
    private VisualElement CreateCameraCell(int index)
    {
        var cell = new VisualElement();
        cell.name = $"camera-cell-{index}";
        cell.AddToClassList("camera-cell");
        
        // Style the cell
        cell.style.flexGrow = 1;
        cell.style.height = Length.Percent(100);
        cell.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        cell.style.borderLeftWidth = 1;
        cell.style.borderRightWidth = 1;
        cell.style.borderTopWidth = 1;
        cell.style.borderBottomWidth = 1;
        cell.style.borderLeftColor = Color.gray;
        cell.style.borderRightColor = Color.gray;
        cell.style.borderTopColor = Color.gray;
        cell.style.borderBottomColor = Color.gray;
        cell.style.marginLeft = 2;
        cell.style.marginRight = 2;
        cell.style.marginTop = 2;
        cell.style.marginBottom = 2;
        
        // Create camera content area
        var contentArea = new VisualElement();
        contentArea.name = $"camera-content-{index}";
        contentArea.style.width = Length.Percent(100);
        contentArea.style.height = Length.Percent(100);
        contentArea.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        
        // Create label overlay
        var label = new Label($"Camera {index + 1} - No Signal");
        label.name = $"camera-label-{index}";
        label.style.position = Position.Absolute;
        label.style.top = 10;
        label.style.left = 10;
        label.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        label.style.color = Color.white;
        label.style.paddingLeft = 8;
        label.style.paddingRight = 8;
        label.style.paddingTop = 4;
        label.style.paddingBottom = 4;
        label.style.fontSize = 12;
        
        cell.Add(contentArea);
        cell.Add(label);
        
        // Create NDI camera cell data
        var cameraCell = new NDICameraCell
        {
            index = index,
            cellElement = cell,
            contentArea = contentArea,
            label = label,
            ndiReceiver = null,
            isActive = false
        };
        
        cameraCells.Add(cameraCell);
        return cell;
    }
    
    private IEnumerator NDISourceDetectionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f); // Check every 2 seconds
            
            Debug.Log("=== NDI SOURCE DETECTION SCAN ===");
            
            try
            {
                var currentSources = NdiFinder.sourceNames.ToList();
                Debug.Log($"[NDI DEBUG] Raw sourceNames enumeration returned {currentSources.Count} items");
                
                // Detailed logging of each source
                for (int i = 0; i < currentSources.Count; i++)
                {
                    Debug.Log($"[NDI DEBUG] Source {i}: '{currentSources[i]}' (Length: {currentSources[i]?.Length ?? 0})");
                }
                
                // Check if sources have changed
                bool sourcesChanged = !currentSources.SequenceEqual(lastKnownSources);
                Debug.Log($"[NDI DEBUG] Sources changed: {sourcesChanged}");
                
                if (sourcesChanged)
                {
                    Debug.Log($"[NDI DEBUG] *** NDI SOURCES CHANGED ***");
                    Debug.Log($"[NDI DEBUG] Previous sources: {string.Join(", ", lastKnownSources)}");
                    Debug.Log($"[NDI DEBUG] Current sources: {string.Join(", ", currentSources)}");
                    
                    Debug.Log($"NDI sources changed. Found {currentSources.Count} sources:");
                    foreach (var source in currentSources)
                    {
                        Debug.Log($"  - '{source}'");
                    }
                    
                    UpdateCameraGrid(currentSources);
                    lastKnownSources = new List<string>(currentSources);
                }
                else
                {
                    Debug.Log($"[NDI DEBUG] No changes detected. Still {currentSources.Count} sources.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NDI DEBUG] Exception during source detection: {e.Message}");
                Debug.LogError($"[NDI DEBUG] Stack trace: {e.StackTrace}");
            }
            
            Debug.Log("=== END NDI SCAN ===");
        }
    }
    
    private void UpdateCameraGrid(List<string> sources)
    {
        Debug.Log($"[NDI DEBUG] === UPDATING CAMERA GRID ===");
        Debug.Log($"[NDI DEBUG] Sources to process: {sources.Count}");
        Debug.Log($"[NDI DEBUG] Available camera cells: {cameraCells.Count}");
        Debug.Log($"[NDI DEBUG] Max cameras: {maxCameras}");
        
        // First, clear all existing receivers
        ClearAllReceivers();
        
        // Assign sources to available camera cells
        int sourcesToAssign = Mathf.Min(sources.Count, maxCameras);
        Debug.Log($"[NDI DEBUG] Will assign {sourcesToAssign} sources to cells");
        
        int cellIndex = 0;
        for (int i = 0; i < sources.Count && cellIndex < maxCameras; i++)
        {
            Debug.Log($"[NDI DEBUG] Assigning source '{sources[i]}' to cell {cellIndex}");
            AssignSourceToCell(cellIndex, sources[i]);
            cellIndex++;
        }
        
        // Update remaining cells to show "No Signal"
        for (int i = cellIndex; i < cameraCells.Count; i++)
        {
            Debug.Log($"[NDI DEBUG] Setting cell {i} to 'No Signal'");
            var cell = cameraCells[i];
            cell.label.text = $"Camera {i + 1} - No Signal";
            cell.contentArea.style.backgroundImage = StyleKeyword.None;
            cell.isActive = false;
        }
        
        Debug.Log($"[NDI DEBUG] === GRID UPDATE COMPLETE ===");
    }
    
    private void ClearAllReceivers()
    {
        foreach (var cell in cameraCells)
        {
            if (cell.ndiReceiver != null)
            {
                // Clean up target texture
                if (cell.ndiReceiver.targetTexture != null)
                {
                    cell.ndiReceiver.targetTexture.Release();
                    if (Application.isPlaying)
                    {
                        Destroy(cell.ndiReceiver.targetTexture);
                    }
                }
                
                if (Application.isPlaying)
                {
                    Destroy(cell.ndiReceiver.gameObject);
                }
                cell.ndiReceiver = null;
            }
            cell.isActive = false;
        }
    }
    
    private void AssignSourceToCell(int cellIndex, string sourceName)
    {
        Debug.Log($"[NDI DEBUG] === ASSIGNING SOURCE TO CELL ===");
        Debug.Log($"[NDI DEBUG] Cell index: {cellIndex}, Source: '{sourceName}'");
        
        if (cellIndex >= cameraCells.Count)
        {
            Debug.LogError($"[NDI DEBUG] Cell index {cellIndex} out of range (max: {cameraCells.Count - 1})");
            return;
        }
        
        var cell = cameraCells[cellIndex];
        Debug.Log($"[NDI DEBUG] Got cell {cellIndex}, creating receiver GameObject");
        
        // Create NDI Receiver GameObject
        var receiverGO = new GameObject($"NDI_Camera_{cellIndex + 1}");
        receiverGO.transform.SetParent(transform);
        Debug.Log($"[NDI DEBUG] Created GameObject: {receiverGO.name}");
        
        var receiver = receiverGO.AddComponent<NdiReceiver>();
        Debug.Log($"[NDI DEBUG] Added NdiReceiver component");
        
        if (ndiResources != null)
        {
            receiver.SetResources(ndiResources);
            Debug.Log($"[NDI DEBUG] Set NDI resources: {ndiResources.name}");
        }
        else
        {
            Debug.LogWarning($"[NDI DEBUG] NDI Resources is null - receiver may not work properly");
        }
        
        // Create target render texture with fixed resolution
        var targetTexture = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        targetTexture.name = $"NDI_Camera_{cellIndex + 1}_Output";
        receiver.targetTexture = targetTexture;
        Debug.Log($"[NDI DEBUG] Created target texture: {targetWidth}x{targetHeight}");
        
        receiver.ndiName = sourceName;
        Debug.Log($"[NDI DEBUG] Set receiver NDI name to: '{sourceName}'");
        
        cell.ndiReceiver = receiver;
        cell.isActive = true;
        cell.label.text = $"Camera {cellIndex + 1} - {sourceName}";
        
        Debug.Log($"[NDI DEBUG] Successfully assigned NDI source '{sourceName}' to camera cell {cellIndex + 1}");
        
        // Start monitoring this receiver for video updates and frame size issues
        StartCoroutine(MonitorCameraFeed(cell));
        StartCoroutine(MonitorFrameSizeIssues(cell));
        Debug.Log($"[NDI DEBUG] Started monitoring coroutines for cell {cellIndex + 1}");
    }
    
    private IEnumerator MonitorCameraFeed(NDICameraCell cell)
    {
        Debug.Log($"[NDI DEBUG] === STARTING CAMERA FEED MONITORING ===");
        Debug.Log($"[NDI DEBUG] Monitoring cell {cell.index + 1}");
        
        int frameCount = 0;
        bool hasLoggedTexture = false;
        
        while (cell.isActive && cell.ndiReceiver != null)
        {
            yield return null; // Wait one frame
            frameCount++;
            
            // Check both the receiver's texture and target texture
            var activeTexture = cell.ndiReceiver.targetTexture ?? cell.ndiReceiver.texture;
            
            if (activeTexture != null)
            {
                if (!hasLoggedTexture)
                {
                    Debug.Log($"[NDI DEBUG] Cell {cell.index + 1}: Got texture! Size: {activeTexture.width}x{activeTexture.height}");
                    hasLoggedTexture = true;
                }
                
                // Update the UI with the video feed
                cell.contentArea.style.backgroundImage = Background.FromRenderTexture(activeTexture);
                cell.contentArea.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                
                // // Log periodically when we have texture
                // if (frameCount % 300 == 0) // Every ~5 seconds at 60fps
                // {
                //     Debug.Log($"[NDI DEBUG] Cell {cell.index + 1}: Still receiving video ({activeTexture.width}x{activeTexture.height})");
                // }
            }
            else
            {
                // No video feed available
                cell.contentArea.style.backgroundImage = StyleKeyword.None;
                
                // Log periodically when we don't have texture
                if (frameCount % 120 == 0) // Every ~2 seconds at 60fps
                {
                    Debug.Log($"[NDI DEBUG] Cell {cell.index + 1}: No texture yet (frame {frameCount})");
                }
            }
        }
        
        Debug.Log($"[NDI DEBUG] Stopped monitoring cell {cell.index + 1} (active: {cell.isActive}, receiver: {cell.ndiReceiver != null})");
    }
    
    private IEnumerator MonitorFrameSizeIssues(NDICameraCell cell)
    {
        Debug.Log($"[NDI DEBUG] === STARTING FRAME SIZE MONITORING ===");
        Debug.Log($"[NDI DEBUG] Monitoring frame size issues for cell {cell.index + 1}");
        
        float lastWarningTime = 0f;
        bool hasShownFrameSizeWarning = false;
        
        while (cell.isActive && cell.ndiReceiver != null)
        {
            yield return new WaitForSeconds(1f); // Check every second
            
            // Check if we're getting texture but also getting warnings
            if (cell.ndiReceiver.texture == null && !hasShownFrameSizeWarning)
            {
                // If no texture after 5 seconds, assume frame size issue
                if (Time.time - lastWarningTime > 5f)
                {
                    Debug.LogWarning($"[NDI DEBUG] Cell {cell.index + 1}: No video texture - likely frame size incompatibility");
                    Debug.LogWarning($"[NDI DEBUG] Cell {cell.index + 1}: NDI source may have incompatible resolution (height must be multiple of 8)");
                    
                    // Update UI to show incompatibility warning
                    cell.label.text = $"Camera {cell.index + 1} - Format Error";
                    
                    hasShownFrameSizeWarning = true;
                    lastWarningTime = Time.time;
                }
            }
            else if (cell.ndiReceiver.texture != null)
            {
                // We're getting texture, reset warning state
                hasShownFrameSizeWarning = false;
                var sourceName = cell.ndiReceiver.ndiName;
                cell.label.text = $"Camera {cell.index + 1} - {sourceName}";
            }
        }
        
        Debug.Log($"[NDI DEBUG] Stopped frame size monitoring for cell {cell.index + 1}");
    }
    
    private void OnDestroy()
    {
        ClearAllReceivers();
    }
}

[System.Serializable]
public class NDICameraCell
{
    public int index;
    public VisualElement cellElement;
    public VisualElement contentArea;
    public Label label;
    public NdiReceiver ndiReceiver;
    public bool isActive;
}