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
                FunctionParameter = new { nickname = nickname }
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

                // 성공
                string token = data["token"].ToString();
                ApplyNickname(nickname, token);
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                SetStatus("서버 통신 오류가 발생했습니다.");
            }
        );
    }
    void ApplyNickname(string nickname, string token)
    {
        // 토큰 검증용으로 서버에 다시 전달
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest(),
            result =>
            {
                if (!result.Data.ContainsKey("NicknameToken") ||
                    result.Data["NicknameToken"].Value != token)
                {
                    Debug.LogError("❌ 토큰 불일치");
                    return;
                }

                // 토큰이 유효하면 닉네임 적용
                PlayFabClientAPI.UpdateUserTitleDisplayName(
                    new UpdateUserTitleDisplayNameRequest
                    {
                        DisplayName = nickname
                    },
                    success =>
                    {
                        Debug.Log("닉네임 변경 성공");
                        profile.UpdateProfile();
                        PlayerData.Instance.AdjustDiamond(-10);

                        // 토큰 소모 (삭제)
                        PlayFabClientAPI.UpdateUserData(
                            new UpdateUserDataRequest
                            {
                                KeysToRemove = new List<string> { "NicknameToken" }
                            },
                            _ => { },
                            _ => { }
                        );
                    },
                    error => Debug.LogError(error.GenerateErrorReport())
                );
            },
            error => Debug.LogError(error.GenerateErrorReport())
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
