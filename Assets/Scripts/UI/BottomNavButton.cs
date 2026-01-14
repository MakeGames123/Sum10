using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 하단 내비게이션 버튼
/// 선택 시 커지면서 위로 솟아오르고 텍스트 표시
/// </summary>
public class BottomNavButton : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    [Header("Select Animation")]
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float selectedYOffset = 80f;
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private Ease selectEase = Ease.OutBack;
    [SerializeField] private Ease deselectEase = Ease.OutQuad;

    [Header("Label Animation")]
    [SerializeField] private float labelFadeDuration = 0.1f;
    [SerializeField] private float labelYOffset = 10f;

    public UnityEvent onClick = new();

    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Tween iconTween;
    private Tween labelTween;
    private bool isSelected;

    private void Awake()
    {
        if(icon != null) originalScale = icon.transform.localScale;
        if(icon != null) originalPosition = icon.rectTransform.anchoredPosition;

        // 라벨 초기 상태: 숨김
        if (label != null)
        {
            label.alpha = 0f;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick.Invoke();
    }

    public void UpdateUI(bool flag)
    {
        if (isSelected == flag) return;
        isSelected = flag;

        iconTween?.Kill();
        labelTween?.Kill();

        if (flag)
        {
            PlaySelectAnimation();
        }
        else
        {
            PlayDeselectAnimation();
        }
    }

    private void PlaySelectAnimation()
    {
        background.enabled = true;

        // 아이콘: 커지면서 위로
        Sequence seq = DOTween.Sequence();
        seq.Append(icon.transform.DOScale(originalScale * selectedScale, animDuration)
            .SetEase(selectEase));
        seq.Join(icon.rectTransform.DOAnchorPosY(originalPosition.y + selectedYOffset, animDuration)
            .SetEase(selectEase));
        iconTween = seq;

        // 라벨: 페이드인 + 아래에서 올라옴
        if (label != null)
        {
            label.rectTransform.anchoredPosition = new Vector2(
                label.rectTransform.anchoredPosition.x,
                label.rectTransform.anchoredPosition.y - labelYOffset
            );

            Sequence labelSeq = DOTween.Sequence();
            labelSeq.Append(label.DOFade(1f, labelFadeDuration));
            labelSeq.Join(label.rectTransform.DOAnchorPosY(
                label.rectTransform.anchoredPosition.y + labelYOffset, labelFadeDuration)
                .SetEase(Ease.OutQuad));
            labelTween = labelSeq;
        }
    }

    private void PlayDeselectAnimation()
    {
        background.enabled = false;

        // 아이콘: 원래 크기/위치로
        Sequence seq = DOTween.Sequence();
        seq.Append(icon.transform.DOScale(originalScale, animDuration)
            .SetEase(deselectEase));
        seq.Join(icon.rectTransform.DOAnchorPosY(originalPosition.y, animDuration)
            .SetEase(deselectEase));
        iconTween = seq;

        // 라벨: 페이드아웃
        if (label != null)
        {
            labelTween = label.DOFade(0f, labelFadeDuration);
        }
    }

    private void OnDisable()
    {
        iconTween?.Kill();
        labelTween?.Kill();
    }
}
