using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;

public class DiaShopUI : MonoBehaviour
{
    [SerializeField] private Transform productGrid;
    [SerializeField] private DiaPopup diaPopup;

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
    public void UpdateUI()
    {
        if (productGrid == null)
        {
            Debug.LogError("DiaShopUI: productGrid가 null!");
            return;
        }

        var items = ShopDataLoader.Instance.Items;

        var firstItem = items[0];
        SetupFreeBonusButton(firstItem);

        foreach (var item in items.Skip(1))
        {
            Transform product = productGrid.Find($"Product_{item.slot}");
            if (product == null) continue;

            // BonusTag > BonusText

            UpdateProductSlotUI(product, item);

            var button = product.GetComponent<Button>();

            string id = item.productId;
            ShopItemData data = item;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => IAPManager.Instance.BuyProduct(id, () => OnSuccess(data)));
        }
    }
    private void SetupFreeBonusButton(ShopItemData data)
    {
        Transform product = productGrid.Find($"Product_{0}");
        Button freeBonusButton = product.GetComponent<Button>();
        freeBonusButton.onClick.RemoveAllListeners();

        UpdateProductSlotUI(product, data);

        var freeBonusStatusText = product.Find("Button_Price").Find("PriceText").GetComponent<TextMeshProUGUI>();

        // PlayFab에서 유저 데이터를 가져와 상태 체크
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            bool isPaid = result.Data.ContainsKey("IsPaidUser") && result.Data["IsPaidUser"].Value == "1";
            bool isClaimed = result.Data.ContainsKey("FirstBonusClaimed") && result.Data["FirstBonusClaimed"].Value == "1";

            if (!isPaid)
            {
                freeBonusButton.interactable = false;
                if (freeBonusStatusText) freeBonusStatusText.text = "결제 후 활성화";
            }
            else if (isClaimed)
            {
                freeBonusButton.interactable = false;
                if (freeBonusStatusText) freeBonusStatusText.text = "수령 완료";
            }
            else
            {
                freeBonusButton.interactable = true;
                if (freeBonusStatusText) freeBonusStatusText.text = "무료";

                // 버튼 클릭 시 CloudScript 호출
                freeBonusButton.onClick.AddListener(() =>
                {
                    PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
                    {
                        FunctionName = "ClaimPaidBonus"
                    }, result =>
                    {
                        OnSuccess(data); // 성공 시 다이아 지급 연출
                    }, null);
                });
            }
        }, null);
    }
    private void UpdateProductSlotUI(Transform product, ShopItemData item)
    {
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

    void OnDestroy()
    {
        if (ShopDataLoader.Instance != null)
        {
            ShopDataLoader.Instance.OnDataLoaded -= UpdateUI;
        }
    }
    void OnSuccess(ShopItemData data)
    {
        diaPopup.gameObject.SetActive(true);
        diaPopup.SetCondition(data.totalQty);
        PlayerData.Instance.AdjustDiamond(data.totalQty);
        UpdateUI();
    }
}
