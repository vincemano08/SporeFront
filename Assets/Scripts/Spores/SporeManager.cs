﻿using Fusion;
using UnityEngine;
using static Fusion.TickRate;
using UnityEngine.UIElements;
using System;
using UnityEngine.SocialPlatforms.Impl;

public enum SporeType
{
    Normal,
    Slow,
    Fast,
    Paralyze,
    DisableCutting
}

public class SporeManager : NetworkBehaviour
{

    [SerializeField] private GameObject sporePrefab;

    [Networked, Capacity(50)]
    private NetworkDictionary<NetworkId, NetworkId> SporeToGridMap { get; }

    [Networked] public SporeType sporeType { get; set; }

    private ScoreManager scoreManager;

    private void Start()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager not found in the scene.");
        }   
    }


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

        Vector3 position = gridObject.transform.position + 0.8f * Vector3.up;
        NetworkObject networkSpore = Runner.Spawn(sporePrefab, position, Quaternion.identity);
        GameObject spore = networkSpore.gameObject;

        networkSpore.transform.SetParent(gridObject.transform);
        networkSpore.name = "Spore";

        SporeToGridMap.Add(networkSpore.Id, gridObject.Object.Id);
        gridObject.occupantType = OccupantType.Spore;

        // Set a random spore type
        Unity.Mathematics.Random rng = new Unity.Mathematics.Random((uint)Environment.TickCount);    //Get a random number generator
        int rand1 = rng.NextInt(Enum.GetNames(typeof(SporeType)).Length); //Get a random number between 0 and the number of spore types

        sporeType = (SporeType)rand1;                                    //Set the spore type to the random number
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
            DespawnSporeAt(gridObject);
        }
        // Otherwise request the state authority to remove the spore
        else // (! Object.HasStateAuthority)
        {
            RPC_RequestRemoveSpore(gridObject.Object);
        }
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRemoveSpore(NetworkId gridObjectId)
    {
        if (Runner.TryFindObject(gridObjectId, out NetworkObject gridObj))
        {
            GridObject gridObject = gridObj.GetComponent<GridObject>();
            if (gridObject != null)
            {
                DespawnSporeAt(gridObject);
            }
        }
    }
    private void DespawnSporeAt(GridObject gridObject)
    {
        if (!Object.HasStateAuthority) return;

        // Get the associated spore from our dictionary
        NetworkId sporeId = default;
        foreach (var pair in SporeToGridMap)
        {
            if (pair.Value == gridObject.Object.Id)
            {
                sporeId = pair.Key;
                break;
            }
        }

        bool sporeFound = false;

        // Try to despawn by dictionary first
        if (sporeId != default && Runner.TryFindObject(sporeId, out NetworkObject sporeObj))
        {
            Runner.Despawn(sporeObj);
            SporeToGridMap.Remove(sporeId);
            sporeFound = true;
        }
        // Fallback to child search if needed
        else if (gridObject.transform.childCount > 0)
        {
            Transform child = gridObject.transform.GetChild(0);
            if (child != null && child.name == "Spore")
            {
                NetworkObject networkSpore = child.GetComponent<NetworkObject>();
                if (networkSpore != null)
                {
                    Runner.Despawn(networkSpore);
                    sporeFound = true;
                }
            }
        }

        // Update grid state if we found and removed a spore
        if (sporeFound)
        {
            // Reset the grid state on all clients
            RPC_UpdateGridState(gridObject.Object.Id, OccupantType.None);
        }
    }



    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateGridState(NetworkId gridId, OccupantType newState)
    {
        if (Runner.TryFindObject(gridId, out NetworkObject gridObj))
        {
            GridObject gridObject = gridObj.GetComponent<GridObject>();
            if (gridObject != null)
            {
                gridObject.occupantType = newState;
            }
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


    public void ConsumeSpores(GridObject gridObject, MoveInsect insect)
    {
        //el�re defini�lt id�be telik az elfogyaszt�s
        //ut�na a sp�r�t t�r�lj�k
        //StartCoroutine(ConsumeSporesCoroutine());
        RemoveSpore(gridObject);
        SetStateForInsect(insect);

        // Award points to the player who owns the insect
        // Use the InputAuthority of the insect to determine the owner
        if (insect.Object.HasInputAuthority || Object.HasStateAuthority)
        {
            if (scoreManager != null)
            {
                // Award 10 points for consuming a spore
                scoreManager.AddScore(insect.Object.InputAuthority, 10);
                Debug.Log($"Added 10 points to player {insect.Object.InputAuthority} for consuming a spore");
            }
            else
            {
                Debug.LogError("ScoreManager not found when trying to award points for spore consumption");
            }
        }
    }


    //It's not a good practice, but it was faster this way. Feel free to change it.
    private void SetStateForInsect(MoveInsect insect)
    {
        switch (sporeType)
        {
            case SporeType.Normal:
                insect.State = new NormalState(insect);
                break;
            case SporeType.Slow:
                insect.State = new SlowState(insect);
                break;
            case SporeType.Fast:
                insect.State = new FastState(insect);
                break;
            case SporeType.Paralyze:
                insect.State = new ParalyzedState(insect);
                break;
            case SporeType.DisableCutting:
                insect.State = new CannotCutThreadState(insect);
                break;
            default:
                Debug.LogError("Unknown spore type");
                insect.State = new NormalState(insect);
                break;
        }
    }

    public GridObject IsSporeNearby(GridObject gridObject)
    {
        //szomsz�dos gridek ellen�rz�se, hogy van-e ott sp�ra
        var neighbourGridObjects = gridObject.GetNeighbors();
        Debug.Log($"neighborGridObejcts == null {neighbourGridObjects == null}");

        foreach (var neighbour in neighbourGridObjects)
        {
            Debug.Log($"neighbour.occupantType {neighbour.occupantType}");
            if (neighbour.occupantType == OccupantType.Spore)
            {

                return neighbour;

            }
        }
        return null;
    }


}
