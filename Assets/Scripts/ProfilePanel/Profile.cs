using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class Profile : MonoBehaviour
{
    [SerializeField]TextMeshProUGUI nickName;
    [SerializeField]TextMeshProUGUI rank;
    public PlayFabLoginManager login;
    void Awake()
    {
        login.onLogined.AddListener(UpdateProfile);
    }

    public void UpdateProfile()
    {
        PlayFabClientAPI.GetAccountInfo(
            new GetAccountInfoRequest(),
            result =>
            {
                nickName.text = result.AccountInfo.TitleInfo.DisplayName;
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            }
        );

        PlayFabClientAPI.GetLeaderboardAroundPlayer(
        new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = "HighScore",
            MaxResultsCount = 1 // 내 순위만 필요
        },
        result =>
        {
            if (result.Leaderboard.Count > 0)
            {
                var entry = result.Leaderboard[0];
                rank.text = "#"+(entry.Position + 1).ToString(); // 0-based → 1-based
            }
            else
            {
                rank.text = "#--";
            }
        },
        error =>
        {
            Debug.LogError(error.GenerateErrorReport());
        }
    );
    }
}
