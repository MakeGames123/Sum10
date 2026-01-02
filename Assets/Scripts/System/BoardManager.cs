using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private bool batteryOptimizationMode = false;

    private Vector2 lastProcessedTouchPosition;
    private const float TOUCH_POSITION_THRESHOLD = 50f;

    // =========================
    //  내부 상태
    // =========================

    private readonly List<CellView> currentPath = new List<CellView>();
    private bool isDragging;
    private int pathSum;
    // 힌트
    private List<CellView> currentHintCells;
    private Coroutine hintCoroutine;

    // 캐싱
    private bool hasValidMoveCache;
    private bool hasValidMoveCacheValid;

    [Tooltip("스마트 첫 터치 최대 거리 (픽셀)")]
    [Range(0f, 300f)]
    [SerializeField] private float smartFirstTouchMaxDistance = 150f;
    [Tooltip("스마트 복구 최소 거리")]
    [Range(0, 5)]
    [SerializeField] private int smartRecoveryMinDistance = 2;
    // 이벤트
    public event Action<List<CellView>> OnCellsRemoved;
    public event Action OnNoMoreMoves;
    public BoardSettingManager boardSettingManager;

    private PathFinder pathFinder = new();

    // =========================
    //  Unity 라이프사이클
    // =========================

    private void Awake()
    {
        SetupFrameRate();
    }

    private void Update()
    {
        // 드래그 중일 때만 체크
        if (isDragging)
        {
            if (!Input.GetMouseButton(0))
            {
                EndDrag();
                return;
            }

            Vector2 currentPos = Input.mousePosition;
            if (Vector2.Distance(currentPos, lastProcessedTouchPosition) > TOUCH_POSITION_THRESHOLD)
            {
                TrySmartPathRecovery();
                lastProcessedTouchPosition = currentPos;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            TrySmartFirstTouchOutsideBoard();
        }
    }

    private void SetupFrameRate()
    {
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.vSyncCount = 0;

        if (batteryOptimizationMode)
        {
            Application.targetFrameRate = 30;
        }
        else
        {
            int maxRefreshRate = (int)Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = maxRefreshRate;
        }
#endif
    }

    private void TrySmartFirstTouchOutsideBoard()
    {
        Vector2 touchPos = Input.mousePosition;

        CellView nearestCell = FindNearestNumberCellToScreenPoint(touchPos);

        if (nearestCell != null)
        {
            var cellRect = nearestCell.GetComponent<RectTransform>();
            if (cellRect != null)
            {
                Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);
                float distance = Vector2.Distance(touchPos, cellScreenPos);

                if (distance <= smartFirstTouchMaxDistance)
                {
                    ResetPath();
                    isDragging = true;
                    lastProcessedTouchPosition = touchPos;
                    TryAddCellToPath(nearestCell);
                }
            }
        }
    }
    private CellView FindNearestNumberCellToScreenPoint(Vector2 screenPos)
    {
        CellView nearest = null;
        float minDistance = float.MaxValue;

        for (int y = 0; y < boardSettingManager.n; y++)
        {
            for (int x = 0; x < boardSettingManager.n; x++)
            {
                var cell = boardSettingManager.cells[y * boardSettingManager.n + x];

                if (cell == null)
                    continue;

                var cellRect = cell.GetComponent<RectTransform>();
                if (cellRect == null)
                    continue;

                Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);
                float dist = Vector2.Distance(screenPos, cellScreenPos);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = cell;
                }
            }
        }

        return nearest;
    }

    private void TrySmartPathRecovery()
    {
        if (currentPath.Count == 0)
            return;

        Vector2 screenPos = Input.mousePosition;
        CellView nearestCell = FindNearestCellToScreenPoint(screenPos);

        if (nearestCell == null)
            return;

        if (currentPath.Count > 0)
        {
            int idx = currentPath.IndexOf(nearestCell);
            if (idx != -1)
            {
                if (idx == currentPath.Count - 1)
                    return;
                BacktrackToIndex(idx);
                return;
            }
        }

        var lastCell = currentPath[currentPath.Count - 1];

        if (currentPath.Contains(nearestCell) || nearestCell == lastCell)
            return;

        if (IsAdjacent(lastCell, nearestCell))
        {
            TryAddCellToPath(nearestCell);
            return;
        }

        int distance = Mathf.Abs(lastCell.X - nearestCell.X) + Mathf.Abs(lastCell.Y - nearestCell.Y);

        if (distance >= smartRecoveryMinDistance && distance <= 4)
        {
            List<CellView> bridge = FindPathBetweenCells(lastCell, nearestCell);

            if (bridge != null && bridge.Count > 0)
            {
                foreach (var cell in bridge)
                {
                    if (!TryAddCellToPath(cell))
                        break;
                }
            }
        }
    }

    private CellView FindNearestCellToScreenPoint(Vector2 screenPos)
    {
        if (boardSettingManager.cells == null)
            return null;

        CellView nearest = null;
        float minDistance = float.MaxValue;

        for (int y = 0; y < boardSettingManager.n; y++)
        {
            for (int x = 0; x < boardSettingManager.n; x++)
            {
                var cell = boardSettingManager.cells[y * boardSettingManager.n + x];
                if (cell == null)
                    continue;

                var cellRect = cell.GetComponent<RectTransform>();
                if (cellRect == null)
                    continue;

                Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);
                float dist = Vector2.Distance(screenPos, cellScreenPos);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = cell;
                }
            }
        }

        return nearest;
    }

    private List<CellView> FindPathBetweenCells(CellView from, CellView to)
    {
        if (from == null || to == null)
            return null;

        var path = new List<CellView>();

        int currentX = from.X;
        int currentY = from.Y;
        int targetX = to.X;
        int targetY = to.Y;

        while (currentX != targetX)
        {
            currentX += (targetX > currentX) ? 1 : -1;

            if (!IsInside(currentX, currentY))
                return null;

            var cell = boardSettingManager.cells[currentY * boardSettingManager.n + currentX];

            if (currentPath.Contains(cell))
                continue;

            path.Add(cell);
        }

        while (currentY != targetY)
        {
            currentY += (targetY > currentY) ? 1 : -1;

            if (!IsInside(currentX, currentY))
                return null;

            var cell = boardSettingManager.cells[currentY * boardSettingManager.n + currentX];

            if (currentPath.Contains(cell))
                continue;

            path.Add(cell);
        }

        return path;
    }



    // =========================
    //  입력 처리
    // =========================

    public void OnCellPointerDown(CellView cell)
    {
        CancelHint();

        if (cell == null)
            return;

        ResetPath();
        isDragging = true;
        lastProcessedTouchPosition = Input.mousePosition;

        TryAddCellToPath(cell);
    }

    public void OnCellPointerEnter(CellView cell)
    {
        if (!isDragging || cell == null)
            return;

        TryAddCellToPath(cell);

        if (currentPath.Contains(cell))
        {
            lastProcessedTouchPosition = Input.mousePosition;
        }
    }

    private void ResetPath()
    {
        foreach (var c in currentPath)
            c.SetHighlight(false);

        currentPath.Clear();
        isDragging = false;
        pathSum = 0;
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && x < boardSettingManager.n && y >= 0 && y < boardSettingManager.n;
    }

    private bool IsAdjacent(CellView a, CellView b)
    {
        int dx = Mathf.Abs(a.X - b.X);
        int dy = Mathf.Abs(a.Y - b.Y);
        return dx + dy == 1;
    }

    private bool AddCellToPathCore(CellView cell)
    {
        if (cell == null)
            return false;

        if (currentPath.Contains(cell))
            return false;

        if (cell.HasNumber)
        {
            pathSum += cell.Value;
        }
        currentPath.Add(cell);
        cell.SetHighlight(true);
        return true;
    }

    private bool TryAddCellToPath(CellView cell)
    {
        if (currentPath.Count > 0)
        {
            int idx = currentPath.IndexOf(cell);
            if (idx != -1)
            {
                if (idx == currentPath.Count - 1)
                    return false;
                BacktrackToIndex(idx);
                return true;
            }
        }

        if (currentPath.Count == 0)
        {
            return AddCellToPathCore(cell);
        }

        var last = currentPath[currentPath.Count - 1];

        if (!IsAdjacent(last, cell))
        {
            int dx = cell.X - last.X;
            int dy = cell.Y - last.Y;

            if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1)
            {
                var bridgeCandidates = new List<CellView>();

                int bx1 = last.X;
                int by1 = cell.Y;
                if (IsInside(bx1, by1))
                {
                    var c1 = boardSettingManager.cells[bx1 + by1 * boardSettingManager.n];
                    if (!c1.HasNumber && !currentPath.Contains(c1))
                        bridgeCandidates.Add(c1);
                }

                int bx2 = cell.X;
                int by2 = last.Y;
                if (IsInside(bx2, by2))
                {
                    var c2 = boardSettingManager.cells[bx1 + by1 * boardSettingManager.n];
                    if (!c2.HasNumber && !currentPath.Contains(c2))
                        bridgeCandidates.Add(c2);
                }

                if (bridgeCandidates.Count > 0)
                {
                    var bridge = bridgeCandidates[UnityEngine.Random.Range(0, bridgeCandidates.Count)];
                    if (!AddCellToPathCore(bridge))
                        return false;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        if (!IsAdjacent(last, cell))
            return false;

        return AddCellToPathCore(cell);
    }

    private void BacktrackToIndex(int idx)
    {
        if (idx < 0 || idx >= currentPath.Count)
            return;

        for (int i = currentPath.Count - 1; i > idx; i--)
        {
            var c = currentPath[i];
            c.SetHighlight(false);
            currentPath.RemoveAt(i);
        }

        RecalculatePathState();
    }

    private void RecalculatePathState()
    {
        pathSum = 0;

        foreach (var cell in currentPath)
        {
            if (!cell.HasNumber) continue;
            pathSum += cell.Value;
        }
    }

    private void EndDrag()
    {
        if (!isDragging) return;

        CancelHint();
        isDragging = false;

        if (ShouldClearCurrentPath())
        {
            var removed = new List<CellView>();
            var cellsToAnimate = new List<CellView>();

            foreach (var cell in currentPath)
            {
                if (cell.HasNumber)
                {
                    boardSettingManager.boardValues[cell.X, cell.Y] = -1;
                    cellsToAnimate.Add(cell);
                    removed.Add(cell);
                }
                cell.SetHighlight(false);
            }

            // 애니메이션 먼저 시작 (fire and forget - 보드 리셋 시 Init에서 kill됨)
            if (cellsToAnimate.Count > 0)
            {
                PlayCellRemoveAnimations(cellsToAnimate);
            }

            // 게임 로직 처리 (애니메이션 시작 후)
            OnCellsRemoved?.Invoke(removed);
            InvalidateCache();
            CheckEndConditions();
        }
        else
        {
            foreach (var cell in currentPath)
                cell.SetHighlight(false);
        }

        currentPath.Clear();
        pathSum = 0;
    }

    /// <summary>
    /// 셀 제거 애니메이션 실행 (시각적 효과만, fire and forget)
    /// </summary>
    private void PlayCellRemoveAnimations(List<CellView> cells)
    {
        foreach (var cell in cells)
        {
            cell.PlayDisappearAndTransform(null);
        }
    }

    private bool ShouldClearCurrentPath()
    {
        return pathSum == ImportantValues.TARGET_SUM;
    }

    // =========================
    //  캐시 관리
    // =========================

    private void InvalidateCache()
    {
        hasValidMoveCacheValid = false;
    }

    // =========================
    //  종료 판정
    // =========================

    private void CheckEndConditions()
    {
        bool anyNumber = false;
        for (int y = 0; y < boardSettingManager.n && !anyNumber; y++)
        {
            for (int x = 0; x < boardSettingManager.n; x++)
            {
                if (boardSettingManager.boardValues[x, y] > 0)
                {
                    anyNumber = true;
                    break;
                }
            }
        }

        if (!anyNumber)
        {
            OnNoMoreMoves?.Invoke();
            return;
        }

        if (!HasAnyValidMove())
        {
            OnNoMoreMoves?.Invoke();
        }
    }

    private bool HasAnyValidMove()
    {
        if (hasValidMoveCacheValid)
            return hasValidMoveCache;

        bool result = pathFinder.FindPathBFS(out _, boardSettingManager.boardValues, boardSettingManager.n);

        hasValidMoveCache = result;
        hasValidMoveCacheValid = true;

        return result;
    }



    // =========================
    //  힌트
    // =========================

    public List<CellView> FindHintPath()
    {
        if (boardSettingManager.cells == null || boardSettingManager.boardValues == null)
            return null;

        List<Vector2Int> pathPositions;


        if (!pathFinder.FindPathBFS(out pathPositions, boardSettingManager.boardValues, boardSettingManager.n))
            return null;

        if (pathPositions == null || pathPositions.Count == 0)
            return null;

        var result = new List<CellView>(pathPositions.Count);
        foreach (var pos in pathPositions)
        {
            var cell = boardSettingManager.cells[pos.x + pos.y * boardSettingManager.n];
            if (cell != null)
                result.Add(cell);
        }

        return result;
    }

    public void ShowHint(float flashDuration = 1.0f)
    {
        CancelHint();

        var path = FindHintPath();

        if (path == null || path.Count == 0)
            return;

        currentHintCells = path;
        hintCoroutine = StartCoroutine(HintRoutine(flashDuration));
    }

    public void CancelHint()
    {
        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }

        if (currentHintCells != null)
        {
            foreach (var cell in currentHintCells)
                cell.SetHintHighlight(false);

            currentHintCells = null;
        }
    }

    private IEnumerator HintRoutine(float duration)
    {
        foreach (var cell in currentHintCells)
            cell.SetHintHighlight(true);

        yield return new WaitForSeconds(duration);

        CancelHint();
    }
}

public class PathState
{
    public int x, y;
    public int sum;
    public List<Vector2Int> path;
    public bool[,] visited;

    public PathState(int x, int y, int sum, bool[,] visited, List<Vector2Int> path)
    {
        this.x = x;
        this.y = y;
        this.sum = sum;
        this.visited = visited;
        this.path = path;
    }
}