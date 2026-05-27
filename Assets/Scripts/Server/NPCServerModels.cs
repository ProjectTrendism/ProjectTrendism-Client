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
    public bool talked;
    public ServerGrantedKeyword granted_keyword;
}

[Serializable]
public class ServerGrantedKeyword
{
    public int id;
    public string name;
    public string keyword_type;
}
