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
    
    void Awake()
    {
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
            // Spawn one fungus body for the player
            var fungusBody = fungusBodyFactory.SpawnDefault(player);
            
            // Spawn insects near the fungus body
            insectSpawner.SpawnInsectsNearBody(player, fungusBody);
            
            // Add player to our tracking collection
            joinedPlayers.Add(player);
            
            // Check if we've reached the required player count
            if (!timerStarted && joinedPlayers.Count >= requiredPlayerCount)
            {
                timerManager.RpcStartTimer(60f);
                timerStarted = true;
                Debug.Log($"Timer started with {joinedPlayers.Count} players connected");
            }
            else
            {
                Debug.Log($"Player joined. Current player count: {joinedPlayers.Count}/{requiredPlayerCount}");
            }
        }
    }
}