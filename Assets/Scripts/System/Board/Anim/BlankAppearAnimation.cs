using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;
public class BlankAppearAnimation : CellAnim
{
    
    public BlankAppearAnimation(CellView cellView, RectTransform rect, CellAnimConfig config) : base(cellView, rect, config)
    {
        this.cellView = cellView;
        this.config = config;
        this.rect = rect;
        
        cellIamge = cellView.cellImage;
        numberText = cellView.numberText;
    }


    // 캔슬 시 역방향 뒤집기 애니메이션
    public override void PlayAnim(Sprite targetSprite = null, Action onComplete = null)
    {
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;

        // 시작 스케일 0
        rect.transform.localScale = Vector3.zero;

        Sequence appearSeq = DOTween.Sequence();

        // 딜레이
        appearSeq.AppendInterval(config.appearDelay);

        // Scale 0 → 1.1 → 1 (톡 튀어나오는 느낌)
        appearSeq.Append(
            rect.transform.DOScale(config.appearScalePeak * themeScale, config.appearDuration * 0.6f)
                .SetEase(Ease.OutBack)
        );
        appearSeq.Append(
            rect.transform.DOScale(themeScale, config.appearDuration * 0.4f)
                .SetEase(Ease.InOutQuad)
        );
        
        cellIamge.sprite = targetSprite;
        seq.OnComplete(() => onComplete?.Invoke());
    }
    public override void KillAnim()
    {
        seq.Kill();
    }
}
