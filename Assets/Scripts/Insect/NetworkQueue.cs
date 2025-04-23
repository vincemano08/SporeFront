using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine; // Required for Debug.LogWarning/Error if used

// Assume GridObject is defined elsewhere as:
// public class GridObject : NetworkBehaviour { /* ... */ }

public class NetworkQueue : NetworkBehaviour
{
    [Networked] private int Head { get; set; }
    [Networked] private int Tail { get; set; }

    // Store IDs, not the behaviours themselves
    [Networked, Capacity(10)]
    private NetworkArray<NetworkBehaviourId> Buffer { get; }
    public override void Spawned()
    {
        base.Spawned();
    }

    // --- Methods now work with GridObject but store/retrieve IDs ---

    // Enqueue takes the GridObject, but stores its ID
    public void Enqueue(GridObject gridObject)
    {
        if (gridObject == null || !gridObject.Id.IsValid)
        {
            Debug.LogWarning($"Attempted to enqueue an invalid GridObject.");
            return;
        }

        if (Buffer.Length > 0)
        {
            // Store the ID of the NetworkBehaviour
            Buffer.Set(Tail % Buffer.Length, gridObject.Id);
            Tail++;
        }
        else
        {
            Debug.LogError("Enqueue failed: Buffer capacity is zero or not initialized.");
        }
    }

    // Dequeue returns the GridObject instance by finding it via its stored ID
    public GridObject Dequeue()
    {
        if (Object.HasStateAuthority && Head < Tail && Buffer.Length > 0)
        {
            NetworkBehaviourId id = Buffer.Get(Head % Buffer.Length);
            Head++;

            // IMPORTANT: Find the behaviour using the Runner and the ID
            if (Runner != null && Runner.TryFindBehaviour(id, out GridObject gridObject))
            {
                return gridObject; // Return the actual instance
            }
            else
            {
                // The object might have been despawned or ID is invalid
                Debug.LogWarning($"Could not find GridObject with ID {id} after dequeueing. It might have been despawned.");
                return null;
            }
        }
        return null; // Return null if queue empty, no authority, or buffer invalid
    }

    // Peek returns the GridObject instance without removing its ID
    public GridObject Peek()
    {
        if (Head < Tail && Buffer.Length > 0)
        {
            NetworkBehaviourId id = Buffer.Get(Head % Buffer.Length);

            // IMPORTANT: Find the behaviour using the Runner and the ID
            if (Runner != null && Runner.TryFindBehaviour(id, out GridObject gridObject))
            {
                return gridObject; // Return the actual instance
            }
            else
            {
                // The object might have been despawned or ID is invalid
                Debug.LogWarning($"Could not find GridObject with ID {id} during peek. It might have been despawned.");
                return null;
            }
        }
        return null; // Return null if queue empty or buffer invalid
    }

    internal void Enqueue(List<GridObject> p)
    {
        foreach (GridObject gridObject in p) 
        {
            Enqueue(gridObject);
        }
    }

    public int Count => Tail - Head;

    public bool IsEmpty => Head >= Tail;
}