using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FungalThreadManager : NetworkBehaviour
{
    public static FungalThreadManager Instance { get; private set; }
    
    [SerializeField] private GameObject threadPrefab;

    private HashSet<(int, int)> connections = new HashSet<(int, int)>();
    private List<FungalThread> fungalThreads = new List<FungalThread>();

    private void Awake()
     {
        if (Instance == null)
        {
            Instance = this;

            if(threadPrefab == null)
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

        if (!source.Neighbors.Contains(target)) return false;

        var key = GetConnectionKey(source, target);
        return !connections.Contains(key);
    }

    public void Connect(Tecton a, Tecton b)
    {
        if (!CanConnect(a, b))
        {
            Debug.LogWarning("Connection already exists between tectons.");
            return;
        }

        var key = GetConnectionKey(a, b);
        connections.Add(key);

        
        NetworkObject threadNetworkObj = Runner.Spawn(threadPrefab, transform.position,transform.rotation);
        GameObject threadObj = threadNetworkObj.gameObject;
        FungalThread thread = threadObj.GetComponent<FungalThread>();
        thread.SetTectons(a, b);
        fungalThreads.Add(thread);
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
        FungalThread threadToRemove = fungalThreads.FirstOrDefault(t =>
            (t.tectonA == a && t.tectonB == b) || (t.tectonA == b && t.tectonB == a));

        if (threadToRemove != null)
        {
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
