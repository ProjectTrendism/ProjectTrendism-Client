using System;
using UnityEngine;

[Serializable]
public class SellableItemData
{
    public CraftedItemResult craftedItem;

    [Header("기본 판매 상태")]
    public int stock = 1;
    public float trendValue = 50f;
    public int lifeTurns = 6;

    [Header("홍보 상태")]
    public int promotionCount = 0;
    public float lastPromotionBoost = 0f;

    [Header("가격 조정")]
    public int manualPrice = 0;
    public bool useManualPrice = false;

    [Header("판매 예측 데이터")]
    public float crowdLevel = 0f;
    public float expectedSellChance = 0f;
    public string priceEvaluation = "-";
    public string recommendationMessage = "-";
    public string riskMessage = "-";

    public SellableItemData(CraftedItemResult item)
    {
        craftedItem = item;

        stock = 1;
        trendValue = 50f;
        lifeTurns = 6;

        promotionCount = 0;
        lastPromotionBoost = 0f;

        manualPrice = 0;
        useManualPrice = false;

        crowdLevel = 0f;
        expectedSellChance = 0f;
        priceEvaluation = "-";
        recommendationMessage = "-";
        riskMessage = "-";
    }
}