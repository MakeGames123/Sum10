using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임오버 패널
/// 자신의 표시/숨김과 이벤트 발생만 담당 (Single Responsibility)
/// UI 전환은 UIController가 처리
/// </summary>
public class NewGameOverPanel : MonoBehaviour
{
    [Header("Panel Elements")]
    [SerializeField] private TMP_Text bigScoreText;         // 큰 점수 텍스트
    [SerializeField] private TMP_Text bestScoreText;        // 베스트 스코어 텍스트
    [SerializeField] private TMP_Text globalRankText;       // 글로벌 랭킹 텍스트

    [Header("Buttons")]
    [SerializeField] private Button replayButton;           // 리플레이 버튼
    [SerializeField] private Button homeButton;             // 홈 버튼

    // 이벤트 (UIController가 구독)
    public event Action OnReplayRequested;
    public event Action OnHomeRequested;

    private void Awake()
    {
        // 버튼 이벤트 코드로 할당 (매뉴얼 규칙)
        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayClicked); // Button_Replay onClick

        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeClicked); // Button_Home onClick
    }

    private void OnDestroy()
    {
        if (replayButton != null)
            replayButton.onClick.RemoveListener(OnReplayClicked);

        if (homeButton != null)
            homeButton.onClick.RemoveListener(OnHomeClicked);
    }

    /// <summary>
    /// 게임오버 시 패널 표시
    /// </summary>
    public void Show(int finalScore, int bestScore, int globalRank)
    {
        gameObject.SetActive(true);

        if (bigScoreText != null)
            bigScoreText.text = finalScore.ToString();

        if (bestScoreText != null)
            bestScoreText.text = bestScore.ToString();

        if (globalRankText != null)
            globalRankText.text = globalRank.ToString();
    }

    /// <summary>
    /// 패널 숨기기
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 리플레이 버튼 클릭 - 이벤트만 발생
    /// </summary>
    private void OnReplayClicked()
    {
        OnReplayRequested?.Invoke();
    }

    /// <summary>
    /// 홈 버튼 클릭 - 이벤트만 발생
    /// </summary>
    private void OnHomeClicked()
    {
        OnHomeRequested?.Invoke();
    }
}
