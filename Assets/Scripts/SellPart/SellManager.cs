using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SellManager : MonoBehaviour
{
    public static SellManager Instance;

    [Header("판매 가능 아이템 목록")]
    public List<SellableItemData> sellableItems = new List<SellableItemData>();

    [Header("현재 선택 아이템")]
    public SellableItemData selectedItem;

    [Header("상단 UI")]
    public TextMeshProUGUI goldText;

    [Header("왼쪽 목록 UI")]
    public SellItemListUI sellItemListUI;

    [Header("중앙 UI")]
    public TextMeshProUGUI selectedItemTitleText;
    public TextMeshProUGUI itemInfoText;
    public TextMeshProUGUI trendValueText;
    public TextMeshProUGUI currentPriceText;

    [Header("오른쪽 로그 UI")]
    public TextMeshProUGUI logText;

    [Header("하단 정산 UI")]
    public TextMeshProUGUI summaryText;

    [Header("유행 슬라이더")]
    public Slider trendSlider;

    [Header("고객 몰림도 UI")]
    public Slider crowdSlider;
    public TextMeshProUGUI crowdValueText;
    public Image[] customerIcons;

    [Header("판매 결과 분석 UI")]
    public SalesFeedbackUI salesFeedbackUI;

    [Header("SNS 홍보 스튜디오")]
    public SNSPromotionStudioUI snsPromotionStudioUI;

    [Header("큰 정산 패널")]
    public SettlementFullPanelUI settlementFullPanelUI;

    [Header("플레이어 골드")]
    public int currentGold = 0;

    [Header("정산 데이터")]
    public int totalSales = 0;
    public int totalDiscountSales = 0;
    public int totalSoldCount = 0;

    [Header("고정 비용")]
    public int materialCost = 100;
    public int rentCost = 80;
    public int manageCost = 40;

    [Header("홍보 정산 데이터")]
    public int totalPromotionCost = 0;

    [Header("판매 로그 데이터")]
    public List<string> sellLogs = new List<string>();

    [Header("이미 반영한 제작 결과 개수")]
    public int processedCraftedItemCount = 0;

     [HideInInspector] 
     public PromotionPanelUI promotionPanelUI;

     public TextMeshProUGUI marketStatusText;

    [HideInInspector] public SettlementPanelUI settlementPanelUI;
    


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 처음 판매씬 진입 시에도 동작하게
        OnEnterSellScene();
    }

    public void OnEnterSellScene()
    {
        SyncNewCraftedItems();

        if (selectedItem == null && sellableItems.Count > 0)
            selectedItem = sellableItems[0];

        RefreshUI();
    }

    void SyncNewCraftedItems()
    {
        if (CraftedItemManager.Instance == null)
        {
            Debug.LogWarning("CraftedItemManager가 없습니다.");
            return;
        }

        List<CraftedItemResult> craftedItems = CraftedItemManager.Instance.GetCraftedItems();

        if (processedCraftedItemCount < 0)
            processedCraftedItemCount = 0;

        for (int i = processedCraftedItemCount; i < craftedItems.Count; i++)
        {
            CraftedItemResult crafted = craftedItems[i];
            SellableItemData existingItem = FindSameSellItem(crafted);

            if (existingItem != null)
            {
                existingItem.stock += 1;
            }
            else
            {
                SellableItemData newItem = new SellableItemData(crafted);
                newItem.stock = 1;
                newItem.trendValue = GetStartTrendValue(crafted);
                newItem.lifeTurns = 6;

                sellableItems.Add(newItem);
            }
        }

        processedCraftedItemCount = craftedItems.Count;
    }

    SellableItemData FindSameSellItem(CraftedItemResult crafted)
    {
        for (int i = 0; i < sellableItems.Count; i++)
        {
            CraftedItemResult target = sellableItems[i].craftedItem;

            bool sameName = target.itemName == crafted.itemName;
            bool sameGrade = target.grade == crafted.grade;
            bool samePrice = target.finalPrice == crafted.finalPrice;

            if (sameName && sameGrade && samePrice)
            {
                return sellableItems[i];
            }
        }

        return null;
    }

    float GetStartTrendValue(CraftedItemResult item)
    {
        float value = 40f;

        if (item.grade == "S") value += 30f;
        else if (item.grade == "A") value += 20f;
        else if (item.grade == "B") value += 10f;

        if (item.trendLevel == "매우 높음") value += 20f;
        else if (item.trendLevel == "높음") value += 10f;
        else if (item.trendLevel == "보통") value += 5f;

        return Mathf.Clamp(value, 0f, 100f);
    }

   int CalculateCurrentPrice(SellableItemData item)
    {
        if (item == null || item.craftedItem == null)
            return 0;

        if (item.useManualPrice)
            return Mathf.Max(1, item.manualPrice);

        float trendMultiplier = 0.7f + (item.trendValue / 100f);
        float lifePenalty = 1f;

        if (item.lifeTurns <= 2)
            lifePenalty = 0.75f;
        else if (item.lifeTurns <= 4)
            lifePenalty = 0.9f;

        float finalValue = item.craftedItem.finalPrice * trendMultiplier * lifePenalty;
        return Mathf.Max(1, Mathf.RoundToInt(finalValue));
    }

    int CalculateDiscountPrice(SellableItemData item)
    {
        int normalPrice = CalculateCurrentPrice(item);
        return Mathf.Max(1, Mathf.RoundToInt(normalPrice * 0.7f));
    }

    void UpdateSalesPrediction(SellableItemData item)
    {
        if (item == null || item.craftedItem == null)
            return;

        CraftedItemResult crafted = item.craftedItem;
        int currentPrice = CalculateCurrentPrice(item);

        float crowd = 0f;

        crowd += item.trendValue * 0.45f;
        crowd += crafted.popularityScore * 0.25f;
        crowd += crafted.freshnessScore * 0.15f;
        crowd += crafted.stabilityScore * 0.10f;

        if (crafted.grade == "S")
            crowd += 12f;
        else if (crafted.grade == "A")
            crowd += 8f;
        else if (crafted.grade == "B")
            crowd += 4f;

        if (crafted.aiBonus > 0)
            crowd += 5f;

        if (crafted.isCorrected)
            crowd += 3f;

        if (item.lifeTurns <= 2)
            crowd -= 15f;
        else if (item.lifeTurns <= 4)
            crowd -= 6f;

        crowd += item.lastPromotionBoost * 0.8f;

        crowd = Mathf.Clamp(crowd, 0f, 100f);
        item.crowdLevel = crowd;

        float priceRatio = 1f;

        if (crafted.finalPrice > 0)
            priceRatio = (float)currentPrice / crafted.finalPrice;

        if (priceRatio <= 0.8f)
            item.priceEvaluation = "저렴함";
        else if (priceRatio <= 1.2f)
            item.priceEvaluation = "적정";
        else if (priceRatio <= 1.5f)
            item.priceEvaluation = "비쌈";
        else
            item.priceEvaluation = "너무 비쌈";

        // 가격이 너무 높으면 실제 고객 몰림도도 떨어지게 보정
        if (item.priceEvaluation == "저렴함")
        {
            crowd += 5f;
        }
        else if (item.priceEvaluation == "비쌈")
        {
            crowd -= 8f;
        }
        else if (item.priceEvaluation == "너무 비쌈")
        {
            crowd -= 18f;
        }

        crowd = Mathf.Clamp(crowd, 0f, 100f);
        item.crowdLevel = crowd;

        float chance = 25f;

        // 고객 몰림도 반영
        chance += crowd * 0.55f;

        // 가격 평가 반영
        if (item.priceEvaluation == "저렴함")
            chance += 25f;
        else if (item.priceEvaluation == "적정")
            chance += 18f;
        else if (item.priceEvaluation == "비쌈")
            chance -= 5f;
        else if (item.priceEvaluation == "너무 비쌈")
            chance -= 22f;

        // 상품 등급 보정
        if (crafted.grade == "S")
            chance += 12f;
        else if (crafted.grade == "A")
            chance += 8f;
        else if (crafted.grade == "B")
            chance += 4f;

        // 유행 수명이 너무 적으면 패널티
        if (item.lifeTurns <= 1)
            chance -= 15f;
        else if (item.lifeTurns <= 2)
            chance -= 8f;

        // 재고 없으면 0
        if (item.stock <= 0)
            chance = 0f;

        // 너무 자주 실패하지 않게 최소 보장
        if (item.stock > 0)
            chance = Mathf.Max(chance, 20f);

        item.expectedSellChance = Mathf.Clamp(chance, 0f, 95f);

        item.riskMessage = GetSalesRiskMessage(item);
        item.recommendationMessage = GetSalesRecommendationMessage(item);
    }

    string GetSalesRiskMessage(SellableItemData item)
    {
        if (item == null || item.craftedItem == null)
            return "-";

        CraftedItemResult crafted = item.craftedItem;

        if (item.lifeTurns <= 1)
            return "유행 수명이 거의 끝났습니다.";

        if (crafted.stabilityScore < 30 && crafted.freshnessScore >= 50)
            return "화제성은 있지만 유행이 빨리 식을 수 있습니다.";

        if (crafted.popularityScore < 30)
            return "대중성이 낮아 구매층이 좁습니다.";

        if (item.priceEvaluation == "너무 비쌈")
            return "가격이 너무 높아 구매 전환이 낮습니다.";

        if (item.crowdLevel < 35)
            return "현재 고객 관심도가 낮습니다.";

        return "큰 위험 요소는 낮습니다.";
    }

    string GetSalesRecommendationMessage(SellableItemData item)
    {
        if (item == null || item.craftedItem == null)
            return "-";

        if (item.lifeTurns <= 2)
            return "유행 수명이 짧으므로 할인 판매를 고려하세요.";

        if (item.crowdLevel >= 75 && item.priceEvaluation == "적정")
            return "고객이 몰리고 있습니다. 정가 판매 추천.";

        if (item.crowdLevel >= 75 && item.priceEvaluation == "저렴함")
            return "수요가 높습니다. 가격을 올려도 좋습니다.";

        if (item.crowdLevel < 45)
            return "홍보 후 판매하는 것이 좋습니다.";

        if (item.priceEvaluation == "비쌈" || item.priceEvaluation == "너무 비쌈")
            return "가격 부담이 큽니다. 할인 판매를 고려하세요.";

        return "현재 조건에서는 정가 판매가 무난합니다.";
    }

    float CalculatePromotionBoost(string background, string filter, string tag1, string tag2, string tag3)
    {
        float boost = 5f;

        // 배경 보너스
        if (background == "고급 천")
            boost += 2f;
       else if (background == "꽃 장식")
            boost += 3f;
        else if (background == "마법 조명")
            boost += 4f;

        // 필터 보너스
        if (filter == "빈티지")
            boost += 3f;
        else if (filter == "화사함")
            boost += 2f;
        else if (filter == "시크함")
            boost += 2.5f;

        // 해시태그 기본 보너스
        boost += 1.5f;

        // 중복 태그 패널티
        if (tag1 == tag2 || tag2 == tag3 || tag1 == tag3)
            boost -= 2f;

        // 선택된 아이템 이름과 태그/필터가 맞으면 추가 보너스
        if (selectedItem != null && selectedItem.craftedItem != null)
        {
            string itemName = selectedItem.craftedItem.itemName;

            if (itemName.Contains("감성"))
            {
                if (tag1 == "감성" || tag2 == "감성" || tag3 == "감성")
                    boost += 3f;
            }

            if (itemName.Contains("럭셔리"))
            {
                if (tag1 == "럭셔리" || tag2 == "럭셔리" || tag3 == "럭셔리")
                    boost += 3f;
            }

            if (itemName.Contains("빈티지"))
            {
                if (filter == "빈티지")
                    boost += 3f;
            }
        }

        // 같은 아이템을 반복 홍보할수록 효율 감소
        if (selectedItem != null)
        {
            boost -= selectedItem.promotionCount * 0.7f;
        }

        if (selectedItem != null)
        {
            if (selectedItem.lifeTurns <= 2)
                boost -= 3f;
            else if (selectedItem.lifeTurns <= 4)
                boost -= 1.5f;
        }

        return Mathf.Clamp(boost, 1f, 20f);
    }

    public void SelectItem(SellableItemData item)
    {
        selectedItem = item;
        RefreshUI();
        RefreshPromotionPanelUI();
    }

    public void RefreshUI()
    {
        RefreshTopUI();
        RefreshCenterUI();
        RefreshSummaryUI();
        RefreshLogUI();
        RefreshSettlementPanelUI();

        if (sellItemListUI != null)
        {
            sellItemListUI.RefreshList(sellableItems);
        }
    }

    void RefreshTopUI()
    {
        if (goldText != null)
        {
            goldText.text = "보유 골드 : " + currentGold + " G";
        }
    }

   void RefreshCenterUI()
    {
        if (selectedItem == null)
        {
            if (selectedItemTitleText != null)
                selectedItemTitleText.text = "선택된 아이템 없음";

            if (itemInfoText != null)
            {
                itemInfoText.text =
                    "판매할 아이템을 왼쪽 목록에서 선택하세요.\n\n" +
                    "제작파트에서 아이템을 만든 뒤 판매파트로 이동하면\n" +
                    "이곳에서 고객 몰림도, 가격 적정성, 예상 판매 확률을 확인할 수 있습니다.";
            }

            if (trendValueText != null)
                trendValueText.text = "유행 지수 : -";

            if (currentPriceText != null)
                currentPriceText.text = "현재 판매가 : -";

            if (trendSlider != null)
                trendSlider.value = 0f;

            ClearCrowdUI();

            return;
        }

        if (selectedItem.craftedItem == null)
        {
            if (selectedItemTitleText != null)
                selectedItemTitleText.text = "아이템 데이터 오류";

            if (itemInfoText != null)
                itemInfoText.text = "선택된 판매 아이템에 제작 결과 데이터가 없습니다.";

            if (trendValueText != null)
                trendValueText.text = "유행 지수 : -";

            if (currentPriceText != null)
                currentPriceText.text = "현재 판매가 : -";

            if (trendSlider != null)
                trendSlider.value = 0f;

            ClearCrowdUI();

            return;
        }

        CraftedItemResult crafted = selectedItem.craftedItem;

        UpdateSalesPrediction(selectedItem);
        RefreshCrowdUI(selectedItem);

        if (selectedItemTitleText != null)
            selectedItemTitleText.text = crafted.itemName;

        string priceModeText = selectedItem.useManualPrice ? "수동 가격" : "자동 가격";
        int currentPrice = CalculateCurrentPrice(selectedItem);

        if (itemInfoText != null)
        {
            itemInfoText.text =
                "등급 : " + crafted.grade + "\n" +
                "기본 가격 : " + crafted.finalPrice + " G\n" +
                "현재 판매가 : " + currentPrice + " G (" + priceModeText + ")\n" +
                "타겟 고객층 : " + crafted.targetAudience + "\n" +
                "재고 : " + selectedItem.stock + "\n" +
                "남은 수명 : " + selectedItem.lifeTurns;
        }

        if (marketStatusText != null)
        {
            marketStatusText.text =
                "가격 적정성 : " + selectedItem.priceEvaluation + "\n" +
                "예상 판매 확률 : " + Mathf.RoundToInt(selectedItem.expectedSellChance) + "%\n" +
                "추천 행동 : " + selectedItem.recommendationMessage + "\n" +
                "위험 원인 : " + selectedItem.riskMessage;
        }

        if (trendValueText != null)
            trendValueText.text = "유행 지수 : " + Mathf.RoundToInt(selectedItem.trendValue);

        if (currentPriceText != null)
            currentPriceText.text = "현재 판매가 : " + currentPrice + " G";

        if (marketStatusText != null)
            marketStatusText.text = "시장 반응 정보가 여기에 표시됩니다.";

        if (trendSlider != null)
            trendSlider.value = selectedItem.trendValue / 100f;
    }

    void RefreshCrowdUI(SellableItemData item)
    {
        if (item == null)
        {
            ClearCrowdUI();
            return;
        }

        float crowd = Mathf.Clamp(item.crowdLevel, 0f, 100f);

        if (crowdSlider != null)
            crowdSlider.value = crowd;

        if (crowdValueText != null)
            crowdValueText.text = Mathf.RoundToInt(crowd) + "%";

        if (customerIcons != null)
        {
            int activeCount = Mathf.CeilToInt(crowd / 20f);

            for (int i = 0; i < customerIcons.Length; i++)
            {
                if (customerIcons[i] == null)
                    continue;

                if (i < activeCount)
                    customerIcons[i].color = Color.white;
                else
                    customerIcons[i].color = new Color(1f, 1f, 1f, 0.25f);
            }
        }
    }

    void ClearCrowdUI()
    {
        if (crowdSlider != null)
            crowdSlider.value = 0f;

        if (crowdValueText != null)
            crowdValueText.text = "-";

        if (customerIcons != null)
        {
            for (int i = 0; i < customerIcons.Length; i++)
            {
                if (customerIcons[i] != null)
                    customerIcons[i].color = new Color(1f, 1f, 1f, 0.25f);
            }
        }
    }
    public void OnClickSellNormal()
    {
        if (selectedItem == null)
        {
            AddLog("선택된 아이템이 없습니다.");
            return;
        }

        if (selectedItem.stock <= 0)
        {
            AddLog("재고가 없습니다.");
            return;
        }

        UpdateSalesPrediction(selectedItem);

        float roll = Random.Range(0f, 100f);

        if (roll > selectedItem.expectedSellChance)
        {
            AddLog("[정가 판매 실패] " + selectedItem.craftedItem.itemName +
                " / 예상 확률 " + Mathf.RoundToInt(selectedItem.expectedSellChance) + "% / " +
                selectedItem.riskMessage);

            if (salesFeedbackUI != null)
                salesFeedbackUI.ShowFail(selectedItem);

            RefreshUI();
            return;
        }

        int sellPrice = CalculateCurrentPrice(selectedItem);

        selectedItem.stock -= 1;
        currentGold += sellPrice;
        totalSales += sellPrice;
        totalSoldCount += 1;

        AddLog("[정가 판매 성공] " + selectedItem.craftedItem.itemName +
            " / +" + sellPrice + " G / 고객 몰림도 " +
            Mathf.RoundToInt(selectedItem.crowdLevel) + "%");

        if (salesFeedbackUI != null)
            salesFeedbackUI.ShowSuccess(selectedItem, sellPrice);

        RemoveItemIfEmpty(selectedItem);
        RefreshUI();
    }

    public void OnClickSellDiscount()
    {
        if (selectedItem == null)
        {
            AddLog("선택된 아이템이 없습니다.");
            return;
        }

        if (selectedItem.stock <= 0)
        {
            AddLog("재고가 없습니다.");
            return;
        }

        UpdateSalesPrediction(selectedItem);

        float discountChance = Mathf.Clamp(selectedItem.expectedSellChance + 20f, 0f, 100f);
        float roll = Random.Range(0f, 100f);

        if (roll > discountChance)
        {
            AddLog("[할인 판매 실패] " + selectedItem.craftedItem.itemName +
                " / 예상 확률 " + Mathf.RoundToInt(discountChance) + "% / " +
                selectedItem.riskMessage);

            if (salesFeedbackUI != null)
                salesFeedbackUI.ShowFail(selectedItem);

            RefreshUI();
            return;
        }

        int sellPrice = CalculateDiscountPrice(selectedItem);

        selectedItem.stock -= 1;
        currentGold += sellPrice;
        totalSales += sellPrice;
        totalDiscountSales += sellPrice;
        totalSoldCount += 1;

        AddLog("[할인 판매 성공] " + selectedItem.craftedItem.itemName +
            " / +" + sellPrice + " G / 할인으로 구매 장벽 감소");

        if (salesFeedbackUI != null)
            salesFeedbackUI.ShowSuccess(selectedItem, sellPrice);

        RemoveItemIfEmpty(selectedItem);
        RefreshUI();
    }
    public void OnClickPriceUp()
    {
        if (selectedItem == null)
        {
            AddLog("가격을 조정할 아이템이 없습니다.");
            return;
        }

        int currentPrice = CalculateCurrentPrice(selectedItem);
        selectedItem.manualPrice = currentPrice + 10;
        selectedItem.useManualPrice = true;

        UpdateSalesPrediction(selectedItem);

        AddLog("[가격 조정] 판매가를 " + selectedItem.manualPrice + "G로 올렸습니다.");

        RefreshUI();
    }

    public void OnClickPriceDown()
    {
        if (selectedItem == null)
        {
            AddLog("가격을 조정할 아이템이 없습니다.");
            return;
        }

        int currentPrice = CalculateCurrentPrice(selectedItem);
        selectedItem.manualPrice = Mathf.Max(1, currentPrice - 10);
        selectedItem.useManualPrice = true;

        UpdateSalesPrediction(selectedItem);

        AddLog("[가격 조정] 판매가를 " + selectedItem.manualPrice + "G로 내렸습니다.");

        RefreshUI();
    }

    public void OnClickPriceReset()
    {
        if (selectedItem == null)
        {
            AddLog("가격을 초기화할 아이템이 없습니다.");
            return;
        }

        selectedItem.manualPrice = 0;
        selectedItem.useManualPrice = false;

        UpdateSalesPrediction(selectedItem);

        AddLog("[가격 조정] 유행 지수 기반 자동 가격으로 되돌렸습니다.");

        RefreshUI();
    }

    void RemoveItemIfEmpty(SellableItemData item)
    {
        if (item.stock > 0)
            return;

        sellableItems.Remove(item);
        AddLog(item.craftedItem.itemName + " 재고 소진");

        if (selectedItem == item)
        {
            if (sellableItems.Count > 0)
                selectedItem = sellableItems[0];
            else
                selectedItem = null;
        }
    }

    public void OnClickNextTick()
    {
        for (int i = 0; i < sellableItems.Count; i++)
        {
            UpdateTrend(sellableItems[i]);
        }

        AddLog("시간 경과: 유행 지수가 변동했습니다.");
        RefreshUI();
    }

    public void OnClickPromote()
    {
        if (selectedItem == null)
        {
            if (promotionPanelUI != null && promotionPanelUI.resultText != null)
                promotionPanelUI.resultText.text = "홍보할 아이템을 먼저 선택하세요.";
            return;
        }

        if (promotionPanelUI == null)
            return;

        if (promotionPanelUI.backgroundDropdown == null ||
            promotionPanelUI.filterDropdown == null ||
            promotionPanelUI.hashtagDropdown1 == null ||
            promotionPanelUI.hashtagDropdown2 == null ||
            promotionPanelUI.hashtagDropdown3 == null)
        {
            return;
        }

        string background =
            promotionPanelUI.backgroundDropdown.options[promotionPanelUI.backgroundDropdown.value].text;

        string filter =
            promotionPanelUI.filterDropdown.options[promotionPanelUI.filterDropdown.value].text;

        string tag1 =
            promotionPanelUI.hashtagDropdown1.options[promotionPanelUI.hashtagDropdown1.value].text;

        string tag2 =
            promotionPanelUI.hashtagDropdown2.options[promotionPanelUI.hashtagDropdown2.value].text;

        string tag3 =
            promotionPanelUI.hashtagDropdown3.options[promotionPanelUI.hashtagDropdown3.value].text;

        int promotionCost = 20;

        if (currentGold < promotionCost)
        {
            if (promotionPanelUI.resultText != null)
                promotionPanelUI.resultText.text = "골드가 부족해서 홍보할 수 없습니다.";
            return;
        }

        float boost = CalculatePromotionBoost(background, filter, tag1, tag2, tag3);

        selectedItem.trendValue += boost;
        selectedItem.trendValue = Mathf.Clamp(selectedItem.trendValue, 0f, 100f);

        selectedItem.promotionCount += 1;
        selectedItem.lastPromotionBoost = boost;

        currentGold -= promotionCost;
        totalPromotionCost += promotionCost;

        if (promotionPanelUI.resultText != null)
        {
           promotionPanelUI.resultText.text =
                "홍보 성공!\n" +
                "배경 : " + background + "\n" +
                "필터 : " + filter + "\n" +
                "해시태그 : #" + tag1 + " #" + tag2 + " #" + tag3 + "\n" +
                "유행 지수 상승 : +" + boost.ToString("F1") + "\n" +
                "홍보비 : -" + promotionCost + " G\n" +
                "누적 홍보 횟수 : " + selectedItem.promotionCount;
        }

        AddLog("[홍보] " + selectedItem.craftedItem.itemName + " / 유행 지수 +" + boost.ToString("F1"));

        RefreshUI();
        RefreshPromotionPanelUI();
    }

    void UpdateTrend(SellableItemData item)
    {
        if (item == null || item.craftedItem == null)
            return;

        item.lifeTurns -= 1;

        CraftedItemResult crafted = item.craftedItem;

        float change = Random.Range(-8f, 9f);

        if (crafted.freshnessScore >= 60 && item.lifeTurns >= 4)
            change += 8f;

        if (crafted.popularityScore >= 60)
            change += 3f;

        if (crafted.stabilityScore >= 60)
            change += 4f;
        else if (crafted.stabilityScore < 30)
            change -= 6f;

        if (crafted.grade == "S")
            change += 8f;
        else if (crafted.grade == "A")
            change += 4f;

        if (crafted.aiBonus > 0)
            change += 3f;

        if (item.lifeTurns <= 2)
            change -= 18f;
        else if (item.lifeTurns <= 4)
            change -= 6f;

        item.trendValue = Mathf.Clamp(item.trendValue + change, 0f, 100f);

        UpdateSalesPrediction(item);
    }

    void AddLog(string message)
    {
        sellLogs.Insert(0, "[기록] " + message);
        RefreshLogUI();
    }

    void RefreshLogUI()
    {
        if (logText == null) return;

        if (sellLogs.Count == 0)
        {
            logText.text = "판매 로그가 여기에 표시됩니다.";
            return;
        }

        string text = "";

        for (int i = 0; i < sellLogs.Count; i++)
        {
            text += sellLogs[i] + "\n\n";
        }

        logText.text = text;
    }

    void RefreshSummaryUI()
    {
        if (summaryText != null)
        {
            summaryText.text =
                "총 판매 수익 : " + totalSales + " G" +
                "   |   할인 판매 수익 : " + totalDiscountSales + " G" +
                "   |   판매 개수 : " + totalSoldCount +
                "   |   홍보비 : " + totalPromotionCost + " G";
        }
    }

    void RefreshPromotionPanelUI()
    {
        if (promotionPanelUI == null || promotionPanelUI.selectedItemText == null)
            return;

        if (selectedItem == null)
        {
            promotionPanelUI.selectedItemText.text = "선택 아이템: 없음";
            return;
        }

        promotionPanelUI.selectedItemText.text = "선택 아이템: " + selectedItem.craftedItem.itemName;
    }

    void RefreshSettlementPanelUI()
    {
        if (settlementPanelUI == null)
            return;

        int totalRevenue = totalSales + totalDiscountSales;
        int totalCost = materialCost + rentCost + manageCost + totalPromotionCost;
        int netProfit = totalRevenue - totalCost;

        if (settlementPanelUI.detailText != null)
        {
            settlementPanelUI.detailText.text =
                "총매출: " + totalRevenue + " G\n" +
                "재료비: " + materialCost + " G\n" +
                "임대료: " + rentCost + " G\n" +
                "홍보비: " + totalPromotionCost + " G\n" +
                "관리비: " + manageCost + " G\n" +
                "순이익: " + netProfit + " G";
        }

        int maxValue = Mathf.Max(totalRevenue, materialCost, rentCost, totalPromotionCost, manageCost, Mathf.Abs(netProfit), 1);

        SetBarWidth(settlementPanelUI.salesBarFill, totalRevenue, maxValue);
        SetBarWidth(settlementPanelUI.materialBarFill, materialCost, maxValue);
        SetBarWidth(settlementPanelUI.rentBarFill, rentCost, maxValue);
        SetBarWidth(settlementPanelUI.promotionBarFill, totalPromotionCost, maxValue);
        SetBarWidth(settlementPanelUI.manageBarFill, manageCost, maxValue);
        SetBarWidth(settlementPanelUI.profitBarFill, Mathf.Abs(netProfit), maxValue);

        if (settlementPanelUI.salesBarLabel != null)
            settlementPanelUI.salesBarLabel.text = "총매출  " + totalRevenue + " G";

        if (settlementPanelUI.materialBarLabel != null)
            settlementPanelUI.materialBarLabel.text = "재료비  " + materialCost + " G";

        if (settlementPanelUI.rentBarLabel != null)
            settlementPanelUI.rentBarLabel.text = "임대료  " + rentCost + " G";

        if (settlementPanelUI.promotionBarLabel != null)
            settlementPanelUI.promotionBarLabel.text = "홍보비  " + totalPromotionCost + " G";

        if (settlementPanelUI.manageBarLabel != null)
            settlementPanelUI.manageBarLabel.text = "관리비  " + manageCost + " G";

            if (settlementPanelUI.profitBarLabel != null)
            {
            if (netProfit >= 0)
                settlementPanelUI.profitBarLabel.text = "순이익  " + netProfit + " G";
            else
                settlementPanelUI.profitBarLabel.text = "순손실  " + netProfit + " G";
        }
        if (settlementPanelUI.profitBarFill != null)
        {
        Image profitImage = settlementPanelUI.profitBarFill.GetComponent<Image>();
        if (profitImage != null)
        {
            if (netProfit >= 0)
                profitImage.color = new Color(0.3f, 0.85f, 0.4f, 1f);
            else
                profitImage.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            }
        }
    }

    public void OpenSNSPromotionStudio()
    {
        if (selectedItem == null)
        {
            AddLog("SNS 홍보할 아이템을 먼저 선택하세요.");
            return;
        }

        if (snsPromotionStudioUI == null)
        {
            AddLog("SNS 홍보 스튜디오가 연결되지 않았습니다.");
            return;
        }

        snsPromotionStudioUI.OpenStudio(selectedItem);
    }

    public void ApplySNSPromotionFromStudio(
        SellableItemData item,
        float trendBoost,
        float crowdBoost,
        int cost,
        string caption,
        string hashtags)
    {
        if (item == null || item.craftedItem == null)
            return;

        if (currentGold < cost)
        {
            AddLog("[SNS 홍보 실패] 골드가 부족합니다.");
            return;
        }

        currentGold -= cost;
        totalPromotionCost += cost;

        item.promotionCount += 1;
        item.lastPromotionBoost = trendBoost;

        item.trendValue = Mathf.Clamp(item.trendValue + trendBoost, 0f, 100f);
        item.crowdLevel = Mathf.Clamp(item.crowdLevel + crowdBoost, 0f, 100f);

        UpdateSalesPrediction(item);

        AddLog("[SNS 홍보] " + item.craftedItem.itemName +
            " / 유행 +" + Mathf.RoundToInt(trendBoost) +
            " / 고객 몰림 +" + Mathf.RoundToInt(crowdBoost) + "%" +
            " / 비용 -" + cost + "G");

        RefreshUI();
    }

    public void OpenSettlementFullPanel()
    {
        if (settlementFullPanelUI == null)
        {
            AddLog("큰 정산 패널이 연결되지 않았습니다.");
            return;
        }

        int normalSales = totalSales - totalDiscountSales;
        if (normalSales < 0)
            normalSales = 0;

        settlementFullPanelUI.Show(
            totalSales,
            normalSales,
            totalDiscountSales,
            totalPromotionCost,
            materialCost,
            rentCost,
            manageCost,
            totalSoldCount
        );
    }

    public void ResetAfterSettlement()
    {
        sellableItems.Clear();
        selectedItem = null;

        totalSales = 0;
        totalDiscountSales = 0;
        totalSoldCount = 0;
        totalPromotionCost = 0;

        sellLogs.Clear();

        processedCraftedItemCount = 0;

        RefreshUI();
    }
    void SetBarWidth(RectTransform barRect, int value, int maxValue)
    {
        if (barRect == null)
            return;

        float maxWidth = 260f;
        float ratio = (float)value / maxValue;
        ratio = Mathf.Clamp01(ratio);

        barRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth * ratio);
    }

    public void GoBackToCraftScene()
    {
        SceneManager.LoadScene("CraftScene");
    }
}