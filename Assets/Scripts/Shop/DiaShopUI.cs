using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using PlayFab;
using PlayFab.ClientModels;

public class DiaShopUI : MonoBehaviour
{
    [SerializeField] private Transform productGrid;
    [SerializeField] private DiaPopup diaPopup;
    [SerializeField] private PlayFabLoginManager login;
    [SerializeField] BoardManager skinBoard;
    [SerializeField] GameObject linkPupUp;
    private bool isShopDataReady = false;
    private bool isLoginReady = false;

    void Start()
    {
        if (ShopDataLoader.Instance == null)
        {
            Debug.LogWarning("DiaShopUI: ShopDataLoader.Instance가 null");
            return;
        }

        // 시트 데이터 콜백 등록
        ShopDataLoader.Instance.OnDataLoaded += OnShopDataLoaded;

        // 로그인 콜백 등록
        login.onLogined.AddListener(OnLoginSuccess);

        // 이미 로드된 경우 대비
        if (ShopDataLoader.Instance.Items.Count > 0)
        {
            OnShopDataLoaded();
        }
    }
    void OnShopDataLoaded()
    {
        isShopDataReady = true;
        TryInitUI();
    }

    void OnLoginSuccess()
    {
        isLoginReady = true;
        TryInitUI();
    }
    void TryInitUI()
    {
        if (!isShopDataReady || !isLoginReady)
            return;

        UpdateUI();
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
            button.onClick.AddListener(() => OnClickNormal(id, data));
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

            // LocalizationManager가 살아있는지 체크하는 방어 조건문
            if (LocalizationLoader.Instance != null)
            {
                if (!isPaid)
                {
                    freeBonusButton.interactable = false;
                    // 예시 ID 24: "결제 후 활성화" (시트에 새로 등록 필요)
                    if (freeBonusStatusText) freeBonusStatusText.text = LocalizationLoader.Instance.GetText(29);
                }
                else if (isClaimed)
                {
                    freeBonusButton.interactable = false;
                    // 예시 ID 25: "수령 완료" (시트에 새로 등록 필요)
                    if (freeBonusStatusText) freeBonusStatusText.text = LocalizationLoader.Instance.GetText(58);
                }
                else
                {
                    freeBonusButton.interactable = true;
                    // 이미지 18번 ID: "적용하기/Apply" (또는 "무료"용 신규 ID 세팅)
                    if (freeBonusStatusText) freeBonusStatusText.text = LocalizationLoader.Instance.GetText(59);

                    // 버튼 클릭 시 CloudScript 호출
                    freeBonusButton.onClick.AddListener(() => OnClickFree(data));
                }
            }
            else
            {
                // 로컬라이즈 매니저가 없을 때의 최소한의 Fallback 조건문
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
                    freeBonusButton.onClick.AddListener(() => OnClickFree(data));
                }
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
                if (tmp != null)
                {
                    // 전역 매니저 클래스명(LocalizationManager)으로 통일 및 Null 체크
                    if (LocalizationLoader.Instance != null) 
                    { 
                        // 시트에서 "보너스 {0}" 또는 "Bonus +{0}" 형태의 포맷 텍스트를 가져옴
                        string rawBonusFormat = LocalizationLoader.Instance.GetText(item.bonusStringId);
                        
                        // 중괄호 포맷 문자가 포함되어 있다면 string.Format으로 bonusQty를 결합
                        if (rawBonusFormat.Contains("{0}"))
                        {
                            tmp.text = string.Format(rawBonusFormat, item.bonusQty);
                        }
                        else if (item.bonusQty > 0)
                        {
                            // 시트에 숫자가 생략된 텍스트만 있을 경우 뒤에 붙여주는 예외 방어 코드
                            tmp.text = $"{rawBonusFormat} {item.bonusQty}";
                        }
                        else
                        {
                            // bonusQty가 0이면 (예: 첫 구매 상품) 숫자 없이 시트 값 그대로
                            tmp.text = rawBonusFormat;
                        }
                    }
                    else  
                    { 
                        // 로컬라이즈 매니저가 없을 때 하드코딩 Fallback 처리 (한글 기본)
                        tmp.text = $"보너스 {item.bonusQty}"; 
                    }
                }
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
                    tmp.text =  LocalizationLoader.Instance.GetText(item.priceTextId);
                    Debug.Log(tmp.text);
                }
            }
        }
    }
    void OnClickNormal(string id, ShopItemData data)
    {
        if (!login.isLinked) linkPupUp.SetActive(true);
        else IAPManager.Instance.BuyProduct(id, () => OnSuccess(data));
    }
    void OnClickFree(ShopItemData data)
    {
        if (!login.isLinked) linkPupUp.SetActive(true);
        else
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {
                FunctionName = "ClaimPaidBonus"
            }, result =>
            {
                OnSuccess(data); // 성공 시 다이아 지급 연출
            }, null);
        }
    }
    public void Disable()
    {
        skinBoard.UnlockCells();
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
