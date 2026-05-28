using UnityEngine;
using UnityEngine.SceneManagement;

public class ExplorationSceneController : MonoBehaviour
{
    [Header("자동 맵 생성기")]
    public AutoTileMapGenerator mapGenerator;

    [Header("플레이어")]
    public Transform player;
    public Rigidbody2D playerRb;

    [Header("씬 이름")]
    public string trendAnalysisSceneName = "TrendAnalysisScene";
    public string craftSceneName = "CraftScene";

    [Header("시작 시 랜덤 맵 생성")]
    public bool generateRandomMapOnStart = true;

    private void Start()
    {
        if (generateRandomMapOnStart)
        {
            GenerateNewMapAndMovePlayer();
        }
    }

    public void GenerateNewMapAndMovePlayer()
    {
        if (mapGenerator == null)
        {
            Debug.LogWarning("[ExplorationSceneController] mapGenerator가 연결되지 않았습니다.");
            return;
        }

        mapGenerator.GenerateMap();

        MovePlayerToStartPoint();
    }

    private void MovePlayerToStartPoint()
    {
        if (player == null)
        {
            Debug.LogWarning("[ExplorationSceneController] player가 연결되지 않았습니다.");
            return;
        }

        if (mapGenerator.generatedPlayerStartPoint == null)
        {
            Debug.LogWarning("[ExplorationSceneController] generatedPlayerStartPoint가 없습니다. AutoTileMapGenerator에서 PlayerStartPoint 생성이 되는지 확인하세요.");
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

        Debug.Log("[ExplorationSceneController] 플레이어 시작 위치 이동 완료: " + startPos);
    }

    public void GoToTrendAnalysisScene()
    {
        SceneManager.LoadScene(trendAnalysisSceneName);
    }

    public void GoToCraftScene()
    {
        SceneManager.LoadScene(craftSceneName);
    }
}