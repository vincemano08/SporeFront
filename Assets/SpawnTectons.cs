using UnityEngine;

public class SpawnTectons : MonoBehaviour
{
    GridManager gridManager;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.GenerateMap();
        }
        else
        {
            Debug.LogError("GridManager not found in the scene!");
        }
    }


    private void Update()
    {
        //if(Input.GetKeyDown(KeyCode.Space))
        //{
        //    gridManager.DestroyObject(2, 3);
        //    gridManager.DestroyObject(4, 5);
        //    gridManager.DestroyObject(6, 7);
        //}

        //if (Input.GetKeyDown(KeyCode.Return))
        //{
        //    gridManager.MoveObject(gridManager.GetObject(1, 2), 2, 3);
        //    gridManager.MoveObject(gridManager.GetObject(1, 3), 4, 5);
        //    gridManager.MoveObject(gridManager.GetObject(1, 4), 6, 7);
        //}
        //if(Input.GetKeyDown(KeyCode.Backspace))
        //{
        //    gridManager.PlaceObject(1, 2, gridManager.prefab);
        //    gridManager.PlaceObject(1, 3, gridManager.prefab);
        //    gridManager.PlaceObject(1, 4, gridManager.prefab);
        //}
        //if(Input.GetKeyDown(KeyCode.Delete))
        //{
        //    gridManager.GetObject(0, 0).transform.localScale = new Vector3(2, 2, 2);
        //    gridManager.GetObject(0, 9).transform.localScale = new Vector3(2, 2, 2);
        //    gridManager.GetObject(9, 0).transform.localScale = new Vector3(2, 2, 2);
        //    gridManager.GetObject(9, 9).transform.localScale = new Vector3(2, 2, 2);
        //}
    }
}

