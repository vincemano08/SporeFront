using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InsectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject insectPrefab;
    [SerializeField] private int numberOfInsects;
    [SerializeField] private float heightOffset;

    [SerializeField] private WorldGeneration worldGen;

    public HashSet<GameObject> insects = new HashSet<GameObject>();

    void Start()
    {
        SpawnInsects();
    }

    public void SpawnInsects()
    {
        int spawnCount = Mathf.Min(numberOfInsects, worldGen.tectonCount);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnInsectOnRandomTecton();
        }
        Debug.Log($"Spawned {spawnCount} insects on Tectons");
    }

    void SpawnInsectOnRandomTecton()
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
        GameObject insect = Instantiate(insectPrefab, spawnPosition, Quaternion.identity);
        insect.name = $"Insect_{selectedTecton.Id}";
        insect.transform.SetParent(gameObject.transform);
        insects.Add(insect);

        // Component should already be on the prefab
        MoveInsect insectComponent = insect.GetComponent<MoveInsect>();
        if (insectComponent == null)
        {
            insectComponent = insect.AddComponent<MoveInsect>();
        }
    }

    public void RemoveAllInsects()
    {
        foreach (Transform child in gameObject.transform)
        {
            if (child == null) continue;
            Destroy(child.gameObject);
        }
        insects.Clear();
    }
}
