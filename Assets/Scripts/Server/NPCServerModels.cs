using System;
using Newtonsoft.Json;

[Serializable]
public class ServerNpcListResponse
{
    public string status;
    public ServerNpcData[] data;
}

[Serializable]
public class ServerNpcData
{
    public int id;
    public string name;
    public string location;
    public string portrait_id;
    public bool is_active;
    public string season_dialogue;

    public string dialogue;
    public string message;
    public string talk_text;
    public string line;

    [JsonProperty("perceived_reliability")]
    public int? perceived_reliability;

    public bool talked;
}

[Serializable]
public class ServerNpcTalkResponse
{
    public string status;
    public ServerNpcTalkData data;
}

[Serializable]
public class ServerNpcTalkData
{
    // /explore/action 응답 기본 필드
    public bool success;
    public string message;
    public string warning;

    // 혹시 다른 talk 응답 구조를 쓰는 경우 대비
    public bool talked;
    public string dialogue;
    public string talk_text;
    public string line;
    public string season_dialogue;

    // 현재 서버 /explore/action은 객체가 아니라 아래 낱개 필드로 키워드를 내려줌
    public int keyword_id;
    public string keyword_name;
    public string keyword_rarity;

    // 서버 버전에 따라 들어올 수 있는 이름 대비
    public string keyword_type;
    public string category;

    // 혹시 서버가 객체 형태로 바뀌었을 때도 대응
    public ServerGrantedKeyword granted_keyword;
    public ServerGrantedKeyword keyword;
    public ServerGrantedKeyword reward_keyword;
}

[Serializable]
public class ServerGrantedKeyword
{
    public int id;
    public string name;
    public string keyword_type;
    public string category;
    public string rarity;
}
