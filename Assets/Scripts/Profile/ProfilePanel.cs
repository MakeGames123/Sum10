using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
public class ProfilePanel : MonoBehaviour
{
    [SerializeField] Image profileImage;
    [SerializeField] ScrollRect scrollRect;
    public List<ProfileSlot> slots = new();
    public Button equipButton;
    public Button buyButton;
    public RectTransform diaShop;
    [SerializeField] BoardManager skinBoard;
    int currentIndex = 0;
    private const string PROFILE_INDEX_KEY = "ProfileIndex";
    void Awake()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetCondition(ProfileList.Instance.profileList[i], i);

            slots[i].onClick.AddListener(SelectProfile);
        }

        equipButton.onClick.AddListener(TryEquipProfile);
        buyButton.onClick.AddListener(() => RequestUnlockProfile(currentIndex));
    }
    void Start()
    {
        PlayerData.Instance.onProfileImageChanged.AddListener(SyncCheck);
        SyncCheck(PlayerData.Instance.EquippedProfileImage);
    }
    public void SetCondition()
    {
        if (scrollRect != null)
            StartCoroutine(ResetScroll());
        SelectProfile(PlayerData.Instance.EquippedProfileImage);
        skinBoard.LockCells();
    }
    public void Disable()
    {
        skinBoard.UnlockCells();
    }
    private IEnumerator ResetScroll()
    {
        yield return null;
        scrollRect.verticalNormalizedPosition = 1f;
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

        if (PlayerData.Instance.profileStatus[currentIndex] == 0)
        {
            buyButton.gameObject.SetActive(true);
            equipButton.gameObject.SetActive(false);
        }
        else if (PlayerData.Instance.profileStatus[currentIndex] == 1 && PlayerData.Instance.EquippedProfileImage == currentIndex)
        {
            buyButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(true);
        }
        else
        {
            buyButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(true);
        }
    }
    public void TryEquipProfile()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].DisableCheck();
        }
        slots[currentIndex].EnableCheck();
        PlayerData.Instance.EquippedProfileImage = currentIndex;
        profileImage.sprite = ProfileList.Instance.profileList[currentIndex];
        SaveProfileIndexToPlayFab(currentIndex);
    }
    public void RequestUnlockProfile(int index)
    {
        if (PlayerData.Instance.GetDiamond() < 20)
        {
            diaShop.anchoredPosition = Vector2.zero;
            return;
        }

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "UnlockProfile",
            FunctionParameter = new { index = index }
        };

        PlayFabClientAPI.ExecuteCloudScript(
            request,
            result =>
            {
                if (result.FunctionResult != null)
                {
                    var data = result.FunctionResult as IDictionary<string, object>;
                    Debug.Log((bool)data["success"]);
                    if ((bool)data["success"])
                    {
                        var diamond = System.Convert.ToInt32(data["newDiamond"]);
                        PlayerData.Instance.SetDiamond(diamond);

                        var statusList = data["profileStatus"] as List<object>;

                        PlayerData.Instance.profileStatus.Clear();

                        foreach (var s in statusList)
                        {
                            PlayerData.Instance.profileStatus.Add(
                                System.Convert.ToInt32(s)
                            );
                        }

                        SelectProfile(index);
                    }
                }
            },
            error =>
            {
                Debug.LogError("해금 요청 실패: " + error.GenerateErrorReport());
            }
        );
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
