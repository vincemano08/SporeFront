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

    public int sporeCount = 0;
    public int sporeThreshold = 5;

    // Add spores to the tekton, then check if enough spores have accumulated for a new fungus body to grow.

    /// <summary>
    /// Adds spores to this grid object and triggers fungus body growth when the spore threshold is reached.
    /// </summary>
    /// <param name="amount">The number of spores to add.</param>
    /// <remarks>
    /// Increments the spore count and logs the updated count along with the grid coordinates.
    /// If the spore count meets or exceeds the threshold and no fungus body is present, attempts to spawn a new fungus body
    /// using the FungusBodyFactory. If the factory instance is unavailable, logs an error; otherwise, resets the spore count.
    /// </remarks>
    public void AddSpores(int amount)
    {
        sporeCount += amount;
        Debug.Log($"({x},{z}) Number of spores: {sporeCount}");

        // If the spore count reaches the threshold and there is no fungus body yet, initiate the growth of a new fungus body.
        if (sporeCount >= sporeThreshold && FungusBody == null)
        {
            if (FungusBodyFactory.Instance != null)
            {
                FungusBodyFactory.Instance.SpawnFungusBody(this);
                sporeCount = 0; // Reset the spore counter.
            }
            else
            {
                Debug.LogError("FungusBodyFactory instance not found.");
            }
        }
    }

    /// <summary>
    /// Validates that the GameObject has an attached Collider component.
    /// </summary>
    /// <remarks>
    /// This method is called during initialization. If no Collider is found, an error is logged.
    /// </remarks>
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
