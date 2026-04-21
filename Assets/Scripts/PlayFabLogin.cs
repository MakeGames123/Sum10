using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class PlayFabLoginManager : MonoBehaviour
{
    public UnityEvent onLogined = new();
    public GameObject reconnectPanel;
    public GameObject linkButton;

    public bool isLinked = false;

    void Awake()
    {
        PlayGamesPlatform.Activate();
    }

    void Start()
    {
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            Debug.LogError("GPGS 로그인 상태: " + status);

            // 로그인 결과 상관없이 게스트 로그인 진행
            LoginWithGuest();
        });
    }
    public void LoginWithGuest()
    {
        var request = new LoginWithAndroidDeviceIDRequest
        {
            AndroidDeviceId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId
        };

        PlayFabClientAPI.LoginWithAndroidDeviceID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.LogError("PlayFab 로그인 성공");

        // 🔥 먼저 연동 여부 확인
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
            result =>
            {
                isLinked = result.AccountInfo?.GooglePlayGamesInfo != null;

                if (isLinked)
                {
                    Debug.LogError("이미 GPGS 연동된 계정");
                    FinishLogin();
                }
                else
                {
                    Debug.LogError("연동 안된 계정 → 자동 연동 시도");
                    TryAutoLink();
                }
            },
            error =>
            {
                Debug.LogError("계정 정보 조회 실패 → 그냥 진행");
                FinishLogin();
            });
    }
    public void ClickLinkButton(System.Action<bool> onComplete)
    {
        // 이미 로그인 안 되어 있으면 로그인부터
        PlayGamesPlatform.Instance.ManuallyAuthenticate((status) =>
        {
            if (status != SignInStatus.Success)
            {
                Debug.LogError("GPGS 로그인 실패");
                onComplete?.Invoke(false);
                return;
            }

            // 로그인 성공 → 서버 코드 요청
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, (authCode) =>
            {
                if (string.IsNullOrEmpty(authCode))
                {
                    Debug.LogError("AuthCode 없음");
                    onComplete?.Invoke(false);
                    return;
                }

                var request = new LinkGooglePlayGamesServicesAccountRequest
                {
                    ServerAuthCode = authCode,
                    ForceLink = false
                };

                PlayFabClientAPI.LinkGooglePlayGamesServicesAccount(request,
                    result =>
                    {
                        Debug.LogError("수동 연동 성공");
                        isLinked = true;
                        linkButton.SetActive(false);
                        onComplete?.Invoke(true);
                    },
                    error =>
                    {
                        Debug.LogError("수동 연동 실패: " + error.Error);
                        onComplete?.Invoke(false);
                    });
            });
        });
    }
    void TryAutoLink()
    {
        // 이미 GPGS 로그인 되어있는지 체크
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.LogError("GPGS 로그인 안됨 → 연동 스킵");
            FinishLogin();
            return;
        }

        Debug.LogError("GPGS 로그인 상태 → 자동 연동 시도");

        PlayGamesPlatform.Instance.RequestServerSideAccess(true, (authCode) =>
        {
            if (string.IsNullOrEmpty(authCode))
            {
                FinishLogin();
                return;
            }

            var request = new LinkGooglePlayGamesServicesAccountRequest
            {
                ServerAuthCode = authCode,
                ForceLink = false
            };

            PlayFabClientAPI.LinkGooglePlayGamesServicesAccount(request,
                result =>
                {
                    Debug.LogError("자동 연동 성공");
                    isLinked = true;
                    FinishLogin();
                },
                error =>
                {
                    Debug.LogError("자동 연동 실패: " + error.Error);
                    FinishLogin();
                });
        });
    }

    void FinishLogin()
    {
        linkButton.SetActive(!isLinked);

        if (reconnectPanel != null)
            reconnectPanel.SetActive(false);

        Debug.LogError("로그인 완료");
        onLogined.Invoke();
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("PlayFab 로그인 실패: " + error.GenerateErrorReport());

        if (reconnectPanel != null)
            reconnectPanel.SetActive(true);
    }
}