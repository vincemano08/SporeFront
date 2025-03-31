using UnityEngine;

public class HudManager : MonoBehaviour
{

    [SerializeField] private EventChannel eventChannel;

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

    private void UpdateHud(int currentScore) => Debug.Log($"Current score of P1 {currentScore}");

}
