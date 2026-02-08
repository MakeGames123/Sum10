using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
public interface IMainPanel
{
    void SetCondition();
    public void OnDisable();
}
public class WorldScorePanel : MonoBehaviour, IMainPanel
{
    public List<WorldScoreUnit> units = new();
    public List<PodiumUnit> podiumUnits = new();
    private const string TargetStatistic = "HighScore";
    public GameManager gameManager;

    public void SetCondition()
    {
        gameManager.OnGameOver.AddListener((val) => LoadWorldLeaderboard());
        UpdateRank();
    }

    public void OnDisable() { }

    public void UpdateRank()
    {
        ClearUI();
        LoadWorldLeaderboard();
    }

    private void LoadWorldLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = TargetStatistic,
            StartPosition = 0,
            MaxResultsCount = 50,
            // 핵심: 프로필 제약 조건을 설정하여 AvatarUrl(인덱스 저장용)을 같이 가져옴
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true,
                ShowAvatarUrl = true 
            }
        };

        PlayFabClientAPI.GetLeaderboard(request, result =>
        {
            string myPlayFabId = PlayFabSettings.staticPlayer.PlayFabId;
            // 이제 추가 API 호출 없이 바로 그립니다.
            DrawLeaderboard(result.Leaderboard, myPlayFabId);
        }, error =>
        {
            Debug.LogError("리더보드 로드 실패: " + error.GenerateErrorReport());
        });
    }

    private void DrawLeaderboard(List<PlayerLeaderboardEntry> leaderboard, string myPlayFabId)
    {
        if (leaderboard == null) return;

        int uiIndex = 0;
        PlayerLeaderboardEntry myEntry = leaderboard.Find(e => e.PlayFabId == myPlayFabId);

        // 1. 내 점수 (리더보드에 내가 있을 경우)
        if (myEntry != null && units.Count > 0)
        {
            units[0].SetCondition(
                myEntry.Position + 1,
                GetPlayerName(myEntry),
                myEntry.StatValue,
                PlayerData.Instance.EquippedProfileImage
            );
            units[0].gameObject.SetActive(true);
            uiIndex = 1;
        }

        // 2. 월드 랭킹 및 시상대 처리
        for (int i = 0; i < leaderboard.Count; i++)
        {
            var entry = leaderboard[i];
            int profileIndex = GetProfileIndex(entry);
            string playerName = GetPlayerName(entry);

            // 리스트 UI 업데이트
            if (uiIndex < units.Count)
            {
                units[uiIndex].SetCondition(entry.Position + 1, playerName, entry.StatValue, profileIndex);
                units[uiIndex].gameObject.SetActive(true);
                uiIndex++;
            }

            // 시상대 UI 업데이트 (상위 3명)
            if (i < 3 && i < podiumUnits.Count)
            {
                podiumUnits[i].SetCondition(playerName, profileIndex);
            }
        }
    }

    // 이름 결정 로직 유틸리티
    private string GetPlayerName(PlayerLeaderboardEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.DisplayName)) return entry.DisplayName;
        if (entry.Profile != null && !string.IsNullOrEmpty(entry.Profile.DisplayName)) return entry.Profile.DisplayName;
        return entry.PlayFabId;
    }

    // AvatarUrl에서 인덱스 추출 유틸리티
    private int GetProfileIndex(PlayerLeaderboardEntry entry)
    {
        if (entry.Profile != null && !string.IsNullOrEmpty(entry.Profile.AvatarUrl))
        {
            if (int.TryParse(entry.Profile.AvatarUrl, out int index))
                return index;
        }
        return 0; // 기본값
    }

    private void ClearUI()
    {
        foreach (var unit in units) unit.gameObject.SetActive(false);
    }
}