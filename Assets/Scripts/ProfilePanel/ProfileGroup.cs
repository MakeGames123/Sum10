using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProfileGroup : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image profileImage;
    public List<Sprite> profileSprites = new();
    public GameObject panel;
    void Start()
    {
        PlayerData.Instance.onProfileImageChanged.AddListener(ChangeProfileImage);

        ChangeProfileImage(PlayerData.Instance.EquippedProfileImage);
    }
    public void OnPointerClick(PointerEventData data)
    {
        AudioManager.Instance?.PlayButtonSFX();
        panel.SetActive(true);
    }
    private void ChangeProfileImage(int index)
    {
        profileImage.sprite = profileSprites[index];
    }
}
