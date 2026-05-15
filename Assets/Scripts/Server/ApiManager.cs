using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    public static ApiManager Instance;
    public bool isServerConnected = false;

    [SerializeField] private string baseUrl = "https://unfocusedly-pleurocarpous-gina.ngrok-free.dev";

    /// <summary>외부에서 base URL 참조용 (이미지 절대 경로 조합 등)</summary>
    public string BaseUrl => baseUrl;

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
        }
    }

    IEnumerator Start()
    {
        yield return StartCoroutine(CheckServerConnection());
    }

    IEnumerator CheckServerConnection()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(baseUrl + ""))
        {
            req.timeout = 3;
            req.SetRequestHeader("ngrok-skip-browser-warning", "true");
            yield return req.SendWebRequest();
            isServerConnected = req.result == UnityWebRequest.Result.Success;
            Debug.Log(isServerConnected ? "[OK] 서버 연결됨" : "[WARN] 오프라인 모드");
        }
    }

    public IEnumerator Get(string endpoint, Action<string> onSuccess, Action<string> onFail = null)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + endpoint))
        {
            request.SetRequestHeader("ngrok-skip-browser-warning", "true");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(request.downloadHandler.text);
            else
                onFail?.Invoke(request.error + "\n" + request.downloadHandler.text);
        }
    }

    public IEnumerator Post(string endpoint, string jsonBody, Action<string> onSuccess, Action<string> onFail = null)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + endpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("ngrok-skip-browser-warning", "true");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(request.downloadHandler.text);
            else
                onFail?.Invoke(request.error + "\n" + request.downloadHandler.text);
        }
    }

    /// <summary>
    /// 백엔드 정적 이미지를 Texture2D로 다운로드.
    /// url이 상대 경로(/static/...)면 baseUrl을 자동으로 prepend.
    /// 절대 경로(https://...)면 그대로 사용.
    /// </summary>
    public IEnumerator GetTexture(string url, Action<Texture2D> onSuccess, Action<string> onFail = null)
    {
        string fullUrl = url.StartsWith("http") ? url : baseUrl + url;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {
            request.SetRequestHeader("ngrok-skip-browser-warning", "true");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                onSuccess?.Invoke(tex);
            }
            else
            {
                onFail?.Invoke(request.error);
            }
        }
    }
}