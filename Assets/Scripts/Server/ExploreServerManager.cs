using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 탐사 서버 전체 흐름 매니저.
/// - 서버/로컬 주소는 ApiManager에서 선택
/// - 시작 시 /explore/start로 ACTIVE 시즌 준비
/// - NPC 목록, 인벤토리 키워드, 빈도수를 서버에서 받아 Unity 매니저에 반영
/// - 테스트 초기화 시 서버 데이터와 Unity 로컬 런타임 데이터를 함께 초기화
/// </summary>
public class ExploreServerManager : MonoBehaviour
{
    public static ExploreServerManager Instance;

    [Header("자동 실행")]
    public bool autoRunOnStart = true;
    public bool startSeasonOnStart = true;
    public bool refreshNpcAfterStart = true;
    public bool syncInventoryAfterStart = true;
    public bool syncFrequencyAfterStart = true;
    public bool syncAfterEveryNpcTalk = true;

    [Header("테스트 초기화")]
    [Tooltip("체크하면 Play를 누를 때마다 서버 ACTIVE 시즌의 시간/인벤토리/대화상태를 테스트용으로 초기화합니다. 실제 진행 테스트가 끝나면 꺼두세요.")]
    public bool resetServerOnStartForTest = false;

    [Tooltip("서버 인벤토리/빈도수 동기화 전에 Unity 로컬 키워드/소문 데이터를 비우고 서버 값으로 교체합니다. 테스트 중에는 켜두는 것을 권장합니다.")]
    public bool replaceLocalDataOnServerSync = true;

    [Header("API 경로")]
    public string startSeasonPath = "/explore/start";
    public string statusPath = "/explore/status";
    public string inventoryPath = "/explore/inventory";
    public string frequencyPath = "/explore/frequency";
    public string dayEndPath = "/explore/day-end";
    public string devResetPath = "/explore/dev-reset";

    [Header("로컬 fallback")]
    public bool allowLocalWhenServerFails = true;

    [Header("디버그")]
    public bool verboseLog = false;

    [Header("최근 서버 상태 - 확인용")]
    [SerializeField] private bool serverReady = false;
    [SerializeField] private string lastStatusText = "";
    [SerializeField] private int syncedInventoryCount = 0;
    [SerializeField] private int syncedFrequencyCount = 0;

    private bool isRunningStartupFlow = false;

    public bool ServerReady => serverReady;
    public string LastStatusText => lastStatusText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator Start()
    {
        if (!autoRunOnStart)
            yield break;

        yield return null;
        yield return StartCoroutine(RunExploreStartupFlow());
    }

    [ContextMenu("탐사 서버 시작 흐름 실행")]
    public void RunExploreStartupFlowFromInspector()
    {
        StartCoroutine(RunExploreStartupFlow());
    }

    public IEnumerator RunExploreStartupFlow()
    {
        if (isRunningStartupFlow)
            yield break;

        isRunningStartupFlow = true;

        if (verboseLog)
            Debug.Log("[ExploreServerManager] 탐사 서버 시작 흐름 시작");

        yield return WaitForApiManager();

        if (ApiManager.Instance == null)
        {
            Debug.LogWarning("[ExploreServerManager] ApiManager가 없습니다. 서버 동기화를 건너뜁니다.");
            isRunningStartupFlow = false;
            yield break;
        }

        yield return StartCoroutine(ApiManager.Instance.CheckServerConnection());

        if (!ApiManager.Instance.isServerConnected)
        {
            serverReady = false;
            Debug.LogWarning("[ExploreServerManager] 서버 연결 실패. 로컬 fallback 사용 가능 상태입니다.");
            isRunningStartupFlow = false;
            yield break;
        }

        if (resetServerOnStartForTest)
        {
            ClearLocalExploreRuntimeData("Play 시작 전 로컬 초기화");
            yield return StartCoroutine(ResetExploreServerForTest());
            ClearLocalExploreRuntimeData("dev-reset 직후 로컬 초기화");
        }

        if (startSeasonOnStart)
            yield return StartCoroutine(StartSeason());

        yield return StartCoroutine(RefreshStatus());
        if (IsTimeExpired())
        {
            Debug.Log("[ExploreServerManager] 22시 이후 감지 -> 자동 day-end");
            yield return StartCoroutine(EndDayOnServer());
        }

        if (refreshNpcAfterStart && NPCServerManager.Instance != null)
            yield return StartCoroutine(NPCServerManager.Instance.RefreshNpcList());

        if (syncInventoryAfterStart)
            yield return StartCoroutine(SyncInventoryFromServer());

        if (syncFrequencyAfterStart)
            yield return StartCoroutine(SyncFrequencyFromServer());

        serverReady = true;
        isRunningStartupFlow = false;

        if (verboseLog)
            Debug.Log("[ExploreServerManager] 탐사 서버 시작 흐름 완료");
    }

    private IEnumerator WaitForApiManager()
    {
        float timer = 0f;

        while (ApiManager.Instance == null && timer < 3f)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    [ContextMenu("테스트용 서버 탐사 초기화")]
    public void ResetExploreServerForTestFromInspector()
    {
        StartCoroutine(ResetExploreServerForTestAndSync());
    }

    public IEnumerator ResetExploreServerForTestAndSync()
    {
        yield return WaitForApiManager();

        if (ApiManager.Instance == null)
        {
            Debug.LogWarning("[ExploreServerManager] ApiManager가 없어 서버 초기화를 실행할 수 없습니다.");
            yield break;
        }

        if (!ApiManager.Instance.isServerConnected)
            yield return StartCoroutine(ApiManager.Instance.CheckServerConnection());

        if (!ApiManager.Instance.isServerConnected)
        {
            Debug.LogWarning("[ExploreServerManager] 서버 연결 실패. 서버 초기화를 실행하지 못했습니다.");
            yield break;
        }

        ClearLocalExploreRuntimeData("수동 dev-reset 전 로컬 초기화");
        yield return StartCoroutine(ResetExploreServerForTest());
        ClearLocalExploreRuntimeData("수동 dev-reset 직후 로컬 초기화");

        yield return StartCoroutine(RefreshStatus());

        if (NPCServerManager.Instance != null)
            yield return StartCoroutine(NPCServerManager.Instance.RefreshNpcList());

        yield return StartCoroutine(SyncInventoryFromServer());
        yield return StartCoroutine(SyncFrequencyFromServer());
    }

    public IEnumerator ResetExploreServerForTest()
    {
        bool success = false;
        string responseText = "";

        yield return StartCoroutine(ApiManager.Instance.Post(
            devResetPath,
            "{}",
            response =>
            {
                success = true;
                responseText = response;
            },
            error =>
            {
                success = false;
                responseText = error;
            }
        ));

        if (success)
        {
            serverReady = true;
            Debug.Log("[ExploreServerManager] 테스트용 서버 탐사 초기화 완료");
            if (verboseLog)
                Debug.Log(responseText);
        }
        else
        {
            Debug.LogWarning("[ExploreServerManager] 테스트용 서버 탐사 초기화 실패. 서버에 /explore/dev-reset 엔드포인트가 추가되어 있는지 확인하세요.\n" + responseText);
        }
    }

    public IEnumerator StartSeason()
    {
        bool success = false;
        string responseText = "";

        yield return StartCoroutine(ApiManager.Instance.Post(
            startSeasonPath,
            "{}",
            response =>
            {
                success = true;
                responseText = response;
            },
            error =>
            {
                success = false;
                responseText = error;
            }
        ));

        if (success)
        {
            serverReady = true;
            if (verboseLog)
                Debug.Log("[ExploreServerManager] 시즌 시작 성공\n" + responseText);
        }
        else
        {
            serverReady = false;
            Debug.LogWarning("[ExploreServerManager] 시즌 시작 실패\n" + responseText);
        }
    }

    public IEnumerator RefreshStatus()
    {
        bool success = false;
        string responseText = "";

        yield return StartCoroutine(ApiManager.Instance.Get(
            statusPath,
            response =>
            {
                success = true;
                responseText = response;
            },
            error =>
            {
                success = false;
                responseText = error;
            }
        ));

        if (success)
        {
            lastStatusText = responseText;
            if (verboseLog)
                Debug.Log("[ExploreServerManager] 상태 갱신 성공\n" + responseText);
        }
        else
        {
            Debug.LogWarning("[ExploreServerManager] 상태 갱신 실패\n" + responseText);
        }
    }

    [ContextMenu("서버 인벤토리 동기화")]
    public void SyncInventoryFromInspector()
    {
        StartCoroutine(SyncInventoryFromServer());
    }

    public IEnumerator SyncInventoryFromServer()
    {
        bool success = false;
        string responseText = "";

        yield return StartCoroutine(ApiManager.Instance.Get(
            inventoryPath,
            response =>
            {
                success = true;
                responseText = response;
            },
            error =>
            {
                success = false;
                responseText = error;
            }
        ));

        if (!success)
        {
            Debug.LogWarning("[ExploreServerManager] 인벤토리 동기화 실패\n" + responseText);
            yield break;
        }

        if (replaceLocalDataOnServerSync && KeywordManager.Instance != null)
            KeywordManager.Instance.ClearKeywords();

        int imported = ImportInventoryResponse(responseText);
        syncedInventoryCount = imported;

        if (verboseLog)
            Debug.Log("[ExploreServerManager] 서버 인벤토리 동기화 완료: " + imported + "개");
    }

    [ContextMenu("서버 빈도수 동기화")]
    public void SyncFrequencyFromInspector()
    {
        StartCoroutine(SyncFrequencyFromServer());
    }

    public IEnumerator SyncFrequencyFromServer()
    {
        bool success = false;
        string responseText = "";

        yield return StartCoroutine(ApiManager.Instance.Get(
            frequencyPath,
            response =>
            {
                success = true;
                responseText = response;
            },
            error =>
            {
                success = false;
                responseText = error;
            }
        ));

        if (!success)
        {
            Debug.LogWarning("[ExploreServerManager] 빈도수 동기화 실패\n" + responseText);
            yield break;
        }

        if (replaceLocalDataOnServerSync && RumorManager.Instance != null)
            RumorManager.Instance.ClearRumors();

        int imported = ImportFrequencyResponse(responseText);
        syncedFrequencyCount = imported;

        if (verboseLog)
            Debug.Log("[ExploreServerManager] 서버 빈도수 동기화 완료: " + imported + "개");
    }

    /*
    public IEnumerator SyncAfterNpcTalk()
    {
        if (!syncAfterEveryNpcTalk)
            yield break;

        yield return StartCoroutine(SyncInventoryFromServer());
        yield return StartCoroutine(SyncFrequencyFromServer());
    }*/
    public bool refreshNpcAfterEveryTalk = true;

    public IEnumerator SyncAfterNpcTalk()
    {
        if (!syncAfterEveryNpcTalk)
            yield break;

        // 대화 후 서버에서 talked / perceived_reliability가 바뀌므로 NPC 목록을 다시 받아야 함
        if (refreshNpcAfterEveryTalk && NPCServerManager.Instance != null)
        {
            yield return StartCoroutine(NPCServerManager.Instance.RefreshNpcList());
        }

        yield return StartCoroutine(SyncInventoryFromServer());
        yield return StartCoroutine(SyncFrequencyFromServer());
    }

    private void ClearLocalExploreRuntimeData(string reason)
    {
        if (KeywordManager.Instance != null)
            KeywordManager.Instance.ClearKeywords();

        if (RumorManager.Instance != null)
            RumorManager.Instance.ClearRumors();

        RumorLogUI rumorUI = FindObjectOfType<RumorLogUI>();
        if (rumorUI != null)
            rumorUI.RefreshRumorUI();

        if (verboseLog)
            Debug.Log("[ExploreServerManager] " + reason + " 완료");
    }

    private int ImportInventoryResponse(string json)
    {
        if (KeywordManager.Instance == null)
        {
            Debug.LogWarning("[ExploreServerManager] KeywordManager가 없어 인벤토리 키워드를 반영하지 못했습니다.");
            return 0;
        }

        List<JToken> keywordTokens = ExtractArrayCandidates(json, new string[]
        {
            "data.keywords",
            "data.inventory",
            "data.items",
            "data",
            "keywords",
            "inventory",
            "items"
        });

        int count = 0;

        for (int i = 0; i < keywordTokens.Count; i++)
        {
            JToken token = keywordTokens[i];
            int id = ReadInt(token, "keyword_id", "id", "server_id");
            string name = ReadString(token, "keyword_name", "name", "keyword");
            string category = ReadString(token, "keyword_type", "category", "type");
            KeywordType type = ParseKeywordType(category, name);

            if (string.IsNullOrEmpty(name))
                continue;

            KeywordManager.Instance.AddKeyword(id, name, type);
            count++;
        }

        return count;
    }

    private int ImportFrequencyResponse(string json)
    {
        if (RumorManager.Instance == null)
        {
            Debug.LogWarning("[ExploreServerManager] RumorManager가 없어 빈도수를 반영하지 못했습니다.");
            return 0;
        }

        List<FrequencyRecord> records = ExtractFrequencyRecords(json);

        for (int i = 0; i < records.Count; i++)
        {
            FrequencyRecord record = records[i];
            if (string.IsNullOrEmpty(record.keywordName))
                continue;

            RumorManager.Instance.SetServerTrendData(
                record.keywordName,
                Mathf.Max(1, record.count),
                Mathf.Max(1, record.trendScore)
            );
        }

        RumorLogUI rumorUI = FindObjectOfType<RumorLogUI>();
        if (rumorUI != null)
            rumorUI.RefreshRumorUI();

        return records.Count;
    }

    private List<JToken> ExtractArrayCandidates(string json, string[] paths)
    {
        List<JToken> result = new List<JToken>();

        try
        {
            JObject root = JObject.Parse(json);

            for (int p = 0; p < paths.Length; p++)
            {
                JToken token = SelectTokenByPath(root, paths[p]);

                if (token == null)
                    continue;

                if (token.Type == JTokenType.Array)
                {
                    foreach (JToken child in token.Children())
                        result.Add(child);

                    if (result.Count > 0)
                        return result;
                }
                else if (token.Type == JTokenType.Object)
                {
                    result.Add(token);
                    return result;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ExploreServerManager] JSON 배열 추출 실패: " + e.Message + "\n" + json);
        }

        return result;
    }

    private List<FrequencyRecord> ExtractFrequencyRecords(string json)
    {
        List<FrequencyRecord> result = new List<FrequencyRecord>();

        try
        {
            JObject root = JObject.Parse(json);
            JToken data = root["data"] ?? root;

            if (data.Type == JTokenType.Object)
            {
                JObject obj = (JObject)data;

                JToken arrayToken =
                    obj["frequencies"] ??
                    obj["frequency"] ??
                    obj["items"] ??
                    obj["keywords"] ??
                    obj["trends"];

                if (arrayToken != null && arrayToken.Type == JTokenType.Array)
                {
                    AddFrequencyArray(result, arrayToken);
                    return result;
                }

                foreach (JProperty prop in obj.Properties())
                {
                    if (prop.Value.Type == JTokenType.Integer || prop.Value.Type == JTokenType.Float)
                    {
                        FrequencyRecord record = new FrequencyRecord();
                        record.keywordName = prop.Name;
                        record.count = prop.Value.Value<int>();
                        record.trendScore = record.count;
                        result.Add(record);
                    }
                    else if (prop.Value.Type == JTokenType.Object)
                    {
                        FrequencyRecord record = ReadFrequencyObject(prop.Value);
                        if (string.IsNullOrEmpty(record.keywordName))
                            record.keywordName = prop.Name;

                        if (!string.IsNullOrEmpty(record.keywordName))
                            result.Add(record);
                    }
                }
            }
            else if (data.Type == JTokenType.Array)
            {
                AddFrequencyArray(result, data);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ExploreServerManager] 빈도수 JSON 파싱 실패: " + e.Message + "\n" + json);
        }

        return result;
    }

    private void AddFrequencyArray(List<FrequencyRecord> result, JToken arrayToken)
    {
        foreach (JToken child in arrayToken.Children())
        {
            FrequencyRecord record = ReadFrequencyObject(child);
            if (!string.IsNullOrEmpty(record.keywordName))
                result.Add(record);
        }
    }

    private FrequencyRecord ReadFrequencyObject(JToken token)
    {
        FrequencyRecord record = new FrequencyRecord();
        record.keywordName = ReadString(token, "keyword_name", "name", "keyword", "relatedKeyword");
        record.count = ReadInt(token, "mention_count", "count", "frequency", "appear_count", "value");
        record.trendScore = ReadInt(token, "trend_score", "score", "trendScore", "weight", "drop_count", "npc_count");

        if (record.trendScore <= 0)
            record.trendScore = record.count;

        return record;
    }

    private JToken SelectTokenByPath(JObject root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path))
            return null;

        string[] parts = path.Split('.');
        JToken current = root;

        for (int i = 0; i < parts.Length; i++)
        {
            if (current == null || current.Type != JTokenType.Object)
                return null;

            current = current[parts[i]];
        }

        return current;
    }

    private int ReadInt(JToken token, params string[] names)
    {
        if (token == null)
            return 0;

        for (int i = 0; i < names.Length; i++)
        {
            JToken value = token[names[i]];
            if (value == null)
                continue;

            int result;
            if (int.TryParse(value.ToString(), out result))
                return result;
        }

        return 0;
    }

    private string ReadString(JToken token, params string[] names)
    {
        if (token == null)
            return "";

        for (int i = 0; i < names.Length; i++)
        {
            JToken value = token[names[i]];
            if (value == null)
                continue;

            string text = value.ToString();
            if (!string.IsNullOrEmpty(text))
                return text;
        }

        return "";
    }

    private KeywordType ParseKeywordType(string category, string name)
    {
        if (!string.IsNullOrEmpty(category))
        {
            string upper = category.Trim().ToUpper();

            if (upper == "BASE" || upper == "MATERIAL" || upper == "재료")
                return KeywordType.Base;

            if (upper == "STYLE" || upper == "스타일")
                return KeywordType.Style;

            if (upper == "CONCEPT" || upper == "컨셉")
                return KeywordType.Concept;

            KeywordType parsed;
            if (Enum.TryParse(category, true, out parsed))
                return parsed;
        }

        if (KeywordManager.Instance != null)
        {
            KeywordData master = KeywordManager.Instance.GetMasterKeywordByName(name);
            if (master != null)
                return master.keywordType;
        }

        return KeywordType.Base;
    }

    [ContextMenu("서버 일차 종료 / 다음 날로 이동")]
    public void EndDayFromInspector()
    {
        StartCoroutine(EndDayOnServer());
    }

    public IEnumerator EndDayOnServer()
    {
        if (ApiManager.Instance == null)
            yield break;

        bool success = false;
        string responseText = "";

        yield return StartCoroutine(ApiManager.Instance.Post(
            dayEndPath,
            "{}",
            response =>
            {
                success = true;
                responseText = response;
            },
            error =>
            {
                success = false;
                responseText = error;
            }
        ));

        if (success)
        {
            Debug.Log("[ExploreServerManager] 서버 일차 종료 성공. 다음 탐험 시간으로 이동했습니다.\n" + responseText);
            yield return StartCoroutine(RefreshStatus());
            yield return StartCoroutine(SyncInventoryFromServer());
            yield return StartCoroutine(SyncFrequencyFromServer());
        }
        else
        {
            Debug.LogWarning("[ExploreServerManager] 서버 일차 종료 실패\n" + responseText);
        }
    }

    private class FrequencyRecord
    {
        public string keywordName;
        public int count;
        public int trendScore;
    }

    private bool IsTimeExpired()
    {
        if (string.IsNullOrEmpty(lastStatusText))
            return false;

        try
        {
            JObject root = JObject.Parse(lastStatusText);
            JToken time = root["data"]?["current_time"] ?? root["current_time"];
            if (time != null && int.TryParse(time.ToString(), out int t))
                return t >= 22;
        }
        catch { }

        return false;
    }
}
