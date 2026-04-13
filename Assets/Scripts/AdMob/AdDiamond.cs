using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class AdDiamond : MonoBehaviour
{
    [SerializeField] string currencyCode = "DM";
    [SerializeField] int rewardAmount = 10;
    int rewardCount = 3;

    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] Button button;
    [SerializeField] PlayFabLoginManager login;
    [SerializeField] DiaPopup popup;

    DateTime nextAvailableTime;
    bool isCooldown = false;
    const int COOLDOWN_MINUTES = 30;
    const string CooldownKey = "AdDiamond_NextTime";

    void Start()
    {
        login.onLogined.AddListener(GetDiamondCount);
        button.onClick.AddListener(TryAdDiamond);
        buttonText.text = "무료 보석";
        LoadCooldown();
    }

    void Update()
    {
        if (!isCooldown) return;

        TimeSpan remain = nextAvailableTime - DateTime.UtcNow;

        if (remain.TotalSeconds <= 0)
        {
            isCooldown = false;
            buttonText.text = "무료 보석";
            PlayerPrefs.DeleteKey(CooldownKey);
            return;
        }

        buttonText.text = $"{remain.Minutes:00}:{remain.Seconds:00}";
    }
    void LoadCooldown()
    {
        if (!PlayerPrefs.HasKey(CooldownKey))
        {
            isCooldown = false;
            buttonText.text = "무료 보석";
            return;
        }

        long savedTicks = long.Parse(PlayerPrefs.GetString(CooldownKey));
        nextAvailableTime = new DateTime(savedTicks, DateTimeKind.Utc);

        isCooldown = DateTime.UtcNow < nextAvailableTime;
        button.enabled = !isCooldown;
    }
    private void StartCooldown()
    {
        nextAvailableTime = DateTime.UtcNow.AddMinutes(30);
        isCooldown = true;
        button.enabled = false;

        PlayerPrefs.SetString(CooldownKey, nextAvailableTime.Ticks.ToString());
        PlayerPrefs.Save();
    }
    public void TryAdDiamond()
    {
        if (rewardCount <= 0 || isCooldown) return;

        if (PlayerData.Instance.GetAdStatus())
            AdDiamondReward(true);
        else
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
                PlayerData.Instance.AdjustDiamond(rewardAmount);

                popup.gameObject.SetActive(true);
                popup.SetCondition(rewardAmount);

                countText.text = $"[{rewardCount}/3]";

                // 쿨타임 시작
                nextAvailableTime = DateTime.UtcNow.AddMinutes(COOLDOWN_MINUTES);
                StartCooldown();
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
                countText.text = $"[{rewardCount}/3]";
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            }
        );
    }
}