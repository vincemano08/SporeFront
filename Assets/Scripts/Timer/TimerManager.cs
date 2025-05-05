using Fusion;
using UnityEngine;

public class TimerManager : NetworkBehaviour
{
    [SerializeField] private EventChannel eventChannel;

    // [Networked]
    public float TimeRemaining { get; set; }
    private bool isRunning = false;

    private void Start()
    {
        // StartTimer(startTime);
    }

    public void StartTimer(float duration)
    {
        Debug.Log("Start Timer called.");
        TimeRemaining = duration;
        isRunning = true;
        eventChannel?.RaiseTimerUpdated(TimeRemaining);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcStartTimer(float duration)
    {
        Debug.Log("RPC Start Timer called on all clients.");
        // This will be called on all clients when the server triggers it
        TimeRemaining = duration;
        isRunning = true;
        StartTimer(duration);
        // eventChannel?.RaiseTimerUpdated(TimeRemaining);
    }

    private void Update()
    {
        if (!isRunning || TimeRemaining <= 0) return;

        TimeRemaining -= Time.deltaTime;
        TimeRemaining = Mathf.Max(TimeRemaining, 0f); // Clamp to 0

        eventChannel?.RaiseTimerUpdated(TimeRemaining);

        if (TimeRemaining <= 0)
        {
            isRunning = false;
            TimerEnded();
        }
    }

    private void TimerEnded()
    {
        eventChannel?.RaiseGameOver();
        Debug.Log("Timer finished!");
        // Trigger a game over or win condition here
    }

    public void StopTimer()
    {
        isRunning = false;
    }
}
