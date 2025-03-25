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
        GridObject closestFromA = null;
        GridObject closestFromB = null;

        var gridObjectsFromA = a.GridObjects.ToList();
        var gridObjectsFromB = b.GridObjects.ToList();

        // find the closest pair of grid objects (for now its kinda ugly since it doesnt really choose the one in the middle but instead last found shortest)
        foreach (var goA in gridObjectsFromA)
        {
            foreach (var goB in gridObjectsFromB)
            {
                float distance = Vector3.Distance(goA.transform.position, goB.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestFromA = goA;
                    closestFromB = goB;
                }
            }
        }

        return (closestFromA, closestFromB);
    }
}
