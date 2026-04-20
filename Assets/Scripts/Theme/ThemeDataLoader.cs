using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class ThemeDataLoader : MonoBehaviour
{
    private string url = "https://docs.google.com/spreadsheets/d/1sXSVodMBbKb3wD5FsQg5WmRYS1rdXWTtK9HuVxrZ5VM/export?format=csv&gid=931903939";

    public List<SkinPriceData> dataList = new List<SkinPriceData>();
    public static ThemeDataLoader Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else { Destroy(gameObject); }
    }
    void Start()
    {
        StartCoroutine(LoadData());
    }

    IEnumerator LoadData()
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("로드 실패: " + www.error);
            yield break;
        }

        ParseCSV(www.downloadHandler.text);
    }

    void ParseCSV(string csv)
    {
        string[] lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++) // 첫 줄은 헤더
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] values = lines[i].Split(',');

            SkinPriceData data = new SkinPriceData
            {
                slot = int.Parse(values[0]),
                diaPrice = int.Parse(values[1]),
                name = values[2]
            };

            dataList.Add(data);
        }

        Debug.Log("로드 완료: " + dataList.Count);
    }
}
[System.Serializable]
public class SkinPriceData
{
    public int slot;
    public int diaPrice;
    public string name;
}