using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SporeManager : MonoBehaviour
{

    [SerializeField] private GameObject sporePrefab;
    [SerializeField] private float sporeConsumptionTime = 2f;
    [SerializeField] private EventChannel eventChannel;

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

    public IEnumerator ConsumeSporesCoroutine(GridObject gridObject)
    {
        if (gridObject == null)
            yield break;

        Debug.Log("Spore consumption has begun...");

        yield return new WaitForSeconds(sporeConsumptionTime);

        if (eventChannel != null)
        {
            int scoreValue = 1; // later may vary depending on the type of spore
            eventChannel.RaiseScoreChanged(scoreValue);
        }
        else
            Debug.LogWarning("EventChannel is not assigned");

        RemoveSpore(gridObject);

        Debug.Log("Spore consumed");
    }

    public GridObject FindNearbySpore(GridObject gridObject)
    {
        if (gridObject == null)
        {
            Debug.LogWarning("FindNearbySpore received null gridObject");
            return null;
        }

        var neighbourGridObjects = gridObject.GetNeighbors();
        foreach (var neighbour in neighbourGridObjects)
        {
            if (neighbour.occupantType == OccupantType.Spore)
            {
                return neighbour;
            }
        }
        return null;
    }
}
