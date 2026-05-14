using UnityEngine;

public class EventNPCInteraction : MonoBehaviour
{
    [TextArea]
    public string dialogueMessage = "도심에서 막 도착했어요. 요즘 상류층은 럭셔리한 분위기를 찾고 있답니다.";

    public string keywordToGive = "럭셔리";
    public KeywordType keywordType = KeywordType.Concept;

    public bool disappearAfterTalk = true;

    [Header("소문 기록 설정")]
    public bool giveRumor = true;

    [TextArea(2, 5)]
    public string rumorText = "도심에서 온 손님들이 럭셔리한 물건을 찾고 있다.";

    public string relatedKeyword = "럭셔리";
    public bool isRareHint = false;

    [Header("유행 분석 정보")]
    [Range(0, 100)]
    public int reliability = 80;

    public int trendWeight = 2;

    public string rumorType = "이벤트 NPC 소문";

    private bool playerInRange = false;
    private bool hasGivenKeyword = false;

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
            if (dialogueManager != null)
            {
                dialogueManager.OpenDialogue(dialogueMessage);
            }

            if (!hasGivenKeyword && keywordManager != null)
            {
                /*KeywordManager.Instance.AddKeyword(0, "키워드이름", KeywordType.Base);*/
                var master = keywordManager.GetMasterKeywordByName(keywordToGive);
                if (master != null)
                    keywordManager.AddKeyword(master.serverId, master.keywordName, master.keywordType);
                else
                    keywordManager.AddKeyword(0, keywordToGive, keywordType);
            hasGivenKeyword = true;
            }

            if (giveRumor && RumorManager.Instance != null)
            {
                RumorData rumor = new RumorData
                {
                    rumorId = gameObject.name + "_" + relatedKeyword,
                    rumorText = rumorText,
                    sourceNPC = gameObject.name,
                    zoneName = "Event",
                    relatedKeyword = relatedKeyword,
                    isRareHint = isRareHint,
                    reliability = reliability,
                    trendWeight = trendWeight,
                    rumorType = rumorType
                };

                RumorManager.Instance.AddRumor(rumor);
            }

            if (uiManager != null)
            {
                uiManager.HideInteractHint();
            }

            if (disappearAfterTalk)
            {
                Invoke(nameof(HideEventNPC), 1.0f);
            }
        }
    }

    void HideEventNPC()
    {
        transform.root.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (uiManager != null)
            {
                uiManager.ShowInteractHint("E키로 대화");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (uiManager != null)
            {
                uiManager.HideInteractHint();
            }
        }
    }
}