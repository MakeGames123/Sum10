using System.Collections.Generic;
using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class NickNamePurchase : MonoBehaviour
{
    [Header("UI 연동")]
    public TMP_InputField nicknameInputField;
    public TextMeshProUGUI statusText; // 상태 메시지를 보여줄 텍스트 (선택사항)
    public ProfileGroup profile;
    private string pendingToken;

    public void OnClickNicknameChange()
    {
        string inputName = nicknameInputField.text;
        RequestNicknameToken(inputName);
    }

    public void RequestNicknameToken(string nickname)
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "RequestNicknameToken",
                FunctionParameter = new { nickname = nickname, isFree = false },
                GeneratePlayStreamEvent = true
            },
            result =>
            {
                var data = result.FunctionResult as IDictionary<string, object>;
                if (data == null)
                {
                    // ID 57: "서버 오류"
                    SetStatusById(57);
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    HandleNicknameFailReason(reason);
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
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    HandleNicknameFailReason(reason);
                    return;
                }

                // 여기서 nickname 받아서 Apply 호출
                string nickname = data["nickname"].ToString();
                ApplyNickname(nickname);
                pendingToken = null;
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                // ID 57: "서버 오류"
                SetStatusById(57);
            }
        );
    }

    void ApplyNickname(string nickname)
    {
        PlayFabClientAPI.UpdateUserTitleDisplayName(
            new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = nickname
            },
            result =>
            {
                // ID 56: "닉네임 변경 성공!"
                SetStatusById(56);
                profile.UpdateProfile();
                PlayerData.Instance.AdjustDiamond(-100);
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                
                // ID 49: "이미 사용중인 닉네임입니다."
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

    public void ResetInput()
    {
        nicknameInputField.text = "";
        if (statusText != null) statusText.text = "";
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

            case "INSUFFICIENT_DIAMOND":
                // ID 54: "다이아가 부족합니다."
                SetStatusById(54);
                break;

            case "INVALID_LENGTH":
                // ID 50: "닉네임 길이는 3~10자여야 합니다." (시트 기준 문구 매칭)
                SetStatusById(50);
                break;

            case "INVALID_CHAR":
                // ID 51: "한글, 영문, 숫자만 사용할 수 있습니다." (쉼표 예외 확인 완료)
                SetStatusById(51);
                break;

            case "BANNED_WORD":
                // ID 52: "사용할 수 없는 단어가 포함되어 있습니다."
                SetStatusById(52);
                break;

            default:
                // ID 53: "닉네임을 변경할 수 없습니다."
                SetStatusById(53);
                break;
        }
    }

    /// <summary>
    /// ID를 인자로 받아 LocalizationManager를 통해 statusText에 반영하는 메서드
    /// </summary>
    private void SetStatusById(int stringId)
    {
        if (statusText == null) return;

        if (LocalizationLoader.Instance != null)
        {
            statusText.text = LocalizationLoader.Instance.GetText(stringId);
        }
        else
        {
            Debug.LogWarning($"[Localization Missing] ID: {stringId}");
        }
    }
}