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
    public List<int> profileStatus = new();
    public string nickName;
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
        login.onLogined.AddListener(LoadProfileStatusFromServer);
        login.onLogined.AddListener(LoadAdStatusFromServer);
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
    public void LoadProfileStatusFromServer()
    {
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null &&
                    result.Data.TryGetValue("PROFILE_STATUS", out var data))
                {
                    profileStatus.Clear();

                    string[] values = data.Value.Split(',');

                    foreach (var v in values)
                    {
                        if (int.TryParse(v, out int parsed))
                            profileStatus.Add(parsed);
                        else
                            profileStatus.Add(0);
                    }

                    Debug.Log("프로필 상태 로드 완료");
                }
                else
                {
                    Debug.Log("프로필 데이터 없음 → 기본값 생성");

                    // 기본값 필요하면 여기서 초기화
                    profileStatus = new List<int> { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // 기본 프로필 0번 해금
                    SaveProfileStatusToServer();
                }
            },
            error =>
            {
                Debug.LogError("프로필 데이터 로드 실패: " + error.GenerateErrorReport());
            }
        );
    }
    public void SaveProfileStatusToServer()
    {
        string joined = string.Join(",", profileStatus);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "PROFILE_STATUS", joined }
        }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log("프로필 상태 저장 완료");
            },
            error =>
            {
                Debug.LogError("프로필 상태 저장 실패: " + error.GenerateErrorReport());
            }
        );
    }
    public void LoadAdStatusFromServer()
    {
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null &&
                    result.Data.TryGetValue("removeAds", out var data))
                {
                    if (int.TryParse(data.Value, out int parsed))
                    {
                        isAdRemoved = parsed == 1;
                    }
                    else
                    {
                        isAdRemoved = false;
                    }

                    onAdRemovedChanged?.Invoke(isAdRemoved);
                    Debug.Log("광고 제거 상태 로드 완료: " + isAdRemoved);
                }
                else
                {
                    Debug.Log("광고 제거 데이터 없음 → 기본값 생성");

                    // 기본값 false
                    isAdRemoved = false;
                    onAdRemovedChanged?.Invoke(isAdRemoved);

                    SaveAdStatusToServer();
                }
            },
            error =>
            {
                Debug.LogError("광고 제거 상태 로드 실패: " + error.GenerateErrorReport());
            }
        );
    }
    public void SaveAdStatusToServer()
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "removeAds", isAdRemoved ? "1" : "0" }
        }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log("광고 제거 상태 저장 완료");
            },
            error =>
            {
                Debug.LogError("광고 제거 상태 저장 실패: " + error.GenerateErrorReport());
            }
        );
    }
    public int GetDiamond()
    {
        return localDiamond;
    }
    public void AdjustDiamond(int val)
    {
        localDiamond += val;
        onDiamondChanged?.Invoke(localDiamond);
    }
    public void SetDiamond(int val)
    {
        localDiamond = val;
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
