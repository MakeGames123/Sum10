using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProfileSlot : MonoBehaviour, IPointerClickHandler
{
    public int index;
    [SerializeField] Image image;
    [SerializeField] Image check;
    [SerializeField] Image border;
    public UnityEvent<int> onClick = new();
    public void SetCondition(Sprite sprite, int index)
    {
        image.sprite = sprite;
        this.index = index;
    }
    public void OnPointerClick(PointerEventData data)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSFX();
        onClick.Invoke(index);
    }
    public void EnableCheck()
    {
        check.enabled = true;
    }
    public void DisableCheck()
    {
        check.enabled = false;
    }
    public void EnableBorder()
    {
        border.enabled = true;
    }
    public void DisableBorder()
    {
        border.enabled = false;
    }
}
