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
    /// <summary>
    /// Initializes the fungus reproduction component by retrieving it from the current GameObject.
    /// </summary>
    /// <remarks>
    /// Called during the object's initialization phase. If the FungusReproduction component is not found,
    /// an error is logged to signal a potential setup issue.
    /// </remarks>
    private void Awake()
    {
        reproduction = GetComponent<FungusReproduction>();
        if (reproduction == null)
        {
            Debug.LogError("FungusReproduction component not found!");
        }
    }

    /// <summary>
    /// Initiates the spore release process for the fungus.
    /// </summary>
    /// <remarks>
    /// Delegates to the associated FungusReproduction component to handle the actual spore release.
    /// Typically invoked as a result of user interaction, such as a mouse click.
    /// </remarks>
    public void TriggerSporeRelease()
    {
        reproduction.TriggerSporeRelease();
    }
    /// <summary>
    /// Handles mouse click events on the GameObject by triggering spore release.
    /// </summary>
    /// <remarks>
    /// This method is automatically invoked by Unity when the GameObject is clicked. It delegates the spore release action to the TriggerSporeRelease method.
    /// </remarks>
    private void OnMouseDown()
    {
        // Trigger spore release on left-click.
        TriggerSporeRelease();
    }

}
