using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
public class ProfileGroup : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image profileImage;
    public List<Sprite> profileSprites = new();
    public GameObject panel;
    [SerializeField] TextMeshProUGUI nickName;
    [SerializeField] TextMeshProUGUI rank;
    public PlayFabLoginManager login;
    void Awake()
    {
        login.onLogined.AddListener(UpdateProfile);
    }
    public void OnPointerClick(PointerEventData data)
    {
        AudioManager.Instance?.PlayButtonSFX();
        panel.SetActive(true);
    }
    public void UpdateProfile()
    {
        PlayFabClientAPI.GetAccountInfo(
            new GetAccountInfoRequest(),
            result =>
            {
                nickName.text = result.AccountInfo.TitleInfo.DisplayName;
                if (int.TryParse(result.AccountInfo.TitleInfo.AvatarUrl, out int index))
                profileImage.sprite = profileSprites[index];
                PlayerData.Instance.EquippedProfileImage = index;
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
                rank.text = "#" + (entry.Position + 1).ToString(); // 0-based → 1-based
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
