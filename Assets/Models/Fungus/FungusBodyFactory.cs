using UnityEngine;

public class FungusBodyFactory : MonoBehaviour
{
    
    // Singleton pattern - later could be DI instead
    public static FungusBodyFactory Instance { get; private set; }
    public GameObject bodyPrefab;
    public Transform bodyParent;

    [Tooltip("How high above the tekton the fungus body should spawn")]
    public float dropHeight = 3;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            // Checks if there is already an instance of the class, if so, it destroys that because of the singleton
            Destroy(gameObject);
    }

    public void Start() {
        if (bodyParent == null) {
            Debug.Log("Creating parent object for fungus bodies");
            bodyParent = new GameObject("FungusBodies").transform;
        }
    }

    public FungusBody SpawnFungusBody(GridObject gridObject) {
        
        if (gridObject == null) {
            Debug.LogError("Invalid tekton");
            return null;
        }

        // additional checks if the requirements for fungus spawning are met

        Vector3 spawnPosition = gridObject.transform.position + new Vector3(0, gridObject.transform.localScale.y + dropHeight, 0);

        GameObject newFungusBodyObj = Instantiate(bodyPrefab, spawnPosition, Quaternion.identity);
        newFungusBodyObj.transform.parent = bodyParent;
        
        FungusBody fungusBody = newFungusBodyObj.GetComponent<FungusBody>();
        if (fungusBody == null)
        {
            Debug.LogError("Spawned FungusBody prefab is missing the FungusBody component!", newFungusBodyObj);
            Destroy(newFungusBodyObj); // Clean up orphaned object
            return null;
        }

        // Assign the fungus body to the tekton and vice versa
        fungusBody.GridObject = gridObject;
        gridObject.FungusBody = fungusBody;

        return fungusBody;

    }

}
