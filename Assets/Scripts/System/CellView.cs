using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
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

    [Header("Kill Particle Settings")]
    [SerializeField] private Sprite particleSprite;                   // 파티클 스프라이트
    [SerializeField] private int particleCount = 5;                   // 파티클 개수
    [SerializeField] private float particleSpreadRadius = 60f;        // 퍼지는 반경
    [SerializeField] private float particleDuration = 0.35f;          // 파티클 지속 시간
    [SerializeField] private float particleStartSize = 30f;           // 시작 크기 (픽셀)
    [SerializeField] private float particleEndSize = 5f;              // 끝 크기 (픽셀)

    [Header("Spawn Animation Settings")]
    [SerializeField] private float spawnDuration = 0.25f;           // 생성 애니메이션 시간
    [SerializeField] private float spawnScalePeak = 1.15f;          // 생성 시 톡 튀어나오는 정도
    [SerializeField] private float spawnDropHeight = 30f;           // 위에서 떨어지는 높이

    private int value;          // -1 = 공백, 1~k = 숫자
    private BoardManager board;

    public bool HasNumber => value > 0;
    public int Value => value;

    private bool isSelected = false;
    private bool isHint = false;

    // 애니메이션 관련
    private Tween selectTween;
    private Sequence hintTween;
    private Vector3 selectOriginalPos;
    private Vector3 textOriginalPos;

    // 캐싱
    private static Transform cachedCanvasTransform;

    public void Init(BoardManager board, int x, int y, int initialValue)
    {
        this.board = board;
        this.X = x;
        this.Y = y;

        // 기존 애니메이션 정리 및 위치/상태 초기화
        KillTileEffectTweens();
        cellImage.transform.localPosition = Vector3.zero;
        cellImage.transform.localRotation = Quaternion.identity;
        if (numberText != null) numberText.alpha = 1f;

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

        if (HasNumber)
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

        // 애니메이션 처리
        if (isSelected)
        {
            StopHintAnimation();
            PlaySelectAnimation();
        }
        else if (isHint)
        {
            StopSelectAnimation();
            PlayHintAnimation();
        }
        else
        {
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

    private void PlaySelectAnimation()
    {
        StopSelectAnimation();
        if (!gameObject.activeSelf) return;

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
            if (numberText != null)
            {
                numberText.transform.localPosition = textOriginalPos;
            }
        }
    }

    private void PlayHintAnimation()
    {
        StopHintAnimation();
        if (!gameObject.activeSelf) return;

        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;

        // 메인 시퀀스 (무한 루프)
        hintTween = DOTween.Sequence();

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

    public void OnPointerDown(PointerEventData eventData)
    {
        board.OnCellPointerDown(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("123");
        board.OnCellPointerEnter(this);
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
    /// 콜백 방식 사라짐 애니메이션
    /// </summary>
    public void PlayDisappearAndTransform(System.Action onComplete)
    {
        KillTileEffectTweens();

        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
        Vector3 originalPos = cellImage.transform.localPosition;
        Vector3 originalTextPos = numberText.transform.localPosition;

        Sequence disappearSeq = DOTween.Sequence();

        // Phase 1: 살짝 커지면서 위로 점프 시작
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

        // Phase 2: 스케일 0으로 줄어들면서 사라짐 + 파티클 생성
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
        Vector3 originalPos = cellImage.transform.localPosition;
        Vector3 originalTextPos = numberText.transform.localPosition;

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
        if (particleSprite == null)
        {
            return;
        }

        // 셀의 월드 위치
        Vector3 cellWorldPos = cellImage.transform.position;

        // Canvas 캐싱 (처음 한 번만 찾음)
        if (cachedCanvasTransform == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            cachedCanvasTransform = canvas.transform;
        }

        for (int i = 0; i < particleCount; i++)
        {
            // 파티클 오브젝트 생성 - Canvas의 자식으로
            GameObject particleObj = new GameObject($"KillParticle_{i}");
            particleObj.transform.SetParent(cachedCanvasTransform, false);

            // Canvas의 맨 위로 (다른 UI 요소 위에 렌더링)
            particleObj.transform.SetAsLastSibling();

            // Image 추가
            Image img = particleObj.AddComponent<Image>();
            img.sprite = particleSprite;
            img.raycastTarget = false;

            // RectTransform 설정
            RectTransform rt = particleObj.GetComponent<RectTransform>();
            rt.position = cellWorldPos;
            rt.sizeDelta = new Vector2(particleStartSize, particleStartSize);
            rt.localScale = Vector3.one;

            // 랜덤 방향
            float angle = (360f / particleCount) * i + Random.Range(-30f, 30f);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // 목표 위치
            Vector3 startPos = rt.position;
            Vector3 endPos = startPos + (Vector3)(dir * particleSpreadRadius);

            // 애니메이션
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOMove(endPos, particleDuration).SetEase(Ease.OutQuad));
            seq.Join(rt.DOSizeDelta(new Vector2(particleEndSize, particleEndSize), particleDuration).SetEase(Ease.InQuad));
            seq.Join(rt.DORotate(new Vector3(0, 0, Random.Range(-180f, 180f)), particleDuration).SetEase(Ease.OutQuad));
            seq.Join(img.DOFade(0f, particleDuration * 0.7f).SetDelay(particleDuration * 0.3f));

            seq.OnComplete(() => Destroy(particleObj));
        }
    }
}
