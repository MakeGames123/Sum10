using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class ErrorLogCollector : MonoBehaviour
{
    private static List<string> errorLogs = new List<string>();
    private const int maxLogs = 20;

    void Awake()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            string log = $"[{System.DateTime.Now}] {logString}\n{stackTrace}\n";

            errorLogs.Add(log);

            // 🔥 너무 많으면 오래된 것 제거
            if (errorLogs.Count > maxLogs)
                errorLogs.RemoveAt(0);
        }
    }

    public static string GetErrorLogs()
    {
        if (errorLogs.Count == 0)
            return "No error logs";

        return string.Join("\n----------------\n", errorLogs);
    }
}