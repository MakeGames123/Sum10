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

        // 2. 이전 데이터 가져오기
        int prevRank = scoreManager.PreviousWeeklyRank;
        if (prevRank < 1) prevRank = 9999;
        int weeklyBest = scoreManager.PreviousWeeklyBestScore;
        previousBestScore = scoreManager.PreviousHighScore;

        // 즉시 역대 최고기록/순위 표시 (애니메이션 완료 전 stale 값 방지)
        if (bestScoreText != null)
            bestScoreText.text = Mathf.Max(previousBestScore, finalScore).ToString();
        if (globalRankText != null)
            globalRankText.text = prevRank < 9999 ? prevRank.ToString() : "-";

        // 3. 하이스코어 체크
        if (animationController != null)
            animationController.SetHighScoreStatus(finalScore > weeklyBest);

        int currentRank = prevRank;

        // 4. 주간 최고 갱신 시
        if (finalScore > weeklyBest)
        {
            currentRank = await scoreManager.CalculateRankForScoreAsync(finalScore);
            await scoreManager.SubmitWeeklyScoreAsync(finalScore);
            scoreManager.CacheCalculatedRank(currentRank, finalScore);
            Invoke(nameof(delayWeek), 1f);
        }

        // 5. 역대 최고 갱신 시 저장 (로컬 + 서버 백업)
        if (finalScore > previousBestScore)
        {
            await scoreManager.SubmitHighScore(finalScore);
            Invoke(nameof(delay), 1f);
        }

        // 7. 순위 애니메이션 시작
        ShowRankAnimation(prevRank, currentRank);
    }
    private void delayWeek()
    {
        // 6. Top 50 캐시 갱신 (백그라운드)
        _ = scoreManager.FetchWeeklyTop50WithProfilesAsync();
    }
    private void delay()
    {
        // 6. Top 50 캐시 갱신 (백그라운드)
        _ = scoreManager.FetchTop50WithProfilesAsync();
    }

    private async void ShowRankAnimation(int prevRank, int currentRank)
    {
        int actualSteps = Mathf.Abs(prevRank - currentRank);

        // 시각적 스크롤 제한에 맞춰 리더보드 범위 계산
        int maxVisualSteps = animationController != null ? animationController.MaxVisualScrollSteps : 15;
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
