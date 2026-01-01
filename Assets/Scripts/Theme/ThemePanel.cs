using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemePanel : MonoBehaviour
{
    public Button rightButton;
    public Button leftButton;
    public Button applyButton;
    public List<ThemeData> themeDatas = new();
    public Image themeThumbnail;
    public Image background;
    public Image gameBackground;
    private int index = 0;

    void Awake()
    {
        rightButton.onClick.AddListener(MoveToRight);
        leftButton.onClick.AddListener(MoveToLeft);
        applyButton.onClick.AddListener(Apply);

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
        themeThumbnail.sprite = themeDatas[index].themeThumbnail;
        background.sprite = themeDatas[index].background;
    }
    private void Apply()
    {
        ThemeManager.Instance.ChangeTheme(themeDatas[index]);
    }
}
