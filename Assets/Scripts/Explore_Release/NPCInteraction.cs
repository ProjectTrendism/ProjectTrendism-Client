/*
using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("NPC 기본 정보")]
    public string npcName = "마을 주민";
    public string zoneName = "광장";

    [Header("대화 내용")]
    [TextArea(2, 5)]
    public string dialogueMessage = "안녕, 요즘 마을에 이상한 유행이 돌고 있어.";

    [Header("상호작용 안내 문구")]
    public string interactHintText = "E키로 대화";

    [Header("키워드 지급 설정")]
    public bool giveKeyword = true;
    public string keywordToGive = "차가운";
    public KeywordType keywordType = KeywordType.Style;

    [Header("소문 기록 설정")]
    public bool giveRumor = true;

    [TextArea(2, 5)]
    public string rumorText = "마을 사람들 사이에서 차가운 느낌의 물건 이야기가 자주 나온다.";

    public string relatedKeyword = "차가운";
    public bool isRareHint = false;

    [Header("유행 분석 정보")]
    [Range(0, 100)]
    public int reliability = 60;

    public int trendWeight = 1;

    public string rumorType = "NPC 소문";

    [Header("반복 대화 설정")]
    public bool giveOnlyOnce = true;

    private bool playerInRange = false;
    private bool hasGivenReward = false;

    private DialogueManager dialogueManager;
    private UIManager uiManager;
    private KeywordManager keywordManager;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        uiManager = FindObjectOfType<UIManager>();
        keywordManager = FindObjectOfType<KeywordManager>();
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (dialogueManager == null)
        {
            Debug.LogWarning("DialogueManager를 찾지 못했습니다.");
            return;
        }

        if (dialogueManager.IsDialogueOpen())
        {
            dialogueManager.CloseDialogue();
            return;
        }

        dialogueManager.OpenDialogue(dialogueMessage);

        if (giveOnlyOnce && hasGivenReward)
        {
            return;
        }

        GiveKeywordIfNeeded();
        GiveRumorIfNeeded();

        hasGivenReward = true;
    }

    private void GiveKeywordIfNeeded()
    {
        if (!giveKeyword) return;

        if (keywordManager == null)
        {
            Debug.LogWarning("KeywordManager를 찾지 못했습니다.");
            return;
        }

        var master = keywordManager.GetMasterKeywordByName(keywordToGive);

        if (master != null)
        {
            keywordManager.AddKeyword(master.serverId, master.keywordName, master.keywordType);
        }
        else
        {
            keywordManager.AddKeyword(0, keywordToGive, keywordType);
        }

        Debug.Log($"{npcName}에게서 키워드 획득: {keywordToGive}");
    }

    private void GiveRumorIfNeeded()
    {
        if (!giveRumor) return;

        if (RumorManager.Instance == null)
        {
            Debug.LogWarning("RumorManager가 씬에 없습니다. 소문은 기록되지 않습니다.");
            return;
        }

       RumorData rumor = new RumorData
    {
        rumorId = npcName + "_" + relatedKeyword,
        rumorText = rumorText,
        sourceNPC = npcName,
        zoneName = zoneName,
        relatedKeyword = relatedKeyword,
        isRareHint = isRareHint,
        reliability = reliability,
        trendWeight = trendWeight,
        rumorType = rumorType
    };

        RumorManager.Instance.AddRumor(rumor);

        Debug.Log($"{npcName}에게서 소문 획득: {rumorText}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (uiManager != null)
        {
            uiManager.ShowInteractHint(interactHintText);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (uiManager != null)
        {
            uiManager.HideInteractHint();
        }

        if (dialogueManager != null && dialogueManager.IsDialogueOpen())
        {
            dialogueManager.CloseDialogue();
        }
    }
}

*/

using System.Collections;
using UnityEngine;

/// <summary>
/// NPC 상호작용 스크립트.
/// 서버가 있으면 서버를 먼저 거치고, 서버가 없거나 실패하면 로컬 fallback 값으로 기존처럼 동작합니다.
/// </summary>
public class NPCInteraction : MonoBehaviour
{
    [Header("서버 연동")]
    [Tooltip("GET /explore/npcs 응답의 id와 맞춰주세요. 서버를 쓰지 않을 NPC는 0으로 둬도 됩니다.")]
    public int npcServerId = 0;

    [Tooltip("켜면 서버 데이터를 우선 사용합니다.")]
    public bool useServer = true;

    [Tooltip("서버가 없거나 서버 요청 실패 시 기존 인스펙터 값으로 로컬 지급/소문 기록을 진행합니다.")]
    public bool allowLocalFallback = true;

    [Header("NPC 기본 정보 - 로컬 fallback 값")]
    public string npcName = "마을 주민";
    public string zoneName = "광장";

    [Header("대화 내용 - 로컬 fallback 값")]
    [TextArea(2, 5)]
    public string dialogueMessage = "안녕, 요즘 마을에 이상한 유행이 돌고 있어.";

    [Header("상호작용 안내 문구")]
    public string interactHintText = "E키로 대화";

    [Header("키워드 지급 설정 - 로컬 fallback 값")]
    public bool giveKeyword = true;
    public string keywordToGive = "차가운";
    public KeywordType keywordType = KeywordType.Style;

    [Header("소문 기록 설정 - 로컬 fallback 값")]
    public bool giveRumor = true;

    [TextArea(2, 5)]
    public string rumorText = "마을 사람들 사이에서 차가운 느낌의 물건 이야기가 자주 나온다.";

    public string relatedKeyword = "차가운";
    public bool isRareHint = false;

    [Header("유행 분석 정보 - 로컬 fallback 값")]
    [Range(0, 100)]
    public int reliability = 60;
    public int trendWeight = 1;
    public string rumorType = "NPC 소문";

    [Header("반복 대화 설정")]
    public bool giveOnlyOnce = true;

    [Header("디버그")]
    public bool verboseLog = false;

    [Header("서버 주입 데이터 - 확인용")]
    [SerializeField] private string portraitId = "";
    [SerializeField] private bool isActive = true;
    [SerializeField] private bool talked = false;
    [SerializeField] private bool reliabilityKnown = false;
    [SerializeField] private int perceivedReliability = 0;

    private bool playerInRange = false;
    private bool isTalkingToServer = false;
    private bool hasLocalRewardGiven = false;
    private bool isCurrentDialogueNpc = false;

    private DialogueManager dialogueManager;
    private UIManager uiManager;
    private KeywordManager keywordManager;
    private NPCServerManager npcServerManager;

    private void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        uiManager = FindObjectOfType<UIManager>();
        keywordManager = FindObjectOfType<KeywordManager>();

        ResolveNpcServerManager();

        if (useServer && npcServerManager != null)
        {
            npcServerManager.RegisterNPC(this);

            if (verboseLog)
            {
                Debug.Log($"[NPCInteraction] 서버 매니저 등록 완료 / NPC={npcName}, npcServerId={npcServerId}");
            }
        }
        else if (useServer && npcServerManager == null)
        {
            Debug.LogWarning($"[NPCInteraction] NPCServerManager가 씬에 없습니다. NPC '{npcName}'은 로컬 fallback 값으로 동작합니다.");
        }

        if (useServer && npcServerId <= 0)
        {
            Debug.LogWarning(
                $"[NPCInteraction] NPC '{npcName}'의 npcServerId가 {npcServerId}입니다. " +
                $"이 상태에서는 서버 대화 요청을 보낼 수 없습니다. 서버 /explore/npcs 응답 id와 맞춰주세요."
            );
        }
    }

    private void OnDestroy()
    {
        if (npcServerManager != null)
        {
            npcServerManager.UnregisterNPC(this);
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void ResolveNpcServerManager()
    {
        if (npcServerManager != null)
            return;

        if (NPCServerManager.Instance != null)
        {
            npcServerManager = NPCServerManager.Instance;
            return;
        }

        npcServerManager = FindObjectOfType<NPCServerManager>();
    }

    public int GetNpcServerId()
    {
        return npcServerId;
    }

    public string GetNpcName()
    {
        return npcName;
    }

    public string GetLocation()
    {
        return zoneName;
    }

    public string GetPortraitId()
    {
        return portraitId;
    }

    public string GetReliabilityText()
    {
        if (useServer)
        {
            if (!reliabilityKnown)
                return "???";

            return perceivedReliability.ToString();
        }

        return reliability.ToString();
    }

    public void ApplyServerData(ServerNpcData data)
    {
        if (data == null)
            return;

        // 서버 id가 Cloudflare DB에서 밀려 있을 수 있으므로, 서버 데이터가 적용되는 순간
        // Unity NPC의 npcServerId도 서버 id로 보정한다.
        if (data.id > 0 && npcServerId != data.id)
        {
            Debug.LogWarning($"[NPCInteraction] npcServerId 자동 보정: {npcServerId} → {data.id} / NPC={data.name}");
            npcServerId = data.id;
        }

        if (!string.IsNullOrEmpty(data.name))
        {
            npcName = data.name;
        }

        if (!string.IsNullOrEmpty(data.location))
        {
            zoneName = data.location;
        }

        if (!string.IsNullOrEmpty(data.portrait_id))
        {
            portraitId = data.portrait_id;
        }

        string serverDialogue = GetServerDialogueFromNpcData(data);

        if (!string.IsNullOrEmpty(serverDialogue))
        {
            dialogueMessage = serverDialogue;
        }
        else
        {
            Debug.LogWarning(
                $"[NPCInteraction] 서버 NPC 대화문이 비어 있습니다. " +
                $"id={data.id}, name={data.name}, 현재 대사 유지={dialogueMessage}"
            );
        }

        isActive = data.is_active;
        talked = data.talked;

        reliabilityKnown = data.perceived_reliability.HasValue;
        perceivedReliability = reliabilityKnown ? data.perceived_reliability.Value : 0;

        gameObject.SetActive(isActive);

        if (verboseLog)
        {
            Debug.Log(
                $"[NPCInteraction] NPC 서버 데이터 적용 완료: " +
                $"id={data.id}, name={npcName}, talked={talked}, reliability={GetReliabilityText()}"
            );
        }

        RefreshOpenedDialogue();
    }

    private string GetServerDialogueFromNpcData(ServerNpcData data)
    {
        if (data == null)
            return "";

        if (!string.IsNullOrEmpty(data.season_dialogue))
            return data.season_dialogue;

        if (!string.IsNullOrEmpty(data.dialogue))
            return data.dialogue;

        if (!string.IsNullOrEmpty(data.message))
            return data.message;

        if (!string.IsNullOrEmpty(data.talk_text))
            return data.talk_text;

        if (!string.IsNullOrEmpty(data.line))
            return data.line;

        return "";
    }

    private void TryInteract()
    {
        if (!isActive)
            return;

        if (dialogueManager == null)
        {
            Debug.LogWarning("[NPCInteraction] DialogueManager를 찾지 못했습니다.");
            return;
        }

        if (dialogueManager.IsDialogueOpen())
        {
            dialogueManager.CloseDialogue();
            isCurrentDialogueNpc = false;
            return;
        }

        ResolveNpcServerManager();

        if (verboseLog) Debug.Log($"[NPCInteraction] 대화창 열기 / npcName={npcName}, npcServerId={npcServerId}, reliability={GetReliabilityText()}");
        
        isCurrentDialogueNpc = true;
        
        dialogueManager.OpenDialogue(GetDialogueText());

        if (giveOnlyOnce && IsRewardAlreadyGiven())
        {
            Debug.Log($"[NPCInteraction] 이미 대화 보상 지급 완료: {npcName}");
            return;
        }

        if (isTalkingToServer)
            return;

        StartCoroutine(ProcessTalkReward());
    }

    private string GetDialogueText()
    {
        return $"{npcName}\n{dialogueMessage}\n\n신뢰도: {GetReliabilityText()}";
    }

    private bool IsRewardAlreadyGiven()
    {
        // 서버 연동 중에는 /explore/npcs의 talked 값만 보고 Unity에서 차단하지 않는다.
        // 현재 서버는 TALK 보상/드랍을 /explore/action에서 판단하므로,
        // 클라이언트가 먼저 막으면 keyword_id를 받을 기회 자체가 사라진다.
        if (useServer)
            return false;

        return hasLocalRewardGiven;
    }

    private void RefreshOpenedDialogue()
    {
        if (dialogueManager == null)
            return;

        if (!dialogueManager.IsDialogueOpen())
            return;

        // 중요:
        // RefreshNpcList()는 모든 NPC에 ApplyServerData()를 호출한다.
        // 그래서 현재 대화 중인 NPC가 아닌 다른 NPC가 대화창을 덮어쓰지 못하게 막는다.
        if (!isCurrentDialogueNpc)
            return;

        if (!playerInRange)
            return;

        dialogueManager.CloseDialogue();
        dialogueManager.OpenDialogue(GetDialogueText());

        if (verboseLog) Debug.Log($"[NPCInteraction] 현재 대화 NPC UI 갱신 완료 / npcName={npcName}, npcServerId={npcServerId}");
    }

    private IEnumerator ProcessTalkReward()
    {
        isTalkingToServer = true;

        if (verboseLog)
        {
            Debug.Log(
                $"[NPCInteraction] 대화 보상 처리 시작\n" +
                $"NPC: {npcName}\n" +
                $"useServer: {useServer}\n" +
                $"npcServerId: {npcServerId}\n" +
                $"npcServerManager 존재: {npcServerManager != null}\n" +
                $"allowLocalFallback: {allowLocalFallback}"
            );
        }

        bool serverSuccess = false;

        ResolveNpcServerManager();

        string serverBlockReason = GetServerTalkBlockReason();

        if (string.IsNullOrEmpty(serverBlockReason))
        {
            ServerNpcTalkResponse response = null;

            if (verboseLog) Debug.Log($"[NPCInteraction] 서버 대화 시도 / npcServerId={npcServerId}, npcName={npcName}");

            yield return StartCoroutine(npcServerManager.TalkToNpc(
                npcServerId,
                result =>
                {
                    response = result;
                }
            ));

            serverSuccess = response != null && response.status == "success";

            if (serverSuccess)
            {
                if (verboseLog) Debug.Log($"[NPCInteraction] 서버 대화 성공 / npcName={npcName}");

                ApplyServerTalkResponse(response);

                if (ExploreServerManager.Instance != null)
                {
                    yield return StartCoroutine(ExploreServerManager.Instance.SyncAfterNpcTalk());
                }
                else
                {
                    yield return StartCoroutine(npcServerManager.RefreshNpcList());
                }
            }
            else
            {
                Debug.LogWarning($"[NPCInteraction] NPC 대화 서버 처리 실패 / npcServerId={npcServerId}, npcName={npcName}");
            }
        }
        else
        {
            Debug.LogWarning($"[NPCInteraction] 서버 대화 시도 불가: {serverBlockReason} / npcName={npcName}");
        }

        if (!serverSuccess)
        {
            if (allowLocalFallback)
            {
                Debug.LogWarning($"서버 대화 실패 또는 서버 미사용 → 로컬 fallback으로 처리합니다: {npcName}");
                GiveKeywordLocallyIfNeeded();
                GiveRumorLocallyIfNeeded();
                hasLocalRewardGiven = true;
            }
            else
            {
                Debug.LogWarning($"서버 대화 실패. allowLocalFallback이 꺼져 있어 보상 지급 안 함: {npcName}");
            }
        }

        isTalkingToServer = false;
    }

    private string GetServerTalkBlockReason()
    {
        if (!useServer)
            return "useServer가 꺼져 있음";

        if (npcServerId <= 0)
            return "npcServerId가 0 이하임. 서버 NPC id를 인스펙터에 넣어야 함";

        if (npcServerManager == null)
            return "NPCServerManager를 찾지 못함";

        return "";
    }

    private string GetServerDialogueFromTalkData(ServerNpcTalkData data)
    {
        if (data == null)
            return "";

        if (!string.IsNullOrEmpty(data.dialogue))
            return data.dialogue;

        if (!string.IsNullOrEmpty(data.message))
            return data.message;

        if (!string.IsNullOrEmpty(data.talk_text))
            return data.talk_text;

        if (!string.IsNullOrEmpty(data.line))
            return data.line;

        if (!string.IsNullOrEmpty(data.season_dialogue))
            return data.season_dialogue;

        return "";
    }

    private void ApplyServerTalkResponse(ServerNpcTalkResponse response)
    {
        if (response == null)
        {
            Debug.LogWarning("[NPCInteraction] 서버 NPC 대화 응답 response 자체가 null입니다.");
            return;
        }

        if (response.data == null)
        {
            Debug.LogWarning("[NPCInteraction] 서버 NPC 대화 응답 data가 null입니다.");
            return;
        }

        if (verboseLog)
        {
            Debug.Log(
                $"[NPCInteraction] 서버 talk 응답 확인 / status={response.status}, success={response.data.success}, " +
                $"keyword_id={response.data.keyword_id}, keyword_name={response.data.keyword_name}"
            );
        }

        if (!response.data.success)
        {
            Debug.LogWarning("[NPCInteraction] 서버 대화 data.success=false 입니다. message/warning을 UI에 표시하고 키워드 지급은 응답에 있는 경우만 반영합니다. warning=" + response.data.warning);
        }

        // 서버 message가 실제 대사다. /explore/action은 talked를 명확히 주지 않을 수 있으므로
        // talked 값은 UI 차단용으로 쓰지 않는다.
        string talkDialogue = GetServerDialogueFromTalkData(response.data);

        if (!string.IsNullOrEmpty(talkDialogue))
        {
            dialogueMessage = talkDialogue;
            if (verboseLog) Debug.Log($"[NPCInteraction] 서버 talk 응답 대화문 적용: {dialogueMessage}");
        }
        else
        {
            if (verboseLog) Debug.Log("[NPCInteraction] 서버 talk 응답에 대화문이 없습니다. 기존 대사를 유지합니다.");
        }

        // 서버 대화 내용도 소문 로그로 남긴다. 서버에는 별도 rumor API가 없으므로 Unity RumorManager에 기록한다.
        GiveRumorFromServerTalk(response.data);

        ServerGrantedKeyword granted = ExtractGrantedKeyword(response.data);

        if (granted != null)
        {
            AddKeywordFromServer(granted);
        }
        else
        {
            if (verboseLog)
            {
                Debug.Log("[NPCInteraction] 서버 대화 성공, 키워드 드랍 없음. 서버 drop_rate 확률상 정상일 수 있습니다.");
            }

            // 서버 대화는 성공했지만 키워드 드랍만 실패한 경우다.
            // 여기서 로컬 키워드를 지급하면 서버 inventory/frequency와 Unity가 어긋날 수 있어서 기본은 지급하지 않는다.
            // 테스트만 빨리 하고 싶으면 아래 두 줄을 임시로 켠다.
            // GiveKeywordLocallyIfNeeded();
            // hasLocalRewardGiven = true;
        }

        if (verboseLog) Debug.Log($"[NPCInteraction] 서버 NPC 대화 처리 완료: {npcName}");

        RefreshOpenedDialogue();
    }

    private ServerGrantedKeyword ExtractGrantedKeyword(ServerNpcTalkData data)
    {
        if (data == null)
            return null;

        if (data.granted_keyword != null)
            return data.granted_keyword;

        if (data.keyword != null)
            return data.keyword;

        if (data.reward_keyword != null)
            return data.reward_keyword;

        if (data.keyword_id > 0 && !string.IsNullOrEmpty(data.keyword_name))
        {
            ServerGrantedKeyword keyword = new ServerGrantedKeyword();
            keyword.id = data.keyword_id;
            keyword.name = data.keyword_name;
            keyword.rarity = data.keyword_rarity;

            if (!string.IsNullOrEmpty(data.keyword_type))
                keyword.keyword_type = data.keyword_type;
            else if (!string.IsNullOrEmpty(data.category))
                keyword.keyword_type = data.category;

            return keyword;
        }

        return null;
    }

    private void GiveRumorFromServerTalk(ServerNpcTalkData data)
    {
        if (!giveRumor)
            return;

        if (RumorManager.Instance == null)
            return;

        string text = GetServerDialogueFromTalkData(data);
        if (string.IsNullOrEmpty(text))
            text = dialogueMessage;

        string keywordName = data != null && !string.IsNullOrEmpty(data.keyword_name)
            ? data.keyword_name
            : relatedKeyword;

        RumorData rumor = new RumorData
        {
            rumorId = npcName + "_server_" + keywordName + "_" + Time.frameCount,
            rumorText = text,
            sourceNPC = npcName,
            zoneName = zoneName,
            relatedKeyword = keywordName,
            isRareHint = data != null && data.keyword_rarity == "RARE",
            reliability = reliabilityKnown ? perceivedReliability : reliability,
            trendWeight = trendWeight,
            rumorType = "서버 NPC 대화"
        };

        RumorManager.Instance.AddRumor(rumor);
        if (verboseLog) Debug.Log($"[NPCInteraction] 서버 대화 소문 기록: {text}");
    }

    private void AddKeywordFromServer(ServerGrantedKeyword granted)
    {
        if (keywordManager == null)
        {
            Debug.LogWarning("[NPCInteraction] KeywordManager를 찾지 못했습니다.");
            return;
        }

        if (granted == null)
            return;

        if (string.IsNullOrEmpty(granted.name))
        {
            Debug.LogWarning("[NPCInteraction] 서버 키워드 name이 비어 있어 키워드 동기화를 건너뜁니다.");
            return;
        }

        KeywordType parsedType = keywordType;

        // 1순위: KeywordManager의 마스터 키워드에서 서버 id로 정확히 찾기
        KeywordData master = keywordManager.GetMasterKeyword(granted.id);
        if (master != null)
        {
            parsedType = master.keywordType;
        }
        else
        {
            string typeText = granted.keyword_type;
            if (string.IsNullOrEmpty(typeText))
                typeText = granted.category;

            if (!string.IsNullOrEmpty(typeText))
                System.Enum.TryParse(typeText, true, out parsedType);
        }

        Debug.Log($"[서버 keyword] id={granted.id} name='{granted.name}' type={parsedType} rarity={granted.rarity}");

        keywordManager.AddKeyword(granted.id, granted.name, parsedType);
        if (verboseLog) Debug.Log($"[NPCInteraction] 서버 확정 키워드 동기화: {granted.name}");
    }

    private void GiveKeywordLocallyIfNeeded()
    {
        if (!giveKeyword)
            return;

        if (keywordManager == null)
        {
            Debug.LogWarning("[NPCInteraction] KeywordManager를 찾지 못했습니다.");
            return;
        }

        var master = keywordManager.GetMasterKeywordByName(keywordToGive);

        if (master != null)
        {
            keywordManager.AddKeyword(master.serverId, master.keywordName, master.keywordType);
        }
        else
        {
            keywordManager.AddKeyword(0, keywordToGive, keywordType);
        }

        Debug.Log($"로컬 키워드 획득: {keywordToGive}");
    }

    private void GiveRumorLocallyIfNeeded()
    {
        if (!giveRumor)
            return;

        if (RumorManager.Instance == null)
        {
            Debug.LogWarning("[NPCInteraction] RumorManager가 씬에 없습니다. 소문은 기록되지 않습니다.");
            return;
        }

        RumorData rumor = new RumorData
        {
            rumorId = npcName + "_" + relatedKeyword,
            rumorText = rumorText,
            sourceNPC = npcName,
            zoneName = zoneName,
            relatedKeyword = relatedKeyword,
            isRareHint = isRareHint,
            reliability = reliability,
            trendWeight = trendWeight,
            rumorType = rumorType
        };

        RumorManager.Instance.AddRumor(rumor);
        Debug.Log($"로컬 소문 획득: {rumorText}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (uiManager != null)
        {
            uiManager.ShowInteractHint(interactHintText);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        isCurrentDialogueNpc = false;

        if (uiManager != null)
        {
            uiManager.HideInteractHint();
        }

        if (dialogueManager != null && dialogueManager.IsDialogueOpen())
        {
            dialogueManager.CloseDialogue();
        }
    }
}