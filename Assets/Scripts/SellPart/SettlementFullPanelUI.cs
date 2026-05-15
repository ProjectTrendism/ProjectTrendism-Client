using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettlementFullPanelUI : MonoBehaviour
{
    [Header("패널")]
    public GameObject panelRoot;

    [Header("요약 텍스트")]
    public TextMeshProUGUI revenueSummaryText;
    public TextMeshProUGUI reviewText;

    [Header("바 Fill")]
    public RectTransform salesBarFill;
    public RectTransform materialBarFill;
    public RectTransform rentBarFill;
    public RectTransform promotionBarFill;
    public RectTransform profitBarFill;

    [Header("바 라벨")]
    public TextMeshProUGUI salesBarLabel;
    public TextMeshProUGUI materialBarLabel;
    public TextMeshProUGUI rentBarLabel;
    public TextMeshProUGUI promotionBarLabel;
    public TextMeshProUGUI profitBarLabel;

    [Header("다음 탐험 씬 이름")]
    public string exploreSceneName = "villageScene";

    private int totalRevenue;
    private int normalSales;
    private int discountSales;
    private int promotionCost;
    private int materialCost;
    private int rentCost;
    private int manageCost;
    private int netProfit;
    private int soldCount;

    public void Show(
        int totalRevenue,
        int normalSales,
        int discountSales,
        int promotionCost,
        int materialCost,
        int rentCost,
        int manageCost,
        int soldCount)
    {
        this.totalRevenue = totalRevenue;
        this.normalSales = normalSales;
        this.discountSales = discountSales;
        this.promotionCost = promotionCost;
        this.materialCost = materialCost;
        this.rentCost = rentCost;
        this.manageCost = manageCost;
        this.soldCount = soldCount;

        int totalCost = materialCost + rentCost + manageCost + promotionCost;
        netProfit = totalRevenue - totalCost;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshUI();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void RefreshUI()
    {
        if (revenueSummaryText != null)
        {
            revenueSummaryText.text =
                "총매출 : " + totalRevenue + " G\n" +
                "정가 판매 수익 : " + normalSales + " G\n" +
                "할인 판매 수익 : " + discountSales + " G\n" +
                "판매 개수 : " + soldCount + " 개\n\n" +
                "재료비 : " + materialCost + " G\n" +
                "임대료 : " + rentCost + " G\n" +
                "홍보비 : " + promotionCost + " G\n" +
                "관리비 : " + manageCost + " G\n\n" +
                "순이익 : " + netProfit + " G";
        }

        int maxValue = Mathf.Max(
            totalRevenue,
            materialCost,
            rentCost,
            promotionCost,
            manageCost,
            Mathf.Abs(netProfit),
            1
        );

        SetBar(salesBarFill, salesBarLabel, "총매출", totalRevenue, maxValue);
        SetBar(materialBarFill, materialBarLabel, "재료비", materialCost, maxValue);
        SetBar(rentBarFill, rentBarLabel, "임대료", rentCost, maxValue);
        SetBar(promotionBarFill, promotionBarLabel, "홍보비", promotionCost, maxValue);
        SetBar(profitBarFill, profitBarLabel, netProfit >= 0 ? "순이익" : "순손실", Mathf.Abs(netProfit), maxValue);

        if (reviewText != null)
            reviewText.text = BuildReviewText();
    }

    private void SetBar(RectTransform fill, TextMeshProUGUI label, string title, int value, int maxValue)
    {
        if (fill != null)
        {
            float maxWidth = 260f;
            float ratio = Mathf.Clamp01((float)value / maxValue);
            fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth * ratio);
        }

        if (label != null)
            label.text = title + " : " + value + " G";
    }

    private string BuildReviewText()
    {
        if (soldCount == 0)
            return "판매가 거의 이루어지지 않았습니다. 다음에는 홍보나 가격 조정을 먼저 시도해보세요.";

        if (netProfit > 0 && promotionCost > 0)
            return "홍보비를 사용했지만 매출 전환이 발생했습니다. 다음 탐험에서는 비슷한 고객층 키워드를 더 찾아보세요.";

        if (netProfit > 0)
            return "흑자 정산입니다. 현재 제작·판매 전략이 안정적으로 작동했습니다.";

        if (promotionCost > totalRevenue)
            return "홍보비가 매출보다 높았습니다. 다음에는 상품 특성에 맞는 홍보만 선택하세요.";

        if (discountSales > normalSales)
            return "할인 판매 의존도가 높았습니다. 다음 제작에서는 대중성과 안정성을 보완하세요.";

        return "손실이 발생했습니다. 다음 탐험에서 더 강한 트렌드 키워드를 수집하고, 제작 보정을 활용하세요.";
    }

    public void ConfirmSettlementAndGoExplore()
    {
        if (SellManager.Instance != null)
            SellManager.Instance.ResetAfterSettlement();

        SceneManager.LoadScene(exploreSceneName);
    }
}