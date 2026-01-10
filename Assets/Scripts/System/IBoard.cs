/// <summary>
/// BoardManager와 ThemeBoard 공통 인터페이스
/// CellView에서 보드 상호작용용
/// </summary>
public interface IBoard
{
    bool IsFirstActiveHintCell(CellView cell);
    void OnCellPointerDown(CellView cell);
    void OnCellPointerEnter(CellView cell);
}
