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

    public void Init(Cell newCellInfo)
    {
        Unsubscribe();

        this.cellInfo = newCellInfo;

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

        Subscribe();
    }
    public void Unsubscribe()
    {
        if (cellInfo == null)
            return;

        cellInfo.onValueChanged -= UpdateVisualState;
        cellInfo.onCellSelectedEvent -= HandleSelect;
        cellInfo.onCellUnSelectedEvent -= HandleUnSelect;
        cellInfo.onEnableHintEvent -= StartHintAnimation;
        cellInfo.onDisableHintEvent -= StopHintAnimation;
    }
    public void Subscribe()
    {
        cellInfo.onValueChanged += UpdateVisualState;
        cellInfo.onCellSelectedEvent += HandleSelect;
        cellInfo.onCellUnSelectedEvent += HandleUnSelect;
        cellInfo.onEnableHintEvent += StartHintAnimation;
        cellInfo.onDisableHintEvent += StopHintAnimation;
    }
    public void HandleSelect()
    {
        PlayAnimation(CellAnimState.Select, GetSelectSprite());
        UpdateFontColor(true);
    }
    public void HandleUnSelect()
    {
        PlayAnimation(CellAnimState.Deselect, GetNormalSprite());
        UpdateFontColor(false);
    }
    public void PlayAnimation(CellAnimState state, Sprite targetSprite = null, Action onComplete = null)
    {
        //Debug.Log(state);
        StopAnimation();
        ResetState();
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
        UpdateFontColor(false);
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
    }
    private void UpdateFontColor(bool isSelected)
    {
        var theme = ThemeManager.Instance.selectedTheme;
        numberText.color = isSelected ? theme.selectedFontColor : theme.normalFontColor;
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
        ResetState();
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