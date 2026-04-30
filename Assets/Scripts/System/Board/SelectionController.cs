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
    public Action<int, bool> OnCellRemoved;

    // 사운드 연결용 이벤트
    public Action OnSelectionStarted;       // 드래그 시작
    public Action<int> OnCellSelected;      // 셀 선택 (pathCount 전달)
    public Action<int> OnCellDeselected;    // 셀 해제 (remainingCount 전달)
    public Action OnCellsDestroyed;         // 셀 파괴

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
        OnSelectionStarted?.Invoke();
    }

    public void Update(Vector2 pointerScreenPos)
    {
        if (!isDragging) return;

        Cell cell = pathFinder.FindNearestCellToScreenPoint(pointerScreenPos, cellSlots); //셀 클릭이 조건이 아닌 그냥 클릭중 가장 가까운 셀을 매 프레임마다 받는다
        if (cell == null)
        {
            End();
            return;
        }

        AddCell(cell);
    }

    //드래그 종료
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
            int gained = 0;
            bool isHintPath = true;

            foreach (var cell in selectedCells)
            {
                if(cell.ReturnNum() > 0) gained++;
                if(!cell.isHintCell) isHintPath = false;
                cell.SetNum(0);
            }

            OnCellsDestroyed?.Invoke();

            OnCellRemoved?.Invoke(gained, isHintPath);
        }
        else
        {
            foreach (var cell in selectedCells) cell.UnSelect();//UnSelect판정은 셀 파괴랑 다른 판정
        }

        selectedCells.Clear();

        if (allCells != null && !pathFinder.HasAnyValidPath(allCells)) OnKeyPadBlocked?.Invoke();
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
            OnCellSelected?.Invoke(selectedCells.Count);
        }

        VibrationManager.Instance.Light();
    }

    private void BacktrackToIndex(int idx)
    {
        for (int i = selectedCells.Count - 1; i > idx; i--)
        {
            selectedCells[i].UnSelect();
            OnCellDeselected?.Invoke(i);
            selectedCells.RemoveAt(i);
        }
    }
}
