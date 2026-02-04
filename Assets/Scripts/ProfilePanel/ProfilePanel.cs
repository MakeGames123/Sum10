using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
public class ProfilePanel : MonoBehaviour
{
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

        slots[currentIndex].EnableBorder();
        slots[currentIndex].EnableCheck();

        equipButton.onClick.AddListener(TryEquipProfile);
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
        SaveProfileIndexToPlayFab(currentIndex);
    }
    private void SaveProfileIndexToPlayFab(int index)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { PROFILE_INDEX_KEY, index.ToString() }
        }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log($"프로필 인덱스 저장 성공: {index}");
            },
            error =>
            {
                Debug.LogError("프로필 인덱스 저장 실패: " + error.GenerateErrorReport());
            }
        );
    }

}
