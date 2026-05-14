using System;

[Serializable]
public class CraftAnalysisResult
{
    public string previewName;

    public int minPrice;
    public int maxPrice;
    public int finalPrice;

    public int craftScore;
    public string grade;

    public string targetAudience;
    public string trendLevel;

    public int popularityScore;
    public int freshnessScore;
    public int stabilityScore;

    public int aiBonus;
    public int trendBonus;

    public string riskMessage;
    public string adviceMessage;
    public string balanceMessage;

    public string aiMessage;
    public string aiDirectionMessage;
    public string aiRisingKeywordName;

    public string baseKeywordName;
    public string styleKeywordName;
    public string conceptKeywordName;
}