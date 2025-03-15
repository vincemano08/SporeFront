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

    /// <param name="amount">Number of spores to be added.</param>
    public void AddSpores(int amount)
    {
        sporeCount += amount;
        Debug.Log($"({x},{z}) tekton spóra száma: {sporeCount}");

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
