using UnityEngine;

public class GridObject : MonoBehaviour
{
    public int x;
    public int z;

    public void OnMouseDown() {
        Debug.Log("Clikced on a tekton, spawning a fungus body");
        FungusBodyFactory.Instance.SpawnFungusBody(this);    
    }

}
