using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CraftedItemListUI : MonoBehaviour
{
    public TextMeshProUGUI craftedItemListText;

    private void Start()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        if (craftedItemListText == null) return;

        if (CraftedItemManager.Instance == null)
        {
            craftedItemListText.text = "제작 결과 매니저 없음";
            return;
        }

        List<CraftedItemResult> items = CraftedItemManager.Instance.GetCraftedItems();

        if (items.Count == 0)
        {
            craftedItemListText.text = "제작 완료 아이템 없음";
            return;
        }

        string text = "제작 완료 아이템 목록\n\n";

        for (int i = 0; i < items.Count; i++)
        {
            string correctionText = "";

            if (items[i].isCorrected)
            {
                correctionText = " / " + items[i].correctionType;
            }

            text += "- " + items[i].itemName +
                    " / " + items[i].grade +
                    " / " + items[i].finalPrice + "G" +
                    correctionText + "\n";
        }

        craftedItemListText.text = text;
    }
}