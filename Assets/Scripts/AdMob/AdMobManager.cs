using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance { get; private set; }
    // 테스트용 ID (개발 완료 후 실제 ID로 교체하세요)
#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
    private string _adUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private string _adUnitId = "unused";
#endif

    private RewardedAd _rewardedAd;
    private void Awake()
    {
        // 싱글톤 구현: 이미 존재하면 파괴, 없으면 유지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // SDK 초기화
        MobileAds.Initialize((InitializationStatus status) =>
        {
            LoadRewardedAd();
        });
    }

    // 1. 광고 로드하기
    public void LoadRewardedAd()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        var adRequest = new AdRequest();
        RewardedAd.Load(_adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("보상형 광고 로드 실패: " + error);
                return;
            }
            _rewardedAd = ad;
            RegisterEventHandlers(_rewardedAd); // 이벤트 연결
        });
    }
    // 3. 광고 이벤트 핸들러 (광고가 닫혔을 때 다시 로드하는 등)
    private void RegisterEventHandlers(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("광고가 닫혔습니다. 다음 광고를 로드합니다.");
            LoadRewardedAd(); // 닫히면 바로 다음 광고 미리 준비
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("광고 표시에 실패했습니다: " + error);
            LoadRewardedAd();
        };
    }

    // 광고 쿨타임 설정을 위한 변수 (초 단위)
    private const float AdCooldownSeconds = 300f;
    private const string LastAdTimeKey = "LastRewardedAdTime";

    public void ShowRewardedAd()
    {
        // 1. 쿨타임 체크
        if (!IsCooldownOver())
        {
            float remaining = GetRemainingCooldown();
            Debug.Log($"광고 쿨타임 중입니다. 남은 시간: {remaining:F0}초");
            return;
        }

        // 2. 광고 로드 여부 체크 및 표시
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                // 광고 시청 완료 시점 기록 (쿨타임 시작)
                SaveLastAdTime();
                Debug.Log("보상 지급 로직을 여기에 실행하세요.");
            });
        }
        else
        {
            Debug.Log("광고가 아직 준비되지 않았습니다.");
            LoadRewardedAd(); // 누락되었다면 로드 시도
        }
    }

    // 쿨타임이 끝났는지 확인
    private bool IsCooldownOver()
    {
        if (!PlayerPrefs.HasKey(LastAdTimeKey)) return true;

        string lastTimeStr = PlayerPrefs.GetString(LastAdTimeKey);
        System.DateTime lastTime = System.DateTime.Parse(lastTimeStr);
        System.TimeSpan elapsed = System.DateTime.Now - lastTime;

        return elapsed.TotalSeconds >= AdCooldownSeconds;
    }

    // 마지막 광고 시청 시간 저장
    private void SaveLastAdTime()
    {
        PlayerPrefs.SetString(LastAdTimeKey, System.DateTime.Now.ToString());
        PlayerPrefs.Save();
    }

    // (선택사항) 남은 쿨타임 계산용
    private float GetRemainingCooldown()
    {
        if (!PlayerPrefs.HasKey(LastAdTimeKey)) return 0;

        System.DateTime lastTime = System.DateTime.Parse(PlayerPrefs.GetString(LastAdTimeKey));
        System.TimeSpan elapsed = System.DateTime.Now - lastTime;
        float remaining = AdCooldownSeconds - (float)elapsed.TotalSeconds;

        return Mathf.Max(0, remaining);
    }
}
