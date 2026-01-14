using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CellAnimState
{
    BlankAppear,
    Deselect,
    Disappear,
    Hint,
    Select,
    Spawn,
}
public class CellView : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    [Header("References")]
    public Image cellImage;
    public Image cellBackground;
    public TextMeshProUGUI numberText;

    private CellSprite normalSprite;
    private CellSprite blankSprite;

    [SerializeField] private ParticleSystem particle;                   // 파티클 스프라이트

    private IBoard board;

    // 파괴 대기 중 플래그 (매칭 성공 후 애니메이션 중 재선택 방지)
    private bool isPendingDestruction = false;

    public bool IsPendingDestruction => isPendingDestruction;

    private bool isSelected = false;
    private bool isHint = false;
    private bool wasSelected = false;  // Flip 모드 선택 해제 애니메이션용

    public bool IsHint => isHint;

    // 힌트 사운드 리더 (여러 힌트 셀 중 하나만 사운드 재생)
    private bool isHintSoundLeader = false;

    private Dictionary<CellAnimState, CellAnim> animMap = new();
    // 애니메이션 관련
    private Tween selectTween;
    private Sequence hintTween;
    public Vector3 selectOriginalPos { get; private set; }
    public Vector3 textOriginalPos { get; private set; }

    // 캐싱
    private static Transform cachedCanvasTransform;
    public CellAnimConfig config;

    // Ghost 애니메이션 관리용 ID
    private const string GHOST_TWEEN_ID = "DisappearGhost";

    void Awake()
    {
        animMap.Add(CellAnimState.BlankAppear, new BlankAppearAnimation(this, config));
        animMap.Add(CellAnimState.Deselect, new DeselectAnimation(this, config));
        animMap.Add(CellAnimState.Disappear, new DisappearAnimation(this, config));
        animMap.Add(CellAnimState.Hint, new HintAnimation(this, config));
        animMap.Add(CellAnimState.Select, new SelectAnimation(this, config));
        animMap.Add(CellAnimState.Spawn, new SpawnAnimation(this, config));
    }
    /// <summary>
    /// 모든 Ghost 애니메이션을 즉시 중단하고 제거 (게임 종료 시 호출)
    /// </summary>
    public static void KillAllGhostAnimations()
    {
        DOTween.Kill(GHOST_TWEEN_ID);
    }

    Cell cellInfo;

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

    public void Init(IBoard board, int x, int y, Cell cellInfo)
    {
        this.board = board;
        this.X = x;
        this.Y = y;
        this.cellInfo = cellInfo;

        // 기존 애니메이션 정리 및 위치/상태 초기화
        StopAnimation();
        cellImage.transform.localPosition = Vector3.zero;
        cellImage.transform.localRotation = Quaternion.identity;
        if (numberText != null)
        {
            numberText.alpha = 1f;
            numberText.transform.localScale = Vector3.one;
            numberText.transform.localRotation = Quaternion.identity;
        }

        normalSprite = ThemeManager.Instance.selectedTheme.normalSpriteSets[UnityEngine.Random.Range(0, ThemeManager.Instance.selectedTheme.normalSpriteSets.Count)];
        blankSprite = ThemeManager.Instance.selectedTheme.blankSpriteSets[UnityEngine.Random.Range(0, ThemeManager.Instance.selectedTheme.blankSpriteSets.Count)];
        numberText.transform.localPosition = new Vector2(0, ThemeManager.Instance.selectedTheme.textOffset);
        cellImage.transform.localScale = new Vector2(ThemeManager.Instance.selectedTheme.cellScale, ThemeManager.Instance.selectedTheme.cellScale);
        cellBackground.transform.localScale = new Vector2(ThemeManager.Instance.selectedTheme.backgroundScale, ThemeManager.Instance.selectedTheme.backgroundScale);

        // 원래 위치 저장
        selectOriginalPos = cellImage.transform.localPosition;
        textOriginalPos = numberText.transform.localPosition;

        cellInfo.onValueChanged += UpdateVisualState;
        cellInfo.onCellSelectedEvent += () => PlayAnimation(CellAnimState.Select, GetSelectSprite());
        cellInfo.onCellUnSelectedEvent += () => PlayAnimation(CellAnimState.Deselect, GetNormalSprite());
        cellInfo.onEnableHintEvent += () => PlayAnimation(CellAnimState.Hint, GetHintSprite());
    }

    public void PlayAnimation(CellAnimState state, Sprite targetSprite = null, Action onComplete = null)
    {
        //Debug.Log(state);
        ResetState();
        StopAnimation();
        animMap[state].PlayAnim(targetSprite, onComplete);
    }
    private void StopAnimation()
    {
        foreach (var anim in animMap.Values)
        {
            anim.KillAnim();
        }
    }

    private void ResetState()
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

    private void UpdateVisualState(bool isAlreadyBlank)
    {
        if (ThemeManager.Instance.selectedTheme.cellBackground != null)
        {
            cellBackground.sprite = ThemeManager.Instance.selectedTheme.cellBackground;
            cellBackground.enabled = true;
        }
        else cellBackground.enabled = false;

        int value = cellInfo.ReturnNum();
        if (value > 0 && !isPendingDestruction)
        {
            numberText.text = value.ToString();
            numberText.enabled = true;
        }
        else
        {
            numberText.text = "";
            numberText.enabled = false;
            if (!isAlreadyBlank) PlayAnimation(CellAnimState.Disappear, GetNormalSprite(), () => PlayAnimation(CellAnimState.BlankAppear, blankSprite.normalSprite));
            else PlayAnimation(CellAnimState.Deselect, GetNormalSprite());
        }

        // 폰트 색상 적용
        if (numberText != null && numberText.enabled)
        {
            var theme = ThemeManager.Instance.selectedTheme;
            numberText.color = isSelected ? theme.selectedFontColor : theme.normalFontColor;
        }
    }

    /*

    /// <summary>
    /// 공백 셀이 매칭에 포함되어 터질 때 호출 (Flip 롤백 애니메이션 재생)
    /// </summary>
    public void PlayDeselectForMatch()
    {
        if (isSelected)
        {
            isSelected = false;
            wasSelected = false;
            isHint = false;
            StopHintAnimation();

            // 스프라이트를 기본으로 변경 (UpdateVisualState 대신 직접 처리)
            cellImage.sprite = blankSprite.normalSprite;

            PlayDeselectAnimationFlip();
        }
        else
        {
            SetHighlight(false);
        }
    }

    */

    /// <summary>
    /// 파괴 대기 상태 설정 (매칭 성공 후 즉시 호출하여 재선택 방지)
    /// </summary>\

    /*
    public void SetValue(int newValue)
    {
        value = newValue;
        isPendingDestruction = false;  // 새 값 설정 시 파괴 대기 해제


        isSelected = false;
        isHint = false;
        UpdateVisualState();
    }
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
    */

    private void PlayHintAnimation(bool playSound = true)
    {
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
    private Sprite GetNormalSprite()
    {
        bool isBlankCell = cellInfo.ReturnNum() <= 0;

        if (isBlankCell) return blankSprite.normalSprite;
        else return normalSprite.normalSprite;
    }
    private Sprite GetHintSprite()
    {
        bool isBlankCell = cellInfo.ReturnNum() <= 0;

        if (isBlankCell) return blankSprite.hintSprite;
        else return normalSprite.hintSprite;
    }
    private Sprite GetSelectSprite()
    {
        bool isBlankCell = cellInfo.ReturnNum() <= 0;

        if (isBlankCell) return blankSprite.selectedSprite;
        else return normalSprite.selectedSprite;
    }


    /// <summary>
    /// 셀 생성 애니메이션 (위에서 떨어지면서 톡 튀어나오는 느낌)
    /// </summary>
    /// <param name="delay">애니메이션 시작 전 딜레이</param>
    public IEnumerator PlaySpawnAnimation(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);
        PlayAnimation(CellAnimState.Spawn, GetNormalSprite());
    }
    private void SpawnKillParticles()
    {
        ParticleSystem particleCpy = Instantiate(particle, transform.position, Quaternion.identity);
        particleCpy.transform.SetParent(transform.parent.parent.parent);
    }
}
