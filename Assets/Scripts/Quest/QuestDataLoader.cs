using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class QuestDataLoader : MonoBehaviour
{
    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1sXSVodMBbKb3wD5FsQg5WmRYS1rdXWTtK9HuVxrZ5VM/gviz/tq?tqx=out:csv&sheet=Daily mission";

    public UnityEvent onLoaded = new();
    private Dictionary<int, QuestData> dataDict
        = new Dictionary<int, QuestData>();


    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(StartLoad());
    }
    public IEnumerator StartLoad()
    {
        yield return StartCoroutine(LoadSheet());
    }
    public IEnumerator LoadSheet()
    {
        UnityWebRequest req = UnityWebRequest.Get(SHEET_URL);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            yield break;
        }

        ParseCSV(req.downloadHandler.text);
    }

    void ParseCSV(string csv)
    {
        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = lines[i].Split(',');

            QuestData data = new QuestData
            {
                MissionId = CleanString(cols[0]),
                MissionType = CleanString(cols[1]),
                TitleKr = CleanString(cols[2]),
                TitleEn = CleanString(cols[3]),
                DescriptionKr = CleanString(cols[4]),
                ConditionType = CleanString(cols[5]),
                AccumulateType = CleanString(cols[6]),
                ConditionValue = ToInt(cols[7]),
                RewardDiamond = ToInt(cols[8]),
            };

            dataDict[i] = data; // 또는 MissionId 기반으로 바꿔도 됨
        }

        Debug.Log($"QuestData Loaded: {dataDict.Count}");
        onLoaded.Invoke();
    }
    int ToInt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        string cleaned = value
            .Trim()
            .Trim('"');

        if (!int.TryParse(cleaned, out int result))
        {
            Debug.LogWarning($"Int Parse 실패: '{value}'");
            return 0;
        }

        return result;
    }
    string CleanString(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return value
            .Trim()
            .Trim('"')
            .Replace("\uFEFF", "")   // BOM
            .Replace("\u200B", "")   // Zero-width space
            .Normalize(System.Text.NormalizationForm.FormC);
    }
    public List<QuestData> GetAllData()
    {
        return new List<QuestData>(dataDict.Values);
    }

    public QuestData GetDataById(string id)
    {
        foreach (var data in dataDict.Values)
        {
            if (data.MissionId == id)
                return data;
        }
        return null;
    }
}
[System.Serializable]
public class QuestData
{
    public string MissionId;
    public string MissionType;
    public string TitleKr;
    public string TitleEn;
    public string DescriptionKr;
    public string ConditionType;
    public string AccumulateType;
    public int ConditionValue;
    public int RewardDiamond;
}
