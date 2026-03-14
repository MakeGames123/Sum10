using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;
    private const string ThemeIndexKey = "SelectedThemeIndex";
    public List<ThemeData> themeDatas = new();
    public List<int> themeStatus = new();
    // 현재 선택된 테마 데이터
    public ThemeData selectedTheme;
    public int selectedIndex = 0;
    public Image board;
    public Image boardLowSkin;
    public Image background;
    public PlayFabLoginManager login;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else { Destroy(gameObject); }

        int selectedIndex = PlayerPrefs.GetInt(ThemeIndexKey, 0);
        ChangeTheme(themeDatas[selectedIndex], selectedIndex);

        login.onLogined.AddListener(LoadProfileStatusFromServer);
    }

    public void ChangeTheme(ThemeData newTheme, int index)
    {
        selectedIndex = index;
        selectedTheme = newTheme;
        board.sprite = selectedTheme.boardSkin;
        boardLowSkin.sprite = selectedTheme.boardLowSkin;
        if (background != null && selectedTheme.background != null)
            background.sprite = selectedTheme.background;

        PlayerPrefs.SetInt(ThemeIndexKey, selectedIndex);
        PlayerPrefs.Save();
    }
    public void LoadProfileStatusFromServer()
    {
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null &&
                    result.Data.TryGetValue("THEME_STATUS", out var data))
                {
                    themeStatus.Clear();

                    string[] values = data.Value.Split(',');

                    foreach (var v in values)
                    {
                        if (int.TryParse(v, out int parsed))
                            themeStatus.Add(parsed);
                        else
                            themeStatus.Add(0);
                    }
                }
                else
                {

                    // 기본값 필요하면 여기서 초기화
                    themeStatus = new List<int> { 1, 0, 0 }; // 기본 프로필 0번 해금
                    SaveProfileStatusToServer();
                }
            },
            error =>
            {
                Debug.LogError("테마 데이터 로드 실패: " + error.GenerateErrorReport());
            }
        );
    }
    public void SaveProfileStatusToServer()
    {
        string joined = string.Join(",", themeStatus);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "THEME_STATUS", joined }
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

}