using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class InsectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject insectPrefab;
    [SerializeField] private int numberOfInsects;
    [SerializeField] private float heightOffset;

    [SerializeField] private WorldGeneration worldGen;

    public HashSet<MoveInsect> insects = new HashSet<MoveInsect>();

    public void SpawnInsectsNearBody(PlayerRef player, FungusBody fungusBody)
    {
        int spawnCount = Mathf.Min(numberOfInsects, worldGen.tectonCount);
        Tecton bodyTecton = fungusBody.Tecton;
        for (int i = 0; i < spawnCount; i++)
        {
            // Only spawn around the fungus body
            Tecton spawnTecton = Tecton.ChooseRandom(t => bodyTecton.Neighbors.Contains(t) || t == bodyTecton);
            SpawnInsectOnTecton(player, spawnTecton);
        }
        Debug.Log($"Spawned {spawnCount} insects.");
    }

    void SpawnInsectOnTecton(PlayerRef player, Tecton tecton)
    {
        if (tecton == null) return;

        // TODO: Logic to prevent multiple insects on the same Tecton

        GridObject gridObject = tecton.ChooseRandomEmptyGridObject();

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

        insect.name = $"Insect_{tecton.Id}";
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
