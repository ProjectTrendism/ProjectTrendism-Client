using UnityEngine;

public class EventNPCSpawner : MonoBehaviour
{
    public GameObject eventNPC;
    public int requiredDay = 3;

    private TimeManager timeManager;
    private bool hasSpawned = false;

    void Start()
    {
        timeManager = FindObjectOfType<TimeManager>();

        if (eventNPC != null)
        {
            eventNPC.SetActive(false);
        }
    }

    void Update()
    {
        if (hasSpawned) return;
        if (timeManager == null) return;
        if (eventNPC == null) return;

        if (timeManager.currentDay >= requiredDay)
        {
            eventNPC.SetActive(true);
            hasSpawned = true;
            Debug.Log("이벤트 NPC 등장!");
        }
    }
}