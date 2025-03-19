using UnityEngine;
using UnityEngine.UIElements;

public class GridObject : MonoBehaviour
{
    public int x;
    public int z;
    public Tecton parentTecton;
    private Renderer objectRenderer;


    public void Awake()
    {
        if (gameObject.GetComponent<MeshRenderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }
        if (gameObject.GetComponent<MeshFilter>() == null)
        {
            gameObject.AddComponent<MeshFilter>().mesh = CreateCubeMesh();
        }
        if (gameObject.GetComponent<Collider>() == null)
        {
            Debug.LogError("GridObject does not have Collider component");
        }
        // Get the Renderer component (Make sure the GameObject has one!)
        objectRenderer = GetComponent<Renderer>();

        // Optional: Set a default color
        ChangeColor(Color.green);
    }

    internal void ChangeColor(Color newColor)
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = newColor;
        }
        else
        {
            Debug.LogWarning("Renderer not found on " + gameObject.name);
        }

    }

    private Mesh CreateCubeMesh()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = cube.GetComponent<MeshFilter>().mesh;
        Destroy(cube);
        return mesh;
    }

    public void OnMouseDown()
    {
        if (FungusBodyFactory.Instance == null)
        {
            Debug.LogError("FungusBodyFactory not found");
            return;
        }
        FungusBodyFactory.Instance.SpawnFungusBody(parentTecton);
    }



}
