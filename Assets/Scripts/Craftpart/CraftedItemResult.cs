using System;
using System.Collections.Generic;

[Serializable]
public class CraftedItemResult
{
    public string itemName;
    public int finalPrice;
    public string grade;
    public string targetAudience;
    public string trendLevel;
    public int craftScore;

    public int popularityScore;
    public int freshnessScore;
    public int stabilityScore;

    public int aiBonus;
    public int trendBonus;

    public bool isCorrected;
    public string correctionType;

    public string riskMessage;
    public string adviceMessage;
    public string analysisMessage;

    public string baseKeywordName;
    public string styleKeywordName;
    public string conceptKeywordName;

    public List<string> usedKeywordNames = new List<string>();

    // 피드백 반영용 제작 세부 평가
    public int trendFitScore;
    public int combinationScore;
    public int informationScore;
    public int targetClarityScore;
    public int originalityScore;

    public string detailScoreMessage;
    public string problemMessage;
    public string nextExploreMessage;

    // 서버 연동용 필드
    public string itemDescription;
    public string imageUrl;
    public int serverItemId;
}
