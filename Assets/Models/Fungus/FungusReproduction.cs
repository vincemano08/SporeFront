using System.Collections;
using UnityEngine;

public class FungusReproduction : MonoBehaviour
{

    public float sporeCooldown = 5f;         // Time interval between spore releases.
    public int sporeReleaseAmount = 3;        // Number of spores released per emission.
    public int sporeProductionLimit = 2;      // Maximum number of emission attempts.
    private int currentProductionCount = 0;   // Number of emissions so far.

    [Header("Advanced Fungi Settings")]
    public bool isAdvanced = false;           // Advanced fungi with a larger spreading radius.

    private bool canRelease = true;

    public void TriggerSporeRelease()
    {
        if (currentProductionCount >= sporeProductionLimit)
        {
            Debug.Log("The fungus body has reached the spore production limit.");
            FungusBody fungusBody = GetComponent<FungusBody>();
            if (fungusBody != null && fungusBody.GridObject != null)
            {
                GridObject currentGrid = fungusBody.GridObject;
                Destroy(fungusBody.gameObject);
                currentGrid.FungusBody = null;
            }
            return;
        }
        if (!canRelease)
        {
            Debug.Log("Spore release in progress, please wait for the cooldown.");
            return;
        }
        StartCoroutine(ReleaseSporesCoroutine());
    }
    private IEnumerator ReleaseSporesCoroutine()
    {
        canRelease = false;
        currentProductionCount++;

        // Access the associated GridObject through the FungusBody.
        FungusBody fungusBody = GetComponent<FungusBody>();
        if (fungusBody != null && fungusBody.GridObject != null)
        {
            GridObject currentGrid = fungusBody.GridObject;
            SpreadSpores(currentGrid.x, currentGrid.z);
        }
        else
        {
            Debug.LogError("FungusBody or the associated GridObject not found!");
        }

        yield return new WaitForSeconds(sporeCooldown);
        canRelease = true;
    }
    /// <summary>
    /// Method for spore spreading: spores are distributed to neighboring (or, in advanced cases, more distant) tektons.
    /// </summary>
    private void SpreadSpores(int x, int z)
    {
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found in the scene!");
            return;
        }

        int spreadRadius = isAdvanced ? 2 : 1;

        for (int dx = -spreadRadius; dx <= spreadRadius; dx++)
        {
            for (int dz = -spreadRadius; dz <= spreadRadius; dz++)
            {
                if (dx == 0 && dz == 0)
                    continue;

                int newX = x + dx;
                int newZ = z + dz;
                if (newX >= 0 && newX < gridManager.width && newZ >= 0 && newZ < gridManager.height)
                {
                    GameObject neighborObj = gridManager.GetObject(newX, newZ);
                    if (neighborObj != null)
                    {
                        GridObject neighborGrid = neighborObj.GetComponent<GridObject>();
                        if (neighborGrid != null)
                        {
                            neighborGrid.AddSpores(sporeReleaseAmount);
                        }
                    }
                }
            }
        }
    }
}
