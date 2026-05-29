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
    [SerializeField] TextMeshProUGUI claimTextTMP;
    [SerializeField] Button button;
    [SerializeField] PlayFabLoginManager login;
    [SerializeField] DiaPopup popup;

    DateTime nextAvailableTime;
    bool isCooldown = false;
    const int COOLDOWN_MINUTES = 30;
    const string CooldownKey = "AdDiamond_NextTime";
    string freeText;
    string claimText;

    void Start()
    {
        login.onLogined.AddListener(GetDiamondCount);
        button.onClick.AddListener(TryAdDiamond);

        if (LocalizationLoader.Instance == null)
        {
            LocalizationLoader.Instance.OnLocalizationLoaded += GetText;
        }
        else if (LocalizationLoader.Instance.isLoaded)
        {
            GetText();
        }
        else
        {
            LocalizationLoader.Instance.OnLocalizationLoaded += GetText;
        }

        LoadCooldown();
    }
    void GetText()
    {
        if (LocalizationLoader.Instance == null) return;

        string freeText = LocalizationLoader.Instance.GetText(64);


        // 3. 쿨타임이 아닐 때만 버튼 텍스트를 무료 텍스트로 즉시 갱신
        if (!isCooldown)
        {
            buttonText.text = freeText;
        }

        // 수령/보상 텍스트 (ID: 62)
        claimText = LocalizationLoader.Instance.GetText(62);
        // 2. 중괄호 포맷 {0}이 포함되어 있으면 rewardAmount를 결합하여 저장
        if (claimText.Contains("{0}"))
        {
            claimTextTMP.text = string.Format(claimText, rewardAmount);
        }
    }
    void Update()
    {
        if (!isCooldown) return;

        TimeSpan remain = nextAvailableTime - DateTime.UtcNow;

        if (remain.TotalSeconds <= 0)
        {
            isCooldown = false;
            buttonText.text = freeText;
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
            buttonText.text = freeText;
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