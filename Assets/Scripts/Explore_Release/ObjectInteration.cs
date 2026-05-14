using UnityEngine;

public class ObjectInteraction : MonoBehaviour
{
    [TextArea]
    public string description = "버섯이다. 신선해 보인다.";

    public string keywordToGive = "엘프 버섯";
    public KeywordType keywordType = KeywordType.Base;

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
            dialogueManager.OpenDialogue(description);

            if (!hasGivenKeyword)
            {
                /*KeywordManager.Instance.AddKeyword(0, "키워드이름", KeywordType.Base);*/
                var master = keywordManager.GetMasterKeywordByName(keywordToGive);
                if (master != null)
                    keywordManager.AddKeyword(master.serverId, master.keywordName, master.keywordType);
                else
                    keywordManager.AddKeyword(0, keywordToGive, keywordType);

            hasGivenKeyword = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            uiManager.ShowInteractHint("E키로 조사");
         }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            uiManager.HideInteractHint();

            if (dialogueManager.IsDialogueOpen())
            {
                dialogueManager.CloseDialogue();
            }
        }
    }
}