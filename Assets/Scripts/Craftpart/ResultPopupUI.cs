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
            resultGradeText.text = "제작 등급 : " + currentResult.grade;

        if (resultPriceText != null)
            resultPriceText.text = "최종 가격 : " + currentResult.finalPrice + " G";

        if (resultTargetText != null)
            resultTargetText.text = "타겟 고객층 : " + currentResult.targetAudience;

        if (resultTrendText != null)
            resultTrendText.text = "유행성 : " + currentResult.trendLevel;

        if (resultStatsText != null)
        {
            if (!string.IsNullOrEmpty(currentResult.detailScoreMessage))
            {
                resultStatsText.text = currentResult.detailScoreMessage;
            }
            else
            {
                resultStatsText.text =
                    "대중성 : " + currentResult.popularityScore +
                    " / 신선함 : " + currentResult.freshnessScore +
                    " / 안정성 : " + currentResult.stabilityScore;
            }
        }

        if (resultRiskText != null)
        {
            if (!string.IsNullOrEmpty(currentResult.problemMessage))
                resultRiskText.text = currentResult.problemMessage;
            else
                resultRiskText.text = "제작 리스크 : " + currentResult.riskMessage;
        }

        if (resultAdviceText != null)
        {
            string correctionText = "";

            if (currentResult.isCorrected)
                correctionText = "\n\n적용된 보정 : " + currentResult.correctionType +
                                 "\n보정은 일부 항목만 개선합니다. 부족한 정보는 다음 탐험에서 보완해야 합니다.";

            if (!string.IsNullOrEmpty(currentResult.nextExploreMessage))
                resultAdviceText.text = currentResult.nextExploreMessage + correctionText;
            else
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

        currentResult.trendFitScore = Mathf.Clamp(currentResult.trendFitScore + 8, 0, 30);
        currentResult.targetClarityScore = Mathf.Clamp(currentResult.targetClarityScore + 3, 0, 15);

        currentResult.isCorrected = true;
        currentResult.correctionType = "대중성 보정";
        currentResult.analysisMessage = "대중성 보정을 적용했습니다. 단, 정보 부족 문제는 탐험에서 해결해야 합니다.";

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

        currentResult.trendFitScore = Mathf.Clamp(currentResult.trendFitScore + 4, 0, 30);
        currentResult.originalityScore = Mathf.Clamp(currentResult.originalityScore + 4, 0, 10);
        currentResult.combinationScore = Mathf.Clamp(currentResult.combinationScore - 2, 0, 25);

        currentResult.isCorrected = true;
        currentResult.correctionType = "신선함 보정";
        currentResult.analysisMessage = "신선함 보정을 적용했습니다. 독창성은 올랐지만 조합 안정성은 조금 낮아질 수 있습니다.";

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

        currentResult.combinationScore = Mathf.Clamp(currentResult.combinationScore + 6, 0, 25);
        currentResult.informationScore = Mathf.Clamp(currentResult.informationScore + 2, 0, 20);
        currentResult.originalityScore = Mathf.Clamp(currentResult.originalityScore - 1, 0, 10);

        currentResult.isCorrected = true;
        currentResult.correctionType = "안정성 보정";
        currentResult.analysisMessage = "안정성 보정을 적용했습니다. 안정성은 올라갔지만 독창성은 조금 낮아질 수 있습니다.";

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
            listUI.RefreshList();
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
            currentResult.trendFitScore +
            currentResult.combinationScore +
            currentResult.informationScore +
            currentResult.targetClarityScore +
            currentResult.originalityScore;

        currentResult.grade = GetGradeByScore(currentResult.craftScore);

        currentResult.riskMessage = GetRiskMessage();
        currentResult.adviceMessage = GetAdviceMessage();

        currentResult.detailScoreMessage = GetDetailScoreMessage();
        currentResult.problemMessage = GetProblemMessage();
        currentResult.nextExploreMessage = GetNextExploreMessage();
    }

    private string GetGradeByScore(int score)
    {
        if (score >= 90) return "S";
        if (score >= 80) return "A";
        if (score >= 65) return "B";
        if (score >= 50) return "C";
        if (score >= 35) return "D";
        return "F";
    }

    private string GetDetailScoreMessage()
    {
        return
            "세부 평가\n" +
            "- 트렌드 적합도 : " + currentResult.trendFitScore + " / 30\n" +
            "- 조합 완성도 : " + currentResult.combinationScore + " / 25\n" +
            "- 정보 충분도 : " + currentResult.informationScore + " / 20\n" +
            "- 타겟 명확도 : " + currentResult.targetClarityScore + " / 15\n" +
            "- 독창성 : " + currentResult.originalityScore + " / 10";
    }

    private string GetProblemMessage()
    {
        string message = "분석 결과\n";
        bool hasProblem = false;

        if (currentResult.trendFitScore < 16)
        {
            message += "- 현재 유행 후보와 맞는 키워드가 부족합니다.\n";
            hasProblem = true;
        }

        if (currentResult.combinationScore < 14)
        {
            message += "- 선택한 키워드 조합의 완성도가 낮습니다.\n";
            hasProblem = true;
        }

        if (currentResult.informationScore < 10)
        {
            message += "- 탐험에서 얻은 정보가 부족하거나 한쪽으로 치우쳤습니다.\n";
            hasProblem = true;
        }

        if (currentResult.targetClarityScore < 8)
        {
            message += "- 타겟 고객층이 명확하지 않습니다.\n";
            hasProblem = true;
        }

        if (currentResult.originalityScore < 4)
        {
            message += "- 상품의 차별성이 약합니다.\n";
            hasProblem = true;
        }

        if (!hasProblem)
            message += "- 전반적으로 균형이 좋은 제작 결과입니다.\n";

        return message.TrimEnd();
    }

    private string GetNextExploreMessage()
    {
        string message = "다음 탐험 추천\n";
        bool hasGoal = false;

        if (currentResult.trendFitScore < 16)
        {
            message += "- 색상, 스타일, 분위기 관련 유행 정보를 더 조사하세요.\n";
            hasGoal = true;
        }

        if (currentResult.combinationScore < 14)
        {
            message += "- 서로 어울리는 Base / Style / Concept 키워드를 추가로 수집하세요.\n";
            hasGoal = true;
        }

        if (currentResult.informationScore < 10)
        {
            message += "- 한 NPC 말만 믿지 말고 다른 출처와 비교하세요.\n";
            message += "- 신뢰도 높은 NPC나 관찰 오브젝트를 우선 조사하세요.\n";
            hasGoal = true;
        }

        if (currentResult.targetClarityScore < 8)
        {
            message += "- 특정 고객층의 취향을 알려주는 NPC를 찾아보세요.\n";
            hasGoal = true;
        }

        if (currentResult.originalityScore < 4)
        {
            message += "- 숨겨진 오브젝트나 희귀 키워드를 찾아 차별성을 높이세요.\n";
            hasGoal = true;
        }

        if (!hasGoal)
            message += "- 현재 조합은 충분히 좋습니다. 판매 단계로 넘어가도 좋습니다.\n";

        return message.TrimEnd();
    }

    private string GetRiskMessage()
    {
        if (currentResult.informationScore < 10)
            return "탐험 정보가 부족해 제작 판단의 확실성이 낮음";

        if (currentResult.trendFitScore < 16)
            return "현재 유행과 어긋날 가능성이 있음";

        if (currentResult.combinationScore < 14)
            return "선택한 키워드 조합이 불안정함";

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
        if (currentResult.trendFitScore < 16)
            return "유행 정보를 더 확인한 뒤 제작하는 것이 좋습니다.";

        if (currentResult.combinationScore < 14)
            return "Base, Style, Concept 조합 균형을 맞추는 것이 좋습니다.";

        if (currentResult.informationScore < 10)
            return "탐험에서 같은 정보를 다른 출처로 한 번 더 확인해 보세요.";

        if (currentResult.targetClarityScore < 8)
            return "타겟 고객층을 더 명확히 잡는 키워드가 필요합니다.";

        if (currentResult.originalityScore < 4)
            return "희귀하거나 신선한 키워드를 추가하면 차별화됩니다.";

        return "현재 결과 유지 추천";
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }
}
