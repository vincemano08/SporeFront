using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public InsectSpawner insectSpawner;
    void Awake()
    {
        insectSpawner = FindFirstObjectByType<InsectSpawner>();
        if (insectSpawner == null)
        {
            Debug.LogWarning("InsectSpawner cannot be found!");
        }
    }
    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            insectSpawner.SpawnInsects(); //spawn 3 insect if a player joins
        }
    }
}
