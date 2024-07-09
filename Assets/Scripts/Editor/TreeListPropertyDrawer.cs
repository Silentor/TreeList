using System;
using MyBox.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Silentor.TreeControl.Editor
{
    //[CustomPropertyDrawer( typeof(TreeList<>), true )]
    public class TreeListPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var labelX = position.x;
            position    = EditorGUI.PrefixLabel( position, label );

            SerializedProperty array = property.FindPropertyRelative( "SerializableNodes" );
            position.height = EditorGUIUtility.singleLineHeight;

            if( array.arraySize == 0 )
            {
                GUI.Label( position, $"Empty {property.type}" );
                return;
            }

            var index = 0;
            DrawLevel( labelX, ref position, 0, array, ref index );
        }

        private void DrawLevel( Single labelX, ref Rect position, Int32 level, SerializedProperty array, ref Int32 index )
        {
            var childIndex = 0;

            for ( ; index < array.arraySize; index++ )
            {
                var nodeItem   = array.GetArrayElementAtIndex( index );
                var valueProp  = nodeItem.FindPropertyRelative( "Value" );
                var parentProp = nodeItem.FindPropertyRelative( "ParentIndex" );
                var levelProp  = nodeItem.FindPropertyRelative( "Level" );

                if ( levelProp.intValue > level )
                {
                    DrawLevel( labelX, ref position, levelProp.intValue, array, ref index );
                }
                else if ( levelProp.intValue < level )
                {
                    index--;
                    return;
                }
                else
                {
                    if ( index > 0 )
                    {
                        var labelPos = new Rect( labelX + level * 16, position.y, position.width, position.height );
                        GUI.Label( labelPos, $"Child{level}.{childIndex++}" );
                    }

                    GUI.Label( position, valueProp.GetValue().ToString() );
                    position.y += EditorGUIUtility.singleLineHeight;
                }
            }
        }

        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            var itemsCount = property.FindPropertyRelative( "SerializableNodes" ).arraySize;
            return itemsCount > 0 
                    ? EditorGUIUtility.singleLineHeight * (property.FindPropertyRelative( "SerializableNodes" ).arraySize )
                    : EditorGUIUtility.singleLineHeight;    
        }
    }
}