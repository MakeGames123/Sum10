using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ThemeBoard : MonoBehaviour, IBoard
{
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
    public ThemeBoardSetting boardSettingManager;

    [Header("Panel Reference")]
    [SerializeField] private RectTransform panelTransform;  // 테마 패널의 RectTransform

    private PathFinder pathFinder = new();

    /// <summary>
    /// 테마 패널이 화면에 보이는지 확인 (위치 기반)
    /// </summary>
    private bool IsPanelVisible()
    {
        if (panelTransform == null) return true;  // 참조 없으면 항상 true

        // 패널 위치가 (0,0) 근처면 보이는 상태
        Vector2 pos = panelTransform.anchoredPosition;
        return Mathf.Abs(pos.x) < 1000f && Mathf.Abs(pos.y) < 1000f;
    }

    // =========================
    //  Unity 라이프사이클
    // =========================
    private float hintIdleThreshold = 5f;
    private bool hintShownForCurrentIdle = false;
    private float timeProgress = 0;

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

        // ----- 힌트용 idle 타이머 -----
        timeProgress += Time.deltaTime;

        if (!hintShownForCurrentIdle && timeProgress >= hintIdleThreshold)
        {
            var hintPath = FindHintPath();
            if (hintPath != null && hintPath.Count > 0)
            {
                ShowHint(1);
                hintShownForCurrentIdle = true;
            }
        }
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
        // CancelHint();  // 임시 비활성화: 클릭해도 힌트 유지

        // 힌트 타이머 리셋
        ResetHintTimer();

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
            //c.SetHighlight(false);

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

        //if (cell.HasNumber)
        //{
        //    pathSum += cell.Value;
        //}
        currentPath.Add(cell);
        //cell.SetHighlight(true);

        // 셀 선택 효과음
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPopSFX(currentPath.Count);
        }

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
                    //if (!c1.HasNumber && !currentPath.Contains(c1))
                    //    bridgeCandidates.Add(c1);
                }

                int bx2 = cell.X;
                int by2 = last.Y;
                if (IsInside(bx2, by2))
                {
                    var c2 = boardSettingManager.cells[bx1 + by1 * boardSettingManager.n];
                    //if (!c2.HasNumber && !currentPath.Contains(c2))
                    //    bridgeCandidates.Add(c2);
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

        int removedCount = currentPath.Count - 1 - idx;

        for (int i = currentPath.Count - 1; i > idx; i--)
        {
            var c = currentPath[i];
            //c.SetHighlight(false);
            currentPath.RemoveAt(i);
        }

        RecalculatePathState();

        // 셀 해제 효과음
        if (removedCount > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDeselectSFX(currentPath.Count);
        }
    }

    private void RecalculatePathState()
    {
        pathSum = 0;

        foreach (var cell in currentPath)
        {
            //if (!cell.HasNumber) continue;
            //pathSum += cell.Value;
        }
    }

    private void EndDrag()
    {
        if (!isDragging) return;

        // CancelHint();  // 임시 비활성화: 드래그 종료해도 힌트 유지
        isDragging = false;

        if (ShouldClearCurrentPath())
        {
            // 매치 성공 시 힌트 타이머 리셋 (다음 힌트를 위해)
            ResetHintTimer();

            var removed = new List<CellView>();
            var cellsToAnimate = new List<CellView>();

            foreach (var cell in currentPath)
            {
                //if (cell.HasNumber)
                //{
                //    boardSettingManager.boardValues[cell.X, cell.Y] = -1;
                //    cellsToAnimate.Add(cell);
                //    removed.Add(cell);
                //}
                //cell.SetHighlight(false);
            }

            // 애니메이션 먼저 시작 (fire and forget - 보드 리셋 시 Init에서 kill됨)
            if (cellsToAnimate.Count > 0)
            {
                PlayCellRemoveAnimations(cellsToAnimate);
            }

            // 게임 로직 처리 (애니메이션 시작 후)
            OnCellsRemoved?.Invoke(removed);

            // 힌트 경로에 포함된 셀이 제거되면 힌트 취소
            if (currentHintCells != null && currentHintCells.Count > 0)
            {
                foreach (var cell in removed)
                {
                    if (currentHintCells.Contains(cell))
                    {
                        CancelHint();
                        break;
                    }
                }
            }

            InvalidateCache();
            CheckEndConditions();
        }
        else
        {
            // 합이 10이 아닌 채로 놓으면 해제 효과음
            if (currentPath.Count > 0 && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDeselectSFX(0);
            }

            // 힌트 셀이 포함되어 있는지 체크
            bool hasHintCells = false;
            if (currentHintCells != null)
            {
                foreach (var cell in currentPath)
                {
                    if (currentHintCells.Contains(cell))
                    {
                        hasHintCells = true;
                        break;
                    }
                }
            }

            foreach (var cell in currentPath)
                //cell.SetHighlight(false);

                // 힌트 셀이 선택되었다가 해제되면 playSound:false로 처리되므로
                // ResyncHintAnimations로 사운드 콜백 복원 필요
                if (hasHintCells)
                {
                    ResyncHintAnimations();
                }
        }

        currentPath.Clear();
        pathSum = 0;
    }

    /// <summary>
    /// 셀 제거 애니메이션 실행 (시각적 효과만, fire and forget)
    /// </summary>
    private void PlayCellRemoveAnimations(List<CellView> cells)
    {
        // 매치 성공 효과음
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCellDestroySFX();

        foreach (var cell in cells)
        {
            //cell.PlayDisappearAndTransform(null);
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
            CancelHint();
            ResetHintTimer();
            boardSettingManager.SetupBoardWithSize();
            return;
        }

        if (!HasAnyValidMove())
        {
            CancelHint();
            ResetHintTimer();
            boardSettingManager.SetupBoardWithSize();
        }
    }

    private bool HasAnyValidMove()
    {
        /*
        if (hasValidMoveCacheValid)
            return hasValidMoveCache;

        bool result = pathFinder.FindPathBFS(out _, boardSettingManager.boardValues, boardSettingManager.n);

        hasValidMoveCache = result;
        hasValidMoveCacheValid = true;

        return result;
        */
        return false;
    }



    // =========================
    //  힌트
    // =========================

    public List<CellView> FindHintPath()
    {
        if (boardSettingManager.cells == null || boardSettingManager.boardValues == null)
            return null;

        List<Vector2Int> pathPositions;

        return null;
        /*

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
                */
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
                //cell.SetHintHighlight(false);

                currentHintCells = null;
        }
    }

    /// <summary>
    /// 힌트 타이머 리셋 (사용자 상호작용 시 호출)
    /// </summary>
    private void ResetHintTimer()
    {
        timeProgress = 0;
        hintShownForCurrentIdle = false;
    }

    /// <summary>
    /// 주어진 셀이 현재 활성 힌트 셀 중 첫 번째인지 확인 (사운드 리더 동적 결정)
    /// </summary>
    /// 
    public bool IsFirstActiveHintCell(CellView cell)
    {/*
        // 패널이 화면에 없으면 힌트 사운드 재생 안 함
        if (!IsPanelVisible())
            return false;

        if (currentHintCells == null || currentHintCells.Count == 0)
            return false;

        // 힌트 상태이면서 선택되지 않은 첫 번째 셀 찾기
        foreach (var hintCell in currentHintCells)
        {
            if (hintCell.IsHint && !hintCell.IsSelected)
                return hintCell == cell;
        }
*/
        return false;
    }
    /// <summary>
    /// 모든 힌트 셀의 애니메이션을 동기화 (선택 해제 후 싱크 맞추기)
    /// </summary>
    private void ResyncHintAnimations()
    {
        if (currentHintCells == null || currentHintCells.Count == 0)
            return;

        foreach (var cell in currentHintCells)
        {
            cell.ForceRestartHintAnimation();
        }
    }

    private IEnumerator HintRoutine(float duration)
    {
        foreach (var cell in currentHintCells)
            //cell.SetHintHighlight(true);

            // 힌트 무한 루프: 애니메이션이 계속 반복됨 (CellView에서 SetLoops(-1) 설정)
            // duration 후 자동 취소 비활성화
            yield break;

        // 기존 로직 (임시 비활성화)
        // yield return new WaitForSeconds(duration);
        // CancelHint();
    }
}
