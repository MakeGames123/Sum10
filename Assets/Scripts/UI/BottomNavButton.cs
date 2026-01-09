using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BottomNavButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image background;
    [SerializeField] Image icon;
    public UnityEvent onClick = new();

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick.Invoke();
    }
    public void UpdateUI(bool flag)
    {
        if (flag)
        {
            background.enabled = true;
            //선택 애니메이션
        }
        else
        {
            background.enabled = false;
            //비선택 애니메이션
        }
    }
}
