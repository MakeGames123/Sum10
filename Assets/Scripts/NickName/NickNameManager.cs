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
    public ProfileGroup profile;
    private string pendingToken;
    public Image intro;
    void Awake()
    {
        login.onLogined.AddListener(CheckNewbie);
    }
    private void CheckNewbie()
    {
        PlayFabClientAPI.GetAccountInfo(
            new GetAccountInfoRequest(),
            result =>
            {
                string nickname = result.AccountInfo.TitleInfo.DisplayName;
                if (nickname == "" || nickname == null) transform.localPosition = Vector2.zero;
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
    bool isProcessing = false;
    public void OnClickNicknameChange()
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
                    SetStatus("서버 응답이 올바르지 않습니다.");
                    isProcessing = false;
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    HandleNicknameFailReason(reason);
                    isProcessing = false;
                    return;
                }

                pendingToken = data["token"].ToString();

                ConfirmNickname();
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                SetStatus("서버 통신 오류가 발생했습니다.");
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
                    SetStatus("서버 응답 오류");
                    isProcessing = false;
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    HandleNicknameFailReason(reason);
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
                SetStatus("서버 오류");
            }
        );
    }
    bool isApplying = false;

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
                SetStatus("닉네임 변경 성공!");
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
                    SetStatus("닉네임 변경 성공!");
                    intro.raycastTarget = true;
                    profile.UpdateProfile();
                    gameObject.SetActive(false);
                    return;
                }
                SetStatus("이미 사용중인 닉네임입니다.");

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
                error =>
                {
                    Debug.LogError("환불 실패: " + error.GenerateErrorReport());
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
    void HandleNicknameFailReason(string reason)
    {
        switch (reason)
        {
            case "EMPTY":
                SetStatus("닉네임을 입력해주세요.");
                break;

            case "ALREADY_EXISTS":
                SetStatus("이미 사용중인 닉네임 입니다.");
                break;

            case "INVALID_LENGTH":
                SetStatus("닉네임 길이는 3~6자여야 합니다.");
                break;

            case "INVALID_CHAR":
                SetStatus("닉네임에는 한글, 영문, 숫자만 사용할 수 있어요.");
                break;

            case "BANNED_WORD":
                SetStatus("사용할 수 없는 단어가 포함되어 있어요.");
                break;

            default:
                Debug.Log(reason);
                SetStatus("닉네임을 변경할 수 없습니다.");
                break;
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log(msg);
    }
}