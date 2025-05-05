using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public InsectSpawner insectSpawner;
    public FungusBodyFactory fungusBodyFactory;
    public TimerManager timerManager;
    void Awake()
    {
        insectSpawner = FindFirstObjectByType<InsectSpawner>();
        fungusBodyFactory = FindFirstObjectByType<FungusBodyFactory>();
        if (insectSpawner == null)
        {
            Debug.LogWarning("InsectSpawner cannot be found!");
        }
        if (fungusBodyFactory == null) 
        {
            Debug.LogWarning("FungusBodyFactor cannot be found!");
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
            timerManager.StartTimer(60f); // Start the timer for 60 seconds
            timerManager.RpcStartTimer(60f);
            // RpcStartTimer(60f); // Start the timer for 60 seconds
        }
    }

}
