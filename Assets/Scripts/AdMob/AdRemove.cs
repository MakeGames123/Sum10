using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class AdRemove : MonoBehaviour
{
    [SerializeField] Button diamondRemoveButton;
    [SerializeField] Button cashRemoveButton;
    [SerializeField] AdRemoveButton adRemoveButton;
    public const string Product_NoAds = "ad_removal_1";
    void Awake()
    {
        diamondRemoveButton.onClick.AddListener(BuyRemoveAdsWithDia);
    }
    private void Start()
    {
        if (adRemoveButton == null)
            adRemoveButton = FindObjectOfType<AdRemoveButton>();
    }
    public void BuyRemoveAdsWithDia()
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "BuyRemoveAdsWithDiamond"
            },
            OnSuccess,
            OnError
        );
    }
    public void BuyRemoveAdsWithCash()
    {
        IAPManager.Instance.BuyProduct(Product_NoAds, OnSuccess);
    }

    private void OnSuccess(ExecuteCloudScriptResult result)
    {
        var data = result.FunctionResult as IDictionary<string, object>;
        if (data == null) return;

        bool success = (bool)data["success"];
        if (!success)
        {
            Debug.Log("이미 광고 제거 구매됨 혹은 다이아 부족");
            return;
        }

        int remaining = System.Convert.ToInt32(data["remaining"]);

        PlayerData.Instance.SetDiamond(remaining);
        PlayerData.Instance.SetAdStatus(true);

        Debug.Log("광고 제거 구매 완료");
        adRemoveButton.gameObject.SetActive(false);
        //배너 제거
        gameObject.SetActive(false);
    }
    private void OnSuccess()
    {
        PlayerData.Instance.SetAdStatus(true);

        Debug.Log("광고 제거 구매 완료");
        adRemoveButton.gameObject.SetActive(false);
        //배너 제거
        gameObject.SetActive(false);
    }


    private void OnError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }
}
