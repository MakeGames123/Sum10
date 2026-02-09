using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
public class AdDiamond : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] string currencyCode = "DM"; // PlayFab 가상화폐 코드
    [SerializeField] int rewardAmount = 20;
    int rewardCount = 2;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] PlayFabLoginManager login;
    void Start()
    {
        login.onLogined.AddListener(GetDiamondCount);
    }

    public void OnPointerClick(PointerEventData data)
    {
        TryAdDiamond();
    }

    public void TryAdDiamond()
    {
        if(rewardCount <= 0) return;

        AdMobManager.Instance.ShowRewardedAd(AdDiamondReward);
    }
    private void AdDiamondReward(bool flag)
    {
        if (!flag) return;

        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "RewardDiamondByAd"
            },
            result =>
            {
                var data = result.FunctionResult as IDictionary<string, object>;
                rewardCount = System.Convert.ToInt32(data["remaining"]);
                PlayerData.Instance.AdjustDiamone(20);
                countText.text = $"[{rewardCount}/2]";
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            }
        );
    }
    private void GetDiamondCount()
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "GetAdDiamondStatus"
            },
            result =>
            {
                var data = result.FunctionResult as IDictionary<string, object>;
                rewardCount = System.Convert.ToInt32(data["remaining"]);
                countText.text = $"[{rewardCount}/2]";
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            }
        );
    }
}
