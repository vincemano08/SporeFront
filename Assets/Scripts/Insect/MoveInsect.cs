using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoveInsect : NetworkBehaviour {

    [SerializeField] private float speed;
    [SerializeField] private InsectSpawner insectSpawner;

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;

    public bool Selected { get; set; } = false;

    private bool isMoving = false;

    private GridObject currentGridObject;

    private Queue<GridObject> path;

    public override void Spawned() {
        base.Spawned();
        path = new Queue<GridObject>();
        insectSpawner = FindFirstObjectByType<InsectSpawner>();
        insectSpawner.insects = new HashSet<MoveInsect>();
        foreach (Transform insect in insectSpawner.gameObject.transform) {
            var insectComponent = insect.GetComponent<MoveInsect>();
            if (insectComponent != null) {
                insectSpawner.insects.Add(insectComponent);
            }
        }
    }

    // This RPC will be called by the client (input authority) and executed on the server (state authority)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestMoveToTarget(NetworkObject targetGridObjectRef) {
        // Find the target GridObject via its network reference.
        GridObject targetGridObject = targetGridObjectRef.GetComponent<GridObject>();
        if (targetGridObject == null) {
            Debug.LogError("Invalid target grid object reference");
            return;
        }

        // TODO: validate client input

        // If the insect is already moving, and we get a new target, we need to mark the previous path unoccupied
        if (path.Count > 0 || isMoving) {
            foreach (var gridObject in path) {
                gridObject.occupantType = OccupantType.None;
            }
            path.Clear();
        }

        // Set the target grid object as occupied if it is not already
        if (targetGridObject.IsOccupied) {
            Debug.Log("Target grid object is already occupied");
            return;
        }

        GridObject startGridObject = GridObject.GetGridObjectAt(transform.position);
        List<GridObject> computedPath = AStarPathFinder.FindPath(startGridObject, targetGridObject);

        if (computedPath == null || computedPath.Count == 0) {
            Debug.Log("No path found on server");
            return;
        }

        // Replace the current path queue with the new path.
        path = new Queue<GridObject>(computedPath);

        // Reserve target position.
        targetGridObject.occupantType = OccupantType.Insect;

        isMoving = true;
        Debug.Log($"Server received a move request with a path length of {path.Count}");
    }

    private void Update() {
        if (!HasInputAuthority) return;
        if (Input.GetMouseButtonDown(1) && Selected) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                var targetGridObject = GridObject.GetGridObjectAt(hit.point);
                if (targetGridObject != null && !targetGridObject.IsOccupied) {
                    NetworkObject targetNetObj = targetGridObject.GetComponent<NetworkObject>();
                    if (targetNetObj != null) {
                        RPC_RequestMoveToTarget(targetNetObj);
                    } else {
                        Debug.LogError("Target GridObject does not have a NetworkObject component.");
                    }
                }
            }
        }
    }

    public override void FixedUpdateNetwork() {
        // Only run on server
        if (!HasStateAuthority) return;

        // If the insect is not moving, return
        if (!isMoving) return;

        // Move towards the target position
        if (path != null && path.Count > 0) {
            var nextGridObject = path.Peek();

            // Reserving the next grid object
            nextGridObject.occupantType = OccupantType.Insect;

            Vector3 targetPosition = nextGridObject.transform.position + new Vector3(0, 1f, 0);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Runner.DeltaTime);

            // If the insect is close to the target position, dequeue the next grid object
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f) {
                if (currentGridObject == null) {
                    currentGridObject = GridObject.GetGridObjectAt(transform.position);
                }
                currentGridObject.occupantType = OccupantType.None;
                currentGridObject = path.Dequeue();
            }

            // If the path is empty, stop moving
            if (path.Count == 0) {
                isMoving = false;
                Debug.Log("Insect has reached the target position");
            }
        }
    }

    private void OnMouseDown() {
        if (!HasInputAuthority) return;

        Selected = !Selected;

        foreach (var insect in insectSpawner.insects) {
            if (insect != this) {
                if (!insect.HasInputAuthority) continue;
                insect.Selected = false;
                insect.SetObjectMaterial(insect.gameObject, defaultMaterial);
            } else {
                SetObjectMaterial(this.gameObject, Selected ? selectedMaterial : defaultMaterial);
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
