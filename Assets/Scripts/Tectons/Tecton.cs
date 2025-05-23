using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TectonType
{
    Default,                     // No special effect
    ThreadGrowthBoost,           // Speeds up FungalThread growth (when there are spores)
    ThreadDecay,                 // FungalThreads disappear over time
    //SingleThreadOnly,            // Only one FungalThread can grow here, this is in the specifikation, but it practically doesnt make sense
    NoFungusBodyAllowed,         // FungusBody cannot grow here
    InsectEffectZone,            // Material affects insects (speed up)
    //Breakable                    // TODO Tecton can split, severing FungalThreads
}
public class Tecton : NetworkBehaviour
{
    [Networked]
    public TectonType TectonType { get; set; }

    public static Transform parent;

    public new int Id { get; private set; }
    public int SporeThreshold { get; private set; }
    public HashSet<GridObject> GridObjects { get; private set; } = new HashSet<GridObject>();
    public HashSet<Tecton> Neighbors { get; set; } = new HashSet<Tecton>();
    public IEnumerable<GridObject> Spores => GridObjects.Where(go => go.occupantType == OccupantType.Spore);

    // this will store all child gridObject networkids. we will use this to reconstruct the hierarchy on the clients
    [Networked, Capacity(1000)] public NetworkArray<NetworkId> GridObjectIds { get; }
    // this will be used to sync the fungusbodies
    [Networked] public NetworkId FungusId { get; set; }

    // this will be used to sync the neighbors
    [Networked, Capacity(1000)] public NetworkArray<NetworkId> NeighborIds { get; }



    private SporeManager sporeManager;

    private FungusBody fungusBody;
    public FungusBody FungusBody
    {
        get { return fungusBody; }
        set
        {
            if (fungusBody != null)
            {
                Debug.Log("Tekton already has a fungus body");
                return;
            }
            fungusBody = value;
        }
    }

    public override void Spawned()
    {
        sporeManager = FindFirstObjectByType<SporeManager>();
        if (sporeManager == null)
        {
            Debug.LogError("SporeManager not found in the scene");
            return;
        }
        // If client side, fill GridObjects with the grid objects of the tecton

        //Runs too early, TODO
        foreach (var gridObject in GetComponentsInChildren<GridObject>())
        {
            GridObjects.Add(gridObject);
        }
    }

    // Not a beautiful solution, good for now
    public void Init(int id, Transform parent)
    {
        this.Id = id;
        this.SporeThreshold = 5;
        Tecton.parent = parent;
    }

    public static Tecton GetById(int id)
    {
        foreach (Transform child in parent)
        {
            Tecton tectonComponent = child.GetComponent<Tecton>();
            if (tectonComponent.Id == id)
            {
                return tectonComponent;
            }
        }
        return null;
    }

    public static HashSet<Tecton> GetAll()
    {
        HashSet<Tecton> tectons = new HashSet<Tecton>();
        if (parent == null)
        {
            Debug.LogError("parent is null");
            return null;
        }

        foreach (Transform child in parent)
        {
            tectons.Add(child.GetComponent<Tecton>());
        }
        return tectons;
    }

    public static Tecton ChooseRandom(Func<Tecton, bool> predicate = null)
    {
        var tectons = GetAll()?.ToList();
        if (tectons == null)
            return null;
        if (predicate != null)
        {
            tectons = tectons.Where(predicate).ToList();
        }
        if (tectons.Count == 0)
        {
            Debug.LogWarning("No tectons found");
            return null;
        }
        int index = UnityEngine.Random.Range(0, tectons.Count);
        return tectons[index];
    }
    private FungalThread HasFullyDevelopedThread()
    {
        if (FungalThreadManager.Instance == null)
        {
            Debug.LogError("FungalThreadManager instance is not available.");
            return null;
        }

        // Check if any thread in the manager is connected to this Tecton and is fully developed
        foreach (var thread in FungalThreadManager.Instance.FungalThreads)
        {
            if (( thread.tectonA == GetComponent<NetworkObject>() || thread.tectonB == GetComponent<NetworkObject>() ) &&
                thread.IsFullyDeveloped)
            {
                return thread; // Found a fully developed thread
            }
        }

        Debug.Log($"No fully developed thread found for Tecton {Id}.");
        return null;
    }

    // Add spores to the tekton, then check if enough spores have accumulated for a new fungus body to grow.
    /// <param name="amount">Number of spores to be added.</param>
    public void AddSpores(int amount)
    {
        if (sporeManager == null)
        {
            sporeManager = FindFirstObjectByType<SporeManager>();
        }

        for (int i = 0; i < amount; i++)
        {
            GridObject spawnGridObject = ChooseRandomEmptyGridObject();

            if (spawnGridObject == null)
            {
                Debug.LogError("No empty grid objects found on the selected Tecton");
                return;
            }

            sporeManager.SpawnSpore(spawnGridObject);
        }

        // If the spore count reaches the threshold and there is no fungus body yet, initiate the growth of a new fungus body.
        if (Spores.Count() >= SporeThreshold && FungusBody == null)
        {
            // Check if the Tecton has a fully developed thread
            FungalThread thread = HasFullyDevelopedThread();
            if (thread == null)
            {
                Debug.Log($"Cannot spawn FungusBody: No fully developed thread on Tecton {Id}.");
                return;
            }
            GridObject spawnGridObject = ChooseRandomEmptyGridObject();

            if (spawnGridObject == null)
            {
                Debug.LogError("No empty grid objects found on the selected Tecton");
                return;
            }

            // Save the selected Tecton into a variable
            Tecton tecton = spawnGridObject.parentTecton;

            //Check if the selected Tecton has Types that allow fungus body growth
            if (tecton.TectonType == TectonType.NoFungusBodyAllowed)
            {
                // If the Tecton is of type NoFungusBodyAllowed, do not allow fungus body growth
                Debug.Log("Fungus body cannot grow on this Tecton (TectonType: NoFungusBodyAllowed)");
                return;
            }

            // Get current tecton's fungus body's player
            PlayerRef player = thread.PlayerReference;

            FungusBodyFactory.Instance.SpawnFungusBody(spawnGridObject, player);
            // Despawn spores
            foreach (var gridObject in Spores)
            {
                sporeManager.RemoveSpore(gridObject);
            }
        }
    }

    public void ChangeColor(Color newColor)
    {
        foreach (var gridObject in GridObjects)
        {
            gridObject.ChangeColor(newColor);
        }
    }

    public GridObject ChooseRandomEmptyGridObject()
    {
        var emptyGridObjects = GridObjects.Where(go => !go.IsOccupied).ToList();
        if (emptyGridObjects.Count == 0)
        {
            Debug.LogWarning("No empty grid objects found");
            return null;
        }
        int index = UnityEngine.Random.Range(0, emptyGridObjects.Count);
        return emptyGridObjects[index];
    }
}
