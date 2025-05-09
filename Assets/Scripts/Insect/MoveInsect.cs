using Fusion;
using System.Collections.Generic;
using System.Net.WebSockets;
using TMPro;
using UnityEngine;

public class MoveInsect : NetworkBehaviour {

    [SerializeField] private float speed;
    [SerializeField] private InsectSpawner insectSpawner;

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private EventChannel eventChannel;

    public bool Selected { get; set; } = false;

    private bool isMoving = false;

    private GridObject _currentGridObject;
    private GridObject CurrentGridObject {
        get {
            // if the _currentGridObject is null, try to find it by its netid
            if (_currentGridObject == null && Runner.TryFindObject(CurrentGridObjectId, out var netObj)) 
                _currentGridObject = netObj.GetComponent<GridObject>();
            return _currentGridObject;

        }
        set { 
            _currentGridObject = value;
            // update the netid field automatically
            CurrentGridObjectId = _currentGridObject.GetComponent<NetworkObject>().Id;
        }
    }
    
    // this will be used to sync the currentGridObject, so we can pass it to the appropriate RPC
    [Networked] private NetworkId CurrentGridObjectId { get; set; }


    private Queue<GridObject> path;

    private SporeManager sporeManager;

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

        sporeManager = FindFirstObjectByType<SporeManager>();
        if (sporeManager == null)
        {
            Debug.LogError("SporeManager not found in the scene.");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ConsumeSpore(NetworkId gridObjectId)
    {
        if (!Runner.TryFindObject(gridObjectId, out var netObj))
        {
            Debug.LogError($"GridObject with ID {gridObjectId} not found on the server.");
            return;
        }

        GridObject gridObject = netObj.GetComponent<GridObject>();
        if (gridObject == null)
        {
            Debug.LogError("GridObject component not found on the server.");
            return;
        }

        var neighbour = sporeManager.IsSporeNearby(gridObject);
        if (neighbour != null)
        {
            sporeManager.ConsumeSpores(neighbour);
            Debug.Log("Spore consumed successfully.");
        }
        else
        {
            Debug.Log("No spores nearby to consume.");
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
        HandleKeyboardInput();
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
            if(CurrentGridObject != null && CurrentGridObject.parentTecton != null)
            {
                if (CurrentGridObject.parentTecton.TectonType == TectonType.InsectEffectZone)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Runner.DeltaTime * 2f);
                }
                else 
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Runner.DeltaTime);
                }
            }

            // If the insect is close to the target position, dequeue the next grid object
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f) {
                if (CurrentGridObject == null) {
                    CurrentGridObject = GridObject.GetGridObjectAt(transform.position);
                }
                CurrentGridObject.occupantType = OccupantType.None;
                CurrentGridObject = path.Dequeue();
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
    public void HandleKeyboardInput()
    {
        if (Selected)
        {
            if (!CurrentGridObjectId.IsValid)
            {
                Debug.LogError("CurrentGridObjectId is invalid.");
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    if(eventChannel != null)
                    {
                        eventChannel.RaiseScoreChanged(1);
                        Debug.Log("Score changed by 1");
                    }
                    else
                    {
                        Debug.LogError("EventChannel is null, score not updated");

                    }
                    // Call the RPC to request spore consumption, since it work well on the server, but it seems the occupantType field is messed up on the clients
                    RPC_ConsumeSpore(CurrentGridObjectId); // xd
                }

                else if (Input.GetKeyDown(KeyCode.X))
                {
                    // Invoke the RPC on the server to request nearby fungal threads
                    RPC_RequestNearbyThreads(CurrentGridObjectId);
                }
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestNearbyThreads(NetworkId gridObjectId)
    {
        if (!Runner.TryFindObject(gridObjectId, out var netObj))
        {
            Debug.LogError($"The GridObject was not found on the server: {gridObjectId}");
            return;
        }

        var gridObject = netObj.GetComponent<GridObject>();
        if (gridObject == null)
        {
            Debug.LogError("The GridObject component was not found.");
            return;
        }

        if (FungalThreadManager.Instance == null)
        {
            Debug.LogError("FungalThreadManager instance not available on server.");
            return;
        }

        var nearbyThreads = new List<FungalThread>();

        foreach (var thread in FungalThreadManager.Instance.FungalThreads)
        {
            if (thread.gridObjectA == gridObject || thread.gridObjectB == gridObject)
                nearbyThreads.Add(thread);
        }

        if (nearbyThreads.Count > 0)
        {
            foreach (var thread in nearbyThreads)
            {
                // Request the server to disconnect each thread
                FungalThreadManager.Instance.RPC_RequestThreadDisconnect(thread.Object.Id);
            }
            Debug.Log($"{nearbyThreads.Count} threads were disconnected.");
        }
        else
        {
            Debug.Log("No fungal threads found nearby.");
        }
    }
}
