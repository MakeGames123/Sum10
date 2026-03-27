using UnityEngine;
using GoogleMobileAds.Api;

public class TopBanner : MonoBehaviour
{
    public static TopBanner Instance;
    private BannerView _bannerView;
    private bool isInitialized = false;

#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3940256099942544/6300978111"; // 테스트 ID
#elif UNITY_IPHONE
    private string _adUnitId = "ca-app-pub-3940256099942544/2934735716"; // 테스트 ID
#endif
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else { Destroy(gameObject); }

        LoadBanner();
    }

    private void LoadBanner()
    {
        if (isInitialized)
        {
            return;
        }

        MobileAds.Initialize(status =>
        {
            isInitialized = true;
        });
    }

    public void PlaceBannerAd()
    {
        if (_bannerView != null || PlayerData.Instance.GetAdStatus())
            return;

        AdSize adSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        _bannerView = new BannerView(_adUnitId, adSize, AdPosition.Top);

        var adRequest = new AdRequest();
        _bannerView.LoadAd(adRequest);

        SettingManager.Instance.LowerPosition(GetBannerHeight());
    }
    public void RemoveBanner()
    {
        if (_bannerView != null)
        {
            _bannerView.Destroy();
            _bannerView = null;
            SettingManager.Instance.ResetPosition();
        }
    }
    private float GetBannerHeight()
    {
        if (_bannerView == null)
            return 0;

        Rect safeArea = Screen.safeArea;
        float unsafeHeight = Screen.height - (safeArea.y + safeArea.height);
        return _bannerView.GetHeightInPixels() / (float)Screen.dpi * 160f + unsafeHeight;
    }
}
