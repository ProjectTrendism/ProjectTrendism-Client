using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ExplorationSummaryManager : MonoBehaviour
{
    public GameObject summaryPanel;
    public TextMeshProUGUI summaryReasonText;
    public TextMeshProUGUI summaryKeywordText;

    public void ShowSummary(string reason)
    {
        if (summaryPanel == null) return;

        summaryPanel.SetActive(true);

        if (summaryReasonText != null)
            summaryReasonText.text = reason;

        KeywordManager keywordManager = FindObjectOfType<KeywordManager>();

        if (keywordManager != null && summaryKeywordText != null)
        {
            List<KeywordData> keywords = keywordManager.GetKeywords();

            string text = "";

            if (keywords.Count == 0)
            {
                text += "오늘 획득한 키워드가 없습니다.\n";
            }
            else
            {
                text += "오늘 획득한 키워드\n\n";

                for (int i = 0; i < keywords.Count; i++)
                {
                    text += "- " + keywords[i].keywordName + " (" + keywords[i].keywordType + ")\n";
                }
            }

            text += "\n";

            if (RumorManager.Instance != null)
            {
                text += RumorManager.Instance.GetTrendSummaryText();
            }
            else
            {
                text += "수집된 소문 정보가 없습니다.";
            }

            summaryKeywordText.text = text;
        }

        Time.timeScale = 0f;
    }

    public void GoToCraftScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TrendAnalysisScene");
    }
}