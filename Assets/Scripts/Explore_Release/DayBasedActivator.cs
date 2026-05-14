using UnityEngine;

public class DayBasedActivator : MonoBehaviour
{
    public int requiredDay = 3;

    private TimeManager timeManager;
    private bool hasActivated = false;

    void Start()
    {
        timeManager = FindObjectOfType<TimeManager>();

        // 시작 시 일단 꺼두기
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (hasActivated) return;
        if (timeManager == null) return;

        if (timeManager.currentDay >= requiredDay)
        {
            gameObject.SetActive(true);
            hasActivated = true;
        }
    }
}