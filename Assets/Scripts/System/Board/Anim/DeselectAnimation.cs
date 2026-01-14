using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;
public class DeselectAnimation : CellAnim
{
    public DeselectAnimation(CellView cellView, CellAnimConfig config) : base(cellView, config)
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
        float rotDir = UnityEngine.Random.value > 0.5f ? 1f : -1f;

        seq = DOTween.Sequence();

        // Phase 1: 위로 점프 + 기울어짐 + 뒤집기 시작 (ScaleX 0으로)
        seq.Append(
            cellImage.transform.DOScaleX(0f, config.flipDuration * 0.35f)
                .SetEase(Ease.InQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalMoveY(cellView.selectOriginalPos.y + config.selectJumpHeight, config.flipDuration * 0.35f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalRotate(new Vector3(0, 0, config.selectRotation * rotDir), config.flipDuration * 0.35f)
                .SetEase(Ease.OutQuad)
        );
        // 숫자도 같이 점프 + flip + 기울어짐
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(0f, config.flipDuration * 0.35f)
                    .SetEase(Ease.InQuad)
            );
            seq.Join(
                numberText.transform.DOLocalMoveY(cellView.textOriginalPos.y + config.selectJumpHeight, config.flipDuration * 0.35f)
                    .SetEase(Ease.OutQuad)
            );
            seq.Join(
                numberText.transform.DOLocalRotate(new Vector3(0, 0, config.selectRotation * rotDir), config.flipDuration * 0.35f)
                    .SetEase(Ease.OutQuad)
            );
        }

        cellImage.sprite = targetSprite;
        // Phase 2: 뒤집기 완료 + 살짝 커짐
        seq.Append(
            cellImage.transform.DOScaleX(config.selectScalePunch * themeScale, config.flipDuration * 0.35f)
                .SetEase(Ease.OutBack)
        );
        // 숫자도 뒤집기 완료 + 살짝 커짐
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(config.selectScalePunch, config.flipDuration * 0.35f)
                    .SetEase(Ease.OutBack)
            );
        }

        // Phase 3: 탄력있게 착지 + 원래 크기 + 회전 복원
        seq.Append(
            cellImage.transform.DOScaleX(themeScale, config.flipDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            cellImage.transform.DOLocalMoveY(cellView.selectOriginalPos.y, config.flipDuration * 0.3f)
                .SetEase(Ease.OutBounce)
        );
        seq.Join(
            cellImage.transform.DOLocalRotate(Vector3.zero, config.flipDuration * 0.3f)
                .SetEase(Ease.OutBack)
        );
        // 숫자도 같이 내려옴 + 원래 크기 + 회전 복원
        if (numberText != null && numberText.enabled)
        {
            seq.Join(
                numberText.transform.DOScaleX(1f, config.flipDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );
            seq.Join(
                numberText.transform.DOLocalMoveY(cellView.textOriginalPos.y, config.flipDuration * 0.3f)
                    .SetEase(Ease.OutBounce)
            );
            seq.Join(
                numberText.transform.DOLocalRotate(Vector3.zero, config.flipDuration * 0.3f)
                    .SetEase(Ease.OutBack)
            );
        }
        
        seq.OnComplete(() => onComplete?.Invoke());
    }
    public override void KillAnim()
    {
        seq.Kill();
    }
}
