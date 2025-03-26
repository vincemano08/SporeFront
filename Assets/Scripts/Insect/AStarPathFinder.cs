using System.Collections.Generic;
using UnityEngine;

public class AStarPathFinder : MonoBehaviour
{
    private static readonly Vector2Int[] Directions = {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0)
    };

    public static List<GridObject> FindPath(GridObject start, GridObject target)
    {
        var openSet = new PriorityQueue<GridObject>();
        var cameFrom = new Dictionary<GridObject, GridObject>();

        var gScore = new Dictionary<GridObject, float> { [start] = 0 };
        var fScore = new Dictionary<GridObject, float> { [start] = Heuristic(start, target) };

        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == target) return ReconstructPath(cameFrom, current);

            foreach (var neighbor in current.GetNeighbors())
            {
                if (neighbor.IsOccupied) continue;

                float tentativeGScore = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Heuristic(neighbor, target);

                    if (!openSet.Contains(neighbor)) openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }
        return null;
    }

    private static float Heuristic(GridObject a, GridObject b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Z - b.Z); // Manhattan Distance
    }

    private static List<GridObject> ReconstructPath(Dictionary<GridObject, GridObject> cameFrom, GridObject current)
    {
        var totalPath = new List<GridObject> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        return totalPath;
    }
}
