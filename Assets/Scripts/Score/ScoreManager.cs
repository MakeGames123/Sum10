using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public const string STAT_HIGH_SCORE = "HighScore";

    // 이전 주간 순위 + 주간 최고점수 (게임 시작 시 저장)
    private int previousWeeklyRank = -1;
    private int previousWeeklyBestScore = 0;
    public int PreviousWeeklyRank => previousWeeklyRank;
    public int PreviousWeeklyBestScore => previousWeeklyBestScore;

    // 이전 전체 최고기록 (게임 시작 시 저장)
    private int previousHighScore = 0;
    public int PreviousHighScore => previousHighScore;

    // 게임오버 시 계산된 순위 캐시 (연속 플레이 시 PlayFab Position 지연 우회)
    private int cachedRank = -1;
    private int cachedRankScore = -1;

    /// <summary>
    /// 게임오버 시 호출: 계산된 순위를 캐시 (다음 게임 시작 시 사용)
    /// </summary>
    public void CacheCalculatedRank(int rank, int score)
    {
        cachedRank = rank;
        cachedRankScore = score;
        Debug.Log($"[캐시] 순위={rank}, 점수={score}");
    }

    /// <summary>
    /// 게임 시작 시 호출: 주간 순위 + 주간 최고점수를 한 번에 저장
    /// </summary>
    public async Task SavePreviousWeeklyRankAsync()
    {
        var tcs = new TaskCompletionSource<(int rank, int score)>();

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
                int score = 0;
                string myId = PlayFabSettings.staticPlayer.PlayFabId;
                foreach (var entry in result.Leaderboard)
                {
                    if (entry.PlayFabId == myId)
                    {
                        rank = entry.Position + 1;
                        score = entry.StatValue;
                        break;
                    }
                }
                tcs.TrySetResult((rank, score));
            },
            error =>
            {
                Debug.LogError("주간 순위 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult((-1, 0));
            }
        );

        var result = await tcs.Task;
        previousWeeklyBestScore = result.score; // StatValue는 항상 즉시 정확

        // 순위: 캐시가 있고 점수가 일치하면 캐시 사용 (PlayFab Position 갱신 지연 우회)
        if (cachedRank > 0 && cachedRankScore == previousWeeklyBestScore)
        {
            previousWeeklyRank = cachedRank;
            Debug.Log($"[게임시작] 캐시 순위 사용: 순위={cachedRank}, 주간최고={previousWeeklyBestScore}");
        }
        else
        {
            previousWeeklyRank = result.rank;
            Debug.Log($"[게임시작] PlayFab 순위 사용: 순위={result.rank}, 주간최고={previousWeeklyBestScore}");
        }

        // 캐시 소비 (1회 사용 후 초기화)
        cachedRank = -1;
        cachedRankScore = -1;
    }

    /// <summary>
    /// 게임 시작 시 호출: 역대 최고기록 저장 (UserData)
    /// </summary>
    public async Task SavePreviousHighScoreAsync()
    {
        previousHighScore = await GetMyHighScoreAsync();
        Debug.Log($"[게임시작] 역대최고={previousHighScore}");
    }

    /// <summary>
    /// 리더보드에 점수 제출 (주간 최고 갱신 시에만 호출할 것)
    /// </summary>
    public Task<bool> SubmitWeeklyScoreAsync(int score)
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
                Debug.Log($"[리더보드] 주간 최고 갱신: {score}");
                tcs.TrySetResult(true);
            },
            error =>
            {
                Debug.LogError("리더보드 전송 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(false);
            }
        );

        return tcs.Task;
    }

    /// <summary>
    /// 역대 최고기록 UserData에 저장 (역대 최고 갱신 시에만 호출할 것)
    /// </summary>
    public void SaveHighScoreToUserData(int score)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { STAT_HIGH_SCORE, score.ToString() }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log($"[UserData] 역대 최고 갱신: {score}"),
            error => Debug.LogError("UserData 저장 실패: " + error.GenerateErrorReport())
        );
    }

    /// <summary>
    /// 리더보드를 1등부터 스캔하여 정확한 순위 계산
    /// PlayFab Position 필드에 의존하지 않음 (순위 갱신 지연/추정 오차 완전 우회)
    /// GetLeaderboard는 점수 내림차순 정렬이 보장되므로, 내 점수 이상인 타인 수 + 1 = 내 순위
    /// </summary>
    public async Task<int> CalculateRankForScoreAsync(int score)
    {
        string myId = PlayFabSettings.staticPlayer.PlayFabId;
        int aboveCount = 0;
        int fromRank = 1;

        while (true)
        {
            int toRank = fromRank + 99;
            var entries = await GetWeeklyLeaderboardRangeAsync(fromRank, toRank);
            if (entries == null || entries.Count == 0) break;

            foreach (var entry in entries)
            {
                if (entry.PlayFabId == myId) continue;
                if (entry.StatValue >= score)
                    aboveCount++;
                else
                    return aboveCount + 1; // 내림차순 정렬, 이후 엔트리는 모두 더 낮음
            }

            // 이 배치 전부 나보다 높음 → 다음 배치 확인
            fromRank += entries.Count;
        }

        return aboveCount + 1;
    }

    /// <summary>
    /// 역대 최고기록 조회 (UserData)
    /// </summary>
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
                Debug.LogError("UserData 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(0);
            }
        );

        return tcs.Task;
    }

    /// <summary>
    /// 주간 리더보드 범위 조회 (fromRank ~ toRank)
    /// </summary>
    public Task<List<PlayerLeaderboardEntry>> GetWeeklyLeaderboardRangeAsync(int fromRank, int toRank)
    {
        var tcs = new TaskCompletionSource<List<PlayerLeaderboardEntry>>();

        int startPosition = Mathf.Max(0, Mathf.Min(fromRank, toRank) - 1);
        int count = Mathf.Abs(toRank - fromRank) + 1;
        count = Mathf.Min(count, 100);

        var request = new GetLeaderboardRequest
        {
            StatisticName = STAT_HIGH_SCORE,
            StartPosition = startPosition,
            MaxResultsCount = count
        };

        PlayFabClientAPI.GetLeaderboard(
            request,
            result => tcs.TrySetResult(result.Leaderboard),
            error =>
            {
                Debug.LogError("주간 리더보드 범위 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(new List<PlayerLeaderboardEntry>());
            }
        );

        return tcs.Task;
    }
}
