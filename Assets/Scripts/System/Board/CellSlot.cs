using UnityEngine;
public class CellSlot : MonoBehaviour
{
    public Cell cellInfo;
    public void SetCondition(Cell cellInfo)
    {
        this.cellInfo = cellInfo;
    }
}
