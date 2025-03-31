using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FungusBody : MonoBehaviour
{

    [Header("Spore Settings")]
    [SerializeField] private float sporeCooldown = 5f;       // Time interval between spore releases.
    [SerializeField] private int sporeReleaseAmount = 3;     // Number of spores released per emission.
    [SerializeField] private int sporeProductionLimit = 2;   // Maximum number of emission attempts.

    [Header("Advanced Fungi Settings")]
    [SerializeField] private bool isAdvanced = false;        // Advanced fungi with a larger spreading radius.

    public Tecton Tecton { get; set; }

    private bool canRelease = true;
    private int currentProductionCount = 0;   // Number of emissions so far.

    private Renderer objectRenderer;
    private WorldGeneration worldGen;

    private void Awake()
    {
        worldGen = FindFirstObjectByType<WorldGeneration>();
        if (worldGen == null)
            Debug.LogError("GridManager not found in the scene!");

        objectRenderer = GetComponent<Renderer>();
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance.CurrentMode == ActionMode.ThreadGrowth)
        {
            // dont let the the fungusbody to get selected if the current mode is threadgrowth.
            return;
        }

        // if the fungus body is already selected, deselect it. Otherwise, select it.
        if (GameManager.Instance.SelectedFungusBody == this)
        {
            GameManager.Instance.DeselectFungus();
        }
        else
        {
            GameManager.Instance.SelectFungusBody(this);
        }
    }

    public void TriggerSporeRelease()
    {
        if (currentProductionCount >= sporeProductionLimit)
        {
            Debug.Log("The fungus body has reached the spore production limit.");
            if (Tecton != null)
            {
                Destroy(gameObject);
                Tecton.FungusBody = null;
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

        if (Tecton != null)
            SpreadSpores();
        else
            Debug.LogError("Tecton not found for the fungus body!");

        //Changing the color to show that cooldown is in progress
        ChangeColor(Color.red);

        yield return new WaitForSeconds(sporeCooldown);

        ChangeColor(Color.white);
        canRelease = true;
    }

    /// <summary>
    /// Method for spore spreading: spores are distributed to neighboring (or, in advanced cases, more distant) tektons.
    /// </summary>
    private void SpreadSpores()
    {
        if (!isAdvanced)
        {
            // Basic fungi spread spores to neighboring tektons only.
            for (int i = 0; i < sporeReleaseAmount; i++)
            {
                Tecton neighbor = Tecton.Neighbors.ElementAt(Random.Range(0, Tecton.Neighbors.Count));
                neighbor.AddSpores(1);
            }
        }
        else
        {
            // Advanced fungi spread spores to neighbors and their neighbors.
            for (int i = 0; i < sporeReleaseAmount; i++)
            {
                Tecton neighbor = Tecton.Neighbors.ElementAt(Random.Range(0, Tecton.Neighbors.Count));
                neighbor.AddSpores(1);
                foreach (Tecton secondNeighbor in neighbor.Neighbors)
                {
                    if (secondNeighbor == Tecton) continue; // Skip original tecton
                    secondNeighbor.AddSpores(1);
                }
            }
        }
    }

    public void ChangeColor(Color newColor)
    {
        if (objectRenderer != null)
            objectRenderer.material.color = newColor;
        else
            Debug.LogWarning("Renderer not found on " + gameObject.name);
    }
}
