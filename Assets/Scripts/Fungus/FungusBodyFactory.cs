using UnityEngine;

public class FungusBodyFactory : MonoBehaviour {

    // Singleton pattern - later could be DI instead
    public static FungusBodyFactory Instance { get; private set; }

    [SerializeField] private GameObject bodyPrefab;

    [Tooltip("How high above the tekton the fungus body should spawn")]
    [SerializeField] private float dropHeight = 3f;

    
    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            // Checks if there is already an instance of the class, if so, it destroys that because of the singleton
            Destroy(gameObject);
    }

    private void Start() {
        // Spawn a few fungus bodies by default
        for (int i = 0; i < 3; i++) {
            Tecton tecton = Tecton.ChooseRandom();
            if (tecton != null && tecton.FungusBody == null) {
                GridObject spawnGridObject = tecton.ChooseRandomEmptyGridObject();
                SpawnFungusBody(spawnGridObject);
            }
        }
    }


    public FungusBody SpawnFungusBody(GridObject spawnGridObject) {
        Tecton tecton = spawnGridObject.parentTecton;

        if (tecton == null || tecton.FungusBody != null) {
            Debug.LogError("Invalid tekton or tekton already occupied by a fungus body");
            return null;
        }

        // additional checks if the requirements for fungus spawning are met

        spawnGridObject.occupantType = OccupantType.FungusBody;
        Vector3 spawnPosition = spawnGridObject.transform.position + Vector3.up * dropHeight;

        GameObject newFungusBody = Instantiate(bodyPrefab, spawnPosition, Quaternion.identity);
        newFungusBody.name = $"FungusBody_{tecton.Id}";
        newFungusBody.transform.SetParent(gameObject.transform);

        FungusBody fungusBody = newFungusBody.GetComponent<FungusBody>();
        // Should never happen, but just in case
        if (fungusBody == null) {
            Debug.LogError("Spawned FungusBody prefab is missing the FungusBody component!", newFungusBody);
            Destroy(newFungusBody); // Clean up orphaned object
            return null;
        }

        // Assign the fungus body to the tekton and vice versa
        fungusBody.Tecton = tecton;
        tecton.FungusBody = fungusBody;

        return fungusBody;
    }
}
