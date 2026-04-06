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
                    SetStatus(StringTableLoader.Instance.GetText(57));
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
                SetStatus(StringTableLoader.Instance.GetText(57));
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
                    SetStatus(StringTableLoader.Instance.GetText(57));
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
                SetStatus(StringTableLoader.Instance.GetText(57));
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
                SetStatus(StringTableLoader.Instance.GetText(56));
                profile.UpdateProfile();
                PlayerData.Instance.AdjustDiamond(-10);
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                SetStatus(StringTableLoader.Instance.GetText(55));
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
                SetStatus(StringTableLoader.Instance.GetText(48));
                break;

            case "ALREADY_EXISTS":
                SetStatus(StringTableLoader.Instance.GetText(49));
                break;

            case "INSUFFICIENT_DIAMOND":
                SetStatus(StringTableLoader.Instance.GetText(54));
                break;

            case "INVALID_LENGTH":
                SetStatus(StringTableLoader.Instance.GetText(50));
                break;

            case "INVALID_CHAR":
                SetStatus(StringTableLoader.Instance.GetText(51));
                break;

            case "BANNED_WORD":
                SetStatus(StringTableLoader.Instance.GetText(52));
                break;

            default:
                Debug.Log(reason);
                SetStatus(StringTableLoader.Instance.GetText(53));
                break;
        }
    }
    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
