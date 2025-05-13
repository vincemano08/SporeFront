using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FungalThreadManager : NetworkBehaviour
{
    public static FungalThreadManager Instance { get; private set; }

    [SerializeField] private GameObject threadPrefab;

    private HashSet<(int, int)> connections = new HashSet<(int, int)>();
    private List<FungalThread> fungalThreads = new List<FungalThread>();
    public IReadOnlyList<FungalThread> FungalThreads => fungalThreads;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (threadPrefab == null)
            {
                Debug.LogError("Thread prefab is not set in the FungalThreadManager.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool CanConnect(Tecton source, Tecton target)
    {
        if (source == target) return false;

        //Log the size of neighbours
        // Debug.LogError($"FungalThreadManagger.CanConnect source: {source.Id} target: {target.Id} neighbours: {source.Neighbors.Count}; {target.Neighbors.Count}");
        if (source == null || target == null)
        {
            Debug.LogError("Source or target is null.");
            return false;
        }
        // Check if the source and target are neighbors
        if (!source.Neighbors.Contains(target)) return false;

        // Check if the connection already exists
        var key = GetConnectionKey(source, target);
        if (connections.Contains(key))
        {
            Debug.LogWarning($"Connection already exists between {source.Id} and {target.Id}");
            return false;
        }
        if (( source.TectonType == TectonType.SingleThreadOnly && HasThread(source) ) ||
            ( target.TectonType == TectonType.SingleThreadOnly && HasThread(target) ))
        {
            Debug.LogWarning($"Cannot connect Thread({source.Id}) and Thread({target.Id}): one of them has a single thread restriction.");
            return false;
        }

        return true;
    }

    private bool HasThread(Tecton source)
    {
        //Check if the source has any threads, with the fungalThreads list
        return fungalThreads.Any(thread => thread.tectonA == source.GetComponent<NetworkObject>()
                                        || thread.tectonB == source.GetComponent<NetworkObject>());
    }

    // the spawn method, that will be called only on the server
    private void SpawnThread(Tecton a, Tecton b, PlayerRef player)
    {
        var key = GetConnectionKey(a, b);
        connections.Add(key);

        NetworkObject threadNetworkObj = Runner.Spawn(threadPrefab, transform.position, transform.rotation);
        GameObject threadObj = threadNetworkObj.gameObject;
        FungalThread thread = threadObj.GetComponent<FungalThread>();

        NetworkObject netA = a.GetComponent<NetworkObject>();
        NetworkObject netB = b.GetComponent<NetworkObject>();

        thread.SetTectons(netA, netB);
        thread.PlayerReference = player;
        fungalThreads.Add(thread);

        // Set logical connection
        var (goA, goB) = thread.FindClosestGridObjectPair(netA, netB);
        if (goA != null && goB != null)
        {
            goA.AddExternalNeighbor(goB);
            goB.AddExternalNeighbor(goA);
            RPC_EstablishLogicalConnection(goA.GetComponent<NetworkObject>().Id, goB.GetComponent<NetworkObject>().Id);
        }
        else
        {
            Debug.LogWarning("Could not find grid objects to establish logical connection.");
        }
        if (a.TectonType == TectonType.ThreadDecay || b.TectonType == TectonType.ThreadDecay)
        {
            StartThreadDecayTimer(thread);
        }
    }

    private void StartThreadDecayTimer(FungalThread thread)
    {
        // Generate a random duration between 10 and 30 seconds
        float decayTime = UnityEngine.Random.Range(10f, 30f);

        // Start a coroutine to destroy the thread after the decay time
        StartCoroutine(ThreadDecayCoroutine(thread, decayTime));

    }

    private System.Collections.IEnumerator ThreadDecayCoroutine(FungalThread thread, float decayTime)
    {
        yield return new WaitForSeconds(decayTime);

        if (thread != null)
        {
            Debug.Log($"Thread between {thread.tectonA.name} and {thread.tectonB.name} has decayed after {decayTime} seconds.");
            RPC_RequestThreadDisconnect(thread.Object.Id);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EstablishLogicalConnection(NetworkId goAId, NetworkId goBId)
    {
        if (!HasStateAuthority)
            return;

        if (!Runner.TryFindObject(goAId, out var aObj) ||
            !Runner.TryFindObject(goBId, out var bObj))
            return;

        var goA = aObj.GetComponent<GridObject>();
        var goB = bObj.GetComponent<GridObject>();

        goA?.AddExternalNeighbor(goB);
        goB?.AddExternalNeighbor(goA);
    }

    public void Connect(Tecton a, Tecton b, PlayerRef player)
    {
        // only the server has the right to spaw threads, so if the caller is the client, send an rpc to the server
        if (Runner.IsServer)
        {
            if (!CanConnect(a, b))
            {
                Debug.LogWarning("Connection already exists between tectons.");
                return;
            }
            SpawnThread(a, b, player);
        }
        else
        {
            RPC_RequestThreadSpawn(a.GetComponent<NetworkObject>().Id, b.GetComponent<NetworkObject>().Id, player);
        }
    }

    // this will be sent by the client in order to have the server spawn a thread
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestThreadSpawn(NetworkId aId, NetworkId bId, PlayerRef player)
    {
        Runner.TryFindObject(aId, out var aObj);
        Runner.TryFindObject(bId, out var bObj);

        if (aObj != null && bObj != null)
        {
            SpawnThread(aObj.GetComponent<Tecton>(), bObj.GetComponent<Tecton>(), player);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestThreadDisconnect(NetworkId threadId)
    {
        // Attempt to locate the thread object by ID
        if (!Runner.TryFindObject(threadId, out var netObj))
        {
            Debug.LogError("Thread object not found.");
            return;
        }

        var thread = netObj.GetComponent<FungalThread>();
        if (thread == null)
        {
            Debug.LogError("Thread component not found.");
            return;
        }

        var tectonA = thread.tectonA?.GetComponent<Tecton>();
        var tectonB = thread.tectonB?.GetComponent<Tecton>();

        if (tectonA == null || tectonB == null)
        {
            Debug.LogError("Tectons not found.");
            return;
        }

        // Use the manager to properly disconnect the fungal connection between two tectons
        Disconnect(tectonA, tectonB);
    }

    public bool Disconnect(Tecton a, Tecton b)
    {
        var key = GetConnectionKey(a, b);

        // check if connection exists
        if (!connections.Contains(key))
        {
            Debug.LogWarning("No connection exists between these tectons.");
            return false;
        }

        // find and remove the thread
        var netA = a.GetComponent<NetworkObject>();
        var netB = b.GetComponent<NetworkObject>();

        FungalThread threadToRemove = fungalThreads.FirstOrDefault(t =>
            ( t.tectonA == netA && t.tectonB == netB ) || ( t.tectonA == netB && t.tectonB == netA ));

        if (threadToRemove != null)
        {
            var goA = threadToRemove.gridObjectA;
            var goB = threadToRemove.gridObjectB;

            if (goA != null && goB != null)
            {
                goA.RemoveExternalNeighbor(goB);
                goB.RemoveExternalNeighbor(goA);
            }
            else
            {
                Debug.LogWarning("GridObject pair not found during disconnect.");
            }

            fungalThreads.Remove(threadToRemove);
            Destroy(threadToRemove.gameObject);
            connections.Remove(key);
            return true;
        }

        // shouldn't happen but just in case
        Debug.LogError("Connection exists but no thread was found between tectons!");
        connections.Remove(key);
        return false;
    }

    private (int, int) GetConnectionKey(Tecton a, Tecton b)
    {
        int id1 = a.Id;
        int id2 = b.Id;
        return (Mathf.Min(id1, id2), Mathf.Max(id1, id2));
    }
}
