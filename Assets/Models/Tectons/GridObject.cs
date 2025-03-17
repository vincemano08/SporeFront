using UnityEngine;
using UnityEngine.UIElements;

public class GridObject : MonoBehaviour
{
    public int x;
    public int z;





    public void Awake()
    {
        if (gameObject.GetComponent<Collider>() == null)
        {
            Debug.LogError("GridObject does not have Collider component");
        }


    }


}
