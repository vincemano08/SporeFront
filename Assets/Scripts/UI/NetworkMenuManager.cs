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

    private const string PLAYER_NAME_KEY = "PlayerName";

    private void Awake()
    {
        // Find the FusionBootstrap component
        if (fusionBootstrap == null)
        {
            // Try to find it as fallback
            fusionBootstrap = FindFirstObjectByType<FusionBootstrap>();
            if (fusionBootstrap == null)
            {
                Debug.LogError("FusionBootstrap not found in the scene!");
                return;
            }
        }

        // Hook up the button events

        if (joinAsHostButton != null)
            joinAsHostButton.onClick.AddListener(StartAsHost);
        else
            Debug.LogWarning("Join As Host Button reference is missing!");

        if (joinAsClientButton != null)
            joinAsClientButton.onClick.AddListener(StartAsClient);
        else
            Debug.LogWarning("Join As Client Button reference is missing!");

        // Hook up username button events
        if (saveUsernameButton != null)
            saveUsernameButton.onClick.AddListener(SaveUsername);
        else
            Debug.LogWarning("Save Username Button reference is missing!");

        if (clearUsernameButton != null)
            clearUsernameButton.onClick.AddListener(ClearUsername);
        else
            Debug.LogWarning("Clear Username Button reference is missing!");

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
            PlayerPrefs.SetString(PLAYER_NAME_KEY, usernameInput.text);
            PlayerPrefs.Save();
            Debug.Log($"Username saved: {usernameInput.text}");
        }
    }

    private void ClearUsername()
    {
        if (usernameInput != null)
        {
            usernameInput.text = "";
            PlayerPrefs.DeleteKey(PLAYER_NAME_KEY);
            PlayerPrefs.Save();
            Debug.Log("Username cleared");
        }
    }

    private void LoadSavedUsername()
    {
        if (usernameInput != null && PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            usernameInput.text = PlayerPrefs.GetString(PLAYER_NAME_KEY);
            Debug.Log($"Loaded saved username: {usernameInput.text}");
        }
    }

    private void StartAsHost()
    {
        // Save username if needed
        SaveUsername();

        // Start as Host
        fusionBootstrap.StartHost();

        HideMenuCanvas();
    }

    private void StartAsClient()
    {
        // Save username if needed
        SaveUsername();

        // Start as Client
        fusionBootstrap.StartClient();

        HideMenuCanvas();
    }

    private void HideMenuCanvas()
    {
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Menu Canvas reference is missing!");
        }
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