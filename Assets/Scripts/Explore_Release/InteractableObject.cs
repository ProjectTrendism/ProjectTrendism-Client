using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public enum InteractType
    {
        NPC,
        Animal,
        Plant,
        Board,
        Hidden,
        Item
    }

    [Header("기본 설정")]
    public InteractType interactType = InteractType.NPC;
    public string objectName = "오브젝트";

    [TextArea(2, 5)]
    public string interactionMessage = "상호작용했습니다.";

    [Header("키워드 보상")]
    public bool giveKeyword = false;
    public string keywordToGive = "차가운";
    public KeywordType keywordType = KeywordType.Style;

    [Header("소문 기록")]
    public bool giveRumor = true;

    [TextArea(2, 5)]
    public string rumorText = "마을에 새로운 소문이 퍼지고 있다.";

    public string relatedKeyword = "차가운";
    public bool isRareHint = false;

    [Header("유행 분석 정보")]
    [Range(0, 100)]
    public int reliability = 50;

    public int trendWeight = 1;

    public string rumorType = "오브젝트 소문";

    [Header("반복 설정")]
    public bool interactOnlyOnce = false;

    private bool hasInteracted = false;
    private bool playerInRange = false;

    private DialogueManager dialogueManager;
    private UIManager uiManager;
    private KeywordManager keywordManager;

    private void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        uiManager = FindObjectOfType<UIManager>();
        keywordManager = FindObjectOfType<KeywordManager>();
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (interactOnlyOnce && hasInteracted)
        {
            if (dialogueManager != null)
            {
                dialogueManager.OpenDialogue(objectName + "은(는) 더 이상 특별한 반응이 없다.");
            }
            return;
        }

        ShowMessage();

        if (giveKeyword)
        {
            GiveKeyword();
        }

        if (giveRumor)
        {   
        GiveRumor();
        }

        hasInteracted = true;
    }

    private void ShowMessage()
    {
        if (dialogueManager == null)
        {
            Debug.LogWarning("DialogueManager를 찾지 못했습니다.");
            return;
        }

        string finalMessage = "";

        if (interactType == InteractType.NPC)
        {
            finalMessage = interactionMessage;
        }
        else if (interactType == InteractType.Animal)
        {
            finalMessage = "[" + objectName + "]\n" + interactionMessage;
        }
        else if (interactType == InteractType.Plant)
        {
            finalMessage = "식물을 조사했다.\n" + interactionMessage;
        }
        else if (interactType == InteractType.Board)
        {
            finalMessage = "게시판을 확인했다.\n" + interactionMessage;
        }
        else if (interactType == InteractType.Hidden)
        {
            finalMessage = "숨겨진 흔적 발견!\n" + interactionMessage;
        }
        else if (interactType == InteractType.Item)
        {
            finalMessage = "아이템을 조사했다.\n" + interactionMessage;
        }

        dialogueManager.OpenDialogue(finalMessage);
    }

    private void GiveKeyword()
    {
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

        Debug.Log("키워드 획득: " + keywordToGive);
    }

    private void GiveRumor()
    {
        if (RumorManager.Instance == null)
        {
            Debug.LogWarning("RumorManager가 씬에 없습니다.");
            return;
        }

        RumorData rumor = new RumorData
    {
        rumorId = objectName + "_" + relatedKeyword,
        rumorText = rumorText,
        sourceNPC = objectName,
        zoneName = interactType.ToString(),
        relatedKeyword = relatedKeyword,
        isRareHint = isRareHint,
        reliability = reliability,
        trendWeight = trendWeight,
        rumorType = rumorType
    };

        RumorManager.Instance.AddRumor(rumor);

        Debug.Log("소문 기록: " + rumorText);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (uiManager != null)
        {
            uiManager.ShowInteractHint("E키 상호작용");
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