using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldTrendAnalysisManager : MonoBehaviour
{
    [Header("유행 구역 4개")]
    public TrendZone[] trendZones;

    [Header("사람 Prefab들")]
    public GameObject[] peoplePrefabs;

    [Header("사람 생성 설정")]
    public int peoplePerTrendScore = 4;
    public int maxVisiblePeoplePerZone = 16;

    [Header("구역 안 랜덤 배치 범위")]
    public float randomRangeX = 1.5f;
    public float randomRangeY = 0.65f;

    [Header("사람 크기")]
    public float peopleScale = 0.55f;

    [Header("선택 UI")]
    public TMP_Text selectedTrendText;
    public Button goToCraftButton;

    [Header("구역 선택 버튼")]
    public Button[] zoneSelectButtons;
    public TMP_Text[] zoneSelectButtonTexts;

    [Header("소문 요약 UI")]
    public TMP_Text rumorSummaryText;

    [Header("선택 설정")]
    public int maxSelectableKeywords = 2;

    private readonly List<string> selectedKeywords = new List<string>();

    private void Start()
    {
        if (goToCraftButton != null)
        {
            goToCraftButton.gameObject.SetActive(false);
        }

        LoadTrendData();
        RefreshSelectedText();
        RefreshRumorSummary();
    }

    private void LoadTrendData()
    {
        Debug.Log("===== 유행 분석 씬 데이터 확인 =====");

        if (RumorManager.Instance == null)
        {
            Debug.LogError("RumorManager.Instance를 찾지 못했습니다.");
            ShowEmptyZones();
            return;
        }

        Debug.Log("RumorManager 발견");
        Debug.Log("소문 개수: " + RumorManager.Instance.GetAllRumors().Count);
        Debug.Log("유행 점수 개수: " + RumorManager.Instance.GetAllTrendScores().Count);

        Dictionary<string, int> trendScores = RumorManager.Instance.GetAllTrendScores();

        if (trendScores != null)
        {
            foreach (var pair in trendScores)
            {
                Debug.Log("저장된 유행 점수: " + pair.Key + " = " + pair.Value);
            }
        }

        // 혹시 RumorManager의 trendScores가 비어 있으면 소문 목록을 보고 다시 계산
        if (trendScores == null || trendScores.Count == 0)
        {
            Debug.LogWarning("유행 점수 데이터가 비어 있어 소문 목록으로 다시 계산합니다.");
            trendScores = BuildTrendScoresFromRumors();
        }

        if (trendScores == null || trendScores.Count == 0)
        {
            Debug.LogWarning("계산된 유행 점수도 없습니다.");
            ShowEmptyZones();
            return;
        }

        List<KeyValuePair<string, int>> sortedTrends = trendScores
            .OrderByDescending(pair => pair.Value)
            .Take(trendZones.Length)
            .ToList();

        Debug.Log("표시할 유행 키워드 개수: " + sortedTrends.Count);

        for (int i = 0; i < trendZones.Length; i++)
        {
            if (trendZones[i] == null)
            {
                Debug.LogError("Trend Zones 배열의 " + i + "번이 비어 있습니다.");
                continue;
            }

            ClearPeople(trendZones[i]);

            if (i < sortedTrends.Count)
            {
                string keyword = sortedTrends[i].Key;
                int score = sortedTrends[i].Value;
                int population = Mathf.Max(1, score * peoplePerTrendScore);

                Debug.Log("구역 " + i + " 표시: " + keyword + " / 점수 " + score + " / 인구 " + population);

                trendZones[i].SetData(keyword, score, population);
                SpawnPeople(trendZones[i], population);
                SetZoneButtonState(i, true, keyword + " 선택");
            }
            else
            {
                trendZones[i].SetData("분석 데이터 없음", 0, 0);
                SetZoneButtonState(i, false, "데이터 없음");
            }
        }
    }

    private Dictionary<string, int> BuildTrendScoresFromRumors()
    {
        Dictionary<string, int> result = new Dictionary<string, int>();

        if (RumorManager.Instance == null)
        {
            return result;
        }

        List<RumorData> rumors = RumorManager.Instance.GetAllRumors();

        for (int i = 0; i < rumors.Count; i++)
        {
            RumorData rumor = rumors[i];

            if (rumor == null) continue;
            if (string.IsNullOrEmpty(rumor.relatedKeyword)) continue;

            int score = CalculateScoreFromRumor(rumor);

            if (result.ContainsKey(rumor.relatedKeyword))
            {
                result[rumor.relatedKeyword] += score;
            }
            else
            {
                result[rumor.relatedKeyword] = score;
            }
        }

        foreach (var pair in result)
        {
            Debug.Log("다시 계산한 유행 점수: " + pair.Key + " = " + pair.Value);
        }

        return result;
    }

    private int CalculateScoreFromRumor(RumorData rumor)
    {
        int score = rumor.trendWeight;

        if (score <= 0)
        {
            score = 1;
        }

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

    private void SpawnPeople(TrendZone zone, int population)
    {
        if (zone == null) return;
        if (zone.peopleParent == null)
        {
            Debug.LogError(zone.name + "의 PeopleParent가 연결되지 않았습니다.");
            return;
        }

        if (peoplePrefabs == null || peoplePrefabs.Length == 0)
        {
            Debug.LogWarning("People Prefabs가 비어 있어서 사람을 생성할 수 없습니다.");
            return;
        }

        int visibleCount = Mathf.Min(population, maxVisiblePeoplePerZone);

        for (int i = 0; i < visibleCount; i++)
        {
            GameObject prefab = peoplePrefabs[Random.Range(0, peoplePrefabs.Length)];

            if (prefab == null)
            {
                Debug.LogWarning("People Prefabs 안에 비어 있는 항목이 있습니다.");
                continue;
            }

            Vector3 randomOffset = new Vector3(
                Random.Range(-randomRangeX, randomRangeX),
                Random.Range(-randomRangeY, randomRangeY),
                0f
            );

            GameObject person = Instantiate(
                prefab,
                zone.peopleParent.position + randomOffset,
                Quaternion.identity,
                zone.peopleParent
            );

            person.name = "TrendPerson_" + i;
            person.transform.localScale = Vector3.one * peopleScale;

            SpriteRenderer renderer = person.GetComponentInChildren<SpriteRenderer>();

            if (renderer != null)
            {
                renderer.sortingOrder = 20 + i;
            }

            Collider2D[] colliders = person.GetComponentsInChildren<Collider2D>();

            for (int c = 0; c < colliders.Length; c++)
            {
                colliders[c].enabled = false;
            }

            Rigidbody2D rb = person.GetComponentInChildren<Rigidbody2D>();

            if (rb != null)
            {
                rb.simulated = false;
            }
        }
    }

    private void ClearPeople(TrendZone zone)
    {
        if (zone == null) return;
        if (zone.peopleParent == null) return;

        for (int i = zone.peopleParent.childCount - 1; i >= 0; i--)
        {
            Destroy(zone.peopleParent.GetChild(i).gameObject);
        }
    }

    private void ShowEmptyZones()
    {
        if (trendZones == null) return;

        for (int i = 0; i < trendZones.Length; i++)
        {
            if (trendZones[i] == null) continue;

            ClearPeople(trendZones[i]);
            trendZones[i].SetData("분석 데이터 없음", 0, 0);
            SetZoneButtonState(i, false, "데이터 없음");
        }

        if (rumorSummaryText != null)
        {
            rumorSummaryText.text =
                "탐험에서 수집한 소문이 없습니다.\n" +
                "탐험 씬에서 NPC와 대화하거나 오브젝트를 조사해야 합니다.";
        }
    }

    private void SetZoneButtonState(int index, bool active, string buttonText)
    {
        if (zoneSelectButtons != null && index >= 0 && index < zoneSelectButtons.Length)
        {
            if (zoneSelectButtons[index] != null)
            {
                zoneSelectButtons[index].interactable = active;
                zoneSelectButtons[index].gameObject.SetActive(active);
            }
        }

        if (zoneSelectButtonTexts != null && index >= 0 && index < zoneSelectButtonTexts.Length)
        {
            if (zoneSelectButtonTexts[index] != null)
            {
                zoneSelectButtonTexts[index].text = buttonText;
            }
        }
    }

    public void SelectZoneByIndex(int index)
    {
        if (index < 0 || index >= trendZones.Length) return;

        TrendZone zone = trendZones[index];

        if (zone == null) return;
        if (!zone.HasValidData()) return;

        string keyword = zone.keyword;

        if (selectedKeywords.Contains(keyword))
        {
            selectedKeywords.Remove(keyword);
        }
        else
        {
            if (selectedKeywords.Count >= maxSelectableKeywords)
            {
                Debug.Log("이미 유행 키워드를 " + maxSelectableKeywords + "개 선택했습니다.");
                return;
            }

            selectedKeywords.Add(keyword);
        }

        RefreshSelectedText();
    }

    private void RefreshSelectedText()
    {
        if (selectedTrendText == null) return;

        if (selectedKeywords.Count == 0)
        {
            selectedTrendText.text = "선택한 유행 키워드: 없음";
        }
        else
        {
            selectedTrendText.text = "선택한 유행 키워드: " + string.Join(" + ", selectedKeywords);
        }

        if (goToCraftButton != null)
        {
            goToCraftButton.gameObject.SetActive(selectedKeywords.Count >= maxSelectableKeywords);
        }
    }

    private void RefreshRumorSummary()
    {
        if (rumorSummaryText == null) return;

        if (RumorManager.Instance == null)
        {
            rumorSummaryText.text = "소문 데이터를 찾을 수 없습니다.";
            return;
        }

        List<RumorData> rumors = RumorManager.Instance.GetAllRumors();

        if (rumors == null || rumors.Count == 0)
        {
            rumorSummaryText.text = "수집한 소문이 없습니다.";
            return;
        }

        StringBuilder builder = new StringBuilder();

        int maxLines = Mathf.Min(rumors.Count, 5);

        for (int i = 0; i < maxLines; i++)
        {
            RumorData rumor = rumors[i];

            string rareMark = rumor.isRareHint ? "[희귀] " : "";

            builder.AppendLine(rareMark + rumor.sourceNPC + " : " + rumor.relatedKeyword);
            builder.AppendLine("- " + rumor.rumorText);
            builder.AppendLine("신뢰도 " + rumor.reliability + " / 영향도 " + rumor.trendWeight);
            builder.AppendLine();
        }

        if (rumors.Count > maxLines)
        {
            builder.AppendLine("외 " + (rumors.Count - maxLines) + "개의 소문이 더 있습니다.");
        }

        rumorSummaryText.text = builder.ToString();
    }

    public void GoToCraftScene()
    {
        if (selectedKeywords.Count < maxSelectableKeywords)
        {
            Debug.Log("유행 키워드를 더 선택해야 합니다.");
            return;
        }

        TrendSelectionData.SelectedTrendKeywords = new List<string>(selectedKeywords);

        SceneManager.LoadScene("CraftScene");
    }
}