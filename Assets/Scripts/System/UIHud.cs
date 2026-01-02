using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Slider timer;

    [Header("Score Animation")]
    [SerializeField] private float scorePunchScale = 0.3f;      // 커지는 정도 (더 통통)
    [SerializeField] private float scorePunchDuration = 0.35f;  // 애니메이션 시간
    [SerializeField] private Transform scoreContainer;          // Score 영역 전체 (함께 움직임)

    [Header("Time Animation")]
    [SerializeField] private float timePunchScale = 0.2f;       // 시간 증가시 펀치 스케일 (더 통통)
    [SerializeField] private float timePunchDuration = 0.25f;   // 시간 애니메이션 시간

    // 점수 UI 위치 (CellView에서 빨려들어가는 효과용)
    public static Transform ScoreTarget { get; private set; }

    private Tween scoreTween;
    private Tween scoreContainerTween;
    private Tween comboTween;
    private Tween timeTween;
    private int lastScore = -1;   // -1로 초기화하여 첫 점수 증가도 감지
    private float lastTime = -1f; // -1로 초기화하여 첫 시간 증가도 감지

    private void Awake()
    {
        // 점수 UI 위치 저장
        if (scoreText != null)
            ScoreTarget = scoreText.transform;

        // GameManager를 직접 안 넣어줬으면 씬에서 찾아온다.
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        // 안전 체크
        if (gameManager == null)
        {
            Debug.LogError("UIHud: GameManager를 찾을 수 없습니다.");
            return;
        }

        // 이벤트 구독
        gameManager.OnScoreChanged += HandleScoreChanged;
        gameManager.OnTimeChanged += HandleTimeChanged;
        gameManager.OnComboChanged += HandleComboChanged;

        // ★ 초기값 한 번 강제 반영 (애니메이션 없이 - lastScore/lastTime은 -1로 유지)
        HandleScoreChanged(gameManager.CurrentScore);
        HandleTimeChanged(gameManager.RemainingTime);
        HandleComboChanged((int)gameManager.combo);

        // 이미 GameManager가 내부에서 초기값을 세팅했다면,
        // 바로 한 번 갱신해주고 싶으면 GameManager에 프로퍼티를 만들어서 읽으면 된다.
        // (아래 3.에서 설명)
    }
    // 점수 변경 이벤트 콜백
    private void HandleScoreChanged(int newScore)
    {
        if (scoreText == null) return;

        scoreText.text = $"{newScore}";

        // 점수가 올랐을 때만 애니메이션 (초기화 시 lastScore=-1이므로 첫 증가도 감지됨)
        if (newScore > lastScore && lastScore >= 0)
        {
            scoreTween?.Kill();
            scoreContainerTween?.Kill();
            scoreText.transform.localScale = Vector3.one;

            scoreTween = scoreText.transform
                .DOPunchScale(Vector3.one * scorePunchScale, scorePunchDuration, 2, 0.3f)
                .SetEase(Ease.OutElastic);

            if (scoreContainer != null)
            {
                scoreContainer.localScale = Vector3.one;
                scoreContainerTween = scoreContainer
                    .DOPunchScale(Vector3.one * 0.12f, scorePunchDuration * 0.7f, 1, 0.4f)
                    .SetEase(Ease.OutBack);
            }
        }

        lastScore = newScore;
    }

    // 콤보 변경 이벤트 콜백
    private void HandleComboChanged(int newCombo)
    {
        if (comboText == null) return;

        // 0콤보일 때는 숨김
        if (newCombo <= 0)
        {
            comboText.gameObject.SetActive(false);
            return;
        }

        // 1콤보 이상일 때 표시
        comboText.gameObject.SetActive(true);
        comboText.text = $"{newCombo} Combo";

        // 콤보 증가 시 통통 튀는 애니메이션 (Score와 동일)
        comboTween?.Kill();
        comboText.transform.localScale = Vector3.one;
        comboTween = comboText.transform
            .DOPunchScale(Vector3.one * scorePunchScale, scorePunchDuration, 2, 0.3f)
            .SetEase(Ease.OutElastic);
    }

    private void HandleTimeChanged(float remainingTime)
    {
        if (timeText != null)
        {
            timeText.text = remainingTime.ToString("0.#");

            float rawValue = remainingTime / 30f;

            float visualValue = (rawValue * (1f - 0.14f)) + 0.14f;

            timer.value = visualValue;

            // 시간이 증가했을 때 (셀 클리어로 보너스 시간 획득) 애니메이션
            if (remainingTime > lastTime && lastTime >= 0f)
            {
                timeTween?.Kill();
                timeText.transform.localScale = Vector3.one;

                // 시간 증가시 통통 튀는 효과
                timeTween = timeText.transform
                    .DOPunchScale(Vector3.one * timePunchScale, timePunchDuration, 1, 0.5f)
                    .SetEase(Ease.OutBack);
            }

            lastTime = remainingTime;
        }
    }
}