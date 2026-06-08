using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeywordButtonUI : MonoBehaviour
{
    public TextMeshProUGUI keywordNameText;
    public TextMeshProUGUI typeText;
    public Button button;

    private KeywordData keywordData;

    public void Setup(KeywordData data)
    {
        keywordData = data;

        if (keywordNameText != null)
        {
            keywordNameText.text = data.keywordName;
            keywordNameText.fontSize = 16f;
            keywordNameText.fontStyle = FontStyles.Bold;
            keywordNameText.alignment = TextAlignmentOptions.Center;
            keywordNameText.enableWordWrapping = false;
            keywordNameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (typeText != null)
        {
            typeText.gameObject.SetActive(false);
        }

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickButton);
        }
    }

    private void OnClickButton()
    {
        if (CraftManager.Instance != null)
        {
            CraftManager.Instance.AddSelectedKeyword(keywordData);
        }
    }
}