using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private EventChannel eventChannel;

    public int ScoreP1 { get; private set; }

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

    private void Start()
    {
        if (eventChannel != null)
            eventChannel.OnScoreChanged += UpdateScore;
    }

    private void OnDestroy()
    {
        if (eventChannel != null)
            eventChannel.OnScoreChanged -= UpdateScore;
    }

    private void UpdateScore(int score)
    {
        ScoreP1 += score;
        eventChannel?.RaiseUpdateHud(ScoreP1);
    }
}
