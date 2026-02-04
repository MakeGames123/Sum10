using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using System;
public class ResetTimer : MonoBehaviour
{
    [SerializeField] PlayFabLoginManager login;
    [SerializeField] TextMeshProUGUI text;
    private DateTime cachedServerTime;

    void Awake()
    {
        login.onLogined.AddListener(UpdateWeeklyResetText);
    }    
    public void UpdateWeeklyResetText()
    {
        PlayFabClientAPI.GetTime(
            new GetTimeRequest(),
            result =>
            {
                DateTime cachedServerTime = result.Time.ToUniversalTime();
                TimeSpan remain = GetRemainingWeeklyTime(cachedServerTime);
                FormatRemainingTime(remain);
            },
            error =>
            {
                Debug.LogError("서버 시간 조회 실패");
            }
        );
    }
    public TimeSpan GetRemainingWeeklyTime(DateTime serverTime)
    {
        // 리셋 기준: 매주 월요일 00:00 (UTC)
        DayOfWeek resetDay = DayOfWeek.Monday;
        int resetHour = 0;

        DateTime nextReset = serverTime.Date.AddHours(resetHour);

        int daysUntilReset =
            ((int)resetDay - (int)serverTime.DayOfWeek + 7) % 7;

        if (daysUntilReset == 0 && serverTime >= nextReset)
            daysUntilReset = 7;

        nextReset = nextReset.AddDays(daysUntilReset);

        return nextReset - serverTime;
    }
    public void FormatRemainingTime(TimeSpan time)
    {
        int days = time.Days;
        int hours = time.Hours;
        int minutes = time.Minutes;

        text.text = $"초기화까지 남은 시간: {days}일 {hours:D2}시간 {minutes:D2}분";
    }
}
