using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeywordListUIManager : MonoBehaviour
{
    public Transform contentParent;
    public GameObject keywordButtonPrefab;

    [Header("키워드 버튼 크기")]
    public float buttonWidth = 150f;
    public float buttonHeight = 36f;
    public float spacing = 7f;

    private void Start()
    {
        SetupContentLayout();
        LoadKeywordButtons();
    }

    private void OnEnable()
    {
        SetupContentLayout();
    }

    private void SetupContentLayout()
    {
        if (contentParent == null)
        {
            Debug.LogWarning("[KeywordListUIManager] contentParent가 연결되지 않았습니다.");
            return;
        }

        RectTransform contentRect = contentParent.GetComponent<RectTransform>();

        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);

            contentRect.offsetMin = new Vector2(0f, contentRect.offsetMin.y);
            contentRect.offsetMax = new Vector2(0f, contentRect.offsetMax.y);
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);
        }

        VerticalLayoutGroup layout = contentParent.GetComponent<VerticalLayoutGroup>();

        if (layout == null)
            layout = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(6, 6, 8, 8);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;

        layout.childControlWidth = true;
        layout.childControlHeight = true;

        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentParent.GetComponent<ContentSizeFitter>();

        if (fitter == null)
            fitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void LoadKeywordButtons()
    {
        if (contentParent == null)
        {
            Debug.LogWarning("[KeywordListUIManager] contentParent가 비어 있습니다.");
            return;
        }

        if (keywordButtonPrefab == null)
        {
            Debug.LogWarning("[KeywordListUIManager] keywordButtonPrefab이 비어 있습니다.");
            return;
        }

        KeywordManager keywordManager = KeywordManager.Instance;

        if (keywordManager == null)
            keywordManager = FindObjectOfType<KeywordManager>();

        if (keywordManager == null)
        {
            Debug.LogWarning("[KeywordListUIManager] KeywordManager를 찾을 수 없습니다.");
            return;
        }

        List<KeywordData> keywords = keywordManager.GetKeywords();

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (keywords == null || keywords.Count == 0)
        {
            Debug.Log("[KeywordListUIManager] 표시할 보유 키워드가 없습니다.");
            return;
        }

        for (int i = 0; i < keywords.Count; i++)
        {
            GameObject obj = Instantiate(keywordButtonPrefab, contentParent);

            KeywordButtonUI ui = obj.GetComponent<KeywordButtonUI>();

            if (ui != null)
            {
                ui.Setup(keywords[i]);
            }

            ForceButtonLayout(obj);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
    }

    private void ForceButtonLayout(GameObject obj)
    {
        if (obj == null)
            return;

        RectTransform rect = obj.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        }

        LayoutElement layoutElement = obj.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = obj.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = buttonWidth;
        layoutElement.preferredHeight = buttonHeight;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        Image image = obj.GetComponent<Image>();

        if (image != null)
        {
            Color c = image.color;
            c.a = 1f;
            image.color = c;
            image.raycastTarget = true;
        }

        Button button = obj.GetComponent<Button>();

        if (button != null && image != null)
        {
            button.targetGraphic = image;
        }

        KeywordButtonUI keywordButtonUI = obj.GetComponent<KeywordButtonUI>();

        if (keywordButtonUI == null)
            return;

        if (keywordButtonUI.typeText != null)
        {
            keywordButtonUI.typeText.gameObject.SetActive(false);
        }

        if (keywordButtonUI.keywordNameText != null)
        {
            RectTransform textRect = keywordButtonUI.keywordNameText.GetComponent<RectTransform>();

            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 4f);
                textRect.offsetMax = new Vector2(-8f, -4f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
            }

            keywordButtonUI.keywordNameText.fontSize = 16f;
            keywordButtonUI.keywordNameText.fontStyle = FontStyles.Bold;
            keywordButtonUI.keywordNameText.alignment = TextAlignmentOptions.Center;
            keywordButtonUI.keywordNameText.enableWordWrapping = false;
            keywordButtonUI.keywordNameText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}