using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public List<Cell> cells = new();
    public List<CellSlot> cellSlots = new();
    public List<CellView> cellUIs = new();
    // 힌트
    private List<Cell> currentHintCells;
    private Coroutine hintSoundCoroutine;
    [SerializeField] private float spawnDelayPerCell = 0.03f;   // 셀 간 딜레이
    private SelectionController selection;
    public GameManager gameManager;
    public TutorialManager tutorialManager;
    public ThemePanel themePanel;
    int n;
    private PathFinder pathFinder = new();

    // =========================
    //  Unity 라이프사이클
    // =========================

    private void Awake()
    {
        selection = new SelectionController(cellSlots);

        if (gameManager != null)
        {
            selection.OnKeyPadBlocked += gameManager.HandleNoMoreMoves;
            selection.OnCellRemoved += gameManager.HandleCellsRemoved;
        }

        // 사운드 연결
        selection.OnSelectionStarted += () => AudioManager.Instance.ResetPopPitch();
        selection.OnCellSelected += (count) => AudioManager.Instance.PlayPopSFX(count);
        selection.OnCellDeselected += (count) => AudioManager.Instance.PlayDeselectSFX(count);
        selection.OnCellsDestroyed += () => AudioManager.Instance.PlayCellDestroySFX();

        if (themePanel != null)
        {
            selection.OnKeyPadBlocked += themePanel.SetUp;
        }
    }
    public void SetupBoardWithSize(int size)
    {
        CancelHint();
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
            cell.ResetVisual();
            cell.Init(info);
            float delay = CalculateSpawnDelay(i % n, i / n);
            StartCoroutine(cell.PlaySpawnAnimation(delay));

            cell.gameObject.SetActive(true);
        }

        if (TutorialStatusManager.Instance.isTutorialCompleted) GenerateBoardValuesUntilValid();
        else GenerateBoardTutorialValues();

        for (int x = n * n; x < cellSlots.Count; x++)
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
    private void GenerateBoardTutorialValues()
    {
        List<int> tutorialValues = new() { 3, 1, 5, 4, 0, 5, 1, 5, 6 };

        for (int i = 0; i < n * n; i++)
        {
            cells[i].SetNum(tutorialValues[i]);
        }

        tutorialManager.TutorialProgress();
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

        if (b.ReturnNum() == 0) return;

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
        foreach (Cell cell in cells)
        {
            cell.UpdateCellLock(false);
        }

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
    public void LockCells()
    {
        foreach (Cell cell in cells)
        {
            cell.UpdateCellLock(true);
        }
    }
    public void UnlockCells()
    {
        foreach (Cell cell in cells)
        {
            cell.UpdateCellLock(false);
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
    public void ShowHint()
    {
        var path = FindHintPath();

        if (path == null || path.Count == 0)
            return;

        currentHintCells = path;

        foreach (var cell in currentHintCells)
        {
            cell.EnableHintMode();
            cell.onRemoved += CancelHint;
        }

        // 힌트 사운드 루프 시작
        var config = cellUIs[0].config;
        float interval = config.hintBounceDuration * config.hintBounceCount + config.hintPauseDuration;
        hintSoundCoroutine = StartCoroutine(HintSoundLoop(interval));
    }

    public void CancelHint()
    {
        // 힌트 사운드 루프 중지
        if (hintSoundCoroutine != null)
        {
            StopCoroutine(hintSoundCoroutine);
            hintSoundCoroutine = null;
        }

        if (currentHintCells != null)
        {
            foreach (var cell in currentHintCells)
            {
                cell.DisableHintMode();
                cell.onRemoved -= CancelHint;
            }
            currentHintCells.Clear();
        }

        gameManager?.ResetIdleTimer();
    }

    private IEnumerator HintSoundLoop(float interval)
    {
        while (true)
        {
            AudioManager.Instance.PlayHintSFX();
            yield return new WaitForSeconds(interval);
        }
    }
}