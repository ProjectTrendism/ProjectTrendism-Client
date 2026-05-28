using UnityEngine;

public class MapCycleManager : MonoBehaviour
{
    public static MapCycleManager Instance;

    [Header("자동 맵 생성기")]
    public AutoTileMapGenerator mapGenerator;

    [Header("플레이어")]
    public Transform player;

    [Tooltip("플레이어에 Rigidbody2D가 있으면 같이 연결해도 됩니다.")]
    public Rigidbody2D playerRb;

    [Header("게임 시작 시 맵 자동 생성")]
    public bool generateMapOnStart = true;

    [Header("사이클 정보")]
    public int currentCycle = 1;

    [Header("선택: 패널 전환용")]
    public GameObject explorationPanel;
    public GameObject craftPanel;
    public GameObject sellPanel;
    public GameObject settlementPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (generateMapOnStart)
        {
            StartNewExplorationCycle(false);
        }
    }

    /// <summary>
    /// 새 탐험 사이클 시작.
    /// 정산 완료 후에도 이 함수를 호출하면 됩니다.
    /// </summary>
    public void StartNewExplorationCycle(bool increaseCycle)
    {
        if (increaseCycle)
        {
            currentCycle++;
        }

        Debug.Log("[MapCycleManager] 새 탐험 사이클 시작: " + currentCycle);

        SetOnlyPanel(explorationPanel);

        GenerateNewRandomMap();
    }

    /// <summary>
    /// 랜덤 맵 생성 후 플레이어를 시작 위치로 이동.
    /// </summary>
    public void GenerateNewRandomMap()
    {
        if (mapGenerator == null)
        {
            Debug.LogWarning("[MapCycleManager] mapGenerator가 연결되지 않았습니다.");
            return;
        }

        mapGenerator.GenerateMap();

        MovePlayerToStartPoint();
    }

    private void MovePlayerToStartPoint()
    {
        if (player == null)
        {
            Debug.LogWarning("[MapCycleManager] player가 연결되지 않았습니다.");
            return;
        }

        if (mapGenerator == null)
        {
            Debug.LogWarning("[MapCycleManager] mapGenerator가 없습니다.");
            return;
        }

        if (mapGenerator.generatedPlayerStartPoint == null)
        {
            Debug.LogWarning("[MapCycleManager] generatedPlayerStartPoint가 없습니다. AutoTileMapGenerator에서 PlayerStartPoint 생성 여부를 확인하세요.");
            return;
        }

        Vector3 startPos = mapGenerator.generatedPlayerStartPoint.position;
        startPos.z = player.position.z;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.position = startPos;
        }
        else
        {
            player.position = startPos;
        }

        Debug.Log("[MapCycleManager] 플레이어 시작 위치 이동: " + startPos);
    }

    /// <summary>
    /// 정산 완료 버튼에서 이 함수를 연결하면 됨.
    /// </summary>
    public void OnClickSettlementComplete()
    {
        Debug.Log("[MapCycleManager] 정산 완료 클릭");

        ClearTemporaryCycleData();

        StartNewExplorationCycle(true);
    }

    public void GoToCraftPhase()
    {
        SetOnlyPanel(craftPanel);
    }

    public void GoToSellPhase()
    {
        SetOnlyPanel(sellPanel);
    }

    public void GoToSettlementPhase()
    {
        SetOnlyPanel(settlementPanel);
    }

    private void ClearTemporaryCycleData()
    {
        Debug.Log("[MapCycleManager] 사이클 임시 데이터 초기화");

        // 필요하면 나중에 여기에 추가
        // 예:
        // 이번 사이클 임시 소문 초기화
        // 이번 판매 결과 초기화
        // 이번 제작 임시 데이터 초기화
    }

    private void SetOnlyPanel(GameObject targetPanel)
    {
        if (explorationPanel != null)
            explorationPanel.SetActive(false);

        if (craftPanel != null)
            craftPanel.SetActive(false);

        if (sellPanel != null)
            sellPanel.SetActive(false);

        if (settlementPanel != null)
            settlementPanel.SetActive(false);

        if (targetPanel != null)
            targetPanel.SetActive(true);
    }
}