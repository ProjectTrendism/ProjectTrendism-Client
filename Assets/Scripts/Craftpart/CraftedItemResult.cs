/*
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
*/

// Craftpart/CraftedItemResult.cs 교체
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

    // ── 서버 연동용 추가 필드 ──
    public string itemDescription;   // 서버 AI 생성 설명
    public string imageUrl;          // 서버 image_url (상대경로 또는 절대경로, ApiManager가 처리)
    public int serverItemId;         // 서버 recipe.id (선택, 추후 판매 연동용)
}

