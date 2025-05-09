using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SessionListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI sessionNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button joinButton;

    public void Initialize(string sessionName, string playerCount, System.Action onJoinClicked)
    {
        sessionNameText.text = sessionName;
        playerCountText.text = playerCount;
        joinButton.onClick.AddListener(() => onJoinClicked());
    }
}