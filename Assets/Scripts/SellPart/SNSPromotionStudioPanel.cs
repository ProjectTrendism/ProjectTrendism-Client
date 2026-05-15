using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SNSPromotionStudioUI : MonoBehaviour
{
    [Header("패널")]
    public GameObject panelRoot;

    [Header("버튼")]
    public Button closeButton;
    public Button cancelButton;
    public Button postButton;

    [Header("선택 상품")]
    public TextMeshProUGUI selectedItemNameText;

    [Header("폰 미리보기")]
    public Image previewBackgroundImage;
    public Image previewItemImage;
    public Image previewFrameImage;
    public Image[] stickerImages;
    public TextMeshProUGUI previewLikeText;
    public TextMeshProUGUI previewCaptionText;
    public TextMeshProUGUI previewHashtagText;

    [Header("입력")]
    public TMP_InputField captionInputField;
    public TextMeshProUGUI selectedHashtagText;

    [Header("효과 예상")]
    public TextMeshProUGUI expectedTrendBoostText;
    public TextMeshProUGUI expectedCrowdText;
    public TextMeshProUGUI promotionCostText;
    public TextMeshProUGUI promotionReasonText;

    [Header("스프라이트")]
    public Sprite fallbackItemSprite;
    public Sprite[] backgroundSprites;
    public Sprite[] frameSprites;
    public Sprite[] stickerSprites;

    private SellableItemData currentItem;

    private int selectedBackgroundIndex = 0;
    private int selectedFilterIndex = 0;
    private int selectedFrameIndex = 0;

    private readonly List<int> selectedStickerIndices = new List<int>();
    private readonly List<string> selectedHashtags = new List<string>();

    private float predictedTrendBoost;
    private float predictedCrowdBoost;
    private int predictedCost;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseStudio);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CloseStudio);

        if (postButton != null)
            postButton.onClick.AddListener(PostPromotion);

        if (captionInputField != null)
            captionInputField.onValueChanged.AddListener(OnCaptionChanged);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void OpenStudio(SellableItemData item)
    {
        currentItem = item;

        selectedBackgroundIndex = 0;
        selectedFilterIndex = 0;
        selectedFrameIndex = 0;
        selectedStickerIndices.Clear();
        selectedHashtags.Clear();

        string itemName = "선택 상품 없음";

        if (currentItem != null && currentItem.craftedItem != null)
            itemName = currentItem.craftedItem.itemName;

        if (selectedItemNameText != null)
            selectedItemNameText.text = "선택 상품 : " + itemName;

        if (captionInputField != null)
        {
            captionInputField.text =
                itemName + " ✨\n오늘 당신에게 작은 행복을 선물하세요!";
        }

        if (previewItemImage != null)
        {
            previewItemImage.sprite = fallbackItemSprite;
            previewItemImage.preserveAspect = true;
            previewItemImage.color = Color.white;
        }

        RefreshPreview();
        RefreshPrediction();

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void CloseStudio()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void SelectBackground(int index)
    {
        selectedBackgroundIndex = index;
        RefreshPreview();
        RefreshPrediction();
    }

    public void SelectFilter(int index)
    {
        selectedFilterIndex = index;
        RefreshPreview();
        RefreshPrediction();
    }

    public void SelectFrame(int index)
    {
        selectedFrameIndex = index;
        RefreshPreview();
        RefreshPrediction();
    }

    public void ToggleSticker(int index)
    {
        if (selectedStickerIndices.Contains(index))
        {
            selectedStickerIndices.Remove(index);
        }
        else
        {
            if (selectedStickerIndices.Count < 3)
                selectedStickerIndices.Add(index);
        }

        RefreshPreview();
        RefreshPrediction();
    }

    public void ToggleHashtag(string tag)
    {
        if (selectedHashtags.Contains(tag))
        {
            selectedHashtags.Remove(tag);
        }
        else
        {
            if (selectedHashtags.Count < 3)
                selectedHashtags.Add(tag);
        }

        RefreshPreview();
        RefreshPrediction();
    }

    private void OnCaptionChanged(string value)
    {
        RefreshPreview();
        RefreshPrediction();
    }

    private void RefreshPreview()
    {
        RefreshBackground();
        RefreshFilter();
        RefreshFrame();
        RefreshStickers();
        RefreshTexts();
    }

    private void RefreshBackground()
    {
        if (previewBackgroundImage == null)
            return;

        if (backgroundSprites != null &&
            selectedBackgroundIndex >= 0 &&
            selectedBackgroundIndex < backgroundSprites.Length &&
            backgroundSprites[selectedBackgroundIndex] != null)
        {
            previewBackgroundImage.sprite = backgroundSprites[selectedBackgroundIndex];
            previewBackgroundImage.color = Color.white;
            return;
        }

        // 스프라이트가 없어도 색으로 변화가 보이게 하는 임시 처리
        if (selectedBackgroundIndex == 0)
            previewBackgroundImage.color = new Color(0.95f, 0.85f, 0.85f, 1f);
        else if (selectedBackgroundIndex == 1)
            previewBackgroundImage.color = new Color(0.85f, 0.9f, 1f, 1f);
        else if (selectedBackgroundIndex == 2)
            previewBackgroundImage.color = new Color(0.85f, 1f, 0.88f, 1f);
        else
            previewBackgroundImage.color = new Color(1f, 0.95f, 0.75f, 1f);
    }

    private void RefreshFilter()
    {
        if (previewItemImage == null)
            return;

        // 필터를 상품 이미지 색감으로 임시 표현
        if (selectedFilterIndex == 0)
            previewItemImage.color = Color.white;
        else if (selectedFilterIndex == 1)
            previewItemImage.color = new Color(1f, 0.85f, 0.7f, 1f);      // 따뜻한
        else if (selectedFilterIndex == 2)
            previewItemImage.color = new Color(0.8f, 0.75f, 0.6f, 1f);    // 빈티지
        else if (selectedFilterIndex == 3)
            previewItemImage.color = new Color(0.75f, 0.9f, 1f, 1f);      // 청량
    }

    private void RefreshFrame()
    {
        if (previewFrameImage == null)
            return;

        if (frameSprites != null &&
            selectedFrameIndex >= 0 &&
            selectedFrameIndex < frameSprites.Length &&
            frameSprites[selectedFrameIndex] != null)
        {
            previewFrameImage.enabled = true;
            previewFrameImage.sprite = frameSprites[selectedFrameIndex];
            previewFrameImage.color = Color.white;
        }
        else
        {
            // 프레임 스프라이트가 없으면 선택 0은 숨기고 나머지는 색 테두리처럼 표시
            if (selectedFrameIndex == 0)
            {
                previewFrameImage.enabled = false;
            }
            else
            {
                previewFrameImage.enabled = true;
                previewFrameImage.color = new Color(1f, 1f, 1f, 0.45f);
            }
        }
    }

    private void RefreshStickers()
    {
        if (stickerImages == null)
            return;

        for (int i = 0; i < stickerImages.Length; i++)
        {
            if (stickerImages[i] == null)
                continue;

            if (i < selectedStickerIndices.Count)
            {
                int stickerIndex = selectedStickerIndices[i];

                stickerImages[i].enabled = true;

                if (stickerSprites != null &&
                    stickerIndex >= 0 &&
                    stickerIndex < stickerSprites.Length &&
                    stickerSprites[stickerIndex] != null)
                {
                    stickerImages[i].sprite = stickerSprites[stickerIndex];
                    stickerImages[i].color = Color.white;
                }
                else
                {
                    stickerImages[i].color = GetStickerFallbackColor(stickerIndex);
                }
            }
            else
            {
                stickerImages[i].enabled = false;
            }
        }
    }

    private Color GetStickerFallbackColor(int index)
    {
        if (index == 0) return new Color(1f, 0.45f, 0.65f, 1f);
        if (index == 1) return new Color(1f, 0.9f, 0.3f, 1f);
        if (index == 2) return new Color(0.75f, 0.6f, 1f, 1f);
        if (index == 3) return new Color(0.4f, 0.9f, 1f, 1f);
        if (index == 4) return new Color(0.7f, 1f, 0.5f, 1f);
        return new Color(1f, 1f, 1f, 1f);
    }

    private void RefreshTexts()
    {
        if (previewCaptionText != null && captionInputField != null)
            previewCaptionText.text = captionInputField.text;

        string hashtagText = selectedHashtags.Count == 0
            ? "#MZ감성 #힐링 #빈티지"
            : string.Join(" ", selectedHashtags);

        if (previewHashtagText != null)
            previewHashtagText.text = hashtagText;

        if (selectedHashtagText != null)
            selectedHashtagText.text = "선택 태그: " + hashtagText;

        if (previewLikeText != null)
        {
            int likeBase = 1000 + Mathf.RoundToInt(predictedTrendBoost * 30f) + Mathf.RoundToInt(predictedCrowdBoost * 20f);
            previewLikeText.text = "♥ " + likeBase.ToString("N0") + "명이 좋아합니다";
        }
    }

    private void RefreshPrediction()
    {
        predictedTrendBoost = 8f;
        predictedCrowdBoost = 10f;
        predictedCost = 800;

        if (currentItem != null && currentItem.craftedItem != null)
        {
            CraftedItemResult crafted = currentItem.craftedItem;

            predictedTrendBoost += crafted.freshnessScore * 0.08f;
            predictedCrowdBoost += crafted.popularityScore * 0.08f;

            if (crafted.aiBonus > 0)
                predictedTrendBoost += 3f;

            if (crafted.grade == "A" || crafted.grade == "S")
                predictedCrowdBoost += 3f;
        }

        predictedTrendBoost += selectedBackgroundIndex * 1.5f;
        predictedTrendBoost += selectedFilterIndex * 2f;
        predictedTrendBoost += selectedFrameIndex * 1f;
        predictedTrendBoost += selectedStickerIndices.Count * 1.5f;
        predictedTrendBoost += selectedHashtags.Count * 2f;

        predictedCrowdBoost += selectedBackgroundIndex * 1f;
        predictedCrowdBoost += selectedFilterIndex * 1f;
        predictedCrowdBoost += selectedFrameIndex * 1.5f;
        predictedCrowdBoost += selectedStickerIndices.Count * 1f;
        predictedCrowdBoost += selectedHashtags.Count * 1.5f;

        int captionLength = captionInputField != null ? captionInputField.text.Length : 0;

        if (captionLength >= 20)
            predictedCrowdBoost += 2f;

        if (captionLength >= 35)
            predictedTrendBoost += 2f;

        predictedCost += selectedBackgroundIndex * 100;
        predictedCost += selectedFilterIndex * 120;
        predictedCost += selectedFrameIndex * 100;
        predictedCost += selectedStickerIndices.Count * 120;
        predictedCost += selectedHashtags.Count * 80;

        predictedTrendBoost = Mathf.Clamp(predictedTrendBoost, 5f, 30f);
        predictedCrowdBoost = Mathf.Clamp(predictedCrowdBoost, 5f, 35f);

        if (expectedTrendBoostText != null)
            expectedTrendBoostText.text = "예상 유행 상승\n+" + Mathf.RoundToInt(predictedTrendBoost);

        if (expectedCrowdText != null)
            expectedCrowdText.text = "예상 고객 몰림도\n+" + Mathf.RoundToInt(predictedCrowdBoost) + "%";

        if (promotionCostText != null)
            promotionCostText.text = "홍보비\n" + predictedCost.ToString("N0") + " G";

        if (promotionReasonText != null)
        {
            string reason = "SNS 감성 홍보로 고객 반응을 높입니다.";

            if (currentItem != null && currentItem.craftedItem != null)
            {
                if (currentItem.craftedItem.freshnessScore >= 60)
                    reason = "신선한 상품이라 SNS 홍보 효과가 좋습니다.";
                else if (currentItem.craftedItem.popularityScore >= 60)
                    reason = "대중성이 높아 공유 반응이 기대됩니다.";
            }

            promotionReasonText.text = reason;
        }
    }

    private void PostPromotion()
    {
        if (currentItem == null)
            return;

        SellManager manager = SellManager.Instance;

        if (manager == null)
            return;

        string caption = captionInputField != null ? captionInputField.text : "";
        string hashtags = selectedHashtags.Count == 0
            ? "#MZ감성 #힐링 #빈티지"
            : string.Join(" ", selectedHashtags);

        manager.ApplySNSPromotionFromStudio(
            currentItem,
            predictedTrendBoost,
            predictedCrowdBoost,
            predictedCost,
            caption,
            hashtags
        );

        CloseStudio();
    }
}