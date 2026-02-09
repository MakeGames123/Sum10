using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public class TopBanner : MonoBehaviour
{
    private BannerView _bannerView;

#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3940256099942544/6300978111"; // 테스트 ID
#elif UNITY_IPHONE
    private string _adUnitId = "ca-app-pub-3940256099942544/2934735716"; // 테스트 ID
#endif

    void Start()
    {
        // SDK 초기화 후 배너 로드
        MobileAds.Initialize((InitializationStatus status) =>
        {
            LoadBannerAd();
        });
    }

    public void LoadBannerAd()
    {
        // 1. 기존 배너가 있다면 제거
        if (_bannerView != null)
        {
            _bannerView.Destroy();
        }

        // 2. 배너 사이즈와 위치 설정 (스마트폰 가로 길이에 맞춘 적응형 배너 권장)
        AdSize adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        // 하단 중앙(Bottom)에 배치
        _bannerView = new BannerView(_adUnitId, adSize, AdPosition.Top);

        // 3. 광고 요청 및 로드
        var adRequest = new AdRequest();
        _bannerView.LoadAd(adRequest);
    }
}
