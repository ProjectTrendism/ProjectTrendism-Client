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
/// 
/// 핵심 흐름:
/// 1. Play 시작 시 GET /explore/npcs 응답이 있으면 NPC 이름/지역/시즌대사/신뢰도/talked를 서버값으로 주입
/// 2. NPC와 대화하면 POST /explore/npcs/{id}/talk 먼저 시도
/// 3. 서버 성공 시 서버가 내려준 granted_keyword만 로컬 KeywordManager에 동기화
/// 4. 서버 실패 시 allowLocalFallback이 켜져 있으면 기존 인스펙터 값으로 키워드/소문 지급
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

    [Header("서버 주입 데이터 - 확인용")]
    [SerializeField] private string portraitId = "";
    [SerializeField] private bool isActive = true;
    [SerializeField] private bool talked = false;
    [SerializeField] private bool reliabilityKnown = false;
    [SerializeField] private int perceivedReliability = 0;

    private bool playerInRange = false;
    private bool isTalkingToServer = false;
    private bool hasLocalRewardGiven = false;

    private DialogueManager dialogueManager;
    private UIManager uiManager;
    private KeywordManager keywordManager;
    private NPCServerManager npcServerManager;

    private void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        uiManager = FindObjectOfType<UIManager>();
        keywordManager = FindObjectOfType<KeywordManager>();
        npcServerManager = FindObjectOfType<NPCServerManager>();

        if (useServer && npcServerManager != null)
        {
            npcServerManager.RegisterNPC(this);
        }
        else if (useServer && npcServerManager == null)
        {
            Debug.LogWarning($"NPCServerManager가 씬에 없습니다. NPC '{npcName}'은 로컬 fallback 값으로 동작합니다.");
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
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
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
            if (!reliabilityKnown) return "???";
            return perceivedReliability.ToString();
        }

        return reliability.ToString();
    }

    /// <summary>
    /// NPCServerManager가 GET /explore/npcs 응답을 받은 뒤 호출합니다.
    /// 여기서 인스펙터 값을 서버 값으로 덮어씁니다.
    /// </summary>
    public void ApplyServerData(ServerNpcData data)
    {
        if (data == null) return;

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

        if (!string.IsNullOrEmpty(data.season_dialogue))
        {
            dialogueMessage = data.season_dialogue;
        }

        isActive = data.is_active;
        talked = data.talked;

        reliabilityKnown = data.perceived_reliability.HasValue;
        perceivedReliability = reliabilityKnown ? data.perceived_reliability.Value : 0;

        gameObject.SetActive(isActive);

        Debug.Log($"NPC 서버 데이터 적용 완료: id={data.id}, name={npcName}, talked={talked}, reliability={GetReliabilityText()}");
    }

    private void TryInteract()
    {
        if (!isActive) return;

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

        dialogueManager.OpenDialogue(GetDialogueText());

        if (giveOnlyOnce && IsRewardAlreadyGiven())
        {
            Debug.Log($"이미 대화 보상 지급 완료: {npcName}");
            return;
        }

        if (isTalkingToServer) return;

        StartCoroutine(ProcessTalkReward());
    }

    private string GetDialogueText()
    {
        return $"{npcName}\n{dialogueMessage}\n\n신뢰도: {GetReliabilityText()}";
    }

    private bool IsRewardAlreadyGiven()
    {
        // 서버 모드에서는 서버의 talked 값을 기준으로 보고,
        // 로컬 fallback 모드에서는 hasLocalRewardGiven 값을 기준으로 봅니다.
        if (useServer && talked) return true;
        if (hasLocalRewardGiven) return true;
        return false;
    }

    private IEnumerator ProcessTalkReward()
    {
        isTalkingToServer = true;

        bool serverSuccess = false;

        if (CanUseServerTalk())
        {
            ServerNpcTalkResponse response = null;

            yield return npcServerManager.TalkToNpc(
                npcServerId,
                result =>
                {
                    response = result;
                }
            );

            serverSuccess = response != null && response.status == "success";

            if (serverSuccess)
            {
                ApplyServerTalkResponse(response);

                // 서버에서 talked / reliability / season_dialogue가 바뀔 수 있으니 최신 목록을 다시 받습니다.
                yield return npcServerManager.RefreshNpcList();
            }
            else
            {
                Debug.LogWarning($"NPC 대화 서버 처리 실패: npcServerId={npcServerId}");
            }
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

    private bool CanUseServerTalk()
    {
        if (!useServer) return false;
        if (npcServerId <= 0) return false;
        if (npcServerManager == null) return false;
        return true;
    }

    private void ApplyServerTalkResponse(ServerNpcTalkResponse response)
    {
        if (response == null || response.data == null)
        {
            Debug.LogWarning("서버 NPC 대화 응답 data가 비어 있습니다.");
            return;
        }

        talked = response.data.talked;

        // 서버가 확정해서 내려준 키워드만 로컬 KeywordManager에 동기화합니다.
        // 서버 성공 시에는 NPCInteraction이 임의로 로컬 키워드를 생성하지 않습니다.
        if (response.data.granted_keyword != null)
        {
            AddKeywordFromServer(response.data.granted_keyword);
        }

        Debug.Log($"서버 NPC 대화 처리 완료: {npcName}");
    }

    private void AddKeywordFromServer(ServerGrantedKeyword granted)
    {
        if (keywordManager == null)
        {
            Debug.LogWarning("KeywordManager를 찾지 못했습니다.");
            return;
        }

        KeywordType parsedType = keywordType;

        if (!string.IsNullOrEmpty(granted.keyword_type))
        {
            System.Enum.TryParse(granted.keyword_type, true, out parsedType);
        }

        if (string.IsNullOrEmpty(granted.name))
        {
            Debug.LogWarning("서버 granted_keyword.name이 비어 있어 키워드 동기화를 건너뜁니다.");
            return;
        }

        keywordManager.AddKeyword(granted.id, granted.name, parsedType);
        Debug.Log($"서버 확정 키워드 동기화: {granted.name}");
    }

    private void GiveKeywordLocallyIfNeeded()
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

        Debug.Log($"로컬 키워드 획득: {keywordToGive}");
    }

    private void GiveRumorLocallyIfNeeded()
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
        Debug.Log($"로컬 소문 획득: {rumorText}");
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
