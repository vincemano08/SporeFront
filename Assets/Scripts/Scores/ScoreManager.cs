using UnityEngine;

public class ScoreManager : MonoBehaviour
{

    [SerializeField] private EventChannel eventChannel;

    // scores of player 1
    public int ScoreP1 { get; set; }

    private void Start()
    {
        if (eventChannel != null)
            eventChannel.OnScoreChanged += UpdateScore;

    }

    private void UpdateScore(int score) 
    {
        ScoreP1 += score;
        if (eventChannel != null) 
            eventChannel.RaiseUpdateHud(ScoreP1);
    }


}
