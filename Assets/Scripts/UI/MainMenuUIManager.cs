using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.SceneManagement;


public class MainMenuManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbySelectPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject colorPickerPanel;


    [Header("Main Menu Panel")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private Button saveUsernameButton;
    [SerializeField] private Button clearUsernameButton;
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button findGamesButton;

    [Header("Lobby Select Panel")]
    [SerializeField] private Transform sessionListContainer;
    [SerializeField] private GameObject sessionListItemPrefab;
    [SerializeField] private Button backToMainMenuButton;

    [Header("Lobby Panel")]
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private Transform playerSlotsContainer;
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Button hostStartGameButton;
    [SerializeField] private Button clientReadyUpButton;
    [SerializeField] private Button leaveLobbyButton;

    [Header("Color Picker")]
    [SerializeField] private Transform colorOptionsContainer;
    [SerializeField] private GameObject colorOptionPrefab;
    [Header("Color Picker Sprites")]
    [SerializeField] private Sprite purpleMushroomSprite;
    [SerializeField] private Sprite blackMushroomSprite;
    [SerializeField] private Sprite greenMushroomSprite;
    [SerializeField] private Sprite blueMushroomSprite;
    [SerializeField] private Sprite yellowMushroomSprite;

    // Network runner and session properties
    private NetworkRunner _runner;
    private Dictionary<PlayerRef, PlayerInfo> _players = new Dictionary<PlayerRef, PlayerInfo>();
    private Dictionary<string, SessionInfo> _sessions = new Dictionary<string, SessionInfo>();
    private string _currentSessionName;
    private bool _isHost;
    private string _gameScene = "Gameplay";

    // Color pick tracking
    private Sprite[] availableSprites;
    private Dictionary<Sprite, bool> _colorAvailability = new Dictionary<Sprite, bool>();
    private Sprite _selectedColor;
    private PlayerRef _localPlayerRef;
    private bool _isReady;
    private GameObject _activePlayerSlot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Build the array of sprites
        availableSprites = new Sprite[]
        {
            purpleMushroomSprite,
            blackMushroomSprite,
            greenMushroomSprite,
            blueMushroomSprite,
            yellowMushroomSprite
        };

        _colorAvailability = new Dictionary<Sprite, bool>();
        foreach (var sprite in availableSprites)
        {
            _colorAvailability[sprite] = true;
        }
    }

    private void Start()
    {
        // Set up UI element listeners
        SetupUIListeners();

        // Show main menu panel initially
        ShowMainMenuPanel();

        // Load saved username if exists
        string savedUsername = PlayerPrefs.GetString("Username", "");
        if (!string.IsNullOrEmpty(savedUsername))
        {
            usernameInputField.text = savedUsername;
        }

        // Initialize the network runner on startup
        InitializeNetworkRunner();
    }

    private async void InitializeNetworkRunner()
    {
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.AddCallbacks(this);
        }

        // Just join the session lobby without creating a game
        await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        Debug.Log("Network runner initialized and joined session lobby.");
    }

    private void SetupUIListeners()
    {
        // Main Menu Panel
        saveUsernameButton.onClick.AddListener(SaveUsername);
        clearUsernameButton.onClick.AddListener(ClearUsername);
        createGameButton.onClick.AddListener(CreateGame);
        findGamesButton.onClick.AddListener(ShowLobbySelectPanel);

        // Lobby Select Panel
        backToMainMenuButton.onClick.AddListener(ShowMainMenuPanel);

        // Lobby Panel
        hostStartGameButton.onClick.AddListener(StartGame);
        clientReadyUpButton.onClick.AddListener(ToggleReady);
        leaveLobbyButton.onClick.AddListener(LeaveLobby);
    }

    #region UI Panel Management

    private void ShowMainMenuPanel()
    {
        Debug.Log("ShowMainMenuPanel called, mainMenuPanel exists: " + (mainMenuPanel != null));

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        else
            Debug.LogError("mainMenuPanel is null!");

        if (lobbySelectPanel != null)
            lobbySelectPanel.SetActive(false);

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (colorPickerPanel != null)
            colorPickerPanel.SetActive(false);
    }

    private void ShowLobbySelectPanel()
    {
        Debug.Log("ShowLobbySelectPanel called, lobbySelectPanel exists: " + (lobbySelectPanel != null));

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (lobbySelectPanel != null)
            lobbySelectPanel.SetActive(true);
        else
            Debug.LogError("lobbySelectPanel is null!");

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (colorPickerPanel != null)
            colorPickerPanel.SetActive(false);

        // Start looking for sessions only if we have a valid panel
        if (lobbySelectPanel != null)
            RefreshSessions();
    }

    private void ShowLobbyPanel()
    {
        mainMenuPanel.SetActive(false);
        lobbySelectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        colorPickerPanel.SetActive(false);

        // Update lobby name
        UpdateLobbyName();
    }

    private void ShowColorPickerPanel()
    {
        colorPickerPanel.SetActive(true);
        PopulateColorPicker();
    }

    private void HideColorPickerPanel()
    {
        colorPickerPanel.SetActive(false);
    }

    #endregion

    #region Main Menu Functions

    private void SaveUsername()
    {
        string username = usernameInputField.text.Trim();
        if (!string.IsNullOrEmpty(username))
        {
            PlayerPrefs.SetString("Username", username);
            PlayerPrefs.Save();
        }
    }

    private void ClearUsername()
    {
        usernameInputField.text = "";
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.Save();
    }

    private string GetUsername()
    {
        string username = PlayerPrefs.GetString("Username", "");
        if (string.IsNullOrEmpty(username))
        {
            username = "Player" + UnityEngine.Random.Range(1000, 10000);
            PlayerPrefs.SetString("Username", username);
            PlayerPrefs.Save();
        }
        return username;
    }

    #endregion

    #region Network Functions

    private async void CreateGame()
    {
        SaveUsername();
        string username = GetUsername();

        // Create a session name based on username
        _currentSessionName = $"{username}'s game";
        _isHost = true;

        // Make sure runner is initialized
        if (_runner == null)
        {
            Debug.LogError("Network runner not initialized");
            return;
        }

        // Start the game as Host
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = _currentSessionName,
            PlayerCount = 4,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            ShowLobbyPanel();

            // Set up local player
            _localPlayerRef = _runner.LocalPlayer;
            _players[_localPlayerRef] = new PlayerInfo
            {
                Username = username,
                IsReady = false,
                SelectedColor = availableSprites[0],
                PlayerRef = _localPlayerRef
            };

            // Mark the first color as selected and unavailable
            _selectedColor = availableSprites[0];
            _colorAvailability[_selectedColor] = false;

            // Spawn player slot
            SpawnLocalPlayerSlot();

            // IMPORTANT: Add this line to announce the host to all clients
            RPC_AnnouncePlayer(_localPlayerRef, username, _selectedColor.name);

            // Configure buttons for host
            hostStartGameButton.gameObject.SetActive(true);
            clientReadyUpButton.gameObject.SetActive(false);
            UpdateStartButtonState();
        }
        else
        {
            Debug.LogError($"Failed to start game: {result.ShutdownReason}");
        }
    }

    public async void JoinGame(string sessionName)
    {
        SaveUsername();
        string username = GetUsername();

        _currentSessionName = sessionName;
        _isHost = false;

        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            PlayerCount = 4,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            ShowLobbyPanel();

            // Set up local player
            _localPlayerRef = _runner.LocalPlayer;

            // Find an available color
            _selectedColor = availableSprites.FirstOrDefault(sprite =>
                !_colorAvailability.ContainsKey(sprite) || _colorAvailability[sprite]);

            if (_selectedColor != null)
            {
                _colorAvailability[_selectedColor] = false;
            }
            else
            {
                _selectedColor = availableSprites[0]; // Fallback
                Debug.LogWarning("No available colors found, using default");
            }

            // Add to players dictionary with complete information
            _players[_localPlayerRef] = new PlayerInfo
            {
                Username = username,
                IsReady = false,
                SelectedColor = _selectedColor,
                PlayerRef = _localPlayerRef
            };

            // Configure buttons for client
            hostStartGameButton.gameObject.SetActive(false);
            clientReadyUpButton.gameObject.SetActive(true);

            // Create my player slot first
            _activePlayerSlot = SpawnLocalPlayerSlot();

            // IMPORTANT: Announce myself to everyone after creating my slot
            RPC_AnnouncePlayer(_localPlayerRef, username, _selectedColor.name);

            // Then request other players' data
            RPC_RequestPlayerData(_localPlayerRef);
        }
        else
        {
            Debug.LogError($"Failed to join game: {result.ShutdownReason}");
        }
    }

    private async void RefreshSessions()
    {
        // Clear existing session list
        foreach (Transform child in sessionListContainer)
        {
            Destroy(child.gameObject);
        }
        _sessions.Clear();

        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.AddCallbacks(this);
        }

        // Request session list updates
        Debug.Log("Requesting session list...");
        await _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    private void CreateSessionListItem(SessionInfo sessionInfo)
    {
        GameObject sessionItem = Instantiate(sessionListItemPrefab, sessionListContainer);

        // Set session name
        TextMeshProUGUI sessionNameText = sessionItem.transform.Find("RoomName").GetComponent<TextMeshProUGUI>();
        sessionNameText.text = sessionInfo.Name;

        // Set player count
        TextMeshProUGUI playerCountText = sessionItem.transform.Find("PlayerCount").GetComponent<TextMeshProUGUI>();
        playerCountText.text = $"{sessionInfo.PlayerCount}/{sessionInfo.MaxPlayers}";

        // Set join button
        Button joinButton = sessionItem.transform.Find("JoinGameButton").GetComponent<Button>();
        joinButton.onClick.AddListener(() => JoinGame(sessionInfo.Name));
    }

    private async void LeaveLobby()
    {
        // Store the host state before we reset everything
        bool wasHost = _isHost;
        Debug.Log("LeaveLobby called, wasHost: " + wasHost);

        // Clean up player slots UI (but don't destroy the container)
        foreach (Transform child in playerSlotsContainer)
        {
            Destroy(child.gameObject);
        }

        // Reset player data
        _players.Clear();
        _colorAvailability.Clear();
        foreach (var color in availableSprites)
        {
            _colorAvailability[color] = true;
        }
        _isReady = false;
        _activePlayerSlot = null;
        _isHost = false; // Reset host flag

        // Leave the game session but don't destroy the runner
        if (_runner != null && _runner.IsRunning)
        {
            try
            {
                await _runner.Shutdown();
                Debug.Log("Game session closed");

                // Rejoin the session lobby
                await _runner.JoinSessionLobby(SessionLobby.ClientServer);
                Debug.Log("Rejoined session lobby");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during session shutdown: {e.Message}");
            }
        }

        // Show the correct panel
        if (wasHost)
        {
            ShowMainMenuPanel();
        }
        else
        {
            ShowLobbySelectPanel();
        }
    }

    private void StartGame()
    {
        if (_isHost && AllPlayersReady())
        {
            // Create the player data package to pass to the game scene
            List<PlayerData> playerDataList = new List<PlayerData>();
            foreach (var player in _players)
            {
                playerDataList.Add(new PlayerData
                {
                    PlayerRef = player.Key,
                    Username = player.Value.Username,
                    ColorName = GetSpriteName(player.Value.SelectedColor)
                });
            }

            // Save player data to be accessed in the game scene
            GameDataManager.Instance.SetPlayerData(playerDataList);

            // Load the game scene for all players
            RPC_LoadGameScene();
        }
    }

    private void ToggleReady()
    {
        _isReady = !_isReady;

        // Update local player data
        if (_players.ContainsKey(_localPlayerRef))
        {
            var playerInfo = _players[_localPlayerRef];
            playerInfo.IsReady = _isReady;
            _players[_localPlayerRef] = playerInfo;

            // Update ready status visuals on player slot
            if (_activePlayerSlot != null)
            {
                UpdatePlayerSlotReadyStatus(_activePlayerSlot, _isReady);
            }
            else
            {
                Debug.LogWarning("Active player slot is null in ToggleReady");
            }

            // Update button visuals
            if (clientReadyUpButton != null)
            {
                var buttonText = clientReadyUpButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = _isReady ? "Unready" : "Ready Up";
                }
                clientReadyUpButton.image.color = _isReady ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.4f, 1f, 0.4f);
            }

            // Notify other players
            if (_runner && _runner.IsRunning)
            {
                RPC_UpdatePlayerReadyStatus(_localPlayerRef, _isReady);
            }
        }
    }

    private bool AllPlayersReady()
    {
        if (_players.Count < 2) // At least 2 players (host + 1 client)
            return false;

        return _players.Values.All(p => p.IsReady || p.PlayerRef == _localPlayerRef);
    }

    private void UpdateStartButtonState()
    {
        if (_isHost && hostStartGameButton != null)
        {
            // Count ready players (excluding the host)
            int playerCount = _players.Count;
            int readyCount = _players.Values.Count(p => p.IsReady);

            if (playerCount > 1) // At least 1 client besides host
            {
                var buttonText = hostStartGameButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"{readyCount}/{playerCount - 1} Players Ready";
                }

                if (AllPlayersReady())
                {
                    hostStartGameButton.interactable = true;
                    hostStartGameButton.image.color = new Color(0.2f, 0.8f, 0.2f); // Green
                }
                else
                {
                    hostStartGameButton.interactable = false;
                    hostStartGameButton.image.color = new Color(0.5f, 0.5f, 0.5f); // Gray
                }
            }
            else
            {
                // No clients yet
                var buttonText = hostStartGameButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "Waiting for players...";
                }
                hostStartGameButton.interactable = false;
                hostStartGameButton.image.color = new Color(0.5f, 0.5f, 0.5f); // Gray
            }
        }
    }

    #endregion

    #region Player Slot Management

    private GameObject SpawnLocalPlayerSlot()
    {
        // Check for null before proceeding
        if (playerSlotPrefab == null || playerSlotsContainer == null)
        {
            Debug.LogError("Player slot prefab or container is null!");
            return null;
        }

        // Create player slot
        GameObject playerSlot = Instantiate(playerSlotPrefab, playerSlotsContainer);
        if (playerSlot == null)
        {
            Debug.LogError("Failed to instantiate player slot!");
            return null;
        }

        _activePlayerSlot = playerSlot;
        playerSlot.name = "PlayerSlot_" + _localPlayerRef.PlayerId;

        // Get the Panel first
        Transform panelTransform = playerSlot.transform.Find("Panel");
        if (panelTransform == null)
        {
            Debug.LogError("Panel not found in player slot!");
            return null;
        }

        // Set username
        TextMeshProUGUI nameText = panelTransform.Find("PlayerName_Text")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = GetUsername();
        }
        else
        {
            Debug.LogError("PlayerName_Text component not found in player slot!");
        }

        // Set sprite
        Image mushroomImage = panelTransform.Find("PlayerMushroom_Image")?.GetComponent<Image>();
        if (mushroomImage != null && _selectedColor != null)
        {
            mushroomImage.sprite = _selectedColor;
        }
        else
        {
            Debug.LogError("PlayerMushroom_Image component not found or _selectedColor is null!");
        }

        // Set color picker button
        Button colorButton = panelTransform.Find("PlayerColor_Button")?.GetComponent<Button>();
        if (colorButton != null)
        {
            colorButton.onClick.RemoveAllListeners();
            colorButton.onClick.AddListener(ShowColorPickerPanel);
        }
        else
        {
            Debug.LogError("PlayerColor_Button not found in player slot!");
        }

        // Set ready status
        Image readyImage = panelTransform.Find("PlayerReady_Image")?.GetComponent<Image>();
        if (readyImage != null)
        {
            readyImage.color = _isReady ? Color.green : Color.gray;
        }
        else
        {
            Debug.LogError("PlayerReady_Image not found in player slot!");
        }

        // Notify others of new player
        if (_runner && _runner.IsRunning && _selectedColor != null)
        {
            RPC_AnnouncePlayer(_localPlayerRef, GetUsername(), _selectedColor.name);
        }
        return playerSlot;
    }

    // Add this to the #region Player Slot Management section
    private void UpdatePlayerSlotSprite(GameObject playerSlot, Sprite sprite)
    {
        if (playerSlot == null)
        {
            Debug.LogWarning("Attempted to update sprite on null player slot");
            return;
        }

        Transform panelTransform = playerSlot.transform.Find("Panel");
        if (panelTransform == null)
        {
            Debug.LogWarning("Panel not found in player slot");
            return;
        }

        Image mushroomImage = panelTransform.Find("PlayerMushroom_Image")?.GetComponent<Image>();
        if (mushroomImage == null)
        {
            Debug.LogWarning("PlayerMushroom_Image component not found on player slot");
            return;
        }

        mushroomImage.sprite = sprite;
    }

    private void SpawnRemotePlayerSlot(PlayerRef playerRef, string username, Sprite sprite, bool isReady)
    {
        // Create player slot
        GameObject playerSlot = Instantiate(playerSlotPrefab, playerSlotsContainer);
        playerSlot.name = "PlayerSlot_" + playerRef.PlayerId;

        // Get the Panel first
        Transform panelTransform = playerSlot.transform.Find("Panel");
        if (panelTransform == null)
        {
            Debug.LogError("Panel not found in remote player slot!");
            return;
        }

        // Set username
        TextMeshProUGUI nameText = panelTransform.Find("PlayerName_Text")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = username;
        }
        else
        {
            Debug.LogError("PlayerName_Text not found in remote player slot!");
        }

        // Set sprite
        Image mushroomImage = panelTransform.Find("PlayerMushroom_Image")?.GetComponent<Image>();
        if (mushroomImage != null)
        {
            mushroomImage.sprite = sprite;
        }
        else
        {
            Debug.LogError("PlayerMushroom_Image not found in remote player slot!");
        }

        // Disable color picker for remote players
        Button colorButton = panelTransform.Find("PlayerColor_Button")?.GetComponent<Button>();
        if (colorButton != null)
        {
            colorButton.interactable = false;
        }
        else
        {
            Debug.LogError("PlayerColor_Button not found in remote player slot!");
        }

        // Set ready status
        Image readyImage = panelTransform.Find("PlayerReady_Image")?.GetComponent<Image>();
        if (readyImage != null)
        {
            readyImage.color = isReady ? Color.green : Color.gray;
        }
        else
        {
            Debug.LogError("PlayerReady_Image not found in remote player slot!");
        }
    }

    private void UpdateLobbyName()
    {
        if (!string.IsNullOrEmpty(_currentSessionName))
        {
            lobbyNameText.text = _currentSessionName;
        }
    }

    private void PopulateColorPicker()
    {
        // Clear existing color options
        foreach (Transform child in colorOptionsContainer)
        {
            Destroy(child.gameObject);
        }

        // Create color options
        for (int i = 0; i < availableSprites.Length; i++)
        {
            Sprite sprite = availableSprites[i];
            bool isAvailable = !_colorAvailability.ContainsKey(sprite) || _colorAvailability[sprite];

            GameObject colorOption = Instantiate(colorOptionPrefab, colorOptionsContainer);

            // Set sprite
            Image colorImage = colorOption.GetComponent<Image>();
            colorImage.sprite = sprite;

            // Disable if already taken
            Button colorButton = colorOption.GetComponent<Button>();
            colorButton.interactable = isAvailable;

            // Add listener
            int spriteIndex = i; // Capture for lambda
            colorButton.onClick.AddListener(() => SelectSprite(spriteIndex));
        }
    }

    private void SelectSprite(int spriteIndex)
    {
        Sprite newSprite = availableSprites[spriteIndex];

        if (!_colorAvailability.ContainsKey(newSprite) || _colorAvailability[newSprite])
        {
            // Release previously selected sprite
            if (_selectedColor != null)
            {
                _colorAvailability[_selectedColor] = true;
            }

            // Assign new sprite
            _selectedColor = newSprite;
            _colorAvailability[_selectedColor] = false;

            // Update player data
            if (_players.ContainsKey(_localPlayerRef))
            {
                var playerInfo = _players[_localPlayerRef];
                playerInfo.SelectedColor = _selectedColor;
                _players[_localPlayerRef] = playerInfo;

                // Update sprite in player slot
                if (_activePlayerSlot != null)
                {
                    Transform panelTransform = _activePlayerSlot.transform.Find("Panel");
                    if (panelTransform != null)
                    {
                        Image mushroomImage = panelTransform.Find("PlayerMushroom_Image")?.GetComponent<Image>();
                        if (mushroomImage != null)
                        {
                            mushroomImage.sprite = _selectedColor;
                        }
                        else
                        {
                            Debug.LogError("PlayerMushroom_Image not found in active player slot");
                        }
                    }
                    else
                    {
                        Debug.LogError("Panel not found in active player slot");
                    }
                }

                // Hide color picker
                HideColorPickerPanel();

                // Notify other players of sprite change
                RPC_UpdatePlayerColor(_localPlayerRef, _selectedColor.name);
            }
        }
    }

    private void UpdatePlayerSlotReadyStatus(GameObject playerSlot, bool isReady)
    {
        if (playerSlot == null)
        {
            Debug.LogWarning("Attempted to update ready status on null player slot");
            return;
        }

        Transform panelTransform = playerSlot.transform.Find("Panel");
        if (panelTransform == null)
        {
            Debug.LogWarning("Panel not found in player slot");
            return;
        }

        Transform readyImageTransform = panelTransform.Find("PlayerReady_Image");
        if (readyImageTransform == null)
        {
            Debug.LogWarning("PlayerReady_Image transform not found in player slot");
            return;
        }

        Image readyImage = readyImageTransform.GetComponent<Image>();
        if (readyImage == null)
        {
            Debug.LogWarning("Image component not found on PlayerReady_Image");
            return;
        }

        readyImage.color = isReady ? Color.green : Color.gray;
    }

    private GameObject GetPlayerSlotByRef(PlayerRef playerRef)
    {
        string slotName = "PlayerSlot_" + playerRef.PlayerId;
        foreach (Transform child in playerSlotsContainer)
        {
            if (child.name == slotName)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    #endregion

    #region RPC Methods

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_AnnouncePlayer(PlayerRef playerRef, string username, string spriteName)
    {
        Debug.Log($"RPC_AnnouncePlayer received: {playerRef.PlayerId}, {username}, {spriteName}");

        // Find the sprite by name
        Sprite sprite = availableSprites.FirstOrDefault(s => s.name == spriteName);

        if (sprite == null)
        {
            Debug.LogError($"Could not find sprite with name: {spriteName}");
            return;
        }

        // Skip if this is about ourselves - we already have our own slot
        if (playerRef == _localPlayerRef)
        {
            Debug.Log($"Skipping self-announcement for player {playerRef.PlayerId}");
            return;
        }

        Debug.Log($"Processing player announcement for {playerRef.PlayerId}");

        // Add or update player info
        _players[playerRef] = new PlayerInfo
        {
            Username = username,
            SelectedColor = sprite,
            IsReady = false,
            PlayerRef = playerRef
        };

        // Mark the sprite as taken
        _colorAvailability[sprite] = false;

        // Create UI element for this remote player
        SpawnRemotePlayerSlot(playerRef, username, sprite, false);

        // Update start button if I'm the host
        if (_isHost)
        {
            UpdateStartButtonState();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPlayerData(PlayerRef requestingPlayer)
    {
        // Only the host (state authority) should respond
        if (_isHost)
        {
            Debug.Log($"Host received data request from player {requestingPlayer.PlayerId}, sending data for {_players.Count} players");

            // Send data about all existing players to the requesting player
            foreach (var player in _players)
            {
                Debug.Log($"Sending data for player {player.Key.PlayerId}: {player.Value.Username}");
                RPC_SendPlayerData(requestingPlayer, player.Key, player.Value.Username,
                    player.Value.SelectedColor.name, player.Value.IsReady);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SendPlayerData(PlayerRef targetPlayer, PlayerRef playerRef, string username,
                               string spriteName, bool isReady)
    {
        // Only process if we're the target player or if this is a broadcast to all
        if (targetPlayer == _localPlayerRef || targetPlayer == default)
        {
            Sprite sprite = availableSprites.FirstOrDefault(s => s.name == spriteName);

            if (!_players.ContainsKey(playerRef))
            {
                _players[playerRef] = new PlayerInfo
                {
                    Username = username,
                    SelectedColor = sprite,
                    IsReady = isReady,
                    PlayerRef = playerRef
                };

                // Mark the sprite as taken
                _colorAvailability[sprite] = false;

                // Create UI element for the player if it's not the local player
                if (playerRef != _localPlayerRef)
                {
                    SpawnRemotePlayerSlot(playerRef, username, sprite, isReady);
                }
            }
            else
            {
                // Update existing player data
                var playerInfo = _players[playerRef];
                playerInfo.Username = username;
                playerInfo.IsReady = isReady;

                // Only update color if it changed
                if (playerInfo.SelectedColor?.name != spriteName)
                {
                    // Free up old color
                    if (playerInfo.SelectedColor != null)
                    {
                        _colorAvailability[playerInfo.SelectedColor] = true;
                    }

                    playerInfo.SelectedColor = sprite;
                    _colorAvailability[sprite] = false;

                    // Update UI
                    GameObject playerSlot = GetPlayerSlotByRef(playerRef);
                    if (playerSlot != null)
                    {
                        UpdatePlayerSlotSprite(playerSlot, sprite);
                    }
                }

                _players[playerRef] = playerInfo;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_UpdatePlayerColor(PlayerRef playerRef, string spriteName)
    {
        if (_runner && _runner.IsRunning)
        {
            // Update player's selected sprite
            if (_players.ContainsKey(playerRef))
            {
                // Release old sprite
                Sprite oldSprite = _players[playerRef].SelectedColor;
                if (oldSprite != null)
                {
                    _colorAvailability[oldSprite] = true;
                }

                // Set new sprite
                Sprite newSprite = availableSprites.First(s => s.name == spriteName);
                var playerInfo = _players[playerRef];
                playerInfo.SelectedColor = newSprite;
                _players[playerRef] = playerInfo;
                _colorAvailability[newSprite] = false;

                // Update UI
                GameObject playerSlot = GetPlayerSlotByRef(playerRef);
                if (playerSlot != null)
                {
                    Transform panelTransform = playerSlot.transform.Find("Panel");
                    if (panelTransform != null)
                    {
                        Image mushroomImage = panelTransform.Find("PlayerMushroom_Image")?.GetComponent<Image>();
                        if (mushroomImage != null)
                        {
                            mushroomImage.sprite = newSprite;
                        }
                        else
                        {
                            Debug.LogError($"PlayerMushroom_Image not found for player {playerRef.PlayerId}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Panel not found in player slot for player {playerRef.PlayerId}");
                    }
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_UpdatePlayerReadyStatus(PlayerRef playerRef, bool isReady)
    {
        if (_players.ContainsKey(playerRef))
        {
            var playerInfo = _players[playerRef];
            playerInfo.IsReady = isReady;
            _players[playerRef] = playerInfo;

            // Update UI
            GameObject playerSlot = GetPlayerSlotByRef(playerRef);
            if (playerSlot != null)
            {
                UpdatePlayerSlotReadyStatus(playerSlot, isReady);
            }

            // Update start button state for host
            UpdateStartButtonState();
        }
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_LoadGameScene()
    {
        // In a real implementation, this would be decorated with [Rpc]
        if (_runner && _runner.IsRunning)
        {
            // In a real implementation, this would use Fusion's scene loading mechanism
            SceneManager.LoadScene(_gameScene);
        }
    }

    #endregion

    #region INetworkRunnerCallbacks Implementation

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} joined");

        // If I'm the joining player
        if (player == runner.LocalPlayer)
        {
            _localPlayerRef = player;

            // If I'm the host (this was already handled in CreateGame)
            // If I'm a client, this was handled in JoinGame
            // We don't need to do anything extra here
        }

        // If I'm the host and another player joined
        if (_isHost && player != runner.LocalPlayer)
        {
            // The new player will announce themselves via RPC_AnnouncePlayer
            UpdateStartButtonState();
        }
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left");

        // If the host left and I'm a client, go back to the lobby select
        if (_players.TryGetValue(player, out var leftPlayer) && _isHost == false && IsHostPlayer(player))
        {
            Debug.Log("Host left the game, returning to lobby selection");
            LeaveLobby();
            return;
        }

        // Remove the player from our dictionary
        if (_players.ContainsKey(player))
        {
            // Free up their color
            Sprite playerColor = _players[player].SelectedColor;
            if (playerColor != null)
            {
                _colorAvailability[playerColor] = true;
            }

            _players.Remove(player);
        }

        // Remove their UI element
        GameObject playerSlot = GetPlayerSlotByRef(player);
        if (playerSlot != null)
        {
            Destroy(playerSlot);
        }

        // If I'm the host, update the start button state
        if (_isHost)
        {
            UpdateStartButtonState();
        }
    }

    // Helper method to determine if a PlayerRef is the host
    private bool IsHostPlayer(PlayerRef player)
    {
        // In Fusion, typically player with ID 0 is the host
        return player.PlayerId == 0;
    }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"Received session list with {sessionList.Count} sessions");

        // Clear existing session list
        foreach (Transform child in sessionListContainer)
        {
            Destroy(child.gameObject);
        }
        _sessions.Clear();

        // Update with new sessions
        foreach (var sessionInfo in sessionList)
        {
            _sessions[sessionInfo.Name] = sessionInfo;
            CreateSessionListItem(sessionInfo);
        }
    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    #endregion

    #region Helper Methods

    private string GetSpriteName(Sprite sprite)
    {
        if (sprite == purpleMushroomSprite) return "Purple";
        if (sprite == blackMushroomSprite) return "Black";
        if (sprite == greenMushroomSprite) return "Green";
        if (sprite == blueMushroomSprite) return "Blue";
        if (sprite == yellowMushroomSprite) return "Yellow";

        return "Unknown"; // Default
    }

    #endregion

    private class PlayerInfo
    {
        public string Username;
        public Sprite SelectedColor;
        public bool IsReady;
        public PlayerRef PlayerRef;
    }
}

// Simple class to store player data for transfer to game scene
public class PlayerData
{
    public PlayerRef PlayerRef;
    public string Username;
    public string ColorName;
}

// Singleton to pass data between scenes
public class GameDataManager
{
    private static GameDataManager _instance;

    public static GameDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameDataManager();
            }
            return _instance;
        }
    }

    private List<PlayerData> _playerData;

    public void SetPlayerData(List<PlayerData> playerData)
    {
        _playerData = playerData;
    }

    public List<PlayerData> GetPlayerData()
    {
        return _playerData;
    }


}