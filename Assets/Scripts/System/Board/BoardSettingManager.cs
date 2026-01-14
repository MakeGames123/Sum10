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
    public RectTransform boardLowSkin;
    [SerializeField] private CellView cellPrefab;
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

        boardSkin.sizeDelta = new Vector2(ThemeManager.Instance.selectedTheme.boardWidth, ThemeManager.Instance.selectedTheme.boardHeight);
        boardLowSkin.sizeDelta = new Vector2(ThemeManager.Instance.selectedTheme.boardWidth, ThemeManager.Instance.selectedTheme.boardHeight);
        boardSkin.anchoredPosition = new Vector2(0, ThemeManager.Instance.selectedTheme.boardOffset);
        boardLowSkin.anchoredPosition = new Vector2(0, ThemeManager.Instance.selectedTheme.boardOffset);


        boardRoot.localScale = Vector3.one * ThemeManager.Instance.selectedTheme.scale[n - 3];
    }
}