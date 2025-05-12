using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EventChannel", menuName = "Scriptable Objects/EventChannel")]
public class EventChannel : ScriptableObject
{

    // This event should be called, whenever the score changes -- called by the individual gameObjects
    public event Action<int> OnScoreChanged;
    // This event should be called, whenever the hud should be updated -- called by the ScoreManager
    public event Action<int> OnUpdateHud;    
    public event Action<float> OnTimerUpdated;
    // this event should be called, whenever the game is over -- called by the TimerManager
    public event Action OnGameOver;    
    public event Action<bool> OnShowHideHud;

    /// <param name="score">The value that should be added to the current scores of the player</param>
    public void RaiseScoreChanged(int score)
    {
        OnScoreChanged?.Invoke(score);
    }

    public void RaiseUpdateHud(int currentScore) {
        OnUpdateHud?.Invoke(currentScore);
    }

    public void RaiseTimerUpdated(float timeRemaining)
    {
        OnTimerUpdated?.Invoke(timeRemaining);
    }

    public void RaiseGameOver()
    {
        Debug.Log("Game Over Event Triggered");
        // This will be called to notify subs that the game is over
        OnGameOver?.Invoke();
    }

    public void RaiseShowHideHud(bool show)
    {
        Debug.Log($"HUD visibility changed to: {(show ? "Visible" : "Hidden")}");
        OnShowHideHud?.Invoke(show);
    }
}
