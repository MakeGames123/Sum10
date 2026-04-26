using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public const string STAT_WEEKLY_HIGH_SCORE = "HighScore";
    public const string STAT_HIGH_SCORE = "PermanentHighScore";

    // 이전 주간 순위 + 주간 최고점수 (게임 시작 시 저장)
    private int previousWeeklyRank = -1;
    private int previousWeeklyBestScore = 0;
    public int PreviousWeeklyRank => previousWeeklyRank;
    public int PreviousWeeklyBestScore => previousWeeklyBestScore;

    // 이전 전체 최고기록 (게임 시작 시 저장)
    private int previousHighScore = 0;
    public int PreviousHighScore => previousHighScore;
    public PlayFabLoginManager login;

    // 게임오버 시 계산된 순위 캐시 (연속 플레이 시 PlayFab Position 지연 우회)
    private int cachedRank = -1;
    private int cachedRankScore = -1;

    // ===== Top 50 공유 캐시 =====
    private List<PlayerLeaderboardEntry> cachedWeeklyTop50;
    public List<PlayerLeaderboardEntry> CachedWeeklyTop50 => cachedWeeklyTop50;
    private List<PlayerLeaderboardEntry> cachedTop50;
    public List<PlayerLeaderboardEntry> CachedTop50 => cachedTop50;

    // ===== 내 기록 단독 캐시 (Top50 밖에서도 표시 가능) =====
    private int myWeeklyRank = -1;
    private int myWeeklyScore = -1;
    private int myOverallRank = -1;
    private int myOverallScore = -1;
    public int MyWeeklyRank => myWeeklyRank;
    public int MyWeeklyScore => myWeeklyScore;
    public int MyOverallRank => myOverallRank;
    public int MyOverallScore => myOverallScore;

    // 주간 Top50 갱신 이벤트
    public System.Action OnTop50Updated;
    // 전체 Top50 갱신 이벤트
    public System.Action OnOverallTop50Updated;
    // 내 순위 갱신 이벤트 (CalculateRankForScoreAsync 완료 시 PlayFab Position 지연 우회)
    public System.Action<int> OnMyRankUpdated;

    // CalculateRank 결과 외부 공개용
    public int CachedRank => cachedRank;
    public int CachedRankScore => cachedRankScore;

    private void Start()
    {
        if (login == null)
            login = FindObjectOfType<PlayFabLoginManager>();

        if (login != null)
        {
            login.onLogined.AddListener(() =>
            {
                _ = FetchWeeklyTop50WithProfilesAsync();
                _ = FetchTop50WithProfilesAsync();
                _ = SyncHighScoreFromServerAsync();
            });
        }
    }

    /// <summary>
    /// 최초 1회: 서버 역대 최고기록을 로컬에 동기화 (이후 로컬이 항상 최신)
    /// 실패 시 플래그를 설정하지 않아 다음 로그인에 재시도
    /// await 후 로컬을 다시 읽어 동기화 중 신기록 덮어쓰기 방지
    /// </summary>
    private const string HIGH_SCORE_SYNCED_KEY = "HighScoreSynced";
    private async Task SyncHighScoreFromServerAsync()
    {
        var (success, serverScore) = await GetMyHighScoreWithStatusAsync();
        if (!success) return; // 실패 시 플래그 미설정 → 다음 로그인에 재시도
    }

    private Task<(bool success, int score)> GetMyHighScoreWithStatusAsync()
    {
        var tcs = new TaskCompletionSource<(bool, int)>();

        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest { Keys = new List<string> { STAT_WEEKLY_HIGH_SCORE } },
            result =>
            {
                int highScore = 0;
                if (result.Data != null &&
                    result.Data.TryGetValue(STAT_WEEKLY_HIGH_SCORE, out var data))
                {
                    int.TryParse(data.Value, out highScore);
                }
                tcs.TrySetResult((true, highScore));
            },
            error =>
            {
                Debug.LogError("UserData 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult((false, 0));
            }
        );

        return tcs.Task;
    }

    /// <summary>
    /// 게임오버 시 호출: 계산된 순위를 캐시 (다음 게임 시작 시 사용)
    /// 내 순위 갱신 이벤트도 브로드캐스트 (UI 즉시 반영용)
    /// </summary>
    public void CacheCalculatedRank(int rank, int score)
    {
        cachedRank = rank;
        cachedRankScore = score;
        OnMyRankUpdated?.Invoke(rank);
    }

    /// <summary>
    /// 게임 시작 시 호출: 주간 순위 + 주간 최고점수를 한 번에 저장
    /// </summary>
    public async Task SavePreviousWeeklyRankAsync()
    {
        var tcs = new TaskCompletionSource<(int rank, int score)>();

        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = STAT_WEEKLY_HIGH_SCORE,
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
            previousWeeklyRank = cachedRank;
        else
            previousWeeklyRank = result.rank;

        // 캐시 소비 (1회 사용 후 초기화)
        cachedRank = -1;
        cachedRankScore = -1;
    }

    /// <summary>
    /// 게임 시작 시 호출: 역대 최고기록 로드
    /// Statistics의 STAT_HIGH_SCORE에서 직접 조회 (UpdatePlayerStatistics와 동일 저장소)
    /// </summary>
    public Task LoadPreviousHighScoreAsync()
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
                int score = 0;
                string myId = PlayFabSettings.staticPlayer.PlayFabId;
                foreach (var entry in result.Leaderboard)
                {
                    if (entry.PlayFabId == myId)
                    {
                        score = entry.StatValue;
                        break;
                    }
                }
                previousHighScore = score;
                tcs.TrySetResult(score);
            },
            error =>
            {
                Debug.LogError("역대 최고기록 로드 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(0);
            }
        );

        return tcs.Task;
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
                    StatisticName = STAT_WEEKLY_HIGH_SCORE,
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(
            request,
            result => tcs.TrySetResult(true),
            error =>
            {
                Debug.LogError("리더보드 전송 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(false);
            }
        );

        return tcs.Task;
    }

    public Task<bool> SubmitHighScore(int score)
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
            result => tcs.TrySetResult(true),
            error =>
            {
                Debug.LogError("리더보드 전송 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(false);
            }
        );

        return tcs.Task;
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
            Keys = new List<string> { STAT_WEEKLY_HIGH_SCORE }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                int highScore = 0;
                if (result.Data != null &&
                    result.Data.TryGetValue(STAT_WEEKLY_HIGH_SCORE, out var data))
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
            StatisticName = STAT_WEEKLY_HIGH_SCORE,
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

    /// <summary>
    /// Top 50 리더보드 + 프로필 인덱스를 조회하여 캐시에 저장
    /// 내 기록도 같이 갱신하여 Top50 밖에서도 정확한 순위/점수 표시 가능
    /// </summary>
    public async Task FetchWeeklyTop50WithProfilesAsync()
    {
        var tcs = new TaskCompletionSource<List<PlayerLeaderboardEntry>>();

        var request = new GetLeaderboardRequest
        {
            StatisticName = STAT_WEEKLY_HIGH_SCORE,
            StartPosition = 0,
            MaxResultsCount = 50,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true,
                ShowAvatarUrl = true
            }
        };

        PlayFabClientAPI.GetLeaderboard(request,
            result =>
            {
                cachedWeeklyTop50 = result.Leaderboard;
                tcs.TrySetResult(result.Leaderboard);
            },
            error =>
            {
                Debug.LogError("Top 50 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(null);
            }
        );

        await tcs.Task;
        await RefreshMyWeeklyRecordAsync();
        OnTop50Updated?.Invoke();
    }

    public async Task FetchTop50WithProfilesAsync()
    {
        var tcs = new TaskCompletionSource<List<PlayerLeaderboardEntry>>();

        var request = new GetLeaderboardRequest
        {
            StatisticName = STAT_HIGH_SCORE,
            StartPosition = 0,
            MaxResultsCount = 50,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true,
                ShowAvatarUrl = true
            }
        };

        PlayFabClientAPI.GetLeaderboard(request,
            result =>
            {
                cachedTop50 = result.Leaderboard;
                tcs.TrySetResult(result.Leaderboard);
            },
            error =>
            {
                Debug.LogError("Top 50 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(null);
            }
        );

        await tcs.Task;
        await RefreshMyOverallRecordAsync();
        OnOverallTop50Updated?.Invoke();
    }

    /// <summary>
    /// 내 주간 기록(순위 + 점수)을 갱신하여 캐시
    /// Top50 안에 있으면 그 위치 사용 (정확함), 밖이면 GetLeaderboardAroundPlayer로 조회
    /// </summary>
    public Task RefreshMyWeeklyRecordAsync()
    {
        return RefreshMyRecordAsync(STAT_WEEKLY_HIGH_SCORE, cachedWeeklyTop50,
            (rank, score) => { myWeeklyRank = rank; myWeeklyScore = score; });
    }

    /// <summary>
    /// 내 전체 기록(순위 + 점수)을 갱신하여 캐시
    /// </summary>
    public Task RefreshMyOverallRecordAsync()
    {
        return RefreshMyRecordAsync(STAT_HIGH_SCORE, cachedTop50,
            (rank, score) => { myOverallRank = rank; myOverallScore = score; });
    }

    private Task RefreshMyRecordAsync(string statName, List<PlayerLeaderboardEntry> top50Cache,
        System.Action<int, int> assign)
    {
        string myId = PlayFabSettings.staticPlayer.PlayFabId;

        // Top50 안에 있으면 그 데이터를 신뢰 (Position 갱신 지연 영향 적음)
        if (top50Cache != null)
        {
            foreach (var entry in top50Cache)
            {
                if (entry.PlayFabId == myId)
                {
                    assign(entry.Position + 1, entry.StatValue);
                    return Task.CompletedTask;
                }
            }
        }

        // Top50 밖이면 GetLeaderboardAroundPlayer로 조회
        var tcs = new TaskCompletionSource<bool>();

        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = statName,
                MaxResultsCount = 1
            },
            result =>
            {
                int rank = -1;
                int score = -1;
                foreach (var entry in result.Leaderboard)
                {
                    if (entry.PlayFabId == myId)
                    {
                        rank = entry.Position + 1;
                        score = entry.StatValue;
                        break;
                    }
                }
                assign(rank, score);
                tcs.TrySetResult(true);
            },
            error =>
            {
                Debug.LogError("내 기록 조회 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(false);
            }
        );

        return tcs.Task;
    }
}
