using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;
using System.Drawing;
using Unity.Mathematics;
public class BoardSettingManager : MonoBehaviour
{
    public int n;

    private GridLayoutGroup gridLayout;
    public RectTransform boardRoot;
    public RectTransform boardSkin;
    public RectTransform boardInnerPanel;
    public RectTransform boardLowSkin;
    public RectTransform boardDecor1;
    public RectTransform boardDecor2;
    public BoardManager board;
    void Awake()
    {
        gridLayout = boardRoot.GetComponent<GridLayoutGroup>();

        // board가 할당되지 않았으면 자동으로 찾기
        if (board == null)
        {
            board = FindObjectOfType<BoardManager>();
        }
    }
    public void SetupBoardWithSize(int size)
    {
        n = size;
        CreateVisualBoard();
    }

    // =========================
    //  비주얼 생성 - 🔧 수정된 부분
    // =========================

    private void CreateVisualBoard()
    {
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = n;
        }

        Vector2 size = new Vector2(ThemeManager.Instance.selectedTheme.boardWidth, ThemeManager.Instance.selectedTheme.boardHeight);
        float offsetX = ThemeManager.Instance.selectedTheme.boardOffsetX;
        float offsetY = ThemeManager.Instance.selectedTheme.boardOffset;
        Vector2 pos = new Vector2(offsetX, offsetY);

        boardSkin.sizeDelta = size;
        boardLowSkin.sizeDelta = size;
        boardSkin.anchoredPosition = pos;
        boardLowSkin.anchoredPosition = pos;

        if (boardInnerPanel != null)
        {
            boardInnerPanel.sizeDelta = size;
            boardInnerPanel.anchoredPosition = pos;
        }

        if (boardDecor1 != null)
        {
            boardDecor1.sizeDelta = size;
            boardDecor1.anchoredPosition = pos + ThemeManager.Instance.selectedTheme.boardDecor1Offset;
        }
        if (boardDecor2 != null)
        {
            boardDecor2.sizeDelta = size;
            boardDecor2.anchoredPosition = pos + ThemeManager.Instance.selectedTheme.boardDecor2Offset;
        }


        boardRoot.localScale = Vector3.one * ThemeManager.Instance.selectedTheme.scale[n - 3];
    }
}