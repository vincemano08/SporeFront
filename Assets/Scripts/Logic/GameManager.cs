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
    public FungusBody PrevFungusBody { get; private set; } = null;

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
        if (PrevFungusBody != null)
        {
            PrevFungusBody.ChangeColor(Color.white);
        }
        SelectedFungusBody = fungusBody;
        PrevFungusBody = fungusBody;
        fungusBody.ChangeColor(Color.cyan);
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
            SelectedFungusBody.ChangeColor(Color.white);
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
                    Debug.Log($"we have a gridobj {gridObject != null}, we have a fungusbody {SelectedFungusBody != null}");
                    Tecton targetTecton = gridObject.parentTecton;
                    if (CurrentMode == ActionMode.ThreadGrowth && SelectedFungusBody != null)
                    {
                        if (CanGrowThread(SelectedFungusBody.Tecton, targetTecton))
                        {
                            FungalThreadManager.Instance.Connect(SelectedFungusBody.Tecton, targetTecton);
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
