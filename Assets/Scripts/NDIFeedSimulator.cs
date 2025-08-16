using UnityEngine;
using System.Collections;

/// <summary>
/// Simulates an NDI feed by creating animated textures and applying them to a MeshRenderer
/// Useful for testing the NDI display system without requiring actual NDI sources
/// </summary>
public class NDIFeedSimulator : MonoBehaviour
{
    [Header("Simulation Settings")]
    [SerializeField] private bool enableSimulation = true;
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 1080;
    [SerializeField] private float updateRate = 30f; // FPS
    
    [Header("Pattern Settings")]
    [SerializeField] private SimulationPattern pattern = SimulationPattern.MovingBars;
    [SerializeField] private Color primaryColor = Color.cyan;
    [SerializeField] private Color secondaryColor = Color.magenta;
    [SerializeField] private float animationSpeed = 1f;
    
    [Header("Text Overlay")]
    [SerializeField] private bool showTextOverlay = true;
    [SerializeField] private string simulationText = "SIMULATED NDI FEED";
    [SerializeField] private int fontSize = 72;
    
    private RenderTexture simulationTexture;
    private Material simulationMaterial;
    private MeshRenderer targetRenderer;
    private float animationTime = 0f;
    private Coroutine simulationCoroutine;
    
    public enum SimulationPattern
    {
        MovingBars,
        ColorCycle,
        CheckerBoard,
        Gradient,
        TestPattern,
        StaticNoise
    }
    
    private void Start()
    {
        if (enableSimulation)
        {
            InitializeSimulation();
        }
    }
    
    private void InitializeSimulation()
    {
        // Get or create target renderer
        targetRenderer = GetComponent<MeshRenderer>();
        if (targetRenderer == null)
        {
            Debug.LogError("NDIFeedSimulator requires a MeshRenderer component!");
            return;
        }
        
        // Create simulation texture
        simulationTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
        simulationTexture.name = "NDI_Simulation_Texture";
        simulationTexture.Create();
        
        // Create material for simulation
        simulationMaterial = new Material(Shader.Find("Unlit/Texture"));
        simulationMaterial.mainTexture = simulationTexture;
        targetRenderer.material = simulationMaterial;
        
        // Start simulation coroutine
        if (simulationCoroutine != null)
        {
            StopCoroutine(simulationCoroutine);
        }
        simulationCoroutine = StartCoroutine(SimulationLoop());
        
        Debug.Log($"NDI Feed Simulator initialized: {textureWidth}x{textureHeight} @ {updateRate}fps");
    }
    
    private IEnumerator SimulationLoop()
    {
        while (enableSimulation && simulationTexture != null)
        {
            UpdateSimulationTexture();
            animationTime += Time.deltaTime * animationSpeed;
            
            yield return new WaitForSeconds(1f / updateRate);
        }
    }
    
    private void UpdateSimulationTexture()
    {
        // Set the simulation texture as active render target
        RenderTexture.active = simulationTexture;
        
        // Clear with background color
        GL.Clear(true, true, Color.black);
        
        // Create a temporary texture to draw on
        Texture2D tempTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        Color[] pixels = new Color[textureWidth * textureHeight];
        
        // Generate pattern based on selected type
        switch (pattern)
        {
            case SimulationPattern.MovingBars:
                GenerateMovingBars(pixels);
                break;
            case SimulationPattern.ColorCycle:
                GenerateColorCycle(pixels);
                break;
            case SimulationPattern.CheckerBoard:
                GenerateCheckerBoard(pixels);
                break;
            case SimulationPattern.Gradient:
                GenerateGradient(pixels);
                break;
            case SimulationPattern.TestPattern:
                GenerateTestPattern(pixels);
                break;
            case SimulationPattern.StaticNoise:
                GenerateStaticNoise(pixels);
                break;
        }
        
        // Apply pixels to texture
        tempTexture.SetPixels(pixels);
        tempTexture.Apply();
        
        // Blit to render texture
        Graphics.Blit(tempTexture, simulationTexture);
        
        // Add text overlay if enabled
        if (showTextOverlay)
        {
            DrawTextOverlay();
        }
        
        // Cleanup
        DestroyImmediate(tempTexture);
        RenderTexture.active = null;
    }
    
    private void GenerateMovingBars(Color[] pixels)
    {
        int barWidth = textureWidth / 8;
        float offset = (animationTime * 100) % (barWidth * 2);
        
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                int adjustedX = (int)(x + offset) % (barWidth * 2);
                Color color = adjustedX < barWidth ? primaryColor : secondaryColor;
                pixels[y * textureWidth + x] = color;
            }
        }
    }
    
    private void GenerateColorCycle(Color[] pixels)
    {
        float hue = (animationTime * 0.1f) % 1f;
        Color cycleColor = Color.HSVToRGB(hue, 1f, 1f);
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = cycleColor;
        }
    }
    
    private void GenerateCheckerBoard(Color[] pixels)
    {
        int checkerSize = 64;
        bool animated = true;
        float offset = animated ? (animationTime * 20) % (checkerSize * 2) : 0;
        
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                int adjustedX = (int)(x + offset);
                bool checker = ((adjustedX / checkerSize) + (y / checkerSize)) % 2 == 0;
                Color color = checker ? primaryColor : secondaryColor;
                pixels[y * textureWidth + x] = color;
            }
        }
    }
    
    private void GenerateGradient(Color[] pixels)
    {
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float t = (float)x / textureWidth;
                t += Mathf.Sin(animationTime) * 0.1f; // Animate the gradient
                t = Mathf.Clamp01(t);
                Color color = Color.Lerp(primaryColor, secondaryColor, t);
                pixels[y * textureWidth + x] = color;
            }
        }
    }
    
    private void GenerateTestPattern(Color[] pixels)
    {
        // Classic TV test pattern with color bars
        Color[] testColors = {
            Color.white, Color.yellow, Color.cyan, Color.green,
            Color.magenta, Color.red, Color.blue, Color.black
        };
        
        int barWidth = textureWidth / testColors.Length;
        
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                int colorIndex = x / barWidth;
                if (colorIndex >= testColors.Length) colorIndex = testColors.Length - 1;
                
                Color color = testColors[colorIndex];
                // Add some animation
                color *= (1f + Mathf.Sin(animationTime * 2f) * 0.1f);
                pixels[y * textureWidth + x] = color;
            }
        }
    }
    
    private void GenerateStaticNoise(Color[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            float noise = Random.Range(0f, 1f);
            pixels[i] = new Color(noise, noise, noise, 1f);
        }
    }
    
    private void DrawTextOverlay()
    {
        // Note: This is a simplified text overlay
        // For more advanced text rendering, consider using Unity's TextMeshPro or UI system
        // This creates a simple colored rectangle where text would be
        
        if (showTextOverlay && !string.IsNullOrEmpty(simulationText))
        {
            // Create a simple text background rectangle
            int textWidth = simulationText.Length * (fontSize / 4);
            int textHeight = fontSize;
            int x = (textureWidth - textWidth) / 2;
            int y = 50; // Top margin
            
            // Draw text background (simplified)
            Graphics.DrawTexture(
                new Rect(x - 10, y - 5, textWidth + 20, textHeight + 10),
                Texture2D.blackTexture
            );
        }
    }
    
    public void SetPattern(SimulationPattern newPattern)
    {
        pattern = newPattern;
        Debug.Log($"NDI Simulator pattern changed to: {newPattern}");
    }
    
    public void SetColors(Color primary, Color secondary)
    {
        primaryColor = primary;
        secondaryColor = secondary;
    }
    
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }
    
    public void ToggleSimulation()
    {
        enableSimulation = !enableSimulation;
        
        if (enableSimulation)
        {
            InitializeSimulation();
        }
        else if (simulationCoroutine != null)
        {
            StopCoroutine(simulationCoroutine);
        }
    }
    
    private void OnDestroy()
    {
        if (simulationCoroutine != null)
        {
            StopCoroutine(simulationCoroutine);
        }
        
        if (simulationTexture != null)
        {
            simulationTexture.Release();
            if (Application.isPlaying)
            {
                Destroy(simulationTexture);
            }
        }
        
        if (simulationMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(simulationMaterial);
            }
        }
    }
    
    private void OnValidate()
    {
        // Clamp values to reasonable ranges
        textureWidth = Mathf.Clamp(textureWidth, 64, 3840);
        textureHeight = Mathf.Clamp(textureHeight, 64, 2160);
        updateRate = Mathf.Clamp(updateRate, 1f, 120f);
        animationSpeed = Mathf.Clamp(animationSpeed, 0f, 10f);
        fontSize = Mathf.Clamp(fontSize, 12, 200);
    }
}