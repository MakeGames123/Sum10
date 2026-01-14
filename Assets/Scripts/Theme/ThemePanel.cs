using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    public BoardSettingManager setting;
    public BoardManager board;
    private int index = 0;

    void Awake()
    {
        rightButton.onClick.AddListener(MoveToRight);
        leftButton.onClick.AddListener(MoveToLeft);
        applyButton.onClick.AddListener(Apply);
    }
    public void SetCondition()
    {
        UpdateUI();
    }
    public void OnDisable()
    {
        board.gameObject.SetActive(false);
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

        SetUp();
    }
    public void SetUp()
    {
        board.gameObject.SetActive(true);
        setting.SetupBoardWithSize(3);
        board.SetupBoardWithSize(3);
    }
    private void Apply()
    {
        ThemeManager.Instance.ChangeTheme(themeDatas[index]);
    }
}
