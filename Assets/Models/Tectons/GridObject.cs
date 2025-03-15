using UnityEngine;

public class GridObject : MonoBehaviour
{
    public int x;
    public int z;

    private FungusBody fungusBody;
    public FungusBody FungusBody
    {
        get { return fungusBody; }
        set
        {
            // one tekton can only have one fungus body
            if (fungusBody != null)
            {
                Debug.LogError("Tekton already has a fungus body");
                return;
            }
            fungusBody = value;
        }
    }

    public void Awake()
    {
        if (gameObject.GetComponent<Collider>() == null)
        {
            Debug.LogError("GridObject does not have Collider component");
        }
    }

    public void OnMouseDown() {
        if (FungusBodyFactory.Instance == null) {
            Debug.LogError("FungusBodyFactory not found");
            return;
        }
        FungusBodyFactory.Instance.SpawnFungusBody(this);    
    }
}
