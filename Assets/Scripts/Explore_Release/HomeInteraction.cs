using UnityEngine;

public class HomeInteraction : MonoBehaviour
{
    private bool playerInRange = false;

    private UIManager uiManager;
    private ExplorationSummaryManager summaryManager;
    private TimeManager timeManager;

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
        summaryManager = FindObjectOfType<ExplorationSummaryManager>();
        timeManager = FindObjectOfType<TimeManager>();
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            GoHome();
        }
    }

    void GoHome()
    {
        if (summaryManager != null)
        {
            string reason = "정상적으로 귀가했습니다.\n수집한 소문을 바탕으로 유행 분석 단계로 넘어갑니다.";
            summaryManager.ShowSummary(reason);
        }
        else
        {
            Debug.Log("정상 귀가 처리");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (uiManager != null)
                uiManager.ShowInteractHint("E키로 귀가");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (uiManager != null)
                uiManager.HideInteractHint();
        }
    }
}