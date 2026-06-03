using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LocalizationLoader : MonoBehaviour
{
    public static LocalizationLoader Instance { get; private set; }

    private string sheetID = "1sXSVodMBbKb3wD5FsQg5WmRYS1rdXWTtK9HuVxrZ5VM";
    private string sheetName = "string_id";

    [Header("언어 설정")]
    [Tooltip("현재 게임에 적용 중인 언어 코드입니다.")]
    [SerializeField] private string currentLanguage = "en";
    public string CurrentLanguage => currentLanguage;

#if UNITY_EDITOR
    [Header("에디터 디버그 설정")]
    [SerializeField] private bool useEditorTestLanguage = false;
    [SerializeField] private string editorTestLanguage = "en";
#endif

    public bool isLoaded = false;
    public bool isInitialized = false;
    private Dictionary<int, LocalizationData> localizationDict = new Dictionary<int, LocalizationData>();
    public event Action OnLocalizationLoaded;
    public event Action OnInitialize;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 시트 데이터를 받기 전에 기기 시스템 언어로 초기 설정
            InitSystemLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(DownloadSpreadsheetCSV());
    }

    /// <summary>
    /// 기기의 시스템 언어를 감지하여 내부 언어 코드로 변환합니다.
    /// </summary>
    private void InitSystemLanguage()
    {
#if UNITY_EDITOR
        // 에디터에서 테스트 언어 사용 체크 시 시스템 언어를 무시하고 지정한 언어로 로드
        if (useEditorTestLanguage)
        {
            currentLanguage = editorTestLanguage.ToLower();
            Debug.Log($"[Localization] 에디터 테스트 언어 적용: {currentLanguage}");
            OnInitialize?.Invoke();
            isInitialized = true;
            return;
        }
#endif

        // 실제 기기의 시스템 언어 판단
        SystemLanguage systemLang = Application.systemLanguage;

        switch (systemLang)
        {
            case SystemLanguage.Korean:
                currentLanguage = "kr";
                break;
            case SystemLanguage.English:
                currentLanguage = "en";
                break;
            default:
                currentLanguage = "en"; // 지원하지 않는 언어일 경우 기본값(영어)
                break;
        }
        OnInitialize?.Invoke();
        isInitialized = true;
        Debug.Log($"[Localization] 시스템 언어 감지 완료: {systemLang} -> 코드: {currentLanguage}");
    }

    IEnumerator DownloadSpreadsheetCSV()
    {
        string url = $"https://docs.google.com/spreadsheets/d/{sheetID}/gviz/tq?tqx=out:csv&sheet={sheetName}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"구글 시트 로드 실패: {webRequest.error}");
            }
            else
            {
                ParseCSV(webRequest.downloadHandler.text);
                OnLocalizationLoaded?.Invoke();
                isLoaded = true;
            }
        }
    }

    void ParseCSV(string csvText)
    {
        string[] lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> row = ParseCsvLine(lines[i]);

            if (row.Count >= 3)
            {
                if (int.TryParse(CleanValue(row[0]), out int parsedId))
                {
                    LocalizationData data = new LocalizationData();
                    data.id = parsedId;
                    data.kr = CleanValue(row[1]).Replace("\\n", "\n");
                    data.en = CleanValue(row[2]).Replace("\\n", "\n");

                    if (!localizationDict.ContainsKey(data.id))
                    {
                        localizationDict.Add(data.id, data);
                    }
                }
            }
        }
    }
    private List<string> ParseCsvLine(string line)
    {
        List<string> fields = new List<string>();

        bool inQuotes = false;
        string currentField = "";

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        fields.Add(currentField);

        return fields;
    }

    private string CleanValue(string value) => string.IsNullOrEmpty(value) ? "" : value.Trim('"');

    public string GetText(int id)
    {
        //Debug.Log(id + " " + currentLanguage);
        if (localizationDict.TryGetValue(id, out LocalizationData data))
        {
            if (id == 51) Debug.Log(data.kr + " " + data.en);
            switch (currentLanguage.ToLower())
            {
                case "kr": return data.kr;
                case "en": return data.en;
                default: return data.kr;
            }
        }
        return $"[Key '{id}' Not Found]";
    }

    /// <summary>
    /// 외부(설정 UI 등)에서 수동으로 언어를 변경할 때 사용하는 함수
    /// </summary>
    public void ChangeLanguage(string langCode)
    {
        currentLanguage = langCode.ToLower();
        OnLocalizationLoaded?.Invoke();
        Debug.Log($"[Localization] 언어 수동 변경: {currentLanguage}");
    }
}
[System.Serializable]
public struct LocalizationData
{
    public int id; // string에서 int로 변경
    public string kr;
    public string en;
}
