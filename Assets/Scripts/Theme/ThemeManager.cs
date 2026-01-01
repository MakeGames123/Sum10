using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;
    
    // 현재 선택된 테마 데이터
    public ThemeData selectedTheme;
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

        ChangeTheme(selectedTheme);
    }

    public void ChangeTheme(ThemeData newTheme)
    {
        selectedTheme = newTheme;
        board.sprite = selectedTheme.boardSkin;
        boardLowSkin.sprite = selectedTheme.boardLowSkin;
        background.sprite = selectedTheme.background;
    }
}