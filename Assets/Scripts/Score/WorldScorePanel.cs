using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class WorldScorePanel : MonoBehaviour
{
    public List<WorldScoreUnit> units = new();
    private const string TargetStatistic = "HighScore";

    void OnEnable()
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
            MaxResultsCount = 10,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true
            }
        };

        PlayFabClientAPI.GetLeaderboard(request, result =>
        {
            string myPlayFabId = PlayFabSettings.staticPlayer.PlayFabId;
            PlayerLeaderboardEntry myEntry = null;

            // 1. 내 엔트리 찾기
            foreach (var entry in result.Leaderboard)
            {
                if (entry.PlayFabId == myPlayFabId)
                {
                    myEntry = entry;
                    break;
                }
            }

            int uiIndex = 0;

            // 2. 첫 번째 유닛 = 내 점수
            if (myEntry != null && units.Count > 0)
            {
                string myName = string.IsNullOrEmpty(myEntry.DisplayName)
                    ? myEntry.PlayFabId
                    : myEntry.DisplayName;

                units[0].SetCondition(
                    myEntry.Position + 1,
                    myName,
                    myEntry.StatValue
                );
                units[0].gameObject.SetActive(true);
                uiIndex = 1;
            }

            // 3. 나머지 유닛 = 월드 랭킹 (내 항목 제외)
            foreach (var entry in result.Leaderboard)
            {
                if (uiIndex >= units.Count) break;
                if (entry.PlayFabId == myPlayFabId) continue;

                string displayName = string.IsNullOrEmpty(entry.DisplayName)
                    ? entry.PlayFabId
                    : entry.DisplayName;

                units[uiIndex].SetCondition(
                    entry.Position + 1,
                    displayName,
                    entry.StatValue
                );
                units[uiIndex].gameObject.SetActive(true);
                uiIndex++;
            }

        }, error =>
        {
            Debug.LogError("리더보드 로드 실패: " + error.GenerateErrorReport());
        });
    }

    private void ClearUI()
    {
        foreach (var unit in units)
        {
            unit.gameObject.SetActive(false);
        }
    }
}
