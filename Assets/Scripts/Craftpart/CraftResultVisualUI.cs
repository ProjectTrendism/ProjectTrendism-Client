using UnityEngine;
using UnityEngine.UI;

public class CraftResultVisualUI : MonoBehaviour
{
    [Header("결과 이미지")]
    public Image resultItemImage;

    [Header("임시 기본 이미지")]
    public Sprite defaultResultSprite;

    [Header("나중에 AI 이미지가 들어올 자리")]
    public Sprite aiGeneratedSprite;

    public void Show(CraftedItemResult result)
    {
        if (result == null)
        {
            Clear();
            return;
        }

        if (resultItemImage == null)
        {
            Debug.LogWarning("ResultItemImage가 연결되지 않았습니다.");
            return;
        }

        resultItemImage.gameObject.SetActive(true);

        // 나중에 AI 이미지가 있으면 이걸 우선 사용
        if (aiGeneratedSprite != null)
        {
            resultItemImage.sprite = aiGeneratedSprite;
            resultItemImage.color = Color.white;
            return;
        }

        // 지금은 임시 이미지 사용
        if (defaultResultSprite != null)
        {
            resultItemImage.sprite = defaultResultSprite;
        }

        // 키워드에 따라 임시 색상 변경
        resultItemImage.color = GetTemporaryColor(result);
    }

    private Color GetTemporaryColor(CraftedItemResult result)
    {
        string allKeywords = "";

        if (!string.IsNullOrEmpty(result.baseKeywordName))
            allKeywords += result.baseKeywordName;

        if (!string.IsNullOrEmpty(result.styleKeywordName))
            allKeywords += result.styleKeywordName;

        if (!string.IsNullOrEmpty(result.conceptKeywordName))
            allKeywords += result.conceptKeywordName;

        if (allKeywords.Contains("차가운") || allKeywords.Contains("가가운"))
            return new Color(0.45f, 0.75f, 1f, 1f);

        if (allKeywords.Contains("매운"))
            return new Color(1f, 0.35f, 0.25f, 1f);

        if (allKeywords.Contains("가성비"))
            return new Color(0.45f, 1f, 0.45f, 1f);

        if (allKeywords.Contains("럭셔리"))
            return new Color(1f, 0.85f, 0.25f, 1f);

        if (allKeywords.Contains("가죽"))
            return new Color(0.75f, 0.5f, 0.3f, 1f);

        if (allKeywords.Contains("강철"))
            return new Color(0.65f, 0.7f, 0.85f, 1f);

        if (result.grade == "S")
            return new Color(1f, 0.8f, 0.2f, 1f);

        if (result.grade == "A")
            return new Color(0.8f, 0.6f, 1f, 1f);

        if (result.grade == "B")
            return new Color(0.7f, 0.9f, 1f, 1f);

        return Color.white;
    }

    public void Clear()
    {
        if (resultItemImage != null)
        {
            resultItemImage.sprite = defaultResultSprite;
            resultItemImage.color = Color.white;
            resultItemImage.gameObject.SetActive(false);
        }
    }
}