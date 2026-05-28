using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class KeywordManager : MonoBehaviour
{
    public static KeywordManager Instance;

    [Header("키워드 UI")]
    public TextMeshProUGUI keywordListText;

    [Header("로드 설정")]
    public bool loadMasterKeywordsOnStart = true;
    public bool useLocalKeywordsWhenServerFails = true;

    [Header("디버그")]
    public bool verboseLog = false;
    public bool autoFindKeywordListText = true;

    // 서버 전체 키워드
    private readonly List<KeywordData> masterKeywords = new List<KeywordData>();

    // 플레이어가 탐험에서 획득한 키워드
    private readonly List<KeywordData> playerKeywords = new List<KeywordData>();

    private bool masterLoaded = false;

    public bool MasterLoaded => masterLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAutoBindKeywordText();
        UpdateKeywordUI();
        RefreshKeywordListUIs();
    }

    private IEnumerator Start()
    {
        if (loadMasterKeywordsOnStart)
            yield return StartCoroutine(LoadMasterKeywordsFromServerOrLocal());
    }

    [ContextMenu("마스터 키워드 다시 로드")]
    public void ReloadMasterKeywordsFromInspector()
    {
        StartCoroutine(LoadMasterKeywordsFromServerOrLocal());
    }

    public IEnumerator LoadMasterKeywordsFromServerOrLocal()
    {
        masterLoaded = false;

        float timer = 0f;
        while (ApiManager.Instance == null && timer < 3f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (ApiManager.Instance == null)
        {
            Debug.LogWarning("[KeywordManager] ApiManager가 없어 로컬 키워드를 사용합니다.");
            LoadLocalKeywords();
            masterLoaded = true;
            yield break;
        }

        bool success = false;
        string responseText = "";

        yield return StartCoroutine(ApiManager.Instance.Get(
            "/keywords",
            response =>
            {
                success = true;
                responseText = response;
            },
            error =>
            {
                success = false;
                responseText = error;
            }
        ));

        if (success)
        {
            bool parsed = TryApplyMasterKeywordResponse(responseText);

            if (parsed)
            {
                masterLoaded = true;
                Debug.Log("[KeywordManager] 서버 마스터 키워드 로드 완료: " + masterKeywords.Count + "개");
                UpdateKeywordUI();
                yield break;
            }

            Debug.LogWarning("[KeywordManager] 서버 응답 파싱 실패. 로컬 키워드를 사용합니다.\n" + responseText);
        }
        else
        {
            Debug.LogWarning("[KeywordManager] 서버 키워드 로드 실패. 로컬 키워드를 사용합니다.\n" + responseText);
        }

        if (useLocalKeywordsWhenServerFails)
        {
            LoadLocalKeywords();
            masterLoaded = true;
            UpdateKeywordUI();
        }
    }

    private bool TryApplyMasterKeywordResponse(string response)
    {
        try
        {
            KeywordListResponse result = JsonUtility.FromJson<KeywordListResponse>(response);

            if (result == null || result.data == null)
                return false;

            masterKeywords.Clear();

            foreach (KeywordItem item in result.data)
            {
                if (item == null || string.IsNullOrEmpty(item.name))
                    continue;

                KeywordData data = new KeywordData(item.name, ConvertCategory(item.category));
                data.serverId = item.id;
                data.description = item.description;
                data.basePrice = Mathf.Max(10, item.base_price);
                data.popularity = Mathf.Max(10, item.popularity);
                data.freshness = Mathf.Max(10, item.freshness);
                data.stability = Mathf.Max(10, item.stability);

                masterKeywords.Add(data);
            }

            return masterKeywords.Count > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[KeywordManager] 키워드 JSON 파싱 예외: " + e.Message);
            return false;
        }
    }

    private void LoadLocalKeywords()
    {
        string[] names =
        {
            "엘프 버섯", "고블린 가죽", "드워프 강철", "사과", "체리", "배", "복숭아", "포도", "우유", "양털",
            "차가운", "빈티지", "빈티지한", "신선한", "바삭한", "포근한",
            "MZ감성", "럭셔리", "힐링", "가성비", "달빛"
        };

        KeywordType[] types =
        {
            KeywordType.Base, KeywordType.Base, KeywordType.Base, KeywordType.Base, KeywordType.Base, KeywordType.Base, KeywordType.Base, KeywordType.Base, KeywordType.Base, KeywordType.Base,
            KeywordType.Style, KeywordType.Style, KeywordType.Style, KeywordType.Style, KeywordType.Style, KeywordType.Style,
            KeywordType.Concept, KeywordType.Concept, KeywordType.Concept, KeywordType.Concept, KeywordType.Concept
        };

        masterKeywords.Clear();

        for (int i = 0; i < names.Length; i++)
        {
            KeywordData data = new KeywordData(names[i], types[i]);
            data.serverId = i + 1;
            data.basePrice = 10;
            data.popularity = 10;
            data.freshness = 10;
            data.stability = 10;
            masterKeywords.Add(data);
        }

        Debug.Log("[KeywordManager] 로컬 키워드 " + masterKeywords.Count + "개 로드 완료");
    }

    public void AddKeyword(int serverId, string name, KeywordType type)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[AddKeyword] 이름이 없는 키워드는 추가하지 않습니다.");
            return;
        }

        if (verboseLog)
            Debug.Log("[AddKeyword] 요청: name=" + name + " / serverId=" + serverId + " / type=" + type);

        if (serverId > 0)
        {
            KeywordData existingById = playerKeywords.Find(k => k.serverId == serverId);
            if (existingById != null)
            {
                if (verboseLog)
                    Debug.Log("[AddKeyword] 이미 보유한 키워드(ID 기준): " + existingById.keywordName + " / serverId=" + existingById.serverId);

                UpdateKeywordUI();
                return;
            }
        }

        KeywordData existingByName = playerKeywords.Find(k => k.keywordName == name);
        if (existingByName != null)
        {
            if (verboseLog)
                Debug.Log("[AddKeyword] 이미 보유한 키워드(이름 기준): " + existingByName.keywordName + " / serverId=" + existingByName.serverId);

            UpdateKeywordUI();
            return;
        }

        KeywordData master = null;

        if (serverId > 0)
            master = GetMasterKeyword(serverId);

        if (master == null)
            master = GetMasterKeywordByName(name);

        if (master != null)
        {
            KeywordData copied = CopyKeyword(master);
            playerKeywords.Add(copied);

            if (verboseLog)
                Debug.Log("[AddKeyword] 마스터 키워드 추가 성공: " + copied.keywordName + " / serverId=" + copied.serverId + " / type=" + copied.keywordType);
        }
        else
        {
            KeywordData data = new KeywordData(name, type);
            data.serverId = serverId;
            data.basePrice = 10;
            data.popularity = 10;
            data.freshness = 10;
            data.stability = 10;

            playerKeywords.Add(data);

            Debug.LogWarning("[AddKeyword] 마스터에 없는 키워드 추가: " + data.keywordName + " / serverId=" + data.serverId + " / type=" + data.keywordType);
        }

        UpdateKeywordUI();

        if (verboseLog)
            Debug.Log("[AddKeyword] 현재 보유 키워드 개수: " + playerKeywords.Count);
    }

    public void AddOrUpdateKeywordFromServer(int serverId, string name, string category)
    {
        AddKeyword(serverId, name, ConvertCategory(category));
    }

    public KeywordData GetMasterKeyword(int serverId)
    {
        return masterKeywords.Find(k => k.serverId == serverId);
    }

    public KeywordData GetMasterKeywordByName(string name)
    {
        return masterKeywords.Find(k => k.keywordName == name);
    }

    private KeywordData CopyKeyword(KeywordData original)
    {
        KeywordData copy = new KeywordData(original.keywordName, original.keywordType);

        copy.serverId = original.serverId;
        copy.description = original.description;
        copy.basePrice = original.basePrice;
        copy.popularity = original.popularity;
        copy.freshness = original.freshness;
        copy.stability = original.stability;
        copy.raceTag = original.raceTag;
        copy.styleTag = original.styleTag;
        copy.conceptTag = original.conceptTag;

        return copy;
    }

    public void ForceRefreshUI()
    {
        TryAutoBindKeywordText();
        UpdateKeywordUI();
        RefreshKeywordListUIs();
    }

    private void TryAutoBindKeywordText()
    {
        if (!autoFindKeywordListText || keywordListText != null)
            return;

        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            string n = text.gameObject.name.ToLower();
            if (n.Contains("keywordlist") || n.Contains("keyword_list") || n.Contains("keywordtext") || n.Contains("keyword_text"))
            {
                keywordListText = text;
                return;
            }
        }

        foreach (TextMeshProUGUI text in texts)
        {
            string current = text.text == null ? "" : text.text;
            if (current.Contains("보유 키워드") || current.Contains("[Base]") || current.Contains("없음"))
            {
                keywordListText = text;
                return;
            }
        }
    }

    private void RefreshKeywordListUIs()
    {
        KeywordListUIManager[] listUIs = FindObjectsOfType<KeywordListUIManager>(true);
        for (int i = 0; i < listUIs.Length; i++)
        {
            if (listUIs[i] != null)
                listUIs[i].LoadKeywordButtons();
        }
    }

    private void UpdateKeywordUI()
    {
        TryAutoBindKeywordText();

        if (keywordListText == null)
        {
            if (verboseLog) Debug.LogWarning("[KeywordManager] keywordListText가 비어 있어 보유 키워드 텍스트 UI를 갱신하지 못했습니다.");
            RefreshKeywordListUIs();
            return;
        }

        if (playerKeywords.Count == 0)
        {
            keywordListText.text = "보유 키워드\n없음";
            RefreshKeywordListUIs();
            return;
        }

        string baseText = "<b>[Base]</b>\n";
        string styleText = "<b>[Style]</b>\n";
        string conceptText = "<b>[Concept]</b>\n";

        bool hasBase = false;
        bool hasStyle = false;
        bool hasConcept = false;

        foreach (KeywordData data in playerKeywords)
        {
            if (data.keywordType == KeywordType.Base)
            {
                baseText += "- " + data.keywordName + "\n";
                hasBase = true;
            }
            else if (data.keywordType == KeywordType.Style)
            {
                styleText += "- " + data.keywordName + "\n";
                hasStyle = true;
            }
            else if (data.keywordType == KeywordType.Concept)
            {
                conceptText += "- " + data.keywordName + "\n";
                hasConcept = true;
            }
        }

        if (!hasBase) baseText += "없음\n";
        if (!hasStyle) styleText += "없음\n";
        if (!hasConcept) conceptText += "없음\n";

        keywordListText.text = "보유 키워드\n" + baseText + "\n" + styleText + "\n" + conceptText;
        RefreshKeywordListUIs();
    }

    public List<KeywordData> GetKeywords()
    {
        return playerKeywords;
    }

    public List<KeywordData> GetMasterKeywords()
    {
        return masterKeywords;
    }

    public void ClearKeywords()
    {
        playerKeywords.Clear();
        UpdateKeywordUI();
    }

    private KeywordType ConvertCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return KeywordType.Base;

        string upper = category.Trim().ToUpper();

        switch (upper)
        {
            case "BASE":
            case "MATERIAL":
            case "재료":
                return KeywordType.Base;

            case "STYLE":
            case "스타일":
                return KeywordType.Style;

            case "CONCEPT":
            case "컨셉":
                return KeywordType.Concept;

            default:
                KeywordType parsed;
                if (System.Enum.TryParse(category, true, out parsed))
                    return parsed;

                Debug.LogWarning("[ConvertCategory] 알 수 없는 category: " + category + " -> Base로 처리");
                return KeywordType.Base;
        }
    }

    [System.Serializable]
    private class KeywordItem
    {
        public int id;
        public string name;
        public string category;
        public string rarity;
        public string description;
        public int base_price;
        public int popularity;
        public int freshness;
        public int stability;
    }

    [System.Serializable]
    private class KeywordListResponse
    {
        public string status;
        public List<KeywordItem> data;
    }
}
