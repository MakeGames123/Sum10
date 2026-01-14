using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectionController
{
    private readonly PathFinder pathFinder = new();
    private readonly List<CellSlot> cellSlots;
    private List<Cell> allCells;
    private readonly List<Cell> selectedCells = new();
    private bool isDragging;
    public Action OnKeyPadBlocked; //판 엎기
    public Action<int> OnCellRemoved; //판 엎기

    public SelectionController(List<CellSlot> cellSlots)
    {
        this.cellSlots = cellSlots;
    }
    public void UpdateCells(List<Cell> allCells)
    {
        this.allCells = allCells;
        pathFinder.SetSize((int)Mathf.Sqrt(allCells.Count));
    }
    public void Begin()
    {
        isDragging = true;
        selectedCells.Clear();
    }

    public void Update(Vector2 pointerScreenPos)
    {
        if (!isDragging) return;

        Cell cell = pathFinder.FindNearestCellToScreenPoint(pointerScreenPos, cellSlots);
        if (cell == null)
        {
            End();
            return;
        }

        AddCell(cell);
    }

    public void End()
    {
        if (!isDragging) return;

        isDragging = false;

        int sum = 0;
        foreach (var cell in selectedCells)
        {
            sum += cell.ReturnNum() > 0 ? cell.ReturnNum() : 0;
        }
        if (sum == 10)
        {
            foreach (var cell in selectedCells)
            {
                cell.SetNum(0);
            }

            OnCellRemoved?.Invoke(selectedCells.Count);
        }
        else
        {
            foreach (var cell in selectedCells) cell.UnSelect();//UnSelect판정은 셀 파괴랑 다른 판정
        }

        selectedCells.Clear();

        if (!pathFinder.HasAnyValidPath(allCells)) OnKeyPadBlocked?.Invoke();
    }

    // ===== Internal Logic =====

    private void AddCell(Cell cell)
    {
        if (cell.ReturnLock()) return;

        // 백트래킹
        if (selectedCells.Contains(cell))
        {
            int idx = selectedCells.IndexOf(cell);
            BacktrackToIndex(idx);
            return;
        }

        // 일반 추가
        List<Cell> bridge = pathFinder.FindPathBetweenCells(
            selectedCells.LastOrDefault(),
            cell,
            selectedCells,
            allCells
        );

        if (bridge == null)
        {
            End();
            return;
        }

        foreach (var newCell in bridge)
        {
            selectedCells.Add(newCell);
            newCell.OnSelect();
        }
    }

    private void BacktrackToIndex(int idx)
    {
        for (int i = selectedCells.Count - 1; i > idx; i--)
        {
            selectedCells[i].UnSelect();
            selectedCells.RemoveAt(i);
        }
    }
}
