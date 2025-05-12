using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerSpawner : NetworkBehaviour, IPlayerJoined
{
    public InsectSpawner insectSpawner;
    public FungusBodyFactory fungusBodyFactory;
    public TimerManager timerManager;
    [SerializeField] private EventChannel eventChannel;
    private bool timerStarted = false;
    private bool hudVisibilitySet = false;

    [Tooltip("Number of players required to start the timer")]
    [SerializeField] private int requiredPlayerCount = 3;

    //Networked Dictionary to store player scores
    [Networked, Capacity(16)]
    private NetworkDictionary<PlayerRef, PlayerScore> PlayerScores { get; }

    // Local cache for UI updates
    private Dictionary<PlayerRef, PlayerScore> localPlayerScores = new Dictionary<PlayerRef, PlayerScore>();

    // Track joined players
    private HashSet<PlayerRef> joinedPlayers = new HashSet<PlayerRef>();

    private Dictionary<PlayerRef, Color> playerColors = new Dictionary<PlayerRef, Color>();
    [SerializeField]
    private List<Color> availablePlayerColors = new List<Color>() {
        Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f), Color.green, Color.blue,Color.yellow, Color.red,
    };
    private int nextColorIndex = 0;

    public static PlayerSpawner Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple instances of PlayerSpawner found. Destroying this one: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        timerManager = FindFirstObjectByType<TimerManager>();
        if (timerManager == null)
        {
            Debug.LogWarning("TimerManager cannot be found!");
        }
        if (insectSpawner == null)
        {
            Debug.LogWarning("InsectSpawner cannot be found!");
        }
        if (fungusBodyFactory == null)
        {
            Debug.LogWarning("FungusBodyFactory cannot be found!");
        }

        timerManager ??= FindFirstObjectByType<TimerManager>();
        insectSpawner ??= FindFirstObjectByType<InsectSpawner>();
        fungusBodyFactory ??= FindFirstObjectByType<FungusBodyFactory>();
    }

    public override void Spawned()
    {
        // Add a delay to ensure network is properly established
        if (Runner.IsServer)
        {
            // Don't call the RPC directly in Spawned - it can cause timing issues
            StartCoroutine(DelayedHudVisibility(false));
            StartCoroutine(ShareScoresRegularly());
        }
    }

    private IEnumerator DelayedHudVisibility(bool show)
    {
        // Wait a frame to ensure network is ready
        yield return new WaitForSeconds(0.5f);
        
        if (!hudVisibilitySet || show)
        {
            RpcSetHudVisibility(show);
            hudVisibilitySet = true;
        }
    }

    private IEnumerator ShareScoresRegularly()
    {
        while (true)
        {
            // Update UI about once per second
            yield return new WaitForSeconds(1.0f);
            RaiseScoreboardUpdatedEvent();
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            if (!joinedPlayers.Contains(player))
            {
                Color newPlayerColor = Color.white;
                if (availablePlayerColors.Count > 0)
                {
                    newPlayerColor = availablePlayerColors[nextColorIndex % availablePlayerColors.Count];
                    playerColors[player] = newPlayerColor; // Store on server
                    nextColorIndex++;
                }
                else
                {
                    playerColors[player] = newPlayerColor; // Store on server
                }
                Debug.Log($"Player {player.PlayerId} joined. Assigned color: {newPlayerColor}.");


            }
            // Spawn one fungus body for the player
            var fungusBody = fungusBodyFactory.SpawnDefault(player);

            // Spawn insects near the fungus body
            insectSpawner.SpawnInsectsNearBody(player, fungusBody);

            // Add player to our tracking collection
            joinedPlayers.Add(player);

            // Update the player score dictionary
            string username = GetUsernameFromPlayer(player);
            AddPlayerToScoreboard(player, username);

            // Check if we've reached the required player count
            if (!timerStarted && joinedPlayers.Count >= requiredPlayerCount)
            {
                if (timerManager != null)
                {
                    timerManager.RpcStartTimer(60f);
                    timerStarted = true;
                    Debug.Log($"Timer started with {joinedPlayers.Count} players connected");
                    StartCoroutine(DelayedHudVisibility(true));
                }
                else
                {
                    Debug.LogWarning("TimerManager is null, cannot start timer.");
                }
            }
            else
            {
                Debug.Log($"Player joined. Current player count: {joinedPlayers.Count}/{requiredPlayerCount}");
            }
        }
    }

    private string GetUsernameFromPlayer(PlayerRef player)
    {
        // Try to get username from the player's connection data
        if (Runner.GetPlayerConnectionToken(player) is byte[] tokenData)
        {
            try 
            {
                string usernameFromToken = System.Text.Encoding.UTF8.GetString(tokenData);
                if (!string.IsNullOrEmpty(usernameFromToken))
                    return usernameFromToken;
            }
            catch {}
        }
        
        // Fallback name if no token or parsing fails
        return $"Player {player.PlayerId}";
    }

    private void AddPlayerToScoreboard(PlayerRef player, string username)
    {
        var playerScore = new PlayerScore { 
            Username = username,
            Score = 0 
        };
        playerScore.CopyToNetworked();
        
        PlayerScores.Add(player, playerScore);
        RaiseScoreboardUpdatedEvent();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdatePlayerScore(PlayerRef player, int scoreChange)
    {
        if (Runner.IsServer && PlayerScores.TryGet(player, out PlayerScore score))
        {
            score.Score += scoreChange;
            score.CopyToNetworked();
            PlayerScores.Set(player, score);
            RaiseScoreboardUpdatedEvent();
        }
    }

    private void RaiseScoreboardUpdatedEvent()
    {
        // Update local cache
        localPlayerScores.Clear();
        foreach (var kvp in PlayerScores)
        {
            var score = kvp.Value;
            score.CopyFromNetworked();
            localPlayerScores[kvp.Key] = score;
        }
        
        // Notify UI
        eventChannel?.RaiseScoreboardUpdated(localPlayerScores);
    }

    // Public method to get scores for UI
    public Dictionary<PlayerRef, PlayerScore> GetScores()
    {
        return localPlayerScores;
    }
    
    /// <summary>
    /// Gets the assigned color for a given player.
    /// This is primarily authoritative and accurate on the Server/Host.
    /// Clients should generally rely on Networked properties on game objects (e.g., FungusBody.NetworkedBodyColor)
    /// to determine colors for other players' assets.
    /// </summary>
    public Color GetPlayerColor(PlayerRef player)
    {
        if (Runner.IsServer && playerColors.TryGetValue(player, out Color color))
        {
            return color;
        }
        // If on a client, or player not found, this might not be reliable for other players.
        // For the local player, a client might store its own color received via RPC or from its own objects.
        // Consider what to return if called on a client for a remote player, or if color is not yet assigned.
        // Returning white as a fallback.
        // Debug.LogWarning($"GetPlayerColor called for {player.PlayerId}. Runner.IsServer: {Runner.IsServer}. Color found: {playerColors.ContainsKey(player)}");
        if (playerColors.TryGetValue(player, out Color serverColor)) return serverColor; // Will work on host
        return Color.white; // Fallback
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcSetHudVisibility(bool show)
    {
        Debug.Log($"RpcSetHudVisibility({show}) called on {(Runner.IsServer ? "Server" : "Client")}");
        eventChannel?.RaiseShowHideHud(show);
    }
}