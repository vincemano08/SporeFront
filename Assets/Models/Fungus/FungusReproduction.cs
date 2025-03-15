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

    /// <summary>
    /// Initiates the spore release process for the fungus.
    /// </summary>
    /// <remarks>
    /// If the fungus has reached its spore production limit, this method logs a message and destroys the fungus body,
    /// clearing its reference from the associated grid object. If a spore release is already in progress (cooldown active),
    /// it logs a corresponding message and takes no further action. Otherwise, it starts the coroutine that manages the spore emission.
    /// </remarks>
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
    /// <summary>
    /// Coroutine that manages the spore release process by disabling further releases, updating the production count, and initiating spore spreading based on the fungus's grid position.
    /// </summary>
    /// <remarks>
    /// The coroutine retrieves the FungusBody component to obtain the current grid coordinates and calls the spore spreading routine. If the FungusBody or its associated GridObject is missing, it logs an error message. After executing the spore spread logic, it waits for a specified cooldown period before re-enabling spore release.
    /// </remarks>
    /// <returns>
    /// An IEnumerator for use with Unity's coroutine system.
    /// </returns>
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
    /// <summary>
    /// Distributes spores from the specified grid cell to its neighboring cells.
    /// </summary>
    /// <param name="x">The x-coordinate of the source cell.</param>
    /// <param name="z">The z-coordinate of the source cell.</param>
    /// <remarks>
    /// The method retrieves the GridManager from the scene and determines the spread radius based on whether advanced spreading is enabled (radius of 2) or not (radius of 1).
    /// It then iterates over adjacent cells within this radius (excluding the source cell) and, if a valid GridObject is found, adds spores equal to the spore release amount.
    /// If the GridManager is not found, an error is logged.
    /// </remarks>
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
