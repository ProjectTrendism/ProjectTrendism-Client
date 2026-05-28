using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum AutoMapTheme
{
    VillageCenter,
    RiverSide,
    ForestVillage,
    FarmVillage,
    LakeVillage,
    CaveTrail
}

public class AutoTileMapGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap pathTilemap;
    public Tilemap waterTilemap;
    public Tilemap decoTilemap;

    [Header("필수 타일")]
    public TileBase grassTile;
    public TileBase pathTile;
    public TileBase waterTile;
    public TileBase treeTile;

    [Header("선택 타일 - 있으면 퀄리티 상승")]
    public TileBase[] grassVariants;
    public TileBase[] pathVariants;
    public TileBase flowerTile;
    public TileBase bushTile;
    public TileBase rockTile;
    public TileBase farmTile;

    [Header("자동 배치 프리팹 - 건물")]
    public GameObject[] housePrefabs;
    public GameObject[] shopPrefabs;
    public GameObject[] tentPrefabs;
    public GameObject[] cavePrefabs;

    [Header("자동 배치 프리팹 - 테마 소품")]
    public GameObject[] farmPropPrefabs;
    public GameObject[] lakePropPrefabs;
    public GameObject[] forestPropPrefabs;

    [Header("자동 배치 프리팹 - 공용 소품")]
    public GameObject[] villagePropPrefabs;
    public GameObject[] roadsidePropPrefabs;

    [Header("자동 배치 프리팹 - NPC")]
    public GameObject[] villageNpcPrefabs;
    public GameObject[] merchantNpcPrefabs;
    public GameObject[] farmerNpcPrefabs;
    public GameObject[] fisherNpcPrefabs;
    public GameObject[] suspiciousNpcPrefabs;
    public GameObject[] travelerNpcPrefabs;

    [Header("전체 NPC 프리팹 한 명씩 배치")]
    public bool spawnEveryNpcPrefabOnce = true;

    [Tooltip("여기에 넣은 NPC 프리팹은 맵 생성 시 전부 한 명씩 배치됩니다.")]
    public GameObject[] allNpcPrefabs;

    [Tooltip("전체 NPC 배치 시 NPC끼리 떨어질 거리")]
    public int allNpcPersonalSpaceRadius = 1;

    [Header("NPC 배치 설정")]
    public int minVillageNpcCount = 3;
    public int maxVillageNpcCount = 5;

    public int minThemeNpcCount = 1;
    public int maxThemeNpcCount = 3;

    public int npcSortingOrder = 30;

    [Tooltip("켜면 같은 서버 NPC id를 가진 프리팹이 한 번의 맵 생성에서 중복 생성되지 않습니다.")]
    public bool preventDuplicateNpcServerIds = true;

    [Tooltip("켜면 NPC를 아무 칸이 아니라 길 주변 빈 칸에 우선 배치합니다.")]
    public bool placeNpcNearRoad = true;

    [Tooltip("NPC가 서로 너무 붙지 않도록 주변 칸을 같이 점유 처리합니다.")]
    public int npcPersonalSpaceRadius = 1;

    [Header("자동 생성 부모")]
    public Transform runtimeObjectParent;

    [Header("플레이어 시작 위치")]
    public GameObject playerStartPointPrefab;
    public Transform generatedPlayerStartPoint;

    [Header("오브젝트 배치 설정")]
    public int minHouseCount = 4;
    public int maxHouseCount = 7;

    public int minVillagePropCount = 4;
    public int maxVillagePropCount = 8;

    public int minRoadsidePropCount = 3;
    public int maxRoadsidePropCount = 6;

    public int minThemePropCount = 2;
    public int maxThemePropCount = 5;

    [Header("맵 크기")]
    public int mapWidth = 44;
    public int mapHeight = 26;

    [Header("랜덤 설정")]
    public int seed = 0;
    public bool useRandomSeed = true;

    [Header("테마 설정")]
    public bool useRandomTheme = true;
    public AutoMapTheme selectedTheme = AutoMapTheme.VillageCenter;

    [Header("길 설정")]
    [Range(1, 3)]
    public int mainRoadThickness = 2;

    [Range(0f, 1f)]
    public float roadWiggleChance = 0.35f;

    [Range(0f, 1f)]
    public float branchRoadChance = 0.75f;

    [Header("장식 밀도")]
    [Range(0f, 1f)]
    public float treeChance = 0.08f;

    [Range(0f, 1f)]
    public float flowerChance = 0.035f;

    [Range(0f, 1f)]
    public float rockChance = 0.012f;

    private int startX;
    private int startY;
    private int endX;
    private int endY;

    private readonly HashSet<Vector3Int> roadCells = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> waterCells = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> reservedCells = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> occupiedObjectCells = new HashSet<Vector3Int>();
    private readonly HashSet<int> usedNpcServerIds = new HashSet<int>();

    [ContextMenu("자동 맵 생성")]
    public void GenerateMap()
    {
        PrepareRandom();
        ClearMap();

        startX = -mapWidth / 2;
        startY = -mapHeight / 2;
        endX = startX + mapWidth - 1;
        endY = startY + mapHeight - 1;

        roadCells.Clear();
        waterCells.Clear();
        reservedCells.Clear();
        occupiedObjectCells.Clear();
        usedNpcServerIds.Clear();

        ChooseTheme();

        GenerateGround();

        switch (selectedTheme)
        {
            case AutoMapTheme.VillageCenter:
                GenerateVillageCenter();
                break;

            case AutoMapTheme.RiverSide:
                GenerateRiverSide();
                break;

            case AutoMapTheme.ForestVillage:
                GenerateForestVillage();
                break;

            case AutoMapTheme.FarmVillage:
                GenerateFarmVillage();
                break;

            case AutoMapTheme.LakeVillage:
                GenerateLakeVillage();
                break;

            case AutoMapTheme.CaveTrail:
                GenerateCaveTrail();
                break;
        }

        GenerateNatureDecoration();
        GenerateSmallDetails();

        GenerateMapObjects();
        GenerateNPCs();
        GeneratePlayerStartPoint();

        Debug.Log("자동 맵 생성 완료 / 테마: " + selectedTheme + " / Seed: " + seed);
    }

    [ContextMenu("맵 삭제")]
    public void ClearMap()
    {
        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        if (pathTilemap != null) pathTilemap.ClearAllTiles();
        if (waterTilemap != null) waterTilemap.ClearAllTiles();
        if (decoTilemap != null) decoTilemap.ClearAllTiles();

        ClearRuntimeObjects();
    }

    private void ClearRuntimeObjects()
    {
        generatedPlayerStartPoint = null;

        if (runtimeObjectParent == null)
            return;

        for (int i = runtimeObjectParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(runtimeObjectParent.GetChild(i).gameObject);
        }
    }

    private void PrepareRandom()
    {
        if (useRandomSeed)
        {
            seed = Random.Range(0, 999999);
        }

        Random.InitState(seed);
    }

    private void ChooseTheme()
    {
        if (!useRandomTheme)
            return;

        int themeCount = System.Enum.GetValues(typeof(AutoMapTheme)).Length;
        selectedTheme = (AutoMapTheme)Random.Range(0, themeCount);
    }

    private void GenerateGround()
    {
        if (groundTilemap == null || grassTile == null)
        {
            Debug.LogWarning("Ground Tilemap 또는 Grass Tile이 비어있습니다.");
            return;
        }

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                groundTilemap.SetTile(pos, GetRandomGrassTile());
            }
        }
    }

    // =========================================================
    // 테마별 맵 생성
    // =========================================================

    private void GenerateVillageCenter()
    {
        Vector3Int center = GetCell(mapWidth / 2, mapHeight / 2);

        CreatePlaza(center, 4, 3);

        Vector3Int westGate = GetCell(2, mapHeight / 2 + Random.Range(-2, 3));
        Vector3Int eastGate = GetCell(mapWidth - 3, mapHeight / 2 + Random.Range(-2, 3));
        Vector3Int northGate = GetCell(mapWidth / 2 + Random.Range(-3, 4), mapHeight - 3);
        Vector3Int southGate = GetCell(mapWidth / 2 + Random.Range(-3, 4), 2);

        DrawOrganicRoad(westGate, center, mainRoadThickness);
        DrawOrganicRoad(center, eastGate, mainRoadThickness);
        DrawOrganicRoad(center, northGate, 1);
        DrawOrganicRoad(center, southGate, 1);

        AddBranchRoads(center, 5);

        CreateRightRiver(Random.Range(3, 5));
        CreateVillageClearArea(center, 8, 6);
    }

    private void GenerateRiverSide()
    {
        CreateRightRiver(Random.Range(4, 6));

        Vector3Int village = GetCell(mapWidth / 2 - 4, mapHeight / 2);
        Vector3Int riverPoint = GetCell(mapWidth - 8, mapHeight / 2 + Random.Range(-3, 4));
        Vector3Int westGate = GetCell(2, mapHeight / 2 + Random.Range(-4, 4));
        Vector3Int northForest = GetCell(mapWidth / 2 - 2, mapHeight - 4);
        Vector3Int farmPoint = GetCell(mapWidth / 2 + 6, 5);

        CreatePlaza(village, 3, 2);

        DrawOrganicRoad(westGate, village, 2);
        DrawOrganicRoad(village, riverPoint, 2);
        DrawOrganicRoad(village, northForest, 1);
        DrawOrganicRoad(village, farmPoint, 1);

        CreateBridgeAround(riverPoint);
        AddBranchRoads(village, 4);
        CreateVillageClearArea(village, 7, 5);
    }

    private void GenerateForestVillage()
    {
        Vector3Int village = GetCell(mapWidth / 2 + Random.Range(-4, 4), mapHeight / 2 + Random.Range(-2, 3));
        Vector3Int westForest = GetCell(3, mapHeight - 5);
        Vector3Int southGate = GetCell(mapWidth / 2, 2);
        Vector3Int hiddenTrail = GetCell(5, 5);
        Vector3Int lakePoint = GetCell(mapWidth - 8, 6);

        CreateSmallPond(lakePoint, 5, 3);
        CreatePlaza(village, 3, 2);

        DrawOrganicRoad(southGate, village, 2);
        DrawOrganicRoad(village, westForest, 1);
        DrawOrganicRoad(village, hiddenTrail, 1);
        DrawOrganicRoad(village, lakePoint, 1);

        AddBranchRoads(village, 6);
        CreateVillageClearArea(village, 7, 5);

        // 숲 테마는 왼쪽과 위쪽을 숲처럼 더 채운다
        ReserveForestCluster(GetCell(6, mapHeight - 6), 8, 5);
        ReserveForestCluster(GetCell(8, 7), 6, 4);
    }

    private void GenerateFarmVillage()
    {
        Vector3Int village = GetCell(mapWidth / 2 - 3, mapHeight / 2);
        Vector3Int farmA = GetCell(mapWidth - 10, 7);
        Vector3Int farmB = GetCell(mapWidth - 13, mapHeight - 8);
        Vector3Int westGate = GetCell(2, mapHeight / 2);
        Vector3Int southGate = GetCell(mapWidth / 2 - 1, 2);

        CreatePlaza(village, 4, 2);

        DrawOrganicRoad(westGate, village, 2);
        DrawOrganicRoad(southGate, village, 1);
        DrawOrganicRoad(village, farmA, 2);
        DrawOrganicRoad(village, farmB, 1);

        CreateFarmArea(farmA, 7, 4);
        CreateFarmArea(farmB, 6, 3);

        CreateSmallPond(GetCell(mapWidth - 6, 5), 3, 2);

        AddBranchRoads(village, 3);
        CreateVillageClearArea(village, 8, 5);
    }

    private void GenerateLakeVillage()
    {
        Vector3Int lakeCenter = GetCell(mapWidth - 9, 8);
        CreateLargeLake(lakeCenter);

        Vector3Int village = GetCell(mapWidth / 2 - 5, mapHeight / 2 + 2);
        Vector3Int lakeDock = GetCell(mapWidth - 14, 8);
        Vector3Int westGate = GetCell(2, mapHeight / 2);
        Vector3Int northGate = GetCell(mapWidth / 2 - 4, mapHeight - 3);

        CreatePlaza(village, 3, 2);

        DrawOrganicRoad(westGate, village, 2);
        DrawOrganicRoad(village, lakeDock, 2);
        DrawOrganicRoad(village, northGate, 1);

        AddCurvedLakePath(lakeCenter);
        AddBranchRoads(village, 4);
        CreateVillageClearArea(village, 7, 5);
    }

    private void GenerateCaveTrail()
    {
        Vector3Int village = GetCell(mapWidth / 2 - 3, mapHeight / 2 - 1);
        Vector3Int cave = GetCell(mapWidth - 6, mapHeight - 5);
        Vector3Int westGate = GetCell(2, mapHeight / 2);
        Vector3Int southGate = GetCell(mapWidth / 2, 2);
        Vector3Int forestPoint = GetCell(6, mapHeight - 5);

        CreatePlaza(village, 3, 2);
        CreateSmallPond(GetCell(mapWidth - 8, 5), 4, 2);

        DrawOrganicRoad(westGate, village, 2);
        DrawOrganicRoad(southGate, village, 1);
        DrawOrganicRoad(village, cave, 1);
        DrawOrganicRoad(village, forestPoint, 1);

        // 동굴 쪽 길은 중간중간 끊긴 느낌의 샛길 추가
        CreateDeadEndTrail(GetCell(mapWidth - 12, mapHeight - 8), GetCell(mapWidth - 5, mapHeight - 3), 1);

        AddBranchRoads(village, 5);
        CreateVillageClearArea(village, 7, 5);

        ReserveForestCluster(cave, 5, 4);
    }

    // =========================================================
    // 길 생성 함수
    // =========================================================

    private void DrawOrganicRoad(Vector3Int start, Vector3Int end, int thickness)
    {
        Vector3Int current = start;

        int safety = 0;

        while (current != end && safety < 300)
        {
            safety++;

            PaintRoad(current, thickness);

            bool moveX = Mathf.Abs(end.x - current.x) > Mathf.Abs(end.y - current.y);

            if (Random.value < 0.35f)
                moveX = !moveX;

            if (moveX)
            {
                if (end.x > current.x) current.x++;
                else if (end.x < current.x) current.x--;
            }
            else
            {
                if (end.y > current.y) current.y++;
                else if (end.y < current.y) current.y--;
            }

            if (Random.value < roadWiggleChance)
            {
                Vector3Int wiggle = current;

                if (Random.value < 0.5f)
                    wiggle.y += Random.value < 0.5f ? 1 : -1;
                else
                    wiggle.x += Random.value < 0.5f ? 1 : -1;

                if (IsInsideMap(wiggle))
                    PaintRoad(wiggle, thickness);
            }
        }

        PaintRoad(end, thickness);
    }

    private void CreateDeadEndTrail(Vector3Int start, Vector3Int end, int thickness)
    {
        Vector3Int current = start;

        int steps = Random.Range(8, 16);

        for (int i = 0; i < steps; i++)
        {
            PaintRoad(current, thickness);

            if (Random.value < 0.65f)
            {
                if (end.x > current.x) current.x++;
                else if (end.x < current.x) current.x--;
            }
            else
            {
                if (end.y > current.y) current.y++;
                else if (end.y < current.y) current.y--;
            }

            if (!IsInsideMap(current))
                break;
        }
    }

    private void PaintRoad(Vector3Int center, int thickness)
    {
        if (pathTilemap == null || pathTile == null)
            return;

        int radius = Mathf.Max(0, thickness - 1);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (!IsInsideMap(pos))
                    continue;

                pathTilemap.SetTile(pos, GetRandomPathTile());
                roadCells.Add(pos);

                // 길 위에 장식 제거
                if (decoTilemap != null)
                    decoTilemap.SetTile(pos, null);
            }
        }
    }

    private void CreatePlaza(Vector3Int center, int radiusX, int radiusY)
    {
        for (int x = -radiusX; x <= radiusX; x++)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (!IsInsideMap(pos))
                    continue;

                float shape = (x * x) / (float)(radiusX * radiusX) + (y * y) / (float)(radiusY * radiusY);

                if (shape <= 1.15f)
                {
                    PaintRoad(pos, 1);
                    reservedCells.Add(pos);
                }
            }
        }
    }

    private void AddBranchRoads(Vector3Int center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (Random.value > branchRoadChance)
                continue;

            Vector3Int target = center;

            int side = Random.Range(0, 4);

            if (side == 0) target = GetCell(Random.Range(3, 12), Random.Range(4, mapHeight - 4));
            if (side == 1) target = GetCell(Random.Range(mapWidth - 13, mapWidth - 4), Random.Range(4, mapHeight - 4));
            if (side == 2) target = GetCell(Random.Range(8, mapWidth - 8), Random.Range(3, 8));
            if (side == 3) target = GetCell(Random.Range(8, mapWidth - 8), Random.Range(mapHeight - 8, mapHeight - 3));

            DrawOrganicRoad(center, target, 1);
        }
    }

    private void AddCurvedLakePath(Vector3Int lakeCenter)
    {
        Vector3Int start = new Vector3Int(lakeCenter.x - 8, lakeCenter.y + 4, 0);
        Vector3Int end = new Vector3Int(lakeCenter.x - 2, lakeCenter.y - 4, 0);

        DrawOrganicRoad(start, end, 1);
    }

    // =========================================================
    // 물 생성
    // =========================================================

    private void CreateRightRiver(int width)
    {
        if (waterTilemap == null || waterTile == null)
            return;

        int baseX = startX + mapWidth - Random.Range(8, 12);

        for (int y = startY; y <= endY; y++)
        {
            int curve = Mathf.RoundToInt(Mathf.Sin(y * 0.28f + seed * 0.001f) * 2f);

            for (int w = 0; w < width; w++)
            {
                Vector3Int pos = new Vector3Int(baseX + curve + w, y, 0);
                PaintWater(pos);
            }
        }

        Vector3Int pond = new Vector3Int(baseX + 1, startY + Random.Range(4, 8), 0);
        CreateSmallPond(pond, Random.Range(3, 5), Random.Range(2, 4));
    }

    private void CreateSmallPond(Vector3Int center, int radiusX, int radiusY)
    {
        if (waterTilemap == null || waterTile == null)
            return;

        for (int x = -radiusX; x <= radiusX; x++)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                float value = (x * x) / (float)(radiusX * radiusX) + (y * y) / (float)(radiusY * radiusY);

                if (value <= 1.05f + Random.Range(-0.08f, 0.12f))
                {
                    Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);
                    PaintWater(pos);
                }
            }
        }
    }

    private void CreateLargeLake(Vector3Int center)
    {
        CreateSmallPond(center, 7, 4);

        for (int i = 0; i < 10; i++)
        {
            Vector3Int extra = new Vector3Int(
                center.x + Random.Range(-6, 7),
                center.y + Random.Range(-4, 5),
                0
            );

            CreateSmallPond(extra, Random.Range(2, 4), Random.Range(1, 3));
        }
    }

    private void PaintWater(Vector3Int pos)
    {
        if (!IsInsideMap(pos))
            return;

        waterTilemap.SetTile(pos, waterTile);
        waterCells.Add(pos);

        // 물 위 장식 제거
        if (decoTilemap != null)
            decoTilemap.SetTile(pos, null);
    }

    private void CreateBridgeAround(Vector3Int target)
    {
        // 물 위에 길을 덮어 다리처럼 보이게 함
        for (int x = target.x - 4; x <= target.x + 4; x++)
        {
            PaintRoad(new Vector3Int(x, target.y, 0), 1);
            PaintRoad(new Vector3Int(x, target.y + 1, 0), 1);
        }
    }

    // =========================================================
    // 농장 / 숲 / 마을 공간
    // =========================================================

    private void CreateFarmArea(Vector3Int center, int width, int height)
    {
        if (decoTilemap == null)
            return;

        TileBase tile = farmTile != null ? farmTile : pathTile;

        int halfW = width / 2;
        int halfH = height / 2;

        for (int x = -halfW; x <= halfW; x++)
        {
            for (int y = -halfH; y <= halfH; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (!IsInsideMap(pos)) continue;
                if (roadCells.Contains(pos)) continue;
                if (waterCells.Contains(pos)) continue;

                if ((x + halfW) % 2 == 0)
                {
                    decoTilemap.SetTile(pos, tile);
                    reservedCells.Add(pos);
                }
            }
        }
    }

    private void CreateVillageClearArea(Vector3Int center, int radiusX, int radiusY)
    {
        for (int x = -radiusX; x <= radiusX; x++)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (!IsInsideMap(pos))
                    continue;

                reservedCells.Add(pos);
            }
        }
    }

    private void ReserveForestCluster(Vector3Int center, int radiusX, int radiusY)
    {
        for (int x = -radiusX; x <= radiusX; x++)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (!IsInsideMap(pos)) continue;
                if (roadCells.Contains(pos)) continue;
                if (waterCells.Contains(pos)) continue;

                if (Random.value < 0.22f)
                {
                    if (decoTilemap != null && treeTile != null)
                        decoTilemap.SetTile(pos, treeTile);
                }
            }
        }
    }

    // =========================================================
    // 자연 장식
    // =========================================================

    private void GenerateNatureDecoration()
    {
        if (decoTilemap == null || treeTile == null)
            return;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (roadCells.Contains(pos)) continue;
                if (waterCells.Contains(pos)) continue;
                if (reservedCells.Contains(pos)) continue;

                float finalTreeChance = treeChance;

                bool edgeArea =
                    x < startX + 7 ||
                    x > endX - 7 ||
                    y < startY + 5 ||
                    y > endY - 5;

                bool leftForestArea =
                    x < startX + 12 &&
                    y > startY + 5;

                bool topForestArea =
                    y > endY - 8;

                if (edgeArea)
                    finalTreeChance *= 2.1f;

                if (selectedTheme == AutoMapTheme.ForestVillage && leftForestArea)
                    finalTreeChance *= 2.7f;

                if (selectedTheme == AutoMapTheme.CaveTrail && topForestArea)
                    finalTreeChance *= 2.3f;

                if (selectedTheme == AutoMapTheme.FarmVillage)
                    finalTreeChance *= 0.65f;

                if (Random.value < finalTreeChance)
                {
                    decoTilemap.SetTile(pos, treeTile);
                }
            }
        }
    }

    private void GenerateSmallDetails()
    {
        if (decoTilemap == null)
            return;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (roadCells.Contains(pos)) continue;
                if (waterCells.Contains(pos)) continue;
                if (reservedCells.Contains(pos)) continue;
                if (decoTilemap.HasTile(pos)) continue;

                float random = Random.value;

                if (flowerTile != null && random < flowerChance)
                {
                    decoTilemap.SetTile(pos, flowerTile);
                }
                else if (bushTile != null && random < flowerChance + 0.018f)
                {
                    decoTilemap.SetTile(pos, bushTile);
                }
                else if (rockTile != null && random < flowerChance + 0.018f + rockChance)
                {
                    decoTilemap.SetTile(pos, rockTile);
                }
            }
        }
    }

    // =========================================================
    // 유틸
    // =========================================================

    private Vector3Int GetCell(int localX, int localY)
    {
        return new Vector3Int(startX + localX, startY + localY, 0);
    }

    private bool IsInsideMap(Vector3Int pos)
    {
        return pos.x >= startX && pos.x <= endX && pos.y >= startY && pos.y <= endY;
    }

    private TileBase GetRandomGrassTile()
    {
        if (grassVariants != null && grassVariants.Length > 0)
        {
            if (Random.value < 0.18f)
                return grassVariants[Random.Range(0, grassVariants.Length)];
        }

        return grassTile;
    }

    private TileBase GetRandomPathTile()
    {
        if (pathVariants != null && pathVariants.Length > 0)
        {
            if (Random.value < 0.18f)
                return pathVariants[Random.Range(0, pathVariants.Length)];
        }

        return pathTile;
    }

    private GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        int index = Random.Range(0, prefabs.Length);
        return prefabs[index];
    }

    private void SpawnThemeProps(GameObject[] prefabArray, Vector3Int[] positions, string baseName)
    {
        if (prefabArray == null || prefabArray.Length == 0)
            return;

        int maxCount = Mathf.Min(positions.Length, Random.Range(minThemePropCount, maxThemePropCount + 1));

        List<int> indices = new List<int>();
        for (int i = 0; i < positions.Length; i++)
        {
            indices.Add(i);
        }

        for (int i = 0; i < indices.Count; i++)
        {
            int randomIndex = Random.Range(i, indices.Count);
            int temp = indices[i];
            indices[i] = indices[randomIndex];
            indices[randomIndex] = temp;
        }

        for (int i = 0; i < maxCount; i++)
        {
            Vector3Int cellPos = positions[indices[i]];

            if (!IsInsideMap(cellPos)) continue;
            if (waterCells.Contains(cellPos)) continue;

            GameObject prefab = GetRandomPrefab(prefabArray);
            if (prefab == null) continue;

            Vector3 worldPos = GetWorldPositionFromCell(cellPos);

            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, runtimeObjectParent);
            obj.name = baseName + "_" + i;

            SetSortingOrderRecursive(obj, 10);
        }
    }

    private void GenerateMapObjects()
    {
        if (runtimeObjectParent == null)
        {
            Debug.LogWarning("Runtime Object Parent가 연결되지 않았습니다.");
            return;
        }

        Vector3Int villageCenter = GetVillageCenterByTheme();

        CreateVillageBuildings(villageCenter);
        CreateVillageProps(villageCenter);
        CreateRoadsideProps(villageCenter);

        switch (selectedTheme)
        {
            case AutoMapTheme.VillageCenter:
                SpawnThemeProps(tentPrefabs, new Vector3Int[]
                {
                    GetCell(6, 6),
                    GetCell(8, 5),
                    GetCell(5, 8)
                }, "Tent");
                break;

            case AutoMapTheme.RiverSide:
                SpawnThemeProps(lakePropPrefabs, new Vector3Int[]
                {
                    GetCell(mapWidth - 10, mapHeight / 2),
                    GetCell(mapWidth - 12, mapHeight / 2 - 2),
                    GetCell(mapWidth - 9, mapHeight / 2 + 2)
                }, "RiverProp");
                SpawnThemeProps(shopPrefabs, new Vector3Int[]
                {
                    villageCenter + new Vector3Int(3, -3, 0),
                    villageCenter + new Vector3Int(5, -2, 0)
                }, "RiverShop");
                break;

            case AutoMapTheme.ForestVillage:
                SpawnThemeProps(forestPropPrefabs, new Vector3Int[]
                {
                    GetCell(6, mapHeight - 7),
                    GetCell(8, mapHeight - 6),
                    GetCell(5, mapHeight - 9),
                    GetCell(10, mapHeight - 8)
                }, "ForestProp");
                SpawnThemeProps(cavePrefabs, new Vector3Int[]
                {
                    GetCell(5, 5),
                    GetCell(7, 6)
                }, "ForestCave");
                break;

            case AutoMapTheme.FarmVillage:
                SpawnThemeProps(farmPropPrefabs, new Vector3Int[]
                {
                    GetCell(mapWidth - 10, 7),
                    GetCell(mapWidth - 12, 7),
                    GetCell(mapWidth - 14, 7),
                    GetCell(mapWidth - 13, mapHeight - 8),
                    GetCell(mapWidth - 15, mapHeight - 8)
                }, "FarmProp");
                break;

            case AutoMapTheme.LakeVillage:
                SpawnThemeProps(lakePropPrefabs, new Vector3Int[]
                {
                    GetCell(mapWidth - 13, 8),
                    GetCell(mapWidth - 15, 7),
                    GetCell(mapWidth - 11, 10)
                }, "LakeProp");
                break;

            case AutoMapTheme.CaveTrail:
                SpawnThemeProps(cavePrefabs, new Vector3Int[]
                {
                    GetCell(mapWidth - 6, mapHeight - 5),
                    GetCell(mapWidth - 8, mapHeight - 6)
                }, "CaveTrail");
                SpawnThemeProps(tentPrefabs, new Vector3Int[]
                {
                    GetCell(7, mapHeight - 6),
                    GetCell(9, mapHeight - 7)
                }, "CaveTent");
                break;
        }
    }

    private void GenerateNPCs()
    {
        if (runtimeObjectParent == null)
        {
            Debug.LogWarning("Runtime Object Parent가 연결되지 않았습니다. NPC 생성 실패");
            return;
        }

        if (spawnEveryNpcPrefabOnce)
        {
            SpawnEveryNpcPrefabOnce();
            return;
        }

        Vector3Int villageCenter = GetVillageCenterByTheme();

        CreateVillageNPCs(villageCenter);
        CreateThemeNPCs(villageCenter);
    }

    private void CreateVillageNPCs(Vector3Int center)
    {
        TrySpawnNPCGroup(
            villageNpcPrefabs,
            center,
            12,
            7,
            minVillageNpcCount,
            maxVillageNpcCount,
            "VillageNPC"
        );

        TrySpawnNPCGroup(
            merchantNpcPrefabs,
            center,
            8,
            5,
            1,
            1,
            "MerchantNPC"
        );
    }

    private void CreateThemeNPCs(Vector3Int villageCenter)
    {
        int themeNpcCount = Random.Range(minThemeNpcCount, maxThemeNpcCount + 1);

        switch (selectedTheme)
        {
            case AutoMapTheme.VillageCenter:
                TrySpawnNPCGroup(
                    travelerNpcPrefabs,
                    villageCenter,
                    14,
                    8,
                    1,
                    themeNpcCount,
                    "TravelerNPC"
                );
                break;

            case AutoMapTheme.RiverSide:
                TrySpawnNPCGroup(
                    fisherNpcPrefabs,
                    GetCell(mapWidth - 12, mapHeight / 2),
                    7,
                    5,
                    1,
                    themeNpcCount,
                    "FisherNPC"
                );
                break;

            case AutoMapTheme.ForestVillage:
                TrySpawnNPCGroup(
                    travelerNpcPrefabs,
                    GetCell(7, mapHeight - 7),
                    8,
                    6,
                    1,
                    themeNpcCount,
                    "ForestTravelerNPC"
                );

                TrySpawnNPCGroup(
                    suspiciousNpcPrefabs,
                    GetCell(5, 5),
                    5,
                    4,
                    1,
                    1,
                    "HiddenSuspiciousNPC"
                );
                break;

            case AutoMapTheme.FarmVillage:
                TrySpawnNPCGroup(
                    farmerNpcPrefabs,
                    GetCell(mapWidth - 11, 7),
                    8,
                    5,
                    1,
                    themeNpcCount,
                    "FarmerNPC"
                );
                break;

            case AutoMapTheme.LakeVillage:
                TrySpawnNPCGroup(
                    fisherNpcPrefabs,
                    GetCell(mapWidth - 13, 8),
                    7,
                    5,
                    1,
                    themeNpcCount,
                    "LakeFisherNPC"
                );
                break;

            case AutoMapTheme.CaveTrail:
                TrySpawnNPCGroup(
                    suspiciousNpcPrefabs,
                    GetCell(mapWidth - 7, mapHeight - 6),
                    6,
                    5,
                    1,
                    themeNpcCount,
                    "SuspiciousNPC"
                );

                TrySpawnNPCGroup(
                    travelerNpcPrefabs,
                    GetCell(7, mapHeight - 6),
                    6,
                    4,
                    1,
                    1,
                    "CaveTravelerNPC"
                );
                break;
        }
    }

    private void TrySpawnNPCGroup(
        GameObject[] npcPrefabs,
        Vector3Int center,
        int radiusX,
        int radiusY,
        int minCount,
        int maxCount,
        string baseName
    )
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
        {
            Debug.LogWarning(baseName + " NPC Prefabs가 비어 있습니다.");
            return;
        }

        int count = Random.Range(minCount, maxCount + 1);
        int spawned = 0;
        int maxTry = count * 80;

        for (int i = 0; i < maxTry; i++)
        {
            if (spawned >= count)
                break;

            GameObject prefab = GetRandomNpcPrefabWithoutDuplicateId(npcPrefabs);

            if (prefab == null)
            {
                Debug.LogWarning(baseName + "에서 사용 가능한 NPC 프리팹이 없습니다. 중복 serverId 또는 배열 비어있음");
                break;
            }

            Vector3Int cellPos;
            bool found;

            if (placeNpcNearRoad)
            {
                found = TryGetNpcCellNearRoadAround(center, radiusX, radiusY, out cellPos);

                if (!found)
                {
                    found = TryGetRandomCellAround(center, radiusX, radiusY, false, out cellPos);
                }
            }
            else
            {
                found = TryGetRandomCellAround(center, radiusX, radiusY, false, out cellPos);
            }

            if (!found)
                continue;

            if (IsTooCloseToOtherObjects(cellPos, npcPersonalSpaceRadius))
                continue;

            GameObject npc = SpawnNPCPrefab(prefab, cellPos, baseName + "_" + spawned);

            if (npc != null)
                spawned++;
        }

        Debug.Log(baseName + " 생성 수: " + spawned + " / 목표: " + count);
    }

    private GameObject SpawnNPCPrefab(GameObject prefab, Vector3Int cellPos, string objectName)
    {
        if (prefab == null)
            return null;

        if (!CanPlaceObjectAt(cellPos, false))
            return null;

        int prefabServerId = GetNpcServerIdFromPrefab(prefab);

        if (preventDuplicateNpcServerIds && prefabServerId > 0 && usedNpcServerIds.Contains(prefabServerId))
            return null;

        Vector3 worldPos = GetWorldPositionFromCell(cellPos);
        worldPos.z = 0f;

        GameObject npc = Instantiate(prefab, worldPos, Quaternion.identity, runtimeObjectParent);
        npc.name = objectName;

        SetSortingOrderRecursive(npc, npcSortingOrder);

        NPCInteraction interaction = npc.GetComponentInChildren<NPCInteraction>(true);
        if (interaction != null && interaction.GetNpcServerId() > 0)
        {
            usedNpcServerIds.Add(interaction.GetNpcServerId());
        }

        occupiedObjectCells.Add(cellPos);
        MarkOccupiedAround(cellPos, npcPersonalSpaceRadius, npcPersonalSpaceRadius);

        return npc;
    }

    private GameObject GetRandomNpcPrefabWithoutDuplicateId(GameObject[] npcPrefabs)
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
            return null;

        List<GameObject> candidates = new List<GameObject>();

        for (int i = 0; i < npcPrefabs.Length; i++)
        {
            GameObject prefab = npcPrefabs[i];

            if (prefab == null)
                continue;

            int serverId = GetNpcServerIdFromPrefab(prefab);

            if (preventDuplicateNpcServerIds && serverId > 0 && usedNpcServerIds.Contains(serverId))
                continue;

            candidates.Add(prefab);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private int GetNpcServerIdFromPrefab(GameObject prefab)
    {
        if (prefab == null)
            return 0;

        NPCInteraction interaction = prefab.GetComponentInChildren<NPCInteraction>(true);

        if (interaction == null)
            return 0;

        return interaction.GetNpcServerId();
    }

    private bool TryGetNpcCellNearRoadAround(Vector3Int center, int radiusX, int radiusY, out Vector3Int result)
    {
        int maxTry = 160;

        Vector3Int[] directions =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, -1, 0)
        };

        for (int i = 0; i < maxTry; i++)
        {
            Vector3Int roadCell = GetRandomRoadCellAround(center, radiusX, radiusY);

            if (roadCell == Vector3Int.zero)
                continue;

            ShuffleArray(directions);

            for (int d = 0; d < directions.Length; d++)
            {
                Vector3Int candidate = roadCell + directions[d];

                if (!CanPlaceObjectAt(candidate, false))
                    continue;

                if (IsTooCloseToOtherObjects(candidate, npcPersonalSpaceRadius))
                    continue;

                result = candidate;
                return true;
            }
        }

        result = Vector3Int.zero;
        return false;
    }

    private Vector3Int GetRandomRoadCellAround(Vector3Int center, int radiusX, int radiusY)
    {
        if (roadCells == null || roadCells.Count == 0)
            return Vector3Int.zero;

        List<Vector3Int> candidates = new List<Vector3Int>();

        foreach (Vector3Int roadCell in roadCells)
        {
            if (Mathf.Abs(roadCell.x - center.x) <= radiusX &&
                Mathf.Abs(roadCell.y - center.y) <= radiusY)
            {
                candidates.Add(roadCell);
            }
        }

        if (candidates.Count == 0)
            return Vector3Int.zero;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private Vector3Int GetVillageCenterByTheme()
    {
        switch (selectedTheme)
        {
            case AutoMapTheme.VillageCenter:
                return GetCell(mapWidth / 2, mapHeight / 2);

            case AutoMapTheme.RiverSide:
                return GetCell(mapWidth / 2 - 4, mapHeight / 2);

            case AutoMapTheme.ForestVillage:
                return GetCell(mapWidth / 2, mapHeight / 2);

            case AutoMapTheme.FarmVillage:
                return GetCell(mapWidth / 2 - 3, mapHeight / 2);

            case AutoMapTheme.LakeVillage:
                return GetCell(mapWidth / 2 - 5, mapHeight / 2 + 2);

            case AutoMapTheme.CaveTrail:
                return GetCell(mapWidth / 2 - 3, mapHeight / 2 - 1);

            default:
                return GetCell(mapWidth / 2, mapHeight / 2);
        }
    }


   private void CreateVillageBuildings(Vector3Int center)
    {
        if (housePrefabs == null || housePrefabs.Length == 0)
        {
            Debug.LogWarning("House Prefabs가 비어 있습니다.");
            return;
        }

        int houseCount = Random.Range(minHouseCount, maxHouseCount + 1);

        int spawned = 0;
        int maxTry = houseCount * 30;

        for (int i = 0; i < maxTry; i++)
        {
            if (spawned >= houseCount)
                break;

            Vector3Int cellPos;

            bool found = TryGetRandomCellAround(
                center,
                15,
                9,
                false,
                out cellPos
            );

            if (!found)
                continue;

            if (IsTooCloseToOtherObjects(cellPos, 2))
                continue;

            GameObject prefab = GetRandomPrefab(housePrefabs);
            if (prefab == null)
                continue;

            Vector3 worldPos = GetWorldPositionFromCell(cellPos);

            GameObject building = Instantiate(prefab, worldPos, Quaternion.identity, runtimeObjectParent);
            building.name = "House_" + spawned;

            SetSortingOrderRecursive(building, 10);

            occupiedObjectCells.Add(cellPos);

            // 집이 너무 겹쳐 보이지 않도록 주변도 점유 처리
            MarkOccupiedAround(cellPos, 2, 1);

            spawned++;
        }

        int shopCount = Random.Range(1, 3);

        for (int i = 0; i < shopCount * 20; i++)
        {
            if (shopCount <= 0)
                break;

            Vector3Int cellPos;

            bool found = TryGetRandomCellAround(
                center,
                10,
                6,
                false,
                out cellPos
            );

            if (!found)
                continue;

            if (IsTooCloseToOtherObjects(cellPos, 2))
                continue;

            GameObject prefab = GetRandomPrefab(shopPrefabs);
            if (prefab == null)
                break;

            Vector3 worldPos = GetWorldPositionFromCell(cellPos);

            GameObject shop = Instantiate(prefab, worldPos, Quaternion.identity, runtimeObjectParent);
            shop.name = "Shop_" + i;

            SetSortingOrderRecursive(shop, 11);

            occupiedObjectCells.Add(cellPos);
            MarkOccupiedAround(cellPos, 2, 1);

            shopCount--;
        }

        Debug.Log("집 생성 수: " + spawned + " / 목표: " + houseCount);
    }

    private bool IsTooCloseToOtherObjects(Vector3Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int checkPos = new Vector3Int(center.x + x, center.y + y, 0);

                if (occupiedObjectCells.Contains(checkPos))
                    return true;
            }
        }

        return false;
    }

    private void MarkOccupiedAround(Vector3Int center, int radiusX, int radiusY)
    {
        for (int x = -radiusX; x <= radiusX; x++)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (IsInsideMap(pos))
                    occupiedObjectCells.Add(pos);
            }
        }
    }

    private void CreateVillageProps(Vector3Int center)
    {
        if (villagePropPrefabs == null || villagePropPrefabs.Length == 0)
            return;

        int propCount = Random.Range(minVillagePropCount, maxVillagePropCount + 1);

        int spawned = 0;
        int maxTry = propCount * 20;

        for (int i = 0; i < maxTry; i++)
        {
            if (spawned >= propCount)
                break;

            Vector3Int cellPos;

            bool found = TryGetRandomCellAround(
                center,
                13,
                8,
                false,
                out cellPos
            );

            if (!found)
                continue;

            GameObject prefab = GetRandomPrefab(villagePropPrefabs);
            if (prefab == null)
                continue;

            Vector3 worldPos = GetWorldPositionFromCell(cellPos);

            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, runtimeObjectParent);
            obj.name = "VillageProp_" + spawned;

            SetSortingOrderRecursive(obj, 12);

            occupiedObjectCells.Add(cellPos);

            spawned++;
        }

        Debug.Log("마을 소품 생성 수: " + spawned + " / 목표: " + propCount);
    }

    private void CreateRoadsideProps(Vector3Int center)
    {
        if (roadsidePropPrefabs == null || roadsidePropPrefabs.Length == 0)
            return;

        int propCount = Random.Range(minRoadsidePropCount, maxRoadsidePropCount + 1);

        int spawned = 0;
        int maxTry = propCount * 30;

        for (int i = 0; i < maxTry; i++)
        {
            if (spawned >= propCount)
                break;

            Vector3Int roadCell = GetRandomRoadCell();

            if (roadCell == Vector3Int.zero)
                continue;

            Vector3Int[] directions =
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, -1, 0)
            };

            Vector3Int dir = directions[Random.Range(0, directions.Length)];
            Vector3Int cellPos = roadCell + dir;

            if (!CanPlaceObjectAt(cellPos, false))
                continue;

            GameObject prefab = GetRandomPrefab(roadsidePropPrefabs);
            if (prefab == null)
                continue;

            Vector3 worldPos = GetWorldPositionFromCell(cellPos);

            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, runtimeObjectParent);
            obj.name = "RoadProp_" + spawned;

            SetSortingOrderRecursive(obj, 12);

            occupiedObjectCells.Add(cellPos);

            spawned++;
        }

        Debug.Log("길가 소품 생성 수: " + spawned + " / 목표: " + propCount);
    }

    private Vector3Int GetRandomRoadCell()
    {
        if (roadCells == null || roadCells.Count == 0)
            return Vector3Int.zero;

        int targetIndex = Random.Range(0, roadCells.Count);
        int index = 0;

        foreach (Vector3Int cell in roadCells)
        {
            if (index == targetIndex)
                return cell;

            index++;
        }

        return Vector3Int.zero;
    }

    private GameObject TrySpawnPrefab(GameObject prefab, Vector3Int cellPos, string objectName)
    {
        if (prefab == null)
            return null;

        if (!CanPlaceObjectAt(cellPos, false))
            return null;

        Vector3 worldPos = GetWorldPositionFromCell(cellPos);
        worldPos.z = 0f;

        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, runtimeObjectParent);
        obj.name = objectName;

        SetSortingOrderRecursive(obj, 10);

        occupiedObjectCells.Add(cellPos);

        return obj;
    }

    private GameObject TrySpawnRandomPrefab(GameObject[] prefabArray, Vector3Int cellPos, string objectName)
    {
        GameObject prefab = GetRandomPrefab(prefabArray);

        if (prefab == null)
            return null;

        return TrySpawnPrefab(prefab, cellPos, objectName);
    }

    private Vector3 GetWorldPositionFromCell(Vector3Int cellPos)
    {
        if (groundTilemap != null)
        {
            return groundTilemap.GetCellCenterWorld(cellPos);
        }

        return new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0f);
    }

    private void ShuffleArray<T>(T[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex = Random.Range(i, array.Length);

            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }

    private void GeneratePlayerStartPoint()
    {
        Vector3Int villageCenter = GetVillageCenterByTheme();

        Vector3Int startCell = villageCenter + new Vector3Int(0, -1, 0);

        Vector3 worldPos = GetWorldPositionFromCell(startCell);

        GameObject startObj;

        if (playerStartPointPrefab != null)
        {
            startObj = Instantiate(playerStartPointPrefab, worldPos, Quaternion.identity, runtimeObjectParent);
        }
        else
        {
            startObj = new GameObject("PlayerStartPoint");
            startObj.transform.position = worldPos;
            startObj.transform.SetParent(runtimeObjectParent);
        }

        startObj.name = "PlayerStartPoint";
        generatedPlayerStartPoint = startObj.transform;
    }

    private void SetSortingOrderRecursive(GameObject obj, int order)
    {
        if (obj == null)
            return;

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = order;
        }
    }

    private bool CanPlaceObjectAt(Vector3Int cellPos, bool allowRoad = false)
    {
        if (!IsInsideMap(cellPos))
            return false;

        if (waterCells.Contains(cellPos))
            return false;

        if (!allowRoad && roadCells.Contains(cellPos))
            return false;

        if (occupiedObjectCells.Contains(cellPos))
            return false;

        return true;
    }

    private bool TryGetRandomCellAround(
        Vector3Int center,
        int radiusX,
        int radiusY,
        bool allowRoad,
        out Vector3Int result
    )
    {
        int maxTry = 80;

        for (int i = 0; i < maxTry; i++)
        {
            int x = Random.Range(center.x - radiusX, center.x + radiusX + 1);
            int y = Random.Range(center.y - radiusY, center.y + radiusY + 1);

            Vector3Int cellPos = new Vector3Int(x, y, 0);

            if (CanPlaceObjectAt(cellPos, allowRoad))
            {
                result = cellPos;
                return true;
            }
        }

        result = Vector3Int.zero;
        return false;
    }

    private void SpawnEveryNpcPrefabOnce()
    {
        if (allNpcPrefabs == null || allNpcPrefabs.Length == 0)
        {
            Debug.LogWarning("All Npc Prefabs 배열이 비어 있습니다. 전체 NPC 배치를 건너뜁니다.");
            return;
        }

        List<GameObject> npcList = new List<GameObject>();

        for (int i = 0; i < allNpcPrefabs.Length; i++)
        {
            if (allNpcPrefabs[i] != null)
            {
                npcList.Add(allNpcPrefabs[i]);
            }
        }

        ShuffleList(npcList);

        int spawned = 0;

        for (int i = 0; i < npcList.Count; i++)
        {
            GameObject prefab = npcList[i];

            Vector3Int cellPos;
            bool found = TryFindNpcSpawnCell(out cellPos);

            if (!found)
            {
                Debug.LogWarning("NPC 배치 가능한 위치를 찾지 못했습니다: " + prefab.name);
                continue;
            }

            GameObject npc = SpawnNpcPrefabDirect(prefab, cellPos, prefab.name + "_Spawned");

            if (npc != null)
            {
                spawned++;
            }
        }

        Debug.Log("전체 NPC 프리팹 배치 완료: " + spawned + " / " + npcList.Count);
    }
    private bool TryFindNpcSpawnCell(out Vector3Int result)
    {
        // 1순위: 길 주변 빈 칸
        if (TryFindCellNearRoad(out result))
        {
            return true;
        }

        // 2순위: 마을 중심 주변
        Vector3Int villageCenter = GetVillageCenterByTheme();

        for (int i = 0; i < 100; i++)
        {
            Vector3Int cellPos;

            bool found = TryGetRandomCellAround(
                villageCenter,
                15,
                9,
                false,
                out cellPos
            );

            if (!found)
                continue;

            if (!CanPlaceObjectAt(cellPos, false))
                continue;

            if (IsTooCloseToOtherObjects(cellPos, allNpcPersonalSpaceRadius))
                continue;

            result = cellPos;
            return true;
        }

        // 3순위: 맵 전체 랜덤
        for (int i = 0; i < 200; i++)
        {
            int x = Random.Range(startX + 2, endX - 2);
            int y = Random.Range(startY + 2, endY - 2);

            Vector3Int cellPos = new Vector3Int(x, y, 0);

            if (!CanPlaceObjectAt(cellPos, false))
                continue;

            if (IsTooCloseToOtherObjects(cellPos, allNpcPersonalSpaceRadius))
                continue;

            result = cellPos;
            return true;
        }

        result = Vector3Int.zero;
        return false;
    }

    private bool TryFindCellNearRoad(out Vector3Int result)
    {
        result = Vector3Int.zero;

        if (roadCells == null || roadCells.Count == 0)
            return false;

        Vector3Int[] directions =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, -1, 0)
        };

        int maxTry = 300;

        for (int i = 0; i < maxTry; i++)
        {
            Vector3Int roadCell = GetRandomRoadCell();

            if (roadCell == Vector3Int.zero)
                continue;

            Vector3Int dir = directions[Random.Range(0, directions.Length)];
            Vector3Int cellPos = roadCell + dir;

            if (!CanPlaceObjectAt(cellPos, false))
                continue;

            if (IsTooCloseToOtherObjects(cellPos, allNpcPersonalSpaceRadius))
                continue;

            result = cellPos;
            return true;
        }

        return false;
    }
    private GameObject SpawnNpcPrefabDirect(GameObject prefab, Vector3Int cellPos, string objectName)
    {
        if (prefab == null)
            return null;

        if (!CanPlaceObjectAt(cellPos, false))
            return null;

        Vector3 worldPos = GetWorldPositionFromCell(cellPos);
        worldPos.z = 0f;

        GameObject npc = Instantiate(prefab, worldPos, Quaternion.identity, runtimeObjectParent);
        npc.name = objectName;

        SetSortingOrderRecursive(npc, npcSortingOrder);

        occupiedObjectCells.Add(cellPos);
        MarkOccupiedAround(cellPos, allNpcPersonalSpaceRadius, allNpcPersonalSpaceRadius);

        return npc;
    }
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }


}