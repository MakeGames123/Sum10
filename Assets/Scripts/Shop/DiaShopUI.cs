using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiaShopUI : MonoBehaviour
{
    [SerializeField] private Transform productGrid;

    void Start()
    {
        if (ShopDataLoader.Instance == null)
        {
            Debug.LogWarning("DiaShopUI: ShopDataLoader.Instance가 null");
            return;
        }

        if (ShopDataLoader.Instance.Items.Count > 0)
        {
            UpdateUI();
        }

        ShopDataLoader.Instance.OnDataLoaded += UpdateUI;
    }

    private void UpdateUI()
    {
        if (productGrid == null)
        {
            Debug.LogError("DiaShopUI: productGrid가 null!");
            return;
        }

        var items = ShopDataLoader.Instance.Items;

        foreach (var item in items)
        {
            Transform product = productGrid.Find($"Product_{item.slot}");
            if (product == null) continue;

            // BonusTag > BonusText
            var bonusTag = product.Find("BonusTag");
            if (bonusTag != null)
            {
                var bonusText = bonusTag.Find("BonusText");
                var tmp = bonusText != null ? bonusText.GetComponent<TextMeshProUGUI>() : null;

                if (item.bonusTag == "-" || string.IsNullOrEmpty(item.bonusTag))
                {
                    bonusTag.gameObject.SetActive(false);
                }
                else
                {
                    bonusTag.gameObject.SetActive(true);
                    if (tmp != null) tmp.text = item.bonusTag;
                }
            }

            // AmountText
            var amountArea = product.Find("AmountArea");
            if (amountArea != null)
            {
                var amountText = amountArea.Find("AmountText");
                if (amountText != null)
                {
                    var tmp = amountText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = item.displayString;
                    }
                    LayoutRebuilder.ForceRebuildLayoutImmediate(amountArea.GetComponent<RectTransform>());
                }
            }

            // Button_Price > PriceText
            var buttonPrice = product.Find("Button_Price");
            if (buttonPrice != null)
            {
                var priceText = buttonPrice.Find("PriceText");
                if (priceText != null)
                {
                    var tmp = priceText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = item.priceText;
                    }
                }
            }
        }
    }

    void OnDestroy()
    {
        if (ShopDataLoader.Instance != null)
        {
            ShopDataLoader.Instance.OnDataLoaded -= UpdateUI;
        }
    }
}
