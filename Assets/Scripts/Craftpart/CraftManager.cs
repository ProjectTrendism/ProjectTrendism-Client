using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CraftManager : MonoBehaviour
{
    public static CraftManager Instance;

    [Header("선택된 키워드")]
    public List<KeywordData> selectedKeywords = new List<KeywordData>();

    [Header("슬롯 텍스트")]
    public TextMeshProUGUI slot1Text;
    public TextMeshProUGUI slot2Text;
    public TextMeshProUGUI slot3Text;

    [Header("미리보기 텍스트")]
    public TextMeshProUGUI previewNameText;
    public TextMeshProUGUI previewPriceText;
    public TextMeshProUGUI previewTargetText;
    public TextMeshProUGUI previewTrendText;

    [Header("결과 팝업")]
    public ResultPopupUI resultPopupUI;

    [Header("AI 트렌드 분석 팝업")]
    public AITrendPopupUI aiTrendPopupUI;

    [Header("AI / 유행 분석 설정")]
    public string aiRisingKeywordName = "차가운";
    public int aiKeywordBonus = 15;
    public int trendKeywordBonus = 10;

    [Header("최근 제작 결과")]
    public CraftedItemResult lastCraftResult;

    [Header("제작 완료 목록 UI")]
    public CraftedItemListUI craftedItemListUI;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateSelectedSlots();
        UpdatePreview();
    }

    public void AddSelectedKeyword(KeywordData keyword)
    {
        if (keyword == null) return;

        if (selectedKeywords.Contains(keyword))
        {
            Debug.Log("이미 선택된 키워드입니다.");
            return;
        }

        if (selectedKeywords.Count >= 3)
        {
            Debug.Log("최대 3개까지만 선택 가능합니다.");
            return;
        }

        selectedKeywords.Add(keyword);
        UpdateSelectedSlots();
        UpdatePreview();
    }

    public void RemoveLastKeyword()
    {
        if (selectedKeywords.Count == 0) return;

        selectedKeywords.RemoveAt(selectedKeywords.Count - 1);
        UpdateSelectedSlots();
        UpdatePreview();
    }

    public void ClearSelectedKeywords()
    {
        selectedKeywords.Clear();
        UpdateSelectedSlots();
        UpdatePreview();
    }

    private void UpdateSelectedSlots()
    {
        if (slot1Text != null)
            slot1Text.text = selectedKeywords.Count > 0 ? selectedKeywords[0].keywordName : "키워드 선택";

        if (slot2Text != null)
            slot2Text.text = selectedKeywords.Count > 1 ? selectedKeywords[1].keywordName : "키워드 선택";

        if (slot3Text != null)
            slot3Text.text = selectedKeywords.Count > 2 ? selectedKeywords[2].keywordName : "키워드 선택";
    }

    private void UpdatePreview()
    {
        CraftAnalysisResult analysis = AnalyzeCurrentCraft();

        if (selectedKeywords.Count == 0)
        {
            if (previewNameText != null)
                previewNameText.text = "결과물 없음";

            if (previewPriceText != null)
                previewPriceText.text = "예상 가격 : -";

            if (previewTargetText != null)
                previewTargetText.text = "타겟 고객층 : -";

            if (previewTrendText != null)
                previewTrendText.text = "유행성 : -";

            return;
        }

        if (previewNameText != null)
            previewNameText.text = analysis.previewName;

        if (previewPriceText != null)
            previewPriceText.text = "예상 가격 : " + analysis.minPrice + " ~ " + analysis.maxPrice + " G";

        if (previewTargetText != null)
            previewTargetText.text = "타겟 고객층 : " + analysis.targetAudience;

        if (previewTrendText != null)
            previewTrendText.text = "유행성 : " + analysis.trendLevel;
    }

    private CraftAnalysisResult AnalyzeCurrentCraft()
    {
        CraftAnalysisResult analysis = new CraftAnalysisResult();

        analysis.aiRisingKeywordName = aiRisingKeywordName;

        if (selectedKeywords.Count == 0)
        {
            analysis.previewName = "결과물 없음";
            analysis.minPrice = 0;
            analysis.maxPrice = 0;
            analysis.finalPrice = 0;
            analysis.craftScore = 0;
            analysis.grade = "C";
            analysis.targetAudience = "-";
            analysis.trendLevel = "-";
            analysis.riskMessage = "-";
            analysis.adviceMessage = "-";
            analysis.balanceMessage = "키워드를 선택해 주세요.";
            analysis.aiMessage = "아직 선택한 키워드가 없습니다.";
            analysis.aiDirectionMessage = "키워드를 선택하면 분석됩니다.";
            return analysis;
        }

        bool hasBase = false;
        bool hasStyle = false;
        bool hasConcept = false;
        bool hasAIKeyword = false;

        int totalBasePrice = 0;
        int totalPopularity = 0;
        int totalFreshness = 0;
        int totalStability = 0;

        string targetAudience = "대중형";

        for (int i = 0; i < selectedKeywords.Count; i++)
        {
            KeywordData keyword = selectedKeywords[i];

            totalBasePrice += keyword.basePrice;
            totalPopularity += keyword.popularity;
            totalFreshness += keyword.freshness;
            totalStability += keyword.stability;

            if (keyword.keywordType == KeywordType.Base)
            {
                hasBase = true;
                analysis.baseKeywordName = keyword.keywordName;
            }

            if (keyword.keywordType == KeywordType.Style)
            {
                hasStyle = true;
                analysis.styleKeywordName = keyword.keywordName;
            }

            if (keyword.keywordType == KeywordType.Concept)
            {
                hasConcept = true;
                analysis.conceptKeywordName = keyword.keywordName;
            }

            if (!string.IsNullOrEmpty(keyword.raceTag))
                targetAudience = keyword.raceTag + " 고객층";

            if (!string.IsNullOrEmpty(aiRisingKeywordName) && keyword.keywordName == aiRisingKeywordName)
                hasAIKeyword = true;
        }

        int count = selectedKeywords.Count;
        int avgPopularity = totalPopularity / count;
        int avgFreshness = totalFreshness / count;
        int avgStability = totalStability / count;

        int score = 0;
        score += totalBasePrice / 2;
        score += totalPopularity / 2;
        score += totalFreshness / 2;
        score += totalStability / 2;

        if (hasBase && hasStyle && hasConcept)
            score += 20;

        if (selectedKeywords.Count == 3)
            score += 10;

        int aiBonus = 0;

        if (hasAIKeyword)
        {
            aiBonus = aiKeywordBonus;
            score += aiBonus;
        }

        int trendBonus = 0;

        if (avgPopularity >= 60)
        {
            trendBonus += trendKeywordBonus;
            score += trendKeywordBonus;
        }

        if (avgFreshness >= 60)
        {
            trendBonus += trendKeywordBonus;
            score += trendKeywordBonus;
        }

        int finalPrice = totalBasePrice + score;

        analysis.previewName = GetPreviewName();
        analysis.minPrice = Mathf.Max(1, finalPrice - 15);
        analysis.maxPrice = finalPrice + 15;
        analysis.finalPrice = finalPrice;

        analysis.craftScore = score;
        analysis.grade = GetGradeByScore(score);

        analysis.targetAudience = targetAudience;
        analysis.trendLevel = GetTrendLevelByScore(avgPopularity, avgFreshness, aiBonus, trendBonus);

        analysis.popularityScore = avgPopularity;
        analysis.freshnessScore = avgFreshness;
        analysis.stabilityScore = avgStability;

        analysis.aiBonus = aiBonus;
        analysis.trendBonus = trendBonus;

        analysis.riskMessage = GetRiskMessage(avgPopularity, avgFreshness, avgStability);
        analysis.adviceMessage = GetAdviceMessage(avgPopularity, avgFreshness, avgStability, selectedKeywords.Count);
        analysis.balanceMessage = GetBalanceMessage(hasBase, hasStyle, hasConcept);

        analysis.aiMessage = GetAIMessage(hasAIKeyword, hasBase, hasStyle, hasConcept);
        analysis.aiDirectionMessage = GetAIDirectionMessage(avgPopularity, avgFreshness, avgStability, hasAIKeyword);

        return analysis;
    }

    private string GetPreviewName()
    {
        if (selectedKeywords.Count == 1)
            return selectedKeywords[0].keywordName + " 상품";

        string result = "";

        for (int i = 0; i < selectedKeywords.Count; i++)
        {
            result += selectedKeywords[i].keywordName;

            if (i < selectedKeywords.Count - 1)
                result += " + ";
        }

        result += " 조합품";
        return result;
    }

    private string GetTrendLevelByScore(int popularity, int freshness, int aiBonus, int trendBonus)
    {
        int score = popularity + freshness + aiBonus + trendBonus;

        if (score >= 140) return "폭발적";
        if (score >= 100) return "매우 높음";
        if (score >= 70) return "높음";
        if (score >= 40) return "보통";
        return "낮음";
    }

    private string GetGradeByScore(int score)
    {
        if (score >= 90) return "S";
        if (score >= 70) return "A";
        if (score >= 45) return "B";
        return "C";
    }

    private string GetRiskMessage(int popularity, int freshness, int stability)
    {
        if (stability < 30 && freshness >= 50)
            return "신선하지만 유행이 빨리 식을 수 있음";

        if (popularity < 30)
            return "대중성이 낮아 구매층이 좁을 수 있음";

        if (freshness < 25)
            return "새로움이 부족해 SNS 반응이 약할 수 있음";

        if (stability >= 60 && popularity >= 50)
            return "안정적인 판매가 기대됨";

        return "보통";
    }

    private string GetAdviceMessage(int popularity, int freshness, int stability, int keywordCount)
    {
        if (keywordCount < 3)
            return "키워드를 더 추가하면 완성도가 올라갑니다.";

        if (popularity < 30)
            return "대중성 보정 추천";

        if (freshness < 30)
            return "신선함 보정 추천";

        if (stability < 30)
            return "안정성 보정 추천";

        return "현재 조합 유지 추천";
    }

    private string GetBalanceMessage(bool hasBase, bool hasStyle, bool hasConcept)
    {
        if (hasBase && hasStyle && hasConcept)
            return "Base, Style, Concept 균형이 좋습니다.";

        if (!hasBase)
            return "Base 키워드가 없어 기본 가치가 약할 수 있습니다.";

        if (!hasStyle)
            return "Style 키워드가 없어 고객층 특징이 약할 수 있습니다.";

        if (!hasConcept)
            return "Concept 키워드가 없어 유행성이 약할 수 있습니다.";

        return "조합을 더 구성해 보세요.";
    }

    private string GetAIMessage(bool hasAIKeyword, bool hasBase, bool hasStyle, bool hasConcept)
    {
        if (hasAIKeyword && hasBase && hasStyle && hasConcept)
            return "AI 추천 키워드가 포함된 균형 좋은 조합입니다.";

        if (hasAIKeyword)
            return "AI 추천 키워드가 포함되어 유행 보너스가 적용됩니다.";

        if (!string.IsNullOrEmpty(aiRisingKeywordName))
            return "AI 추천 키워드 '" + aiRisingKeywordName + "'를 포함하면 유행 보너스를 받을 수 있습니다.";

        return "AI 추천 키워드가 아직 설정되지 않았습니다.";
    }

    private string GetAIDirectionMessage(int popularity, int freshness, int stability, bool hasAIKeyword)
    {
        if (hasAIKeyword)
            return "급상승 키워드를 활용한 유행형 상품";

        if (freshness >= 60 && popularity >= 50)
            return "SNS 반응을 노리는 화제성 상품";

        if (stability >= 60)
            return "오래 팔리는 안정형 상품";

        if (popularity >= 60)
            return "넓은 고객층을 노리는 대중형 상품";

        return "균형 잡힌 기본 상품";
    }

    public void OnClickAITrendAnalysis()
    {
        CraftAnalysisResult analysis = AnalyzeCurrentCraft();

        if (aiTrendPopupUI != null)
        {
            aiTrendPopupUI.Show(analysis);
        }
        else
        {
            Debug.LogWarning("AITrendPopupUI가 CraftManager에 연결되지 않았습니다.");
        }
    }

    public CraftedItemResult CraftItem()
    {
        CraftAnalysisResult analysis = AnalyzeCurrentCraft();

        CraftedItemResult result = new CraftedItemResult();

        result.itemName = analysis.previewName;
        result.finalPrice = analysis.finalPrice;
        result.grade = analysis.grade;
        result.targetAudience = analysis.targetAudience;
        result.trendLevel = analysis.trendLevel;
        result.craftScore = analysis.craftScore;

        result.popularityScore = analysis.popularityScore;
        result.freshnessScore = analysis.freshnessScore;
        result.stabilityScore = analysis.stabilityScore;

        result.aiBonus = analysis.aiBonus;
        result.trendBonus = analysis.trendBonus;

        result.riskMessage = analysis.riskMessage;
        result.adviceMessage = analysis.adviceMessage;
        result.analysisMessage = analysis.aiMessage;

        result.baseKeywordName = analysis.baseKeywordName;
        result.styleKeywordName = analysis.styleKeywordName;
        result.conceptKeywordName = analysis.conceptKeywordName;

        for (int i = 0; i < selectedKeywords.Count; i++)
        {
            result.usedKeywordNames.Add(selectedKeywords[i].keywordName);
        }

        return result;
    }

    public void OnClickCraft()
    {
        if (selectedKeywords.Count == 0)
        {
            Debug.Log("선택된 키워드가 없습니다.");
            return;
        }

        lastCraftResult = CraftItem();

        if (ApiManager.Instance != null && ApiManager.Instance.isServerConnected)
        {
            StartCoroutine(CallCombineThenPredict(lastCraftResult));
        }
        else
        {
            Debug.LogWarning("[CraftManager] 서버 연결 없음 -> 로컬 결과로 제작 완료");
            FinalizeCraftResult();
        }
    }

    private IEnumerator CallCombineThenPredict(CraftedItemResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("[CraftManager] 제작 결과가 null입니다.");
            yield break;
        }

        List<int> keywordIdList = new List<int>();

        for (int i = 0; i < selectedKeywords.Count; i++)
        {
            if (selectedKeywords[i] == null)
                continue;

            if (selectedKeywords[i].serverId <= 0)
            {
                Debug.LogWarning("[CraftManager] serverId가 없는 키워드가 있습니다: " + selectedKeywords[i].keywordName);
                continue;
            }

            keywordIdList.Add(selectedKeywords[i].serverId);
        }

        // 현재 서버 app/schemas/craft.py의 CombineRequest는 min_length=3, max_length=3이다.
        // 따라서 제작 서버 AI 이름/설명/이미지를 받으려면 서로 다른 서버 키워드 3개가 반드시 필요하다.
        List<int> uniqueKeywordIds = new List<int>();
        for (int i = 0; i < keywordIdList.Count; i++)
        {
            if (!uniqueKeywordIds.Contains(keywordIdList[i]))
                uniqueKeywordIds.Add(keywordIdList[i]);
        }

        if (uniqueKeywordIds.Count != 3)
        {
            Debug.LogWarning(
                "[CraftManager] 서버 제작은 서로 다른 keyword_id 3개가 필요합니다. " +
                "현재 정상 서버 키워드 개수=" + uniqueKeywordIds.Count +
                " -> 로컬 결과로 진행"
            );
            FinalizeCraftResult();
            yield break;
        }

        var combineBody = new CombineRequestBody
        {
            keyword_ids = uniqueKeywordIds.ToArray()
        };

        string combineJson = JsonUtility.ToJson(combineBody);
        string combinationId = null;

        Debug.Log("[CraftManager] POST /craft/combine body=" + combineJson);

        yield return StartCoroutine(ApiManager.Instance.Post("/craft/combine", combineJson,
            (response) =>
            {
                Debug.Log("[CraftManager] /craft/combine response=" + response);

                CombineWrapper wrapper = JsonUtility.FromJson<CombineWrapper>(response);

                if (wrapper != null && wrapper.data != null)
                {
                    combinationId = wrapper.data.combination_id;

                    if (!string.IsNullOrEmpty(wrapper.data.preview_name))
                        result.itemName = wrapper.data.preview_name;
                }
            },
            (error) =>
            {
                Debug.LogWarning("[CraftManager] /craft/combine 실패: " + error);
            }
        ));

        if (string.IsNullOrEmpty(combinationId))
        {
            Debug.LogWarning("[CraftManager] combination_id 못 받음 -> 로컬 결과로 제작 완료");
            FinalizeCraftResult();
            yield break;
        }

        var predictBody = new PredictRequestBody
        {
            combination_id = combinationId,
            grade = result.grade
        };

        string predictJson = JsonUtility.ToJson(predictBody);

        Debug.Log("[CraftManager] POST /craft/predict body=" + predictJson);

        yield return StartCoroutine(ApiManager.Instance.Post("/craft/predict", predictJson,
            (response) =>
            {
                Debug.Log("[CraftManager] /craft/predict response=" + response);

                PredictWrapper wrapper = JsonUtility.FromJson<PredictWrapper>(response);

                if (wrapper != null && wrapper.data != null)
                {
                    if (!string.IsNullOrEmpty(wrapper.data.item_name))
                        result.itemName = wrapper.data.item_name;

                    result.itemDescription = wrapper.data.item_description;
                    result.imageUrl = wrapper.data.image_url;
                    result.serverItemId = wrapper.data.item_id;

                    if (wrapper.data.final_value > 0)
                        result.finalPrice = Mathf.RoundToInt(wrapper.data.final_value);

                    if (!string.IsNullOrEmpty(wrapper.data.grade))
                        result.grade = wrapper.data.grade;

                    Debug.Log("[CraftManager] predict 적용 완료 name=" + result.itemName +
                              " / itemId=" + result.serverItemId +
                              " / imageUrl=" + result.imageUrl +
                              " / cache=" + wrapper.data._cache);
                }
            },
            (error) =>
            {
                Debug.LogWarning("[CraftManager] /craft/predict 실패: " + error);
            }
        ));

        FinalizeCraftResult();
    }

    private void FinalizeCraftResult()
    {
        if (CraftedItemManager.Instance != null)
        {
            CraftedItemManager.Instance.AddCraftedItem(lastCraftResult);
        }

        if (RecipeBookManager.Instance != null)
        {
            RecipeBookManager.Instance.SaveRecipe(selectedKeywords, lastCraftResult);
        }

        if (resultPopupUI != null)
        {
            resultPopupUI.Show(lastCraftResult);
        }
        else
        {
            Debug.LogWarning("ResultPopupUI가 CraftManager에 연결되지 않았습니다.");
        }

        if (craftedItemListUI != null)
        {
            craftedItemListUI.RefreshList();
        }

        Debug.Log("제작 완료: " + lastCraftResult.itemName +
                  " / 등급: " + lastCraftResult.grade +
                  " / imageUrl: " + lastCraftResult.imageUrl);

        ClearSelectedKeywords();
    }

    [System.Serializable]
    private class CombineRequestBody
    {
        public int[] keyword_ids;
    }

    [System.Serializable]
    private class CombineDataDto
    {
        public string combination_id;
        public float estimated_value;
        public string preview_name;
    }

    [System.Serializable]
    private class CombineWrapper
    {
        public string status;
        public CombineDataDto data;
    }

    [System.Serializable]
    private class PredictRequestBody
    {
        public string combination_id;
        public string grade;
    }

    [System.Serializable]
    private class PredictDataDto
    {
        public string grade;
        public float final_value;
        public int item_id;
        public string item_name;
        public string item_description;
        public string image_url;
        public string _cache;
    }

    [System.Serializable]
    private class PredictWrapper
    {
        public string status;
        public PredictDataDto data;
    }
}