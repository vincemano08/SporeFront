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
    public FungusBody PrevFungusBody { get; private set; } = null;

    [SerializeField] private EventChannel eventChannel;

    public ActionMode CurrentMode { get; private set; } = ActionMode.None;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SelectFungusBody(FungusBody fungusBody)
    {
        if(PrevFungusBody != null)
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
        if(SelectedFungusBody != null)
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

        FindFirstObjectByType<HighlightManager>()?.ResetAllHighlights();
    }

    private void Update()
    {
        if (SelectedFungusBody != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
                SelectedFungusBody.TriggerSporeRelease();
            else if (Input.GetKeyDown(KeyCode.F))
                EnterThreadGrowthMode();
            else if (Input.GetKeyDown(KeyCode.Escape))
                DeselectFungus();
        }
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridObject gridObject = hit.collider.GetComponent<GridObject>();
                if (gridObject != null)
                {
                    Tecton targetTecton = gridObject.parentTecton;
                    if (CurrentMode == ActionMode.ThreadGrowth && SelectedFungusBody != null)
                    {
                        if (CanGrowThread(SelectedFungusBody.Tecton, targetTecton))
                        {
                            FungalThreadManager.Instance.Connect(SelectedFungusBody.Tecton, targetTecton);
                            DeselectFungus();
                        }
                    }
                    else if (SelectedFungusBody == null && CurrentMode == ActionMode.None)
                    {
                        // added quality of life feature you dont have to click the fungus itself to select it. yippieee
                        if (targetTecton.FungusBody != null)
                        {
                            SelectFungusBody(targetTecton.FungusBody);
                        }
                        else
                        {
                            FungusBodyFactory.Instance.SpawnFungusBody(gridObject);
                            if (eventChannel != null)
                                eventChannel.RaiseScoreChanged(1);
                        }
                    }
                }
            }
        }
    }

    private bool CanGrowThread(Tecton source, Tecton target)
    {
        if (source == target) return false;

        if (!source.Neighbors.Contains(target)) return false;

        return FungalThreadManager.Instance.CanConnect(source, target);
    }
}
