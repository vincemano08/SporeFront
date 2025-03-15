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

    // Reference to the component handling spore production
    private FungusReproduction reproduction;
    private void Awake()
    {
        reproduction = GetComponent<FungusReproduction>();
        if (reproduction == null)
        {
            Debug.LogError("FungusReproduction komponens nem található!");
        }
    }

    // Public method to trigger spore release
    public void TriggerSporeRelease()
    {
        reproduction.TriggerSporeRelease();
    }
    private void OnMouseDown()
    {
        // Trigger spore release on left-click.
        TriggerSporeRelease();
    }

}
