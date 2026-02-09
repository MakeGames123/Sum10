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

    // 2. 광고 보여주기 (버튼 클릭 이벤트 등에 연결)
    public void ShowRewardedAd(System.Action<bool> onComplete)
    {
        Debug.Log(_rewardedAd);
        Debug.Log(_rewardedAd.CanShowAd());
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
}
