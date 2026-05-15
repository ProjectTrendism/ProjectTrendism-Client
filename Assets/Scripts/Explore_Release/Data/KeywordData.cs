using UnityEngine;

[System.Serializable]
public class KeywordData
{
    public int serverId;        // 서버 API id 추가!
    public string keywordName;
    public KeywordType keywordType;

    [TextArea]
    public string description;

    public int basePrice;
    public int popularity;
    public int freshness;
    public int stability;

    public string raceTag;
    public string styleTag;
    public string conceptTag;

    public KeywordData(string name, KeywordType type)
    {
        serverId = 0;
        keywordName = name;
        keywordType = type;

        description = "";
        basePrice = 10;
        popularity = 10;
        freshness = 10;
        stability = 10;

        raceTag = "";
        styleTag = "";
        conceptTag = "";
    }
}