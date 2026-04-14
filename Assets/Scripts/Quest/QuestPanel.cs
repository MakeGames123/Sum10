using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] List<QuestUnit> units = new();
    [SerializeField] QuestManager manager = new();
    [SerializeField] Image alarm;
    [SerializeField] TextMeshProUGUI resetTime;

    void OnEnable()
    {
        for (int i = 0; i < 3; i++)
        {
            units[i].SetCondition(manager.todayQuests[i]);
        }

        alarm.enabled = false;

        UpdateResetTime(); // 여기서 갱신
    }

    void OnDisable()
    {
        Debug.Log("asd");
        foreach (var state in manager.todayQuests)
        {
            if (!state.isClaimed && state.isClear) alarm.enabled = true;
        }
    }

    public void RerollUpdate(int index)
    {
        units[index].SetCondition(manager.todayQuests[index]);
    }

    void UpdateResetTime()
    {
        DateTime now = DateTime.Now;

        // 오늘 자정
        DateTime nextReset = new DateTime(now.Year, now.Month, now.Day).AddDays(1);

        TimeSpan remain = nextReset - now;

        int hours = remain.Hours;
        int minutes = remain.Minutes;

        resetTime.text = $"남은 시간 : {hours}h {minutes}m";
    }
}