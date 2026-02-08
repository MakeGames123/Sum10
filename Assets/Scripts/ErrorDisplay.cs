using UnityEngine;
using TMPro;

public class ErrorDisplay : MonoBehaviour
{
    private string errorText = "";
    private TextMeshProUGUI text;
    public static ErrorDisplay instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
        text = transform.GetComponent<TextMeshProUGUI>();
        Application.logMessageReceived += HandleLog;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 0 = 왼쪽 마우스 클릭
        {
            text.enabled = false;
        }
    }
    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        text.enabled = true;
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorText = logString + "\n" + stackTrace + "\n";
            text.text += errorText;
        }
    }
}
