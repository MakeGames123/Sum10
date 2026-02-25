using UnityEngine;

/// <summary>
/// 화면 비율에 따라 자동으로 스케일을 조정
/// Cover 모드: 배경용 — 어떤 비율에서든 화면을 빈틈없이 채움 (세로 기준, 가로 초과 허용)
/// Fit 모드: UI용 — 넓은 화면에서 잘리지 않도록 축소
/// </summary>
public class AspectSafeScaler : MonoBehaviour
{
    public enum ScaleMode { Fit, Cover }

    [SerializeField] private ScaleMode mode = ScaleMode.Fit;
    [SerializeField] private float referenceWidth = 1080f;
    [SerializeField] private float referenceHeight = 2340f;
    [SerializeField] private bool allowScaleUp = false;

    public static float SafeScale { get; private set; } = 1f;

    void Start()
    {
        AdjustScale();
    }

    /// <summary>
    /// 테마 변경 등으로 배경 스프라이트가 바뀔 때 외부에서 호출 가능
    /// </summary>
    public void Refresh() => AdjustScale();

    void AdjustScale()
    {
        if (mode == ScaleMode.Cover)
            AdjustCover();
        else
            AdjustFit();
    }

    /// <summary>
    /// Cover: 캔버스 세로에 정확히 맞춤 (가로는 초과 허용)
    /// </summary>
    void AdjustCover()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransform rect = GetComponent<RectTransform>();

        float scale = canvasRect.rect.height / rect.rect.height;
        transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// Fit: 기존 방식 — 넓은 화면에서 UI가 잘리지 않도록 축소
    /// </summary>
    void AdjustFit()
    {
        float referenceAspect = referenceWidth / referenceHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        float scale = currentAspect / referenceAspect;

        if (scale < 1f || allowScaleUp)
        {
            SafeScale = scale;
            transform.localScale = Vector3.one * scale;
        }
        else
        {
            SafeScale = 1f;
        }
    }
}
