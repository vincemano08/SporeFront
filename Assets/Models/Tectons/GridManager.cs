using System.CodeDom.Compiler;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 100;
    public int height = 100;
    public int tectonSize = 5;

    private GameObject[,] grid;

    public Transform mapParent;
    public GameObject gridObjectPrefab;



    private void Awake()
    {
        grid = new GameObject[width, height];
    }


    public void GenerateMap()
    {
        // Calculate how many tectons we can fit
        int tectonCountX = width / (tectonSize + 2);
        int tectonCountZ = height / (tectonSize + 2);

        for (int tx = 0; tx < tectonCountX; tx++)
        {
            for (int tz = 0; tz < tectonCountZ; tz++)
            {
                // Calculate actual position (with spacing)
                int x = tx * (tectonSize + 2);
                int z = tz * (tectonSize + 2);

                GameObject tectonGO = new GameObject($"Tecton_{x}_{z}");
                tectonGO.transform.SetParent(mapParent);
                Tecton tecton = tectonGO.AddComponent<Tecton>();
                tecton.x = x;
                tecton.z = z;
                tecton.gridSize = tectonSize;




                // Create the grid objects for this tecton
                for (int i = 0; i < tectonSize; i++)
                {
                    for (int j = 0; j < tectonSize; j++)
                    {
                        int gridX = x + i;
                        int gridZ = z + j;

                        GameObject gridObject = Instantiate(gridObjectPrefab, new Vector3(gridX, 0, gridZ), Quaternion.identity);
                        gridObject.transform.SetParent(tectonGO.transform);
                        gridObject.name = $"GridObject_{gridX}_{gridZ}";

                        // Assign coordinates to the GridObject component
                        GridObject gridObjectComponent = gridObject.GetComponent<GridObject>();
                        if (gridObjectComponent == null)
                        {
                            gridObjectComponent = gridObject.AddComponent<GridObject>();
                        }
                        gridObjectComponent.x = gridX;
                        gridObjectComponent.z = gridZ;
                        gridObjectComponent.parentTecton = tecton;
                        //store grid object in grid
                        grid[gridX, gridZ] = gridObject;
                        tecton.grid[i, j] = gridObjectComponent;
                    }
                }
            }
        }

        Debug.Log($"Generated {tectonCountX * tectonCountZ} Tectons with {tectonCountX * tectonCountZ * tectonSize * tectonSize} GridObjects");
    }





    public void MoveObject(GameObject obj, int newX, int newZ)
    {
        if (obj == null)
        {
            Debug.LogError("Invalid object");
            return;
        }

        Tecton tecton = obj.GetComponent<Tecton>();

        if (tecton == null)
        {
            Debug.LogError("Object does not have tecton component");
            return;
        }

        int currentX = tecton.x;
        int currentZ = tecton.z;

        if (newX < 0 || newX >= width || newZ < 0 || newZ >= height)
        {
            Debug.LogError($"New position ({newX}, {newZ}) is out of bounds.");
            return;
        }

        if (grid[newX, newZ] != null)
        {
            Debug.LogError("There is already an object at this position");
            return;
        }

        grid[currentX, currentZ] = null;
        grid[newX, newZ] = obj;

        tecton.x = newX;
        tecton.z = newZ;

        obj.transform.position = new Vector3(newX, 0.5f, newZ);
    }


    // Destroy using Coordinates
    public void DestroyObject(int x, int z)
    {
        if (x < 0 || x >= width || z < 0 || z >= height)
        {
            Debug.LogError($"Position ({x}, {z}) is out of bounds.");
            return;
        }
        if (grid[x, z] == null)
        {
            Debug.LogWarning($"No cube at position ({x}, {z}).");
            return;
        }

        Destroy(grid[x, z]);
        grid[x, z] = null;
    }

    // Destroy using GameObject reference
    public void DestroyGameObject(GameObject obj)
    {

        if (obj == null)
        {
            Debug.LogError("Invalid object");
            return;
        }

        Tecton tecton = obj.GetComponent<Tecton>();

        if (tecton == null)
        {
            Debug.LogError("Invalid object");
            return;
        }

        int x = tecton.x;
        int z = tecton.z;

        Destroy(grid[x, z]);
        grid[x, z] = null;
    }


    // Get object at position for other scripts to use
    public GameObject GetObject(int x, int z)
    {
        if (x < 0 || x >= width || z < 0 || z >= height)
        {
            Debug.LogError($"Position ({x}, {z}) is out of bounds.");
            return null;
        }
        return grid[x, z];
    }
}
