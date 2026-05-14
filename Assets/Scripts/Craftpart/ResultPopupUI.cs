using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ResultPopupUI : MonoBehaviour
{
    public GameObject popupRoot;

    public TextMeshProUGUI resultNameText;
    public TextMeshProUGUI resultGradeText;
    public TextMeshProUGUI resultPriceText;
    public TextMeshProUGUI resultTargetText;
    public TextMeshProUGUI resultTrendText;

    [Header("추가 분석 텍스트")]
    public TextMeshProUGUI resultStatsText;
    public TextMeshProUGUI resultRiskText;
    public TextMeshProUGUI resultAdviceText;

    [Header("결과 이미지")]
    public CraftResultVisualUI resultVisualUI;

    [Header("보정 버튼")]
    public Button popularityFixButton;
    public Button freshnessFixButton;
    public Button stabilityFixButton;

    private CraftedItemResult currentResult;

    public void Show(CraftedItemResult result)
    {
        currentResult = result;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        RefreshUI();
        RefreshCorrectionButtons();
    }

    private void RefreshUI()
    {
        if (currentResult == null)
            return;

        if (resultNameText != null)
            resultNameText.text = "결과물 : " + currentResult.itemName;

        if (resultGradeText != null)
            resultGradeText.text = "등급 : " + currentResult.grade;

        if (resultPriceText != null)
            resultPriceText.text = "최종 가격 : " + currentResult.finalPrice + " G";

        if (resultTargetText != null)
            resultTargetText.text = "타겟 고객층 : " + currentResult.targetAudience;

        if (resultTrendText != null)
            resultTrendText.text = "유행성 : " + currentResult.trendLevel;

        if (resultStatsText != null)
        {
            resultStatsText.text =
                "대중성 : " + currentResult.popularityScore +
                " / 신선함 : " + currentResult.freshnessScore +
                " / 안정성 : " + currentResult.stabilityScore;
        }

        if (resultRiskText != null)
            resultRiskText.text = "제작 리스크 : " + currentResult.riskMessage;

        if (resultAdviceText != null)
        {
            string correctionText = "";

            if (currentResult.isCorrected)
                correctionText = "\n적용된 보정 : " + currentResult.correctionType;

            resultAdviceText.text = "추천 보정 : " + currentResult.adviceMessage + correctionText;
        }

        if (resultVisualUI != null)
            resultVisualUI.Show(currentResult);
    }

    public void OnClickPopularityFix()
    {
        if (currentResult == null)
            return;

        if (currentResult.isCorrected)
        {
            Debug.Log("이미 보정된 결과물입니다.");
            return;
        }

        currentResult.popularityScore += 15;
        currentResult.finalPrice += 10;
        currentResult.targetAudience = "대중형 고객층";

        currentResult.isCorrected = true;
        currentResult.correctionType = "대중성 보정";
        currentResult.analysisMessage = "대중성 보정을 적용했습니다.";

        RecalculateAfterFix();
        RefreshUI();
        RefreshCorrectionButtons();
        RefreshCraftedItemList();
    }

    public void OnClickFreshnessFix()
    {
        if (currentResult == null)
            return;

        if (currentResult.isCorrected)
        {
            Debug.Log("이미 보정된 결과물입니다.");
            return;
        }

        currentResult.freshnessScore += 15;
        currentResult.stabilityScore -= 5;

        if (currentResult.stabilityScore < 0)
            currentResult.stabilityScore = 0;

        currentResult.finalPrice += 15;

        currentResult.isCorrected = true;
        currentResult.correctionType = "신선함 보정";
        currentResult.analysisMessage = "신선함 보정을 적용했습니다.";

        RecalculateAfterFix();
        RefreshUI();
        RefreshCorrectionButtons();
        RefreshCraftedItemList();
    }

    public void OnClickStabilityFix()
    {
        if (currentResult == null)
            return;

        if (currentResult.isCorrected)
        {
            Debug.Log("이미 보정된 결과물입니다.");
            return;
        }

        currentResult.stabilityScore += 15;
        currentResult.freshnessScore -= 5;

        if (currentResult.freshnessScore < 0)
            currentResult.freshnessScore = 0;

        currentResult.isCorrected = true;
        currentResult.correctionType = "안정성 보정";
        currentResult.analysisMessage = "안정성 보정을 적용했습니다.";

        RecalculateAfterFix();
        RefreshUI();
        RefreshCorrectionButtons();
        RefreshCraftedItemList();
    }

    private void RefreshCorrectionButtons()
    {
        bool canCorrect = currentResult != null && currentResult.isCorrected == false;

        if (popularityFixButton != null)
            popularityFixButton.interactable = canCorrect;

        if (freshnessFixButton != null)
            freshnessFixButton.interactable = canCorrect;

        if (stabilityFixButton != null)
            stabilityFixButton.interactable = canCorrect;
    }

    private void RefreshCraftedItemList()
    {
        CraftedItemListUI listUI = FindObjectOfType<CraftedItemListUI>();

        if (listUI != null)
        {
            listUI.RefreshList();
        }
    }

    private void RecalculateAfterFix()
    {
        if (currentResult == null)
            return;

        int trendScore = currentResult.popularityScore + currentResult.freshnessScore;

        if (trendScore >= 140)
            currentResult.trendLevel = "폭발적";
        else if (trendScore >= 100)
            currentResult.trendLevel = "매우 높음";
        else if (trendScore >= 70)
            currentResult.trendLevel = "높음";
        else if (trendScore >= 40)
            currentResult.trendLevel = "보통";
        else
            currentResult.trendLevel = "낮음";

        currentResult.craftScore =
            currentResult.popularityScore +
            currentResult.freshnessScore +
            currentResult.stabilityScore;

        if (currentResult.craftScore >= 90)
            currentResult.grade = "S";
        else if (currentResult.craftScore >= 70)
            currentResult.grade = "A";
        else if (currentResult.craftScore >= 45)
            currentResult.grade = "B";
        else
            currentResult.grade = "C";

        currentResult.riskMessage = GetRiskMessage();
        currentResult.adviceMessage = GetAdviceMessage();
    }

    private string GetRiskMessage()
    {
        if (currentResult.stabilityScore < 30 && currentResult.freshnessScore >= 50)
            return "신선하지만 유행이 빨리 식을 수 있음";

        if (currentResult.popularityScore < 30)
            return "대중성이 낮아 구매층이 좁을 수 있음";

        if (currentResult.freshnessScore < 25)
            return "새로움이 부족해 SNS 반응이 약할 수 있음";

        if (currentResult.stabilityScore >= 60 && currentResult.popularityScore >= 50)
            return "안정적인 판매가 기대됨";

        return "보통";
    }

    private string GetAdviceMessage()
    {
        if (currentResult.popularityScore < 30)
            return "대중성 보정 추천";

        if (currentResult.freshnessScore < 30)
            return "신선함 보정 추천";

        if (currentResult.stabilityScore < 30)
            return "안정성 보정 추천";

        return "현재 결과 유지 추천";
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }
}