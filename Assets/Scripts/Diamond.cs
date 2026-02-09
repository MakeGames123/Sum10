using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Diamond : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] Button button;
    [SerializeField] RectTransform panel;

    void Start()
    {
        PlayerData.Instance.onDiamondChanged.AddListener(UpdateUI);
        UpdateUI(PlayerData.Instance.GetDiamond());
        button.onClick.AddListener(EnablePanel);
    }
    private void UpdateUI(int val)
    {
        text.text = val.ToString();
    }
    private void EnablePanel()
    {
        panel.anchoredPosition = Vector2.zero;
    }
}
