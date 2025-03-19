using UnityEngine;
using System.Collections.Generic;

public class Tecton : MonoBehaviour
{
    public int gridSize = 5;

    public GridObject[,] grid;

    public int x;
    public int z;
    public int sporeCount = 0;
    public int sporeThreshold = 10;
    private FungusBody fungusBody;
    public GameObject gridPrefab;


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
        if (fungusBody != null)
        {
            return;
        }
        sporeCount += amount;
        Debug.Log($"({x},{z}) Number of spores: {sporeCount}");

        this.ChangeColor(new Color(0, 1, sporeCount / 10f));

        // If the spore count reaches the threshold and there is no fungus body yet, initiate the growth of a new fungus body.
        if (sporeCount >= sporeThreshold && FungusBody == null)
        {
            if (FungusBodyFactory.Instance != null)
            {
                FungusBodyFactory.Instance.SpawnFungusBody(this);
                sporeCount = 0; // Reset the spore counter.
                this.ChangeColor(Color.green);
            }
            else
            {
                Debug.LogError("FungusBodyFactory instance not found.");
            }
        }
    }

    public void InitializeGrid()
    {
        grid = new GridObject[gridSize, gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {

                if (gridPrefab == null)
                {
                    Debug.LogError("gridPrefab is not assigned in Tecton!");
                    return;
                }

                GameObject gridObjectGO = Instantiate(gridPrefab, new Vector3(x + i, 0, z + j), Quaternion.identity);
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
        //initialize the grid
        grid = new GridObject[gridSize, gridSize];

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

    internal void ChangeColor(Color newColor)
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                grid[i, j].ChangeColor(newColor);
            }
        }

    }
}
