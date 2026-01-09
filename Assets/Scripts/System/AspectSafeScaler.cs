using UnityEngine;

/// <summary>
/// 화면 비율에 따라 자동으로 스케일을 조정하여 잘림 방지
/// </summary>
public class AspectSafeScaler : MonoBehaviour
{
    [SerializeField] private float referenceWidth = 1080f;
    [SerializeField] private float referenceHeight = 2340f;

    // 공유 스케일 값 (다른 스크립트에서 참조 가능)
    public static float SafeScale { get; private set; } = 1f;

    void Start()
    {
        AdjustScale();
    }

    [SerializeField] private bool allowScaleUp = false; // 넓은 화면에서 확대 허용

    void AdjustScale()
    {
        float referenceAspect = referenceWidth / referenceHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        float scale = currentAspect / referenceAspect;

        // 축소는 항상 허용, 확대는 옵션
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
