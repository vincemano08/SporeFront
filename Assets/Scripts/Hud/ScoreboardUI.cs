using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;
using System.Linq;

public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private EventChannel eventChannel;
    [SerializeField] private GameObject scoreEntryPrefab;
    [SerializeField] private Transform scoreboardContainer;
    [SerializeField] private int maxEntriesToShow = 4;

    private List<GameObject> scoreEntries = new List<GameObject>();
    private NetworkRunner runner;

    private void Awake()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
    }

    private void OnEnable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnScoreboardUpdated += UpdateScoreboard;
        }
    }

    private void OnDisable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnScoreboardUpdated -= UpdateScoreboard;
        }
    }

    private void UpdateScoreboard(Dictionary<PlayerRef, PlayerScore> scores)
    {
        // Clear existing entries
        ClearScoreEntries();

        // Sort players by score (descending)
        var sortedPlayers = scores
            .OrderByDescending(s => s.Value.Score)
            .Take(maxEntriesToShow);

        // Create new entries
        foreach (var player in sortedPlayers)
        {
            CreateScoreEntry(player.Key, player.Value);
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
            // Highlight the current player (optional)
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