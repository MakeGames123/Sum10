using TMPro;
using UnityEngine;

public class AdRemoveShopUI : MonoBehaviour
{
    void Start()
    {
        if (ShopDataLoader.Instance == null) return;

        if (ShopDataLoader.Instance.AdRemoveData.Count > 0)
            UpdateUI();

        ShopDataLoader.Instance.OnDataLoaded += UpdateUI;
    }

    private void UpdateUI()
    {
        var data = ShopDataLoader.Instance.AdRemoveData;
        if (data.Count == 0) return;

        // DiaPurchaseBtn
        var diaBtn = transform.Find("MainPanelArea/ContentArea/ButtonGroup/DiaPurchaseBtn");
        if (diaBtn != null)
        {
            // PriceSection > PriceText
            var priceText = diaBtn.Find("PriceSection/PriceText");
            if (priceText != null)
            {
                var tmp = priceText.GetComponent<TextMeshProUGUI>();
                if (tmp != null && data.ContainsKey("diaPrice"))
                    tmp.text = data["diaPrice"];
            }

            // PriceSection > OriginalSection
            var originalSection = diaBtn.Find("PriceSection/OriginalSection");
            if (originalSection != null)
            {
                if (data.ContainsKey("diaOriginalPrice") && data["diaOriginalPrice"] != "-")
                {
                    originalSection.gameObject.SetActive(true);
                    var origText = originalSection.Find("OriginalPriceText");
                    if (origText != null)
                    {
                        var tmp = origText.GetComponent<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = data["diaOriginalPrice"];
                    }
                }
                else
                {
                    originalSection.gameObject.SetActive(false);
                }
            }
        }

        // CashPurchaseBtn
        var cashBtn = transform.Find("MainPanelArea/ContentArea/ButtonGroup/CashPurchaseBtn");
        if (cashBtn != null)
        {
            // PriceText (직접 자식)
            var priceText = cashBtn.Find("PriceText");
            if (priceText != null)
            {
                var tmp = priceText.GetComponent<TextMeshProUGUI>();
                if (tmp != null && data.ContainsKey("cashPrice"))
                    tmp.text = data["cashPrice"];
            }

            // OriginalPriceText (직접 자식)
            var origPriceText = cashBtn.Find("OriginalPriceText");
            if (origPriceText != null)
            {
                if (data.ContainsKey("cashOriginalPrice") && data["cashOriginalPrice"] != "-")
                {
                    origPriceText.gameObject.SetActive(true);
                    var tmp = origPriceText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = data["cashOriginalPrice"];
                }
                else
                {
                    origPriceText.gameObject.SetActive(false);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (ShopDataLoader.Instance != null)
            ShopDataLoader.Instance.OnDataLoaded -= UpdateUI;
    }
}
