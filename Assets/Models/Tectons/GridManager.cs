using System.CodeDom.Compiler;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 10;
    public int height = 10;


    private GameObject[,] grid;

    public Transform mapParent;

    public GameObject prefab;

    private void Awake()
    {
        grid = new GameObject[width, height];
    }


    public void GenerateMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                PlaceObject(x, z, prefab);
            }
        }
    }

    public void PlaceObject(int x, int z, GameObject prefab)
    {
        if(x < 0 || x >= width || z < 0 || z >= height)
        {
            Debug.LogError("Invalid coordinates");
            return;
        }

        if(grid[x, z] != null)
        {
            Debug.LogError("There is already an object at this position");
            return;
        }

        Vector3 position = new Vector3(x, 0.5f, z);

        GameObject Tecton = Instantiate(prefab, position, Quaternion.identity, mapParent);

        GridObject gridObject = Tecton.GetComponent<GridObject>();

        if (gridObject == null)
        {
            Debug.LogError("Object does not have GridObject component");
            return;
        }

        gridObject.x = x;
        gridObject.z = z;

        grid[x, z] = Tecton;
    }


    public void MoveObject(GameObject obj, int newX, int newZ)
    {
        if (obj == null)
        {
            Debug.LogError("Invalid object");
            return;
        }

        GridObject gridObject = obj.GetComponent<GridObject>();

        if (gridObject == null)
        {
            Debug.LogError("Object does not have GridObject component");
            return;
        }

        int currentX = gridObject.x;
        int currentZ = gridObject.z;

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

        gridObject.x = newX;
        gridObject.z = newZ;

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
        
        GridObject gridObject = obj.GetComponent<GridObject>();

        if (gridObject == null)
        {
            Debug.LogError("Invalid object");
            return;
        }

        int x = gridObject.x;
        int z = gridObject.z;

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
