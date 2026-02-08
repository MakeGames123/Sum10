using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;
public class SpawnAnimation : CellAnim
{
    
    public SpawnAnimation(CellView cellView, RectTransform rect, CellAnimConfig config) : base(cellView, rect, config)
    {
        this.cellView = cellView;
        this.config = config;
        this.rect = rect;
        
        cellIamge = cellView.cellImage;
        numberText = cellView.numberText;
    }


    //공백셀 등장 애니메이션
    public override void PlayAnim(Sprite targetSprite = null, Action onComplete = null)
    {
        float themeScale = ThemeManager.Instance.selectedTheme.cellScale;

        // 저장된 원래 위치 사용 (이전 애니메이션 중단 상태 복구)
        Vector3 originalTextPos = cellView.textOriginalPos;

        // 시작 상태: 위에 있고 스케일 0
        rect.anchoredPosition = Vector3.zero + Vector3.up * config.spawnDropHeight;
        rect.transform.localScale = Vector3.zero;

        // 숫자 텍스트도 위에서 시작하고 투명하게
        if (numberText != null && numberText.enabled)
        {
            numberText.transform.localPosition = originalTextPos + Vector3.up * config.spawnDropHeight;
            numberText.alpha = 0f;
        }

        seq = DOTween.Sequence();

        // Phase 1: 떨어지면서 커지기 (60%)
        seq.Append(
            rect.transform.DOLocalMove(Vector3.zero, config.spawnDuration * 0.6f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            rect.transform.DOScale(config.spawnScalePeak * themeScale, config.spawnDuration * 0.6f)
                .SetEase(Ease.OutBack)
        );

        // 숫자도 같이 떨어지면서 페이드인
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOLocalMove(originalTextPos, config.spawnDuration * 0.6f)
                    .SetEase(Ease.OutQuad)
            );
            seq.Join(
                numberText.DOFade(1f, config.spawnDuration * 0.4f)
                    .SetDelay(config.spawnDuration * 0.1f)
            );
        }

        // Phase 2: 살짝 줄어들면서 안정 (40%)
        seq.Append(
            rect.transform.DOScale(themeScale, config.spawnDuration * 0.4f)
                .SetEase(Ease.OutQuad)
        );
        cellIamge.sprite = targetSprite;
        seq.OnComplete(() => onComplete?.Invoke());
    }
    public override void KillAnim()
    {
        seq.Kill();
    }
}
