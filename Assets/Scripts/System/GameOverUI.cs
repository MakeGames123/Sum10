using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 기존 게임오버 UI (Deprecated)
/// UIController + NewGameOverPanel 구조로 대체됨
/// 하위 호환성을 위해 유지하되, UIController가 없을 때만 동작
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [SerializeField] private GameObject gameOverRoot;       // 기존 GameOver 패널
    [SerializeField] private GameObject inGameUIRoot;       // 보드 + HUD 루트
    [SerializeField] private GameObject scoreButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject fastRestartButton;

    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainButton;

    private void Awake()
    {
        // UIController가 있으면 이 스크립트는 비활성화
        if (UIController.Instance != null)
        {
            enabled = false;
            return;
        }

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("GameOverUI: GameManager를 찾을 수 없습니다.");
            enabled = false;
            return;
        }

        gameManager.OnGameOver += HandleGameOver;

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked); // RetryButton onClick
        if (mainButton != null)
            mainButton.onClick.AddListener(OnMain); // MainButton onClick
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver(int finalScore)
    {
        if (inGameUIRoot != null)
            inGameUIRoot.SetActive(false);
        if (fastRestartButton != null)
            fastRestartButton.SetActive(false);

        if (gameOverRoot != null)
            gameOverRoot.SetActive(true);
        if (finalScoreText != null)
            finalScoreText.text = $"Score : {finalScore}";
    }

    public void OnRetryClicked()
    {
        if (gameManager != null)
            gameManager.RestartRun();

        if (inGameUIRoot != null)
            inGameUIRoot.SetActive(true);

        if (gameOverRoot != null)
            gameOverRoot.SetActive(false);
        if (fastRestartButton != null)
            fastRestartButton.SetActive(true);
    }

    private void OnMain()
    {
        if (scoreButton != null)
            scoreButton.SetActive(true);
        if (startButton != null)
            startButton.SetActive(true);
        if (gameOverRoot != null)
            gameOverRoot.SetActive(false);
        if (fastRestartButton != null)
            fastRestartButton.SetActive(false);
    }
}
