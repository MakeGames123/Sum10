using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab.ClientModels;
using System.Collections.Generic;

/// <summary>
/// 게임오버 패널
/// 주간 랭킹 제치기 애니메이션 + UI 표시
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    [Header("Panel Elements")]
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text globalRankText;

    [Header("Buttons")]
    public Button replayButton;
    public Button homeButton;

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameOverAnimationController animationController;
    public ScoreManager scoreManager;

    private RectTransform myRect;
    private Vector2 disablePos = new Vector2(-9999, 9999);
    private int finalScore;
    private int previousBestScore;

    private void Awake()
    {
        myRect = GetComponent<RectTransform>();

        if (replayButton != null) replayButton.onClick.AddListener(Hide);
        if (homeButton != null) homeButton.onClick.AddListener(Hide);

        gameManager.OnGameOver.AddListener(SetCondition);
    }

    private async void SetCondition(int finalScore)
    {
        this.finalScore = finalScore;
        myRect.anchoredPosition = Vector2.zero;
        gameObject.SetActive(true);

        // 1. 점수 카운트업 먼저 시작 (API 기다리지 않음)
        if (animationController != null)
            animationController.PlayScoreCountUp(finalScore);

        // 2. 이전 순위 가져오기
        int prevRank = scoreManager.PreviousWeeklyRank;
        if (prevRank < 1) prevRank = 9999;  // 첫 플레이

        // 3. 하이스코어 체크
        previousBestScore = scoreManager.PreviousHighScore;
        if (animationController != null)
            animationController.SetHighScoreStatus(finalScore > previousBestScore);

        // 4. 점수 제출 후 순위 조회
        bool success = await scoreManager.SubmitScoreAsync(finalScore);
        if (success)
            FetchWeeklyRankAndAnimate(prevRank);
    }

    private async void FetchWeeklyRankAndAnimate(int prevRank)
    {
        int currentRank = await scoreManager.GetMyWeeklyRankAsync();
        if (currentRank < 1) currentRank = prevRank;

        // 시각적 스크롤 제한에 맞춰 리더보드 범위 계산
        int maxVisualSteps = animationController != null ? animationController.MaxVisualScrollSteps : 15;
        int actualSteps = Mathf.Abs(prevRank - currentRank);
        int visualSteps = Mathf.Min(actualSteps, maxVisualSteps);

        bool isRising = prevRank > currentRank;
        int visualPrevRank = isRising ? currentRank + visualSteps : currentRank - visualSteps;
        visualPrevRank = Mathf.Max(1, visualPrevRank);

        // 시각적 시작 순위 기준으로 리더보드 데이터 조회
        int fromRank = Mathf.Max(1, Mathf.Min(visualPrevRank, currentRank) - 5);
        int toRank = Mathf.Max(visualPrevRank, currentRank) + 5;

        var leaderboardData = await scoreManager.GetWeeklyLeaderboardRangeAsync(fromRank, toRank);

        if (animationController != null)
        {
            animationController.PlayOvertakeSequence(finalScore, prevRank, currentRank,
                                                      leaderboardData, () => SetFinalUI(currentRank));
        }
        else
        {
            SetFinalUI(currentRank);
        }
    }

    private void SetFinalUI(int currentRank)
    {
        if (bestScoreText != null)
            bestScoreText.text = Mathf.Max(previousBestScore, finalScore).ToString();

        if (globalRankText != null)
            globalRankText.text = currentRank.ToString();
    }

    public void Hide()
    {
        myRect.anchoredPosition = disablePos;
    }
}
