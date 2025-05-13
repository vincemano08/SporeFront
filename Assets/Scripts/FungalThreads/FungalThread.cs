using Fusion;
using NUnit.Framework;
using System;
using System.Collections;
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

    private NetworkObject lastTectonA;
    private NetworkObject lastTectonB;

    //This variable is used when the insect tries to cross the fungal thread
    //The insect can only cross the fungal thread if it is fully developed
    private bool isFullyDeveloped = false;
    public bool IsFullyDeveloped => isFullyDeveloped;

    [Networked]
    private float GrowthProgress { get; set; }

    public GridObject gridObjectA { get; private set; }
    public GridObject gridObjectB { get; private set; }

    [SerializeField] private AnimationCurve widthCurve;

    private Material _materialInstance;
    [Networked, OnChangedRender(nameof(OnColorChanged))] public Color NetworkedColor { get; set; }

    private Coroutine growthCoroutine;

    [Networked] public PlayerRef PlayerReference { get; set; }

    public void OnColorChanged()
    {
        GetComponent<Renderer>().material.SetColor("_Color", NetworkedColor);
    }

    public override void Spawned()
    {
        base.Spawned();
        IsSpawned = true;

        OnColorChanged();
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer component not found on the object.");
            return;
        }
        lineRenderer.positionCount = 2;
        lineRenderer.widthCurve = widthCurve;
    }
    private void Update()
    {
        if (!IsSpawned)
            return;

        // Update the line renderer based on GrowthProgress
        if (GrowthProgress > 0f && GrowthProgress < 1f)
        {
            if (gridObjectA != null && gridObjectB != null)
            {
                Vector3 startPos = gridObjectA.transform.position;
                Vector3 endPos = gridObjectB.transform.position;
                Vector3 currentEndPos = Vector3.Lerp(startPos, endPos, GrowthProgress);

                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, currentEndPos);
            }
        }

        // Manuálisan ellenõrizzük, hogy megváltoztak-e a networked változók
        if (tectonA != lastTectonA || tectonB != lastTectonB)
        {
            UpdateLineRenderer();
            lastTectonA = tectonA;
            lastTectonB = tectonB;
        }
        // Check if the tectonA and tectonB still has a FungusBody (that has the same InputAuthority as the Thread, check if the FUngusbody has inputauthority)
        // if not, than destroy the thread
        if (tectonA != null && tectonB != null)
        {

            var tectonComponentA = tectonA.GetComponent<Tecton>();
            var tectonComponentB = tectonB.GetComponent<Tecton>();

            if (tectonComponentA != null && tectonComponentB != null)
            {
                // Try to find FungusBody through NetworkId (FungusId)
                NetworkObject fungusNetObjA = null;
                NetworkObject fungusNetObjB = null;

                // Only try to find the objects if the IDs are valid
                if (tectonComponentA.FungusId.IsValid)
                    Runner.TryFindObject(tectonComponentA.FungusId, out fungusNetObjA);

                if (tectonComponentB.FungusId.IsValid)
                    Runner.TryFindObject(tectonComponentB.FungusId, out fungusNetObjB);

                // Log for debugging
                //Debug.Log($"FungusNetObjA: {fungusNetObjA != null}, FungusNetObjB: {fungusNetObjB != null}");

                FungusBody fungusBodyA = fungusNetObjA?.GetComponent<FungusBody>();
                FungusBody fungusBodyB = fungusNetObjB?.GetComponent<FungusBody>();

                // Only validate if we can find at least one FungusBody
                if (( fungusBodyA == null && fungusBodyB == null ) ||
                    ( fungusBodyA != null && fungusBodyA.Object.InputAuthority != PlayerReference &&
                     fungusBodyB != null && fungusBodyB.Object.InputAuthority != PlayerReference ))
                {
                    Debug.Log("The thread has no valid access to its parent FungusBodies. Despawning thread.");
                    if (Object.HasStateAuthority)
                    {
                        Runner.Despawn(Object);
                    }
                }
            }

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
            RPC_SetTectonNames(a.name, b.name);
            //RPC_SetTectons(a, b);
            UpdateLineRenderer();
            if (growthCoroutine != null)
            {
                StopCoroutine(growthCoroutine);
            }
            growthCoroutine = StartCoroutine(GrowThreadOverTime(8f));
        }
        else
        {
            RPC_SetTectons(a, b);
        }
    }

    private IEnumerator GrowThreadOverTime(float duration)
    {
        if (tectonA == null || tectonB == null)
        {
            Debug.LogError("Tectons not set for the fungal thread.");
            yield break;
        }

        // Find the closest grid objects for the line endpoints
        var closestPair = FindClosestGridObjectPair(tectonA, tectonB);
        if (closestPair.Item1 == null || closestPair.Item2 == null)
        {
            Debug.LogError("Closest grid object pair not found for rendering.");
            yield break;
        }

        gridObjectA = closestPair.Item1;
        gridObjectB = closestPair.Item2;

        Vector3 startPos = gridObjectA.transform.position;
        Vector3 endPos = gridObjectB.transform.position;

        var tectonAComponent = tectonA.GetComponent<Tecton>();
        var tectonBComponent = tectonB.GetComponent<Tecton>();

        if (( tectonAComponent != null && tectonAComponent.TectonType == TectonType.ThreadGrowthBoost ) ||
        ( tectonBComponent != null && tectonBComponent.TectonType == TectonType.ThreadGrowthBoost ))
        {
            duration /= 2f; // Grow twice as fast
            Debug.Log("ThreadGrowthBoost detected. Growth duration halved.");
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            //Note: Here we dont use Runner.DeltaTime!! The elapsedTime will still be the same on all clients
            elapsedTime += Time.deltaTime;
            GrowthProgress = elapsedTime / duration;

            // Interpolate the second endpoint of the line
            Vector3 currentEndPos = Vector3.Lerp(startPos, endPos, GrowthProgress);

            lineRenderer.SetPosition(0, startPos); // Start position remains fixed
            lineRenderer.SetPosition(1, currentEndPos); // Gradually move the end position

            yield return null;
        }

        // Ensure the line fully reaches the endpoint
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        isFullyDeveloped = true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetTectonNames(string nameA, string nameB)
    {
        tectonA.name = nameA;
        tectonB.name = nameB;
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetTectons(NetworkObject a, NetworkObject b)
    {
        tectonA = a;
        tectonB = b;
        tectonA.name = a.name;
        tectonB.name = b.name;

        // Start the growth coroutine on all clients
        if (growthCoroutine != null)
        {
            StopCoroutine(growthCoroutine);
        }

        growthCoroutine = StartCoroutine(GrowThreadOverTime(10f));

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
        //The name of the Networkobjects are synchronized using RPC
        var closestPair = FindClosestGridObjectPair(tectonA, tectonB);

        if (closestPair.Item1 != null && closestPair.Item2 != null)
        {
            Vector3 startPos = closestPair.Item1.transform.position;
            Vector3 endPos = closestPair.Item2.transform.position;

            Vector3 currentEndPos = Vector3.Lerp(startPos, endPos, GrowthProgress);

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, currentEndPos);
        }
        else
        {
            Debug.LogWarning("Closest grid object pair not found for rendering.");
        }
        gridObjectA = closestPair.Item1;
        gridObjectB = closestPair.Item2;
    }

    public (GridObject, GridObject) FindClosestGridObjectPair(NetworkObject netA, NetworkObject netB)
    {
        if (netA == null || netB == null)
        {
            Debug.LogError("One or both of the tectons are null.");
            return (null, null);
        }
        string aName = netA.name;
        string bName = netB.name;
        GameObject gridParent = GameObject.Find("Grid Parent");
        HashSet<GridObject> gridObjectsFromA = new HashSet<GridObject>();
        HashSet<GridObject> gridObjectsFromB = new HashSet<GridObject>();

        foreach (Transform child in gridParent.transform) // Changed from 'var' to 'Transform'
        {
            if (child.name == aName) // 'child' is now correctly typed as 'Transform'
            {
                //loop through the children of the 'child' variable and fill the gridObjectsFroma Hashset with its children
                foreach (Transform grandChild in child)
                {
                    GridObject gridObject = grandChild.GetComponent<GridObject>();
                    if (gridObject != null)
                    {
                        gridObjectsFromA.Add(gridObject);
                    }
                }
            }
            if (child.name == bName) // 'child' is now correctly typed as 'Transform'
            {
                //loop through the children of the 'child' variable and fill the gridObjectsFroma Hashset with its children
                foreach (Transform grandChild in child)
                {
                    GridObject gridObject = grandChild.GetComponent<GridObject>();
                    if (gridObject != null)
                    {
                        gridObjectsFromB.Add(gridObject);
                    }
                }
            }
        }

        if (gridObjectsFromA == null || gridObjectsFromB == null) // Add a null check to ensure 'gridObjectsFromA' and 'gridObjectsFromB' are assigned
        {
            Debug.LogError("One or both of the tectons could not be found in the grid.");
            return (null, null);
        }

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
                float distance = ( goA.transform.position - goB.transform.position ).sqrMagnitude;
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
                float distance = ( goA.transform.position - goB.transform.position ).sqrMagnitude;
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
            float dist = ( goA.transform.position - avgPosA ).sqrMagnitude;
            if (dist < closestDistA)
            {
                closestDistA = dist;
                closestFromA = goA;
            }
        }

        foreach (var goB in candidatesFromB)
        {
            float dist = ( goB.transform.position - avgPosB ).sqrMagnitude;
            if (dist < closestDistB)
            {
                closestDistB = dist;
                closestFromB = goB;
            }
        }

        return (closestFromA, closestFromB);
    }
}
