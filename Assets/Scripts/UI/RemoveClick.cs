using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RemoveClick : MonoBehaviour, IPointerClickHandler
{
    public RectTransform panel;
    [Tooltip("이 패널이 화면 안에 있으면 클릭 무시. 상위 팝업이 떠 있을 때 하위 패널이 닫히지 않게 하는 용도.")]
    public RectTransform blockerPanel;
    public UnityEvent onClick = new();

    const float OFFSCREEN_THRESHOLD = 5000f;

    public void OnPointerClick(PointerEventData data)
    {
        if (!IsVisible(panel)) return;
        // 상위 팝업이 떠 있으면 차단
        if (IsVisible(blockerPanel)) return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSFX();
        panel.anchoredPosition = new Vector2(9999, 9999);
        onClick.Invoke();
    }

    // SetActive(false) 또는 anchoredPosition 오프스크린 둘 다 "안 보이는 상태"로 간주
    static bool IsVisible(RectTransform rt)
    {
        if (rt == null) return false;
        if (!rt.gameObject.activeInHierarchy) return false;
        return rt.anchoredPosition.x < OFFSCREEN_THRESHOLD;
    }
}
