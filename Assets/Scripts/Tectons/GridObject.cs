using Fusion;
using System.Collections.Generic;
using System.Linq;
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
    public HashSet<GridObject> ExternalNeighbors { get; set; }

    [Networked, Capacity(4)] // Initial capacity
    private NetworkLinkedList<NetworkId> ExternalNeighborIds => default;

    private Renderer objectRenderer;

    private List<GridObject> _cachedNeighbors = new List<GridObject>();

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        ExternalNeighbors = new HashSet<GridObject> { };
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
            RPC_RequestAddExternalNeighbor(neighbor.Object.Id);
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

            // Clear neighbor cache to ensure GetNeighbors() returns fresh data
            _cachedNeighbors.Clear();
            Debug.Log($"Added external neighbor {neighbor.Object.Id} to {this.Object.Id}");
        }
    }

    public void RemoveExternalNeighbor(GridObject neighbor)
    {
        if (!HasStateAuthority)
        {
            RPC_RequestRemoveExternalNeighbor(neighbor.Object.Id);
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

        // Clear cached neighbors so GetNeighbors() will rebuild the list next time
        _cachedNeighbors.Clear();
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

        // 1. K�zvetlen (Adjacent) szomsz�dok
        var directions = new Vector2Int[] {
            new Vector2Int(0, 1), // Fel
            new Vector2Int(1, 0), // Jobbra
            new Vector2Int(0, -1), // Le
            new Vector2Int(-1, 0)  // Balra
        };

        foreach (var direction in directions)
        {
            // A GetGridObjectAt haszn�lata helyett hat�konyabb lehet, ha van egy k�zponti
            // rendszer (pl. GridManager), ami ismeri az �sszes GridObject-et X, Z alapj�n.
            // Itt most maradunk a Raycast-os megold�sn�l a p�lda kedv��rt.
            var neighbor = GetGridObjectAt(new Vector3(X + direction.x, 0, Z + direction.y));

            // Gy�z�dj�nk meg r�la, hogy a tal�lt szomsz�d �rv�nyes a h�l�zaton
            if (neighbor != null && neighbor.Object != null && neighbor.Object.IsValid)
            {
                _cachedNeighbors.Add(neighbor);
            }
        }

        // 2. K�ls� (External) szomsz�dok hozz�ad�sa a szinkroniz�lt list�b�l
        if (Runner != null && ExternalNeighborIds.Count > 0) // Runner ellen�rz�se fontos
        {
            foreach (NetworkId neighborId in ExternalNeighborIds)
            {
                // Pr�b�ljuk megkeresni a NetworkObject-et az ID alapj�n
                if (Runner.TryFindObject(neighborId, out NetworkObject networkObject))
                {
                    // Sikeresen megtal�ltuk, pr�b�ljuk GridObject-t� alak�tani
                    GridObject externalNeighbor = networkObject.GetComponent<GridObject>();
                    if (externalNeighbor != null && externalNeighbor != this) // Ne adjuk hozz� �nmag�t
                    {
                        // Opcion�lis: Ellen�rizz�k, hogy a k�zvetlen szomsz�dok k�z�tt nem szerepel-e m�r
                        // (HashSet helyett List-et haszn�lunk, �gy Contains lassabb lehet)
                        if (!_cachedNeighbors.Contains(externalNeighbor))
                        {
                            _cachedNeighbors.Add(externalNeighbor);
                        }
                    }
                }
                // Ha nem tal�ltuk (pl. az objektum m�r megsz�nt), akkor nem csin�lunk semmit.
                // Fontos lehet egy mechanizmus, ami elt�vol�tja az �rv�nytelen ID-kat a list�b�l id�nk�nt.
            }
        }

        return _cachedNeighbors;
    }

    [Rpc(RpcSources.Proxies, RpcTargets.StateAuthority)]
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
    [Rpc(RpcSources.Proxies, RpcTargets.StateAuthority)]
    public void RPC_RequestRemoveExternalNeighbor(NetworkId neighborId)
    {
        if (Runner.TryFindObject(neighborId, out var networkObject))
        {
            var neighborGridObject = networkObject.GetComponent<GridObject>();
            if (neighborGridObject != null)
            {
                RemoveExternalNeighbor(neighborGridObject);
            }
        }
    }
}
