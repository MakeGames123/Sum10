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
    private const string GOOGLE_LINK_KEY = "IsGoogleLinked";
    public GameObject reconnectPanel;
    public GameObject linkButton;
    public bool isLinked = false;

    void Awake()
    {
        PlayGamesPlatform.Activate();
    }

    void Start()
    {
        // 1. 이전에 구글 연동을 완료했었는지 확인
        if (PlayerPrefs.GetInt(GOOGLE_LINK_KEY, 0) == 1)
        {
            Debug.LogError("구글 연동 유저: 구글 로그인 시도");
            LoginWithGoogle();
        }
        else
        {
            Debug.LogError("게스트 유저: 게스트 로그인 시도");
            LoginWithGuest();
        }
    }

    // [구글 로그인] - 기존 연동 유저용
    public void LoginWithGoogle()
    {
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, (authCode) =>
                {
                    LoginToPlayFabWithGoogle(authCode);
                });
                isLinked = true;
                linkButton.SetActive(false);
            }
            else
            {
                // 구글 로그인 실패 시 게스트로라도 들여보낼지 선택 (안전빵으로 게스트 로그인 호출 가능)
                LoginWithGuest();
            }
        });
    }

    // [게스트 로그인] - 신규 유저용
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

    // [연동하기 버튼용] - 설정창의 '연동 버튼'에 연결
    public void ClickLinkButton(System.Action<bool> onComplete)
    {
        // 1. Google Play Games 인증 시작
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                // 2. 서버 인증 코드 요청
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, (authCode) =>
                {
                    if (string.IsNullOrEmpty(authCode))
                    {
                        Debug.LogError("Google 서버 인증 코드를 가져오지 못했습니다.");
                        onComplete?.Invoke(false);
                        return;
                    }

                    var request = new LinkGooglePlayGamesServicesAccountRequest
                    {
                        ServerAuthCode = authCode,
                        ForceLink = false
                    };

                    // 3. PlayFab 계정 연동 시도
                    PlayFabClientAPI.LinkGooglePlayGamesServicesAccount(request,
                        result =>
                        {
                            PlayerPrefs.SetInt(GOOGLE_LINK_KEY, 1);
                            PlayerPrefs.Save();

                            Debug.LogError("구글 연동 성공!");
                            isLinked = true;
                            linkButton.SetActive(false);

                            onComplete?.Invoke(true);
                        },
                        error =>
                        {
                            Debug.LogError("구글 연동 실패!");
                            HandleLinkError(error);
                            onComplete?.Invoke(false);
                        });
                });
            }
            else
            {
                Debug.LogError($"Google 로그인 실패: {status}");
                onComplete?.Invoke(false);
            }
        });
    }
    private void HandleLinkError(PlayFabError error)
    {
        switch (error.Error)
        {
            // 케이스 1: 현재 PlayFab 계정에 이미 구글 계정이 연동되어 있는 경우
            case PlayFabErrorCode.AccountAlreadyLinked:
                Debug.LogError("이 PlayFab 계정은 이미 다른 구글 계정과 연동되어 있습니다.");
                break;

            // 케이스 3: 서버 코드 만료 또는 잘못된 구성
            case PlayFabErrorCode.InvalidGooglePlayGamesServerAuthCode:
                Debug.LogError("구글 서버 인증 코드가 유효하지 않습니다. 설정을 확인하세요.");
                break;

            default:
                Debug.LogError($"연동 실패 (에러코드: {error.Error}): {error.ErrorMessage}");
                break;
        }
    }
    private void LoginToPlayFabWithGoogle(string serverAuthCode)
    {
        var request = new LoginWithGooglePlayGamesServicesRequest
        {
            ServerAuthCode = serverAuthCode,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId
        };

        PlayFabClientAPI.LoginWithGooglePlayGamesServices(request, OnLoginSuccess, OnLoginFailure);
    }
    // 로그인 성공 시 호출 (기존 코드 유지)
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.LogError("플레이팹(구글연동) 로그인 성공!");
        Debug.LogError($"사용자 ID: {result.PlayFabId}");

        if (reconnectPanel != null) reconnectPanel.SetActive(false);
        onLogined.Invoke();

        if (result.NewlyCreated)
        {
            Debug.LogError("신규 구글 기반 계정이 생성되었습니다.");
            // 신규 계정일 경우 초기 리더보드 등록 등을 여기서 수행할 수 있습니다.
            InitializeNewPlayer();
        }
    }

    // 로그인 실패 시 호출 (기존 코드 유지)
    private void OnLoginFailure(PlayFabError error)
    {
        if (error != null)
            Debug.LogError("PlayFab 로그인 실패: " + error.GenerateErrorReport());

        if (reconnectPanel != null) reconnectPanel.SetActive(true);
    }

    private void InitializeNewPlayer()
    {
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> {
                new StatisticUpdate { StatisticName = "HighScore", Value = 0 }
            }
        }, result => Debug.LogError("초기 점수 등록 완료"), null);
    }
}