using UnityEngine;
using UnityEngine.UI;

public class ColorOption : MonoBehaviour
{
    private Button _button;
    private Image _image;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
    }

    public void Initialize(Color color, bool isAvailable, System.Action<Color> onColorSelected)
    {
        _image.color = color;
        _button.interactable = isAvailable;
        _button.onClick.AddListener(() => onColorSelected(color));
    }
}