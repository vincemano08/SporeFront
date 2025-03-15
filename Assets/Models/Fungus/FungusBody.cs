using UnityEngine;

public class FungusBody : MonoBehaviour
{

    private GridObject gridObject;
    public GridObject GridObject
    {
        get { return gridObject; }
        set
        {
            if (gridObject != null)
            {
                Debug.LogError("Fungus body already has a tekton");
                return;
            }
            gridObject = value;
        }
    }

}
