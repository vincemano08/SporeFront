using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGeneration : NetworkBehaviour
{

    public int tectonCount;

    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int relaxationIterations;

    [SerializeField] private Transform mapParent;
    [SerializeField] private GameObject tectonPrefab;
    [SerializeField] private GameObject gridObjectPrefab;

    // thsi will store all the tecton ids we have
    [Networked, Capacity(1000)] public NetworkArray<NetworkId> TectonIds { get; }
    private GameObject[,] grid;

    public int Width { get => width; }
    public int Height { get => height; }

    public override void Spawned() 
    {
        if (Runner.IsServer) {
            ClearMap();
            GenerateMap();
        } else if (!Runner.IsServer) {
            StartCoroutine(DelayedReconstruct());
        }
    }

    private System.Collections.IEnumerator DelayedReconstruct()
    {
        // wait for 0.5s so the networkedarrays will be synced
        yield return new WaitForSeconds(.5f);

        // loop through all the tectons
        foreach(var tectonId in TectonIds) {
            NetworkObject networkObjectTecton;

            // get the tectons as networkobjects
            Runner.TryFindObject(tectonId, out networkObjectTecton);
            if (networkObjectTecton != null) {
                
                // cast the networkobjects to tectons
                Tecton tecton = networkObjectTecton.gameObject.GetComponent<Tecton>();
                
                // loop through all the gridobject ids that are associated with the current tecton
                foreach(var gridObjId in tecton.GridObjectIds) {
                    NetworkObject networkObjectGridObj;
                    Runner.TryFindObject(gridObjId, out networkObjectGridObj);

                    if (networkObjectGridObj != null) {
                        // extract the gridobject component
                        GridObject gridObject = networkObjectGridObj.gameObject.GetComponent<GridObject>();
                        
                        // set parenttecton
                        gridObject.parentTecton = tecton;

                        // add gridObject to tecton
                        tecton.GridObjects.Add(gridObject);
                    }
                }

                NetworkObject networkObjectFungus;
                Runner.TryFindObject(tecton.FungusId, out networkObjectFungus);
                if (networkObjectFungus != null) {
                    // extract the fungus comp from the networkobj
                    FungusBody fungusBody = networkObjectFungus.gameObject.GetComponent<FungusBody>();
                    // set parentttecton for the fungus
                    fungusBody.Tecton = tecton;
                }

                foreach(var neighborTectonId in tecton.NeighborIds) {
                    NetworkObject networkObjectNeighborTecton;
                    Runner.TryFindObject(neighborTectonId, out networkObjectNeighborTecton);

                    if (networkObjectNeighborTecton != null) {
                        // extract the tecton component and add to the neihgbors hashset
                        Tecton neighborTecton = networkObjectNeighborTecton.GetComponent<Tecton>();
                        tecton.Neighbors.Add(neighborTecton);
                    }
                }

            }
        }
    }


    /*
    private void OnEnable()
    {
        ClearMap();
        GenerateMap();
    }
    */

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
        int index = 0;
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

                // fill the neworked array of tectonIds - this works as intended
                NetworkObject tectonNetworkObject = Runner.Spawn(tectonPrefab, Vector3.zero, Quaternion.identity);
                TectonIds.Set(index, tectonNetworkObject.Id);
                Debug.Log($"Tecton added to tectonIds at {index}: (value) {TectonIds.Get(index)}");
                index++;


                GameObject tectonObject = tectonNetworkObject.gameObject;
                tectonObject.name = $"Tecton_{tectonMap[x, z]}";

                tectonObject.transform.SetParent(mapParent);

                // Init Tecton component
                Tecton tectonComponent = tectonObject.GetComponent<Tecton>();
                tectonComponent.Init(tectonMap[x, z], mapParent);

                // Assign a random TectonType (or use specific logic)
                tectonComponent.TectonType = (TectonType)Random.Range(0, System.Enum.GetValues(typeof(TectonType)).Length);
            }
        }
        // Not too efficient, but whatever
        foreach (Transform tectonTransform in mapParent)
        {
            Tecton tecton = tectonTransform.GetComponent<Tecton>();
            var neighbors = FindNeighboringTectons(tectonMap, tecton.Id)
                .Select(id => Tecton.GetById(id)).ToHashSet();
            tecton.Neighbors = neighbors;

            // init the networkarray that stores the ids of the neighbors of the current tecton
            int indexForNetArray = 0;
            foreach (var neighbor in neighbors) {
                // extract the netobject
                var networkObjectNeighbor = neighbor.gameObject.GetComponent<NetworkObject>();
                tecton.NeighborIds.Set(indexForNetArray, networkObjectNeighbor.Id);
                indexForNetArray++;
            }
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
        NetworkObject networkObject = Runner.Spawn(gridObjectPrefab, new Vector3(x, 0, z), Quaternion.identity);
        GameObject gridObject = networkObject.gameObject;

        Tecton parentTecton = Tecton.GetById(tectonId);

        gridObject.transform.SetParent(parentTecton.transform);
        gridObject.name = $"GridObject_{x}_{z}";

        GridObject gridObjectComponent = gridObject.GetComponent<GridObject>()
            ?? gridObject.AddComponent<GridObject>();

        parentTecton.GridObjects.Add(gridObjectComponent);

        gridObjectComponent.X = x;
        gridObjectComponent.Z = z;
        gridObjectComponent.parentTecton = parentTecton;

        // fill the networkarray with the ids of gridobjects.
        // the indices seem to be fucked up, but it works anyway
        int index = parentTecton.GridObjects.Count - 1;
        parentTecton.GridObjectIds.Set(index, gridObject.GetComponent<NetworkObject>().Id); // could use the networkObject instead
        // Debug.Log($"Adding Id to networkArray at {index}: (value) {parentTecton.GridObjectIds.Get(index)}");

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
