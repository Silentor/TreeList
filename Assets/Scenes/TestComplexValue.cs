using System;
using UnityEditor;
using UnityEngine;
                             
[Serializable]
public class TestComplexValue
{
    public RectOffset           RectOffset;
    public Rect                 Rect;
    [VariableHeight]
    public Boolean              VariableHeightValue;
    [Header("Header attribute")]
    public CustomNode           CustomClass;
    public CustomNodeWithDrawer CustomDrawer;
}

public class VariableHeightAttribute : PropertyAttribute{}

#if UNITY_EDITOR

[CustomPropertyDrawer( typeof(VariableHeightAttribute))]
public class VariableHeightDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
    {
        EditorGUI.PropertyField( position, property, label );
    }

    public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
    {
        if ( property.boolValue )
            return EditorGUIUtility.singleLineHeight * 2;
        else
            return EditorGUIUtility.singleLineHeight;
    }
}

#endif