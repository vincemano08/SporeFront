using UnityEngine;

public class SporeManager : MonoBehaviour
{

    [SerializeField] private GameObject sporePrefab;

    public void SpawnSpore(GridObject gridObject)
    {
        if (gridObject == null)
        {
            Debug.LogError("Invalid grid object");
            return;
        }

        if (sporePrefab == null)
        {
            Debug.LogError("Spore prefab is not assigned");
            return;
        }

        Vector3 position = gridObject.transform.position + Vector3.up;
        GameObject spore = Instantiate(sporePrefab, position, Quaternion.identity);



        spore.transform.SetParent(gridObject.transform);
        spore.name = "Spore";
        gridObject.occupantType = OccupantType.Spore;
    }

    public void RemoveSpore(GridObject gridObject)
    {
        if (gridObject == null)
        {
            Debug.LogError("Invalid grid object");
            return;
        }

        gridObject.occupantType = OccupantType.None;

        if (gridObject.transform.childCount > 0 && gridObject.transform.GetChild(0) != null)
        {
            Destroy(gridObject.transform.GetChild(0).gameObject);
        }
        else
        {
            Debug.LogWarning("No spore found to remove on grid object");
        }
    }
}
