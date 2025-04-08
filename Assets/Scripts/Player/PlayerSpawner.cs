using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public InsectSpawner insectSpawner;
    public FungusBodyFactory fungusBodyFactory;
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
            insectSpawner.SpawnInsects(player); //spawn 3 insect if a player joins
            fungusBodyFactory.SpawnDefault();
        }
    }
}
