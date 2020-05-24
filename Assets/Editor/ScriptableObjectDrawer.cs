using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Allows expanding of scriptable object references and editing their values
/// </summary>
[CustomPropertyDrawer(typeof(ScriptableObject), true)]
public class ScriptableObjectDrawer : PropertyDrawer
{
    private Editor editor;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);

        if (property.objectReferenceValue)
        {
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
        }

        if (property.isExpanded)
        {
            if (!editor)
            {
                Editor.CreateCachedEditor(property.objectReferenceValue, null, ref editor);
                if (!editor) return;
            }
            EditorGUI.indentLevel++;

            editor.OnInspectorGUI();

            EditorGUI.indentLevel--;
        }
        
    }
    
}