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
    protected RectTransform rect;
    protected Image cellIamge;
    protected TextMeshProUGUI numberText;
    protected CellAnimConfig config;
    protected Sequence seq;
    public CellAnim(CellView cellView, RectTransform rect, CellAnimConfig config)
    {
        this.cellView = cellView;
        this.config = config;
        this.rect = rect;
        
        cellIamge = cellView.cellImage;
        numberText = cellView.numberText;
    }
    abstract public void PlayAnim(Sprite targetSprite = null, Action onComplete = null);
    abstract public void KillAnim();
}
