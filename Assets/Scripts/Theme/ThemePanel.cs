using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemePanel : MonoBehaviour, IMainPanel
{
    public Button rightButton;
    public Button leftButton;
    public Button applyButton;
    public List<ThemeData> themeDatas = new();
    public Image boardBackground;
    public Image boardLowSpecial;
    public ThemeBoardSetting setting;
    private int index = 0;

    void Awake()
    {
        rightButton.onClick.AddListener(MoveToRight);
        leftButton.onClick.AddListener(MoveToLeft);
        applyButton.onClick.AddListener(Apply);

        UpdateUI();
    }
    public void SetCondition()
    {
        UpdateUI();
    }
    private void MoveToRight()
    {
        index++;
        if (index >= themeDatas.Count) index = 0;

        UpdateUI();
    }
    private void MoveToLeft()
    {
        index--;
        if (index < 0) index = themeDatas.Count - 1;

        UpdateUI();
    }
    private void UpdateUI()
    {
        boardBackground.sprite = themeDatas[index].boardSkin;
        boardLowSpecial.sprite = themeDatas[index].boardLowSkin;

        setting.SetupBoardWithSize();
    }
    private void Apply()
    {
        ThemeManager.Instance.ChangeTheme(themeDatas[index]);
    }
}
