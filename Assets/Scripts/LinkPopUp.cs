using UnityEngine;

public class LinkPopUp : MonoBehaviour
{
    public PlayFabLoginManager login;
    public GameObject successPanel;
    public GameObject failPanel;

    public void StartLink()
    {
        login.ClickLinkButton(OnLink);
    }
    public void OnLink(bool success)
    {
        gameObject.SetActive(false);
        if(success) successPanel.SetActive(true);
        else failPanel.SetActive(true);
    }
}
