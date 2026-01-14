using System;
using UnityEngine;

public class Cell
{
    int num;
    bool cellLock = false;
    public Vector2Int Position { get; private set; }
    public Action<bool> onValueChanged;
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
        onEnableHintEvent?.Invoke();
    }
    public void DisableHintMode()
    {
        onDisableHintEvent?.Invoke();
    }
}
