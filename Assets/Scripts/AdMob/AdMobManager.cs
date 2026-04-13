using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance { get; private set; }
    // 테스트용 ID (개발 완료 후 실제 ID로 교체하세요)
#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3940256099942544/1033173712";
#else
    private string _adUnitId = "unused";
#endif

#if UNITY_ANDROID
    private string _adRewradId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
    private string _adRewradId = "ca-app-pub-3940256099942544/1712485313";
#else
    private string _adRewradId = "unused";
#endif
    private InterstitialAd _interstitialAd;
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
            // 앱 실행 시 쿨타임 리셋 로직:
            // - 키가 없거나(최초 실행) 쿨타임이 이미 끝난 경우 → 현재 시각으로 리셋 (5분 유예 시작)
            // - 쿨타임이 아직 유효한 경우 → 리셋 안 함 (강종 후 재실행 꼼수 방어)
            if (!PlayerPrefs.HasKey(LastAdTimeKey) || IsCooldownOver())
            {
                SaveLastAdTime();
            }

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

    public void ShowInterstitialAd()
    {
        if (PlayerData.Instance.GetAdStatus()) return;

        if (!IsCooldownOver())
        {
            float remaining = GetRemainingCooldown();
            Debug.LogError($"쿨타임: {remaining:F0}초 남음");
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
    public void ShowRewardedAd(System.Action<bool> onComplete)
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                // 광고 시청 완료 (보상 지급 가능)
                onComplete?.Invoke(true);
            });

            // 광고가 닫히면 다음을 위해 미리 로드
            _rewardedAd.OnAdFullScreenContentClosed += () => { LoadRewardedAd(); };
        }
        else
        {
            Debug.Log("광고 준비 안 됨");
            onComplete?.Invoke(false); // 실패 알림
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
