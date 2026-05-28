using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public enum TrendismServerTarget
{
    Cloudflare,
    Localhost,
    Custom
}

public class ApiManager : MonoBehaviour
{
    public static ApiManager Instance;

    [Header("서버 선택")]
    public TrendismServerTarget serverTarget = TrendismServerTarget.Cloudflare;

    [Tooltip("서버 담당자가 열어준 Cloudflare 주소. 끝에 / 없이 입력")]
    [SerializeField] private string cloudflareBaseUrl = "https://logic-yarn-advances-colorado.trycloudflare.com";

    [Tooltip("내 컴퓨터에서 uvicorn을 직접 실행할 때 쓰는 주소")]
    [SerializeField] private string localBaseUrl = "http://127.0.0.1:8000";

    [Tooltip("직접 입력하고 싶을 때 사용")]
    [SerializeField] private string customBaseUrl = "";

    [Header("하위 호환용 Base URL")]
    [Tooltip("기존 스크립트가 baseUrl을 참조하던 경우를 위한 값입니다. Custom 모드가 아니면 위 서버 선택값이 우선입니다.")]
    [SerializeField] private string baseUrl = "https://logic-yarn-advances-colorado.trycloudflare.com";
    [Header("연결 상태")]
    public bool isServerConnected = false;
    public long lastResponseCode = 0;
    public string lastError = "";
    public string lastResponseBody = "";

    [Header("요청 설정")]
    public int defaultTimeoutSeconds = 10;
    public bool addNgrokSkipHeader = true;

    [Header("로그")]
    public bool verboseLog = false;

    public string BaseUrl
    {
        get
        {
            string selected = "";

            if (serverTarget == TrendismServerTarget.Localhost)
                selected = localBaseUrl;
            else if (serverTarget == TrendismServerTarget.Custom)
                selected = string.IsNullOrWhiteSpace(customBaseUrl) ? baseUrl : customBaseUrl;
            else
                selected = cloudflareBaseUrl;

            if (string.IsNullOrWhiteSpace(selected))
                selected = baseUrl;

            return NormalizeBaseUrl(selected);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            cloudflareBaseUrl = NormalizeBaseUrl(cloudflareBaseUrl);
            localBaseUrl = NormalizeBaseUrl(localBaseUrl);
            customBaseUrl = NormalizeBaseUrl(customBaseUrl);
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
        cloudflareBaseUrl = NormalizeBaseUrl(cloudflareBaseUrl);
        localBaseUrl = NormalizeBaseUrl(localBaseUrl);
        customBaseUrl = NormalizeBaseUrl(customBaseUrl);
        baseUrl = NormalizeBaseUrl(baseUrl);
    }
#endif

    [ContextMenu("서버 모드: Cloudflare")]
    public void UseCloudflare()
    {
        serverTarget = TrendismServerTarget.Cloudflare;
        Debug.Log("[ApiManager] 서버 모드 변경: Cloudflare / " + BaseUrl);
    }

    [ContextMenu("서버 모드: Localhost")]
    public void UseLocalhost()
    {
        serverTarget = TrendismServerTarget.Localhost;
        Debug.Log("[ApiManager] 서버 모드 변경: Localhost / " + BaseUrl);
    }

    public void SetCustomBaseUrl(string url)
    {
        customBaseUrl = NormalizeBaseUrl(url);
        baseUrl = customBaseUrl;
        serverTarget = TrendismServerTarget.Custom;
        Debug.Log("[ApiManager] Custom BaseUrl 설정: " + BaseUrl);
    }

    [ContextMenu("서버 연결 테스트")]
    public void TestConnectionFromInspector()
    {
        StartCoroutine(CheckServerConnection());
    }

    public string NormalizeBaseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        return url.Trim().TrimEnd('/');
    }

    public string BuildUrl(string endpoint)
    {
        string fixedBase = BaseUrl;
        string fixedEndpoint = string.IsNullOrWhiteSpace(endpoint) ? "" : endpoint.Trim();

        if (string.IsNullOrEmpty(fixedBase))
            return fixedEndpoint;

        if (string.IsNullOrEmpty(fixedEndpoint))
            return fixedBase;

        return fixedBase + "/" + fixedEndpoint.TrimStart('/');
    }

    public void ApplyCommonHeaders(UnityWebRequest request)
    {
        if (request == null)
            return;

        request.SetRequestHeader("Accept", "application/json");

        if (addNgrokSkipHeader)
            request.SetRequestHeader("ngrok-skip-browser-warning", "true");
    }

    public bool IsSuccess(UnityWebRequest request)
    {
        return request != null &&
               request.result == UnityWebRequest.Result.Success &&
               request.responseCode >= 200 &&
               request.responseCode < 300;
    }

    private void SaveLastResult(UnityWebRequest request)
    {
        if (request == null)
            return;

        lastResponseCode = request.responseCode;
        lastError = request.error;
        lastResponseBody = request.downloadHandler != null ? request.downloadHandler.text : "";
        isServerConnected = IsSuccess(request);
    }

    public IEnumerator CheckServerConnection()
    {
        string url = BuildUrl("/keywords");

        if (verboseLog)
            Debug.Log("[ApiManager] 서버 연결 테스트 URL = " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = defaultTimeoutSeconds;
            ApplyCommonHeaders(req);

            yield return req.SendWebRequest();

            SaveLastResult(req);

            if (isServerConnected)
            {
                Debug.Log("[OK] 서버 연결됨 / URL=" + url + " / HTTP=" + req.responseCode);
            }
            else
            {
                Debug.LogWarning(
                    "[WARN] 서버 연결 실패 -> 로컬 fallback 가능\n" +
                    "URL: " + url + "\n" +
                    "HTTP: " + req.responseCode + "\n" +
                    "ERROR: " + req.error + "\n" +
                    "BODY: " + lastResponseBody
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
            request.timeout = defaultTimeoutSeconds;
            ApplyCommonHeaders(request);

            yield return request.SendWebRequest();

            SaveLastResult(request);

            if (IsSuccess(request))
            {
                if (verboseLog)
                    Debug.Log("[ApiManager] GET 성공\nURL: " + url + "\nHTTP: " + request.responseCode + "\nBODY: " + lastResponseBody);

                onSuccess?.Invoke(lastResponseBody);
            }
            else
            {
                string error =
                    "GET 실패\n" +
                    "URL: " + url + "\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "ERROR: " + request.error + "\n" +
                    "BODY: " + lastResponseBody;

                Debug.LogWarning("[ApiManager] " + error);
                onFail?.Invoke(error);
            }
        }
    }

    public IEnumerator Post(string endpoint, string jsonBody, Action<string> onSuccess, Action<string> onFail = null)
    {
        yield return StartCoroutine(SendWithBody("POST", endpoint, jsonBody, onSuccess, onFail));
    }

    public IEnumerator Patch(string endpoint, string jsonBody, Action<string> onSuccess, Action<string> onFail = null)
    {
        yield return StartCoroutine(SendWithBody("PATCH", endpoint, jsonBody, onSuccess, onFail));
    }

    public IEnumerator SendWithBody(string method, string endpoint, string jsonBody, Action<string> onSuccess, Action<string> onFail = null)
    {
        string url = BuildUrl(endpoint);
        string bodyText = string.IsNullOrEmpty(jsonBody) ? "{}" : jsonBody;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyText);
        string fixedMethod = string.IsNullOrWhiteSpace(method) ? "POST" : method.Trim().ToUpper();

        if (verboseLog)
            Debug.Log("[ApiManager] " + fixedMethod + " 요청\nURL: " + url + "\nBODY: " + bodyText);

        using (UnityWebRequest request = new UnityWebRequest(url, fixedMethod))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = defaultTimeoutSeconds;

            ApplyCommonHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            SaveLastResult(request);

            if (IsSuccess(request))
            {
                if (verboseLog)
                    Debug.Log("[ApiManager] " + fixedMethod + " 성공\nURL: " + url + "\nHTTP: " + request.responseCode + "\nBODY: " + lastResponseBody);

                onSuccess?.Invoke(lastResponseBody);
            }
            else
            {
                string error =
                    fixedMethod + " 실패\n" +
                    "URL: " + url + "\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "ERROR: " + request.error + "\n" +
                    "BODY: " + lastResponseBody;

                Debug.LogWarning("[ApiManager] " + error);
                onFail?.Invoke(error);
            }
        }
    }

    public IEnumerator GetTexture(string url, Action<Texture2D> onSuccess, Action<string> onFail = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            onFail?.Invoke("이미지 URL이 비어 있습니다.");
            yield break;
        }

        string fullUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : BuildUrl(url);

        if (verboseLog)
            Debug.Log("[ApiManager] IMAGE GET URL = " + fullUrl);

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {
            request.timeout = defaultTimeoutSeconds;
            ApplyCommonHeaders(request);

            yield return request.SendWebRequest();

            SaveLastResult(request);

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
                    "BODY: " + lastResponseBody;

                Debug.LogWarning("[ApiManager] " + error);
                onFail?.Invoke(error);
            }
        }
    }
}
