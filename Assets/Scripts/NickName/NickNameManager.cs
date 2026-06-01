using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class NicknameManager : MonoBehaviour
{
    [Header("UI 연동")]
    public TMP_InputField nicknameInputField;
    public TextMeshProUGUI statusText; // 상태 메시지를 보여줄 텍스트 (선택사항)
    public PlayFabLoginManager login;
    public LocalizationLoader localizationLoader;
    public ProfileGroup profile;
    public NickNameStatus status;
    private string pendingToken;
    public Image intro;

    private bool isProcessing = false;
    private bool isApplying = false;

    private bool isLogin = false;
    private bool isLocalized = false;

    void Awake()
    {
        // 람다식 내부에서 상태를 바로 변경하고 체크 함수 호출
        login.onLogined.AddListener(() => { isLogin = true; TryCheckNewbie(); });

        if (localizationLoader.isInitialized)
        {
            isLocalized = true;
            TryCheckNewbie();
        }
        else localizationLoader.OnInitialize += () => { isLocalized = true; TryCheckNewbie(); };
    }

    private void TryCheckNewbie()
    {
        // 숫자 플래그 체크 없이 변수 상태만 확인
        if (isLogin && isLocalized)
        {
            CheckNewbie();
        }
    }
    private void CheckNewbie()
    {
        PlayFabClientAPI.GetAccountInfo(
            new GetAccountInfoRequest(),
            result =>
            {
                string nickname = result.AccountInfo.TitleInfo.DisplayName;

                if (string.IsNullOrEmpty(nickname))
                {
                    if (localizationLoader.CurrentLanguage == "kr")
                    {
                        transform.localPosition = Vector2.zero;
                    }
                    else//영어권은 닉네임 자동 생성
                    {
                        SetAutoNickname(result.AccountInfo.PlayFabId, 0);
                    }
                }
                else
                {
                    intro.raycastTarget = true;
                    gameObject.SetActive(false);
                }
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            }
        );
    }

    public void OnClickNicknameChange()//패널 버튼에 할당
    {
        if (isProcessing)
            return;

        isProcessing = true;

        string inputName = nicknameInputField.text;
        RequestNicknameToken(inputName);
    }

    public void RequestNicknameToken(string nickname)
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "RequestNicknameToken",
                FunctionParameter = new { nickname = nickname, isFree = true },
                GeneratePlayStreamEvent = true
            },
            result =>
            {
                var data = result.FunctionResult as IDictionary<string, object>;
                if (data == null)
                {
                    // ID 57: "서버 오류" -> "서버 오류가 발생했습니다." 대용으로 세팅
                    status.SetStatusById(57);
                    isProcessing = false;
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    status.HandleNicknameFailReason(reason);
                    isProcessing = false;
                    return;
                }

                pendingToken = data["token"].ToString();
                ConfirmNickname();
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                // ID 57: "서버 오류"
                status.SetStatusById(57);
                isProcessing = false;
            }
        );
    }

    public void ConfirmNickname()
    {
        if (string.IsNullOrEmpty(pendingToken))
        {
            Debug.LogError("토큰 없음");
            return;
        }

        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "ConfirmNickname",
                FunctionParameter = new { token = pendingToken },
                GeneratePlayStreamEvent = true
            },
            result =>
            {
                var data = result.FunctionResult as IDictionary<string, object>;
                if (data == null)
                {
                    // ID 57: "서버 오류"
                    status.SetStatusById(57);
                    isProcessing = false;
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    status.HandleNicknameFailReason(reason);
                    isProcessing = false;
                    return;
                }

                string nickname = data["nickname"].ToString();
                ApplyNickname(nickname);
                pendingToken = null;
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                // ID 57: "서버 오류"
                status.SetStatusById(57);
                isProcessing = false;
            }
        );
    }

    void ApplyNickname(string nickname)
    {
        if (isApplying)
            return;
        isApplying = true;

        PlayFabClientAPI.UpdateUserTitleDisplayName(
            new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = nickname
            },
            result =>
            {
                isApplying = false;
                isProcessing = false;

                profile.UpdateProfile();
                SaveNewbieProfile();

                // ID 56: "닉네임 변경 성공!"
                status.SetStatusById(56);

                intro.raycastTarget = true;
                gameObject.SetActive(false);
            },
            error =>
            {
                isApplying = false;
                isProcessing = false;

                Debug.LogError(error.GenerateErrorReport());

                if (error.ErrorMessage.Contains("display name"))
                {
                    // ID 56: "닉네임 변경 성공!"
                    status.SetStatusById(56);
                    intro.raycastTarget = true;
                    profile.UpdateProfile();
                    gameObject.SetActive(false);
                    return;
                }

                // ID 49 또는 55번: "이미 사용중인 닉네임입니다."
                status.SetStatusById(49);

                // ⭐ 환불 요청
                PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
                {
                    FunctionName = "RefundNicknameIfFailed",
                    FunctionParameter = new { token = pendingToken },
                    GeneratePlayStreamEvent = true
                },
                result =>
                {
                    Debug.Log("환불 처리 완료");
                },
                errorResult =>
                {
                    Debug.LogError("환불 실패: " + errorResult.GenerateErrorReport());
                });
            }
        );
    }

    private void SaveNewbieProfile()
    {
        var request = new UpdateAvatarUrlRequest
        {
            ImageUrl = "0"
        };

        PlayFabClientAPI.UpdateAvatarUrl(request, result =>
        {
            Debug.Log("클라우드 프로필 인덱스 업데이트 완료");
        }, error => Debug.LogError(error.GenerateErrorReport()));
    }
    private void SetAutoNickname(string playFabId, int retry = 0)
    {
        string nickname;

        if (retry > 10)
        {
            Debug.Log("실패");
            return;
        }

        if (retry == 0)
            nickname = "Cat" + playFabId[^6..];
        else
            nickname = "Cat" + UnityEngine.Random.Range(100000, 999999);

        PlayFabClientAPI.UpdateUserTitleDisplayName(
            new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = nickname
            },
            result =>
            {
                Debug.Log($"닉네임 설정 성공 : {result.DisplayName}");
                intro.raycastTarget = true;
                gameObject.SetActive(false);
            },
            error =>
            {
                if (retry < 5)
                {
                    SetAutoNickname(playFabId, retry + 1);
                }
                else
                {
                    Debug.LogError("닉네임 설정 실패");
                    Debug.LogError(error.GenerateErrorReport());
                }
            });
    }
}