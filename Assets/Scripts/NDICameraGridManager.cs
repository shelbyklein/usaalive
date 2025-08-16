using UnityEngine;
using UnityEngine.UIElements;
using Klak.Ndi;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class NDICameraGridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private int maxCameras = 6;
    [SerializeField] private int gridColumns = 2;
    [SerializeField] private int gridRows = 2;
    
    [Header("Video Configuration")]
    [SerializeField] private int targetWidth = 1920;
    [SerializeField] private int targetHeight = 1080;
    
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Video Display")]
    [SerializeField] private Material videoMaterial;
    [SerializeField] private Camera displayCamera;
    
    [Header("NDI Resources")]
    [SerializeField] private NdiResources ndiResources;
    
    private VisualElement gridContainer;
    private List<NDICameraCell> cameraCells = new List<NDICameraCell>();
    private List<string> lastKnownSources = new List<string>();
    
    // 2D Display variables
    private Vector2 displayBounds = new Vector2(16f, 9f); // 16:9 aspect ratio in camera units
    
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
        
        InitializeDisplay();
        // InitializeUI(); // Disabled UI for now to focus on 3D video display
        
        // Immediate NDI source check
        var sources = NdiFinder.sourceNames.ToList();
        Debug.Log($"Immediate NDI source check found {sources.Count} sources:");
        foreach (var source in sources)
        {
            Debug.Log($"  - {source}");
        }
        
        StartCoroutine(NDISourceDetectionLoop());
    }
    
    private void InitializeDisplay()
    {
        // Setup display camera if not assigned
        if (displayCamera == null)
        {
            displayCamera = Camera.main;
            if (displayCamera == null)
            {
                displayCamera = FindObjectOfType<Camera>();
            }
        }
        
        // Ensure camera is orthographic for 2D display
        if (displayCamera != null)
        {
            displayCamera.orthographic = true;
            displayCamera.orthographicSize = displayBounds.y / 2f; // Half height (4.5 units)
            
            // Position camera to look at the display objects
            displayCamera.transform.position = new Vector3(0, 0, -10f);
            displayCamera.transform.rotation = Quaternion.identity;
            
            // Ensure camera clears with a solid color
            displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = Color.black;
            
            Debug.Log($"Display camera configured: orthographic size = {displayCamera.orthographicSize}, position = {displayCamera.transform.position}");
        }
        
        // Load video material if not assigned
        if (videoMaterial == null)
        {
            videoMaterial = Resources.Load<Material>("NDI_VideoMaterial");
            if (videoMaterial == null)
            {
                Debug.LogWarning("NDI Video Material not found. Video display may not work correctly.");
            }
        }
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
        SetupDynamicGridLayout(0); // Initialize with empty grid
    }
    
    private void SetupDynamicGridLayout(int sourceCount)
    {
        // Clear existing camera objects and UI
        ClearExistingDisplayObjects();
        
        // Configure grid layout based on source count
        int rows, cols;
        GetOptimalGridLayout(sourceCount, out rows, out cols);
        
        if (sourceCount == 0)
        {
            // Show empty state in UI only
            if (gridContainer != null)
            {
                var emptyLabel = new Label("No NDI sources detected");
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.fontSize = 18;
                emptyLabel.style.color = Color.gray;
                emptyLabel.style.flexGrow = 1;
                gridContainer.Add(emptyLabel);
            }
            return;
        }
        
        // Create 2D GameObjects for video display
        CreateVideoDisplayObjects(rows, cols, sourceCount);
        
        Debug.Log($"Dynamic 2D grid layout created: {rows}x{cols} for {sourceCount} sources");
    }
    
    private void ClearExistingDisplayObjects()
    {
        // Clear UI
        if (gridContainer != null)
        {
            gridContainer.Clear();
        }
        
        // Destroy existing GameObjects
        foreach (var cell in cameraCells)
        {
            if (cell.displayGameObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(cell.displayGameObject);
                }
                else
                {
                    DestroyImmediate(cell.displayGameObject);
                }
            }
        }
        
        cameraCells.Clear();
    }
    
    private void CreateVideoDisplayObjects(int rows, int cols, int sourceCount)
    {
        // Calculate cell dimensions maintaining 16:9 aspect ratio
        Vector2 availableSize = new Vector2(displayBounds.x / cols, displayBounds.y / rows);
        
        // Calculate actual cell size maintaining 16:9 aspect ratio
        float targetAspectRatio = 16f / 9f;
        Vector2 cellSize;
        
        if (availableSize.x / availableSize.y > targetAspectRatio)
        {
            // Available space is wider than 16:9, constrain by height
            cellSize.y = availableSize.y * 0.9f; // Add small margin
            cellSize.x = cellSize.y * targetAspectRatio;
        }
        else
        {
            // Available space is taller than 16:9, constrain by width
            cellSize.x = availableSize.x * 0.9f; // Add small margin
            cellSize.y = cellSize.x / targetAspectRatio;
        }
        
        // Calculate spacing between cells
        Vector2 cellSpacing = new Vector2(displayBounds.x / cols, displayBounds.y / rows);
        Vector2 startPos = new Vector2(-displayBounds.x / 2f + cellSpacing.x / 2f, displayBounds.y / 2f - cellSpacing.y / 2f);
        
        int cellIndex = 0;
        for (int row = 0; row < rows && cellIndex < sourceCount; row++)
        {
            for (int col = 0; col < cols && cellIndex < sourceCount; col++)
            {
                Vector3 position = new Vector3(
                    startPos.x + col * cellSpacing.x,
                    startPos.y - row * cellSpacing.y,
                    0f
                );
                
                CreateVideoDisplayObject(cellIndex, position, cellSize);
                cellIndex++;
            }
        }
    }
    
    private void GetOptimalGridLayout(int sourceCount, out int rows, out int cols)
    {
        switch (sourceCount)
        {
            case 0:
                rows = 0; cols = 0;
                break;
            case 1:
                rows = 1; cols = 1;
                break;
            case 2:
                rows = 1; cols = 2; // Side by side
                break;
            case 3:
            case 4:
                rows = 2; cols = 2; // 2x2 grid
                break;
            case 5:
            case 6:
                rows = 2; cols = 3; // 3x2 grid
                break;
            default:
                // For more than 6, still use 3x2 and show first 6
                rows = 2; cols = 3;
                break;
        }
    }
    
    private void CreateVideoDisplayObject(int index, Vector3 position, Vector2 size)
    {
        // Create the main GameObject
        var displayObj = new GameObject($"NDI_Display_{index + 1}");
        displayObj.transform.SetParent(transform);
        displayObj.transform.position = position;
        
        // Add MeshFilter with Quad mesh
        var meshFilter = displayObj.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateQuadMesh(size);
        
        // Add MeshRenderer with video material
        var meshRenderer = displayObj.AddComponent<MeshRenderer>();
        if (videoMaterial != null)
        {
            meshRenderer.material = new Material(videoMaterial); // Create instance
            Debug.Log($"Applied material '{videoMaterial.name}' with shader '{videoMaterial.shader.name}' to display object {index + 1}");
        }
        else
        {
            Debug.LogError("Video material is null! Creating fallback material.");
            // Create a simple unlit material as fallback
            var fallbackMaterial = new Material(Shader.Find("Unlit/Texture"));
            meshRenderer.material = fallbackMaterial;
        }
        
        // Set sorting layer for 2D rendering
        meshRenderer.sortingLayerName = "Default";
        meshRenderer.sortingOrder = 0;
        
        // Create NDI camera cell data
        var cameraCell = new NDICameraCell
        {
            index = index,
            displayGameObject = displayObj,
            meshRenderer = meshRenderer,
            ndiReceiver = null,
            isActive = false
        };
        
        cameraCells.Add(cameraCell);
        
        // Debug.Log($"Created video display object {index + 1} at position {position} with size {size}");
    }
    
    private Mesh CreateQuadMesh(Vector2 size)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Video Quad";
        
        // Vertices for a quad centered at origin
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-size.x / 2, -size.y / 2, 0); // Bottom left
        vertices[1] = new Vector3(size.x / 2, -size.y / 2, 0);  // Bottom right
        vertices[2] = new Vector3(-size.x / 2, size.y / 2, 0);  // Top left
        vertices[3] = new Vector3(size.x / 2, size.y / 2, 0);   // Top right
        
        // UV coordinates
        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 0); // Bottom left
        uv[1] = new Vector2(1, 0); // Bottom right
        uv[2] = new Vector2(0, 1); // Top left
        uv[3] = new Vector2(1, 1); // Top right
        
        // Triangles (two triangles make a quad)
        int[] triangles = new int[6];
        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1; // First triangle
        triangles[3] = 2; triangles[4] = 3; triangles[5] = 1; // Second triangle
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private IEnumerator NDISourceDetectionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f); // Check every 2 seconds
            
            try
            {
                var currentSources = NdiFinder.sourceNames.ToList();
                
                // Check if sources have changed
                if (!currentSources.SequenceEqual(lastKnownSources))
                {
                    Debug.Log($"NDI sources changed. Found {currentSources.Count} sources:");
                    foreach (var source in currentSources)
                    {
                        Debug.Log($"  - {source}");
                    }
                    
                    UpdateCameraGrid(currentSources);
                    lastKnownSources = new List<string>(currentSources);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during NDI source detection: {e.Message}");
            }
        }
    }
    
    private void UpdateCameraGrid(List<string> sources)
    {
        // First, clear all existing receivers
        ClearAllReceivers();
        
        // Clear the current grid and rebuild it based on source count
        int sourceCount = Mathf.Min(sources.Count, maxCameras);
        SetupDynamicGridLayout(sourceCount);
        
        // Assign sources to available camera cells
        for (int i = 0; i < sourceCount; i++)
        {
            AssignSourceToCell(i, sources[i]);
        }
    }
    
    private void ClearAllReceivers()
    {
        foreach (var cell in cameraCells)
        {
            if (cell.ndiReceiver != null)
            {
                // NDI receiver is on the display GameObject, so we destroy it with the GameObject
                cell.ndiReceiver = null;
            }
            cell.isActive = false;
        }
    }
    
    private void AssignSourceToCell(int cellIndex, string sourceName)
    {
        if (cellIndex >= cameraCells.Count)
        {
            Debug.LogError($"Cell index {cellIndex} out of range (max: {cameraCells.Count - 1})");
            return;
        }
        
        var cell = cameraCells[cellIndex];
        
        // Create NDI Receiver on the display GameObject
        var receiver = cell.displayGameObject.AddComponent<NdiReceiver>();
        
        if (ndiResources != null)
        {
            receiver.SetResources(ndiResources);
        }
        else
        {
            Debug.LogWarning($"NDI Resources is null - receiver may not work properly");
        }
        
        // Configure receiver to use MeshRenderer instead of RenderTexture
        receiver.targetRenderer = cell.meshRenderer;
        receiver.targetMaterialProperty = "_MainTex";
        receiver.ndiName = sourceName;
        
        cell.ndiReceiver = receiver;
        cell.isActive = true;
        
        // Start monitoring for frame size issues
        StartCoroutine(MonitorFrameSizeIssues(cell));
        
        Debug.Log($"Assigned NDI source '{sourceName}' to MeshRenderer on display object {cellIndex + 1}");
    }
    
    
    private IEnumerator MonitorFrameSizeIssues(NDICameraCell cell)
    {
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
                    Debug.LogWarning($"Display {cell.index + 1}: No video texture - likely frame size incompatibility");
                    hasShownFrameSizeWarning = true;
                    lastWarningTime = Time.time;
                }
            }
            else if (cell.ndiReceiver.texture != null)
            {
                // We're getting texture, reset warning state
                hasShownFrameSizeWarning = false;
                // Debug.Log($"Display {cell.index + 1}: Receiving video from {cell.ndiReceiver.ndiName}");
            }
        }
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
    public GameObject displayGameObject;
    public MeshRenderer meshRenderer;
    public NdiReceiver ndiReceiver;
    public bool isActive;
}