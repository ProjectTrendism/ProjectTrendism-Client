using UnityEngine;

public class HiddenInteractable : MonoBehaviour
{
    public float hiddenAlpha = 0.08f;
    public float revealedAlpha = 1f;
    public float revealDuration = 2f;

    public GameObject highlightObject;

    private SpriteRenderer spriteRenderer;
    private float revealTimer = 0f;
    private bool isRevealed = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetAlpha(hiddenAlpha);

        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    void Update()
    {
        if (isRevealed)
        {
            revealTimer -= Time.deltaTime;

            if (revealTimer <= 0f)
            {
                HideAgain();
            }
        }
    }

    public void Reveal()
    {
        isRevealed = true;
        revealTimer = revealDuration;

        SetAlpha(revealedAlpha);

        if (highlightObject != null)
            highlightObject.SetActive(true);
    }

    void HideAgain()
    {
        isRevealed = false;
        SetAlpha(hiddenAlpha);

        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    void SetAlpha(float alpha)
    {
        if (spriteRenderer == null) return;

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}