using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ThemeData))]
public class ThemeDataEditor : Editor
{
    SerializedProperty themeName;
    SerializedProperty textOffset;

    SerializedProperty normalSpriteSets;
    SerializedProperty blankSpriteSets;

    SerializedProperty gridSize;
    SerializedProperty spacing;

    SerializedProperty boardSkin;
    SerializedProperty boardWidth;
    SerializedProperty boardOffset;

    SerializedProperty cellBackground;
    SerializedProperty scale;

    void OnEnable()
    {
        themeName = serializedObject.FindProperty("themeName");
        textOffset = serializedObject.FindProperty("textOffset");

        normalSpriteSets = serializedObject.FindProperty("normalSpriteSets");
        blankSpriteSets = serializedObject.FindProperty("blankSpriteSets");

        gridSize = serializedObject.FindProperty("gridSize");
        spacing = serializedObject.FindProperty("spacing");

        boardSkin = serializedObject.FindProperty("boardSkin");
        boardWidth = serializedObject.FindProperty("boardWidth");
        boardOffset = serializedObject.FindProperty("boardOffset");

        cellBackground = serializedObject.FindProperty("cellBackground");
        scale = serializedObject.FindProperty("scale");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("Basic");
        EditorGUILayout.PropertyField(themeName);
        EditorGUILayout.PropertyField(textOffset, new GUIContent("텍스트 세로축 보정"));

        DrawHeader("Grid");
        EditorGUILayout.PropertyField(gridSize);
        EditorGUILayout.PropertyField(spacing);

        DrawHeader("Board");
        EditorGUILayout.PropertyField(boardSkin, new GUIContent("보드 스킨"));
        EditorGUILayout.PropertyField(boardWidth, new GUIContent("보드 길이"));
        EditorGUILayout.PropertyField(boardOffset, new GUIContent("보드 세로축 보정"));

        DrawHeader("Scale Presets 3x3부터");
        EditorGUILayout.PropertyField(scale, true);

        DrawHeader("Cell");
        EditorGUILayout.PropertyField(cellBackground, new GUIContent("셀 배경"));

        DrawHeader("Normal Cell Sprites");
        DrawCellSpriteList(normalSpriteSets);

        DrawHeader("Blank Cell Sprites");
        DrawCellSpriteList(blankSpriteSets);

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
