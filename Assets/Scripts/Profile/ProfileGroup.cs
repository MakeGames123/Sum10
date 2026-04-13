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
    public ProfilePanel panel;
    [SerializeField] TextMeshProUGUI nickName;
    [SerializeField] TextMeshProUGUI rank;
    [SerializeField] GameManager game;
    [SerializeField] ScoreManager scoreManager;
    public PlayFabLoginManager login;
    void Awake()
    {
        login.onLogined.AddListener(UpdateProfile);
        game.OnGameOver.AddListener((value) => UpdateProfile());

        // 랭킹 갱신 이벤트 구독 - 새로고침 버튼/게임오버 후 Top50 갱신 시 탑바 순위도 같이 갱신
        if (scoreManager != null)
        {
            scoreManager.OnTop50Updated += UpdateRank;
            scoreManager.OnOverallTop50Updated += UpdateRank;
            // 내 순위가 계산되면 즉시 반영 (PlayFab Position 갱신 지연 우회)
            scoreManager.OnMyRankUpdated += HandleMyRankUpdated;
        }
    }

    void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnTop50Updated -= UpdateRank;
            scoreManager.OnOverallTop50Updated -= UpdateRank;
            scoreManager.OnMyRankUpdated -= HandleMyRankUpdated;
        }
    }

    private void HandleMyRankUpdated(int newRank)
    {
        // ScoreManager가 계산한 최신 순위를 즉시 표시 (API 지연 우회)
        rank.text = "#" + newRank.ToString();
    }
    public void OnPointerClick(PointerEventData data)
    {
        AudioManager.Instance?.PlayButtonSFX();
        panel.gameObject.SetActive(true);
        panel.SetCondition();
    }
    public void UpdateProfile()
    {
        PlayFabClientAPI.GetAccountInfo(
            new GetAccountInfoRequest(),
            result =>
            {
                nickName.text = result.AccountInfo.TitleInfo.DisplayName;
                if (int.TryParse(result.AccountInfo.TitleInfo.AvatarUrl, out int index)) profileImage.sprite = ProfileList.Instance.profileList[index];//기본프로필 시작 : 저장된 프로필 시작
                else profileImage.sprite = ProfileList.Instance.profileList[0];
                PlayerData.Instance.EquippedProfileImage = index;
                PlayerData.Instance.nickName = result.AccountInfo.TitleInfo.DisplayName;
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            }
        );
        DelayUpdateRank();
    }
    public void DelayUpdateRank()
    {
        Invoke(nameof(UpdateRank), 1);
    }
    public void UpdateRank()
    {
        // ScoreManager에 최신 캐시 순위가 있으면 우선 사용 (PlayFab Position 갱신 지연 우회)
        if (scoreManager != null && scoreManager.CachedRank > 0)
        {
            rank.text = "#" + scoreManager.CachedRank.ToString();
            return;
        }

        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = "HighScore",
                MaxResultsCount = 1 // 내 순위만 필요
            },
            result =>
            {
                // 1. 리더보드 데이터가 있고, 2. 그 점수가 0보다 클 때만 순위 표시
                if (result.Leaderboard.Count > 0 && result.Leaderboard[0].StatValue > 0)
                {
                    var entry = result.Leaderboard[0];
                    rank.text = "#" + (entry.Position + 1).ToString(); // 0-based → 1-based
                }
                else
                {
                    // 데이터가 아예 없거나 점수가 0점인 경우
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
