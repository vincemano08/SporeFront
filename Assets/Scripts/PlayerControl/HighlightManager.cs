using UnityEngine;

public class HighlightManager : MonoBehaviour
{
    [SerializeField] private LayerMask highlightLayerMask;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material greenHighlightMaterial;
    [SerializeField] private Material redHighlightMaterial;
    [SerializeField] private int maxHighlightDistance;

    public GameObject currentHighlightedObject { get; private set; }
    private Tecton currentHighlightedTecton;

    void Update()
    {
        HandleHighlight();
    }

    private void HandleHighlight()
    {
        if (GameManager.Instance.CurrentMode == ActionMode.ThreadGrowth)
        {
            HighLightTecton();
        }
        else
        {
            HighLightGridObject();
        }
    }

    public void HighLightGridObject()
    {
        // reset the material of the previously highlighted object
        if (currentHighlightedObject != null)
        {
            SetObjectMaterial(currentHighlightedObject, defaultMaterial);
            currentHighlightedObject = null;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Highlight the object under the mouse cursor
        if (Physics.Raycast(ray, out RaycastHit hit, maxHighlightDistance, highlightLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject.CompareTag("GridObject"))
            {
                currentHighlightedObject = hitObject;
                SetObjectMaterial(currentHighlightedObject, highlightMaterial);
            }
        }
    }

    private void HighLightTecton()
    {
        if (currentHighlightedTecton != null)
        {
            ResetTectonMaterial(currentHighlightedTecton);
            currentHighlightedTecton = null;
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxHighlightDistance, highlightLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject.CompareTag("GridObject"))
            {
                GridObject gridObject = hitObject.GetComponent<GridObject>();
                Tecton tecton = gridObject.parentTecton;
                if(tecton != null)
                {
                    currentHighlightedTecton = tecton;
                    bool canConnect = GameManager.Instance.SelectedFungusBody != null && FungalThreadManager.Instance.CanConnect(GameManager.Instance.SelectedFungusBody.Tecton, tecton);
                    Material highlightMaterial = canConnect ? greenHighlightMaterial : redHighlightMaterial;
                    HighLightTecton(tecton, highlightMaterial);
                }
            }
        }
    }

    private void SetObjectMaterial(GameObject obj, Material material)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    private void HighLightTecton(Tecton tecton, Material material)
    {
        foreach (var gridObject in tecton.GridObjects)
        {
            SetObjectMaterial(gridObject.gameObject, material);
        }
    }

    private void ResetTectonMaterial(Tecton tecton)
    {
        foreach (var gridObject in tecton.GridObjects)
        {
            SetObjectMaterial(gridObject.gameObject, defaultMaterial);
        }
    }
    public void ResetAllHighlights()
    {
        if (currentHighlightedTecton != null)
        {
            ResetTectonMaterial(currentHighlightedTecton);
            currentHighlightedTecton = null;
        }

        if (currentHighlightedObject != null)
        {
            SetObjectMaterial(currentHighlightedObject, defaultMaterial);
            currentHighlightedObject = null;
        }
    }
}
