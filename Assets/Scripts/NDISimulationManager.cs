using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages multiple simulated NDI feeds for testing the camera grid system
/// Creates multiple feed simulators with different patterns and colors
/// </summary>
public class NDISimulationManager : MonoBehaviour
{
    [Header("Simulation Control")]
    [SerializeField] private bool enableSimulation = true;
    [SerializeField] private int numberOfFeeds = 1;
    [SerializeField] private Vector2 feedSpacing = new Vector2(2f, 2f);
    
    [Header("Feed Configuration")]
    [SerializeField] private Vector2 feedSize = new Vector2(8f, 4.5f); // 16:9 aspect ratio
    [SerializeField] private Material videoMaterial;
    
    [Header("Pattern Settings")]
    [SerializeField] private bool randomizePatterns = true;
    [SerializeField] private bool randomizeColors = true;
    
    private List<GameObject> simulatedFeeds = new List<GameObject>();
    private List<NDIFeedSimulator> feedSimulators = new List<NDIFeedSimulator>();
    
    // Predefined color pairs for different feeds
    private Color[][] colorPairs = new Color[][]
    {
        new Color[] { Color.cyan, Color.magenta },
        new Color[] { Color.red, Color.blue },
        new Color[] { Color.green, Color.yellow },
        new Color[] { Color.white, Color.black },
        new Color[] { new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f) }, // Orange/Purple
        new Color[] { new Color(0f, 1f, 0.5f), new Color(1f, 0f, 0.5f) }  // Mint/Pink
    };
    
    private void Start()
    {
        if (enableSimulation)
        {
            CreateSimulatedFeeds();
        }
    }
    
    private void CreateSimulatedFeeds()
    {
        ClearExistingFeeds();
        
        // Load video material if not assigned
        if (videoMaterial == null)
        {
            videoMaterial = Resources.Load<Material>("NDI_VideoMaterial");
            if (videoMaterial == null)
            {
                Debug.LogWarning("Video material not found. Creating default material.");
                videoMaterial = new Material(Shader.Find("Unlit/Texture"));
            }
        }
        
        for (int i = 0; i < numberOfFeeds; i++)
        {
            CreateSimulatedFeed(i);
        }
        
        Debug.Log($"Created {numberOfFeeds} simulated NDI feeds");
    }
    
    private void CreateSimulatedFeed(int index)
    {
        // Calculate position
        Vector3 position = CalculateFeedPosition(index);
        
        // Create GameObject
        GameObject feedObj = new GameObject($"Simulated_NDI_Feed_{index + 1}");
        feedObj.transform.SetParent(transform);
        feedObj.transform.position = position;
        
        // Add MeshFilter with Quad mesh
        MeshFilter meshFilter = feedObj.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateQuadMesh(feedSize);
        
        // Add MeshRenderer
        MeshRenderer meshRenderer = feedObj.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(videoMaterial);
        
        // Set sorting for 2D rendering
        meshRenderer.sortingLayerName = "Default";
        meshRenderer.sortingOrder = 0;
        
        // Add NDIFeedSimulator component
        NDIFeedSimulator simulator = feedObj.AddComponent<NDIFeedSimulator>();
        ConfigureSimulator(simulator, index);
        
        // Store references
        simulatedFeeds.Add(feedObj);
        feedSimulators.Add(simulator);
        
        Debug.Log($"Created simulated feed {index + 1} at position {position}");
    }
    
    private Vector3 CalculateFeedPosition(int index)
    {
        // Arrange feeds in a grid pattern
        int cols = Mathf.CeilToInt(Mathf.Sqrt(numberOfFeeds));
        int row = index / cols;
        int col = index % cols;
        
        // Center the grid
        float startX = -(cols - 1) * feedSpacing.x / 2f;
        float startY = (Mathf.CeilToInt((float)numberOfFeeds / cols) - 1) * feedSpacing.y / 2f;
        
        return new Vector3(
            startX + col * feedSpacing.x,
            startY - row * feedSpacing.y,
            0f
        );
    }
    
    private void ConfigureSimulator(NDIFeedSimulator simulator, int index)
    {
        // Set different patterns for each feed
        if (randomizePatterns)
        {
            NDIFeedSimulator.SimulationPattern[] patterns = System.Enum.GetValues(typeof(NDIFeedSimulator.SimulationPattern)) as NDIFeedSimulator.SimulationPattern[];
            simulator.SetPattern(patterns[index % patterns.Length]);
        }
        
        // Set different colors for each feed
        if (randomizeColors && index < colorPairs.Length)
        {
            simulator.SetColors(colorPairs[index][0], colorPairs[index][1]);
        }
        
        // Vary animation speed slightly
        simulator.SetAnimationSpeed(1f + (index * 0.2f));
    }
    
    private Mesh CreateQuadMesh(Vector2 size)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Simulated Feed Quad";
        
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
        
        // Triangles
        int[] triangles = new int[6];
        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 2; triangles[4] = 3; triangles[5] = 1;
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private void ClearExistingFeeds()
    {
        foreach (GameObject feed in simulatedFeeds)
        {
            if (feed != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(feed);
                }
                else
                {
                    DestroyImmediate(feed);
                }
            }
        }
        
        simulatedFeeds.Clear();
        feedSimulators.Clear();
    }
    
    [System.Serializable]
    public class FeedConfiguration
    {
        public NDIFeedSimulator.SimulationPattern pattern;
        public Color primaryColor = Color.cyan;
        public Color secondaryColor = Color.magenta;
        public float animationSpeed = 1f;
        public string displayText = "FEED";
    }
    
    // Public methods for runtime control
    public void SetNumberOfFeeds(int count)
    {
        numberOfFeeds = Mathf.Clamp(count, 1, 16);
        if (enableSimulation && Application.isPlaying)
        {
            CreateSimulatedFeeds();
        }
    }
    
    public void ToggleSimulation()
    {
        enableSimulation = !enableSimulation;
        
        if (enableSimulation)
        {
            CreateSimulatedFeeds();
        }
        else
        {
            ClearExistingFeeds();
        }
    }
    
    public void ChangePattern(int feedIndex, NDIFeedSimulator.SimulationPattern pattern)
    {
        if (feedIndex >= 0 && feedIndex < feedSimulators.Count)
        {
            feedSimulators[feedIndex].SetPattern(pattern);
        }
    }
    
    public void ChangeColors(int feedIndex, Color primary, Color secondary)
    {
        if (feedIndex >= 0 && feedIndex < feedSimulators.Count)
        {
            feedSimulators[feedIndex].SetColors(primary, secondary);
        }
    }
    
    public void CyclePatterns()
    {
        for (int i = 0; i < feedSimulators.Count; i++)
        {
            NDIFeedSimulator.SimulationPattern[] patterns = System.Enum.GetValues(typeof(NDIFeedSimulator.SimulationPattern)) as NDIFeedSimulator.SimulationPattern[];
            int nextPattern = (i + 1) % patterns.Length;
            feedSimulators[i].SetPattern(patterns[nextPattern]);
        }
    }
    
    private void OnDestroy()
    {
        ClearExistingFeeds();
    }
    
    private void OnValidate()
    {
        numberOfFeeds = Mathf.Clamp(numberOfFeeds, 1, 16);
        feedSize.x = Mathf.Max(feedSize.x, 0.1f);
        feedSize.y = Mathf.Max(feedSize.y, 0.1f);
        feedSpacing.x = Mathf.Max(feedSpacing.x, 0.1f);
        feedSpacing.y = Mathf.Max(feedSpacing.y, 0.1f);
    }
}