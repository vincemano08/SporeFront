using UnityEngine;

public class FungusBody : MonoBehaviour
{

    private Tecton tecton;
    public Tecton Tecton
    {
        get { return tecton; }
        set
        {
            if (tecton != null)
            {
                Debug.LogError("Fungus body already has a tekton");
                return;
            }
            tecton = value;
        }
    }

    // Reference to the component handling spore production
    private FungusReproduction reproduction;
    private void Awake()
    {
        reproduction = GetComponent<FungusReproduction>();
        if (reproduction == null)
        {
            Debug.LogError("FungusReproduction component not found!");
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
