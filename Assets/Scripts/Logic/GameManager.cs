using Fusion;
using System.Diagnostics.Tracing;
using UnityEngine;

public enum ActionMode
{
    None,
    ThreadGrowth
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public FungusBody SelectedFungusBody { get; private set; }

    [SerializeField] private HighlightManager highlightManager;
    [SerializeField] private EventChannel eventChannel;

    public ActionMode CurrentMode { get; private set; } = ActionMode.None;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (highlightManager == null)
            {
                highlightManager = FindFirstObjectByType<HighlightManager>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SelectFungusBody(FungusBody fungusBody)
    {
        if (SelectedFungusBody != null && SelectedFungusBody != fungusBody)
        {
            SelectedFungusBody.GetComponent<Outline>().enabled = false;
        }
        SelectedFungusBody = fungusBody;
        if (SelectedFungusBody.Tecton == null)
        {
            // Try to find the GridObject the FungusBody is on
            GridObject gridObjectBelow = GridObject.GetGridObjectAt(SelectedFungusBody.transform.position);
            if (gridObjectBelow != null && gridObjectBelow.parentTecton != null)
            {
                // Assign the parentTecton to the FungusBody
                SelectedFungusBody.Tecton = gridObjectBelow.parentTecton;
                Debug.Log($"Assigned Tecton {gridObjectBelow.parentTecton.Id} to FungusBody {SelectedFungusBody.name} at {SelectedFungusBody.transform.position}");
            }
            else
            {
                Debug.LogWarning($"Could not determine Tecton for FungusBody {SelectedFungusBody.name} at {SelectedFungusBody.transform.position}. Raycast did not find a GridObject with a parentTecton.");
            }
        }




        fungusBody.GetComponent<Outline>().enabled = true;
        CurrentMode = ActionMode.None;
    }

    public void EnterThreadGrowthMode()
    {
        if (SelectedFungusBody != null)
        {
            CurrentMode = ActionMode.ThreadGrowth;
        }
    }

    public void DeselectFungus()
    {

        if (SelectedFungusBody != null)
        {
            SelectedFungusBody.GetComponent<Outline>().enabled = false;
            SelectedFungusBody = null;
        }

        CurrentMode = ActionMode.None;

        highlightManager?.ResetAllHighlights();
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
    }

    private void HandleKeyboardInput()
    {
        if (SelectedFungusBody != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
                SelectedFungusBody.TriggerSporeRelease();
            else if (Input.GetKeyDown(KeyCode.F))
                EnterThreadGrowthMode();
            else if (Input.GetKeyDown(KeyCode.Escape))
                DeselectFungus();
            else if (Input.GetKeyDown(KeyCode.C))
            {
                // eventChannel.RaiseScoreChanged(1);
            }
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridObject gridObject = hit.collider.GetComponent<GridObject>();
                if (gridObject != null)
                {
                    //Debug.Log($"we have a gridobj {gridObject != null}, we have a fungusbody {SelectedFungusBody != null}");
                    Tecton targetTecton = gridObject.parentTecton;
                    if (CurrentMode == ActionMode.ThreadGrowth && SelectedFungusBody != null)
                    {
                        if (CanGrowThread(SelectedFungusBody.Tecton, targetTecton))
                        {
                            FungalThreadManager.Instance.Connect(SelectedFungusBody.Tecton, targetTecton, SelectedFungusBody.PlayerReference);
                            DeselectFungus();
                        }
                    }
                }
            }
        }
    }

    private bool CanGrowThread(Tecton source, Tecton target)
    {
        if (source == null || target == null)
        {
            Debug.LogError("Source or target Tecton is null");
            return false;
        }

        if (source == target) return false;

        if (source.Neighbors == null || source.Neighbors.Count == 0)
        {
            Debug.LogError("Tecton.Neighbours could not been synshronised");
            return false;
        }

        if (!source.Neighbors.Contains(target)) return false;

        return FungalThreadManager.Instance.CanConnect(source, target);
    }
}
