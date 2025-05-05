using UnityEngine;
using TMPro;

public class HudManager : MonoBehaviour
{

    [SerializeField] private EventChannel eventChannel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;

    private void OnEnable()
    {
        if (eventChannel != null) {
            eventChannel.OnTimerUpdated += UpdateTimer;
            eventChannel.OnUpdateHud += UpdateHud;
        }
    }

    private void OnDisable()
    {
        if (eventChannel != null) {
            eventChannel.OnUpdateHud -= UpdateHud;
            eventChannel.OnTimerUpdated -= UpdateTimer;
        }
    }

    private void UpdateHud(int currentScore)
    {
        scoreText.text = $"Score: {currentScore}";
    }

    private void UpdateTimer(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

}
