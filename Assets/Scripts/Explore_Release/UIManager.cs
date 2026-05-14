using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameObject interactHint;
    public TextMeshProUGUI interactHintText;

    public void ShowInteractHint(string message)
    {
        if (interactHint != null)
            interactHint.SetActive(true);

        if (interactHintText != null)
            interactHintText.text = message;
    }

    public void HideInteractHint()
    {
        if (interactHint != null)
            interactHint.SetActive(false);
    }
}