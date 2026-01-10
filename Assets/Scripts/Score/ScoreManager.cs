using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // 점수 서버 저장 (Task 기반)
    public Task<bool> SubmitScoreAsync(int score)
    {
        var tcs = new TaskCompletionSource<bool>();

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "HighScore",
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(
            request,
            result =>
            {
                Debug.Log("점수 전송 완료! (서버에서 순위를 검증합니다.)");
                tcs.TrySetResult(true);
            },
            error =>
            {
                Debug.LogError("전송 실패: " + error.GenerateErrorReport());
                tcs.TrySetResult(false);
            }
        );

        return tcs.Task;
    }
}
