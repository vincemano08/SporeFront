using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class MoveInsect : NetworkBehaviour
{

    [SerializeField] private float speed;
    [SerializeField] private InsectSpawner insectSpawner;

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;

    public bool Selected { get; set; } = false;

    private GridObject currentGridObject;
    
    private NetworkQueue path;
    public override void Spawned()
    {
        base.Spawned();
        path = new NetworkQueue();
    }

    private void Awake()
    {
        insectSpawner = FindFirstObjectByType<InsectSpawner>();
        currentGridObject = GridObject.GetGridObjectAt(transform.position);
    }

    private void Update()
    {
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
                        path.Enqueue(p);
                        targetGridObject.occupantType = OccupantType.Insect;
                    }
                    else
                        Debug.Log("No path found");
                }
            }
        }
        

    }
    public override void FixedUpdateNetwork()
    {
        // Move towards the target position
        if (path != null && path.Count > 0)
        {
            var nextGridObject = path.Peek();
            // Reserving the next grid object
            nextGridObject.occupantType = OccupantType.Insect;

            Vector3 targetPosition = nextGridObject.transform.position + new Vector3(0, 1f, 0);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Runner.DeltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                currentGridObject.occupantType = OccupantType.None;
                currentGridObject = path.Dequeue();
            }
        }
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
}
