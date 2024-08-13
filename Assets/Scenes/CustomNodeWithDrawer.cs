using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[Serializable]
public class CustomNodeWithDrawer
{
    public Int32   Int;
    public Boolean Bool;
}

#if UNITY_EDITOR

[CustomPropertyDrawer( typeof( CustomNodeWithDrawer ) )]
public class CustomNodeWithDrawerDrawer : PropertyDrawer
{
    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
    {
        var heightBool = property.FindPropertyRelative( "Bool" );

        EditorGUI.BeginProperty( position, label, property );
        GUI.Label( position, label );
        position.x += EditorGUIUtility.labelWidth;

        if ( heightBool.boolValue )
        {
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField( position, heightBool, GUIContent.none );
            position.y += EditorGUIUtility.singleLineHeight;
            GUI.Label( position, "Variable height custom property drawer" );
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField( position, property.FindPropertyRelative( "Int" ),  GUIContent.none );
        }
        else
        {
            EditorGUI.PropertyField( position, heightBool, GUIContent.none );
            position.x += 50;
            EditorGUI.PropertyField( position, property.FindPropertyRelative( "Int" ), GUIContent.none );
        }
        
        EditorGUI.EndProperty();
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty property )
    {
        var rootUITk = new VisualElement(){ name = "Root" };
        var label                              = new Label(property.displayName);
        var boolField                          = new PropertyField(property.FindPropertyRelative("Bool"), String.Empty );
        boolField.RegisterValueChangeCallback( (evt) => BoolChanged(rootUITk, evt) );
        var intFieldField = new PropertyField(property.FindPropertyRelative("Int"), String.Empty );
        rootUITk.Add( label );
        rootUITk.Add( boolField );
        rootUITk.Add( intFieldField );

        ModifyStyle( rootUITk, property.FindPropertyRelative("Bool").boolValue );

        return rootUITk;
    }

    private void BoolChanged( VisualElement root, SerializedPropertyChangeEvent evt )
    {
       ModifyStyle( root, evt.changedProperty.boolValue );
    }

    private void ModifyStyle( VisualElement root, bool isMultiline )
    {
        if ( isMultiline )
        {
            root.style.flexDirection = FlexDirection.Column;
        }
        else
        {
            root .style.flexDirection = FlexDirection.Row;
        }
    }

    public override Single GetPropertyHeight( SerializedProperty property, GUIContent label )
    {
        var boolProp = property.FindPropertyRelative( "Bool" );
        if( boolProp.boolValue )
            return EditorGUIUtility.singleLineHeight * 3;
        return EditorGUIUtility.singleLineHeight;
    }
}

#endif

