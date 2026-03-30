using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] List<QuestUnit> units = new();
    [SerializeField] QuestManager manager = new();
    [SerializeField] Image alarm;

    void OnEnable()
    {
        for (int i = 0; i < 3; i++)
        {
            units[i].SetCondition(manager.todayQuests[i]);
        }

        alarm.enabled = false;
    }
    void OnDisable()
    {
        foreach (var state in manager.todayQuests)
        {
            if (!state.isClaimed && state.isClear) alarm.enabled = true;
        }
    }
    public void RerollUpdate(int index)
    {
        units[index].SetCondition(manager.todayQuests[index]);
    }
}
