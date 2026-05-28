using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KeywordManager : MonoBehaviour
{
    public static KeywordManager Instance;

    public TextMeshProUGUI keywordListText;

    // 서버 전체 키워드 (참조용)
    private List<KeywordData> masterKeywords = new List<KeywordData>();

    // 플레이어가 탐험에서 획득한 키워드만
    private List<KeywordData> playerKeywords = new List<KeywordData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        StartCoroutine(LoadMasterKeywordsFromServer());
    }

    // 서버에서 전체 키워드 마스터 데이터 로드 (참조용)
    /*IEnumerator LoadMasterKeywordsFromServer()
    {
        yield return StartCoroutine(ApiManager.Instance.Get(
            "keywords",
            (response) =>
            {
                var result = JsonUtility.FromJson<KeywordListResponse>(response);
                masterKeywords.Clear();

                foreach (var item in result.data)
                {
                    KeywordData data = new KeywordData(item.name, ConvertCategory(item.category));
                    data.serverId = item.id;
                    data.description = item.description;
                    data.basePrice = 10;
                    data.popularity = 10;
                    data.freshness = 10;
                    data.stability = 10;
                    masterKeywords.Add(data);
                }

                Debug.Log($"마스터 키워드 {masterKeywords.Count}개 로드 완료!");
            },
            (error) => Debug.LogError("키워드 로드 실패: " + error)
        ));
    }*/


    IEnumerator LoadMasterKeywordsFromServer()
{
    yield return StartCoroutine(ApiManager.Instance.Get(
        "keywords",
        (response) =>
        {
            var result = JsonUtility.FromJson<KeywordListResponse>(response);
            masterKeywords.Clear();

            foreach (var item in result.data)
            {
                KeywordData data = new KeywordData(item.name, ConvertCategory(item.category));
                data.serverId = item.id;
                data.description = item.description;
                data.basePrice = 10;
                data.popularity = 10;
                data.freshness = 10;
                data.stability = 10;
                masterKeywords.Add(data);
            }
            Debug.Log($"마스터 키워드 {masterKeywords.Count}개 로드 완료!");
        },
        (error) =>
        {
            Debug.LogWarning("서버 연결 실패 → 로컬 데이터 사용");
            LoadLocalKeywords();
        }
    ));
}

void LoadLocalKeywords()
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

    Debug.Log("로컬 키워드 " + masterKeywords.Count + "개 로드 완료!");
}
    // 탐험에서 키워드 획득 시 호출
    public void AddKeyword(int serverId, string name, KeywordType type)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[AddKeyword] 이름이 없는 키워드는 추가하지 않습니다.");
            return;
        }

        Debug.Log($"[AddKeyword] 요청: name={name} / serverId={serverId} / type={type}");

        // 1순위: 서버 ID 기준 중복 방지
        if (serverId > 0)
        {
            KeywordData existingById = playerKeywords.Find(k => k.serverId == serverId);
            if (existingById != null)
            {
                Debug.Log($"[AddKeyword] 이미 보유한 키워드(ID 기준): {existingById.keywordName} / serverId={existingById.serverId}");
                UpdateKeywordUI();
                return;
            }
        }

        // 2순위: 이름 기준 중복 방지
        KeywordData existingByName = playerKeywords.Find(k => k.keywordName == name);
        if (existingByName != null)
        {
            Debug.Log($"[AddKeyword] 이미 보유한 키워드(이름 기준): {existingByName.keywordName} / serverId={existingByName.serverId}");
            UpdateKeywordUI();
            return;
        }

        KeywordData master = null;

        // 서버 ID가 있으면 무조건 ID로 먼저 찾는다.
        if (serverId > 0)
        {
            master = GetMasterKeyword(serverId);
        }

        // ID 매칭 실패 시 이름으로 보조 매칭
        if (master == null)
        {
            master = GetMasterKeywordByName(name);
        }

        if (master != null)
        {
            KeywordData copied = CopyKeyword(master);
            playerKeywords.Add(copied);

            Debug.Log($"[AddKeyword] 마스터 키워드 추가 성공: {copied.keywordName} / serverId={copied.serverId} / type={copied.keywordType}");
        }
        else
        {
            KeywordData data = new KeywordData(name, type);

            // 중요: 서버 ID가 없으면 0으로 둔다.
            // 절대 GetHashCode로 가짜 서버 ID를 만들지 않는다.
            data.serverId = serverId;
            data.basePrice = 10;
            data.popularity = 10;
            data.freshness = 10;
            data.stability = 10;

            playerKeywords.Add(data);

            Debug.LogWarning($"[AddKeyword] 마스터에 없는 키워드 추가: {data.keywordName} / serverId={data.serverId} / type={data.keywordType}");
        }

        UpdateKeywordUI();
        Debug.Log($"[AddKeyword] 현재 보유 키워드 개수: {playerKeywords.Count}");
    }

    // serverId로 마스터 키워드 찾기 (NPC 드랍 시 사용)
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

        return copy;
    }


    private void UpdateKeywordUI()
    {
        if (keywordListText == null) return;

        if (playerKeywords.Count == 0)
        {
            keywordListText.text = "없음";
            return;
        }

        string baseText = "<b>[Base]</b>\n";
        string styleText = "<b>[Style]</b>\n";
        string conceptText = "<b>[Concept]</b>\n";

        bool hasBase = false, hasStyle = false, hasConcept = false;

        foreach (var data in playerKeywords)
        {
            if (data.keywordType == KeywordType.Base)
            { baseText += "- " + data.keywordName + "\n"; hasBase = true; }
            else if (data.keywordType == KeywordType.Style)
            { styleText += "- " + data.keywordName + "\n"; hasStyle = true; }
            else if (data.keywordType == KeywordType.Concept)
            { conceptText += "- " + data.keywordName + "\n"; hasConcept = true; }
        }

        if (!hasBase) baseText += "없음\n";
        if (!hasStyle) styleText += "없음\n";
        if (!hasConcept) conceptText += "없음\n";

        keywordListText.text = baseText + "\n" + styleText + "\n" + conceptText;
    }

    // 제작에서 사용 (플레이어 보유 키워드만)
    public List<KeywordData> GetKeywords()
    {
        return playerKeywords;
    }

    public void ClearKeywords()
    {
        playerKeywords.Clear();
        UpdateKeywordUI();
    }

    KeywordType ConvertCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return KeywordType.Base;

        string upper = category.ToUpper();

        switch (upper)
        {
            case "BASE": return KeywordType.Base;
            case "STYLE": return KeywordType.Style;
            case "CONCEPT": return KeywordType.Concept;
            default:
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
    }

    [System.Serializable]
    private class KeywordListResponse
    {
        public string status;
        public List<KeywordItem> data;
    }
}