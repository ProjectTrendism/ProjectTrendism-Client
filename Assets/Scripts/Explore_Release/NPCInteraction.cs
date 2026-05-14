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