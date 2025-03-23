using TMPro;
using UnityEngine;

public class HudManager : MonoBehaviour
{

    [SerializeField] private EventChannel eventChannel;
    [SerializeField] private TextMeshProUGUI scoreText;

    private void OnEnable()
    {
        if (eventChannel != null) 
            eventChannel.OnUpdateHud += UpdateHud;
    }

    private void OnDisable()
    {
        if (eventChannel != null) 
            eventChannel.OnUpdateHud -= UpdateHud;
    }

    private void UpdateHud(int currentScore) 
    {
        if (scoreText != null) 
            scoreText.text = $"Score: {currentScore}";
    }

}
