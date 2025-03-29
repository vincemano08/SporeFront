using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FungalThread : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public Tecton tectonA { get; set; }
    public Tecton tectonB { get; set; }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if(lineRenderer == null)
        {
            Debug.LogError("LineRenderer component not found on the object.");
        }
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.3f;
        lineRenderer.positionCount = 2;
    }

    public void SetTectons(Tecton a, Tecton b)
    {
        tectonA = a;
        tectonB = b;
        UpdateLineRenderer();
    }

    public void UpdateLineRenderer()
    {
        if (tectonA == null || tectonB == null)
        {
            Debug.LogError("Tectons not set for the fungal thread.");
            return;
        }
        var closestPair = FindClosestGridObjectPair(tectonA, tectonB);

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
        float minDistance = float.MaxValue;
        var gridObjectsFromA = a.GridObjects.ToList();
        var gridObjectsFromB = b.GridObjects.ToList();

        // First pass: find the minimum distance between any grid objects of the two tectons
        foreach (var goA in gridObjectsFromA)
        {
            foreach (var goB in gridObjectsFromB)
            {
                float distance = Vector3.Distance(goA.transform.position, goB.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }


        // Second pass: collect all pairs that are within the minimum distance + threshold
        List<GridObject> candidatesFromA = new List<GridObject>();
        List<GridObject> candidatesFromB = new List<GridObject>();

        foreach (var goA in gridObjectsFromA)
        {
            foreach (var goB in gridObjectsFromB)
            {
                float distance = Vector3.Distance(goA.transform.position, goB.transform.position);
                if (distance <= minDistance)
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
            float dist = Vector3.Distance(goA.transform.position, avgPosA);
            if (dist < closestDistA)
            {
                closestDistA = dist;
                closestFromA = goA;
            }
        }

        foreach (var goB in candidatesFromB)
        {
            float dist = Vector3.Distance(goB.transform.position, avgPosB);
            if (dist < closestDistB)
            {
                closestDistB = dist;
                closestFromB = goB;
            }
        }

        return (closestFromA, closestFromB);
    }
}
