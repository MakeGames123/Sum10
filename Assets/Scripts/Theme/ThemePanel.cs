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
    private ThemeData appliedTheme;

    void Awake()
    {
        rightButton.onClick.AddListener(MoveToRight);
        leftButton.onClick.AddListener(MoveToLeft);
        applyButton.onClick.AddListener(Apply);
    }
    public void SetCondition()
    {
        // 현재 적용된 테마 기억 (닫을 때 복원용)
        appliedTheme = ThemeManager.Instance.selectedTheme;

        // 현재 적용된 테마의 index 찾기
        int found = themeDatas.IndexOf(appliedTheme);
        if (found >= 0) index = found;

        UpdateUI();
    }
    public void OnDisable()
    {
        board.gameObject.SetActive(false);

        // Apply 안 하고 닫았으면 원래 테마로 복원
        if (appliedTheme != null)
            ThemeManager.Instance.ChangeTheme(appliedTheme);
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
        // 프리뷰용: selectedTheme을 미리 변경하여 CellView.Init()이 올바른 스프라이트를 읽도록 함
        ThemeManager.Instance.selectedTheme = themeDatas[index];

        boardBackground.sprite = themeDatas[index].boardSkin;
        boardLowSpecial.sprite = themeDatas[index].boardLowSkin;

        var bg = ThemeManager.Instance.background;
        if (bg != null && themeDatas[index].background != null)
            bg.sprite = themeDatas[index].background;

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
        appliedTheme = themeDatas[index]; // 적용 완료 → 닫을 때 복원 안 함
    }

#if UNITY_EDITOR
    private void Update()
    {
        // F7: SO 값 변경 후 즉시 프리뷰 리빌드
        if (Input.GetKeyDown(KeyCode.F7))
            UpdateUI();
    }
#endif
}
