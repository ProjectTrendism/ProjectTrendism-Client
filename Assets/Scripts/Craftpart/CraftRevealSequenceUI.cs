using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftRevealSequenceUI : MonoBehaviour
{
    [Header("루트")]
    public GameObject root;
    public CanvasGroup rootCanvasGroup;

    [Header("키워드 1")]
    public RectTransform keyword1Rect;
    public CanvasGroup keyword1Group;
    public TextMeshProUGUI keyword1Text;

    [Header("키워드 2")]
    public RectTransform keyword2Rect;
    public CanvasGroup keyword2Group;
    public TextMeshProUGUI keyword2Text;

    [Header("키워드 3")]
    public RectTransform keyword3Rect;
    public CanvasGroup keyword3Group;
    public TextMeshProUGUI keyword3Text;

    [Header("합성 효과")]
    public RectTransform fusionGlowRect;
    public Image flashImage;
    public ParticleSystem[] burstEffects;

    [Header("FX Anchor")]
    public RectTransform fxAnchor;

    [Header("Cartoon FX Prefabs")]
    public GameObject chargeFxPrefab;
    public GameObject burstFxPrefab;
    public GameObject revealFxPrefab;

    public float fxDestroyDelay = 3f;

    [Header("결과 카드")]
    public CanvasGroup revealCardGroup;
    public RectTransform revealCardRect;
    public Image resultImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI revealHintText;

    [Header("기본 이미지")]
    public Sprite defaultResultSprite;

    [Header("연출 시간")]
    public float keywordAppearTime = 0.22f;
    public float keywordInterval = 0.15f;
    public float fusionMoveTime = 0.35f;
    public float flashTime = 0.22f;
    public float revealHoldTime = 1.2f;
    public float endFadeTime = 0.2f;

    private Vector2 keyword1StartPos;
    private Vector2 keyword2StartPos;
    private Vector2 keyword3StartPos;

    private Action onSequenceFinished;

    private void Awake()
    {
        SaveStartPositions();
        HideInstant();
    }
    private void HideInstant()
    {
        if (root != null)
            root.SetActive(true);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }
    }

    private void SaveStartPositions()
    {
        if (keyword1Rect != null)
            keyword1StartPos = keyword1Rect.anchoredPosition;

        if (keyword2Rect != null)
            keyword2StartPos = keyword2Rect.anchoredPosition;

        if (keyword3Rect != null)
            keyword3StartPos = keyword3Rect.anchoredPosition;
    }

    public void Play(CraftedItemResult result, Action onFinished)
    {
        if (result == null)
        {
            onFinished?.Invoke();
            return;
        }

        if (root != null)
            root.SetActive(true);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
        }

        onSequenceFinished = onFinished;

        StopAllCoroutines();
        StartCoroutine(PlayRoutine(result));
    }

    private IEnumerator PlayRoutine(CraftedItemResult result)
    {
        if (root != null)
            root.SetActive(true);

        ResetVisuals();
        ApplyTexts(result);

        yield return StartCoroutine(LoadResultImage(result));

        yield return StartCoroutine(FadeCanvasGroup(rootCanvasGroup, 0f, 1f, 0.15f));

       if (revealHintText != null)
            revealHintText.text = "키워드를 조합하는 중...";

        yield return StartCoroutine(ShowKeyword(keyword1Rect, keyword1Group));
        yield return new WaitForSeconds(keywordInterval);

        yield return StartCoroutine(ShowKeyword(keyword2Rect, keyword2Group));
        yield return new WaitForSeconds(keywordInterval);

        yield return StartCoroutine(ShowKeyword(keyword3Rect, keyword3Group));
        yield return new WaitForSeconds(0.15f);

        if (revealHintText != null)
            revealHintText.text = "키워드를 융합하는 중...";

        SpawnFX(chargeFxPrefab);

        yield return StartCoroutine(MoveKeywordsToCenter());

        yield return StartCoroutine(PulseGlow());

        SpawnFX(burstFxPrefab);

        yield return StartCoroutine(FlashScreen());

        if (revealHintText != null)
            revealHintText.text = "결과 이미지를 생성하는 중...";

        yield return StartCoroutine(ShowRevealCard());

        if (revealHintText != null)
            revealHintText.text = "제작 완료!";

        SpawnFX(revealFxPrefab);

        yield return new WaitForSeconds(revealHoldTime);

        yield return StartCoroutine(FadeCanvasGroup(rootCanvasGroup, 1f, 0f, endFadeTime));

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        onSequenceFinished?.Invoke();
    }

    private void ResetVisuals()
    {
        if (rootCanvasGroup != null)
            rootCanvasGroup.alpha = 0f;

        ResetKeyword(keyword1Rect, keyword1Group, keyword1StartPos);
        ResetKeyword(keyword2Rect, keyword2Group, keyword2StartPos);
        ResetKeyword(keyword3Rect, keyword3Group, keyword3StartPos);

        if (fusionGlowRect != null)
            fusionGlowRect.localScale = Vector3.one * 0.5f;

        if (flashImage != null)
            SetImageAlpha(flashImage, 0f);

        if (revealCardGroup != null)
        {
            revealCardGroup.alpha = 0f;
            revealCardGroup.transform.localScale = Vector3.one * 0.8f;
        }

        if (revealHintText != null)
            revealHintText.text = "결과 확인 중...";
    }

    private void ResetKeyword(RectTransform rect, CanvasGroup group, Vector2 startPos)
    {
        if (rect != null)
        {
            rect.anchoredPosition = startPos;
            rect.localScale = Vector3.zero;
        }

        if (group != null)
            group.alpha = 0f;
    }

    private void ApplyTexts(CraftedItemResult result)
    {
        if (keyword1Text != null)
            keyword1Text.text = GetKeywordByIndex(result, 0);

        if (keyword2Text != null)
            keyword2Text.text = GetKeywordByIndex(result, 1);

        if (keyword3Text != null)
            keyword3Text.text = GetKeywordByIndex(result, 2);

        if (itemNameText != null)
            itemNameText.text = result.itemName;

        if (gradeText != null)
        {
            gradeText.text = "등급 : " + result.grade;
            gradeText.color = GetGradeColor(result.grade);
        }
    }

    private string GetKeywordByIndex(CraftedItemResult result, int index)
    {
        if (result.usedKeywordNames != null && index < result.usedKeywordNames.Count)
            return result.usedKeywordNames[index];

        if (index == 0 && !string.IsNullOrEmpty(result.baseKeywordName))
            return result.baseKeywordName;

        if (index == 1 && !string.IsNullOrEmpty(result.styleKeywordName))
            return result.styleKeywordName;

        if (index == 2 && !string.IsNullOrEmpty(result.conceptKeywordName))
            return result.conceptKeywordName;

        return "키워드";
    }

    private IEnumerator ShowKeyword(RectTransform rect, CanvasGroup group)
    {
        float time = 0f;

        while (time < keywordAppearTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / keywordAppearTime);

            if (rect != null)
                rect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, EaseOutBack(t));

            if (group != null)
                group.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        if (rect != null)
            rect.localScale = Vector3.one;

        if (group != null)
            group.alpha = 1f;
    }

    private IEnumerator MoveKeywordsToCenter()
    {
        Vector2 center = Vector2.zero;
        float time = 0f;

        Vector2 p1 = keyword1Rect != null ? keyword1Rect.anchoredPosition : Vector2.zero;
        Vector2 p2 = keyword2Rect != null ? keyword2Rect.anchoredPosition : Vector2.zero;
        Vector2 p3 = keyword3Rect != null ? keyword3Rect.anchoredPosition : Vector2.zero;

        while (time < fusionMoveTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fusionMoveTime);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (keyword1Rect != null)
                keyword1Rect.anchoredPosition = Vector2.Lerp(p1, center, eased);

            if (keyword2Rect != null)
                keyword2Rect.anchoredPosition = Vector2.Lerp(p2, center, eased);

            if (keyword3Rect != null)
                keyword3Rect.anchoredPosition = Vector2.Lerp(p3, center, eased);

            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        if (keyword1Group != null) keyword1Group.alpha = 0f;
        if (keyword2Group != null) keyword2Group.alpha = 0f;
        if (keyword3Group != null) keyword3Group.alpha = 0f;
    }

    private IEnumerator PulseGlow()
    {
        if (fusionGlowRect == null)
            yield break;

        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float scale = Mathf.Lerp(0.5f, 1.5f, t);
            fusionGlowRect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    private IEnumerator FlashScreen()
    {
        if (flashImage == null)
            yield break;

        float half = flashTime * 0.5f;
        float time = 0f;

        while (time < half)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / half);
            SetImageAlpha(flashImage, Mathf.Lerp(0f, 0.9f, t));
            yield return null;
        }

        time = 0f;

        while (time < half)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / half);
            SetImageAlpha(flashImage, Mathf.Lerp(0.9f, 0f, t));
            yield return null;
        }

        SetImageAlpha(flashImage, 0f);
    }

    private IEnumerator ShowRevealCard()
    {
        if (revealCardGroup == null || revealCardRect == null)
            yield break;

        float duration = 0.35f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            revealCardGroup.alpha = t;

            float scale = Mathf.Lerp(0.8f, 1f, EaseOutBack(t));
            revealCardRect.localScale = Vector3.one * scale;

            yield return null;
        }

        revealCardGroup.alpha = 1f;
        revealCardRect.localScale = Vector3.one;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float time = 0f;
        group.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private void PlayBurstEffects()
    {
        if (burstEffects == null)
            return;

        for (int i = 0; i < burstEffects.Length; i++)
        {
            if (burstEffects[i] != null)
                burstEffects[i].Play();
        }
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null)
            return;

        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private IEnumerator LoadResultImage(CraftedItemResult result)
    {
        if (resultImage == null)
            yield break;

        resultImage.sprite = defaultResultSprite;
        resultImage.color = GetFallbackColor(result);

        if (!string.IsNullOrEmpty(result.imageUrl) && ApiManager.Instance != null)
        {
            yield return StartCoroutine(ApiManager.Instance.GetTexture(
                result.imageUrl,
                (tex) =>
                {
                    resultImage.sprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    resultImage.color = Color.white;
                },
                (err) =>
                {
                    Debug.LogWarning("[CraftRevealSequenceUI] 결과 이미지 로드 실패: " + err);
                }
            ));
        }
    }

    private Color GetFallbackColor(CraftedItemResult result)
    {
        string allKeywords = "";

        if (!string.IsNullOrEmpty(result.baseKeywordName))
            allKeywords += result.baseKeywordName;

        if (!string.IsNullOrEmpty(result.styleKeywordName))
            allKeywords += result.styleKeywordName;

        if (!string.IsNullOrEmpty(result.conceptKeywordName))
            allKeywords += result.conceptKeywordName;

        if (allKeywords.Contains("강철"))
            return new Color(0.65f, 0.7f, 0.85f, 1f);

        if (allKeywords.Contains("가성비"))
            return new Color(0.45f, 1f, 0.45f, 1f);

        if (allKeywords.Contains("달달한"))
            return new Color(1f, 0.75f, 0.85f, 1f);

        if (result.grade == "S")
            return new Color(1f, 0.85f, 0.2f, 1f);

        if (result.grade == "A")
            return new Color(0.8f, 0.6f, 1f, 1f);

        if (result.grade == "B")
            return new Color(0.7f, 0.9f, 1f, 1f);

        return Color.white;
    }

    private Color GetGradeColor(string grade)
    {
        if (grade == "S")
            return new Color(1f, 0.85f, 0.2f, 1f);

        if (grade == "A")
            return new Color(0.8f, 0.6f, 1f, 1f);

        if (grade == "B")
            return new Color(0.7f, 0.9f, 1f, 1f);

        return Color.white;
    }

    private void SpawnFX(GameObject fxPrefab)
    {
        if (fxPrefab == null || fxAnchor == null)
            return;

        GameObject fx = Instantiate(fxPrefab, fxAnchor.position, Quaternion.identity);

        ParticleSystem[] particles = fx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play();
        }

        ParticleSystemRenderer[] renderers = fx.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = 500;
        }

        Destroy(fx, fxDestroyDelay);
    }
}