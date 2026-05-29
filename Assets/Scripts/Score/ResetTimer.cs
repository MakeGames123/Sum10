using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

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
                // 로컬 변수 중복 선언 제거하고 클래스 필드에 저장
                cachedServerTime = result.Time.ToUniversalTime();
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

        int daysUntilReset = ((int)resetDay - (int)serverTime.DayOfWeek + 7) % 7;

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

        // 1. 매니저가 정상적으로 로드되었는지 체크하는 조건문
        if (LocalizationLoader.Instance != null)
        {
            // 19번 ID의 로컬라이즈 원본 포맷 텍스트를 가져옴
            // 예 (KR): "초기화까지 남은 시간\n{0}일 {1}시간 {2}분" 
            // 예 (EN): "Time until reset\n{0}d {1}h {2}m"
            string localizedFormat = LocalizationLoader.Instance.GetText(19);

            // 시트 데이터에 {0}, {1} 같은 포맷팅 문자가 포함되어 있다면 string.Format으로 조립
            if (localizedFormat.Contains("{0}"))
            {
                text.text = string.Format(localizedFormat, days, hours.ToString("D2"), minutes.ToString("D2"));
            }
            else
            {
                // 구글 시트에 {0} 포맷을 안 적어두고 그냥 타이틀만 적어두셨을 경우를 대비한 예외 처리 예시
                // (현재 다국어 언어 코드에 따라 뒤에 붙는 단위를 조건문 분기 처리)
                string lang = LocalizationLoader.Instance.CurrentLanguage;
                if (lang == "kr")
                    text.text = $"{localizedFormat}: {days}일 {hours:D2}시간 {minutes:D2}분";
                else // en, ch 등 기본 단위
                    text.text = $"{localizedFormat}: {days}d {hours:D2}h {minutes:D2}m";
            }
        }
        else
        {
            // 매니저가 없거나 로드가 안 되었을 때를 대비한 기본 방어 코드(한글 기본 fallback)
            text.text = $"초기화까지 남은 시간: {days}일 {hours:D2}시간 {minutes:D2}분";
        }
    }
}