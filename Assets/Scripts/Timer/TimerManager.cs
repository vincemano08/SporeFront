using UnityEngine;

public class TimerManager : MonoBehaviour
{
   

    [SerializeField] private EventChannel eventChannel;
    // default to 5 mins
    [Tooltip("in seconds")]
    [SerializeField] private float duration = 300;
    private float timeRemaining;
    private bool isRunning = false;

    private void Start()
    {
        StartTimer(duration);
    }

    private void StartTimer(float duration)
    {
        timeRemaining = duration;
        isRunning = true;
    }

    private void Update()
    {
        if (isRunning && timeRemaining > 0)
        {
            if (eventChannel != null) 
                eventChannel.RaiseUpdateHudTimer(timeRemaining);

            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                isRunning = false;
                eventChannel.RaiseTimesUp();
            }
        }
    }

    public void StopTimer()
    {
        isRunning = false;
    }

}
