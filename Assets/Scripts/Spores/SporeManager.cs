using UnityEngine;

public class SporeManager : MonoBehaviour {

    [SerializeField] private GameObject sporePrefab;

    public void SpawnSpore(GridObject gridObject) {
        Vector3 position = gridObject.transform.position + Vector3.up;
        GameObject spore = Instantiate(sporePrefab, position, Quaternion.identity);
        spore.transform.SetParent(gridObject.transform);
        spore.name = "Spore";
        gridObject.occupantType = OccupantType.Spore;
    }

    public void RemoveSpore(GridObject gridObject) {
        gridObject.occupantType = OccupantType.None;
        Destroy(gridObject.transform.GetChild(0).gameObject);
    }
}
