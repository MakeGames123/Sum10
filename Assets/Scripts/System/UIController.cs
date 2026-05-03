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
    [SerializeField] private GameObject homePanel;          // 홈 패널 (버튼 묶음)
    [SerializeField] private GameObject topBar;             // 상단 바
    [SerializeField] private GameObject bottomNavBar;       // 하단 네비게이션 바
    [SerializeField] private GameObject startButton;        // 시작 버튼
    [SerializeField] private GameObject scoreButton;        // 스코어 버튼
    [SerializeField] private GameObject questButton;        // 스코어 버튼

    [Header("InGame UI Elements")]
    [SerializeField] private GameObject inGameBackground;   // 인게임 배경
    [SerializeField] private GameObject panelBoard;         // 게임 보드 패널
    [SerializeField] private GameObject fastRestartButton;  // 빠른 재시작 버튼
    [SerializeField] private GameOverPanel gameOverPanel;  // 빠른 재시작 버튼
    [SerializeField] private SettingManager setting;  // 빠른 재시작 버튼

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
        gameOverPanel.replayButton.onClick.AddListener(HandleReplayRequested);
    }

    private void Update()
    {
        // 인게임 중 안드로이드 뒤로가기 → 설정 토글 (어르신 UX)
        if (Input.GetKeyDown(KeyCode.Escape) && CurrentState == UIState.InGame)
        {
            SettingManager.Instance?.Toggle();
        }
    }
    /// <summary>
    /// 로비 상태로 전환
    /// </summary>
    public void TransitionToLobby()
    {
        // 게임오버 → 홈 진입인지 캡처 (CurrentState 변경 전)
        bool fromGameOver = CurrentState == UIState.GameOver;

        CurrentState = UIState.Lobby;
        gameOverPanel.Hide();
        TopBanner.Instance.RemoveBanner();
        // 로비 UI 표시
        lobbyUIRoot.anchoredPosition = Vector2.zero;
        SetActive(homePanel, true);
        SetActive(topBar, true);
        SetActive(bottomNavBar, true);
        SetActive(startButton, true);
        SetActive(questButton, true);
        SetActive(scoreButton, true);

        // 인게임 UI 숨기기
        inGameUIRoot.anchoredPosition = disablePos;
        SetActive(fastRestartButton, false);

        // 게임오버 후 홈 진입 시, 수령 가능한 퀘스트가 있으면 퀘스트 창 자동 오픈
        if (fromGameOver && QuestManager.Instance != null && QuestManager.Instance.HasUnclaimedReward())
        {
            QuestManager.Instance.OpenPanel();
        }

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
        SetActive(questButton, false);

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
        TopBanner.Instance.RemoveBanner();

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
        // UI 먼저 전환 (panelBoard 활성화)
        TransitionToInGame();
        TopBanner.Instance.PlaceBannerAd();

        // 그 다음 게임 시작 (보드 생성 + 스폰 애니메이션)
        if (gameManager != null)
            gameManager.StartGame();
    }

    /// <summary>
    /// 게임오버 패널 재시작 버튼 핸들러 (Panel_Board 활성화 후 보드 초기화 보장)
    /// </summary>
    private void HandleReplayRequested()
    {
        TransitionToInGame();
        if (gameManager != null)
            gameManager.RestartRun();
    }

    /// <summary>
    /// 빠른 재시작 요청 핸들러
    /// </summary>
    private void HandleFastRestartRequested()
    {
        TransitionToInGame();
        if (gameManager != null)
            gameManager.RestartRun();
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
