using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RemoveClick : MonoBehaviour, IPointerClickHandler
{
    public RectTransform panel;
    public UnityEvent onClick = new();
    public void OnPointerClick(PointerEventData data)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSFX();
        panel.anchoredPosition = new Vector2(9999, 9999);
        onClick.Invoke();
    }
}
