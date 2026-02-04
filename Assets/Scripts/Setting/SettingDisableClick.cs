using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SettingDisableClick : MonoBehaviour, IPointerClickHandler
{
    public SettingManager panel;
    public void OnPointerClick(PointerEventData data)
    {
        panel.Hide();
    }
}
