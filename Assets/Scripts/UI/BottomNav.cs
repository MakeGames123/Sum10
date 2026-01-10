using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BottomNav : MonoBehaviour
{
    [SerializeField] RectTransform homePanel;
    [SerializeField] RectTransform themePanel;
    [SerializeField] RectTransform shopPanel;
    [SerializeField] RectTransform rankPanel;
    List<RectTransform> allPanels = new();
    [SerializeField] BottomNavButton homeButton;
    [SerializeField] BottomNavButton themeButton;
    [SerializeField] BottomNavButton shopButton;
    [SerializeField] BottomNavButton rankButton;
    List<BottomNavButton> allButtons = new();
    Vector2 disablePos = new Vector2(-9999, -9999);
    void Awake()
    {
        allPanels.Add(homePanel);
        allPanels.Add(themePanel);
        allPanels.Add(shopPanel);
        allPanels.Add(rankPanel);

        allButtons.Add(homeButton);
        allButtons.Add(themeButton);
        allButtons.Add(shopButton);
        allButtons.Add(rankButton);

        homeButton.onClick.AddListener(() => EnablePanel(homePanel));
        themeButton.onClick.AddListener(() => EnablePanel(themePanel));
        shopButton.onClick.AddListener(() => EnablePanel(shopPanel));
        rankButton.onClick.AddListener(() => EnablePanel(rankPanel));

        homeButton.onClick.AddListener(() => UpdateButtonUI(homeButton));
        themeButton.onClick.AddListener(() => UpdateButtonUI(themeButton));
        shopButton.onClick.AddListener(() => UpdateButtonUI(shopButton));
        rankButton.onClick.AddListener(() => UpdateButtonUI(rankButton));
    }
    void Start()
    {
        EnablePanel(homePanel);
        UpdateButtonUI(homeButton);
    }
    public void EnablePanel(RectTransform target)
    {
        foreach (RectTransform panel in allPanels)
        {
            panel.anchoredPosition = disablePos;
        }

        if (target.TryGetComponent(out IMainPanel ipanel))
        {
            ipanel.SetCondition();
        }
        target.anchoredPosition = Vector2.zero;
    }
    private void UpdateButtonUI(BottomNavButton target)
    {
        foreach (BottomNavButton button in allButtons)
        {
            if (target != button) button.UpdateUI(false);
        }

        target.UpdateUI(true);
    }
}
