using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 일반 버튼 클릭 애니메이션
/// 누르면 작아졌다가, 놓으면 바운스로 복귀
/// </summary>
public class ButtonClickAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float pressScale = 0.9f;
    [SerializeField] private float pressDuration = 0.1f;
    [SerializeField] private float releaseDuration = 0.15f;
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.OutBack;

    private Vector3 originalScale;
    private Tween currentTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * pressScale, pressDuration)
            .SetEase(pressEase)
            .SetUpdate(true); // TimeScale 영향 안 받음
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, releaseDuration)
            .SetEase(releaseEase)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        currentTween?.Kill();
        transform.localScale = originalScale;
    }
}
