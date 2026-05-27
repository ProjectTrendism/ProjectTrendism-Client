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
/// NPC 오브젝트에 붙이는 상호작용 스크립트.
/// 인스펙터에는 npcServerId만 직접 설정하고,
/// 이름/대사/신뢰도/초상화/대화 여부는 서버 GET /explore/npcs 응답으로 런타임 주입한다.
/// </summary>
public class NPCInteraction : MonoBehaviour
{
    [Header("서버 NPC ID만 설정")]
    public int npcServerId = 0;

    [Header("상호작용 안내 문구")]
    public string interactHintText = "E키로 대화";

    [Header("반복 대화 설정")]
    public bool giveOnlyOnce = true;

    [Header("서버 주입 데이터 - 확인용")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private string location = "";
    [SerializeField, TextArea(2, 5)] private string seasonDialogue = "대화 정보를 불러오는 중입니다.";
    [SerializeField] private string portraitId = "";
    [SerializeField] private bool isActive = true;
    [SerializeField] private bool talked = false;
    [SerializeField] private bool reliabilityKnown = false;
    [SerializeField] private int perceivedReliability = 0;

    private bool playerInRange = false;
    private bool isTalkingToServer = false;

    private DialogueManager dialogueManager;
    private UIManager uiManager;
    private NPCServerManager npcServerManager;

    private void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        uiManager = FindObjectOfType<UIManager>();
        npcServerManager = FindObjectOfType<NPCServerManager>();

        if (npcServerManager == null)
        {
            Debug.LogWarning("NPCServerManager가 씬에 없습니다. 서버 NPC 데이터 주입이 불가능합니다.");
            return;
        }

        npcServerManager.RegisterNPC(this);
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
        if (!Input.GetKeyDown(KeyCode.E)) return;

        TryInteract();
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
        return location;
    }

    public string GetPortraitId()
    {
        return portraitId;
    }

    public string GetReliabilityText()
    {
        if (!reliabilityKnown) return "???";
        return perceivedReliability.ToString();
    }

    public void ApplyServerData(ServerNpcData data)
    {
        if (data == null) return;

        npcName = data.name;
        location = data.location;
        portraitId = data.portrait_id;
        isActive = data.is_active;
        talked = data.talked;

        seasonDialogue = string.IsNullOrEmpty(data.season_dialogue)
            ? "..."
            : data.season_dialogue;

        reliabilityKnown = data.perceived_reliability.HasValue;
        perceivedReliability = reliabilityKnown ? data.perceived_reliability.Value : 0;

        gameObject.SetActive(isActive);
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

        string reliabilityText = GetReliabilityText();
        dialogueManager.OpenDialogue($"{npcName}\n{seasonDialogue}\n\n신뢰도: {reliabilityText}");

        if (giveOnlyOnce && talked)
        {
            return;
        }

        if (isTalkingToServer)
        {
            return;
        }

        if (npcServerManager == null)
        {
            Debug.LogWarning("NPCServerManager가 없어 NPC 대화 서버 처리를 할 수 없습니다.");
            return;
        }

        StartCoroutine(TalkToServer());
    }

    private IEnumerator TalkToServer()
    {
        isTalkingToServer = true;

        bool finished = false;
        bool success = false;
        ServerNpcTalkResponse response = null;

        yield return npcServerManager.TalkToNpc(
            npcServerId,
            result =>
            {
                success = result != null && result.status == "success";
                response = result;
                finished = true;
            }
        );

        while (!finished)
        {
            yield return null;
        }

        isTalkingToServer = false;

        if (!success)
        {
            Debug.LogWarning($"NPC 대화 서버 처리 실패: npcServerId={npcServerId}");
            return;
        }

        talked = true;

        // 서버가 확정해서 내려준 키워드만 로컬 KeywordManager에 동기화한다.
        // 즉, NPCInteraction이 임의로 키워드를 직접 생성하지 않는다.
        if (response != null && response.data != null && response.data.granted_keyword != null)
        {
            KeywordManager keywordManager = FindObjectOfType<KeywordManager>();
            if (keywordManager != null)
            {
                ServerGrantedKeyword granted = response.data.granted_keyword;
                KeywordType parsedType = KeywordType.Style;

                if (!string.IsNullOrEmpty(granted.keyword_type))
                {
                    System.Enum.TryParse(granted.keyword_type, true, out parsedType);
                }

                keywordManager.AddKeyword(granted.id, granted.name, parsedType);
                Debug.Log($"서버 확정 키워드 동기화: {granted.name}");
            }
        }

        // 서버에서 신뢰도/대화여부가 바뀔 수 있으니 다시 받아서 화면 상태를 최신화한다.
        yield return npcServerManager.RefreshNpcList();
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
