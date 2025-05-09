using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private Image playerSprite;
    [SerializeField] private Button colorPickerButton;
    [SerializeField] private Image readyStatus;

    public void Initialize(string username, Color color, bool isLocalPlayer, bool isReady)
    {
        usernameText.text = username;
        playerSprite.color = color;
        colorPickerButton.interactable = isLocalPlayer;
        readyStatus.color = isReady ? Color.green : Color.gray;
    }

    public void UpdateReadyStatus(bool isReady)
    {
        readyStatus.color = isReady ? Color.green : Color.gray;
    }

    public void UpdateColor(Color color)
    {
        playerSprite.color = color;
    }
}