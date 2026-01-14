using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public abstract class CellAnim
{
    protected CellView cellView;
    protected Image cellImage;
    protected TextMeshProUGUI numberText;
    protected CellAnimConfig config;
    protected Sequence seq;
    public CellAnim(CellView cellView, CellAnimConfig config)
    {
        this.cellView = cellView;
        this.config = config;

        cellImage = cellView.cellImage;
        numberText = cellView.numberText;
    }
    abstract public void PlayAnim(Sprite targetSprite = null, Action onComplete = null);
    abstract public void KillAnim();
}
