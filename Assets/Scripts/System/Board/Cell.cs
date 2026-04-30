using System;
using UnityEngine;

public class Cell
{
    int num;
    bool cellLock = false;
    public bool isHintCell = false;
    public Vector2Int Position { get; private set; }
    public Action<bool> onValueChanged;
    public Action onRemoved;
    public Action onCellSelectedEvent;
    public Action onCellUnSelectedEvent;
    public Action onEnableHintEvent;
    public Action onDisableHintEvent;
    public void SetPosition(int x, int y)
    {
        Position = new Vector2Int(x, y);
    }
    public void SetNum(int n)
    {
        bool isAlreadyBlank = num == 0 && n == 0;
        num = n;
        onValueChanged?.Invoke(isAlreadyBlank);
        if (num == 0)
        {
            isHintCell = false;
            onRemoved?.Invoke();
        }
    }
    public int ReturnNum()
    {
        return num;
    }
    public bool ReturnLock()
    {
        return cellLock;
    }
    public void UpdateCellLock(bool flag)
    {
        cellLock = flag;
    }
    public void OnSelect()
    {
        if (cellLock) return;

        onCellSelectedEvent?.Invoke();
    }
    public void UnSelect()
    {
        onCellUnSelectedEvent?.Invoke();
    }
    public void EnableHintMode()
    {
        isHintCell = true;
        onEnableHintEvent?.Invoke();
    }
    public void DisableHintMode()//isHintCell 변경은 setnum에서 왜냐하면 보드매니저 CancelHint랑 호출 순서가 꼬임
    {
        onDisableHintEvent?.Invoke();
    }
}
