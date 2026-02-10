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
    public UnityEvent<int> onProfileImageChanged = new();
    private int localDiamond;
    public UnityEvent<int> onDiamondChanged = new();
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
    private bool isAdRemoved = false;
    public UnityEvent<bool> onAdRemovedChanged = new();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else { Destroy(gameObject); }

        login.onLogined.AddListener(LoadDiamondFromServer);
    }
    public void LoadDiamondFromServer()
    {
        PlayFabClientAPI.GetUserInventory(
            new GetUserInventoryRequest(),
            result =>
            {
                if (result.VirtualCurrency != null &&
                    result.VirtualCurrency.TryGetValue("DM", out int diamond))
                {
                    localDiamond = diamond;
                    onDiamondChanged?.Invoke(localDiamond);
                    Debug.Log($"다이아 로드 성공: {localDiamond}");
                }
                else
                {
                    localDiamond = 0;
                    Debug.Log("다이아 없음 (0으로 초기화)");
                }
            },
            error =>
            {
                Debug.LogError("다이아 로드 실패: " + error.GenerateErrorReport());
            }
        );
    }
    public int GetDiamond()
    {
        return localDiamond;
    }
    public void AdjustDiamone(int val)
    {
        localDiamond += val;
        onDiamondChanged?.Invoke(localDiamond);
    }
    public void SetAdStatus(bool flag)
    {
        isAdRemoved = flag;
        onAdRemovedChanged.Invoke(flag);
    }
    public bool GetAdStatus()
    {
        return isAdRemoved;
    }
}
