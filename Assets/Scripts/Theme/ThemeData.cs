using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewTheme", menuName = "Skins/ThemeData")]
[Serializable]
public class ThemeData : ScriptableObject
{
    public string themeName;
    public float textOffset;
    public List<CellSprite> normalSpriteSets;
    public List<CellSprite> blankSpriteSets;
    public float cellScale;
    public float backgroundScale;
    public Sprite boardSkin;//판떼기
    public float boardWidth;
    public float boardOffset;
    public Sprite cellBackground;
    public List<float> scale;
}
[Serializable]
public struct CellSprite
{
    public Sprite normalSprite;       // 기본 셀
    public Sprite hintSprite;         // 힌트 셀
    public Sprite selectedSprite;     // 선택된 셀
}