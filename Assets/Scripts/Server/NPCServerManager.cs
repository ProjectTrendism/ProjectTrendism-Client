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
/// - NPC 대화 시 POST /explore/npcs/{id}/talk 요청을 서버로 보냄
/// - 성공 응답만 NPCInteraction 쪽에서 로컬 KeywordManager에 반영하도록 연결
/// </summary>
public class NPCServerManager : MonoBehaviour
{
    public static NPCServerManager Instance;

    [Header("서버 설정")]
    [Tooltip("예: https://xxxx.ngrok-free.dev")]
    public string baseUrl = "https://neural-positioning-migration-commissioners.trycloudflare.com";

    [Tooltip("NPC 목록 조회 API")]
    public string npcListPath = "/explore/npcs";

    [Header("NPC 대화 API")]
    [Tooltip("NPC 대화 endpoint. {0} 자리에 NPC 서버 id가 들어갑니다.")]
    public string npcTalkPathFormat = "/explore/action";

    [Tooltip("대화 요청 body. 현재 서버는 /explore/action에 { action_type:TALK, target_id:npcId }를 보냅니다.")]
    [TextArea(2, 4)]
    public string npcTalkJsonBodyFormat = "{\"action_type\":\"TALK\",\"target_id\":{0}}";

    [Tooltip("대화 요청 방식. 서버가 POST면 POST, GET이면 GET으로 설정하세요.")]
    public string npcTalkMethod = "POST";

    [Header("요청 설정")]
    [Tooltip("서버 요청 시간 제한")]
    public int timeoutSeconds = 5;

    [Tooltip("ngrok 무료 주소 사용 시 반드시 켜두세요.")]
    public bool useNgrokSkipWarningHeader = true;

    [Tooltip("요청/응답 로그 출력")]
    public bool verboseLog = true;

    [Tooltip("켜면 ApiManager의 BaseUrl을 우선 사용합니다. 서버 주소를 한 곳에서 관리하기 위함입니다.")]
    public bool useApiManagerBaseUrl = true;

    private readonly Dictionary<int, ServerNpcData> npcDataById = new Dictionary<int, ServerNpcData>();
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

    private void Start()
    {
        StartCoroutine(RefreshNpcList());
    }

    public void RegisterNPC(NPCInteraction npc)
    {
        if (npc == null)
            return;

        if (!registeredNpcs.Contains(npc))
        {
            registeredNpcs.Add(npc);
        }

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
        {
            Debug.Log($"[NPCServerManager] NPC 목록 요청\nURL: {url}");
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            ApplyCommonHeaders(request);
            request.timeout = timeoutSeconds;

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[NPCServerManager] NPC 목록 로드 실패\n" +
                    $"HTTP: {request.responseCode}\n" +
                    $"ERROR: {request.error}\n" +
                    $"URL: {url}\n" +
                    $"RES BODY: {responseText}"
                );

                yield break;
            }

            if (verboseLog)
            {
                Debug.Log(
                    $"[NPCServerManager] NPC 목록 응답 성공\n" +
                    $"HTTP: {request.responseCode}\n" +
                    $"RES BODY: {responseText}"
                );
            }

            ServerNpcListResponse response = null;

            try
            {
                response = JsonConvert.DeserializeObject<ServerNpcListResponse>(responseText);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[NPCServerManager] NPC 목록 JSON 파싱 실패\n" +
                    $"ERROR: {e.Message}\n" +
                    $"RES BODY: {responseText}"
                );

                yield break;
            }

            if (response == null)
            {
                Debug.LogWarning($"[NPCServerManager] NPC 목록 응답이 null입니다.\nRES BODY: {responseText}");
                yield break;
            }

            if (response.status != "success")
            {
                Debug.LogWarning(
                    $"[NPCServerManager] NPC 목록 응답 status 확인 필요\n" +
                    $"STATUS: {response.status}\n" +
                    $"RES BODY: {responseText}"
                );

                yield break;
            }

            if (response.data == null)
            {
                Debug.LogWarning($"[NPCServerManager] NPC 목록 data가 null입니다.\nRES BODY: {responseText}");
                yield break;
            }

            npcDataById.Clear();

            for (int i = 0; i < response.data.Length; i++)
            {
                ServerNpcData data = response.data[i];

                if (data == null)
                    continue;

                npcDataById[data.id] = data;
            }

            Debug.Log($"[NPCServerManager] NPC 목록 로드 완료: {npcDataById.Count}개");

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
                $"[NPCServerManager] NPC 대화 요청\n" +
                $"METHOD: {method}\n" +
                $"URL: {url}\n" +
                $"REQ BODY: {jsonBody}"
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
            {
                request.downloadHandler = new DownloadHandlerBuffer();
            }

            ApplyCommonHeaders(request);
            request.timeout = timeoutSeconds;

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[NPCServerManager] NPC 대화 처리 실패\n" +
                    $"HTTP: {request.responseCode}\n" +
                    $"ERROR: {request.error}\n" +
                    $"METHOD: {method}\n" +
                    $"URL: {url}\n" +
                    $"REQ BODY: {jsonBody}\n" +
                    $"RES BODY: {responseText}"
                );

                onComplete?.Invoke(null);
                yield break;
            }

            if (verboseLog)
            {
                Debug.Log(
                    $"[NPCServerManager] NPC 대화 응답 성공\n" +
                    $"HTTP: {request.responseCode}\n" +
                    $"RES BODY: {responseText}"
                );
            }

            ServerNpcTalkResponse response = null;

            try
            {
                response = JsonConvert.DeserializeObject<ServerNpcTalkResponse>(responseText);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[NPCServerManager] NPC 대화 응답 JSON 파싱 실패\n" +
                    $"ERROR: {e.Message}\n" +
                    $"RES BODY: {responseText}"
                );

                onComplete?.Invoke(null);
                yield break;
            }

            if (response == null)
            {
                Debug.LogWarning($"[NPCServerManager] NPC 대화 응답이 null입니다.\nRES BODY: {responseText}");
                onComplete?.Invoke(null);
                yield break;
            }

            if (response.status != "success")
            {
                Debug.LogWarning(
                    $"[NPCServerManager] NPC 대화 응답 status 확인 필요\n" +
                    $"STATUS: {response.status}\n" +
                    $"RES BODY: {responseText}"
                );

                onComplete?.Invoke(null);
                yield break;
            }

            onComplete?.Invoke(response);
        }
    }

    private string BuildTalkPath(int npcId)
    {
        if (string.IsNullOrEmpty(npcTalkPathFormat))
        {
            return $"/explore/npcs/{npcId}/talk";
        }

        if (npcTalkPathFormat.Contains("{0}"))
        {
            return string.Format(npcTalkPathFormat, npcId);
        }

        return npcTalkPathFormat;
    }

    private string BuildTalkBody(int npcId)
    {
        if (string.IsNullOrWhiteSpace(npcTalkJsonBodyFormat))
        {
            return "{}";
        }

        if (npcTalkJsonBodyFormat.Contains("{0}"))
        {
            return string.Format(npcTalkJsonBodyFormat, npcId);
        }

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
        }
        else if (npcId != 0)
        {
            Debug.LogWarning(
                $"[NPCServerManager] 서버 응답에서 NPC id={npcId} 데이터를 찾지 못했습니다.\n" +
                $"NPC 오브젝트의 npcServerId와 서버 /explore/npcs 응답 id를 확인하세요."
            );
        }
    }

    private string BuildUrl(string path)
    {
        string effectiveBaseUrl = baseUrl;

        if (useApiManagerBaseUrl && ApiManager.Instance != null && !string.IsNullOrEmpty(ApiManager.Instance.BaseUrl))
        {
            effectiveBaseUrl = ApiManager.Instance.BaseUrl;
        }

        if (string.IsNullOrEmpty(effectiveBaseUrl))
            return path;

        string trimmedBase = effectiveBaseUrl.TrimEnd('/');
        string fixedPath = string.IsNullOrEmpty(path) ? "" : path;

        if (!fixedPath.StartsWith("/"))
        {
            fixedPath = "/" + fixedPath;
        }

        return trimmedBase + fixedPath;
    }

    private void ApplyCommonHeaders(UnityWebRequest request)
    {
        if (request == null)
            return;

        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");

        if (useNgrokSkipWarningHeader)
        {
            request.SetRequestHeader("ngrok-skip-browser-warning", "true");
        }
    }
}