using System.Collections.Generic;
using UnityEngine;

public class MovableObjectSpawner : MonoBehaviour
{
    public GameObject insectPrefab;
    public int numberOfInsectsToSpawn;
    public float heightOffset;

    private GridManager gridManager;
    private List<Tecton> availableTectons = new List<Tecton>();

    void Start()
    {
        // Find the GridManager
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found!");
            return;
        }

        // Wait a frame to ensure grid is generated
        Invoke("SpawnInsects", 0.1f);
    }

    void SpawnInsects()
    {
        // Find all Tectons in the scene
        Tecton[] allTectons = FindObjectsByType<Tecton>(FindObjectsSortMode.None);
        availableTectons.AddRange(allTectons);

        // Spawn the insects
        int spawnCount = Mathf.Min(numberOfInsectsToSpawn, availableTectons.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnInsectOnRandomTecton();
        }

        Debug.Log($"Spawned {spawnCount} insects on Tectons");
    }

    void SpawnInsectOnRandomTecton()
    {
        if (availableTectons.Count == 0)
        {
            Debug.LogWarning("No available Tectons to spawn insects on!");
            return;
        }

        // Select a random Tecton
        int index = Random.Range(0, availableTectons.Count);
        Tecton selectedTecton = availableTectons[index];

        // Optional: Remove this Tecton from available ones if you don't want multiple insects on the same Tecton
        // availableTectons.RemoveAt(index);

        // Calculate spawn position at center of Tecton
        Vector3 spawnPosition = new Vector3(
            selectedTecton.x + selectedTecton.gridSize / 2f - insectPrefab.transform.localScale.x / 2f,
            heightOffset,
            selectedTecton.z + selectedTecton.gridSize / 2f - insectPrefab.transform.localScale.z / 2f
        );

        // log sp
        Debug.Log($"Spawning insect at position: {spawnPosition}");

        // Spawn the insect
        GameObject insect = Instantiate(insectPrefab, spawnPosition, Quaternion.identity);
        insect.name = $"Insect_{selectedTecton.x}_{selectedTecton.z}";

        // MoveToMouse component should already be on the prefab
        MoveToMouse moveComponent = insect.GetComponent<MoveToMouse>();
        if (moveComponent == null)
        {
            moveComponent = insect.AddComponent<MoveToMouse>();
        }
    }
}
