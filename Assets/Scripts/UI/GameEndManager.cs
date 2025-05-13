using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;
using System.Linq;

public class GameEndManager : MonoBehaviour
{
    [SerializeField] private EventChannel eventChannel;
    [SerializeField] private GameObject gameEndCanvas;
    [SerializeField] private TextMeshProUGUI playerPositionText;
    [SerializeField] private Transform scoreboardContainer;
    [SerializeField] private GameObject scoreEntryPrefab;

    private NetworkRunner runner;
    private List<GameObject> scoreEntries = new List<GameObject>();

    private void Awake()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        gameEndCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnGameOver += ShowGameEndScreen;
            eventChannel.OnScoreboardUpdated += UpdateFinalScoreboard;
        }
    }

    private void OnDisable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnGameOver -= ShowGameEndScreen;
            eventChannel.OnScoreboardUpdated -= UpdateFinalScoreboard;
        }
    }

    private void ShowGameEndScreen()
    {
        // Show the game end canvas
        gameEndCanvas.SetActive(true);

        // Hide the HUD if needed
        eventChannel.RaiseShowHideHud(false);
    }

    private void UpdateFinalScoreboard(Dictionary<PlayerRef, PlayerScore> scores)
    {
        // Only update if game end screen is active
        if (!gameEndCanvas.activeSelf)
            return;

        // Clear existing entries
        ClearScoreEntries();

        // Sort players by score (descending)
        var sortedPlayers = scores
            .OrderByDescending(s => s.Value.Score)
            .ToList();

        // Find local player position
        int localPlayerPosition = -1;
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            if (sortedPlayers[i].Key == runner.LocalPlayer)
            {
                localPlayerPosition = i + 1; // +1 because positions are 1-based
                break;
            }
        }

        // Update position text
        if (localPlayerPosition > 0)
        {
            string positionSuffix = GetPositionSuffix(localPlayerPosition);
            playerPositionText.text = $"You Finished {localPlayerPosition}{positionSuffix}!";
        }
        else
        {
            playerPositionText.text = "Game Over!";
        }

        // Create score entries
        foreach (var player in sortedPlayers)
        {
            CreateScoreEntry(player.Key, player.Value);
        }
    }

    private string GetPositionSuffix(int position)
    {
        if (position % 100 >= 11 && position % 100 <= 13)
            return "th";

        switch (position % 10)
        {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }

    private void CreateScoreEntry(PlayerRef player, PlayerScore score)
    {
        GameObject entry = Instantiate(scoreEntryPrefab, scoreboardContainer);

        // Set the name
        TextMeshProUGUI nameText = entry.transform.Find("UsernameText").GetComponent<TextMeshProUGUI>();
        nameText.text = score.UsernameString;

        // Set the score
        TextMeshProUGUI scoreText = entry.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        scoreText.text = score.Score.ToString();

        // Highlight local player
        if (runner != null && player == runner.LocalPlayer)
        {
            // Highlight the current player
            nameText.color = Color.yellow;
            scoreText.color = Color.yellow;
        }

        scoreEntries.Add(entry);
    }

    private void ClearScoreEntries()
    {
        foreach (var entry in scoreEntries)
        {
            Destroy(entry);
        }
        scoreEntries.Clear();
    }
}