using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public BoardManager board;
    public GameManager game;
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
            return;
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
}
