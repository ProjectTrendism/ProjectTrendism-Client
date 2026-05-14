using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TrendAnalysisManager : MonoBehaviour
{
    [Header("부스 UI")]
    public TMP_Text[] boothKeywordTexts;
    public TMP_Text[] boothPopulationTexts;
    public TMP_Text[] boothScoreTexts;
    public Button[] boothSelectButtons;

    [Header("사람 몰림 시각화")]
    public Transform[] crowdParents;
    public GameObject personIconTemplate;

    [Tooltip("유행 점수 1점당 관심 인구 수")]
    public int peoplePerTrendScore = 4;

    [Tooltip("한 부스에 실제로 보여줄 최대 사람 아이콘 수")]
    public int maxVisiblePeoplePerBooth = 20;

    [Header("선택 결과 UI")]
    public TMP_Text selectedTrendText;
    public Button goToCraftButton;

    [Header("소문 요약 UI")]
    public TMP_Text rumorSummaryText;

    [Header("설정")]
    public int maxSelectableKeywords = 2;

    private List<string> displayedKeywords = new List<string>();
    private List<string> selectedKeywords = new List<string>();

    private void Start()
    {
        if (goToCraftButton != null)
        {
            goToCraftButton.gameObject.SetActive(false);
        }

        if (personIconTemplate != null)
        {
            personIconTemplate.SetActive(false);
        }

        LoadTrendData();
        RefreshSelectedText();
    }

    private void LoadTrendData()
    {
        if (RumorManager.Instance == null)
        {
            ShowEmptyData();
            return;
        }

        Dictionary<string, int> trendScores = RumorManager.Instance.GetAllTrendScores();

        if (trendScores == null || trendScores.Count == 0)
        {
            ShowEmptyData();
            return;
        }

        List<KeyValuePair<string, int>> sortedTrends = trendScores
            .OrderByDescending(pair => pair.Value)
            .Take(boothKeywordTexts.Length)
            .ToList();

        displayedKeywords.Clear();

        for (int i = 0; i < boothKeywordTexts.Length; i++)
        {
            ClearCrowd(i);

            if (i < sortedTrends.Count)
            {
                string keyword = sortedTrends[i].Key;
                int score = sortedTrends[i].Value;
                int count = RumorManager.Instance.GetKeywordCount(keyword);
                int population = Mathf.Max(1, score * peoplePerTrendScore);

                displayedKeywords.Add(keyword);

                boothKeywordTexts[i].text = keyword;
                boothPopulationTexts[i].text = "관심 인구: " + population + "명";
                boothScoreTexts[i].text = "등장 " + count + "회 / 유행 점수 " + score;

                DrawCrowd(i, population);

                int index = i;
                boothSelectButtons[i].gameObject.SetActive(true);
                boothSelectButtons[i].onClick.RemoveAllListeners();
                boothSelectButtons[i].onClick.AddListener(() => SelectKeyword(index));
            }
            else
            {
                displayedKeywords.Add("");

                boothKeywordTexts[i].text = "데이터 없음";
                boothPopulationTexts[i].text = "관심 인구: 0명";
                boothScoreTexts[i].text = "유행 점수: 0";

                if (boothSelectButtons[i] != null)
                {
                    boothSelectButtons[i].gameObject.SetActive(false);
                }
            }
        }

        RefreshRumorSummary();
    }

    private void DrawCrowd(int boothIndex, int population)
    {
        if (crowdParents == null) return;
        if (personIconTemplate == null) return;
        if (boothIndex < 0 || boothIndex >= crowdParents.Length) return;
        if (crowdParents[boothIndex] == null) return;

        int visibleCount = Mathf.Min(population, maxVisiblePeoplePerBooth);

        for (int i = 0; i < visibleCount; i++)
        {
            GameObject icon = Instantiate(personIconTemplate, crowdParents[boothIndex]);
            icon.SetActive(true);
            icon.name = "PersonIcon_" + i;
        }
    }

    private void ClearCrowd(int boothIndex)
    {
        if (crowdParents == null) return;
        if (boothIndex < 0 || boothIndex >= crowdParents.Length) return;
        if (crowdParents[boothIndex] == null) return;

        for (int i = crowdParents[boothIndex].childCount - 1; i >= 0; i--)
        {
            Transform child = crowdParents[boothIndex].GetChild(i);

            if (child.gameObject == personIconTemplate)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void ShowEmptyData()
    {
        displayedKeywords.Clear();

        for (int i = 0; i < boothKeywordTexts.Length; i++)
        {
            displayedKeywords.Add("");

            ClearCrowd(i);

            boothKeywordTexts[i].text = "데이터 없음";
            boothPopulationTexts[i].text = "관심 인구: 0명";
            boothScoreTexts[i].text = "유행 점수: 0";

            if (boothSelectButtons[i] != null)
            {
                boothSelectButtons[i].gameObject.SetActive(false);
            }
        }

        if (rumorSummaryText != null)
        {
            rumorSummaryText.text =
                "탐험에서 수집한 소문이 없습니다.\n" +
                "탐험 씬으로 돌아가 NPC와 대화하거나 오브젝트를 조사해야 합니다.";
        }
    }

    private void SelectKeyword(int index)
    {
        if (index < 0 || index >= displayedKeywords.Count) return;

        string keyword = displayedKeywords[index];

        if (string.IsNullOrEmpty(keyword)) return;

        if (selectedKeywords.Contains(keyword))
        {
            selectedKeywords.Remove(keyword);
        }
        else
        {
            if (selectedKeywords.Count >= maxSelectableKeywords)
            {
                Debug.Log("이미 최대 선택 개수에 도달했습니다.");
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

        int maxLines = Mathf.Min(rumors.Count, 8);

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