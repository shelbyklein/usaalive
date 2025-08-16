using UnityEngine;
using UnityEngine.UIElements;
using Klak.Ndi;
using System.Linq;

public class CameraManager : MonoBehaviour
{
    [Header("NDI Configuration")]
    public NdiReceiver ndiReceiver;
    public RenderTexture targetRenderTexture;
    
    [Header("UI References")]
    public UIDocument uiDocument;
    
    private VisualElement cameraContent;
    private Label cameraLabel;
    private Label connectionStatus;
    private Label statusMessage;
    private Button connectButton;
    private Button disconnectButton;
    
    private void Start()
    {
        Debug.Log("Camera Manager starting...");
        InitializeUI();
        SetupNDIReceiver();
        UpdateConnectionStatus();
    }
    
    private void InitializeUI()
    {
        if (uiDocument == null)
        {
            uiDocument = FindObjectOfType<UIDocument>();
        }
        
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            var root = uiDocument.rootVisualElement;
            
            // Get UI elements
            cameraContent = root.Q<VisualElement>("camera-1-content");
            cameraLabel = root.Q<Label>("camera-1-label");
            connectionStatus = root.Q<Label>("connection-status");
            statusMessage = root.Q<Label>("status-message");
            connectButton = root.Q<Button>("connect-btn");
            disconnectButton = root.Q<Button>("disconnect-btn");
            
            // Setup button events
            if (connectButton != null)
            {
                connectButton.clicked += OnConnectClicked;
            }
            
            if (disconnectButton != null)
            {
                disconnectButton.clicked += OnDisconnectClicked;
            }
            
            Debug.Log("UI initialized successfully");
        }
        else
        {
            Debug.LogError("UIDocument not found or root element is null");
        }
    }
    
    private void SetupNDIReceiver()
    {
        if (ndiReceiver == null)
        {
            ndiReceiver = FindObjectOfType<NdiReceiver>();
        }
        
        if (ndiReceiver == null)
        {
            Debug.LogWarning("No NDI Receiver found in scene");
            return;
        }
        
        // Create render texture if not assigned
        if (targetRenderTexture == null)
        {
            targetRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            targetRenderTexture.name = "NDI_Camera_Output";
        }
        
        // Set the target texture for the NDI receiver
        ndiReceiver.targetTexture = targetRenderTexture;
        
        Debug.Log("NDI Receiver setup completed");
    }
    
    private void Update()
    {
        UpdateCameraFeed();
        UpdateUI();
    }
    
    private void UpdateCameraFeed()
    {
        if (ndiReceiver != null && cameraContent != null)
        {
            // Update camera content with NDI feed
            if (ndiReceiver.texture != null)
            {
                // Apply the render texture as background using Background constructor
                cameraContent.style.backgroundImage = Background.FromRenderTexture(ndiReceiver.texture);
                cameraContent.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
            else
            {
                // Clear background when no texture
                cameraContent.style.backgroundImage = StyleKeyword.None;
            }
        }
    }
    
    private void UpdateUI()
    {
        if (ndiReceiver != null)
        {
            // Update camera label
            if (cameraLabel != null)
            {
                bool hasTexture = ndiReceiver.texture != null;
                string labelText = hasTexture 
                    ? $"Camera 1 - {ndiReceiver.ndiName}" 
                    : "Camera 1 - No Signal";
                cameraLabel.text = labelText;
            }
            
            // Update connection status
            UpdateConnectionStatus();
        }
    }
    
    private void UpdateConnectionStatus()
    {
        if (connectionStatus != null && ndiReceiver != null)
        {
            bool hasTexture = ndiReceiver.texture != null;
            string status = hasTexture ? "Connected" : "Disconnected";
            connectionStatus.text = $"Status: {status}";
        }
        
        if (statusMessage != null && ndiReceiver != null)
        {
            bool hasTexture = ndiReceiver.texture != null;
            if (hasTexture)
            {
                statusMessage.text = $"Receiving: {ndiReceiver.ndiName}";
            }
            else
            {
                statusMessage.text = "Ready - No NDI source connected";
            }
        }
    }
    
    private void OnConnectClicked()
    {
        Debug.Log("Connect button clicked");
        
        if (ndiReceiver != null)
        {
            // Try to connect to the first available NDI source
            var sources = NdiFinder.sourceNames.ToArray();
            if (sources.Length > 0)
            {
                string sourceName = sources[0];
                ndiReceiver.ndiName = sourceName;
                
                Debug.Log($"Attempting to connect to NDI source: {sourceName}");
                
                if (statusMessage != null)
                {
                    statusMessage.text = $"Connecting to {sourceName}...";
                }
            }
            else
            {
                Debug.LogWarning("No NDI sources found");
                if (statusMessage != null)
                {
                    statusMessage.text = "No NDI sources available";
                }
            }
        }
    }
    
    private void OnDisconnectClicked()
    {
        Debug.Log("Disconnect button clicked");
        
        if (ndiReceiver != null)
        {
            ndiReceiver.ndiName = "";
            
            if (statusMessage != null)
            {
                statusMessage.text = "Disconnected";
            }
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup
        if (connectButton != null)
        {
            connectButton.clicked -= OnConnectClicked;
        }
        
        if (disconnectButton != null)
        {
            disconnectButton.clicked -= OnDisconnectClicked;
        }
        
        if (targetRenderTexture != null)
        {
            targetRenderTexture.Release();
        }
    }
}