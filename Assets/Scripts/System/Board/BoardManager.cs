using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardManager : MonoBehaviour, IBoard
{
    private Vector2 lastProcessedTouchPosition;
    private const float TOUCH_POSITION_THRESHOLD = 50f;

    public List<Cell> cells = new();
    public List<CellSlot> cellSlots = new();
    public List<CellView> cellUIs = new();
    // 힌트
    private List<Cell> currentHintCells;
    private Coroutine hintCoroutine;

    [SerializeField] private float spawnDelayPerCell = 0.03f;   // 셀 간 딜레이

    // 이벤트
    public event Action<List<Cell>> OnCellsRemoved;
    private SelectionController selection;
    public GameManager gameManager;
    int n;
    private PathFinder pathFinder = new();

    // =========================
    //  Unity 라이프사이클
    // =========================

    private void Awake()
    {
        selection = new SelectionController(cellSlots);
        selection.OnKeyPadBlocked += gameManager.HandleNoMoreMoves;
        selection.OnCellRemoved += gameManager.HandleCellsRemoved;
    }
    public void SetupBoardWithSize(int size)
    {
        n = size;
        pathFinder.SetSize(n);

        cells.Clear();
        for (int i = 0; i < n * n; i++)
        {
            Cell info = new Cell();
            cells.Add(info);

            info.SetPosition(i % n, i / n);
            cellSlots[i].SetCondition(info);

            CellView cell = cellUIs[i];
            cell.Init(this, info);
            float delay = CalculateSpawnDelay(i % 5, i / 5);
            StartCoroutine(cell.PlaySpawnAnimation(delay));

            cell.gameObject.SetActive(true);
        }

        GenerateBoardValuesUntilValid();

        for (int x = n * n; x < 36; x++)
        {
            var cell = cellUIs[x];
            cell.gameObject.SetActive(false);
        }
        selection.UpdateCells(cells);
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CancelHint();
            selection.Begin();
        }

        if (Input.GetMouseButton(0))
            selection.Update(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            selection.End();
    }
    private float CalculateSpawnDelay(int x, int y)
    {
        return (y * n + x) * spawnDelayPerCell;
    }
    private void GenerateBoardValuesUntilValid()
    {
        FillBoardRandom();

        InsertTwoCellPath();
        InsertTwoCellPath();
    }
    private void InsertTwoCellPath()
    {
        int centerIndex = 99;
        if (n % 2 == 1)
        {
            centerIndex = n * (n / 2) + n / 2;
        }

        List<(Cell a, Cell b)> candidates = new();

        for (int i = 0; i < n * n; i++)
        {
            if (i == centerIndex) continue;
            Vector2Int p = cells[i].Position;

            TryAddPair(cells[i], p + Vector2Int.right, candidates);
            TryAddPair(cells[i], p + Vector2Int.up, candidates);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("삽입 가능한 셀 쌍 없음");
            return;
        }

        var pair = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        AssignPairSumToTen(pair.a, pair.b);
    }

    private void TryAddPair(Cell a, Vector2Int pos, List<(Cell, Cell)> list)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= n || pos.y >= n)
            return;

        Cell b = cells[pos.x + pos.y * n];
        list.Add((a, b));
    }

    private void AssignPairSumToTen(Cell a, Cell b)
    {
        int v = UnityEngine.Random.Range(1, 9); // 1~8
        a.SetNum(v);
        b.SetNum(10 - v);
    }
    private void FillBoardRandom()
    {
        int centerIndex = 99;
        if (n % 2 == 1)
        {
            centerIndex = n * (n / 2) + n / 2;
        }

        for (int i = 0; i < n * n; i++)
        {
            if (i != centerIndex) cells[i].SetNum(UnityEngine.Random.Range(1, 10));
            else cells[i].SetNum(0);
        }
    }
    // =========================
    //  힌트
    // =========================

    public List<Cell> FindHintPath()
    {
        List<Cell> result = pathFinder.FindPathBFS(cells, 10);

        if (result == null || result.Count == 0)
            return null;

        return result;
    }

    public void ShowHint(float flashDuration = 1.0f)
    {
        var path = FindHintPath();

        if (path == null || path.Count == 0)
            return;

        currentHintCells = path;

        foreach (var cell in currentHintCells)
        {
            cell.EnableHintMode();
        }
        //hintCoroutine = StartCoroutine(HintRoutine(flashDuration));
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
            {
                cell.DisableHintMode();
            }
        }
    }

    /// <summary>
    /// 주어진 셀이 현재 활성 힌트 셀 중 첫 번째인지 확인 (사운드 리더 동적 결정)
    /// </summary>
    public bool IsFirstActiveHintCell(CellView cell)
    {
        return false;
        /*
        if (currentHintCells == null || currentHintCells.Count == 0)
            return false;

        // 힌트 상태이면서 선택되지 않은 첫 번째 셀 찾기
        foreach (var hintCell in currentHintCells)
        {
            if (hintCell.IsHint && !hintCell.IsSelected)
                return hintCell == cell;
        }
        return false;*/
    }

    /// <summary>
    /// 모든 힌트 셀의 애니메이션을 동기화 (선택 해제 후 싱크 맞추기)
    /// </summary>
    private void ResyncHintAnimations()
    {/*
        if (currentHintCells == null || currentHintCells.Count == 0)
            return;

        foreach (var cell in currentHintCells)
        {
            cell.ForceRestartHintAnimation();
        }*/
    }

    private IEnumerator HintRoutine(float duration)
    {
        yield break;
        /*
        // 첫 번째 힌트 셀을 사운드 리더로 지정
        for (int i = 0; i < currentHintCells.Count; i++)
        {
            currentHintCells[i].SetHintSoundLeader(i == 0);
            currentHintCells[i].SetHintHighlight(true);
        }

        // DOTween 콜백으로 사운드 재생하므로 코루틴 대기 불필요
        yield break;
        */
    }
}