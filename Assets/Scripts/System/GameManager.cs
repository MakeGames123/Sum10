using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardSettingManager boardSettingManager;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 30f;  // 30초

    [Header("Hint Settings")]
    [SerializeField] private float hintIdleThreshold = 5f;
    [SerializeField] private float hintFlashDuration = 3.0f;
    public Vector2 scoreTextPosition = new();
    public Vector2 timeTextPosition = new();

    private float remainingTime;
    public float RemainingTime
    {
        get { return remainingTime; }
        set
        {
            remainingTime = Mathf.Min(30, value);  // 최대 30초 제한
        }
    }
    private float timeProgress = 0;
    private bool isRunning = false;

    private int currentBoardSize = 3;   // 3x3 시작
    private int stageIndex = 0;         // 스테이지 인덱스 (테스트용)
    private int score = 0;
    private int bestScore = 0;

    private float idleTimer = 0f;
    private bool hintShownForCurrentIdle = false;

    public Action<int> OnScoreChanged;
    public Action<int> OnComboChanged;
    public Action<float> OnTimeChanged;
    public Action<int> OnGameOver;

    public int CurrentScore => score;
    public int BestScore => bestScore;
    public GameObject floatingText;
    public GameObject comboText;
    public GameObject textGroup;
    public ScoreManager scoreManager;
    public PersonalScoreManager personalScoreManager;

    public int combo = 0;
    public int maxCombo = 0;
    private void Awake()
    {
        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();

        if (boardManager == null)
        {
            Debug.LogError("GameManager: BoardManager를 찾을 수 없습니다.");
            return;
        }

        boardManager.OnNoMoreMoves += HandleNoMoreMoves;
        boardManager.OnCellsRemoved += HandleCellsRemoved;

        bestScore = PlayerPrefs.GetInt("BestScore", 0);
    }

    private void OnDestroy()
    {
        if (boardManager != null)
        {
            boardManager.OnNoMoreMoves -= HandleNoMoreMoves;
            boardManager.OnCellsRemoved -= HandleCellsRemoved;
        }
    }

    private void Start()
    {
        // 모바일 프레임레이트 최적화 (120Hz 디바이스 지원)
        Application.targetFrameRate = 120;

        // 자동으로 게임을 시작하지 않는다.
        isRunning = false;
        score = 0;
        timeProgress = 0;
        RemainingTime = gameDuration;

        OnScoreChanged?.Invoke(score);
        OnTimeChanged?.Invoke(RemainingTime);
    }

    private void Update()
    {
        // ========== AB 테스트: T키로 선택 애니메이션 전환 ==========
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (CellView.CurrentSelectMode == CellView.SelectMode.Bounce)
            {
                CellView.CurrentSelectMode = CellView.SelectMode.Flip;
                Debug.Log("[AB Test] 선택 모드: Flip (뒤집기 + 색상 변경)");
            }
            else
            {
                CellView.CurrentSelectMode = CellView.SelectMode.Bounce;
                Debug.Log("[AB Test] 선택 모드: Bounce (점프 + 기울어짐)");
            }
        }
#endif

        if (!isRunning)
            return;

        // ----- 게임 타이머 -----
        RemainingTime -= Time.deltaTime;
        timeProgress += Time.deltaTime;
        if (RemainingTime < 0f)
            RemainingTime = 0f;

        OnTimeChanged?.Invoke(RemainingTime);

        if (RemainingTime <= 0f)
        {
            EndRun();
            return;
        }

        // ----- 힌트용 idle 타이머 -----
        idleTimer += Time.deltaTime;

        if (!hintShownForCurrentIdle && idleTimer >= hintIdleThreshold)
        {
            var hintPath = boardManager.FindHintPath();
            if (hintPath != null && hintPath.Count > 0)
            {
                boardManager.ShowHint(hintFlashDuration);
                combo = 0;
                OnComboChanged?.Invoke((int)combo);
                hintShownForCurrentIdle = true;
            }
        }
    }

    // ========= 외부에서 호출 =========

    // Start 버튼에서 호출할 함수
    public void StartGame()
    {
        if (isRunning)
            return;

        StartNewRun();
    }

    // Retry 버튼에서 호출
    public void RestartRun()
    {
        StartNewRun();
    }

    // ========= 내부 로직 =========

    private void StartNewRun()
    {
        // 이전 게임의 파티클/고스트 정리
        CellView.KillAllGhostAnimations();
        CellView.DestroyAllActiveParticles();

        score = 0;
        combo = 0;
        maxCombo = 0;
        timeProgress = 0;
        OnScoreChanged?.Invoke(score);
        OnComboChanged?.Invoke(score);

        RemainingTime = gameDuration;
        OnTimeChanged?.Invoke(RemainingTime);

        isRunning = true;

        stageIndex = 0;
        currentBoardSize = GetBoardSizeForStage(stageIndex);
        boardSettingManager.SetupBoardWithSize(currentBoardSize);

        ResetIdleTimer();
    }

    private void EndRun()
    {
        isRunning = false;

        // 남아있는 Ghost 애니메이션 및 파티클 정리
        CellView.KillAllGhostAnimations();
        CellView.DestroyAllActiveParticles();

        // 최고점수 갱신
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        scoreManager.SubmitScore(score, maxCombo);
        personalScoreManager.SubmitScore(score, maxCombo);
        OnGameOver?.Invoke(score);
    }

    private void ResetIdleTimer()
    {
        idleTimer = 0f;
        hintShownForCurrentIdle = false;
    }

    private void HandleCellsRemoved(List<CellView> removedCells)
    {
        if (!isRunning || removedCells == null)
            return;

        int gained = removedCells.Count;
        if (gained <= 0)
            return;

        combo++;
        if (maxCombo < combo) maxCombo = combo;
        int comboBonus = (int)(combo / 10);  // 0~9: 0점, 10~19: 1점, 20~29: 2점...
        int scoreGain = gained + comboBonus;
        score += scoreGain;

        GameObject text = Instantiate(floatingText, textGroup.transform);
        text.GetComponent<RectTransform>().anchoredPosition = scoreTextPosition;
        text.GetComponent<TextFloating>().SetCondition("+" + scoreGain.ToString());

        // TODO: 10콤보마다 화면 중앙 텍스트 (임시 비활성화)
        // if (combo % 10 == 0)
        // {
        //     GameObject comboTextCpy = Instantiate(comboText, textGroup.transform);
        //     comboTextCpy.GetComponent<ComboTextFade>().SetCondition("Combo " + combo.ToString() + "!");
        // }

        // 시간 추가 규칙 (경과 시간에 따라 감소)
        float timeBonus;
        if (timeProgress < 30) timeBonus = 2f;
        else if (timeProgress < 60) timeBonus = 1.5f;
        else if (timeProgress < 120) timeBonus = 1f;
        else timeBonus = 0.5f;

        RemainingTime += timeBonus;
        GameObject timetext = Instantiate(floatingText, textGroup.transform);
        timetext.GetComponent<RectTransform>().anchoredPosition = timeTextPosition;
        timetext.GetComponent<TextFloating>().SetCondition("+" + timeBonus.ToString("0.#") + "s", true);
        OnComboChanged?.Invoke((int)combo);


        OnScoreChanged?.Invoke(score);

        ResetIdleTimer();
    }

    private void HandleNoMoreMoves()
    {
        if (!isRunning)
            return;

        stageIndex++;
        currentBoardSize = GetBoardSizeForStage(stageIndex);
        boardSettingManager.SetupBoardWithSize(currentBoardSize);
        ResetIdleTimer();
    }

    // 스테이지별 보드 크기: 3, 3, 4, 4, 5, 5, 6, 6, 6...
    private int GetBoardSizeForStage(int stage)
    {
        // stage 0-1: 3
        // stage 2-3: 4
        // stage 4-5: 5
        // stage 6+: 6
        if (stage <= 1) return 3;
        if (stage <= 3) return 4;
        if (stage <= 5) return 5;
        return 6;
    }
}
