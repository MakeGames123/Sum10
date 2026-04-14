using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class AdRemoveButton : MonoBehaviour
{
    public PlayFabLoginManager login;
    [SerializeField] Image image;
    [SerializeField] TextMeshProUGUI text;
    private const string REMOVE_ADS_KEY = "removeAds";

    private void Awake()
    {
        login.onLogined.AddListener(CheckRemoveAdsStatus);
    }

    private void CheckRemoveAdsStatus()
    {
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest
            {
                Keys = new List<string> { REMOVE_ADS_KEY }
            },
            result =>
            {
                bool isAdRemoved = false;
                if (result.Data != null &&
                    result.Data.TryGetValue(REMOVE_ADS_KEY, out var value) &&
                    value.Value == "1")
                {
                    isAdRemoved = true;
                }

                ApplyRemoveAdsStatus(isAdRemoved);
            },
            error =>
            {
                Debug.LogError("광고 제거 상태 조회 실패: " + error.GenerateErrorReport());
            }
        );
    }

    private void ApplyRemoveAdsStatus(bool removed)
    {
        //Debug.Log(removed);
        // 이미 구매했다면 버튼 비활성화
        image.enabled = !removed;
        text.enabled = !removed;
        PlayerData.Instance.SetAdStatus(removed);
    }
}
