using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;
    private const string ThemeIndexKey = "SelectedThemeIndex";
    public List<ThemeData> themeDatas = new();
    // 현재 선택된 테마 데이터
    public ThemeData selectedTheme;
    public int selectedIndex = 0;
    public Image board;
    public Image boardLowSkin;
    public Image background;
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
}