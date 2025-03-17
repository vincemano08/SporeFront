using UnityEngine;
using System.Collections.Generic;

public class Tecton : MonoBehaviour
{
    private static int gridSize = 5;
    public static int GridSize
    {
        get { return gridSize; }
    }
    private GridObject[,] grid = new GridObject[GridSize, GridSize];

    public int x;
    public int z;
    public int sporeCount = 0;
    public int sporeThreshold = 5;
    private FungusBody fungusBody;


    public FungusBody FungusBody
    {
        get { return fungusBody; }
        set
        {
            // one tekton can only have one fungus body
            if (fungusBody != null)
            {
                Debug.LogError("Tekton already has a fungus body");
                return;
            }
            fungusBody = value;
        }
    }


    // Add spores to the tekton, then check if enough spores have accumulated for a new fungus body to grow.
    /// <param name="amount">Number of spores to be added.</param>
    public void AddSpores(int amount)
    {
        sporeCount += amount;
        Debug.Log($"({x},{z}) Number of spores: {sporeCount}");

        // If the spore count reaches the threshold and there is no fungus body yet, initiate the growth of a new fungus body.
        if (sporeCount >= sporeThreshold && FungusBody == null)
        {
            if (FungusBodyFactory.Instance != null)
            {
                FungusBodyFactory.Instance.SpawnFungusBody(this);
                sporeCount = 0; // Reset the spore counter.
            }
            else
            {
                Debug.LogError("FungusBodyFactory instance not found.");
            }
        }
    }

    public void InitializeGrid()
    {
        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                GameObject gridObjectGO = new GameObject($"GridObject_{i}_{j}");
                gridObjectGO.transform.parent = this.transform;
                GridObject gridObject = gridObjectGO.AddComponent<GridObject>();
                gridObject.x = x + i;
                gridObject.z = z + j;
                grid[i, j] = gridObject;
            }
        }
    }

    public void Awake()
    {
        InitializeGrid();
    }

    public void OnMouseDown()
    {
        if (FungusBodyFactory.Instance == null)
        {
            Debug.LogError("FungusBodyFactory not found");
            return;
        }
        FungusBodyFactory.Instance.SpawnFungusBody(this);
    }
}