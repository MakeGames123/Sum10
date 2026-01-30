using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public const string STAT_HIGH_SCORE = "HighScore";

    // 이전 주간 순위 (게임 시작 시 저장)
    private int previousWeeklyRank = -1;
    public int PreviousWeeklyRank => previousWeeklyRank;

    // 이전 전체 최고기록 (게임 시작 시 저장)
    private int previousHighScore = 0;
    public int PreviousHighScore => previousHighScore;

    // 점수 서버 저장 (Task 기반) - 전체 + 주간 동시 저장
    public Task<bool> SubmitScoreAsync(int score)
    {
        var tcs = new TaskCompletionSource<bool>();

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = STAT_HIGH_SCORE,
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(
            request,
            result =>
            {
                Debug.Log("점수 전송 완료! (전체 + 주간 리더보드)");
                tcs.TrySetResult(true);
            },
            error =>
            {
                Debug.LogError("전송 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(false);
            }
        );

        var request2 = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { STAT_HIGH_SCORE, score.ToString() }
        }
        };

        PlayFabClientAPI.UpdateUserData(request2, null, error =>
        {
            tcs.TrySetResult(false);
            Debug.LogError("하이스코어(UserData) 저장 실패: " + error.GenerateErrorReport());
        });

        return tcs.Task;
    }

    public Task<int> GetMyHighScoreAsync()
    {
        var tcs = new TaskCompletionSource<int>();

        var request = new GetUserDataRequest
        {
            Keys = new List<string> { STAT_HIGH_SCORE }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                int highScore = 0;

                if (result.Data != null &&
                    result.Data.TryGetValue(STAT_HIGH_SCORE, out var data))
                {
                    int.TryParse(data.Value, out highScore);
                }

                tcs.TrySetResult(highScore);
            },
            error =>
            {
                Debug.LogError("하이스코어(UserData) 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(0);
            }
        );

        return tcs.Task;
    }


    /// <summary>
    /// 내 주간 순위 조회 및 저장
    /// </summary>
    public Task<int> GetMyWeeklyRankAsync()
    {
        var tcs = new TaskCompletionSource<int>();

        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = STAT_HIGH_SCORE,
            MaxResultsCount = 1
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            request,
            result =>
            {
                int rank = -1;
                string myId = PlayFabSettings.staticPlayer.PlayFabId;
                foreach (var entry in result.Leaderboard)
                {
                    if (entry.PlayFabId == myId)
                    {
                        rank = entry.Position + 1;  // 1-based
                        break;
                    }
                }
                tcs.TrySetResult(rank);
            },
            error =>
            {
                Debug.LogError("주간 순위 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(-1);
            }
        );

        return tcs.Task;
    }

    /// <summary>
    /// 이전 순위 저장 (게임 시작 시 호출)
    /// </summary>
    public async Task SavePreviousWeeklyRankAsync()
    {
        previousWeeklyRank = await GetMyWeeklyRankAsync();
        Debug.Log($"이전 주간 순위 저장: {previousWeeklyRank}");
    }

    /// <summary>
    /// 이전 전체 최고기록 저장 (게임 시작 시 호출)
    /// </summary>
    public async Task SavePreviousHighScoreAsync()
    {
        previousHighScore = await GetMyHighScoreAsync();
        Debug.Log($"이전 전체 최고기록 저장: {previousHighScore}");
    }

    /// <summary>
    /// 주간 리더보드 범위 조회 (fromRank ~ toRank)
    /// </summary>
    public Task<List<PlayerLeaderboardEntry>> GetWeeklyLeaderboardRangeAsync(int fromRank, int toRank)
    {
        var tcs = new TaskCompletionSource<List<PlayerLeaderboardEntry>>();

        // PlayFab은 0-based index 사용
        int startPosition = Mathf.Max(0, Mathf.Min(fromRank, toRank) - 1);
        int count = Mathf.Abs(toRank - fromRank) + 1;
        count = Mathf.Min(count, 100);  // PlayFab 최대 100개

        var request = new GetLeaderboardRequest
        {
            StatisticName = STAT_HIGH_SCORE,
            StartPosition = startPosition,
            MaxResultsCount = count
        };

        PlayFabClientAPI.GetLeaderboard(
            request,
            result =>
            {
                tcs.TrySetResult(result.Leaderboard);
            },
            error =>
            {
                Debug.LogError("주간 리더보드 범위 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(new List<PlayerLeaderboardEntry>());
            }
        );

        return tcs.Task;
    }
}
