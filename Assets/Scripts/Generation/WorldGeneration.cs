using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGeneration : MonoBehaviour
{

    public int tectonCount;

    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int relaxationIterations;

    [SerializeField] private Transform mapParent;
    [SerializeField] private GameObject gridObjectPrefab;

    private GameObject[,] grid;

    public int Width { get => width; }
    public int Height { get => height; }


    private void OnEnable()
    {
        ClearMap();
        GenerateMap();
    }

    private void OnDisable()
    {
        ClearMap();
    }

    public void GenerateMap()
    {
        grid = new GameObject[Width, Height];
        int[,] tectonMap = new int[Width, Height];
        Tecton.parent = mapParent;

        Vector2Int[] tectonCenters = GenerateTectonCenters();

        tectonCenters = PerformLloydRelaxation(tectonCenters, tectonMap);

        CreateTectons(tectonMap);
        PlaceGridObjects(tectonMap, tectonCenters);
    }


    private Vector2Int[] GenerateTectonCenters()
    {
        Vector2Int[] tectonCenters = new Vector2Int[tectonCount];

        for (int i = 0; i < tectonCount; i++)
        {
            tectonCenters[i] = new Vector2Int(
                Random.Range(0, Width),
                Random.Range(0, Height)
            );
        }

        return tectonCenters;
    }

    private Vector2Int[] PerformLloydRelaxation(Vector2Int[] tectonCenters, int[,] tectonMap)
    {
        for (int iteration = 0; iteration < relaxationIterations; iteration++)
        {
            Vector2Int[] newTectonCenters = new Vector2Int[tectonCount];
            int[] tectonSizes = new int[tectonCount];

            // Initialize arrays
            for (int i = 0; i < tectonCount; i++)
            {
                newTectonCenters[i] = Vector2Int.zero;
                tectonSizes[i] = 0;
            }

            // Assign each cell to the closest tecton & calculate new tecton centers
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    int closestTecton = FindClosestTecton(tectonCenters, x, z);

                    tectonMap[x, z] = closestTecton;
                    newTectonCenters[closestTecton] += new Vector2Int(x, z);
                    tectonSizes[closestTecton]++;
                }
            }

            // Recalculate tecton centers
            for (int i = 0; i < tectonCount; i++)
            {
                if (tectonSizes[i] > 0)
                {
                    tectonCenters[i] = newTectonCenters[i] / tectonSizes[i];
                }
            }
        }

        return tectonCenters;
    }

    private int FindClosestTecton(Vector2Int[] tectonCenters, int x, int z)
    {
        int closestTecton = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < tectonCount; i++)
        {
            float distance = Vector2Int.Distance(new Vector2Int(x, z), tectonCenters[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTecton = i;
            }
        }

        return closestTecton;
    }

    private void CreateTectons(int[,] tectonMap)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Height; z++)
            {
                // Check if tecton exists with the current id
                bool tectonExists = false;
                foreach (Transform transform in mapParent)
                {
                    if (transform.name == $"Tecton_{tectonMap[x, z]}")
                    {
                        tectonExists = true;
                    }
                }
                if (tectonExists) continue;

                GameObject tectonObject = new GameObject($"Tecton_{tectonMap[x, z]}");
                tectonObject.transform.SetParent(mapParent);

                // Add Tecton component
                Tecton tectonComponent = tectonObject.AddComponent<Tecton>();
                tectonComponent.Init(tectonMap[x, z]);
            }
        }
        // Not too efficient, but whatever
        foreach (Transform tectonTransform in mapParent)
        {
            Tecton tecton = tectonTransform.GetComponent<Tecton>();
            tecton.Neighbors = FindNeighboringTectons(tectonMap, tecton.Id)
                .Select(id => Tecton.GetById(id)).ToHashSet();
        }
    }

    private IEnumerable<int> FindNeighboringTectons(int[,] tectonMap, int tectonID)
    {
        HashSet<int> neighbors = new HashSet<int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (tectonMap[x, z] == tectonID && !visited.Contains(new Vector2Int(x, z)))
                {
                    VisitPerimeter(x, z, tectonMap, tectonID, neighbors, visited);
                }
            }
        }
        return neighbors;
    }

    private void VisitPerimeter(int x, int z, int[,] tectonMap, int tectonID, HashSet<int> neighbors, HashSet<Vector2Int> visited)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(x, z));
        visited.Add(new Vector2Int(x, z));

        // Directions for N, S, E, W, NE, NW, SE, SW
        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dir in directions)
            {
                int nx = current.x + dir.x;
                int nz = current.y + dir.y;

                if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                {
                    int neighborID = tectonMap[nx, nz];
                    var neighborPos = new Vector2Int(nx, nz);

                    if (neighborID == tectonID && !visited.Contains(neighborPos))
                    {
                        visited.Add(neighborPos);
                        queue.Enqueue(neighborPos);
                    }
                    else if (neighborID != tectonID)
                    {
                        neighbors.Add(neighborID);
                    }
                }
            }
        }
    }

    private void PlaceGridObjects(int[,] tectonMap, Vector2Int[] tectonCenters)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Height; z++)
            {
                if (!IsEdgeCell(tectonMap, x, z))
                {
                    CreateGridObject(x, z, tectonMap[x, z]);
                }
            }
        }
    }

    private bool IsEdgeCell(int[,] tectonMap, int x, int z)
    {
        if (x > 0 && tectonMap[x, z] != tectonMap[x - 1, z]) return true;
        if (z > 0 && tectonMap[x, z] != tectonMap[x, z - 1]) return true;
        if (x > 0 && z > 0 && tectonMap[x, z] != tectonMap[x - 1, z - 1]) return true;

        return false;
    }

    private void CreateGridObject(int x, int z, int tectonId)
    {
        GameObject gridObject = Instantiate(gridObjectPrefab, new Vector3(x, 0, z), Quaternion.identity);

        Tecton parentTecton = Tecton.GetById(tectonId);

        gridObject.transform.SetParent(parentTecton.transform);
        gridObject.name = $"GridObject_{x}_{z}";

        GridObject gridObjectComponent = gridObject.GetComponent<GridObject>()
            ?? gridObject.AddComponent<GridObject>();

        parentTecton.GridObjects.Add(gridObjectComponent);

        gridObjectComponent.X = x;
        gridObjectComponent.Z = z;
        gridObjectComponent.parentTecton = parentTecton;

        grid[x, z] = gridObject;
    }


    // Destroy all children of the map parent
    public void ClearMap()
    {
        if (mapParent == null) return;
        foreach (Transform child in mapParent)
        {
            if (child == null) continue;
            foreach (Transform grandchild in child)
            {
                if (grandchild == null) continue;
                Destroy(grandchild.gameObject);
            }
            Destroy(child.gameObject);
        }
    }

    // Get object at position for other scripts to use
    public GameObject GetObject(int x, int z)
    {
        if (x < 0 || x >= Width || z < 0 || z >= Height)
        {
            Debug.LogError($"Position ({x}, {z}) is out of bounds.");
            return null;
        }
        return grid[x, z];
    }
}
