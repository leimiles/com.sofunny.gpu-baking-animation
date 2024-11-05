using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GPUSkinningAnimation), true)]
public class GPUSkinningAnimationEditor : Editor
{
    static class Styles
    {
    }

    SerializedProperty m_GUID;
    SerializedProperty m_Name;
    SerializedProperty m_Clips;
    SerializedProperty m_Bounds;

    void OnEnable()
    {
        m_GUID = serializedObject.FindProperty("guid");
        m_Name = serializedObject.FindProperty("name");
        m_Clips = serializedObject.FindProperty("clips");
        m_Bounds = serializedObject.FindProperty("bounds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("GUID:");
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField(m_GUID.stringValue);
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("Name:");
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField(m_Name.stringValue);
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("Animations:(" + m_Clips.arraySize + ")");
        EditorGUI.indentLevel++;
        for (int i = 0; i < m_Clips.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            SerializedProperty prop = m_Clips.GetArrayElementAtIndex(i);

            SerializedProperty name = prop.FindPropertyRelative("name");
            EditorGUILayout.LabelField(name.stringValue, GUILayout.Width(200));

            Rect wrapRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            wrapRect.width = 65;
            SerializedProperty wrapMode = prop.FindPropertyRelative("wrapMode");
            EditorGUI.PropertyField(wrapRect, wrapMode, GUIContent.none);

            Rect diffRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            diffRect.width = 50;
            SerializedProperty diff = prop.FindPropertyRelative("individualDifferenceEnabled");
            EditorGUI.PropertyField(diffRect, diff, GUIContent.none);

            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField("Bounds:");
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(m_Bounds);
        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
