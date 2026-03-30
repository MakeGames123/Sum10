using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
public class QuestManager : MonoBehaviour
{
    public QuestDataLoader dataLoader;
    public List<QuestState> todayQuests = new List<QuestState>();
    public Image alarm;
    [SerializeField] QuestPanel panel;

    const string SAVE_KEY_DATE = "QUEST_DATE";
    const string SAVE_KEY_LIST = "QUEST_LIST";
    public static QuestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        dataLoader.onLoaded.AddListener(OnDataLoaded);
    }

    void OnDataLoaded()
    {
        if (IsNewDay())
        {
            GenerateNewQuests();
        }
        else
        {
            LoadTodayQuests();
        }
    }

    bool IsNewDay()
    {
        string savedDate = PlayerPrefs.GetString(SAVE_KEY_DATE, "");
        string today = DateTime.Now.ToString("yyyyMMdd");

        return savedDate != today;
    }

    void GenerateNewQuests()
    {
        var allData = dataLoader.GetAllData();

        List<QuestData> pool = new List<QuestData>(allData);
        Shuffle(pool);

        todayQuests.Clear();

        HashSet<string> usedTypes = new HashSet<string>();

        // 1차: 타입 중복 없이 뽑기
        foreach (var data in pool)
        {
            if (todayQuests.Count >= 3)
                break;

            if (usedTypes.Contains(data.MissionType))
                continue;

            todayQuests.Add(new QuestState
            {
                data = data,
                progress = 0,
                isClear = false,
                isRerolled = false
            });

            usedTypes.Add(data.MissionType);
        }

        // 2차: 부족하면 타입 무시하고 채움
        if (todayQuests.Count < 3)
        {
            foreach (var data in pool)
            {
                if (todayQuests.Count >= 3)
                    break;

                bool alreadyExists = false;
                foreach (var q in todayQuests)
                {
                    if (q.data.MissionId == data.MissionId)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists) continue;

                todayQuests.Add(new QuestState
                {
                    data = data,
                    progress = 0,
                    isClear = false,
                    isRerolled = false
                });
            }
        }

        SaveTodayQuests();
    }
    public void AddProgress(string conditionType, int amount)
    {
        foreach (var quest in todayQuests)
        {
            if (quest.isClear) continue;

            if (quest.data.ConditionType != conditionType)
                continue;

            if (quest.data.AccumulateType == "누적")
            {
                quest.progress += amount;
            }
            else if (quest.data.AccumulateType == "비누적")
            {
                quest.progress = Mathf.Max(quest.progress, amount);
            }

            // 클리어 체크
            if (quest.progress >= quest.data.ConditionValue)
            {
                quest.progress = quest.data.ConditionValue;
                quest.isClear = true;
                alarm.enabled = true;

                Debug.Log($"퀘스트 완료: {quest.data.MissionId}");
            }
        }

        SaveTodayQuests();
    }
    void SaveTodayQuests()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        PlayerPrefs.SetString(SAVE_KEY_DATE, today);

        List<string> saveList = new List<string>();

        foreach (var q in todayQuests)
        {
            string entry =
                $"{q.data.MissionId}|{q.progress}|{(q.isClear ? 1 : 0)}|{(q.isRerolled ? 1 : 0)}|{(q.isClaimed ? 1 : 0)}";

            saveList.Add(entry);
        }

        string joined = string.Join(",", saveList);
        PlayerPrefs.SetString(SAVE_KEY_LIST, joined);

        PlayerPrefs.Save();
    }
    void LoadTodayQuests()
    {
        todayQuests.Clear();

        string saved = PlayerPrefs.GetString(SAVE_KEY_LIST, "");

        if (string.IsNullOrEmpty(saved))
        {
            GenerateNewQuests();
            return;
        }

        string[] entries = saved.Split(',');

        foreach (var entry in entries)
        {
            string[] parts = entry.Split('|');

            if (parts.Length < 3) continue;

            string id = parts[0];
            int progress = int.Parse(parts[1]);
            bool isClear = parts[2] == "1";

            bool isRerolled = parts.Length > 3 && parts[3] == "1";
            bool isClaimed = parts.Length > 4 && parts[4] == "1"; // ✅ 추가

            var data = dataLoader.GetDataById(id);
            if (data == null) continue;

            todayQuests.Add(new QuestState
            {
                data = data,
                progress = progress,
                isClear = isClear,
                isRerolled = isRerolled,
                isClaimed = isClaimed
            });
        }
    }
    public void RerollQuest(int index)
    {
        if (index < 0 || index >= todayQuests.Count)
            return;

        var target = todayQuests[index];

        if (target.isRerolled)
        {
            Debug.Log("이미 리롤한 미션입니다.");
            return;
        }

        var allData = dataLoader.GetAllData();

        HashSet<string> currentIds = new HashSet<string>();
        HashSet<string> usedTypes = new HashSet<string>();

        for (int i = 0; i < todayQuests.Count; i++)
        {
            if (i == index) continue;

            currentIds.Add(todayQuests[i].data.MissionId);
            usedTypes.Add(todayQuests[i].data.MissionType);
        }

        List<QuestData> candidates = new List<QuestData>();

        foreach (var data in allData)
        {
            if (currentIds.Contains(data.MissionId))
                continue;

            if (usedTypes.Contains(data.MissionType))
                continue;

            candidates.Add(data);
        }

        // fallback (타입 무시)
        if (candidates.Count == 0)
        {
            foreach (var data in allData)
            {
                if (!currentIds.Contains(data.MissionId))
                {
                    candidates.Add(data);
                }
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("리롤 가능한 미션이 없음");
            return;
        }

        int rand = UnityEngine.Random.Range(0, candidates.Count);
        var newQuest = candidates[rand];

        todayQuests[index] = new QuestState
        {
            data = newQuest,
            progress = 0,
            isClear = false,
            isRerolled = true
        };

        SaveTodayQuests();

        panel.RerollUpdate(index);

        Debug.Log($"리롤 완료: {target.data.MissionId} → {newQuest.MissionId}");
    }
    public void ClaimReward(int index)
    {
        if (index < 0 || index >= todayQuests.Count)
            return;

        var quest = todayQuests[index];

        if (!quest.isClear)
        {
            Debug.Log("아직 클리어 안됨");
            return;
        }

        if (quest.isClaimed)
        {
            Debug.Log("이미 보상 수령함");
            return;
        }

        quest.isClaimed = true;

        SaveTodayQuests();

        ClaimRewardToServer(quest.data);
    }
    void Shuffle(List<QuestData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
    public void ClaimRewardToServer(QuestData data)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "ClaimMissionReward",
            FunctionParameter = new
            {
                missionId = data.MissionId
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result =>
            {
                Debug.Log("보상 지급 성공");
                PlayerData.Instance.AdjustDiamond(data.RewardDiamond);
            },
            error =>
            {
                Debug.LogError("보상 지급 실패: " + error.GenerateErrorReport());
            }
        );
    }
    public void ClearQuestLocalData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY_DATE);
        PlayerPrefs.DeleteKey(SAVE_KEY_LIST);
        PlayerPrefs.Save();

        todayQuests.Clear();

        Debug.Log("퀘스트 로컬 데이터 초기화 완료");
    }
}
[System.Serializable]
public class QuestState
{
    public QuestData data;
    public int progress;
    public bool isClear;
    public bool isRerolled;
    public bool isClaimed;
}