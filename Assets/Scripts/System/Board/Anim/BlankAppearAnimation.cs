using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;
public class BlankAppearAnimation : CellAnim
{
    
    public BlankAppearAnimation(CellView cellView, CellAnimConfig config) : base(cellView, config)
    {
        this.cellView = cellView;
        this.config = config;

        cellImage = cellView.cellImage;
        numberText = cellView.numberText;
    }


    // 캔슬 시 역방향 뒤집기 애니메이션
    public override void PlayAnim(Sprite targetSprite = null, Action onComplete = null)
    {
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;

        // 시작 스케일 0
        cellImage.transform.localScale = Vector3.zero;

        Sequence appearSeq = DOTween.Sequence();

        // 딜레이
        appearSeq.AppendInterval(config.appearDelay);

        // Scale 0 → 1.1 → 1 (톡 튀어나오는 느낌)
        appearSeq.Append(
            cellImage.transform.DOScale(config.appearScalePeak * themeScale, config.appearDuration * 0.6f)
                .SetEase(Ease.OutBack)
        );
        appearSeq.Append(
            cellImage.transform.DOScale(themeScale, config.appearDuration * 0.4f)
                .SetEase(Ease.InOutQuad)
        );
/*
        appearSeq.OnComplete(() =>
        {
            isSelected = false;
            isHint = false;
            onComplete?.Invoke();
        });
        */
        cellImage.sprite = targetSprite;
        seq.OnComplete(() => onComplete?.Invoke());
    }
    public override void KillAnim()
    {
        seq.Kill();
    }
}
