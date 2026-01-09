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
    [SerializeField] private RectTransform lobbyUIRoot;        // 로비 UI 루트
    [SerializeField] private RectTransform inGameUIRoot;       // 인게임 UI 루트

    [Header("Lobby UI Elements")]
    [SerializeField] private GameObject topBar;             // 상단 바
    [SerializeField] private GameObject bottomNavBar;       // 하단 네비게이션 바
    [SerializeField] private GameObject startButton;        // 시작 버튼
    [SerializeField] private GameObject scoreButton;        // 스코어 버튼

    [Header("InGame UI Elements")]
    [SerializeField] private GameObject inGameBackground;   // 인게임 배경
    [SerializeField] private GameObject panelBoard;         // 게임 보드 패널
    [SerializeField] private GameObject fastRestartButton;  // 빠른 재시작 버튼
    [SerializeField] private GameOverPanel gameOverPanel;  // 빠른 재시작 버튼

    // UI 상태
    public enum UIState { Lobby, InGame, GameOver }
    public UIState CurrentState { get; private set; } = UIState.Lobby;

    // 이벤트
    public event Action<UIState> OnUIStateChanged;

    Vector2 disablePos = new Vector2(-9999, 9999);
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

        // 빠른 재시작 버튼 이벤트 구독
        if (fastRestartButton != null)
        {
            var btn = fastRestartButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(HandleFastRestartRequested); // FastRestartButton onClick
        }

        gameManager.OnGameOver.AddListener(TransitionToGameOver);
        gameOverPanel.homeButton.onClick.AddListener(TransitionToLobby);
        gameOverPanel.replayButton.onClick.AddListener(TransitionToInGame);
    }
    /// <summary>
    /// 로비 상태로 전환
    /// </summary>
    public void TransitionToLobby()
    {
        CurrentState = UIState.Lobby;

        // 로비 UI 표시
        lobbyUIRoot.anchoredPosition = Vector2.zero;
        SetActive(topBar, true);
        SetActive(bottomNavBar, true);
        SetActive(startButton, true);
        SetActive(scoreButton, true);

        // 인게임 UI 숨기기
        inGameUIRoot.anchoredPosition = disablePos;
        SetActive(fastRestartButton, false);

        OnUIStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// 인게임 상태로 전환
    /// </summary>
    public void TransitionToInGame()
    {
        CurrentState = UIState.InGame;

        // 로비 UI 숨기기 (루트 포함)
        lobbyUIRoot.anchoredPosition = disablePos;
        SetActive(topBar, false);
        SetActive(bottomNavBar, false);
        SetActive(startButton, false);
        SetActive(scoreButton, false);

        // 인게임 UI 표시
        inGameUIRoot.anchoredPosition = Vector2.zero;
        SetActive(inGameBackground, true);
        SetActive(panelBoard, true);
        SetActive(fastRestartButton, true);

        OnUIStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// 게임오버 상태로 전환
    /// </summary>
    public void TransitionToGameOver(int val)
    {
        CurrentState = UIState.GameOver;

        // 게임오버 패널이 InGameUIRoot 안에 있으므로 InGameUIRoot는 켜둠
        // 대신 인게임 요소들만 숨김
        SetActive(inGameBackground, false);
        SetActive(panelBoard, false);
        SetActive(fastRestartButton, false);


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
        if (gameManager != null)
            gameManager.StartGame();

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
    /// null-safe SetActive
    /// </summary>
    private void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
            obj.SetActive(active);
    }
}
