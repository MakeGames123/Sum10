using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
public class ProfilePanel : MonoBehaviour
{
    [SerializeField] Image profileImage;
    public List<Sprite> profileSprites = new();
    public List<ProfileSlot> slots = new();
    public Button equipButton;
    int currentIndex = 0;
    private const string PROFILE_INDEX_KEY = "ProfileIndex";
    void Awake()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetCondition(profileSprites[i], i);

            slots[i].onClick.AddListener(SelectProfile);
        }

        equipButton.onClick.AddListener(TryEquipProfile);
    }
    void Start()
    {
        PlayerData.Instance.onProfileImageChanged.AddListener(SyncCheck);
        SyncCheck(PlayerData.Instance.EquippedProfileImage);
    }
    private void SyncCheck(int index)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].DisableBorder();
            slots[i].DisableCheck();
        }
        currentIndex = index;
        slots[currentIndex].EnableBorder();
        slots[currentIndex].EnableCheck();
    }
    public void SelectProfile(int index)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].DisableBorder();
        }
        slots[index].EnableBorder();
        currentIndex = index;
    }
    public void TryEquipProfile()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].DisableCheck();
        }
        slots[currentIndex].EnableCheck();
        PlayerData.Instance.EquippedProfileImage = currentIndex;
        profileImage.sprite = profileSprites[currentIndex];
        SaveProfileIndexToPlayFab(currentIndex);
    }
    private void SaveProfileIndexToPlayFab(int index)
    {
        var request = new UpdateAvatarUrlRequest
        {
            ImageUrl = index.ToString()
        };

        PlayFabClientAPI.UpdateAvatarUrl(request, result =>
        {
            Debug.Log("클라우드 프로필 인덱스 업데이트 완료");
        }, error => Debug.LogError(error.GenerateErrorReport()));
    }

}
