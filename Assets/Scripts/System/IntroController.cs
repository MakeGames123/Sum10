using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class IntroController : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private UIController uiController;
    [SerializeField] private RectTransform lobbyUIRoot;
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private CanvasGroup tapToStartGroup;
    [SerializeField] private Image image;

    [Header("인트로 중 숨길 요소")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject topBar;
    [SerializeField] private GameObject bottomNavBar;
    [SerializeField] private GameObject settingButton;
    [SerializeField] private GameObject questButton;

    [Header("로비 등장 애니메이션 대상")]
    [SerializeField] private RectTransform topBarRect;
    [SerializeField] private RectTransform bottomNavBarRect;
    [SerializeField] private RectTransform homeLogo;         // HomePanel 안의 Logo
    [SerializeField] private RectTransform startButtonRect;   // 시작 버튼
    [SerializeField] private RectTransform adRemoveButtonRect; // 광고 제거 버튼
    [SerializeField] private RectTransform settingButtonRect;  // 설정 버튼
    [SerializeField] private RectTransform questButtonRect;    // 퀘스트 버튼

    [Header("Intro Settings")]
    [SerializeField] private float riseOffset = 80f;
    [SerializeField] private float logoAnimDuration = 1f;
    [SerializeField] private float waitAfterLogo = 1f;
    [SerializeField] private float breathSpeed = 0.5f;
    [SerializeField] private float breathAmount = 0.05f;

    [Header("로비 등장 애니메이션 설정")]
    [SerializeField] private float lobbyAnimDuration = 0.4f;
    [SerializeField] private float lobbyStagger = 0.06f;

    [Header("시작값 폴리싱")]
    [SerializeField] private float topBarSlideOffset = 250f;
    [SerializeField] private float bottomNavSlideOffset = 250f;
    [SerializeField] private float startButtonSlideOffset = 0f;
    [SerializeField] private float startButtonStartScale = 0f;

    private Vector2 _logoTargetPos;
    private bool _canTouch;

    private void Awake()
    {
        if (lobbyUIRoot != null)
            lobbyUIRoot.anchoredPosition = Vector2.zero;

        _logoTargetPos = logoRect.anchoredPosition;
        logoRect.anchoredPosition = _logoTargetPos + Vector2.down * riseOffset;
        logoGroup.alpha = 0f;

        tapToStartGroup.alpha = 0f;
        tapToStartGroup.blocksRaycasts = false;
    }

    private void Start()
    {
        if (homePanel != null) homePanel.SetActive(false);
        if (topBar != null) topBar.SetActive(false);
        if (bottomNavBar != null) bottomNavBar.SetActive(false);
        if (settingButton != null) settingButton.SetActive(false);
        if (questButton != null) questButton.SetActive(false);

        StartCoroutine(PlayIntro());
    }
    public void OnPointerClick(PointerEventData data) // 인트로 레이캐스트는 닉네임 패널에서 관리
    {
        if (!_canTouch) return;

        _canTouch = false;
        StopAllCoroutines();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSFX();

        tapToStartGroup.alpha = 0f;
        tapToStartGroup.blocksRaycasts = false;
        logoRect.gameObject.SetActive(false);

        StartCoroutine(PlayLobbyEntrance());

        this.enabled = false;
        image.enabled = false;
    }
    private IEnumerator PlayIntro()
    {
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

        yield return new WaitForSeconds(waitAfterLogo);

        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            tapToStartGroup.alpha = Mathf.Clamp01(t / 0.4f);
            yield return null;
        }
        tapToStartGroup.alpha = 1f;
        tapToStartGroup.blocksRaycasts = true;

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
            float s = 1f + breathAmount * Mathf.Sin(elapsed * breathSpeed * Mathf.PI * 2f);
            tf.localScale = Vector3.one * s;
            yield return null;
        }
        tf.localScale = Vector3.one;
    }
    private Vector2 _topBarOrig;
    private Vector2 _bottomNavOrig;
    private Vector2 _startButtonOrig;

    private IEnumerator PlayLobbyEntrance()
    {
        if (topBarRect != null) _topBarOrig = topBarRect.anchoredPosition;
        if (bottomNavBarRect != null) _bottomNavOrig = bottomNavBarRect.anchoredPosition;
        if (startButtonRect != null) _startButtonOrig = startButtonRect.anchoredPosition;

        // 위치: 시작 오프셋 세팅
        if (topBarRect != null)
            topBarRect.anchoredPosition = _topBarOrig + Vector2.up * topBarSlideOffset;
        if (startButtonRect != null)
            startButtonRect.anchoredPosition = _startButtonOrig + Vector2.down * startButtonSlideOffset;
        if (bottomNavBarRect != null)
            bottomNavBarRect.anchoredPosition = _bottomNavOrig + Vector2.down * bottomNavSlideOffset;

        // 설정 버튼, 퀘스트 버튼: 활성화
        if (settingButton != null) settingButton.SetActive(true);
        if (questButton != null) questButton.SetActive(true);
        if (homeLogo != null) homeLogo.localScale = Vector3.zero;
        if (startButtonRect != null) startButtonRect.localScale = Vector3.zero;
        if (adRemoveButtonRect != null) adRemoveButtonRect.localScale = Vector3.zero;
        if (questButtonRect != null) questButtonRect.localScale = Vector3.zero;

        uiController.TransitionToLobby();

        // 1프레임 대기 — AspectSafeScaler.Start()가 localScale을 최종값으로 세팅
        yield return null;

        // AspectSafeScaler가 있는 요소는 최종 스케일이 세팅됨, 없으면 0 그대로이므로 1로 대체
        Vector3 logoScale = homeLogo != null && homeLogo.localScale != Vector3.zero ? homeLogo.localScale : Vector3.one;
        Vector3 startBtnScale = startButtonRect != null && startButtonRect.localScale != Vector3.zero ? startButtonRect.localScale : Vector3.one;
        Vector3 adRemoveScale = adRemoveButtonRect != null && adRemoveButtonRect.localScale != Vector3.zero ? adRemoveButtonRect.localScale : Vector3.one;
        Vector3 questScale = questButtonRect != null && questButtonRect.localScale != Vector3.zero ? questButtonRect.localScale : Vector3.one;

        if (homeLogo != null) homeLogo.localScale = Vector3.zero;
        if (startButtonRect != null) startButtonRect.localScale = Vector3.zero;
        if (adRemoveButtonRect != null) adRemoveButtonRect.localScale = Vector3.zero;
        if (questButtonRect != null) questButtonRect.localScale = Vector3.zero;

        float dur = lobbyAnimDuration;
        WaitForSeconds wait = new WaitForSeconds(lobbyStagger);

        // 1. TopBar
        if (topBarRect != null)
            topBarRect.DOAnchorPos(_topBarOrig, dur).SetEase(Ease.OutBack);
        yield return wait;

        yield return wait;

        // 2. 홈 로고
        if (homeLogo != null)
            homeLogo.DOScale(logoScale, dur).SetEase(Ease.OutBack);
        yield return wait;

        // 4. 시작 버튼
        if (startButtonRect != null)
        {
            startButtonRect.DOAnchorPos(_startButtonOrig, dur).SetEase(Ease.OutBack);
            startButtonRect.DOScale(startBtnScale, dur).SetEase(Ease.OutBack);
        }
        yield return wait;

        // 5. 광고 제거 버튼
        if (adRemoveButtonRect != null)
            adRemoveButtonRect.DOScale(adRemoveScale, dur).SetEase(Ease.OutBack);
        yield return wait;

        // 5-1. 퀘스트 버튼
        if (questButtonRect != null)
            questButtonRect.DOScale(questScale, dur).SetEase(Ease.OutBack);
        yield return wait;

        // 6. BottomNavBar
        if (bottomNavBarRect != null)
            bottomNavBarRect.DOAnchorPos(_bottomNavOrig, dur).SetEase(Ease.OutBack);
    }
}
