using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class NickNamePurchase : MonoBehaviour
{
    [Header("UI 연동")]
    public TMP_InputField nicknameInputField;
    public TextMeshProUGUI statusText; // 상태 메시지를 보여줄 텍스트 (선택사항)
    public TextMeshProUGUI diaAmount;
    public Image diaIcon;
    public ProfileGroup profile;
    private string pendingToken;
    public NickNameStatus status;

    void Start()
    {
        if (LocalizationLoader.Instance.CurrentLanguage == "kr")
        {
            diaAmount.text = "100";
            diaIcon.enabled = true;
        }
        else
        {
            diaIcon.enabled = false;
            diaAmount.text = "Change";
        }
    }
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
                    status.SetStatusById(57);
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    status.HandleNicknameFailReason(reason);
                    return;
                }

                pendingToken = data["token"].ToString();
                if (LocalizationLoader.Instance.CurrentLanguage == "kr") ConfirmNickname();
                else ConfirmNicknameFree();
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                // ID 57: "서버 오류"
                status.SetStatusById(57);
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
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    status.HandleNicknameFailReason(reason);
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
                status.SetStatusById(57);
            }
        );
    }

    public void ConfirmNicknameFree()//영어권용
    {
        Debug.Log(1);
        if (string.IsNullOrEmpty(pendingToken))
        {
            Debug.LogError("토큰 없음");
            return;
        }

        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "ConfirmNicknameFree",
                FunctionParameter = new { token = pendingToken },
                GeneratePlayStreamEvent = true
            },
            result =>
            {
                var data = result.FunctionResult as IDictionary<string, object>;
                Debug.Log(2);
                if (data == null)
                {
                    // ID 57: "서버 오류"
                    status.SetStatusById(57);
                    return;
                }

                bool ok = data.ContainsKey("ok") && (bool)data["ok"];
                Debug.Log(ok);
                if (!ok)
                {
                    string reason = data.ContainsKey("reason")
                        ? data["reason"].ToString()
                        : "UNKNOWN";

                    status.HandleNicknameFailReason(reason);
                    return;
                }

                // 여기서 nickname 받아서 Apply 호출
                string nickname = data["nickname"].ToString();
                ApplyNicknameFree(nickname);
                pendingToken = null;
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
                // ID 57: "서버 오류"
                status.SetStatusById(57);
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
                status.SetStatusById(56);
                profile.UpdateProfile();
                PlayerData.Instance.AdjustDiamond(-100);
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());

                // ID 49: "이미 사용중인 닉네임입니다."
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
    void ApplyNicknameFree(string nickname)
    {
        Debug.Log(1);
        PlayFabClientAPI.UpdateUserTitleDisplayName(
            new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = nickname
            },
            result =>
            {
                // ID 56: "닉네임 변경 성공!"
                status.SetStatusById(56);
                profile.UpdateProfile();
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());

                // ID 49: "이미 사용중인 닉네임입니다."
                status.SetStatusById(49);
            }
        );
    }

    public void ResetInput()
    {
        nicknameInputField.text = "";
        if (statusText != null) statusText.text = "";
    }
}