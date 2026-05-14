using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AITrendAdvisor : MonoBehaviour
{
    [Header("AI 트렌드 분석 텍스트")]
    public TextMeshProUGUI risingKeywordText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI directionText;
    public TextMeshProUGUI riskText;
    public TextMeshProUGUI selectionText;

    private KeywordData risingKeyword;
    private string recommendedTarget = "대중형";
    private string recommendedDirection = "균형 잡힌 아이템";
    private string currentRisk = "선택한 키워드에 따라 달라집니다.";

    private void Start()
    {
        GenerateDailyAdvice();
        RefreshSelectionAdvice(null);
    }

    public void GenerateDailyAdvice()
    {
        KeywordManager keywordManager = FindObjectOfType<KeywordManager>();

        if (keywordManager == null)
        {
            SetEmptyAdvice("KeywordManager를 찾을 수 없습니다.");
            return;
        }

        List<KeywordData> keywords = keywordManager.GetKeywords();

        if (keywords == null || keywords.Count == 0)
        {
            SetEmptyAdvice("분석할 키워드가 없습니다.");
            return;
        }

        risingKeyword = FindBestTrendKeyword(keywords);
        recommendedTarget = GetRecommendedTarget(risingKeyword);
        recommendedDirection = GetRecommendedDirection(risingKeyword);
        currentRisk = GetRiskMessage(risingKeyword);

        RefreshBaseAdvice();
    }

    private KeywordData FindBestTrendKeyword(List<KeywordData> keywords)
    {
        KeywordData bestKeyword = keywords[0];
        int bestScore = -999;

        for (int i = 0; i < keywords.Count; i++)
        {
            KeywordData keyword = keywords[i];

            int score = keyword.popularity + keyword.freshness;

            if (keyword.keywordType == KeywordType.Concept)
                score += 10;

            if (score > bestScore)
            {
                bestScore = score;
                bestKeyword = keyword;
            }
        }

        return bestKeyword;
    }

    private string GetRecommendedTarget(KeywordData keyword)
    {
        if (keyword == null)
            return "대중형";

        if (!string.IsNullOrEmpty(keyword.raceTag))
            return keyword.raceTag + " 고객층";

        if (keyword.popularity >= 60)
            return "대중형 고객층";

        return "취향형 고객층";
    }

    private string GetRecommendedDirection(KeywordData keyword)
    {
        if (keyword == null)
            return "균형 잡힌 아이템";

        if (keyword.freshness >= 60 && keyword.popularity >= 50)
            return "화제성 높은 신상품";

        if (keyword.stability >= 60)
            return "오래 팔리는 안정형 상품";

        if (keyword.popularity >= 60)
            return "넓은 고객층을 노리는 대중형 상품";

        return "개성 있는 틈새 유행 상품";
    }

    private string GetRiskMessage(KeywordData keyword)
    {
        if (keyword == null)
            return "선택한 키워드에 따라 달라집니다.";

        if (keyword.stability < 30 && keyword.freshness >= 50)
            return "급상승 가능성은 높지만 유행이 빨리 식을 수 있음";

        if (keyword.popularity < 30)
            return "관심 고객층이 좁을 수 있음";

        if (keyword.freshness < 30)
            return "새로움이 부족해 SNS 반응이 약할 수 있음";

        return "큰 위험 요소는 낮음";
    }

    private void RefreshBaseAdvice()
    {
        if (risingKeywordText != null)
            risingKeywordText.text = "오늘의 급상승 키워드 : " + risingKeyword.keywordName;

        if (targetText != null)
            targetText.text = "추천 고객층 : " + recommendedTarget;

        if (directionText != null)
            directionText.text = "추천 제작 방향 : " + recommendedDirection;

        if (riskText != null)
            riskText.text = "위험 요소 : " + currentRisk;
    }

    public void RefreshSelectionAdvice(List<KeywordData> selectedKeywords)
    {
        if (selectionText == null)
            return;

        if (selectedKeywords == null || selectedKeywords.Count == 0)
        {
            selectionText.text = "현재 조합 평가 : 아직 키워드를 선택하지 않았습니다.";
            return;
        }

        bool hasBase = false;
        bool hasStyle = false;
        bool hasConcept = false;
        bool hasRisingKeyword = false;

        int totalPopularity = 0;
        int totalFreshness = 0;
        int totalStability = 0;

        for (int i = 0; i < selectedKeywords.Count; i++)
        {
            KeywordData keyword = selectedKeywords[i];

            if (keyword.keywordType == KeywordType.Base)
                hasBase = true;

            if (keyword.keywordType == KeywordType.Style)
                hasStyle = true;

            if (keyword.keywordType == KeywordType.Concept)
                hasConcept = true;

            if (risingKeyword != null && keyword.keywordName == risingKeyword.keywordName)
                hasRisingKeyword = true;

            totalPopularity += keyword.popularity;
            totalFreshness += keyword.freshness;
            totalStability += keyword.stability;
        }

        int count = selectedKeywords.Count;
        int avgPopularity = totalPopularity / count;
        int avgFreshness = totalFreshness / count;
        int avgStability = totalStability / count;

        if (hasBase && hasStyle && hasConcept && hasRisingKeyword)
        {
            selectionText.text = "현재 조합 평가 : AI 추천 키워드가 포함된 균형 좋은 조합입니다.";
            return;
        }

        if (hasBase && hasStyle && hasConcept)
        {
            selectionText.text = "현재 조합 평가 : Base, Style, Concept 균형이 좋습니다.";
            return;
        }

        if (!hasBase)
        {
            selectionText.text = "현재 조합 평가 : Base 키워드가 없어 기본 가격이 약할 수 있습니다.";
            return;
        }

        if (!hasStyle)
        {
            selectionText.text = "현재 조합 평가 : Style 키워드가 없어 고객층 특징이 약할 수 있습니다.";
            return;
        }

        if (!hasConcept)
        {
            selectionText.text = "현재 조합 평가 : Concept 키워드가 없어 유행성이 약할 수 있습니다.";
            return;
        }

        if (avgStability < 30 && avgFreshness >= 50)
        {
            selectionText.text = "현재 조합 평가 : 신선하지만 유행 수명이 짧을 수 있습니다.";
            return;
        }

        if (avgPopularity < 30)
        {
            selectionText.text = "현재 조합 평가 : 대중성이 낮아 판매량이 제한될 수 있습니다.";
            return;
        }

        selectionText.text = "현재 조합 평가 : 제작 가능한 조합입니다.";
    }

    private void SetEmptyAdvice(string message)
    {
        if (risingKeywordText != null)
            risingKeywordText.text = "오늘의 급상승 키워드 : -";

        if (targetText != null)
            targetText.text = "추천 고객층 : -";

        if (directionText != null)
            directionText.text = "추천 제작 방향 : -";

        if (riskText != null)
            riskText.text = "위험 요소 : " + message;

        if (selectionText != null)
            selectionText.text = "현재 조합 평가 : -";
    }
}