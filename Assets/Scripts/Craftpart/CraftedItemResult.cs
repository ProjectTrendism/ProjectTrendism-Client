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
}