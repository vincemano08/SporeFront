using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InsectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject insectPrefab;
    [SerializeField] private int numberOfInsects;
    [SerializeField] private float heightOffset;

    [SerializeField] private WorldGeneration worldGen;

    public HashSet<MoveInsect> insects = new HashSet<MoveInsect>();
    /*
    void Start()
    {
        SpawnInsects();
    }*/
    /*
    public override void Spawned()
    {
        // Csak a state authority (szerver/host) spawnolja az insektet.
        if (Object.HasStateAuthority)
        {
            SpawnInsects();
        }
    }*/

    public void SpawnInsects(PlayerRef player)
    {
        int spawnCount = Mathf.Min(numberOfInsects, worldGen.tectonCount);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnInsectOnRandomTecton(player);
        }
        Debug.Log($"Spawned {spawnCount} insects on Tectons");
    }

    void SpawnInsectOnRandomTecton(PlayerRef player)
    {
        // Select a random Tecton
        Tecton selectedTecton = Tecton.ChooseRandom();
        if (selectedTecton == null)
        {
            Debug.LogError("No Tectons found to spawn insects on");
            return;
        }

        // TODO: Logic to prevent multiple insects on the same Tecton

        GridObject gridObject = selectedTecton.ChooseRandomEmptyGridObject();

        if (gridObject == null)
        {
            Debug.LogError("No empty grid objects found on the selected Tecton");
            return;
        }

        gridObject.occupantType = OccupantType.Insect;
        Vector3 spawnPosition = gridObject.transform.position + new Vector3(0, heightOffset, 0);

        // Spawn the insect
        NetworkObject insectNetworkObject = Runner.Spawn(insectPrefab, spawnPosition, Quaternion.identity, player);
        GameObject insect = insectNetworkObject.gameObject;
        Debug.Log($"Insect owner: {insectNetworkObject.InputAuthority}");

        insect.name = $"Insect_{selectedTecton.Id}";
        insect.transform.SetParent(gameObject.transform);

        // Component should already be on the prefab
        MoveInsect insectComponent = insect.GetComponent<MoveInsect>();
        if (insectComponent == null)
        {
            insectComponent = insect.AddComponent<MoveInsect>();
        }
        insects.Add(insectComponent);
        Debug.Log($"Insect added.");
    }

    public void RemoveAllInsects()
    {
        foreach (Transform child in gameObject.transform)
        {
            if (child == null) continue;
            NetworkObject networkObj = child.GetComponent<NetworkObject>(); // Get the NetworkObject component
            if (networkObj != null && Runner != null)
            {
                // Only the state authority can despawn objects
                if (Object.HasStateAuthority)
                {
                    Runner.Despawn(networkObj);
                }
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
        insects.Clear();
    }
}
