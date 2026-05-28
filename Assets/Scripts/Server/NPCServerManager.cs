using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// NPC 서버 연동 매니저.
/// - GET /explore/npcs 로 NPC 목록을 받아 NPCInteraction에 런타임 주입
/// - NPC 대화 시 POST /explore/action 요청을 서버로 보냄
/// - ApiManager의 BaseUrl을 우선 사용해서 Cloudflare/Localhost를 한 곳에서 바꿀 수 있게 함
/// </summary>
public class NPCServerManager : MonoBehaviour
{
    public static NPCServerManager Instance;

    [Header("서버 설정")]
    [Tooltip("ApiManager가 없을 때만 사용하는 예비 Base URL")]
    public string baseUrl = "https://neural-positioning-migration-commissioners.trycloudflare.com";

    [Tooltip("켜면 ApiManager의 BaseUrl을 우선 사용합니다.")]
    public bool useApiManagerBaseUrl = true;

    [Tooltip("NPC 목록 조회 API")]
    public string npcListPath = "/explore/npcs";

    [Header("NPC 대화 API")]
    [Tooltip("현재 서버 기준: /explore/action")]
    public string npcTalkPathFormat = "/explore/action";

    [Tooltip("현재 서버 기준 body: { action_type:TALK, target_id:npcId }")]
    [TextArea(2, 4)]
    public string npcTalkJsonBodyFormat = "{\"action_type\":\"TALK\",\"target_id\":{0}}";

    [Tooltip("대화 요청 방식")]
    public string npcTalkMethod = "POST";

    [Header("요청 설정")]
    public int timeoutSeconds = 10;
    public bool logResponseBodyOnSuccess = false;
    public bool useNgrokSkipWarningHeader = true;
    public bool verboseLog = false;

    [Tooltip("ExploreServerManager 없이 단독 배치했을 때만 자동 새로고침합니다.")]
    public bool autoRefreshIfNoExploreManager = true;

    private readonly Dictionary<int, ServerNpcData> npcDataById = new Dictionary<int, ServerNpcData>();
    private readonly Dictionary<string, ServerNpcData> npcDataByName = new Dictionary<string, ServerNpcData>();
    private readonly List<NPCInteraction> registeredNpcs = new List<NPCInteraction>();

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

    private IEnumerator Start()
    {
        yield return null;

        if (ExploreServerManager.Instance == null && autoRefreshIfNoExploreManager)
        {
            yield return StartCoroutine(RefreshNpcList());
        }
    }

    public void RegisterNPC(NPCInteraction npc)
    {
        if (npc == null)
            return;

        if (!registeredNpcs.Contains(npc))
            registeredNpcs.Add(npc);

        ApplyCachedDataToNpc(npc);
    }

    public void UnregisterNPC(NPCInteraction npc)
    {
        if (npc == null)
            return;

        registeredNpcs.Remove(npc);
    }

    [ContextMenu("서버 NPC 목록 새로고침")]
    public void RefreshNpcListFromInspector()
    {
        StartCoroutine(RefreshNpcList());
    }

    public IEnumerator RefreshNpcList()
    {
        string url = BuildUrl(npcListPath);

        if (verboseLog)
            Debug.Log("[NPCServerManager] NPC 목록 요청\nURL: " + url);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            ApplyCommonHeaders(request);
            request.timeout = timeoutSeconds;

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (!IsHttpSuccess(request))
            {
                Debug.LogWarning(
                    "[NPCServerManager] NPC 목록 로드 실패\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "ERROR: " + request.error + "\n" +
                    "URL: " + url + "\n" +
                    "RES BODY: " + responseText
                );
                yield break;
            }

            if (verboseLog)
                Debug.Log(logResponseBodyOnSuccess ? "[NPCServerManager] NPC 목록 응답 성공\nHTTP: " + request.responseCode + "\nRES BODY: " + responseText : "[NPCServerManager] NPC 목록 응답 성공 / HTTP: " + request.responseCode);

            ServerNpcListResponse response = null;

            try
            {
                response = JsonConvert.DeserializeObject<ServerNpcListResponse>(responseText);
            }
            catch (Exception e)
            {
                Debug.LogError("[NPCServerManager] NPC 목록 JSON 파싱 실패\nERROR: " + e.Message + "\nRES BODY: " + responseText);
                yield break;
            }

            if (response == null || response.status != "success" || response.data == null)
            {
                Debug.LogWarning("[NPCServerManager] NPC 목록 응답 구조 확인 필요\nRES BODY: " + responseText);
                yield break;
            }

            npcDataById.Clear();
            npcDataByName.Clear();

            List<string> loadedList = new List<string>();

            for (int i = 0; i < response.data.Length; i++)
            {
                ServerNpcData data = response.data[i];
                if (data == null)
                    continue;

                npcDataById[data.id] = data;

                string key = NormalizeNpcName(data.name);
                if (!string.IsNullOrEmpty(key) && !npcDataByName.ContainsKey(key))
                    npcDataByName[key] = data;

                loadedList.Add($"{data.id}:{data.name}");
            }

            if (verboseLog)
            {
                Debug.Log("[NPCServerManager] NPC 목록 로드 완료: " + npcDataById.Count + "개");
                if (logResponseBodyOnSuccess)
                    Debug.Log("[NPCServerManager] 서버 NPC id 목록 = " + string.Join(", ", loadedList));
            }

            ApplyCachedDataToAllNpcs();
        }
    }

    public IEnumerator TalkToNpc(int npcId, Action<ServerNpcTalkResponse> onComplete)
    {
        string path = BuildTalkPath(npcId);
        string url = BuildUrl(path);
        string jsonBody = BuildTalkBody(npcId);
        string method = string.IsNullOrWhiteSpace(npcTalkMethod) ? "POST" : npcTalkMethod.Trim().ToUpper();

        if (verboseLog)
        {
            Debug.Log(
                "[NPCServerManager] NPC 대화 요청\n" +
                "METHOD: " + method + "\n" +
                "URL: " + url + "\n" +
                "REQ BODY: " + jsonBody
            );
        }

        UnityWebRequest request = null;

        if (method == "GET")
        {
            request = UnityWebRequest.Get(url);
        }
        else
        {
            request = new UnityWebRequest(url, method);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
        }

        using (request)
        {
            if (request.downloadHandler == null)
                request.downloadHandler = new DownloadHandlerBuffer();

            ApplyCommonHeaders(request);
            request.timeout = timeoutSeconds;

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (!IsHttpSuccess(request))
            {
                Debug.LogWarning(
                    "[NPCServerManager] NPC 대화 처리 실패\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "ERROR: " + request.error + "\n" +
                    "METHOD: " + method + "\n" +
                    "URL: " + url + "\n" +
                    "REQ BODY: " + jsonBody + "\n" +
                    "RES BODY: " + responseText
                );

                onComplete?.Invoke(null);
                yield break;
            }

            if (verboseLog)
                Debug.Log(logResponseBodyOnSuccess ? "[NPCServerManager] NPC 대화 응답 성공\nHTTP: " + request.responseCode + "\nRES BODY: " + responseText : "[NPCServerManager] NPC 대화 응답 성공 / HTTP: " + request.responseCode);

            ServerNpcTalkResponse response = null;

            try
            {
                response = JsonConvert.DeserializeObject<ServerNpcTalkResponse>(responseText);
            }
            catch (Exception e)
            {
                Debug.LogError("[NPCServerManager] NPC 대화 응답 JSON 파싱 실패\nERROR: " + e.Message + "\nRES BODY: " + responseText);
                onComplete?.Invoke(null);
                yield break;
            }

            if (response == null || response.status != "success")
            {
                Debug.LogWarning("[NPCServerManager] NPC 대화 응답 status 확인 필요\nRES BODY: " + responseText);
                onComplete?.Invoke(null);
                yield break;
            }

            onComplete?.Invoke(response);
        }
    }

    private bool IsHttpSuccess(UnityWebRequest request)
    {
        return request != null &&
               request.result == UnityWebRequest.Result.Success &&
               request.responseCode >= 200 &&
               request.responseCode < 300;
    }

    private string BuildTalkPath(int npcId)
    {
        if (string.IsNullOrEmpty(npcTalkPathFormat))
            return "/explore/action";

        if (npcTalkPathFormat.Contains("{0}"))
            return string.Format(npcTalkPathFormat, npcId);

        return npcTalkPathFormat;
    }

    private string BuildTalkBody(int npcId)
    {
        // 중요:
        // JSON 문자열에는 { } 중괄호가 들어간다.
        // string.Format("{\"action_type\":...}", id)를 쓰면 JSON 중괄호까지 포맷 토큰으로 오해해서
        // FormatException: Input string was not in a correct format 오류가 난다.
        // 그래서 {0}만 직접 치환한다.
        if (string.IsNullOrWhiteSpace(npcTalkJsonBodyFormat))
            return JsonConvert.SerializeObject(new { action_type = "TALK", target_id = npcId });

        if (npcTalkJsonBodyFormat.Contains("{0}"))
            return npcTalkJsonBodyFormat.Replace("{0}", npcId.ToString());

        return npcTalkJsonBodyFormat;
    }

    private void ApplyCachedDataToAllNpcs()
    {
        for (int i = registeredNpcs.Count - 1; i >= 0; i--)
        {
            if (registeredNpcs[i] == null)
            {
                registeredNpcs.RemoveAt(i);
                continue;
            }

            ApplyCachedDataToNpc(registeredNpcs[i]);
        }
    }

    private void ApplyCachedDataToNpc(NPCInteraction npc)
    {
        if (npc == null)
            return;

        int npcId = npc.GetNpcServerId();

        if (npcDataById.TryGetValue(npcId, out ServerNpcData data))
        {
            npc.ApplyServerData(data);
            return;
        }

        // Cloudflare 서버 DB가 여러 번 seed된 경우, NPC id가 1~25가 아니라 26~50, 51~75처럼 밀릴 수 있다.
        // 이때는 Unity 인스펙터의 예전 id가 맞지 않으므로 이름으로 자동 보정한다.
        string nameKey = NormalizeNpcName(npc.GetNpcName());
        if (!string.IsNullOrEmpty(nameKey) && npcDataByName.TryGetValue(nameKey, out data))
        {
            Debug.LogWarning(
                "[NPCServerManager] NPC id 불일치 감지 → 이름으로 자동 매칭합니다. " +
                $"Unity id={npcId}, 서버 id={data.id}, name={data.name}"
            );

            npc.ApplyServerData(data);
            return;
        }

        if (npcId != 0)
        {
            Debug.LogWarning(
                "[NPCServerManager] 서버 응답에서 NPC id=" + npcId + " 데이터를 찾지 못했습니다.\n" +
                "서버 NPC id 목록 로그를 확인하거나, NPC 이름이 서버 name과 정확히 같은지 확인하세요."
            );
        }
    }

    private string NormalizeNpcName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value.Trim().Replace(" ", "").Replace("　", "");
    }

    public bool TryGetNpcData(int npcId, out ServerNpcData data)
    {
        return npcDataById.TryGetValue(npcId, out data);
    }

    private string BuildUrl(string path)
    {
        if (useApiManagerBaseUrl && ApiManager.Instance != null && !string.IsNullOrEmpty(ApiManager.Instance.BaseUrl))
            return ApiManager.Instance.BuildUrl(path);

        string effectiveBaseUrl = string.IsNullOrEmpty(baseUrl) ? "" : baseUrl.TrimEnd('/');

        if (string.IsNullOrEmpty(effectiveBaseUrl))
            return path;

        string fixedPath = string.IsNullOrEmpty(path) ? "" : path;
        if (!fixedPath.StartsWith("/"))
            fixedPath = "/" + fixedPath;

        return effectiveBaseUrl + fixedPath;
    }

    private void ApplyCommonHeaders(UnityWebRequest request)
    {
        if (request == null)
            return;

        if (ApiManager.Instance != null)
        {
            ApiManager.Instance.ApplyCommonHeaders(request);
        }
        else
        {
            request.SetRequestHeader("Accept", "application/json");
            if (useNgrokSkipWarningHeader)
                request.SetRequestHeader("ngrok-skip-browser-warning", "true");
        }

        request.SetRequestHeader("Content-Type", "application/json");
    }
}
