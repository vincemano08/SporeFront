using Fusion;
using UnityEngine;

public class NetworkTimer : NetworkBehaviour
{
    [Networked] public float TimeRemaining { get; private set; }
    [Networked] public bool IsRunning { get; private set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            StartTimer(60f); // Only server starts it
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsRunning)
            return;

        TimeRemaining -= Runner.DeltaTime;

        if (TimeRemaining <= 0)
        {
            TimeRemaining = 0;
            IsRunning = false;
            Debug.Log("Timer ended!");
            // Trigger end game logic here
        }
    }

    public void StartTimer(float seconds)
    {
        if (!HasStateAuthority) return;

        TimeRemaining = seconds;
        IsRunning = true;
    }

    public void StopTimer()
    {
        if (!HasStateAuthority) return;

        IsRunning = false;
    }
}
