using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveInsect : MonoBehaviour
{

    [SerializeField] private float speed;
    [SerializeField] private InsectSpawner insectSpawner;

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;

    public bool Selected { get; set; } = false;

    private GridObject currentGridObject;
    private Queue<GridObject> path;
    private SporeManager sporeManager;
    private bool isConsumingSpore = false;

    private void Awake()
    {
        insectSpawner = FindFirstObjectByType<InsectSpawner>();
        sporeManager = FindFirstObjectByType<SporeManager>();
        currentGridObject = GridObject.GetGridObjectAt(transform.position);
    }

    private void Update()
    {
        if (isConsumingSpore) return;

        if (Input.GetMouseButtonDown(1) && Selected)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var targetGridObject = GridObject.GetGridObjectAt(hit.point);
                if (targetGridObject != null && !targetGridObject.IsOccupied)
                {
                    var startGridObject = GridObject.GetGridObjectAt(transform.position);
                    var p = AStarPathFinder.FindPath(startGridObject, targetGridObject);
                    if (p != null)
                    {
                        path = new Queue<GridObject>(p);
                        targetGridObject.occupantType = OccupantType.Insect;
                    }
                    else
                        Debug.Log("No path found");
                }
            }
        }
        // Move towards the target position
        if (path != null && path.Count > 0)
        {
            var nextGridObject = path.Peek();
            // Reserving the next grid object
            nextGridObject.occupantType = OccupantType.Insect;

            Vector3 targetPosition = nextGridObject.transform.position + new Vector3(0, 1f, 0);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                currentGridObject.occupantType = OccupantType.None;
                currentGridObject = path.Dequeue();
            }
        }
        HandleKeyboardInput();
    }
    private void OnMouseDown()
    {
        Selected = !Selected;

        foreach (var insect in insectSpawner.insects)
        {
            if (insect != this.gameObject)
            {
                var insectComponent = insect.GetComponent<MoveInsect>();
                insectComponent.Selected = false;
                insectComponent.SetObjectMaterial(insect, defaultMaterial);
            }
            else
            {
                SetObjectMaterial(this.gameObject, Selected ? selectedMaterial : defaultMaterial);
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
    
    public void HandleKeyboardInput()
    {
        if(Selected && Input.GetKeyDown(KeyCode.C))
        {
            var neighbour = sporeManager.IsSporeNearby(currentGridObject);
            if (neighbour != null)
                StartCoroutine(ConsumeSporeAndContinue(neighbour));
            else
                Debug.Log("No spores nearby");
        }
    }

    private IEnumerator ConsumeSporeAndContinue(GridObject sporeGridObject)
    {
        isConsumingSpore = true;

        yield return StartCoroutine(sporeManager.ConsumeSporesCoroutine(sporeGridObject));

        isConsumingSpore = false;
        Debug.Log("Spore consumed, continuing path...");
    }
}
