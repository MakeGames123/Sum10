using UnityEngine;
using UnityEngine.Networking;
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
        string body = EscapeURL(
            $"Device Model : {SystemInfo.deviceModel}\\n" +
            $"Device OS : {SystemInfo.operatingSystem}\\n" +
            $"App Version : {Application.version}\\n" +
            $"\\n--- 아래에 문의 내용을 작성해 주세요 ---\\n\\n"
        );
        Application.OpenURL($"mailto:{email}?subject={subject}&body={body}");
    }

    private string EscapeURL(string str)
    {
        return UnityWebRequest.EscapeURL(str).Replace("+", "%20");
    }
}
