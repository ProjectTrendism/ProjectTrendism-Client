using TMPro;
using UnityEngine;

public class AITrendPopupUI : MonoBehaviour
{
    [Header("팝업")]
    public GameObject popupRoot;

    [Header("AI 분석 텍스트")]
    public TextMeshProUGUI risingKeywordText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI directionText;
    public TextMeshProUGUI riskText;
    public TextMeshProUGUI selectionText;

    public void Show(CraftAnalysisResult analysis)
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (analysis == null)
        {
            SetEmpty();
            return;
        }

        if (risingKeywordText != null)
        {
            string keywordText = string.IsNullOrEmpty(analysis.aiRisingKeywordName)
                ? "-"
                : analysis.aiRisingKeywordName;

            risingKeywordText.text = "오늘의 급상승 키워드 : " + keywordText;
        }

        if (targetText != null)
            targetText.text = "추천 고객층 : " + analysis.targetAudience;

        if (directionText != null)
            directionText.text =
                "추천 제작 방향 : " + analysis.aiDirectionMessage + "\n\n" +
                "중요: AI 분석은 A등급 보장 버튼이 아니라, 부족한 부분을 알려주는 보조 기능입니다.";

        if (riskText != null)
            riskText.text =
                "위험 요소 : " + analysis.riskMessage + "\n\n" +
                analysis.problemMessage;

        if (selectionText != null)
            selectionText.text =
                "현재 조합 평가 : " + analysis.aiMessage + "\n\n" +
                analysis.nextExploreMessage;
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void SetEmpty()
    {
        if (risingKeywordText != null)
            risingKeywordText.text = "오늘의 급상승 키워드 : -";

        if (targetText != null)
            targetText.text = "추천 고객층 : -";

        if (directionText != null)
            directionText.text = "추천 제작 방향 : -";

        if (riskText != null)
            riskText.text = "위험 요소 : -";

        if (selectionText != null)
            selectionText.text = "현재 조합 평가 : 키워드를 선택해 주세요.";
    }
}
