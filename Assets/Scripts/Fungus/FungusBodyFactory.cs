using Fusion;
using UnityEngine;

public class FungusBodyFactory : NetworkBehaviour
{

    // Singleton pattern - later could be DI instead
    public static FungusBodyFactory Instance { get; private set; }

    [SerializeField] private GameObject bodyPrefab;

    [Tooltip("How high above the tekton the fungus body should spawn")]
    [SerializeField] private float dropHeight = 3f;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            // Checks if there is already an instance of the class, if so, it destroys that because of the singleton
            Destroy(gameObject);
    }

    public FungusBody SpawnDefault(PlayerRef player)
    {
        Debug.Log($"[FungusBodyFactory] SpawnDefault called for player: {player}");
        Tecton tecton = Tecton.ChooseRandom(x => !x.FungusBody);
        if (tecton != null && tecton.FungusBody == null)
        {
            GridObject spawnGridObject = tecton.ChooseRandomEmptyGridObject();
            Debug.Log($"[FungusBodyFactory] Found spawnGridObject: {spawnGridObject}");

            if (spawnGridObject == null)
            {
                Debug.LogError("[FungusBodyFactory] spawnGridObject is NULL before calling SpawnFungusBody!");
                return null;
            }
            return SpawnFungusBody(spawnGridObject, player);
        }
        else
        {
            Debug.LogError("No tectons found to spawn fungus body on");
            return null;
        }
    }

    public FungusBody SpawnFungusBody(GridObject spawnGridObject, PlayerRef player)
    {
        Debug.Log($"[FungusBodyFactory] SpawnFungusBody called with spawnGridObject: {spawnGridObject}, player: {player}");

        if (spawnGridObject == null)
        {
            Debug.LogError("[FungusBodyFactory] CRITICAL: spawnGridObject is NULL inside SpawnFungusBody!");
            return null; // Ne folytasd, ha null
        }

        Tecton tecton = spawnGridObject.parentTecton;

        if (tecton == null || tecton.FungusBody != null)
        {
            Debug.LogError("Invalid tekton or tekton already occupied by a fungus body");
            return null;
        }

        // additional checks if the requirements for fungus spawning are met

        spawnGridObject.occupantType = OccupantType.FungusBody;
        Vector3 spawnPosition = spawnGridObject.transform.position + Vector3.up * dropHeight;

        NetworkObject newNetworkFungusBody = Runner.Spawn(bodyPrefab, spawnPosition, Quaternion.identity, player);

        // set the networkid of this fungusbody for the tecton it resides on -- this is required for the syncronization
        tecton.FungusId = newNetworkFungusBody.Id;

        GameObject newFungusBody = newNetworkFungusBody.gameObject;
        newFungusBody.name = $"FungusBody_{tecton.Id}";
        newFungusBody.transform.SetParent(gameObject.transform);

        FungusBody fungusBody = newFungusBody.GetComponent<FungusBody>();
        // Should never happen, but just in case
        if (fungusBody == null)
        {
            Debug.LogError("Spawned FungusBody prefab is missing the FungusBody component!", newFungusBody);
            Destroy(newFungusBody); // Clean up orphaned object
            return null;
        }

        // Assign the fungus body to the tekton and vice versa
        fungusBody.Tecton = tecton;
        tecton.FungusBody = fungusBody;

        fungusBody.NetworkedColor = PlayerSpawner.Instance.GetPlayerColor(player);

        // Set the player reference for the fungus body
        fungusBody.PlayerReference = player;

        return fungusBody;
    }
}
