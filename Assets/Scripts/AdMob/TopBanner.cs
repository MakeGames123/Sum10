using UnityEngine;
using GoogleMobileAds.Api;

public class TopBanner : MonoBehaviour
{
    private BannerView _bannerView;
    private bool isInitialized = false;

#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3940256099942544/6300978111"; // 테스트 ID
#elif UNITY_IPHONE
    private string _adUnitId = "ca-app-pub-3940256099942544/2934735716"; // 테스트 ID
#endif

    private void Awake()
    {
        PlayerData.Instance.onAdRemovedChanged.AddListener(OnAdRemovedChanged);
    }

    private void OnDestroy()
    {
        if (PlayerData.Instance != null)
            PlayerData.Instance.onAdRemovedChanged.RemoveListener(OnAdRemovedChanged);
    }

    private void OnAdRemovedChanged(bool removed)
    {
        if (removed)
        {
            RemoveBanner();
        }
        else
        {
            LoadBanner();
        }
    }

    private void LoadBanner()
    {
        if (isInitialized)
        {
            PlaceBannerAd();
            return;
        }

        MobileAds.Initialize(status =>
        {
            isInitialized = true;
            PlaceBannerAd();
        });
    }

    public void PlaceBannerAd()
    {
        if (_bannerView != null)
            return;

        AdSize adSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        _bannerView = new BannerView(_adUnitId, adSize, AdPosition.Top);

        var adRequest = new AdRequest();
        _bannerView.LoadAd(adRequest);
    }

    public void RemoveBanner()
    {
        if (_bannerView != null)
        {
            _bannerView.Destroy();
            _bannerView = null;
        }
    }
}
