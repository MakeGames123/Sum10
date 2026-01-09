using UnityEngine;

/// <summary>
/// 화면 모서리에 항상 고정되도록 위치 조정
/// Canvas Scaler와 무관하게 실제 화면 기준으로 배치
/// </summary>
public class ScreenCornerAnchor : MonoBehaviour
{
    public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

    [SerializeField] private Corner corner = Corner.TopRight;
    [SerializeField] private Vector2 offset = new Vector2(-20, -20); // 모서리에서의 오프셋

    private RectTransform rectTransform;
    private Canvas canvas;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        AdjustPosition();
    }

    void AdjustPosition()
    {
        if (rectTransform == null || canvas == null) return;

        // 실제 화면 크기
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Canvas 스케일 팩터
        float scaleFactor = canvas.scaleFactor;

        // Canvas 기준 화면 크기
        float canvasWidth = screenWidth / scaleFactor;
        float canvasHeight = screenHeight / scaleFactor;

        // Canvas RectTransform
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasRectWidth = canvasRect.rect.width;
        float canvasRectHeight = canvasRect.rect.height;

        // Canvas 영역과 실제 화면의 차이 계산
        float widthDiff = (canvasWidth - canvasRectWidth) / 2f;
        float heightDiff = (canvasHeight - canvasRectHeight) / 2f;

        // 앵커를 중앙으로 설정
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // 모서리별 위치 계산
        Vector2 position = Vector2.zero;
        Vector2 pivot = Vector2.zero;

        switch (corner)
        {
            case Corner.TopRight:
                position = new Vector2(canvasWidth / 2f + offset.x, canvasHeight / 2f + offset.y);
                pivot = new Vector2(1f, 1f);
                break;
            case Corner.TopLeft:
                position = new Vector2(-canvasWidth / 2f - offset.x, canvasHeight / 2f + offset.y);
                pivot = new Vector2(0f, 1f);
                break;
            case Corner.BottomRight:
                position = new Vector2(canvasWidth / 2f + offset.x, -canvasHeight / 2f - offset.y);
                pivot = new Vector2(1f, 0f);
                break;
            case Corner.BottomLeft:
                position = new Vector2(-canvasWidth / 2f - offset.x, -canvasHeight / 2f - offset.y);
                pivot = new Vector2(0f, 0f);
                break;
        }

        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = position;
    }
}
