using Fusion;
using UnityEngine;
using static Fusion.TickRate;
using UnityEngine.UIElements;

public class SporeManager : NetworkBehaviour
{

    [SerializeField] private GameObject sporePrefab;

    public void SpawnSpore(GridObject gridObject)
    {
        if (gridObject == null)
        {
            Debug.LogError("Invalid grid object");
            return;
        }

        if (sporePrefab == null)
        {
            Debug.LogError("Spore prefab is not assigned");
            return;
        }

        Vector3 position = gridObject.transform.position + Vector3.up;
        NetworkObject networkSpore = Runner.Spawn(sporePrefab, position, Quaternion.identity);
        GameObject spore = networkSpore.gameObject;

        spore.transform.SetParent(gridObject.transform);
        spore.name = "Spore";
        if (Object.HasStateAuthority)
        {
            RPC_SetSporeParent(networkSpore, gridObject.Object);
        }
        gridObject.occupantType = OccupantType.Spore;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetSporeParent(NetworkObject sporeObj, NetworkObject parentObj)
    {
        if (sporeObj != null && parentObj != null)
        {
            sporeObj.transform.SetParent(parentObj.transform);
        }
    }

    public void RemoveSpore(GridObject gridObject)
    {
        if (gridObject == null)
        {
            Debug.LogError("Invalid grid object");
            return;
        }

       

        // If we have state authority, call the RPC directly
        if (Object.HasStateAuthority)
        {
            RPC_RemoveSpore(gridObject.Object);
        }
        // Otherwise request the state authority to remove the spore
        else // (! Object.HasStateAuthority)
        {
            RPC_RequestRemoveSpore(gridObject.Object);
        }
        
        



       
    }
    

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestRemoveSpore(NetworkObject gridObjectNetObj)
    {
        if (gridObjectNetObj == null) return;

        var gridObject = gridObjectNetObj.GetComponent<GridObject>();
        if (gridObject != null)
        {
            RPC_RemoveSpore(gridObjectNetObj);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RemoveSpore(NetworkObject gridObjectNetObj)
    {
        if (gridObjectNetObj == null) return;

        var gridObject = gridObjectNetObj.GetComponent<GridObject>();
        if (gridObject == null) return;

        gridObject.occupantType = OccupantType.None;

        // Find and despawn the networked spore object
        if (gridObject.transform.childCount > 0)
        {
            Transform child = gridObject.transform.GetChild(0);
            if (child != null)
            {
                // Try to despawn if it's a NetworkObject
                NetworkObject networkSpore = child.GetComponent<NetworkObject>();
                if (networkSpore != null && Runner != null && Object.HasStateAuthority)
                {
                    Runner.Despawn(networkSpore);
                }
                else if (child.gameObject != null)
                {
                    // Fallback to Destroy if not a NetworkObject or we don't have authority
                    Destroy(child.gameObject);
                }
            }
        }
        else
        {
            Debug.LogWarning("No spore found to remove on grid object");
        }
    }

    public void ConsumeSpores(GridObject gridObject)
    {
        //el�re defini�lt id�be telik az elfogyaszt�s
        //ut�na a sp�r�t t�r�lj�k
        //StartCoroutine(ConsumeSporesCoroutine());
        RemoveSpore(gridObject);
    }
    public GridObject IsSporeNearby(GridObject gridObject)
    {
        //szomsz�dos gridek ellen�rz�se, hogy van-e ott sp�ra
        var neighbourGridObjects = gridObject.GetNeighbors();
        
        foreach (var neighbour in neighbourGridObjects)
        {
            if (neighbour.occupantType == OccupantType.Spore)
            {
                return neighbour;
            }
        }
        return null;
    }


}
