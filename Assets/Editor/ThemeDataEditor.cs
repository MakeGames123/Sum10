using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ThemeData))]
public class ThemeDataEditor : Editor
{
    SerializedProperty themeName;
    SerializedProperty textOffset;

    SerializedProperty normalSpriteSets;
    SerializedProperty blankSpriteSets;

    SerializedProperty gridScale;
    SerializedProperty backgroundScale;

    SerializedProperty boardSkin;
    SerializedProperty boardLowSkin;
    SerializedProperty background;
    SerializedProperty boardWidth;
    SerializedProperty boardHeight;
    SerializedProperty boardOffset;
    SerializedProperty boardOffsetX;

    SerializedProperty themeThumbnail;
    SerializedProperty cellBackground;
    SerializedProperty scale;

    SerializedProperty normalFontColor;
    SerializedProperty selectedFontColor;

    void OnEnable()
    {
        themeName = serializedObject.FindProperty("themeName");
        textOffset = serializedObject.FindProperty("textOffset");

        normalSpriteSets = serializedObject.FindProperty("normalSpriteSets");
        blankSpriteSets = serializedObject.FindProperty("blankSpriteSets");

        gridScale = serializedObject.FindProperty("cellScale");
        backgroundScale = serializedObject.FindProperty("backgroundScale");

        boardSkin = serializedObject.FindProperty("boardSkin");
        boardLowSkin = serializedObject.FindProperty("boardLowSkin");
        background = serializedObject.FindProperty("background");
        boardWidth = serializedObject.FindProperty("boardWidth");
        boardHeight = serializedObject.FindProperty("boardHeight");
        boardOffset = serializedObject.FindProperty("boardOffset");
        boardOffsetX = serializedObject.FindProperty("boardOffsetX");

        themeThumbnail = serializedObject.FindProperty("themeThumbnail");
        cellBackground = serializedObject.FindProperty("cellBackground");
        scale = serializedObject.FindProperty("scale");

        normalFontColor = serializedObject.FindProperty("normalFontColor");
        selectedFontColor = serializedObject.FindProperty("selectedFontColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("Basic");
        EditorGUILayout.PropertyField(themeName);
        EditorGUILayout.PropertyField(textOffset, new GUIContent("텍스트 세로축 보정"));

        DrawHeader("Scale");
        EditorGUILayout.PropertyField(gridScale, new GUIContent("셀 스케일"));
        EditorGUILayout.PropertyField(backgroundScale, new GUIContent("셀 배경 스케일"));

        DrawHeader("Scale Presets 3x3부터");
        EditorGUILayout.PropertyField(scale, true);

        DrawHeader("Board");
        EditorGUILayout.PropertyField(background, new GUIContent("배경"));
        EditorGUILayout.PropertyField(boardSkin, new GUIContent("보드 스킨"));
        EditorGUILayout.PropertyField(boardLowSkin, new GUIContent("보드 하단 스킨"));
        EditorGUILayout.PropertyField(boardWidth, new GUIContent("보드 길이"));
        EditorGUILayout.PropertyField(boardHeight, new GUIContent("보드 세로 길이"));
        EditorGUILayout.PropertyField(boardOffset, new GUIContent("보드 세로축 보정"));
        EditorGUILayout.PropertyField(boardOffsetX, new GUIContent("보드 가로축 보정"));

        DrawHeader("Cell");
        EditorGUILayout.PropertyField(themeThumbnail, new GUIContent("대표 이미지"));
        EditorGUILayout.PropertyField(cellBackground, new GUIContent("셀 배경"));

        DrawHeader("Normal Cell Sprites");
        DrawCellSpriteList(normalSpriteSets);

        DrawHeader("Blank Cell Sprites");
        DrawCellSpriteList(blankSpriteSets);

        DrawHeader("Font Colors");
        EditorGUILayout.PropertyField(normalFontColor, new GUIContent("기본 폰트 색상"));
        EditorGUILayout.PropertyField(selectedFontColor, new GUIContent("선택 시 폰트 색상"));

        serializedObject.ApplyModifiedProperties();
    }

    void DrawHeader(string title)
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    void DrawCellSpriteList(SerializedProperty list)
    {
        EditorGUILayout.PropertyField(list, new GUIContent(list.displayName), true);
    }
}
