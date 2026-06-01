using UnityEngine;
using TMPro;

public class NickNameStatus : MonoBehaviour
{
    public TextMeshProUGUI statusText; // 상태 메시지를 보여줄 텍스트 (선택사항)
    
    public void HandleNicknameFailReason(string reason)
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
    public void SetStatusById(int stringId)
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
