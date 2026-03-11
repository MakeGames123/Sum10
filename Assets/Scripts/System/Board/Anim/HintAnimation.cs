using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;
public class HintAnimation : CellAnim
{

    public HintAnimation(CellView cellView, RectTransform rect, CellAnimConfig config) : base(cellView, rect, config)
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

        // 메인 시퀀스 (무한 루프)
        seq = DOTween.Sequence();
        // 연속 바운스 (hintBounceCount번)
        for (int i = 0; i < config.hintBounceCount; i++)
        {
            float bounceScale = config.hintScalePunch - (i * 0.03f); // 점점 작아지는 바운스
            float bounceHeight = config.hintJumpHeight - (i * 2f);   // 점점 낮아지는 점프

            // 위로 올라가면서 커지기
            seq.Append(
                rect.transform.DOScale(bounceScale * themeScale, config.hintBounceDuration * 0.4f)
                    .SetEase(Ease.OutQuad)
            );
            seq.Join(
                rect.transform.DOLocalMoveY(bounceHeight, config.hintBounceDuration * 0.4f)
                    .SetEase(Ease.OutQuad)
            );
            // 숫자도 같이
            if (numberText != null)
            {
                seq.Join(
                    numberText.transform.DOLocalMoveY(cellView.textOriginalPos.y + bounceHeight, config.hintBounceDuration * 0.4f)
                        .SetEase(Ease.OutQuad)
                );
            }

            // 내려오면서 원래 크기로
            seq.Append(
                rect.transform.DOScale(themeScale, config.hintBounceDuration * 0.6f)
                    .SetEase(Ease.OutBounce)
            );
            seq.Join(
                rect.transform.DOLocalMoveY(0, config.hintBounceDuration * 0.6f)
                    .SetEase(Ease.OutBounce)
            );
            // 숫자도 같이
            if (numberText != null)
            {
                seq.Join(
                    numberText.transform.DOLocalMoveY(cellView.textOriginalPos.y, config.hintBounceDuration * 0.6f)
                        .SetEase(Ease.OutBounce)
                );
            }
        }

        // 멈춤
        seq.AppendInterval(config.hintPauseDuration);

        // 무한 루프
        seq.SetLoops(-1, LoopType.Restart);
        seq.OnComplete(() => onComplete?.Invoke());
    }
    public override void KillAnim()
    {
        if (seq != null)
        {
            seq.Kill(true);
            seq = null;
        }
    }
}
