using System.Collections;
using UnityEngine;

public class IntroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIController uiController;
    [SerializeField] private RectTransform lobbyUIRoot;
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private CanvasGroup tapToStartGroup;

    [Header("인트로 중 숨길 요소")]
    [SerializeField] private GameObject homePanel;      // HomePanel 통째로
    [SerializeField] private GameObject topBar;
    [SerializeField] private GameObject bottomNavBar;
    [SerializeField] private GameObject settingButton;   // 설정 버튼

    [Header("Settings")]
    [SerializeField] private float riseOffset = 80f;
    [SerializeField] private float logoAnimDuration = 1f;
    [SerializeField] private float waitAfterLogo = 1f;
    [SerializeField] private float breathSpeed = 0.5f;
    [SerializeField] private float breathAmount = 0.05f;

    private Vector2 _logoTargetPos;
    private bool _canTouch;

    private void Awake()
    {
        // LobbyUIRoot 표시 (Logo가 안에 있으므로)
        if (lobbyUIRoot != null)
            lobbyUIRoot.anchoredPosition = Vector2.zero;

        // 로고 초기 상태: 아래 + 투명
        _logoTargetPos = logoRect.anchoredPosition;
        logoRect.anchoredPosition = _logoTargetPos + Vector2.down * riseOffset;
        logoGroup.alpha = 0f;

        // TapToStart 초기 상태: 활성 유지 + 투명 (SetActive 리빌드 방지)
        tapToStartGroup.alpha = 0f;
        tapToStartGroup.blocksRaycasts = false;
    }

    private void Start()
    {
        // 인트로 중 숨길 것들 — Start()에서 처리하여
        // topBar 등의 자식 컴포넌트 Awake()가 먼저 실행되도록 보장
        // (Awake에서 SetActive(false) 하면 ProfileGroup.Awake 등이 스킵됨)
        if (homePanel != null) homePanel.SetActive(false);
        if (topBar != null) topBar.SetActive(false);
        if (bottomNavBar != null) bottomNavBar.SetActive(false);
        if (settingButton != null) settingButton.SetActive(false);

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        // 1. 로고 Ease-up (아래 → 원위치, 투명 → 불투명)
        float t = 0f;
        Vector2 startPos = logoRect.anchoredPosition;
        while (t < logoAnimDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / logoAnimDuration);
            logoRect.anchoredPosition = Vector2.Lerp(startPos, _logoTargetPos, p);
            logoGroup.alpha = p;
            yield return null;
        }
        logoRect.anchoredPosition = _logoTargetPos;
        logoGroup.alpha = 1f;

        // 2. 대기
        yield return new WaitForSeconds(waitAfterLogo);

        // 3. TapToStart 페이드인
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            tapToStartGroup.alpha = Mathf.Clamp01(t / 0.4f);
            yield return null;
        }
        tapToStartGroup.alpha = 1f;
        tapToStartGroup.blocksRaycasts = true;

        // 4. 터치 대기 + Breath 루프
        _canTouch = true;
        StartCoroutine(BreathLoop());
    }

    private IEnumerator BreathLoop()
    {
        Transform tf = tapToStartGroup.transform;
        float startTime = Time.time;
        while (_canTouch)
        {
            float elapsed = Time.time - startTime;
            // elapsed=0에서 Sin(0)=0 → scale=1.0부터 부드럽게 시작
            float s = 1f + breathAmount * Mathf.Sin(elapsed * breathSpeed * Mathf.PI * 2f);
            tf.localScale = Vector3.one * s;
            yield return null;
        }
        tf.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!_canTouch) return;

        bool touched = Input.GetMouseButtonDown(0) ||
                       (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        if (!touched) return;

        _canTouch = false;
        StopAllCoroutines();

        // 팝 사운드 재생
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSFX();

        // 인트로 로고 + TapToStart 숨김
        tapToStartGroup.alpha = 0f;
        tapToStartGroup.blocksRaycasts = false;
        logoRect.gameObject.SetActive(false);

        // 설정 버튼 복원
        if (settingButton != null) settingButton.SetActive(true);

        // 로비 전환 (HomePanel의 실제 로고 + 버튼들 표시)
        uiController.TransitionToLobby();

        this.enabled = false;
    }
}
