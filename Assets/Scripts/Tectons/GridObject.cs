using System.Collections.Generic;
using UnityEngine;

public enum OccupantType {
    None,
    FungusBody,
    Insect,
    Spore
}

public class GridObject : MonoBehaviour {

    public int X { get; set; }
    public int Z { get; set; }
    public Tecton parentTecton { get; set; }
    public OccupantType occupantType { get; set; } = OccupantType.None;
    public bool IsOccupied => occupantType != OccupantType.None;

    private Renderer objectRenderer;

    private void Awake() {
        objectRenderer = GetComponent<Renderer>();
    }

    public void ChangeColor(Color newColor) {
        if ( occupantType == OccupantType.Spore ) {
            newColor = Color.magenta;
        }
        if ( objectRenderer != null )
            objectRenderer.material.color = newColor;
        else
            Debug.LogWarning("Renderer not found on " + gameObject.name);
    }

    public static GridObject GetGridObjectAt(float x, float z) {
        return GetGridObjectAt(new Vector3(x, 0, z));
    }

    public static GridObject GetGridObjectAt(Vector3 position) {
        Ray ray = new Ray(position + new Vector3(0, 10, 0), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        foreach ( var hit in hits ) {
            // Check tag to make sure we are hitting a GridObject
            if ( hit.collider.tag != "GridObject" ) continue;
            var gridObject = hit.collider.GetComponent<GridObject>();
            if ( gridObject != null ) {
                return gridObject;
            }
        }
        return null;
    }

    public IEnumerable<GridObject> GetNeighbors() {
        var neighbors = new List<GridObject>();
        var directions = new Vector2Int[] {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };
        foreach ( var direction in directions ) {
            var neighbor = GetGridObjectAt(new Vector3(X + direction.x, 0, Z + direction.y));
            if ( neighbor != null ) {
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }
}
