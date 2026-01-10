using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 상태 관리 중앙 컨트롤러
/// 패널 전환, 탑바/네비바 관리를 담당
/// </summary>
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Game Reference")]
    [SerializeField] private GameManager gameManager;

    [Header("UI Roots")]
    [SerializeField] private GameObject lobbyUIRoot;        // 로비 UI 루트
    [SerializeField] private GameObject inGameUIRoot;       // 인게임 UI 루트

    [Header("Lobby UI Elements")]
    [SerializeField] private GameObject topBar;             // 상단 바
    [SerializeField] private GameObject bottomNavBar;       // 하단 네비게이션 바
    [SerializeField] private GameObject startButton;        // 시작 버튼
    [SerializeField] private GameObject scoreButton;        // 스코어 버튼

    [Header("InGame UI Elements")]
    [SerializeField] private GameObject inGameBackground;   // 인게임 배경
    [SerializeField] private GameObject panelBoard;         // 게임 보드 패널
    [SerializeField] private GameObject fastRestartButton;  // 빠른 재시작 버튼
    [SerializeField] private NewGameOverPanel gameOverPanel; // 게임오버 패널

    // UI 상태
    public enum UIState { Lobby, InGame, GameOver }
    public UIState CurrentState { get; private set; } = UIState.Lobby;

    // 이벤트
    public event Action<UIState> OnUIStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        // 게임오버 패널 이벤트 구독
        if (gameOverPanel != null)
        {
            gameOverPanel.OnReplayRequested += HandleReplayRequested;
            gameOverPanel.OnHomeRequested += HandleHomeRequested;
        }

        // 게임매니저 이벤트 구독
        if (gameManager != null)
        {
            gameManager.OnGameOver += HandleGameOver;
        }

        // 빠른 재시작 버튼 이벤트 구독
        if (fastRestartButton != null)
        {
            var btn = fastRestartButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(HandleFastRestartRequested); // FastRestartButton onClick
        }
    }

    private void OnDestroy()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.OnReplayRequested -= HandleReplayRequested;
            gameOverPanel.OnHomeRequested -= HandleHomeRequested;
        }

        if (gameManager != null)
        {
            gameManager.OnGameOver -= HandleGameOver;
        }

        if (fastRestartButton != null)
        {
            var btn = fastRestartButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.RemoveListener(HandleFastRestartRequested);
        }
    }

    /// <summary>
    /// 로비 상태로 전환
    /// </summary>
    public void TransitionToLobby()
    {
        CurrentState = UIState.Lobby;

        // 로비 UI 표시
        SetActive(lobbyUIRoot, true);
        SetActive(topBar, true);
        SetActive(bottomNavBar, true);
        SetActive(startButton, true);
        SetActive(scoreButton, true);

        // 인게임 UI 숨기기
        SetActive(inGameUIRoot, false);
        SetActive(fastRestartButton, false);
        if (gameOverPanel != null)
            gameOverPanel.Hide();

        OnUIStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// 인게임 상태로 전환
    /// </summary>
    public void TransitionToInGame()
    {
        CurrentState = UIState.InGame;

        // 로비 UI 숨기기 (루트 포함)
        SetActive(lobbyUIRoot, false);
        SetActive(topBar, false);
        SetActive(bottomNavBar, false);
        SetActive(startButton, false);
        SetActive(scoreButton, false);

        // 인게임 UI 표시
        SetActive(inGameUIRoot, true);
        SetActive(inGameBackground, true);
        SetActive(panelBoard, true);
        SetActive(fastRestartButton, true);
        if (gameOverPanel != null)
            gameOverPanel.Hide();

        OnUIStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// 게임오버 상태로 전환
    /// </summary>
    public void TransitionToGameOver(int finalScore)
    {
        CurrentState = UIState.GameOver;

        // 게임오버 패널이 InGameUIRoot 안에 있으므로 InGameUIRoot는 켜둠
        // 대신 인게임 요소들만 숨김
        SetActive(inGameBackground, false);
        SetActive(panelBoard, false);
        SetActive(fastRestartButton, false);

        // 게임오버 패널 표시
        if (gameOverPanel != null)
        {
            int bestScore = PlayerPrefs.GetInt("BestScore", 0);
            int globalRank = 0; // TODO: 서버에서 랭킹 가져오기
            gameOverPanel.Show(finalScore, bestScore, globalRank);
        }

        // 로비 UI는 숨김 유지
        SetActive(topBar, false);
        SetActive(bottomNavBar, false);

        OnUIStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// 게임 시작 요청 (StartUI에서 호출)
    /// </summary>
    public void RequestStartGame()
    {
        // UI 먼저 전환 (panelBoard 활성화)
        TransitionToInGame();

        // 그 다음 게임 시작 (보드 생성 + 스폰 애니메이션)
        if (gameManager != null)
            gameManager.StartGame();
    }

    /// <summary>
    /// 게임오버 이벤트 핸들러
    /// </summary>
    private void HandleGameOver(int finalScore)
    {
        TransitionToGameOver(finalScore);
    }

    /// <summary>
    /// 리플레이 요청 핸들러
    /// </summary>
    private void HandleReplayRequested()
    {
        if (gameManager != null)
            gameManager.RestartRun();

        TransitionToInGame();
    }

    /// <summary>
    /// 빠른 재시작 요청 핸들러
    /// </summary>
    private void HandleFastRestartRequested()
    {
        if (gameManager != null)
            gameManager.RestartRun();

        TransitionToInGame();
    }

    /// <summary>
    /// 홈 요청 핸들러
    /// </summary>
    private void HandleHomeRequested()
    {
        TransitionToLobby();
    }

    /// <summary>
    /// null-safe SetActive
    /// </summary>
    private void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
            obj.SetActive(active);
    }
}
