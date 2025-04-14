using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tecton : NetworkBehaviour
{
    public static Transform parent;

    public new int Id { get; private set; }
    public int SporeThreshold { get; private set; }
    public HashSet<GridObject> GridObjects { get; private set; } = new HashSet<GridObject>();
    public HashSet<Tecton> Neighbors { get; set; }
    public IEnumerable<GridObject> Spores => GridObjects.Where(go => go.occupantType == OccupantType.Spore);
    
    
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
        if (sporeManager == null) {
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

    // Add spores to the tekton, then check if enough spores have accumulated for a new fungus body to grow.
    /// <param name="amount">Number of spores to be added.</param>
    public void AddSpores(int amount)
    {
        if (sporeManager == null) {
            sporeManager = FindFirstObjectByType<SporeManager>();
        }

        for (int i = 0; i < amount; i++)
        {
            GridObject spawnGridObject = ChooseRandomEmptyGridObject();

            sporeManager.SpawnSpore(spawnGridObject);
        }

        // If the spore count reaches the threshold and there is no fungus body yet, initiate the growth of a new fungus body.
        if (Spores.Count() >= SporeThreshold && FungusBody == null)
        {
            GridObject spawnGridObject = ChooseRandomEmptyGridObject();

            if (spawnGridObject == null)
            {
                Debug.LogError("No empty grid objects found on the selected Tecton");
                return;
            }

            // Get current tecton's fungus body's player
            PlayerRef player = FungusBody != null ? FungusBody.GetComponent<NetworkObject>().InputAuthority : default;

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
