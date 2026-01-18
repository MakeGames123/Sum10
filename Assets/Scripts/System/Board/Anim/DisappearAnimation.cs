using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;
public class DisappearAnimation : CellAnim
{

    public DisappearAnimation(CellView cellView, RectTransform rect, CellAnimConfig config) : base(cellView, rect, config)
    {
        this.cellView = cellView;
        this.config = config;
        this.rect = rect;
        
        cellIamge = cellView.cellImage;
        numberText = cellView.numberText;
    }

    public override void PlayAnim(Sprite targetSprite = null, Action onComplete = null)
    {
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;
        Vector3 originalPos = rect.transform.localPosition;
        Vector3 originalTextPos = numberText.transform.localPosition;

        seq = DOTween.Sequence();

        // Phase 1: 살짝 커지면서 위로 점프
        seq.Append(
            rect.transform.DOScale(config.disappearScalePeak * themeScale, config.disappearDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            rect.transform.DOLocalMoveY(originalPos.y + config.disappearJumpHeight, config.disappearDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOLocalMoveY(originalTextPos.y + config.disappearJumpHeight, config.disappearDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );
        }

        // Phase 2: 파티클 + 스케일 0으로 사라짐
        //seq.AppendCallback(() => SpawnKillParticles());
        seq.Append(
            rect.transform.DOScale(0f, config.disappearDuration * 0.7f)
                .SetEase(Ease.InBack)
        );
        seq.Join(
            rect.transform.DOLocalMoveY(originalPos.y + config.disappearJumpHeight * 0.5f, config.disappearDuration * 0.7f)
                .SetEase(Ease.InQuad)
        );
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOLocalMoveY(originalTextPos.y + config.disappearJumpHeight * 0.5f, config.disappearDuration * 0.7f)
                    .SetEase(Ease.InQuad)
            );
            seq.Join(
                numberText.DOFade(0f, config.disappearDuration * 0.5f)
            );
        }
        seq.OnComplete(() => onComplete?.Invoke());
    }
    public override void KillAnim()
    {
        seq.Kill();
    }
}
