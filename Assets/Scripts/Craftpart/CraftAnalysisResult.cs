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

    // 피드백 반영용 제작 세부 평가
    public int trendFitScore;          // 0~30: 현재 유행/AI 추천과 얼마나 맞는가
    public int combinationScore;       // 0~25: Base/Style/Concept 조합이 안정적인가
    public int informationScore;       // 0~20: 탐험 정보가 충분하고 믿을 만한가
    public int targetClarityScore;     // 0~15: 타겟 고객층이 명확한가
    public int originalityScore;       // 0~10: 차별화/신선함이 있는가

    public string detailScoreMessage;  // 세부 평가 표시용
    public string problemMessage;      // 낮은 등급 원인
    public string nextExploreMessage;  // 다음 탐험에서 보완할 점
}
