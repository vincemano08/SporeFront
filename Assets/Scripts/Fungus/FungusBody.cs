using UnityEngine;
using System.Linq;
using Fusion;

public class FungusBody : NetworkBehaviour
{

    [Header("Spore Settings")]
    [Tooltip("Time interval between spore releases.")]
    [SerializeField, Networked] private float sporeCooldown { get; set; } = 5f;
    [Tooltip("Number of spores released per emission.")]
    [SerializeField, Networked] private int sporeReleaseAmount { get; set; } = 3;
    [Tooltip("Maximum number of emission attempts before the fungus body is destroyed.")]
    [SerializeField, Networked] private int sporeProductionLimit { get; set; } = 2;

    [Header("Advanced Fungi Settings")]
    [Tooltip("Is this fungus body an advanced type with larger range?")]
    [SerializeField, Networked] private bool isAdvanced { get; set; } = false;

    public Tecton Tecton { get; set; }

    [Networked] private bool canRelease { get; set; } = true;
    [Networked] private int currentProductionCount { get; set; } = 0;   // Number of emissions so far.
    [Networked] private TickTimer sporeCooldownTimer { get; set; }

    private Renderer objectRenderer;

    private void Awake() {
        objectRenderer = GetComponent<Renderer>();
    }

    private void OnMouseDown()
    {
        if (!HasInputAuthority) return;

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
        if (!HasStateAuthority) return;

        if (sporeCooldownTimer.IsRunning) {
            Debug.Log("Spore release is on cooldown.");
            return;
        }

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

        currentProductionCount++;
        sporeCooldownTimer = TickTimer.CreateFromSeconds(Runner, sporeCooldown);

        if (Tecton != null) {
            SpreadSpores();
            ChangeColor(Color.red);
            canRelease = false;
        }
    }

    public override void FixedUpdateNetwork() {
        if (!HasStateAuthority) return;

        if (sporeCooldownTimer.Expired(Runner)) {
            // Terrible soltuion but whatever
            ChangeColor(Color.cyan);
            sporeCooldownTimer = TickTimer.None;
            canRelease = true;
        }
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
