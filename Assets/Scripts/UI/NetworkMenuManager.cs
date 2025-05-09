using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class NetworkMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button joinAsHostButton;
    [SerializeField] private Button joinAsClientButton;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button saveUsernameButton;
    [SerializeField] private Button clearUsernameButton;

    [Header("Canvases")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject hudCanvas;

    [Header("Fusion Bootstrap")]
    private FusionBootstrap fusionBootstrap;
    
    private void Awake()
    {
        // Find the FusionBootstrap component
        fusionBootstrap = FindFirstObjectByType<FusionBootstrap>();
        if (fusionBootstrap == null)
        {
            Debug.LogError("FusionBootstrap not found in the scene!");
            return;
        }
        
        // Hook up the button events
        joinAsHostButton.onClick.AddListener(StartAsHost);
        joinAsClientButton.onClick.AddListener(StartAsClient);

        // Hook up username button events
        saveUsernameButton.onClick.AddListener(SaveUsername);
        clearUsernameButton.onClick.AddListener(ClearUsername);

        // Load saved username if exists
        LoadSavedUsername();

        if (hudCanvas != null)
        {
            hudCanvas.SetActive(false);
        }
    }

    private void SaveUsername()
    {
        if (usernameInput != null && !string.IsNullOrEmpty(usernameInput.text))
        {
            PlayerPrefs.SetString("PlayerName", usernameInput.text);
            PlayerPrefs.Save();
            Debug.Log($"Username saved: {usernameInput.text}");
        }
    }
    
    private void ClearUsername()
    {
        if (usernameInput != null)
        {
            usernameInput.text = "";
            PlayerPrefs.DeleteKey("PlayerName");
            PlayerPrefs.Save();
            Debug.Log("Username cleared");
        }
    }
    
    private void LoadSavedUsername()
    {
        if (usernameInput != null && PlayerPrefs.HasKey("PlayerName"))
        {
            usernameInput.text = PlayerPrefs.GetString("PlayerName");
            Debug.Log($"Loaded saved username: {usernameInput.text}");
        }
    }
    
    private void StartAsHost()
    {
        // Save username if needed
        if (usernameInput != null && !string.IsNullOrEmpty(usernameInput.text))
        {
            PlayerPrefs.SetString("PlayerName", usernameInput.text);
            PlayerPrefs.Save();
        }
        
        // Start as Host
        fusionBootstrap.StartHost();
        
        // Switch visibility: hide menu canvas, show HUD canvas
        ToggleUI(showHud: true);
    }
    
    private void StartAsClient()
    {
        // Save username if needed
        if (usernameInput != null && !string.IsNullOrEmpty(usernameInput.text))
        {
            PlayerPrefs.SetString("PlayerName", usernameInput.text);
            PlayerPrefs.Save();
        }
        
        // Start as Client
        fusionBootstrap.StartClient();
        
        // Switch visibility: hide menu canvas, show HUD canvas
        ToggleUI(showHud: true);
    }
    
    // New helper method to toggle UI elements
    private void ToggleUI(bool showHud)
    {
        // Hide menu
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(!showHud);
        }
        else
        {
            Debug.LogWarning("Menu Canvas reference is missing!");
        }
        
        // Show/hide HUD based on parameter
        if (hudCanvas != null)
        {
            hudCanvas.SetActive(showHud);
        }
        else
        {
            Debug.LogWarning("HUD Canvas reference is missing!");
        }
    }
}