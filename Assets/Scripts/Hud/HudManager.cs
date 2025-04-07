using TMPro;
using UnityEngine;

public class HudManager : MonoBehaviour
{

    [SerializeField] private EventChannel eventChannel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;

    private void Start()
    {
        if (scoreText != null)
            scoreText.text = "Score: 0";
    }

    private void OnEnable()
    {
        if (eventChannel != null) 
        {
            eventChannel.OnUpdateHudScore += UpdateHudScore;
            eventChannel.OnUpdateHudTimer += UpdateTimer;
        }
    }

    private void OnDisable()
    {
        if (eventChannel != null) 
        {
            eventChannel.OnUpdateHudScore -= UpdateHudScore;
            eventChannel.OnUpdateHudTimer -= UpdateTimer;
        }
    }

    private void UpdateHudScore(int currentScore) 
    {
        if (scoreText != null) 
            scoreText.text = $"Score: {currentScore}";
    }

    private void UpdateTimer(float timeRemaining) {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}";
    }

}
