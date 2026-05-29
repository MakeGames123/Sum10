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
    
    private bool isProcessing = false;
    private bool isApplying = false;

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
                if (string.IsNullOrEmpty(nickname)) transform.localPosition = Vector2.zero;
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
                    // ID 57: "서버 오류" -> "서버 오류가 발생했습니다." 대용으로 세팅
                    SetStatusById(57);
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
                // ID 57: "서버 오류"
                SetStatusById(57);
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
                    SetStatusById(57);
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
                // ID 57: "서버 오류"
                SetStatusById(57);
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
                SetStatusById(56);
                
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
                    SetStatusById(56);
                    intro.raycastTarget = true;
                    profile.UpdateProfile();
                    gameObject.SetActive(false);
                    return;
                }
                
                // ID 49 또는 55번: "이미 사용중인 닉네임입니다."
                SetStatusById(49);

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

    void HandleNicknameFailReason(string reason)
    {
        switch (reason)
        {
            case "EMPTY":
                // ID 48: "닉네임을 입력해주세요."
                SetStatusById(48);
                break;

            case "ALREADY_EXISTS":
                // ID 49: "이미 사용중인 닉네임 입니다."
                SetStatusById(49);
                break;

            case "INVALID_LENGTH":
                // ID 50: "닉네임 길이는 3~10자여야 합니다." (시트 기준으로 표기 변경 확인)
                SetStatusById(50);
                break;

            case "INVALID_CHAR":
                // ID 51: "한글, 영문, 숫자만 사용할 수 있습니다."
                SetStatusById(51);
                break;

            case "BANNED_WORD":
                // ID 52: "사용할 수 없는 단어가 포함되어 있습니다."
                SetStatusById(52);
                break;

            default:
                Debug.Log(reason);
                // ID 53: "닉네임을 변경할 수 없습니다."
                SetStatusById(53);
                break;
        }
    }

    /// <summary>
    /// ID를 인자로 받아 LocalizationManager를 통해 statusText에 갱신하는 보조 메서드
    /// </summary>
    private void SetStatusById(int stringId)
    {
        if (statusText == null) return;

        Debug.Log(stringId);

        if (LocalizationLoader.Instance != null)
        {
            statusText.text = LocalizationLoader.Instance.GetText(stringId);
        Debug.Log(statusText.text);
        }
        else
        {
            // Fallback: 매니저가 없을 때 최소한 로그로 파악하기 위함
            Debug.LogWarning($"[Localization Missing] ID: {stringId}");
        }
    }
}