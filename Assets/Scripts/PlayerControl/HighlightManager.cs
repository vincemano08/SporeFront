using UnityEngine;

public class HighlightManager : MonoBehaviour {
    [SerializeField] private LayerMask highlightLayerMask;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private int maxHighlightDistance;

    public GameObject currentHighlightedObject { get; private set; }

    void Update() {
        HandleHighlight();
    }

    private void HandleHighlight() {
        // Reset the material of the previously highlighted object
        if (currentHighlightedObject != null) {
            SetObjectMaterial(currentHighlightedObject, defaultMaterial);
            currentHighlightedObject = null;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Highlight the object under the mouse cursor
        if (Physics.Raycast(ray, out RaycastHit hit, maxHighlightDistance, highlightLayerMask)) {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject.CompareTag("GridObject")) {
                currentHighlightedObject = hitObject;
                SetObjectMaterial(currentHighlightedObject, highlightMaterial);
            }
        }
    }

    private void SetObjectMaterial(GameObject obj, Material material) {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) {
            renderer.material = material;
        }
    }
}
