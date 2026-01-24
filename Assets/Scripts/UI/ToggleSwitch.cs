using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 토글 스위치 핸들 애니메이션
/// Toggle 컴포넌트가 있는 오브젝트에 붙여서 사용
/// </summary>
[RequireComponent(typeof(Toggle))]
public class ToggleSwitch : MonoBehaviour
{
    [Header("Handle")]
    [SerializeField] private RectTransform handle;
    [SerializeField] private float onPosX = 25f;
    [SerializeField] private float offPosX = -25f;

    [Header("Background")]
    [SerializeField] private Image background;
    [SerializeField, Range(0.5f, 1f)] private float offBrightness = 0.7f;  // OFF일 때 밝기 (1 = 원본)

    [Header("Animation")]
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private Toggle toggle;
    private Tweener handleTween;
    private Tweener colorTween;
    private Color originalColor;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (background != null)
            originalColor = background.color;
        toggle.onValueChanged.AddListener(OnToggleChanged);
        UpdateImmediate(toggle.isOn);
    }

    void OnDestroy()
    {
        handleTween?.Kill();
        colorTween?.Kill();
    }

    private void OnToggleChanged(bool isOn)
    {
        float targetX = isOn ? onPosX : offPosX;

        handleTween?.Kill();
        handleTween = handle.DOAnchorPosX(targetX, duration)
            .SetEase(easeType)
            .SetUpdate(true);

        if (background != null)
        {
            Color targetColor = isOn ? originalColor : originalColor * offBrightness;
            targetColor.a = originalColor.a;
            colorTween?.Kill();
            colorTween = background.DOColor(targetColor, duration)
                .SetUpdate(true);
        }
    }

    private void UpdateImmediate(bool isOn)
    {
        float targetX = isOn ? onPosX : offPosX;
        handle.anchoredPosition = new Vector2(targetX, handle.anchoredPosition.y);

        if (background != null)
        {
            Color targetColor = isOn ? originalColor : originalColor * offBrightness;
            targetColor.a = originalColor.a;
            background.color = targetColor;
        }
    }

    public void SetValueWithoutAnimation(bool isOn)
    {
        toggle.SetIsOnWithoutNotify(isOn);
        UpdateImmediate(isOn);
    }
}
