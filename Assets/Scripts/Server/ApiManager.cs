using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    public static ApiManager Instance;

    [Header("서버 설정")]
    [Tooltip("Cloudflare/Ngrok 등 서버 Base URL. 끝에 / 없이 입력")]
    [SerializeField] private string baseUrl = "https://neural-positioning-migration-commissioners.trycloudflare.com";

    [Header("연결 상태")]
    public bool isServerConnected = false;

    [Header("로그")]
    public bool verboseLog = true;

    public string BaseUrl => NormalizeBaseUrl(baseUrl);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            baseUrl = NormalizeBaseUrl(baseUrl);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        yield return StartCoroutine(CheckServerConnection());
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        baseUrl = NormalizeBaseUrl(baseUrl);
    }
#endif

    [ContextMenu("서버 연결 테스트")]
    public void TestConnectionFromInspector()
    {
        StartCoroutine(CheckServerConnection());
    }

    private string NormalizeBaseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        return url.Trim().TrimEnd('/');
    }

    private string BuildUrl(string endpoint)
    {
        string fixedBase = BaseUrl;
        string fixedEndpoint = string.IsNullOrWhiteSpace(endpoint) ? "" : endpoint.Trim();

        if (string.IsNullOrEmpty(fixedBase))
            return fixedEndpoint;

        if (string.IsNullOrEmpty(fixedEndpoint))
            return fixedBase;

        return fixedBase + "/" + fixedEndpoint.TrimStart('/');
    }

    private void ApplyCommonHeaders(UnityWebRequest request)
    {
        if (request == null)
            return;

        request.SetRequestHeader("Accept", "application/json");

        // Cloudflare에서는 없어도 되지만, ngrok에서 다시 테스트할 때 문제 없게 유지
        request.SetRequestHeader("ngrok-skip-browser-warning", "true");
    }

    private bool IsSuccess(UnityWebRequest request)
    {
        return request.result == UnityWebRequest.Result.Success &&
               request.responseCode >= 200 &&
               request.responseCode < 300;
    }

    public IEnumerator CheckServerConnection()
    {
        // 루트("/")는 서버 설정에 따라 404가 날 수 있어서,
        // 실제로 게임에서 쓰는 /keywords 엔드포인트로 연결 확인
        string url = BuildUrl("/keywords");

        if (verboseLog)
            Debug.Log("[ApiManager] 서버 연결 테스트 URL = " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            ApplyCommonHeaders(req);

            yield return req.SendWebRequest();

            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            isServerConnected = IsSuccess(req);

            if (isServerConnected)
            {
                Debug.Log("[OK] 서버 연결됨 / URL=" + url + " / HTTP=" + req.responseCode);
            }
            else
            {
                Debug.LogWarning(
                    "[WARN] 서버 연결 실패 -> 오프라인 모드 가능\n" +
                    "URL: " + url + "\n" +
                    "HTTP: " + req.responseCode + "\n" +
                    "ERROR: " + req.error + "\n" +
                    "BODY: " + body
                );
            }
        }
    }

    public IEnumerator Get(string endpoint, Action<string> onSuccess, Action<string> onFail = null)
    {
        string url = BuildUrl(endpoint);

        if (verboseLog)
            Debug.Log("[ApiManager] GET URL = " + url);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 8;
            ApplyCommonHeaders(request);

            yield return request.SendWebRequest();

            string body = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (IsSuccess(request))
            {
                if (verboseLog)
                {
                    Debug.Log(
                        "[ApiManager] GET 성공\n" +
                        "URL: " + url + "\n" +
                        "HTTP: " + request.responseCode + "\n" +
                        "BODY: " + body
                    );
                }

                onSuccess?.Invoke(body);
            }
            else
            {
                string error =
                    "GET 실패\n" +
                    "URL: " + url + "\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "ERROR: " + request.error + "\n" +
                    "BODY: " + body;

                Debug.LogWarning("[ApiManager] " + error);
                onFail?.Invoke(error);
            }
        }
    }

    public IEnumerator Post(string endpoint, string jsonBody, Action<string> onSuccess, Action<string> onFail = null)
    {
        string url = BuildUrl(endpoint);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jsonBody) ? "{}" : jsonBody);

        if (verboseLog)
        {
            Debug.Log(
                "[ApiManager] POST 요청\n" +
                "URL: " + url + "\n" +
                "BODY: " + jsonBody
            );
        }

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 15;

            ApplyCommonHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            string body = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (IsSuccess(request))
            {
                if (verboseLog)
                {
                    Debug.Log(
                        "[ApiManager] POST 성공\n" +
                        "URL: " + url + "\n" +
                        "HTTP: " + request.responseCode + "\n" +
                        "BODY: " + body
                    );
                }

                onSuccess?.Invoke(body);
            }
            else
            {
                string error =
                    "POST 실패\n" +
                    "URL: " + url + "\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "ERROR: " + request.error + "\n" +
                    "BODY: " + body;

                Debug.LogWarning("[ApiManager] " + error);
                onFail?.Invoke(error);
            }
        }
    }

    /// <summary>
    /// 백엔드 정적 이미지를 Texture2D로 다운로드.
    /// url이 상대 경로(/static/...)면 baseUrl을 자동으로 붙임.
    /// 절대 경로(https://...)면 그대로 사용.
    /// </summary>
    public IEnumerator GetTexture(string url, Action<Texture2D> onSuccess, Action<string> onFail = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            onFail?.Invoke("이미지 URL이 비어 있습니다.");
            yield break;
        }

        string fullUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : BaseUrl + "/" + url.TrimStart('/');

        if (verboseLog)
            Debug.Log("[ApiManager] IMAGE GET URL = " + fullUrl);

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {
            request.timeout = 15;
            ApplyCommonHeaders(request);

            yield return request.SendWebRequest();

            string body = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (IsSuccess(request))
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                onSuccess?.Invoke(tex);
            }
            else
            {
                string error =
                    "IMAGE GET 실패\n" +
                    "URL: " + fullUrl + "\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "ERROR: " + request.error + "\n" +
                    "BODY: " + body;

                Debug.LogWarning("[ApiManager] " + error);
                onFail?.Invoke(error);
            }
        }
    }
}
