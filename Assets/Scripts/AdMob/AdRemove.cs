using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class AdRemove : MonoBehaviour
{
    [SerializeField] Button diamondRemoveButton;
    [SerializeField] AdRemoveButton adRemoveButton;
    void Awake()
    {
        diamondRemoveButton.onClick.AddListener(BuyRemoveAds);
    }
    private void Start()
    {
        if (adRemoveButton == null)
            adRemoveButton = FindObjectOfType<AdRemoveButton>();
    }
    public void BuyRemoveAds()
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

    private void OnSuccess(ExecuteCloudScriptResult result)
    {
        var data = result.FunctionResult as IDictionary<string, object>;
        if (data == null) return;

        bool success = (bool)data["success"];
        if (!success)
        {
            Debug.Log("이미 광고 제거 구매됨");
            return;
        }

        PlayerData.Instance.AdjustDiamone(-15);
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
