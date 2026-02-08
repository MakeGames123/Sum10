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

}
