using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Text;

public class SupportManager : MonoBehaviour
{
    public void OpenPrivacyPolicy()
    {
        Application.OpenURL("https://sites.google.com/view/deohanyan-privacypolicy-kr/");
    }

    public void OpenSupportEmail()
    {
        string email = "deohanyan@gmail.com";

        string subject = EscapeURL("[Game Support][더하냥] (Korean)");

        string log = GetRecentLog(); // 🔥 로그 가져오기

        string body = EscapeURL(
            $"Player ID : {GetPlayerId()}\n" +
            $"Device Model : {SystemInfo.deviceModel}\n" +
            $"Device OS : {SystemInfo.operatingSystem}\n" +
            $"App Version : {Application.version}\n" +
            $"\n--- 문의 내용 ---\n\n" +
            $"\n--- 최근 로그 ---\n{log}\n"
        );

        Application.OpenURL($"mailto:{email}?subject={subject}&body={body}");
    }

    private string EscapeURL(string str)
    {
        return UnityWebRequest.EscapeURL(str).Replace("+", "%20");
    }
    private string GetRecentLog()
    {
        string log = ErrorLogCollector.GetErrorLogs();

        // 너무 길면 자르기
        if (log.Length > 2000)
            log = log.Substring(log.Length - 2000);

        return log;
    }

    // 🔥 PlayFab ID 가져오기 (없으면 N/A)
    private string GetPlayerId()
    {
        if (PlayFab.PlayFabSettings.staticPlayer != null)
            return PlayFab.PlayFabSettings.staticPlayer.PlayFabId;

        return "N/A";
    }
}