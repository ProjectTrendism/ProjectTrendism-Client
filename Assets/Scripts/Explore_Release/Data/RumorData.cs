using System;

[Serializable]
public class RumorData
{
    public string rumorId;
    public string rumorText;
    public string sourceNPC;
    public string zoneName;
    public string relatedKeyword;
    public bool isRareHint;

    // 0~100, 높을수록 믿을 만한 소문
    public int reliability = 50;

    // 유행 점수에 주는 영향도
    public int trendWeight = 1;

    // UI 표시용
    public string rumorType = "일반 소문";
}