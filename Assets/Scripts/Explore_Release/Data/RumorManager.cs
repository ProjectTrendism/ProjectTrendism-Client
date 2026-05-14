using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RumorManager : MonoBehaviour
{
    public static RumorManager Instance;

    [Header("수집한 소문")]
    public List<RumorData> collectedRumors = new List<RumorData>();

    [Header("키워드 등장 횟수")]
    public Dictionary<string, int> keywordCounts = new Dictionary<string, int>();

    [Header("유행 분석 점수")]
    public Dictionary<string, int> keywordTrendScores = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("RumorManager 유지 시작");
        }
        else
        {
            Debug.Log("중복 RumorManager 삭제");
            Destroy(gameObject);
        }
    }

    public void AddRumor(RumorData newRumor)
    {
        if (newRumor == null) return;

        if (string.IsNullOrEmpty(newRumor.relatedKeyword))
        {
            Debug.LogWarning("relatedKeyword가 비어 있는 소문은 저장하지 않습니다.");
            return;
        }

        if (string.IsNullOrEmpty(newRumor.rumorType))
        {
            newRumor.rumorType = newRumor.isRareHint ? "희귀 소문" : "일반 소문";
        }

        if (newRumor.reliability <= 0)
        {
            newRumor.reliability = newRumor.isRareHint ? 90 : 50;
        }

        if (newRumor.trendWeight <= 0)
        {
            newRumor.trendWeight = newRumor.isRareHint ? 3 : 1;
        }

        collectedRumors.Add(newRumor);

        AddKeywordCount(newRumor.relatedKeyword);
        AddTrendScore(newRumor.relatedKeyword, CalculateTrendScore(newRumor));

        Debug.Log("소문 추가: " + newRumor.rumorText);
        Debug.Log("현재 소문 개수: " + collectedRumors.Count);
        Debug.Log("키워드 [" + newRumor.relatedKeyword + "] 유행 점수: " + GetKeywordTrendScore(newRumor.relatedKeyword));

        RumorLogUI rumorUI = FindObjectOfType<RumorLogUI>();
        if (rumorUI != null)
        {
            rumorUI.RefreshRumorUI();
        }
    }

    private void AddKeywordCount(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return;

        if (keywordCounts.ContainsKey(keyword))
        {
            keywordCounts[keyword]++;
        }
        else
        {
            keywordCounts[keyword] = 1;
        }
    }

    private void AddTrendScore(string keyword, int score)
    {
        if (string.IsNullOrEmpty(keyword)) return;

        if (keywordTrendScores.ContainsKey(keyword))
        {
            keywordTrendScores[keyword] += score;
        }
        else
        {
            keywordTrendScores[keyword] = score;
        }
    }

    private int CalculateTrendScore(RumorData rumor)
    {
        int score = Mathf.Max(1, rumor.trendWeight);

        if (rumor.reliability >= 80)
        {
            score += 2;
        }
        else if (rumor.reliability >= 60)
        {
            score += 1;
        }

        if (rumor.isRareHint)
        {
            score += 2;
        }

        return score;
    }

    public int GetKeywordCount(string keyword)
    {
        if (keywordCounts.ContainsKey(keyword))
        {
            return keywordCounts[keyword];
        }

        return 0;
    }

    public int GetKeywordTrendScore(string keyword)
    {
        if (keywordTrendScores.ContainsKey(keyword))
        {
            return keywordTrendScores[keyword];
        }

        return 0;
    }

    public Dictionary<string, int> GetAllKeywordCounts()
    {
        return keywordCounts;
    }

    public Dictionary<string, int> GetAllTrendScores()
    {
        return keywordTrendScores;
    }

    public List<RumorData> GetAllRumors()
    {
        return collectedRumors;
    }

    public string GetTrendSummaryText()
    {
        StringBuilder builder = new StringBuilder();

        if (keywordTrendScores.Count == 0)
        {
            builder.AppendLine("아직 분석할 유행 정보가 없습니다.");
            return builder.ToString();
        }

        builder.AppendLine("유행 후보 점수");
        builder.AppendLine();

        foreach (var pair in keywordTrendScores)
        {
            string keyword = pair.Key;
            int score = pair.Value;
            int count = GetKeywordCount(keyword);

            builder.AppendLine("- " + keyword + " / 등장 " + count + "회 / 유행 점수 " + score);
        }

        return builder.ToString();
    }

    public void ClearRumors()
    {
        collectedRumors.Clear();
        keywordCounts.Clear();
        keywordTrendScores.Clear();

        RumorLogUI rumorUI = FindObjectOfType<RumorLogUI>();
        if (rumorUI != null)
        {
            rumorUI.RefreshRumorUI();
        }
    }
}