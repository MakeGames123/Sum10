using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;


public class PlayFabLoginManager : MonoBehaviour
{
    public UnityEvent onLogined = new();
    public GameObject reconnectPanel;
    void Start()
    {
        Login();
    }

    public void Login()
    {
        // 1. 요청 객체 생성
        var request = new LoginWithCustomIDRequest
        {
            // 사용자의 기기 고유 식별자를 ID로 사용 (테스트용으로 임의 문자열 가능)
            CustomId = SystemInfo.deviceUniqueIdentifier,

            // 계정이 없으면 새로 생성하도록 설정
            CreateAccount = true
        };

        // 2. PlayFab API 호출
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }
    public void CustomLogin()
    {
        PlayFabClientAPI.LoginWithCustomID(
    new LoginWithCustomIDRequest
    {
        CustomId = "TEST_USER_002",
        CreateAccount = true
    },
    loginResult =>
    {
        Debug.Log("테스트 계정 로그인 성공");

        // 로그인 성공 후 0점 등록
        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = "HighScore",
                        Value = 0
                    }
                }
            },
            result => Debug.Log("리더보드에 0점 등록 완료"),
            error => Debug.LogError("점수 등록 실패: " + error.GenerateErrorReport())
        );
    },
    error =>
    {
        Debug.LogError("로그인 실패: " + error.GenerateErrorReport());
    }
);

    }

    // 로그인 성공 시 호출
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("플레이팹 로그인 성공!");
        Debug.Log($"사용자 ID: {result.PlayFabId}");
        reconnectPanel.SetActive(false);

        onLogined.Invoke();
        // 새로 생성된 계정인지 확인
        if (result.NewlyCreated)
        {
            Debug.Log("신규 계정이 생성되었습니다.");
        }
    }

    // 로그인 실패 시 호출
    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("로그인 실패...");
        Debug.LogError(error.GenerateErrorReport());
        reconnectPanel.SetActive(true);
    }
}