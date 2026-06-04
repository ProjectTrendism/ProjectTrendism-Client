using System;

/// <summary>
/// 판매(Market) 서버 연동 DTO.
/// 서버 app/schemas/market.py, app/routers/market.py 기준.
/// JsonUtility 호환을 위해 모든 클래스에 [Serializable] 적용.
/// </summary>

// ===============================
// Request bodies
// ===============================

[Serializable]
public class MarketRegisterReqBody
{
    public string item_name;
    public int[] keyword_ids;
    public string grade;
    public float base_value;
    public int stock = 1;
    public int release_day = 0;
}

[Serializable]
public class MarketSellReqBody
{
    public int item_id;
    public int quantity = 1;
    public float discount_rate = 0f;
}

[Serializable]
public class MarketPriceReqBody
{
    public int item_id;
    public float new_price;
}

[Serializable]
public class MarketAdjustNodeReqBody
{
    public string node;
    public float new_value;
}

// ===============================
// Common / register
// ===============================

[Serializable]
public class MarketItemDto
{
    public int id;
    public string item_name;
    public string grade;
    public float base_value;
    public int stock;
    public int release_day;
    public string status;
}

[Serializable]
public class MarketRegisterEnvelope
{
    public string status;
    public MarketItemDto data;
}

// ===============================
// Trend
// ===============================

[Serializable]
public class MarketTrendPoint
{
    public int day;
    public float index;
}

[Serializable]
public class MarketTrendData
{
    public int item_id;
    public string item_name;
    public string grade;
    public float current_index;
    public MarketTrendPoint[] chart_data;
}

[Serializable]
public class MarketTrendEnvelope
{
    public string status;
    public MarketTrendData data;
}

// ===============================
// Sell
// ===============================

[Serializable]
public class MarketSellData
{
    public float revenue;
    public int remaining_stock;
    public float trend_index;
}

[Serializable]
public class MarketSellEnvelope
{
    public string status;
    public MarketSellData data;
}

// ===============================
// Price
// ===============================

[Serializable]
public class MarketPriceData
{
    public int item_id;
    public string item_name;
    public float old_price;
    public float new_price;
    public float change_percent;
    public string message;
}

[Serializable]
public class MarketPriceEnvelope
{
    public string status;
    public MarketPriceData data;
}

// ===============================
// Settlement
// ===============================

[Serializable]
public class MarketSettlementData
{
    public int season_id;
    public float total_revenue;
    public float material_cost;
    public float rent_cost;
    public float marketing_cost;
    public float management_cost;
    public float net_profit;
    public bool penalty;
}

[Serializable]
public class MarketSettlementEnvelope
{
    public string status;
    public MarketSettlementData data;
}

// ===============================
// Analyze
// ===============================

[Serializable]
public class MarketIssueDto
{
    public string type;
    public string severity;
    public string message;
}

[Serializable]
public class MarketServerAnalysisDto
{
    public MarketIssueDto[] issues;
    public string[] suggestions;
    public int overall_score;
    public string trend_status;
    public string optimal_sell_window;
}

[Serializable]
public class MarketAIAnalysisDto
{
    public string summary;
    public string keyword_analysis;
    public string timing_analysis;
    public string price_analysis;
    public string next_action;
    public int score;
}

[Serializable]
public class MarketAnalyzeData
{
    public int item_id;
    public string item_name;
    public MarketServerAnalysisDto server_analysis;
    public MarketAIAnalysisDto ai_analysis;
}

[Serializable]
public class MarketAnalyzeEnvelope
{
    public string status;
    public MarketAnalyzeData data;
}

// ===============================
// Simulate
// ===============================

[Serializable]
public class MarketSimulationSummary
{
    public int total_sold;
    public int remaining_stock;
    public float total_revenue;
    public int sellout_day;
    public int peak_buyers_day;
    public int peak_buyers_count;
}

[Serializable]
public class MarketSimulationDay
{
    public int day;
    public float trend_index;
    public int buyers_visited;
    public int units_sold;
    public int remaining_stock;
    public float cumulative_revenue;
}

[Serializable]
public class MarketSimulateData
{
    public int item_id;
    public string item_name;
    public string grade;
    public int initial_stock;
    public MarketSimulationSummary summary;
    public MarketSimulationDay[] daily_data;
}

[Serializable]
public class MarketSimulateEnvelope
{
    public string status;
    public MarketSimulateData data;
}
