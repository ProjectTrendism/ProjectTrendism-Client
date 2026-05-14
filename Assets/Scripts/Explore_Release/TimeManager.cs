using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;
    public Image darknessOverlay;

    [Header("Time Settings")]
    public int currentDay = 1;
    public int hour = 8;
    public int minute = 0;

    public float realSecondsPerGame10Minutes = 1f;

    private float timer;

    private bool hasEndedDay = false;

    void Start()
    {
        UpdateTimeUI();
        UpdateDarkness();
    }

    void Update()
    {
        if (hasEndedDay) return;

        timer += Time.deltaTime;

        if (timer >= realSecondsPerGame10Minutes)
        {
            timer = 0f;
            Add10Minutes();
        }
    }

    void Add10Minutes()
    {
        minute += 10;

        if (minute >= 60)
        {
            minute = 0;
            hour++;
        }

        if (hour >= 24)
        {
            hour = 24;
            minute = 0;
            EndExplorationByTimeout();
            return;
        }

        UpdateTimeUI();
        UpdateDarkness();
    }

    void UpdateTimeUI()
    {
        if (dayText != null)
            dayText.text = "Day " + currentDay;

        if (timeText != null)
            timeText.text = hour.ToString("00") + ":" + minute.ToString("00");
    }

    void UpdateDarkness()
    {
        if (darknessOverlay == null) return;

        float alpha = 0f;

        if (hour >= 22)
        {
            // 22:00부터 24:00까지 점점 어두워짐
            float totalMinutesAfter22 = (hour - 22) * 60 + minute;
            alpha = Mathf.Lerp(0.2f, 0.6f, totalMinutesAfter22 / 120f);
        }

        Color color = darknessOverlay.color;
        color.a = alpha;
        darknessOverlay.color = color;
    }

    void EndExplorationByTimeout()
    {
        hasEndedDay = true;

        if (dayText != null)
            dayText.text = "Day " + currentDay;

        if (timeText != null)
            timeText.text = "24:00";

        Debug.Log("시간 초과! 강제 귀가 처리");

        ExplorationSummaryManager summary = FindObjectOfType<ExplorationSummaryManager>();
        if (summary != null)
        {
            summary.ShowSummary("시간 초과로 강제 귀가했습니다.");
        }
    }

    public bool IsAfter22()
    {
        return hour >= 22;
    }

    public bool IsDayEnded()
    {
        return hasEndedDay;
    }
}