using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 서버의 GET /explore/npcs 응답을 받아 씬 안의 NPCInteraction에 런타임 주입한다.
/// NPC 대화 처리는 서버 POST를 먼저 거친 뒤, 서버가 내려준 결과만 로컬 UI/KeywordManager에 동기화한다.
/// </summary>
public class NPCServerManager : MonoBehaviour
{
    [Header("서버 설정")]
    [Tooltip("예: http://localhost:8000 또는 ApiManager.BaseUrl과 동일하게 맞추기")]
    public string baseUrl = "http://localhost:8000";

    [Tooltip("GET /explore/npcs")]
    public string npcListPath = "/explore/npcs";

    [Tooltip("백엔드와 맞춰야 함. 예: POST /explore/npcs/{id}/talk")]
    public string npcTalkPathFormat = "/explore/npcs/{0}/talk";

    private readonly Dictionary<int, ServerNpcData> npcDataById = new Dictionary<int, ServerNpcData>();
    private readonly List<NPCInteraction> registeredNpcs = new List<NPCInteraction>();

    private void Start()
    {
        StartCoroutine(RefreshNpcList());
    }

    public void RegisterNPC(NPCInteraction npc)
    {
        if (npc == null) return;

        if (!registeredNpcs.Contains(npc))
        {
            registeredNpcs.Add(npc);
        }

        ApplyCachedDataToNpc(npc);
    }

    public void UnregisterNPC(NPCInteraction npc)
    {
        if (npc == null) return;
        registeredNpcs.Remove(npc);
    }

    public IEnumerator RefreshNpcList()
    {
        string url = BuildUrl(npcListPath);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"NPC 목록 로드 실패: {request.responseCode} / {request.error}\n{url}");
                yield break;
            }

            string json = request.downloadHandler.text;

            ServerNpcListResponse response = null;

            try
            {
                response = JsonConvert.DeserializeObject<ServerNpcListResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"NPC 목록 JSON 파싱 실패\n{e.Message}\n{json}");
                yield break;
            }

            if (response == null || response.status != "success" || response.data == null)
            {
                Debug.LogWarning($"NPC 목록 응답 형식 확인 필요\n{json}");
                yield break;
            }

            npcDataById.Clear();

            for (int i = 0; i < response.data.Length; i++)
            {
                ServerNpcData data = response.data[i];
                npcDataById[data.id] = data;
            }

            ApplyCachedDataToAllNpcs();
        }
    }

    public IEnumerator TalkToNpc(int npcId, Action<ServerNpcTalkResponse> onComplete)
    {
        string path = string.Format(npcTalkPathFormat, npcId);
        string url = BuildUrl(path);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"NPC 대화 처리 실패: {request.responseCode} / {request.error}\n{url}");
                onComplete?.Invoke(null);
                yield break;
            }

            string json = request.downloadHandler.text;

            ServerNpcTalkResponse response = null;

            try
            {
                response = JsonConvert.DeserializeObject<ServerNpcTalkResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"NPC 대화 응답 JSON 파싱 실패\n{e.Message}\n{json}");
                onComplete?.Invoke(null);
                yield break;
            }

            onComplete?.Invoke(response);
        }
    }

    private void ApplyCachedDataToAllNpcs()
    {
        for (int i = 0; i < registeredNpcs.Count; i++)
        {
            ApplyCachedDataToNpc(registeredNpcs[i]);
        }
    }

    private void ApplyCachedDataToNpc(NPCInteraction npc)
    {
        if (npc == null) return;

        int npcId = npc.GetNpcServerId();

        if (npcDataById.TryGetValue(npcId, out ServerNpcData data))
        {
            npc.ApplyServerData(data);
        }
        else if (npcId != 0)
        {
            Debug.LogWarning($"서버 응답에서 NPC id={npcId} 데이터를 찾지 못했습니다. 인스펙터 npcServerId를 확인하세요.");
        }
    }

    private string BuildUrl(string path)
    {
        if (string.IsNullOrEmpty(baseUrl)) return path;

        string trimmedBase = baseUrl.TrimEnd('/');
        string fixedPath = path.StartsWith("/") ? path : "/" + path;

        return trimmedBase + fixedPath;
    }
}
