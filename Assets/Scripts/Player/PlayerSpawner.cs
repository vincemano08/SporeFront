using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{



    public InsectSpawner insectSpawner;
    public FungusBodyFactory fungusBodyFactory;
    public TimerManager timerManager;
    private bool timerStarted = false;

    [Tooltip("Number of players required to start the timer")]
    [SerializeField] private int requiredPlayerCount = 3;
    
    // Track joined players
    private HashSet<PlayerRef> joinedPlayers = new HashSet<PlayerRef>();




    private Dictionary<PlayerRef, Color> playerColors = new Dictionary<PlayerRef, Color>();
    [SerializeField]
    private List<Color> availablePlayerColors = new List<Color>() {
        Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f), Color.green, Color.blue,Color.yellow, Color.red,
    };
    private int nextColorIndex = 0;

    public object Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"Multiple instances of PlayerSpawner found. Destroying this one: {gameObject.name}");
            Destroy(gameObject); // Destroy duplicate instance
            return;
        }

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
            
            // Check if we've reached the required player count
            if (!timerStarted && joinedPlayers.Count >= requiredPlayerCount)
            {
                if (timerManager != null)
                {
                    timerManager.RpcStartTimer(60f);
                    timerStarted = true;
                    Debug.Log($"Timer started with {joinedPlayers.Count} players connected");
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



}