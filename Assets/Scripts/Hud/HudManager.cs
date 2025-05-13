using UnityEngine;
using TMPro;

public class HudManager : MonoBehaviour
{

    [SerializeField] private EventChannel eventChannel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject hudContent;

    private void OnEnable()
    {
        if (eventChannel != null) {
            eventChannel.OnTimerUpdated += UpdateTimer;
            eventChannel.OnUpdateHud += UpdateHud;
            eventChannel.OnShowHideHud += ShowHideHud;
        }
    }

    private void OnDisable()
    {
        if (eventChannel != null) {
            eventChannel.OnUpdateHud -= UpdateHud;
            eventChannel.OnTimerUpdated -= UpdateTimer;
            eventChannel.OnShowHideHud -= ShowHideHud;
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

    private void ShowHideHud(bool show)
    {
        if (hudContent != null)
        {
            hudContent.SetActive(show);
            Debug.Log($"HUD visibility set to {show}");
        }
        else
        {
            Debug.LogWarning("HUD content reference is missing in HudManager");
        }
    }
}
