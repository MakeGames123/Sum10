using UnityEngine;
using TMPro;
using DG.Tweening;

public class TextFloating : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;

    [Header("Animation Settings")]
    [SerializeField] float moveUpDistance = 50f;
    [SerializeField] float duration = 0.7f;
    [SerializeField] float punchScale = 0.3f;

    [Header("Colors (Inspector에서 설정)")]
    [SerializeField] Color scoreColor = new Color(1f, 0.85f, 0.2f, 1f);  // 노란색/금색
    [SerializeField] Color timeColor = new Color(0.4f, 1f, 0.6f, 1f);    // 초록색/민트색

    public void SetCondition(string value, bool isTime = false)
    {
        text.text = value;
        text.color = isTime ? timeColor : scoreColor;

        RectTransform rt = text.rectTransform;
        Vector2 startPos = rt.anchoredPosition;

        // 초기 스케일 설정
        rt.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        // 시작 시 톡 튀어나오는 효과
        seq.Append(
            rt.DOPunchScale(Vector3.one * punchScale, 0.15f, 1, 0.5f)
              .SetEase(Ease.OutBack)
        );

        // 위로 올라가면서 페이드아웃
        seq.Insert(0f,
            rt.DOAnchorPosY(startPos.y + moveUpDistance, duration)
              .SetEase(Ease.OutCubic)
        );

        // 완전히 페이드아웃 (후반부에)
        seq.Insert(duration * 0.4f,
            text.DOFade(0f, duration * 0.6f)
                .SetEase(Ease.InQuad)
        );

        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}
