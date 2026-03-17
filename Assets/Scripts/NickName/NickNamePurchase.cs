using System.Collections.Generic;
using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using GooglePlayGames.BasicApi; // TMP 사용을 위해 필수


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
                    SetStatus("서버 응답이 올바르지 않습니다.");
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

                // ⭐ 여기서 nickname 받아서 Apply 호출
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
    void ApplyNickname(string nickname)
    {
        PlayFabClientAPI.UpdateUserTitleDisplayName(
            new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = nickname
            },
            result =>
            {
                SetStatus("닉네임 변경 성공!");
                profile.UpdateProfile();
                PlayerData.Instance.AdjustDiamond(-10);
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                SetStatus("이미 사용중인 닉네임입니다.");
            }
        );
    }
    public void ResetInput()
    {
        nicknameInputField.text = "";
        SetStatus("");
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

            case "INSUFFICIENT_DIAMOND":
                SetStatus("다이아가 모자랍니다.");
                break;

            case "INVALID_LENGTH":
                SetStatus("닉네임 길이는 3~10자여야 합니다.");
                break;

            case "INVALID_CHAR":
                SetStatus("닉네임에는 한글, 영문, 숫자만 사용할 수 있어요.");
                break;

            case "BANNED_WORD":
                SetStatus("사용할 수 없는 단어가 포함되어 있어요.");
                break;

            default:
                SetStatus("닉네임을 변경할 수 없습니다.");
                break;
        }
    }
    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
