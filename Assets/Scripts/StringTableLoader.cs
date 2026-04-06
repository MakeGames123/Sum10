using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class StringTableLoader : MonoBehaviour
{
    public static StringTableLoader Instance { get; private set; }

    private string url = "https://docs.google.com/spreadsheets/d/1sXSVodMBbKb3wD5FsQg5WmRYS1rdXWTtK9HuVxrZ5VM/export?format=csv&gid=914195797";

    private Dictionary<int, Dictionary<string, string>> table = new();

    public string CurrentLanguage { get; private set; } = "en";

    public bool isLoaded = false;

    public UnityEvent onLoaded = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadSheet());
    }
    IEnumerator LoadSheet()
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("StringTable Load Failed: " + request.error);
            yield break;
        }

        ParseCSV(request.downloadHandler.text);
        isLoaded = true;

        onLoaded.Invoke();
        Debug.Log("StringTable Loaded");
    }

    void ParseCSV(string csv)
    {
        string[] lines = csv.Split('\n');

        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');

            int id = int.Parse(values[0]);

            var langDict = new Dictionary<string, string>();

            for (int j = 1; j < headers.Length; j++)
            {
                if (j < values.Length)
                {
                    langDict[headers[j]] = values[j].Replace("\\n", "\n");
                }
            }

            table[id] = langDict;
        }
    }

    public string GetText(int id)
    {
        if (!isLoaded)
        {
            Debug.LogWarning("StringTable not loaded yet");
            return $"LOADING_{id}";
        }

        if (table.TryGetValue(id, out var langDict))
        {
            if (langDict.TryGetValue(CurrentLanguage, out var value))
            {
                return value;
            }
        }

        return $"MISSING_{id}";
    }

    public void SetLanguage(string lang)
    {
        CurrentLanguage = lang;
    }
}