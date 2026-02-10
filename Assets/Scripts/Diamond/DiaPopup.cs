using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DiaPopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;

    public void SetCondition(int amount)
    {
        text.text = $"{amount} 다이아 구매 성공!";
    }
}
