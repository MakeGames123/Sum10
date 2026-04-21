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
    public Image boardInnerPanel;
    public Image boardLowSkin;
    public Image boardDecor1;
    public Image boardDecor2;
    public Image background;
    public RectTransform scoreSection;
    public RectTransform timerSection;
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
        if (selectedIndex < 0 || selectedIndex >= themeDatas.Count)
            selectedIndex = 0;
        if (themeDatas.Count > 0)
            ChangeTheme(themeDatas[selectedIndex], selectedIndex);

        // 서버 로드 전 기본값 (0번만 해금) - themeStatus가 비어있을 때 크래시 방지
        for (int i = 0; i < themeDatas.Count; i++)
            themeStatus.Add(i == 0 ? 1 : 0);

        login.onLogined.AddListener(LoadProfileStatusFromServer);
    }

    public void ChangeTheme(ThemeData newTheme, int index)
    {
        selectedIndex = index;
        selectedTheme = newTheme;
        board.sprite = selectedTheme.boardSkin;
        if (boardInnerPanel != null)
        {
            boardInnerPanel.sprite = selectedTheme.boardInnerPanel;
            boardInnerPanel.enabled = selectedTheme.boardInnerPanel != null;
        }
        boardLowSkin.sprite = selectedTheme.boardLowSkin;
        if (boardDecor1 != null)
        {
            boardDecor1.sprite = selectedTheme.boardDecor1;
            boardDecor1.enabled = selectedTheme.boardDecor1 != null;
            var mover1 = boardDecor1.GetComponent<BoardDecorMoving>();
            if (mover1 != null) mover1.enabled = selectedTheme.boardDecor1Moving;
        }
        if (boardDecor2 != null)
        {
            boardDecor2.sprite = selectedTheme.boardDecor2;
            boardDecor2.enabled = selectedTheme.boardDecor2 != null;
            var mover2 = boardDecor2.GetComponent<BoardDecorMoving>();
            if (mover2 != null) mover2.enabled = selectedTheme.boardDecor2Moving;
        }
        if (background != null && selectedTheme.background != null)
            background.sprite = selectedTheme.background;

        if (scoreSection != null)
            scoreSection.anchoredPosition = selectedTheme.scoreSectionPos;
        if (timerSection != null)
            timerSection.anchoredPosition = selectedTheme.timerSectionPos;

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

                    // 신규 테마 추가 시 기존 유저 데이터 확장 (미소유로 패딩)
                    while (themeStatus.Count < themeDatas.Count)
                        themeStatus.Add(0);
                }
                else
                {
                    themeStatus = new List<int>();
                    for (int i = 0; i < themeDatas.Count; i++)
                        themeStatus.Add(i == 0 ? 1 : 0); // 0번만 해금
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