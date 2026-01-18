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
    [Header("References")]
    public Image cellImage;
    public Image hintImage;
    public Image cellBackground;
    public TextMeshProUGUI numberText;
    private CellSprite normalSprite;
    private CellSprite blankSprite;

    [SerializeField] private ParticleSystem particle;                   // 파티클 스프라이트

    private IBoard board;
    private bool isHint = false;

    public bool IsHint => isHint;

    // 힌트 사운드 리더 (여러 힌트 셀 중 하나만 사운드 재생)
    private bool isHintSoundLeader = false;

    private Dictionary<CellAnimState, CellAnim> animMap = new();
    private CellAnim hintAnim;
    // 애니메이션 관련
    public Vector3 textOriginalPos { get; private set; }

    public CellAnimConfig config;
    public RectTransform parent;
    void Awake()
    {
        RectTransform rect = parent;
        animMap.Add(CellAnimState.BlankAppear, new BlankAppearAnimation(this, rect, config));
        animMap.Add(CellAnimState.Deselect, new DeselectAnimation(this, rect, config));
        animMap.Add(CellAnimState.Disappear, new DisappearAnimation(this, rect, config));
        animMap.Add(CellAnimState.Select, new SelectAnimation(this, rect, config));
        animMap.Add(CellAnimState.Spawn, new SpawnAnimation(this, rect, config));

        hintAnim = new HintAnimation(this, rect, config);
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

    public void Init(IBoard board, Cell cellInfo)
    {
        this.board = board;
        this.cellInfo = cellInfo;

        // 기존 애니메이션 정리 및 위치/상태 초기화
        StopAnimation();
        parent.anchoredPosition = Vector3.zero;
        parent.localRotation = Quaternion.identity;
        if (numberText != null)
        {
            numberText.alpha = 1f;
            numberText.transform.localScale = Vector3.one;
            numberText.transform.localRotation = Quaternion.identity;
        }

        normalSprite = ThemeManager.Instance.selectedTheme.normalSpriteSets[UnityEngine.Random.Range(0, ThemeManager.Instance.selectedTheme.normalSpriteSets.Count)];
        blankSprite = ThemeManager.Instance.selectedTheme.blankSpriteSets[UnityEngine.Random.Range(0, ThemeManager.Instance.selectedTheme.blankSpriteSets.Count)];
        numberText.transform.localPosition = new Vector2(0, ThemeManager.Instance.selectedTheme.textOffset);
        parent.localScale = new Vector2(ThemeManager.Instance.selectedTheme.cellScale, ThemeManager.Instance.selectedTheme.cellScale);
        cellBackground.transform.localScale = new Vector2(ThemeManager.Instance.selectedTheme.backgroundScale, ThemeManager.Instance.selectedTheme.backgroundScale);

        textOriginalPos = numberText.transform.localPosition;

        cellInfo.onValueChanged += UpdateVisualState;
        cellInfo.onCellSelectedEvent += () => PlayAnimation(CellAnimState.Select, GetSelectSprite());
        cellInfo.onCellUnSelectedEvent += () => PlayAnimation(CellAnimState.Deselect, GetNormalSprite());
        cellInfo.onEnableHintEvent += StartHintAnimation;
        cellInfo.onDisableHintEvent += StopHintAnimation;
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
        parent.anchoredPosition = Vector2.zero;
        parent.localScale = Vector3.one * themeScale;
        parent.localRotation = Quaternion.identity;
        if (numberText != null)
        {
            numberText.transform.localPosition = textOriginalPos;
            numberText.transform.localScale = Vector3.one;
            numberText.transform.localRotation = Quaternion.identity;
        }
    }
    public void ResetVisual()
    {
        numberText.text = "";
        numberText.enabled = false;
        cellImage.enabled = false;
        StopHintAnimation();
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
        if (value > 0)
        {
            numberText.text = value.ToString();
        }
        else
        {
            numberText.text = "";
            //매칭된 빈 셀은 뒤집기, 일반셀은 제거
            if (!isAlreadyBlank) PlayAnimation(CellAnimState.Disappear, GetNormalSprite(), () => PlayAnimation(CellAnimState.BlankAppear, blankSprite.normalSprite));
            else PlayAnimation(CellAnimState.Deselect, GetNormalSprite());
        }

        // 폰트 색상 적용
        if (numberText != null && numberText.enabled)
        {
            var theme = ThemeManager.Instance.selectedTheme;
            //numberText.color = isSelected ? theme.selectedFontColor : theme.normalFontColor;
        }
    }
    private void StartHintAnimation()
    {
        hintImage.sprite = GetHintSprite();
        hintImage.enabled = true;
        hintAnim.PlayAnim();
    }
    private void StopHintAnimation()
    {
        hintAnim.KillAnim();
        hintImage.enabled = false;
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
        cellImage.enabled = true;
        numberText.enabled = true;
        PlayAnimation(CellAnimState.Spawn, GetNormalSprite());
    }
    private void SpawnKillParticles()
    {
        ParticleSystem particleCpy = Instantiate(particle, transform.position, Quaternion.identity);
        particleCpy.transform.SetParent(transform.parent.parent.parent);
    }
}


/*


    /// <summary>
    /// 힌트 애니메이션을 강제로 재시작 (싱크 맞추기용)
    /// </summary>
    public void ForceRestartHintAnimation()
    {
        if (isHint)
        {
            StopHintAnimation();
            PlayHintAnimation(playSound: true);
        }
    }

    private void PlayHintAnimation(bool playSound = true)
    {
    }

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