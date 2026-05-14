using UnityEngine;

public class ScanHintObject : MonoBehaviour
{
    [TextArea(2, 4)]
    public string scanHintText;

    public string relatedKeyword;
    public bool consumed = false;

    public void RevealHint()
    {
        if (consumed) return;

        Debug.Log("스캔 힌트 발견: " + scanHintText);

        if (RumorManager.Instance != null)
        {
            RumorData data = new RumorData
            {
                rumorId = gameObject.name + "_" + relatedKeyword,
                rumorText = scanHintText,
                sourceNPC = gameObject.name,
                zoneName = "Hidden",
                relatedKeyword = relatedKeyword,
                isRareHint = true,
                reliability = 90,
                trendWeight = 3,
                rumorType = "스캔 힌트"
            };

            RumorManager.Instance.AddRumor(data);
        }

        consumed = true;
    }
}