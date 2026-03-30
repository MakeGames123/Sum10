using System.Collections.Generic;
using UnityEngine;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] List<QuestUnit> units = new();
    [SerializeField] QuestManager manager = new();

    void OnEnable()
    {
        for (int i = 0; i < 3; i++)
        {
            units[i].SetCondition(manager.todayQuests[i]);
        }
    }
    public void RerollUpdate(int index)
    {
        units[index].SetCondition(manager.todayQuests[index]);
    }
}
