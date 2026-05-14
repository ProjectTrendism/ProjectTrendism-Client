using TMPro;
using UnityEngine;

public class TrendZone : MonoBehaviour
{
    [Header("구역 텍스트")]
    public TMP_Text zoneTitleText;
    public TMP_Text populationText;

    [Header("사람들이 생성될 위치")]
    public Transform peopleParent;

    [Header("현재 구역 데이터")]
    public string keyword;
    public int trendScore;
    public int population;

    public void SetData(string newKeyword, int newTrendScore, int newPopulation)
    {
        keyword = newKeyword;
        trendScore = newTrendScore;
        population = newPopulation;

        if (zoneTitleText != null)
        {
            zoneTitleText.text = keyword;
        }

        if (populationText != null)
        {
            if (population > 0)
            {
                populationText.text = "관심 인구: " + population + "명";
            }
            else
            {
                populationText.text = "관심 인구: 0명";
            }
        }
    }

    public bool HasValidData()
    {
        if (string.IsNullOrEmpty(keyword)) return false;
        if (keyword == "데이터 없음") return false;
        if (keyword == "분석 데이터 없음") return false;
        if (population <= 0) return false;

        return true;
    }
}