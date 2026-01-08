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
    public Sprite themeThumbnail;
    public List<CellSprite> normalSpriteSets;
    public List<CellSprite> blankSpriteSets;
    public float cellScale;
    public float backgroundScale;
    public Sprite boardLowSkin;//판떼기
    public Sprite boardSkin;//판떼기
    public Sprite background;//판떼기
    public float boardWidth;
    public float boardHeight;
    public float boardOffset;
    public Sprite cellBackground;
    public List<float> scale;

    [Header("Font Colors")]
    public Color normalFontColor = new Color(0.294f, 0.216f, 0.188f, 1f);      // 기본 폰트 색상 (#4B3730)
    public Color selectedFontColor = new Color(0.294f, 0.216f, 0.188f, 1f);    // 선택 시 폰트 색상 (#4B3730)
}
[Serializable]
public struct CellSprite
{
    public Sprite normalSprite;       // 기본 셀
    public Sprite hintSprite;         // 힌트 셀
    public Sprite selectedSprite;     // 선택된 셀
}