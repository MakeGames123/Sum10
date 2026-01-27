using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 점수 카운트업 애니메이션
/// 0에서 목표 점수까지 숫자가 올라가는 연출
/// </summary>
public class ScoreCountUpAnimation : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

    private Tween countTween;
    private int displayValue;

    /// <summary>
    /// 카운트업 애니메이션 재생
    /// </summary>
    /// <param name="targetScore">목표 점수</param>
    /// <param name="onComplete">완료 시 콜백</param>
    public void Play(int targetScore, System.Action onComplete = null)
    {
        if (targetText == null) return;

        countTween?.Kill();

        displayValue = 0;
        targetText.text = "0";

        countTween = DOTween.To(
            () => displayValue,
            x =>
            {
                displayValue = x;
                targetText.text = displayValue.ToString();
            },
            targetScore,
            duration
        )
        .SetEase(easeType)
        .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 애니메이션 즉시 완료 (목표값으로 설정)
    /// </summary>
    public void Complete(int targetScore)
    {
        countTween?.Kill();
        displayValue = targetScore;
        if (targetText != null)
            targetText.text = targetScore.ToString();
    }

    /// <summary>
    /// 애니메이션 중지
    /// </summary>
    public void Stop()
    {
        countTween?.Kill();
    }

    private void OnDestroy()
    {
        countTween?.Kill();
    }

#if UNITY_EDITOR
    [ContextMenu("Test - Count to 273")]
    private void TestCount()
    {
        Play(273, () => Debug.Log("카운트업 완료!"));
    }
#endif
}
