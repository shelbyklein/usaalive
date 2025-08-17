using UnityEngine;
using UnityEngine.InputSystem;

public class CameraInputManager : MonoBehaviour
{
    [Header("Input Configuration")]
    [SerializeField] private InputActionAsset cameraControlsAsset;
    
    [Header("Camera Manager Reference")]
    [SerializeField] private NDICameraGridManager cameraGridManager;
    
    private InputActionMap cameraSelectionMap;
    private InputAction selectCamera1Action;
    private InputAction selectCamera2Action;
    private InputAction selectCamera3Action;
    private InputAction selectCamera4Action;
    private InputAction selectCamera5Action;
    private InputAction selectCamera6Action;
    
    private int currentlySelectedCamera = -1;
    
    private void Awake()
    {
        InitializeInputActions();
    }
    
    private void OnEnable()
    {
        EnableInputActions();
    }
    
    private void OnDisable()
    {
        DisableInputActions();
    }
    
    private void InitializeInputActions()
    {
        if (cameraControlsAsset == null)
        {
            Debug.LogError("Camera Controls Input Action Asset not assigned!");
            return;
        }
        
        cameraSelectionMap = cameraControlsAsset.FindActionMap("CameraSelection");
        
        if (cameraSelectionMap == null)
        {
            Debug.LogError("CameraSelection action map not found in Input Action Asset!");
            return;
        }
        
        selectCamera1Action = cameraSelectionMap.FindAction("SelectCamera1");
        selectCamera2Action = cameraSelectionMap.FindAction("SelectCamera2");
        selectCamera3Action = cameraSelectionMap.FindAction("SelectCamera3");
        selectCamera4Action = cameraSelectionMap.FindAction("SelectCamera4");
        selectCamera5Action = cameraSelectionMap.FindAction("SelectCamera5");
        selectCamera6Action = cameraSelectionMap.FindAction("SelectCamera6");
        
        SetupActionCallbacks();
    }
    
    private void SetupActionCallbacks()
    {
        // Fixed offset: Key 1 selects camera 0, Key 2 selects camera 1, etc.
        // This fixes the "selecting camera 3 controls camera 2" issue
        if (selectCamera1Action != null)
            selectCamera1Action.performed += ctx => SelectCamera(0); // Key 1 → Camera 1 (index 0)
            
        if (selectCamera2Action != null)
            selectCamera2Action.performed += ctx => SelectCamera(1); // Key 2 → Camera 2 (index 1)
            
        if (selectCamera3Action != null)
            selectCamera3Action.performed += ctx => SelectCamera(2); // Key 3 → Camera 3 (index 2)
            
        if (selectCamera4Action != null)
            selectCamera4Action.performed += ctx => SelectCamera(3); // Key 4 → Camera 4 (index 3)
            
        if (selectCamera5Action != null)
            selectCamera5Action.performed += ctx => SelectCamera(4); // Key 5 → Camera 5 (index 4)
            
        if (selectCamera6Action != null)
            selectCamera6Action.performed += ctx => SelectCamera(5); // Key 6 → Camera 6 (index 5)
    }
    
    private void EnableInputActions()
    {
        cameraSelectionMap?.Enable();
    }
    
    private void DisableInputActions()
    {
        cameraSelectionMap?.Disable();
    }
    
    private void SelectCamera(int cameraIndex)
    {
        if (cameraGridManager == null)
        {
            Debug.LogWarning("Camera Grid Manager not assigned!");
            return;
        }
        
        int previousCamera = currentlySelectedCamera;
        currentlySelectedCamera = cameraIndex;
        
        if (previousCamera != cameraIndex)
        {
            Debug.Log($"[INPUT] Camera {cameraIndex + 1} SELECTED - Now active for control");
            if (previousCamera >= 0)
            {
                Debug.Log($"[INPUT] Camera {previousCamera + 1} deselected");
            }
        }
        else
        {
            Debug.Log($"[INPUT] Camera {cameraIndex + 1} already selected - confirming selection");
        }
        
        OnCameraSelected(cameraIndex);
    }
    
    private void OnCameraSelected(int cameraIndex)
    {
        Debug.Log($"[CAMERA] Camera {cameraIndex + 1} is now ACTIVE and ready for PTZ control");
        Debug.Log($"[SYSTEM] Selected camera index: {cameraIndex} (0-based), Display: Camera {cameraIndex + 1}");
        
        // Update visual border feedback
        if (cameraGridManager != null)
        {
            cameraGridManager.SetSingleCameraSelection(cameraIndex);
        }
        else
        {
            Debug.LogWarning("[BORDER] Cannot update camera borders - NDICameraGridManager not found");
        }
        
        // Update VISCA controller with selected camera
        var viscaController = FindObjectOfType<VISCAController>();
        if (viscaController != null)
        {
            viscaController.SetSelectedCamera(cameraIndex);
            Debug.Log($"[VISCA] Camera {cameraIndex + 1} selected for PTZ control");
        }
        else
        {
            Debug.LogWarning("[VISCA] VISCAController not found - PTZ control unavailable");
        }
    }
    
    public int GetCurrentlySelectedCamera()
    {
        return currentlySelectedCamera;
    }
    
    public bool IsCameraSelected()
    {
        return currentlySelectedCamera >= 0;
    }
    
    private void Start()
    {
        // Auto-find the camera grid manager if not assigned
        if (cameraGridManager == null)
        {
            cameraGridManager = FindObjectOfType<NDICameraGridManager>();
            if (cameraGridManager == null)
            {
                Debug.LogWarning("No NDICameraGridManager found in scene. Camera selection may not work properly.");
            }
            else
            {
                Debug.Log("[INPUT] Camera Grid Manager found and connected");
            }
        }
        
        Debug.Log("[INPUT] Camera Input System initialized - Press keys 1-6 to select cameras");
        Debug.Log("[INPUT] Camera selection ready - No camera currently selected");
    }
}