using UnityEngine;
using System.Collections.Generic;
using PlayFab.ClientModels;
using PlayFab;

/// <summary>
/// 게임오버 애니메이션 오케스트레이터
/// 점수 카운트업 → 랭킹 제치기 → 폭죽 순서 제어
/// </summary>
public class GameOverAnimationController : MonoBehaviour
{
    [Header("Animation Components")]
    [SerializeField] private ScoreCountUpAnimation scoreCountUpAnimation;
    [SerializeField] private RankingOvertakeAnimation rankingOvertakeAnimation;
    [SerializeField] private HighScoreFireworksEffect fireworksEffect;

    private bool isNewHighScore;

    /// <summary>
    /// 제치기 애니메이션 시퀀스 시작
    /// </summary>
    public void PlayOvertakeSequence(int finalScore, int prevRank, int currentRank,
                                      List<PlayerLeaderboardEntry> leaderboardData,
                                      System.Action onComplete = null)
    {
        string myPlayerId = PlayFabSettings.staticPlayer.PlayFabId;

        if (rankingOvertakeAnimation != null)
        {
            rankingOvertakeAnimation.Play(prevRank, currentRank, leaderboardData,
                                          finalScore, myPlayerId, () =>
            {
                OnAnimationComplete(onComplete);
            });
        }
        else
        {
            OnAnimationComplete(onComplete);
        }
    }

    private void OnAnimationComplete(System.Action onComplete)
    {
        if (isNewHighScore && fireworksEffect != null)
            fireworksEffect.Play();

        onComplete?.Invoke();
    }

    /// <summary>
    /// 점수 카운트업 시작
    /// </summary>
    public void PlayScoreCountUp(int finalScore)
    {
        if (scoreCountUpAnimation != null)
            scoreCountUpAnimation.Play(finalScore, null);
    }

    /// <summary>
    /// 하이스코어 갱신 여부 설정
    /// </summary>
    public void SetHighScoreStatus(bool isNew)
    {
        isNewHighScore = isNew;
    }

    /// <summary>
    /// 시각적 스크롤 최대 칸수 (리더보드 범위 계산용)
    /// </summary>
    public int MaxVisualScrollSteps =>
        rankingOvertakeAnimation != null ? rankingOvertakeAnimation.MaxVisualScrollSteps : 15;
}
