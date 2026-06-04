using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExploreUIRefresher : MonoBehaviour
{
    public static ExploreUIRefresher Instance;

    [Header("키워드 UI")]
    public TMP_Text keywordListText;

    [Header("탐험 상태 UI")]
    public TMP_Text dayText;
    public TMP_Text timeText;
    public TMP_Text npcProgressText;
    public TMP_Text keywordCountText;
    public TMP_Text rumorCountText;

    [Header("요약 패널")]
    public GameObject summaryPanel;
    public TMP_Text summaryReasonText;
    public TMP_Text summaryKeywordText;
    public Button openSummaryButton;
    public Button closeSummaryButton;
    public Button goToCraftButton;

    [Header("씬 이동")]
    public string craftSceneName = "CraftScene";

    [Header("설정")]
    public int totalNPCCount = 6;

    private readonly List<string> collectedKeywordNames = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        if (openSummaryButton != null)
        {
            openSummaryButton.onClick.RemoveAllListeners();
            openSummaryButton.onClick.AddListener(OpenSummaryPanel);
        }

        if (closeSummaryButton != null)
        {
            closeSummaryButton.onClick.RemoveAllListeners();
            closeSummaryButton.onClick.AddListener(CloseSummaryPanel);
        }

        if (goToCraftButton != null)
        {
            goToCraftButton.onClick.RemoveAllListeners();
            goToCraftButton.onClick.AddListener(GoToCraftScene);
        }

        RefreshAll();
    }

    public void RegisterKeyword(string keywordName)
    {
        if (string.IsNullOrEmpty(keywordName))
            return;

        if (!collectedKeywordNames.Contains(keywordName))
            collectedKeywordNames.Add(keywordName);

        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshKeywordPanel();
        RefreshStatusPanel();
    }

    public void RefreshKeywordPanel()
    {
        if (keywordListText == null)
            return;

        if (collectedKeywordNames.Count == 0)
        {
            keywordListText.text = "없음";
            return;
        }

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < collectedKeywordNames.Count; i++)
        {
            sb.AppendLine("- " + collectedKeywordNames[i]);
        }

        keywordListText.text = sb.ToString();
    }

    public void RefreshStatusPanel()
    {
        int keywordCount = collectedKeywordNames.Count;
        int rumorCount = GetRumorCount();
        int talkedNPCCount = GetTalkedNPCCount();

        if (dayText != null)
        {
            dayText.text = "탐험 1일차";
        }

        if (timeText != null)
        {
            timeText.text = "시간: 낮";
        }

        if (npcProgressText != null)
        {
            npcProgressText.text = $"NPC 대화: {talkedNPCCount} / {totalNPCCount}";
        }

        if (keywordCountText != null)
        {
            keywordCountText.text = $"키워드: {keywordCount}개";
        }

        if (rumorCountText != null)
        {
            rumorCountText.text = $"소문: {rumorCount}개";
        }
    }

    public void OpenSummaryPanel()
    {
        RefreshSummaryPanel();

        if (summaryPanel != null)
            summaryPanel.SetActive(true);
    }

    public void CloseSummaryPanel()
    {
        if (summaryPanel != null)
            summaryPanel.SetActive(false);
    }

    private void RefreshSummaryPanel()
    {
        int talked = GetTalkedNPCCount();

        if (summaryReasonText != null)
        {
            summaryReasonText.text =
                $"NPC 대화: {talked} / {totalNPCCount}\n" +
                $"수집한 정보를 바탕으로 제작 단계로 이동할 수 있습니다.";
        }

        if (summaryKeywordText != null)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[수집 키워드]");

            if (collectedKeywordNames.Count == 0)
            {
                sb.AppendLine("없음");
            }
            else
            {
                for (int i = 0; i < collectedKeywordNames.Count; i++)
                {
                    sb.AppendLine("- " + collectedKeywordNames[i]);
                }
            }

            sb.AppendLine();
            sb.AppendLine("[수집 소문]");

            AppendRumors(sb);

            summaryKeywordText.text = sb.ToString();
        }
    }

    private void AppendRumors(StringBuilder sb)
    {
        if (RumorManager.Instance == null)
        {
            sb.AppendLine("없음");
            return;
        }

        var rumors = RumorManager.Instance.GetAllRumors();

        if (rumors == null || rumors.Count == 0)
        {
            sb.AppendLine("없음");
            return;
        }

        foreach (var rumor in rumors)
        {
            if (rumor == null)
                continue;

            sb.AppendLine("- " + rumor.rumorText);
        }
    }

    private int GetRumorCount()
    {
        if (RumorManager.Instance == null)
            return 0;

        var rumors = RumorManager.Instance.GetAllRumors();
        return rumors == null ? 0 : rumors.Count;
    }

    private int GetTalkedNPCCount()
    {
        NPCInteraction[] npcs = FindObjectsByType<NPCInteraction>(FindObjectsSortMode.None);

        int count = 0;

        foreach (var npc in npcs)
        {
            if (npc != null && npc.HasTalkedThisCycle)
                count++;
        }

        if (totalNPCCount <= 0)
            totalNPCCount = npcs.Length;

        return count;
    }

    private void GoToCraftScene()
    {
        SceneManager.LoadScene(craftSceneName);
    }
}