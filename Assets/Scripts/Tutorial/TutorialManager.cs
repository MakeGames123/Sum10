using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TutorialManager : MonoBehaviour
{
    public BoardManager board;
    public GameManager game;

    [Header("Tutorial UI")]
    [SerializeField] private RectTransform scoreSection;
    [SerializeField] private RectTransform timerSection;
    [SerializeField] private RectTransform tutorialText;

    [Header("전환 애니메이션")]
    [SerializeField] private float transitionDuration = 0.35f;
    [SerializeField] private float breathSpeed = 0.5f;
    [SerializeField] private float breathAmount = 0.05f;

    private Tween breathTween;

    List<List<int>> tutorialCellIndexs = new()
    {
        new List<int>() { 2, 5 },
        new List<int>() { 3, 6, 7 },
        new List<int>() { 0, 1, 2, 5, 8 }
    };
    int progress = 0;
    List<Cell> tutorialCells = new();
    public void TutorialProgress()
    {
        game.isTutorial = true;

        if (progress == 3)
        {
            game.isTutorial = false;
            TutorialStatusManager.Instance.UpdateStatus(true);
            PlayTimerEntrance();
            return;
        }

        // 첫 진입 시 스코어/타이머 숨기고 튜토리얼 텍스트 표시
        if (progress == 0)
        {
            if (scoreSection != null) scoreSection.gameObject.SetActive(false);
            if (timerSection != null) timerSection.gameObject.SetActive(false);
            if (tutorialText != null)
            {
                tutorialText.localScale = Vector3.zero;
                tutorialText.gameObject.SetActive(true);
                tutorialText.DOScale(Vector3.one, transitionDuration).SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        // 숨쉬기 애니메이션
                        breathTween = tutorialText.DOScale(1f + breathAmount, 1f / breathSpeed)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    });
            }
        }

        tutorialCells.Clear();

        for (int i = 0; i < tutorialCellIndexs[progress].Count; i++)
        {
            tutorialCells.Add(board.cells[tutorialCellIndexs[progress][i]]);
        }

        foreach (Cell cell in board.cells)
        {
            cell.UpdateCellLock(true);
        }

        foreach (Cell cell in tutorialCells)
        {
            cell.EnableHintMode();
            cell.UpdateCellLock(false);
        }
    }
    public void TutorialProgressEnd()
    {
        progress++;

        foreach (Cell cell in tutorialCells)
        {
            cell.DisableHintMode();
            cell.onRemoved -= TutorialProgressEnd;
        }

        TutorialProgress();
    }

    private void PlayTimerEntrance()
    {
        // 튜토리얼 텍스트 스케일 아웃 → 완료 후 스코어/타이머 스케일 인
        if (tutorialText != null)
        {
            breathTween?.Kill();
            tutorialText.localScale = Vector3.one;
            tutorialText.DOScale(Vector3.zero, transitionDuration).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    tutorialText.gameObject.SetActive(false);
                    ShowHudElement(scoreSection);
                    ShowHudElement(timerSection);
                });
        }
    }

    private void ShowHudElement(RectTransform element)
    {
        if (element == null) return;
        element.localScale = Vector3.zero;
        element.gameObject.SetActive(true);
        element.DOScale(Vector3.one, transitionDuration).SetEase(Ease.OutBack);
    }
}
