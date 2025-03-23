using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EventChannel", menuName = "Scriptable Objects/EventChannel")]
public class EventChannel : ScriptableObject
{

    // This event should be called, whenever the score changes -- called by the individual gameObjects
    public event Action<int> OnScoreChanged;
    // This event should be called, whenever the hud should be updated -- called by the ScoreManager
    public event Action<int> OnUpdateHudScore;

    public event Action<float> OnUpdateHudTimer;
    // this event will be raised if theres no time left
    public event Action OnTimesUp;

    /// <param name="score">The value that should be added to the current scores of the player</param>
    public void RaiseScoreChanged(int score)
    {
        OnScoreChanged?.Invoke(score);
    }

    public void RaiseUpdateHudScore(int currentScore) {
        OnUpdateHudScore?.Invoke(currentScore);
    }    

    public void RaiseUpdateHudTimer(float timeRemaining) {
        OnUpdateHudTimer?.Invoke(timeRemaining);
    }

    public void RaiseTimesUp() {
        OnTimesUp?.Invoke();
    }


}
