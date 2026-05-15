using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    private bool isDialogueOpen = false;

    public bool IsDialogueOpen()
    {
        return isDialogueOpen;
    }

    public void OpenDialogue(string message)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = message;
        isDialogueOpen = true;
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        isDialogueOpen = false;
    }
}