using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using TMPro;
using UnityEngine;

public class MoveInsect : NetworkBehaviour
{

    [SerializeField] private float speed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private InsectSpawner insectSpawner;

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private EventChannel eventChannel;
    [SerializeField] private Animator animator;


    [SerializeField] private InsectState state;
    public InsectState State
    {
        get => state;
        set
        {
            state?.Exit();
            state = value;
            state?.Enter();
        }
    }

    private bool selected = false;
    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;
            this.GetComponent<Outline>().enabled = value;
        }
    }

    [Networked] private bool IsMoving { get; set; }
    [Networked] private float AnimationSpeed { get; set; }
    [Networked] private int NetworkedBiteTrigger { get; set; }

    // Local tracker for the networked trigger
    private int _lastProcessedBiteTrigger;


    private GridObject _currentGridObject;
    private GridObject CurrentGridObject
    {
        get
        {
            // if the _currentGridObject is null, try to find it by its netid
            if (_currentGridObject == null && Runner.TryFindObject(CurrentGridObjectId, out var netObj))
                _currentGridObject = netObj.GetComponent<GridObject>();
            return _currentGridObject;

        }
        set
        {
            _currentGridObject = value;
            // update the netid field automatically
            CurrentGridObjectId = _currentGridObject.GetComponent<NetworkObject>().Id;
        }
    }

    // this will be used to sync the currentGridObject, so we can pass it to the appropriate RPC
    [Networked] private NetworkId CurrentGridObjectId { get; set; }


    private Queue<GridObject> path;

    private SporeManager sporeManager;

    public override void Spawned()
    {
        base.Spawned();

        _lastProcessedBiteTrigger = NetworkedBiteTrigger;

        path = new Queue<GridObject>();

        insectSpawner = FindFirstObjectByType<InsectSpawner>();
        insectSpawner.insects = new HashSet<MoveInsect>();
        foreach (Transform insect in insectSpawner.gameObject.transform)
        {
            var insectComponent = insect.GetComponent<MoveInsect>();
            if (insectComponent != null)
            {
                insectSpawner.insects.Add(insectComponent);
            }
        }

        // Set the default state
        State = new NormalState(this);

        sporeManager = FindFirstObjectByType<SporeManager>();
        if (sporeManager == null)
        {
            Debug.LogError("SporeManager not found in the scene.");
        }
        // Wait 0.5 seconds, than Initialize CurrentGridObject
        Invoke(nameof(InitializeCurrentGridObject), 0.5f);
    }

    private void InitializeCurrentGridObject()
    {
        CurrentGridObject = GridObject.GetGridObjectAt(transform.position);
        if (CurrentGridObject == null)
        {
            Debug.LogError("Failed to initialize CurrentGridObject at the insect's starting position.");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ConsumeSpore(NetworkId gridObjectId, MoveInsect insect)
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
            // Check if the insect is not paralysed before consuming spores
            if (State?.IsParalised() == false)
            {
                NetworkedBiteTrigger++;
                sporeManager.ConsumeSpores(neighbour, insect);
                Debug.Log("Spore consumed successfully.");
            }
            else
            {
                Debug.Log("Insect is paralysed and cannot consume spores.");
            }
        }
        else
        {
            Debug.Log("No spores nearby to consume.");
        }
    }

    // This RPC will be called by the client (input authority) and executed on the server (state authority)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestMoveToTarget(NetworkObject targetGridObjectRef)
    {
        // Find the target GridObject via its network reference.
        GridObject targetGridObject = targetGridObjectRef.GetComponent<GridObject>();
        if (targetGridObject == null)
        {
            Debug.LogError("Invalid target grid object reference");
            return;
        }

        // TODO: validate client input

        // If the insect is already moving, and we get a new target, we need to mark the previous path unoccupied
        if (path.Count > 0 || IsMoving)
        {
            foreach (var gridObject in path)
            {
                gridObject.occupantType = OccupantType.None;
            }
            path.Clear();
        }

        // Set the target grid object as occupied if it is not already
        if (targetGridObject.IsOccupied)
        {
            Debug.Log("Target grid object is already occupied");
            return;
        }

        GridObject startGridObject = GridObject.GetGridObjectAt(transform.position);
        List<GridObject> computedPath = AStarPathFinder.FindPath(startGridObject, targetGridObject);

        if (computedPath == null || computedPath.Count == 0)
        {
            Debug.Log("No path found on server");
            return;
        }

        // Replace the current path queue with the new path.
        path = new Queue<GridObject>(computedPath);

        // Reserve target position.
        targetGridObject.occupantType = OccupantType.Insect;

        IsMoving = true;
        animator.SetBool("isMoving", true);
        Debug.Log($"Server received a move request with a path length of {path.Count}");
    }

    private void Update()
    {
        if (!HasInputAuthority) return;
        if (Input.GetMouseButtonDown(1) && Selected)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var targetGridObject = GridObject.GetGridObjectAt(hit.point);
                if (targetGridObject != null && !targetGridObject.IsOccupied)
                {
                    NetworkObject targetNetObj = targetGridObject.GetComponent<NetworkObject>();
                    if (targetNetObj != null)
                    {
                        RPC_RequestMoveToTarget(targetNetObj);
                    }
                    else
                    {
                        Debug.LogError("Target GridObject does not have a NetworkObject component.");
                    }
                }
            }
        }
        HandleKeyboardInput();
        State?.Update();
    }
    private bool CanCrossThread(GridObject current, GridObject next)
    {
        if (current == null || next == null)
        {
            Debug.LogError("Current or next GridObject is null.");
            return false;
        }

        // Find the thread connecting the two GridObjects
        var thread = FungalThreadManager.Instance.FungalThreads.FirstOrDefault(t =>
            ( t.gridObjectA == current && t.gridObjectB == next ) ||
            ( t.gridObjectA == next && t.gridObjectB == current ));

        if (thread == null)
        {
            //Debug.Log("No thread found between the current and next GridObjects.");
            return true; // Allow movement if no thread exists
        }

        if (!thread.IsFullyDeveloped)
        {
            Debug.Log("Insect cannot cross the thread because it is not fully developed.");
            return false;
        }

        return true;
    }
    public override void FixedUpdateNetwork()
    {
        // Only run on server
        if (!HasStateAuthority) return;

        // If the insect is not moving, return
        if (!IsMoving) return;

        // Move towards the target position
        if (path != null && path.Count > 0)
        {
            var nextGridObject = path.Peek();
            if (CurrentGridObject == null)
            {
                CurrentGridObject = GridObject.GetGridObjectAt(transform.position);
            }
            if (!CanCrossThread(CurrentGridObject, nextGridObject))
            {
                Debug.Log("Insect cannot cross the thread.");
                return;
            }

            // Reserving the next grid object
            nextGridObject.occupantType = OccupantType.Insect;

            Vector3 targetPosition = nextGridObject.transform.position + new Vector3(0, 0.5f, 0);

            Vector3 moveDirection = targetPosition - transform.position;

            // Only rotate if there is a significant movement direction (not standing still)
            if (moveDirection.sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion targetRotation = Quaternion.LookRotation(-moveDirection);

                // Smoothly interpolate the current rotation towards the target rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime);
            }


            if (CurrentGridObject != null && CurrentGridObject.parentTecton != null)
            {
                // Calculate the speed based on the state of the insect
                float currentSpeed = speed * State?.GetSpeedMultiplier() ?? speed;
                if (CurrentGridObject.parentTecton.TectonType == TectonType.InsectEffectZone)
                {
                    currentSpeed = speed * 2f; // Speed up
                }

                AnimationSpeed = currentSpeed;
                animator.SetFloat("speed", currentSpeed);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Runner.DeltaTime);
            }

            // If the insect is close to the target position, dequeue the next grid object
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                if (CurrentGridObject == null)
                {
                    CurrentGridObject = GridObject.GetGridObjectAt(transform.position);
                }
                CurrentGridObject.occupantType = OccupantType.None;
                CurrentGridObject = path.Dequeue();
            }

            // If the path is empty, stop moving
            if (path.Count == 0)
            {
                IsMoving = false;
                Debug.Log("Insect has reached the target position");
            }
        }

        State?.FixedUpdateNetwork();
    }

    // Render runs every frame on all clients for visual updates
    public override void Render()
    {
        if (animator == null) return;

        // Apply networked state to the local Animator
        animator.SetBool("isMoving", IsMoving);
        animator.SetFloat("speed", AnimationSpeed);

        // Trigger bite animation based on networked trigger changes
        if (NetworkedBiteTrigger != _lastProcessedBiteTrigger)
        {
            animator.SetTrigger("bite");
            _lastProcessedBiteTrigger = NetworkedBiteTrigger;
        }
        // --- End Animation Updates ---

        // Fusion automatically handles transform interpolation/extrapolation here
    }

    private void OnMouseDown()
    {
        if (!HasInputAuthority) return;

        Selected = !Selected;

        foreach (var insect in insectSpawner.insects)
        {
            if (insect != this)
            {
                if (!insect.HasInputAuthority) continue;
                insect.Selected = false;
                insect.SetObjectMaterial(insect.gameObject, defaultMaterial);
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
        if (!Selected) return;

        if (!CurrentGridObjectId.IsValid)
        {
            Debug.LogError("CurrentGridObjectId is invalid.");
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (eventChannel != null)
                {
                    eventChannel.RaiseScoreChanged(1);
                    Debug.Log("Score changed by 1");
                }
                else
                {
                    Debug.LogError("EventChannel is null, score not updated");

                }

                // Call the RPC to request spore consumption, since it work well on the server, but it seems the occupantType field is messed up on the clients
                RPC_ConsumeSpore(CurrentGridObjectId, this); // xd
            }

            else if (Input.GetKeyDown(KeyCode.X))
            {
                // Invoke the RPC on the server to request nearby fungal threads
                if (state?.CanCutThread() ?? false)
                {
                    // Call the RPC to request nearby threads
                    RPC_RequestNearbyThreads(CurrentGridObjectId);
                }
                else
                {
                    Debug.Log("Insect is paralysed and cannot cut threads.");
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
            // Play bite animation
            NetworkedBiteTrigger++;

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
