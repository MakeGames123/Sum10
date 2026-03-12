using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DisableClick : MonoBehaviour, IPointerClickHandler
{
    public GameObject panel;
    public void OnPointerClick(PointerEventData data)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSFX();
        panel.SetActive(false);
    }
}
