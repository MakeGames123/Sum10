using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance { get; private set; }
    // 테스트용 ID (개발 완료 후 실제 ID로 교체하세요)
#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-1896612926436691/5978071864";
#else
    private string _adUnitId = "unused";
#endif

#if UNITY_ANDROID
    private string _adRewradId = "ca-app-pub-1896612926436691/7716037204";
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
            LoadRewardedAd();
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
        RewardedAd.Load(_adRewradId, adRequest, (RewardedAd ad, LoadAdError error) =>
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
    // 광고 닫힘 후 실행할 콜백 (퀘스트/점수 기록 용도)
    private System.Action _pendingAdCallback;
    // 재진입 가드: 광고 표시 중 두 번째 호출이 _interstitialAd를 destroy하는 race 차단
    private bool _isShowingAd = false;

    // 3. 광고 이벤트 핸들러 (광고가 닫혔을 때 다시 로드하는 등)
    private void RegisterEventHandlers(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("광고 닫힘 → 쿨타임 시작 → 콜백 실행 → 다시 로드");
            _isShowingAd = false;
            SaveLastAdTime(); // 광고 끝까지 봐야 쿨타임 시작
            _pendingAdCallback?.Invoke();
            _pendingAdCallback = null;
            LoadInterstitialAd();
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("광고 표시 실패: " + error);
            _isShowingAd = false;
            SaveLastAdTime(); // 실패도 쿨타임 시작 (무한 재시도 방지)
            _pendingAdCallback?.Invoke();
            _pendingAdCallback = null;
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
    private const float AdCooldownSeconds = 240f;
    private const string LastAdTimeKey = "LastRewardedAdTime";

    /// <summary>
    /// 삽입형 광고 표시. onComplete는 광고 시청 완료(또는 광고 미표시) 후 호출됨.
    /// 유저가 광고 중 강종하면 onComplete 미호출 → 퀘스트/점수 미기록 (의도된 동작)
    /// </summary>
    public void ShowInterstitialAd(System.Action onComplete = null)
    {
        if (PlayerData.Instance.GetAdStatus() || LocalizationLoader.Instance.CurrentLanguage != "kr")
        {
            onComplete?.Invoke(); // 광고 제거 유저 → 즉시 콜백
            return;
        }

        if (!IsCooldownOver())
        {
            float remaining = GetRemainingCooldown();
            Debug.LogError($"쿨타임: {remaining:F0}초 남음");
            onComplete?.Invoke(); // 쿨타임 중 → 즉시 콜백
            return;
        }

        // 재진입 가드: 이미 광고 표시 중이면 LoadInterstitialAd로 ad destroy되는 race 차단
        // 첫 광고가 정상 종료해서 쿨타임/콜백 보장. 두 번째 호출자는 자기 콜백만 즉시 실행.
        if (_isShowingAd)
        {
            Debug.Log("이미 광고 표시 중 → 두 번째 호출 무시 (콜백만 즉시 실행)");
            onComplete?.Invoke();
            return;
        }

        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _isShowingAd = true;
            _pendingAdCallback = onComplete; // 광고 닫힘 시 호출
            _interstitialAd.Show();
            // SaveLastAdTime은 광고 닫힘 콜백(RegisterEventHandlers)에서 호출
            // → 강종 시 쿨타임 미시작 → 다음 게임에서 다시 광고 표시
        }
        else
        {
            Debug.Log("광고 준비 안됨");
            onComplete?.Invoke(); // 광고 미준비 → 즉시 콜백
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
            // 광고 닫힘 시 다음 광고 로드는 RegisterEventHandlers에서 이미 등록됨
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

#if UNITY_EDITOR
    // [에디터 전용] F8: 광고 쿨타임 리셋. 빌드에선 컴파일 자체에서 제외됨.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            PlayerPrefs.DeleteKey(LastAdTimeKey);
            PlayerPrefs.Save();
            Debug.Log("[AdMob Test] 쿨타임 리셋 완료. 즉시 광고 노출 가능.");
        }
    }
#endif
}
