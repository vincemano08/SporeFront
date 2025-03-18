using UnityEngine;
using UnityEngine.UIElements;

public class GridObject : MonoBehaviour
{
    public int x;
    public int z;


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
    }

    private Mesh CreateCubeMesh()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = cube.GetComponent<MeshFilter>().mesh;
        Destroy(cube);
        return mesh;
    }



}
