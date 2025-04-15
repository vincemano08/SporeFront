using Fusion;
using System.Collections.Generic;
using UnityEngine;

public enum OccupantType
{
    None,
    FungusBody,
    Insect,
    Spore
}

public class GridObject : NetworkBehaviour
{

    [Networked] public int X { get; set; }
    [Networked] public int Z { get; set; }
    public Tecton parentTecton { get; set; }
    [Networked] public OccupantType occupantType { get; set; } = OccupantType.None;
    public bool IsOccupied => occupantType != OccupantType.None;

    [Networked, Capacity(4)] // Initial capacity
    private NetworkLinkedList<NetworkId> ExternalNeighborIds => default;

    private Renderer objectRenderer;

    private List<GridObject> _cachedNeighbors = new List<GridObject>();

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
    }


    public void ChangeColor(Color newColor)
    {
        if (occupantType == OccupantType.Spore)
        {
            newColor = Color.magenta;
        }
        if (objectRenderer != null)
            objectRenderer.material.color = newColor;
        else
            Debug.LogWarning("Renderer not found on " + gameObject.name);
    }

    public static GridObject GetGridObjectAt(float x, float z) =>
        GetGridObjectAt(new Vector3(x, 0, z));

    public static GridObject GetGridObjectAt(Vector3 position)
    {
        Ray ray = new Ray(position + new Vector3(0, 10, 0), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        foreach (var hit in hits)
        {
            // Check tag to make sure we are hitting a GridObject
            if (hit.collider.tag != "GridObject") continue;
            var gridObject = hit.collider.GetComponent<GridObject>();
            if (gridObject != null)
            {
                return gridObject;
            }
        }
        return null;
    }

    public void AddExternalNeighbor(GridObject neighbor)
    {
        if (!HasStateAuthority)
        {
            Debug.LogError("Cannot add external neighbor directly. Must be called on State Authority.");
            return;
        }

        if (neighbor == null || neighbor.Object == null || !neighbor.Object.Id.IsValid) return; // Invalid neighbour

        // Check if the neighbor is already in the list
        bool found = false;
        foreach (var id in ExternalNeighborIds)
        {
            if (id == neighbor.Object.Id)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            ExternalNeighborIds.Add(neighbor.Object.Id);
            Debug.Log($"Added external neighbor {neighbor.Object.Id} to {this.Object.Id}");
        }
    }

    public void RemoveExternalNeighbor(GridObject neighbor)
    {
        if (!HasStateAuthority)
        {
            Debug.LogError("Cannot remove external neighbor directly. Must be called on State Authority.");
            return;
        }
        if (neighbor == null || neighbor.Object == null || !neighbor.Object.Id.IsValid) return;

        // Check if the neighbor is in the list
        bool found = false;
        foreach (var id in ExternalNeighborIds)
        {
            if (id == neighbor.Object.Id)
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            Debug.LogWarning($"Neighbor {neighbor.Object.Id} not found in external neighbors of {this.Object.Id}");
            return;
        }

        ExternalNeighborIds.Remove(neighbor.Object.Id);
    }

    public IEnumerable<GridObject> GetNeighbors()
    {

        if (_cachedNeighbors.Count > 0)
        {
            // If we already have cached neighbors, return them
            return _cachedNeighbors;
        }

        // Use the cached neighbors
        _cachedNeighbors.Clear();

        // 1. Közvetlen (Adjacent) szomszédok
        var directions = new Vector2Int[] {
            new Vector2Int(0, 1), // Fel
            new Vector2Int(1, 0), // Jobbra
            new Vector2Int(0, -1), // Le
            new Vector2Int(-1, 0)  // Balra
        };

        foreach (var direction in directions)
        {
            // A GetGridObjectAt használata helyett hatékonyabb lehet, ha van egy központi
            // rendszer (pl. GridManager), ami ismeri az összes GridObject-et X, Z alapján.
            // Itt most maradunk a Raycast-os megoldásnál a példa kedvéért.
            var neighbor = GetGridObjectAt(new Vector3(X + direction.x, 0, Z + direction.y));

            // Gyõzõdjünk meg róla, hogy a talált szomszéd érvényes a hálózaton
            if (neighbor != null && neighbor.Object != null && neighbor.Object.IsValid)
            {
                _cachedNeighbors.Add(neighbor);
            }
        }

        // 2. Külsõ (External) szomszédok hozzáadása a szinkronizált listából
        if (Runner != null && ExternalNeighborIds.Count > 0) // Runner ellenõrzése fontos
        {
            foreach (NetworkId neighborId in ExternalNeighborIds)
            {
                // Próbáljuk megkeresni a NetworkObject-et az ID alapján
                if (Runner.TryFindObject(neighborId, out NetworkObject networkObject))
                {
                    // Sikeresen megtaláltuk, próbáljuk GridObject-té alakítani
                    GridObject externalNeighbor = networkObject.GetComponent<GridObject>();
                    if (externalNeighbor != null && externalNeighbor != this) // Ne adjuk hozzá önmagát
                    {
                        // Opcionális: Ellenõrizzük, hogy a közvetlen szomszédok között nem szerepel-e már
                        // (HashSet helyett List-et használunk, így Contains lassabb lehet)
                        if (!_cachedNeighbors.Contains(externalNeighbor))
                        {
                            _cachedNeighbors.Add(externalNeighbor);
                        }
                    }
                }
                // Ha nem találtuk (pl. az objektum már megszûnt), akkor nem csinálunk semmit.
                // Fontos lehet egy mechanizmus, ami eltávolítja az érvénytelen ID-kat a listából idõnként.
            }
        }

        return _cachedNeighbors;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestAddExternalNeighbor(NetworkId neighborId)
    {
        if (Runner.TryFindObject(neighborId, out var networkObject))
        {
            var neighborGridObject = networkObject.GetComponent<GridObject>();
            if (neighborGridObject != null)
            {
                AddExternalNeighbor(neighborGridObject);
            }
        }
    }
}
