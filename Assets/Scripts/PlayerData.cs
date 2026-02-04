using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PlayFab;
using PlayFab.ClientModels;
public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;
    public PlayFabLoginManager login;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else { Destroy(gameObject); }

        login.onLogined.AddListener(LoadMyProfileImage);
    }

    private int equippedProfileImage = 0;
    public int EquippedProfileImage
    {
        get { return equippedProfileImage; }
        set
        {
            equippedProfileImage = value;
            onProfileImageChanged.Invoke(equippedProfileImage);
        }
    }
    public UnityEvent<int> onProfileImageChanged = new();

    public void LoadMyProfileImage()
    {
        string myPlayFabId = PlayFabSettings.staticPlayer.PlayFabId;

        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest
            {
                PlayFabId = myPlayFabId,
                Keys = new List<string> { "ProfileIndex" }
            },
            result =>
            {
                int index = 0;

                if (result.Data != null &&
                    result.Data.TryGetValue("ProfileIndex", out var data))
                {
                    int.TryParse(data.Value, out index);
                }

                EquippedProfileImage = index;
                Debug.Log($"내 프로필 이미지 로드 완료 : {index}");
            },
            error =>
            {
                Debug.LogError("내 프로필 이미지 로드 실패: " + error.GenerateErrorReport());
                EquippedProfileImage = 0; // 기본값
            }
        );
    }
}
