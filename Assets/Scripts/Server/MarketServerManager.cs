using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// /market API 전용 통신 매니저.
/// 실제 판매 계산은 서버가 하고, 실패하면 SellManager의 기존 로컬 로직으로 fallback할 수 있게 콜백만 제공한다.
/// </summary>
public class MarketServerManager : MonoBehaviour
{
    public static MarketServerManager Instance;

    [Header("동작 설정")]
    public bool useApiManagerBaseUrl = true;
    public bool verboseLog = true;

    [Header("기본 쿼리")]
    public int trendDays = 60;
    public int simulateDays = 60;
    public int simulateBaseBuyers = 10;

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

    private bool IsReady()
    {
        if (ApiManager.Instance == null)
        {
            Debug.LogWarning("[MarketServerManager] ApiManager.Instance가 없습니다.");
            return false;
        }

        return true;
    }

    public IEnumerator RegisterItem(
        MarketRegisterReqBody body,
        Action<MarketItemDto> onSuccess,
        Action<string> onFail = null)
    {
        if (!IsReady())
        {
            onFail?.Invoke("ApiManager 없음");
            yield break;
        }

        if (body == null)
        {
            onFail?.Invoke("등록 body가 null입니다.");
            yield break;
        }

        if (body.keyword_ids == null)
            body.keyword_ids = new int[0];

        if (body.stock <= 0)
            body.stock = 1;

        string json = JsonUtility.ToJson(body);

        if (verboseLog)
            Debug.Log("[MarketServerManager] POST /market/items\n" + json);

        yield return ApiManager.Instance.Post("/market/items", json,
            response =>
            {
                if (verboseLog)
                    Debug.Log("[MarketServerManager] /market/items 응답\n" + response);

                MarketRegisterEnvelope wrapper = JsonUtility.FromJson<MarketRegisterEnvelope>(response);
                if (wrapper != null && wrapper.data != null && wrapper.data.id > 0)
                    onSuccess?.Invoke(wrapper.data);
                else
                    onFail?.Invoke("아이템 등록 응답 파싱 실패: " + response);
            },
            error =>
            {
                Debug.LogWarning("[MarketServerManager] /market/items 실패\n" + error);
                onFail?.Invoke(error);
            });
    }

    public IEnumerator GetTrend(
        int itemId,
        Action<MarketTrendData> onSuccess,
        Action<string> onFail = null)
    {
        if (!IsReady()) { onFail?.Invoke("ApiManager 없음"); yield break; }

        string endpoint = "/market/trend/" + itemId + "?days=" + trendDays;
        yield return ApiManager.Instance.Get(endpoint,
            response =>
            {
                MarketTrendEnvelope wrapper = JsonUtility.FromJson<MarketTrendEnvelope>(response);
                if (wrapper != null && wrapper.data != null)
                    onSuccess?.Invoke(wrapper.data);
                else
                    onFail?.Invoke("trend 응답 파싱 실패: " + response);
            },
            error =>
            {
                Debug.LogWarning("[MarketServerManager] trend 실패\n" + error);
                onFail?.Invoke(error);
            });
    }

    public IEnumerator AnalyzeItem(
        int itemId,
        Action<MarketAnalyzeData> onSuccess,
        Action<string> onFail = null)
    {
        if (!IsReady()) { onFail?.Invoke("ApiManager 없음"); yield break; }

        yield return ApiManager.Instance.Post("/market/analyze/" + itemId, "{}",
            response =>
            {
                if (verboseLog)
                    Debug.Log("[MarketServerManager] /market/analyze 응답\n" + response);

                MarketAnalyzeEnvelope wrapper = JsonUtility.FromJson<MarketAnalyzeEnvelope>(response);
                if (wrapper != null && wrapper.data != null)
                    onSuccess?.Invoke(wrapper.data);
                else
                    onFail?.Invoke("analyze 응답 파싱 실패: " + response);
            },
            error =>
            {
                Debug.LogWarning("[MarketServerManager] analyze 실패\n" + error);
                onFail?.Invoke(error);
            });
    }

    public IEnumerator SimulateBuyers(
        int itemId,
        Action<MarketSimulateData> onSuccess,
        Action<string> onFail = null)
    {
        if (!IsReady()) { onFail?.Invoke("ApiManager 없음"); yield break; }

        string endpoint = "/market/simulate/" + itemId +
                          "?days=" + simulateDays +
                          "&base_buyers=" + simulateBaseBuyers;

        yield return ApiManager.Instance.Get(endpoint,
            response =>
            {
                MarketSimulateEnvelope wrapper = JsonUtility.FromJson<MarketSimulateEnvelope>(response);
                if (wrapper != null && wrapper.data != null)
                    onSuccess?.Invoke(wrapper.data);
                else
                    onFail?.Invoke("simulate 응답 파싱 실패: " + response);
            },
            error =>
            {
                Debug.LogWarning("[MarketServerManager] simulate 실패\n" + error);
                onFail?.Invoke(error);
            });
    }

    public IEnumerator AdjustPrice(
        int itemId,
        float newPrice,
        Action<MarketPriceData> onSuccess,
        Action<string> onFail = null)
    {
        if (!IsReady()) { onFail?.Invoke("ApiManager 없음"); yield break; }

        MarketPriceReqBody body = new MarketPriceReqBody
        {
            item_id = itemId,
            new_price = newPrice
        };

        string json = JsonUtility.ToJson(body);

        yield return ApiManager.Instance.Patch("/market/price", json,
            response =>
            {
                MarketPriceEnvelope wrapper = JsonUtility.FromJson<MarketPriceEnvelope>(response);
                if (wrapper != null && wrapper.data != null)
                    onSuccess?.Invoke(wrapper.data);
                else
                    onFail?.Invoke("price 응답 파싱 실패: " + response);
            },
            error =>
            {
                Debug.LogWarning("[MarketServerManager] price 실패\n" + error);
                onFail?.Invoke(error);
            });
    }

    public IEnumerator SellItem(
        int itemId,
        int quantity,
        float discountRate,
        Action<MarketSellData> onSuccess,
        Action<string> onFail = null)
    {
        if (!IsReady()) { onFail?.Invoke("ApiManager 없음"); yield break; }

        MarketSellReqBody body = new MarketSellReqBody
        {
            item_id = itemId,
            quantity = Mathf.Max(1, quantity),
            discount_rate = Mathf.Clamp(discountRate, 0f, 0.7f)
        };

        string json = JsonUtility.ToJson(body);

        if (verboseLog)
            Debug.Log("[MarketServerManager] POST /market/sell\n" + json);

        yield return ApiManager.Instance.Post("/market/sell", json,
            response =>
            {
                if (verboseLog)
                    Debug.Log("[MarketServerManager] /market/sell 응답\n" + response);

                MarketSellEnvelope wrapper = JsonUtility.FromJson<MarketSellEnvelope>(response);
                if (wrapper != null && wrapper.data != null)
                    onSuccess?.Invoke(wrapper.data);
                else
                    onFail?.Invoke("sell 응답 파싱 실패: " + response);
            },
            error =>
            {
                Debug.LogWarning("[MarketServerManager] sell 실패\n" + error);
                onFail?.Invoke(error);
            });
    }

    public IEnumerator GetSettlement(
        int seasonId,
        Action<MarketSettlementData> onSuccess,
        Action<string> onFail = null)
    {
        if (!IsReady()) { onFail?.Invoke("ApiManager 없음"); yield break; }

        yield return ApiManager.Instance.Get("/market/settlement/" + seasonId,
            response =>
            {
                MarketSettlementEnvelope wrapper = JsonUtility.FromJson<MarketSettlementEnvelope>(response);
                if (wrapper != null && wrapper.data != null)
                    onSuccess?.Invoke(wrapper.data);
                else
                    onFail?.Invoke("settlement 응답 파싱 실패: " + response);
            },
            error =>
            {
                Debug.LogWarning("[MarketServerManager] settlement 실패\n" + error);
                onFail?.Invoke(error);
            });
    }
}
