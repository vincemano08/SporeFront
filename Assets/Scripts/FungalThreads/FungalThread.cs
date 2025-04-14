using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FungalThread : NetworkBehaviour
{
    private LineRenderer lineRenderer;
    private bool IsSpawned = false;
    [Networked]
    public NetworkObject tectonA { get; set; }
    [Networked]
    public NetworkObject tectonB { get; set; }

    private NetworkObject  lastTectonA;
    private NetworkObject lastTectonB;

    public override void Spawned()
    {
        base.Spawned();
        IsSpawned = true;
    }



    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if(lineRenderer == null)
        {
            Debug.LogError("LineRenderer component not found on the object.");
            return;
        }
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.3f;
        lineRenderer.positionCount = 2;
    }
    private void Update()
    {
        if (!IsSpawned)
            return;
        // Manuálisan ellenõrizzük, hogy megváltoztak-e a networked változók
        if (tectonA != lastTectonA || tectonB != lastTectonB)
        {
            UpdateLineRenderer();
            lastTectonA = tectonA;
            lastTectonB = tectonB;
        }
    }

    public void SetTectons(NetworkObject a, NetworkObject b)
    {
        if (a == null || b == null)
        {
            Debug.LogError("SetTectons called with null tecton(s)");
            return;
        }
        var netObjA = a.GetComponent<NetworkObject>();
        var netObjB = b.GetComponent<NetworkObject>();

        Debug.Log($"Tecton A has NetworkObject: {netObjA != null})");
        Debug.Log($"Tecton B has NetworkObject: {netObjB != null})");

        if (Object.HasStateAuthority)
        {
            tectonA = a;
            tectonB = b;
            //RPC_SetTectons(a, b);
            UpdateLineRenderer();
        }
        else 
        {
            RPC_SetTectons(a, b);
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetTectons(NetworkObject a, NetworkObject b)
    {
        tectonA = a;
        tectonB = b;
        UpdateLineRenderer();
        RPC_UpdateLineRendererOnClients();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateLineRendererOnClients()
    {
        UpdateLineRenderer();
    }

    public void UpdateLineRenderer()
    {
        if (tectonA == null || tectonB == null)
        {
            Debug.LogError("Tectons not set for the fungal thread.");
            return;
        }
        var closestPair = FindClosestGridObjectPair(tectonA.gameObject.GetComponent<Tecton>(), tectonB.gameObject.GetComponent<Tecton>());

        if (closestPair.Item1 != null && closestPair.Item2 != null)
        {
            Vector3 startPos = closestPair.Item1.transform.position;
            Vector3 endPos = closestPair.Item2.transform.position;

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
        }
    }

    private (GridObject, GridObject) FindClosestGridObjectPair(Tecton a, Tecton b)
    {
        if(a == null || b == null)
        {
            Debug.LogError("One or both of the tectons are null.");
            return (null, null);
        }

        var gridObjectsFromA = a.GridObjects;
        var gridObjectsFromB = b.GridObjects;

        if (gridObjectsFromA.Count == 0 || gridObjectsFromB.Count == 0)
        {
            Debug.LogError("One or both of the tectons have no grid objects.");
            return (null, null);
        }

        float minDistance = float.MaxValue;

        // First pass: find the minimum distance between any two grid objects
        foreach (var goA in gridObjectsFromA)
        {
            foreach (var goB in gridObjectsFromB)
            {
                float distance = (goA.transform.position - goB.transform.position).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }

        float exactMinDistance = minDistance;

        // Second pass: collect all pairs that are within the minimum distance + threshold
        List<GridObject> candidatesFromA = new List<GridObject>();
        List<GridObject> candidatesFromB = new List<GridObject>();

        foreach (var goA in gridObjectsFromA)
        {
            foreach (var goB in gridObjectsFromB)
            {
                float distance = (goA.transform.position - goB.transform.position).sqrMagnitude;
                if (distance <= exactMinDistance + 0.0001f)
                {
                    candidatesFromA.Add(goA);
                    candidatesFromB.Add(goB);
                }
            }
        }

        // Calculate the average position of candidates from each tecton
        Vector3 avgPosA = Vector3.zero;
        Vector3 avgPosB = Vector3.zero;

        foreach (var candidate in candidatesFromA)
        {
            avgPosA += candidate.transform.position;
        }

        foreach (var candidate in candidatesFromB)
        {
            avgPosB += candidate.transform.position;
        }

        avgPosA /= candidatesFromA.Count;
        avgPosB /= candidatesFromB.Count;

        // Find the grid objects closest to the average positions
        GridObject closestFromA = null;
        GridObject closestFromB = null;
        float closestDistA = float.MaxValue;
        float closestDistB = float.MaxValue;

        foreach (var goA in candidatesFromA)
        {
            float dist = (goA.transform.position - avgPosA).sqrMagnitude;
            if (dist < closestDistA)
            {
                closestDistA = dist;
                closestFromA = goA;
            }
        }

        foreach (var goB in candidatesFromB)
        {
            float dist = (goB.transform.position - avgPosB).sqrMagnitude;
            if (dist < closestDistB)
            {
                closestDistB = dist;
                closestFromB = goB;
            }
        }

        return (closestFromA, closestFromB);
    }
}
