using TMPro;
using UnityEngine;

public class SalesFeedbackUI : MonoBehaviour
{
    public GameObject panelRoot;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI reasonText;
    public TextMeshProUGUI adviceText;

    public void ShowSuccess(SellableItemData item, int sellPrice)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "판매 결과 분석";

        if (resultText != null)
            resultText.text = "결과 : 판매 성공 / +" + sellPrice + " G";

        if (reasonText != null)
            reasonText.text = "성공 원인\n" + BuildSuccessReason(item);

        if (adviceText != null)
            adviceText.text = "다음 추천\n" + BuildNextAdvice(item, true);
    }

    public void ShowFail(SellableItemData item)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "판매 결과 분석";

        if (resultText != null)
            resultText.text = "결과 : 판매 실패";

        if (reasonText != null)
            reasonText.text = "실패 원인\n" + BuildFailReason(item);

        if (adviceText != null)
            adviceText.text = "다음 추천\n" + BuildNextAdvice(item, false);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private string BuildSuccessReason(SellableItemData item)
    {
        if (item == null || item.craftedItem == null)
            return "- 판매 데이터가 없습니다.";

        string text = "";

        if (item.crowdLevel >= 70)
            text += "- 고객 몰림도가 높았습니다.\n";

        if (item.priceEvaluation == "적정" || item.priceEvaluation == "저렴함")
            text += "- 가격이 구매하기 좋은 수준이었습니다.\n";

        if (item.craftedItem.grade == "S" || item.craftedItem.grade == "A")
            text += "- 제작 등급이 높아 상품 신뢰도가 좋았습니다.\n";

        if (item.craftedItem.freshnessScore >= 60)
            text += "- 신선함이 높아 초반 반응이 좋았습니다.\n";

        if (item.craftedItem.popularityScore >= 60)
            text += "- 대중성이 높아 고객층이 넓었습니다.\n";

        if (item.craftedItem.stabilityScore >= 60)
            text += "- 안정성이 높아 유행 유지력이 좋았습니다.\n";

        if (text == "")
            text = "- 현재 판매 조건이 무난하게 맞았습니다.\n";

        return text;
    }

    private string BuildFailReason(SellableItemData item)
    {
        if (item == null || item.craftedItem == null)
            return "- 판매 데이터가 없습니다.";

        string text = "";

        if (item.priceEvaluation == "비쌈" || item.priceEvaluation == "너무 비쌈")
            text += "- 가격이 높아 구매 전환이 낮았습니다.\n";

        if (item.crowdLevel < 45)
            text += "- 고객 몰림도가 낮았습니다.\n";

        if (item.craftedItem.popularityScore < 30)
            text += "- 대중성이 낮아 구매층이 좁았습니다.\n";

        if (item.craftedItem.freshnessScore < 30)
            text += "- 신선함이 부족해 화제성이 약했습니다.\n";

        if (item.craftedItem.stabilityScore < 30)
            text += "- 안정성이 낮아 유행이 빨리 식을 위험이 있습니다.\n";

        if (item.lifeTurns <= 2)
            text += "- 남은 유행 수명이 짧습니다.\n";

        if (text == "")
            text = "- 확률 판정에서 판매가 성사되지 않았습니다.\n";

        return text;
    }

    private string BuildNextAdvice(SellableItemData item, bool success)
    {
        if (item == null || item.craftedItem == null)
            return "- 아이템 정보를 확인하세요.";

        string text = "";

        if (!success)
        {
            if (item.priceEvaluation == "비쌈" || item.priceEvaluation == "너무 비쌈")
                text += "- 가격을 낮추거나 할인 판매를 시도하세요.\n";

            if (item.crowdLevel < 45)
                text += "- 판매 전에 홍보를 진행하세요.\n";

            if (item.craftedItem.popularityScore < 30)
                text += "- 다음 탐험에서 대중성 높은 키워드를 더 찾으세요.\n";

            if (item.craftedItem.stabilityScore < 30)
                text += "- 제작 단계에서 안정성 보정을 고려하세요.\n";

            if (item.lifeTurns <= 2)
                text += "- 유행이 끝나기 전에 재고를 빠르게 정리하세요.\n";
        }
        else
        {
            text += "- 비슷한 조합을 레시피로 유지하세요.\n";

            if (item.crowdLevel >= 75)
                text += "- 수요가 높으므로 다음 판매에서는 가격을 올려도 좋습니다.\n";

            if (item.craftedItem.targetAudience != "")
                text += "- " + item.craftedItem.targetAudience + "을 계속 노려보세요.\n";
        }

        if (text == "")
            text = "- 현재 전략을 유지해도 좋습니다.\n";

        return text;
    }
}