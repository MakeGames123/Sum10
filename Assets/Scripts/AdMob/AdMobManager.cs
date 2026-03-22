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
    private InterstitialAd _interstitialAd;
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
            LoadInterstitialAd();
        });
    }

    // 1. 광고 로드하기
    public void LoadInterstitialAd()
    {
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        var adRequest = new AdRequest();

        InterstitialAd.Load(_adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("보상형 광고 로드 실패: " + error);
                return;
            }

            _interstitialAd = ad;
            RegisterEventHandlers(_interstitialAd);

            // 🔥 처음 로드된 경우에만 쿨타임 시작
            if (!PlayerPrefs.HasKey(LastAdTimeKey))
            {
                SaveLastAdTime();
            }
        });
    }
    // 3. 광고 이벤트 핸들러 (광고가 닫혔을 때 다시 로드하는 등)
    private void RegisterEventHandlers(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("광고 닫힘 → 다시 로드");
            LoadInterstitialAd();
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("광고 표시 실패: " + error);
            LoadInterstitialAd();
        };
    }

    // 광고 쿨타임 설정을 위한 변수 (초 단위)
    private const float AdCooldownSeconds = 300f;
    private const string LastAdTimeKey = "LastRewardedAdTime";

    public void ShowInterstitialAd()
    {
        if (!IsCooldownOver())
        {
            float remaining = GetRemainingCooldown();
            Debug.Log($"쿨타임: {remaining:F0}초 남음");
            return;
        }

        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();

            SaveLastAdTime(); // 보상 대신 그냥 여기서 쿨타임 시작
        }
        else
        {
            Debug.Log("광고 준비 안됨");
            LoadInterstitialAd();
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
