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
            Destroy(gameObject);
    }

    public void Start() {
        if (bodyParent == null) {
            Debug.Log("Creating parent object for fungus bodies");
            bodyParent = new GameObject("FungusBodies").transform;
        }
    }


    // Typename should be renamed later
    public FungusBody SpawnFungusBody(GridObject tekton) {
        
        if (tekton == null) {
            Debug.LogError("Invalid tekton");
            return null;
        }

        float bodyHeight = bodyPrefab.GetComponent<FungusBody>().height;
        // float tektonHeight = tekton.height;
        Vector3 spawnPosition = tekton.transform.position + new Vector3(0, tekton.transform.localScale.y + dropHeight, 0);

        GameObject newFungusBody = Instantiate(bodyPrefab, spawnPosition, Quaternion.identity);
        newFungusBody.transform.parent = bodyParent;
        return newFungusBody.GetComponent<FungusBody>();

    }

}
