using UnityEngine;
using Fusion;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private EventChannel eventChannel;
    [SerializeField] private PlayerSpawner playerSpawner;

    public int ScoreP1 { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (eventChannel != null)
            eventChannel.OnScoreChanged += UpdateScore;
    }

    private void OnDestroy()
    {
        if (eventChannel != null)
            eventChannel.OnScoreChanged -= UpdateScore;
    }

    public void AddScore(PlayerRef player, int score)
    {
        playerSpawner.RPC_UpdatePlayerScore(player, score);
    }

    private void UpdateScore(int score)
    {
        // Update local player's score
        if (playerSpawner.Runner.LocalPlayer.IsValid)
        {
            AddScore(playerSpawner.Runner.LocalPlayer, score);
        }
    }
}
