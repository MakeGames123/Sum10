using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    [Header("References")]
    [SerializeField] private Image cellImage;
    [SerializeField] private Image cellBackground;
    [SerializeField] private TextMeshProUGUI numberText;

    private CellSprite normalSprite;
    private CellSprite blankSprite;

    [Header("Select Animation Settings")]
    [SerializeField] private float selectAnimDuration = 0.25f;   // 탄력 효과 보이게
    [SerializeField] private float selectScalePunch = 1.12f;     // 처음 커지는 정도
    [SerializeField] private float selectJumpHeight = 8f;        // 위로 점프 높이
    [SerializeField] private float selectRotation = 6f;          // 살짝 기울어지는 각도

    [Header("Flip Select Animation Settings (B)")]
    [SerializeField] private float flipDuration = 0.25f;         // 뒤집기 전체 시간 (1.2배 빠르게)
    [SerializeField] private Color flipSelectedColor = new Color(1f, 0.8f, 0.3f, 1f);  // 선택 시 색상 (주황/노란색)

    [Header("Hint Animation Settings")]
    [SerializeField] private float hintBounceDuration = 0.2f;    // 한 번 바운스 시간
    [SerializeField] private float hintScalePunch = 1.15f;       // 커지는 정도
    [SerializeField] private float hintJumpHeight = 12f;         // 위로 점프 높이
    [SerializeField] private int hintBounceCount = 3;            // 연속 바운스 횟수
    [SerializeField] private float hintPauseDuration = 1.2f;     // 바운스 세트 사이 멈춤 시간

    [Header("Tile Effect Settings")]
    [SerializeField] private float disappearDuration = 0.18f;       // 사라짐 전체 시간
    [SerializeField] private float disappearScalePeak = 1.2f;       // 사라질 때 처음 커지는 정도
    [SerializeField] private float disappearJumpHeight = 25f;       // 위로 점프 높이 (px)
    [SerializeField] private float appearDelay = 0.1f;              // 젤리발 등장 딜레이
    [SerializeField] private float appearDuration = 0.15f;          // 젤리발 등장 시간
    [SerializeField] private float appearScalePeak = 1.1f;          // 등장 시 톡 튀어나오는 정도

    [SerializeField] private ParticleSystem particle;                   // 파티클 스프라이트

    [Header("Spawn Animation Settings")]
    [SerializeField] private float spawnDuration = 0.25f;           // 생성 애니메이션 시간
    [SerializeField] private float spawnScalePeak = 1.15f;          // 생성 시 톡 튀어나오는 정도
    [SerializeField] private float spawnDropHeight = 30f;           // 위에서 떨어지는 높이

    private int value;          // -1 = 공백, 1~k = 숫자

    // 파괴 대기 중 플래그 (매칭 성공 후 애니메이션 중 재선택 방지)
    private bool isPendingDestruction = false;

    public bool HasNumber => value > 0 && !isPendingDestruction;
    public bool IsPendingDestruction => isPendingDestruction;
    public int Value => value;

    private bool isSelected = false;
    private bool isHint = false;
    private bool wasSelected = false;  // Flip 모드 선택 해제 애니메이션용

    public bool IsSelected => isSelected;
    public bool IsHint => isHint;

    // 힌트 사운드 리더 (여러 힌트 셀 중 하나만 사운드 재생)
    private bool isHintSoundLeader = false;

    // 애니메이션 관련
    private Tween selectTween;
    private Sequence hintTween;
    private Vector3 selectOriginalPos;
    private Vector3 textOriginalPos;
    
    // 캐싱
    private static Transform cachedCanvasTransform;

    // Ghost 애니메이션 관리용 ID
    private const string GHOST_TWEEN_ID = "DisappearGhost";

    // ========== AB 테스트용 (현재 Pop 사용) ==========
    public enum DisappearMode
    {
        Pop,        // 팡팡 터지는 연출 (기본값)
        Ghost       // 빨려들어가는 연출 (미사용)
    }
    public static DisappearMode CurrentDisappearMode = DisappearMode.Pop;

    // ========== AB 테스트용: 선택 애니메이션 ==========
    public enum SelectMode
    {
        Bounce,     // 기존: 점프 + 기울어짐 (A)
        Flip        // 새로운: 뒤집기 + 색상 변경 (B)
    }
    public static SelectMode CurrentSelectMode = SelectMode.Flip;  // B 모드 기본값

    /// <summary>
    /// 모든 Ghost 애니메이션을 즉시 중단하고 제거 (게임 종료 시 호출)
    /// </summary>
    public static void KillAllGhostAnimations()
    {
        DOTween.Kill(GHOST_TWEEN_ID);
    }

    /// <summary>
    /// 모든 활성 파티클 제거 (리플레이 시 호출)
    /// </summary>
    public static void DestroyAllActiveParticles()
    {
        var particles = FindObjectsOfType<ParticleSystem>();
        foreach (var p in particles)
        {
            if (p != null && p.gameObject.name.Contains("(Clone)"))
            {
                Destroy(p.gameObject);
            }
        }
    }

    public void Init(int x, int y, int initialValue)
    {
        this.X = x;
        this.Y = y;

        // 기존 애니메이션 정리 및 위치/상태 초기화
        KillTileEffectTweens();
        cellImage.transform.localPosition = Vector3.zero;
        cellImage.transform.localRotation = Quaternion.identity;
        if (numberText != null)
        {
            numberText.alpha = 1f;
            numberText.transform.localScale = Vector3.one;
            numberText.transform.localRotation = Quaternion.identity;
        }

        normalSprite = ThemeManager.Instance.selectedTheme.normalSpriteSets[Random.Range(0, ThemeManager.Instance.selectedTheme.normalSpriteSets.Count)];
        blankSprite = ThemeManager.Instance.selectedTheme.blankSpriteSets[Random.Range(0, ThemeManager.Instance.selectedTheme.blankSpriteSets.Count)];
        numberText.transform.localPosition = new Vector2(0, ThemeManager.Instance.selectedTheme.textOffset);
        cellImage.transform.localScale = new Vector2(ThemeManager.Instance.selectedTheme.cellScale, ThemeManager.Instance.selectedTheme.cellScale);
        cellBackground.transform.localScale = new Vector2(ThemeManager.Instance.selectedTheme.backgroundScale, ThemeManager.Instance.selectedTheme.backgroundScale);

        // 원래 위치 저장
        selectOriginalPos = cellImage.transform.localPosition;
        textOriginalPos = numberText.transform.localPosition;

        SetValue(initialValue);
    }

    public void SetValue(int newValue)
    {
        value = newValue;
        isPendingDestruction = false;  // 새 값 설정 시 파괴 대기 해제

        if (value > 0 && !isPendingDestruction)
        {
            numberText.text = value.ToString();
            numberText.enabled = true;
        }
        else
        {
            numberText.text = "";
            numberText.enabled = false;
        }

        isSelected = false;
        isHint = false;
        UpdateVisualState();
    }

    /// <summary>
    /// 파괴 대기 상태 설정 (매칭 성공 후 즉시 호출하여 재선택 방지)
    /// </summary>
    public void SetPendingDestruction(bool pending)
    {
        isPendingDestruction = pending;
    }

    // 기존 SetHighlight → 선택 하이라이트로 사용
    public void SetHighlight(bool on)
    {
        SetSelectionHighlight(on);
    }

    public void SetSelectionHighlight(bool on)
    {
        isSelected = on;
        UpdateVisualState();
    }

    public void SetHintHighlight(bool on)
    {
        isHint = on;
        UpdateVisualState();
    }

    public void SetHint(bool on)
    {
        SetHintHighlight(on);
    }

    /// <summary>
    /// 힌트 애니메이션을 강제로 재시작 (싱크 맞추기용)
    /// </summary>
    public void ForceRestartHintAnimation()
    {
        if (isHint && !isSelected)
        {
            StopHintAnimation();
            PlayHintAnimation(playSound: true);
        }
    }

    /// <summary>
    /// 힌트 사운드 리더 설정 (이 셀만 힌트 효과음 재생)
    /// </summary>
    public void SetHintSoundLeader(bool isLeader)
    {
        isHintSoundLeader = isLeader;
    }

    private void UpdateVisualState()
    {
        if (ThemeManager.Instance.selectedTheme.cellBackground != null)
        {
            cellBackground.sprite = ThemeManager.Instance.selectedTheme.cellBackground;
            cellBackground.enabled = true;
        }
        else cellBackground.enabled = false;
        if (cellImage != null)
        {
            Sprite targetSprite = GetCurrentSprite();
            if (targetSprite != null)
                cellImage.sprite = targetSprite;

            cellImage.color = Color.white; // 스프라이트 자체 색상 유지
        }

        // 폰트 색상 적용
        if (numberText != null && numberText.enabled)
        {
            var theme = ThemeManager.Instance.selectedTheme;
            numberText.color = isSelected ? theme.selectedFontColor : theme.normalFontColor;
        }

        // 애니메이션 처리
        if (isSelected)
        {
            wasSelected = true;
            StopHintAnimation();
            PlaySelectAnimation();
        }
        else if (isHint)
        {
            // 선택 → 힌트 전환 시에는 사운드 스킵
            bool wasJustDeselected = wasSelected;
            wasSelected = false;
            StopSelectAnimation();
            PlayHintAnimation(playSound: !wasJustDeselected);
        }
        else
        {
            // Flip 모드에서 선택 해제 시 애니메이션 재생
            if (wasSelected && CurrentSelectMode == SelectMode.Flip)
            {
                wasSelected = false;
                StopHintAnimation();
                PlayDeselectAnimationFlip();
            }
            else
            {
                wasSelected = false;
                StopAllCellAnimations();
                transform.localScale = Vector3.one;
                transform.localRotation = Quaternion.identity;
                // 셀 이미지 위치/회전/스케일 복원
                float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
                cellImage.transform.localScale = Vector3.one * themeScale;
                cellImage.transform.localRotation = Quaternion.identity;
                cellImage.transform.localPosition = selectOriginalPos;
                // 숫자 위치도 복원
                if (numberText != null)
                {
                    numberText.transform.localPosition = textOriginalPos;
                }
            }
        }
    }

    private void PlaySelectAnimation()
    {
        StopSelectAnimation();
        if (!gameObject.activeSelf) return;

        // AB 테스트용 분기 (T키로 전환)
        if (CurrentSelectMode == SelectMode.Flip)
        {
            PlaySelectAnimationFlip();
        }
        else
        {
            PlaySelectAnimationBounce();
        }
    }

    // A 모드: 기존 점프 + 기울어짐
    private void PlaySelectAnimationBounce()
    {
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;

        // 랜덤 방향으로 살짝 기울어짐 (-1 or 1)
        float rotDir = Random.value > 0.5f ? 1f : -1f;

        Sequence seq = DOTween.Sequence();

        // Phase 1: 위로 점프 + 커지면서 + 살짝 기울어짐 (40%) - 빠르게 튀어오름
        seq.Append(
            cellImage.transform.DOScale(selectScalePunch * themeScale, selectAnimDuration * 0.4f)
                .SetEase(Ease.OutBack, 2f)
        );
        seq.Join(
            cellImage.transform.DOLocalMoveY(selectOriginalPos.y + selectJumpHeight, selectAnimDuration * 0.4f)
                .SetEase(Ease.OutBack, 1.5f)
        );
        seq.Join(
            cellImage.transform.DOLocalRotate(new Vector3(0, 0, selectRotation * rotDir), selectAnimDuration * 0.4f)
                .SetEase(Ease.OutBack)
        );
        // 숫자도 같이 점프
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOLocalMoveY(textOriginalPos.y + selectJumpHeight, selectAnimDuration * 0.4f)
                    .SetEase(Ease.OutBack, 1.5f)
            );
        }

        // Phase 2: 안정화 - 탄력있게 착지 (60%)
        seq.Append(
            cellImage.transform.DOScale(themeScale, selectAnimDuration * 0.6f)
                .SetEase(Ease.OutBounce)
        );
        seq.Join(
            cellImage.transform.DOLocalMoveY(selectOriginalPos.y, selectAnimDuration * 0.6f)
                .SetEase(Ease.OutBounce)
        );
        seq.Join(
            cellImage.transform.DOLocalRotate(Vector3.zero, selectAnimDuration * 0.6f)
                .SetEase(Ease.OutBack)
        );
        // 숫자도 같이 내려옴
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOLocalMoveY(textOriginalPos.y, selectAnimDuration * 0.6f)
                    .SetEase(Ease.OutBounce)
            );
        }

        selectTween = seq;
    }

    // B 모드: 뒤집기 + 바운스 + 기울어짐
    private void PlaySelectAnimationFlip()
    {
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
        float rotDir = Random.value > 0.5f ? 1f : -1f;

        Sequence seq = DOTween.Sequence();

        // Phase 1: 위로 점프 + 기울어짐 + 뒤집기 시작 (ScaleX 0으로)
        seq.Append(
            cellImage.transform.DOScaleX(0f, flipDuration * 0.35f)
                .SetEase(Ease.InQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalMoveY(selectOriginalPos.y + selectJumpHeight, flipDuration * 0.35f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalRotate(new Vector3(0, 0, selectRotation * rotDir), flipDuration * 0.35f)
                .SetEase(Ease.OutQuad)
        );
        // 숫자도 같이 점프 + flip + 기울어짐
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(0f, flipDuration * 0.35f)
                    .SetEase(Ease.InQuad)
            );
            seq.Join(
                numberText.transform.DOLocalMoveY(textOriginalPos.y + selectJumpHeight, flipDuration * 0.35f)
                    .SetEase(Ease.OutQuad)
            );
            seq.Join(
                numberText.transform.DOLocalRotate(new Vector3(0, 0, selectRotation * rotDir), flipDuration * 0.35f)
                    .SetEase(Ease.OutQuad)
            );
        }

        // Phase 2: 뒤집기 완료 + 살짝 커짐
        seq.Append(
            cellImage.transform.DOScaleX(selectScalePunch * themeScale, flipDuration * 0.35f)
                .SetEase(Ease.OutBack)
        );
        // 숫자도 뒤집기 완료 + 살짝 커짐
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(selectScalePunch, flipDuration * 0.35f)
                    .SetEase(Ease.OutBack)
            );
        }

        // Phase 3: 탄력있게 착지 + 원래 크기 + 회전 복원
        seq.Append(
            cellImage.transform.DOScaleX(themeScale, flipDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalMoveY(selectOriginalPos.y, flipDuration * 0.3f)
                .SetEase(Ease.OutBounce)
        );
        seq.Join(
            cellImage.transform.DOLocalRotate(Vector3.zero, flipDuration * 0.3f)
                .SetEase(Ease.OutBack)
        );
        // 숫자도 같이 내려옴 + 원래 크기 + 회전 복원
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(1f, flipDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );
            seq.Join(
                numberText.transform.DOLocalMoveY(textOriginalPos.y, flipDuration * 0.3f)
                    .SetEase(Ease.OutBounce)
            );
            seq.Join(
                numberText.transform.DOLocalRotate(Vector3.zero, flipDuration * 0.3f)
                    .SetEase(Ease.OutBack)
            );
        }

        selectTween = seq;
    }

    // B 모드: 캔슬 시 역방향 뒤집기 애니메이션
    private void PlayDeselectAnimationFlip()
    {
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
        float rotDir = Random.value > 0.5f ? 1f : -1f;

        Sequence seq = DOTween.Sequence();

        // Phase 1: 위로 점프 + 기울어짐 + 뒤집기 시작 (ScaleX 0으로)
        seq.Append(
            cellImage.transform.DOScaleX(0f, flipDuration * 0.35f)
                .SetEase(Ease.InQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalMoveY(selectOriginalPos.y + selectJumpHeight, flipDuration * 0.35f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalRotate(new Vector3(0, 0, selectRotation * rotDir), flipDuration * 0.35f)
                .SetEase(Ease.OutQuad)
        );
        // 숫자도 같이 점프 + flip + 기울어짐
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(0f, flipDuration * 0.35f)
                    .SetEase(Ease.InQuad)
            );
            seq.Join(
                numberText.transform.DOLocalMoveY(textOriginalPos.y + selectJumpHeight, flipDuration * 0.35f)
                    .SetEase(Ease.OutQuad)
            );
            seq.Join(
                numberText.transform.DOLocalRotate(new Vector3(0, 0, selectRotation * rotDir), flipDuration * 0.35f)
                    .SetEase(Ease.OutQuad)
            );
        }

        // Phase 2: 뒤집기 완료 + 살짝 커짐
        seq.Append(
            cellImage.transform.DOScaleX(selectScalePunch * themeScale, flipDuration * 0.35f)
                .SetEase(Ease.OutBack)
        );
        // 숫자도 뒤집기 완료 + 살짝 커짐
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(selectScalePunch, flipDuration * 0.35f)
                    .SetEase(Ease.OutBack)
            );
        }

        // Phase 3: 탄력있게 착지 + 원래 크기 + 회전 복원
        seq.Append(
            cellImage.transform.DOScaleX(themeScale, flipDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalMoveY(selectOriginalPos.y, flipDuration * 0.3f)
                .SetEase(Ease.OutBounce)
        );
        seq.Join(
            cellImage.transform.DOLocalRotate(Vector3.zero, flipDuration * 0.3f)
                .SetEase(Ease.OutBack)
        );
        // 숫자도 같이 내려옴 + 원래 크기 + 회전 복원
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(1f, flipDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );
            seq.Join(
                numberText.transform.DOLocalMoveY(textOriginalPos.y, flipDuration * 0.3f)
                    .SetEase(Ease.OutBounce)
            );
            seq.Join(
                numberText.transform.DOLocalRotate(Vector3.zero, flipDuration * 0.3f)
                    .SetEase(Ease.OutBack)
            );
        }

        selectTween = seq;
    }

    private void StopSelectAnimation()
    {
        if (selectTween != null)
        {
            selectTween.Kill();
            selectTween = null;

            // 즉시 원위치로 복원
            float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
            cellImage.transform.localPosition = selectOriginalPos;
            cellImage.transform.localScale = Vector3.one * themeScale;
            cellImage.transform.localRotation = Quaternion.identity;
            cellImage.color = Color.white;  // 색상 복원 (Flip 모드용)
            if (numberText != null)
            {
                numberText.transform.localPosition = textOriginalPos;
                numberText.transform.localScale = Vector3.one;
                numberText.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void PlayHintAnimation(bool playSound = true)
    {
        StopHintAnimation();
        if (!gameObject.activeSelf) return;

        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;

        // 메인 시퀀스 (무한 루프)
        hintTween = DOTween.Sequence();

        // 모든 힌트 셀에 콜백 추가, 실제 재생은 동적으로 결정
        if (playSound)
        {
            hintTween.AppendCallback(() =>
            {
                // 동적 체크: 현재 활성 힌트 셀 중 첫 번째인 경우에만 재생
                if (AudioManager.Instance != null && board != null && board.IsFirstActiveHintCell(this))
                    AudioManager.Instance.PlayHintSFX();
            });
        }

        // 연속 바운스 (hintBounceCount번)
        for (int i = 0; i < hintBounceCount; i++)
        {
            float bounceScale = hintScalePunch - (i * 0.03f); // 점점 작아지는 바운스
            float bounceHeight = hintJumpHeight - (i * 2f);   // 점점 낮아지는 점프

            // 위로 올라가면서 커지기
            hintTween.Append(
                cellImage.transform.DOScale(bounceScale * themeScale, hintBounceDuration * 0.4f)
                    .SetEase(Ease.OutQuad)
            );
            hintTween.Join(
                cellImage.transform.DOLocalMoveY(selectOriginalPos.y + bounceHeight, hintBounceDuration * 0.4f)
                    .SetEase(Ease.OutQuad)
            );
            // 숫자도 같이
            if (numberText != null && numberText.enabled)
            {
                hintTween.Join(
                    numberText.transform.DOLocalMoveY(textOriginalPos.y + bounceHeight, hintBounceDuration * 0.4f)
                        .SetEase(Ease.OutQuad)
                );
            }

            // 내려오면서 원래 크기로
            hintTween.Append(
                cellImage.transform.DOScale(themeScale, hintBounceDuration * 0.6f)
                    .SetEase(Ease.OutBounce)
            );
            hintTween.Join(
                cellImage.transform.DOLocalMoveY(selectOriginalPos.y, hintBounceDuration * 0.6f)
                    .SetEase(Ease.OutBounce)
            );
            // 숫자도 같이
            if (numberText != null && numberText.enabled)
            {
                hintTween.Join(
                    numberText.transform.DOLocalMoveY(textOriginalPos.y, hintBounceDuration * 0.6f)
                        .SetEase(Ease.OutBounce)
                );
            }
        }

        // 멈춤
        hintTween.AppendInterval(hintPauseDuration);

        // 무한 루프
        hintTween.SetLoops(-1, LoopType.Restart);
    }

    private void StopHintAnimation()
    {
        if (hintTween != null)
        {
            hintTween.Kill();
            hintTween = null;

            // 위치 복원
            float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
            cellImage.transform.localPosition = selectOriginalPos;
            cellImage.transform.localScale = Vector3.one * themeScale;
            if (numberText != null)
            {
                numberText.transform.localPosition = textOriginalPos;
            }
        }
    }

    private void StopAllCellAnimations()
    {
        StopSelectAnimation();
        StopHintAnimation();
    }

    private Sprite GetCurrentSprite()
    {
        bool isBlankCell = value <= 0;

        if (isBlankCell)
        {
            // 숫자 셀
            if (isSelected && blankSprite.selectedSprite != null)
                return blankSprite.selectedSprite;
            if (isHint && blankSprite.hintSprite != null)
                return blankSprite.hintSprite;
            return blankSprite.normalSprite;
        }
        else
        {
            // 숫자 셀
            if (isSelected && normalSprite.selectedSprite != null)
                return normalSprite.selectedSprite;
            if (isHint && normalSprite.hintSprite != null)
                return normalSprite.hintSprite;
            return normalSprite.normalSprite;
        }
    }
    // =========================
    //  타일 이펙트 애니메이션
    // =========================

    private Tween currentDisappearTween;
    private Tween currentAppearTween;

    /// <summary>
    /// 고양이 셀이 사라지고 젤리발로 변하는 애니메이션 (async/await)
    /// </summary>
    public async Task PlayDisappearAndTransformAsync()
    {
        KillTileEffectTweens();

        var tcs = new TaskCompletionSource<bool>();
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
        Vector3 originalPos = cellImage.transform.localPosition;
        Vector3 originalTextPos = numberText.transform.localPosition;

        // 사라짐 시퀀스: Scale 1 → 1.2 → 0, 위로 점프
        Sequence disappearSeq = DOTween.Sequence();

        // Phase 1: 살짝 커지면서 위로 점프 시작 (전체 시간의 30%)
        disappearSeq.Append(
            cellImage.transform.DOScale(disappearScalePeak * themeScale, disappearDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        disappearSeq.Join(
            cellImage.transform.DOLocalMoveY(originalPos.y + disappearJumpHeight, disappearDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        // 숫자도 같이 점프
        if (numberText != null && numberText.enabled)
        {
            disappearSeq.Join(
                numberText.transform.DOLocalMoveY(originalTextPos.y + disappearJumpHeight, disappearDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );
        }

        // Phase 2: 스케일 0으로 줄어들면서 사라짐 + 파티클 생성 (전체 시간의 70%)
        disappearSeq.AppendCallback(() => SpawnKillParticles());
        disappearSeq.Append(
            cellImage.transform.DOScale(0f, disappearDuration * 0.7f)
                .SetEase(Ease.InBack)
        );
        disappearSeq.Join(
            cellImage.transform.DOLocalMoveY(originalPos.y + disappearJumpHeight * 0.5f, disappearDuration * 0.7f)
                .SetEase(Ease.InQuad)
        );
        // 숫자도 같이 내려오면서 페이드아웃
        if (numberText != null && numberText.enabled)
        {
            disappearSeq.Join(
                numberText.transform.DOLocalMoveY(originalTextPos.y + disappearJumpHeight * 0.5f, disappearDuration * 0.7f)
                    .SetEase(Ease.InQuad)
            );
            disappearSeq.Join(
                numberText.DOFade(0f, disappearDuration * 0.5f)
            );
        }

        currentDisappearTween = disappearSeq;

        disappearSeq.OnComplete(() =>
        {
            // 값 변경 (공백셀로)
            value = -1;
            isPendingDestruction = false;  // 파괴 완료 - 공백셀 선택 가능하도록 리셋
            numberText.text = "";
            numberText.enabled = false;
            numberText.alpha = 1f; // 알파값 복원

            // 위치 복원
            cellImage.transform.localPosition = originalPos;
            numberText.transform.localPosition = originalTextPos;

            // 젤리발 스프라이트로 변경
            blankSprite = ThemeManager.Instance.selectedTheme.blankSpriteSets[Random.Range(0, ThemeManager.Instance.selectedTheme.blankSpriteSets.Count)];
            cellImage.sprite = blankSprite.normalSprite;

            // 젤리발 등장 애니메이션
            PlayAppearAnimation(() => tcs.TrySetResult(true));
        });

        await tcs.Task;
    }

    /// <summary>
    /// 젤리발(공백셀) 등장 애니메이션
    /// </summary>
    private void PlayAppearAnimation(System.Action onComplete = null)
    {
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;

        // 시작 스케일 0
        cellImage.transform.localScale = Vector3.zero;

        Sequence appearSeq = DOTween.Sequence();

        // 딜레이
        appearSeq.AppendInterval(appearDelay);

        // Scale 0 → 1.1 → 1 (톡 튀어나오는 느낌)
        appearSeq.Append(
            cellImage.transform.DOScale(appearScalePeak * themeScale, appearDuration * 0.6f)
                .SetEase(Ease.OutBack)
        );
        appearSeq.Append(
            cellImage.transform.DOScale(themeScale, appearDuration * 0.4f)
                .SetEase(Ease.InOutQuad)
        );

        currentAppearTween = appearSeq;

        appearSeq.OnComplete(() =>
        {
            isSelected = false;
            isHint = false;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 사라짐 애니메이션 (AB 테스트: Ghost vs Pop 모드)
    /// </summary>
    public void PlayDisappearAndTransform(System.Action onComplete)
    {
        if (CurrentDisappearMode == DisappearMode.Pop)
        {
            PlayDisappearPop(onComplete);
        }
        else
        {
            PlayDisappearGhost(onComplete);
        }
    }

    /// <summary>
    /// Ghost 모드: 빨려들어가는 연출
    /// </summary>
    private void PlayDisappearGhost(System.Action onComplete)
    {
        // 파티클 생성
        SpawnKillParticles();

        // 고스트 애니메이션 생성 (독립적으로 날아감)
        SpawnDisappearGhost();

        // 즉시 공백 셀로 변경 (애니메이션과 분리)
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
        value = -1;
        isPendingDestruction = false;  // 파괴 완료 - 공백셀 선택 가능하도록 리셋
        numberText.text = "";
        numberText.enabled = false;
        numberText.alpha = 1f;

        // 젤리발 스프라이트로 변경
        blankSprite = ThemeManager.Instance.selectedTheme.blankSpriteSets[Random.Range(0, ThemeManager.Instance.selectedTheme.blankSpriteSets.Count)];
        cellImage.sprite = blankSprite.normalSprite;
        cellImage.transform.localScale = Vector3.one * themeScale;

        // 젤리발 등장 애니메이션
        PlayAppearAnimation(onComplete);
    }

    /// <summary>
    /// Pop 모드: 팡팡 터지는 연출 (제자리에서 커졌다 사라짐)
    /// </summary>
    private void PlayDisappearPop(System.Action onComplete)
    {
        KillTileEffectTweens();

        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
        Vector3 originalPos = cellImage.transform.localPosition;
        Vector3 originalTextPos = numberText.transform.localPosition;

        Sequence disappearSeq = DOTween.Sequence();

        // Phase 1: 살짝 커지면서 위로 점프
        disappearSeq.Append(
            cellImage.transform.DOScale(disappearScalePeak * themeScale, disappearDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        disappearSeq.Join(
            cellImage.transform.DOLocalMoveY(originalPos.y + disappearJumpHeight, disappearDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        if (numberText != null && numberText.enabled)
        {
            disappearSeq.Join(
                numberText.transform.DOLocalMoveY(originalTextPos.y + disappearJumpHeight, disappearDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );
        }

        // Phase 2: 파티클 + 스케일 0으로 사라짐
        disappearSeq.AppendCallback(() => SpawnKillParticles());
        disappearSeq.Append(
            cellImage.transform.DOScale(0f, disappearDuration * 0.7f)
                .SetEase(Ease.InBack)
        );
        disappearSeq.Join(
            cellImage.transform.DOLocalMoveY(originalPos.y + disappearJumpHeight * 0.5f, disappearDuration * 0.7f)
                .SetEase(Ease.InQuad)
        );
        if (numberText != null && numberText.enabled)
        {
            disappearSeq.Join(
                numberText.transform.DOLocalMoveY(originalTextPos.y + disappearJumpHeight * 0.5f, disappearDuration * 0.7f)
                    .SetEase(Ease.InQuad)
            );
            disappearSeq.Join(
                numberText.DOFade(0f, disappearDuration * 0.5f)
            );
        }

        currentDisappearTween = disappearSeq;

        disappearSeq.OnComplete(() =>
        {
            // 값 변경 (공백셀로)
            value = -1;
            isPendingDestruction = false;  // 파괴 완료 - 공백셀 선택 가능하도록 리셋
            numberText.text = "";
            numberText.enabled = false;
            numberText.alpha = 1f;

            // 위치 복원
            cellImage.transform.localPosition = originalPos;
            numberText.transform.localPosition = originalTextPos;

            // 젤리발 스프라이트로 변경
            blankSprite = ThemeManager.Instance.selectedTheme.blankSpriteSets[Random.Range(0, ThemeManager.Instance.selectedTheme.blankSpriteSets.Count)];
            cellImage.sprite = blankSprite.normalSprite;

            // 젤리발 등장 애니메이션
            PlayAppearAnimation(onComplete);
        });
    }

    /// <summary>
    /// 사라지는 고스트 복사본 생성 (독립 애니메이션)
    /// </summary>
    private void SpawnDisappearGhost()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // 목표 지점: 절대 위치 (0, 800)를 월드 좌표로 변환
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 targetAnchoredPos = new Vector2(0, 800);
        Vector2 canvasSize = canvasRect.sizeDelta;

        Vector2 normalizedPos = new Vector2(
            targetAnchoredPos.x / canvasSize.x + 0.5f,
            targetAnchoredPos.y / canvasSize.y + 0.5f
        );
        Vector3 targetWorldPos = canvasRect.TransformPoint(new Vector3(
            (normalizedPos.x - canvasRect.pivot.x) * canvasSize.x,
            (normalizedPos.y - canvasRect.pivot.y) * canvasSize.y,
            0
        ));

        // 고스트 오브젝트 생성
        GameObject ghostObj = new GameObject("DisappearGhost");
        ghostObj.transform.SetParent(canvas.transform, false);
        ghostObj.transform.SetAsLastSibling();

        // 셀 이미지 복사
        Image ghostImage = ghostObj.AddComponent<Image>();
        ghostImage.sprite = cellImage.sprite;
        ghostImage.raycastTarget = false;

        RectTransform cellRt = (RectTransform)cellImage.transform;
        RectTransform ghostRt = ghostObj.GetComponent<RectTransform>();
        ghostRt.position = cellImage.transform.position;
        ghostRt.sizeDelta = cellRt.rect.size;
        ghostRt.localScale = cellImage.transform.lossyScale;

        // 숫자 텍스트도 복사 (고스트의 자식으로)
        if (numberText != null && numberText.enabled && !string.IsNullOrEmpty(numberText.text))
        {
            GameObject numberGhost = new GameObject("NumberGhost");
            numberGhost.transform.SetParent(ghostObj.transform, false);

            var textGhost = numberGhost.AddComponent<TextMeshProUGUI>();
            textGhost.text = numberText.text;
            textGhost.font = numberText.font;
            textGhost.fontSize = numberText.fontSize;
            textGhost.alignment = numberText.alignment;
            textGhost.color = numberText.color;
            textGhost.raycastTarget = false;

            RectTransform textRt = numberGhost.GetComponent<RectTransform>();
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = ((RectTransform)numberText.transform).rect.size;
            textRt.localScale = Vector3.one;
        }

        // 애니메이션
        float flyDuration = 0.35f;
        Vector3 originalScale = ghostRt.localScale;

        Sequence ghostSeq = DOTween.Sequence();
        ghostSeq.SetId(GHOST_TWEEN_ID);

        ghostSeq.Append(ghostRt.DOScale(originalScale * 1.15f, 0.08f).SetEase(Ease.OutQuad));
        ghostSeq.Append(ghostRt.DOScale(originalScale * 0.35f, flyDuration - 0.08f).SetEase(Ease.InQuad));
        ghostSeq.Insert(0f, ghostRt.DOMove(targetWorldPos, flyDuration).SetEase(Ease.InQuad));
        ghostSeq.Insert(0f, ghostRt.DORotate(new Vector3(0, 0, 360f), flyDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        ghostSeq.Insert(flyDuration * 0.3f, ghostImage.DOFade(0f, flyDuration * 0.7f));

        ghostSeq.OnKill(() => { if (ghostObj != null) Destroy(ghostObj); });
        ghostSeq.OnComplete(() => { if (ghostObj != null) Destroy(ghostObj); });
    }

    private void KillTileEffectTweens()
    {
        bool needRestore = selectTween != null || hintTween != null;

        selectTween?.Kill();
        hintTween?.Kill();
        currentDisappearTween?.Kill();
        currentAppearTween?.Kill();
        currentSpawnTween?.Kill();
        selectTween = null;
        hintTween = null;
        currentDisappearTween = null;
        currentAppearTween = null;
        currentSpawnTween = null;

        // 선택/힌트 애니메이션 중이었으면 위치 복원
        if (needRestore)
        {
            float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
            cellImage.transform.localPosition = selectOriginalPos;
            cellImage.transform.localScale = Vector3.one * themeScale;
            cellImage.transform.localRotation = Quaternion.identity;
            if (numberText != null)
            {
                numberText.transform.localPosition = textOriginalPos;
                numberText.transform.localScale = Vector3.one;
                numberText.transform.localRotation = Quaternion.identity;
            }
        }
    }

    // =========================
    //  보드 생성 애니메이션
    // =========================

    private Tween currentSpawnTween;

    /// <summary>
    /// 셀 생성 애니메이션 (위에서 떨어지면서 톡 튀어나오는 느낌)
    /// </summary>
    /// <param name="delay">애니메이션 시작 전 딜레이</param>
    public void PlaySpawnAnimation(float delay = 0f)
    {
        currentSpawnTween?.Kill();

        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;

        // 저장된 원래 위치 사용 (이전 애니메이션 중단 상태 복구)
        Vector3 originalPos = selectOriginalPos;
        Vector3 originalTextPos = textOriginalPos;

        // 시작 상태: 위에 있고 스케일 0
        cellImage.transform.localPosition = originalPos + Vector3.up * spawnDropHeight;
        cellImage.transform.localScale = Vector3.zero;

        // 숫자 텍스트도 위에서 시작하고 투명하게
        if (numberText != null && numberText.enabled)
        {
            numberText.transform.localPosition = originalTextPos + Vector3.up * spawnDropHeight;
            numberText.alpha = 0f;
        }

        Sequence spawnSeq = DOTween.Sequence();

        // 딜레이
        if (delay > 0f)
        {
            spawnSeq.AppendInterval(delay);
        }

        // Phase 1: 떨어지면서 커지기 (60%)
        spawnSeq.Append(
            cellImage.transform.DOLocalMove(originalPos, spawnDuration * 0.6f)
                .SetEase(Ease.OutQuad)
        );
        spawnSeq.Join(
            cellImage.transform.DOScale(spawnScalePeak * themeScale, spawnDuration * 0.6f)
                .SetEase(Ease.OutBack)
        );

        // 숫자도 같이 떨어지면서 페이드인
        if (numberText != null && numberText.enabled)
        {
            spawnSeq.Join(
                numberText.transform.DOLocalMove(originalTextPos, spawnDuration * 0.6f)
                    .SetEase(Ease.OutQuad)
            );
            spawnSeq.Join(
                numberText.DOFade(1f, spawnDuration * 0.4f)
                    .SetDelay(spawnDuration * 0.1f)
            );
        }

        // Phase 2: 살짝 줄어들면서 안정 (40%)
        spawnSeq.Append(
            cellImage.transform.DOScale(themeScale, spawnDuration * 0.4f)
                .SetEase(Ease.OutQuad)
        );

        currentSpawnTween = spawnSeq;
    }

    /// <summary>
    /// 즉시 표시 (애니메이션 없이)
    /// </summary>
    public void ShowImmediate()
    {
        currentSpawnTween?.Kill();

        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
        cellImage.transform.localScale = Vector3.one * themeScale;

        if (numberText != null)
        {
            numberText.alpha = 1f;
        }
    }

    private void OnDisable()
    {
        KillTileEffectTweens();
    }

    // =========================
    //  킬 파티클 효과
    // =========================

    /// <summary>
    /// 셀이 사라질 때 파티클 효과 생성
    /// </summary>
    private void SpawnKillParticles()
    {
        ParticleSystem particleCpy = Instantiate(particle, transform.position, Quaternion.identity);
        particleCpy.transform.SetParent(transform.parent.parent.parent);
    }

}
