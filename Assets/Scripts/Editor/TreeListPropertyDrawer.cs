using System;
using MyBox.EditorTools;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = System.Object;

namespace Silentor.TreeControl.Editor
{
    [CustomPropertyDrawer( typeof(TreeList<>), true )]
    public class TreeListPropertyDrawer : PropertyDrawer
    {
        private TreeViewState          _treeViewState;
        private MyTreeView             _tree;
        private MultiColumnHeaderState _multiColumnHeaderState;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            position    = EditorGUI.PrefixLabel( position, label );

            if( _treeViewState == null )
                _treeViewState = new TreeViewState();
            if( _multiColumnHeaderState == null )
            {
                _multiColumnHeaderState = new MultiColumnHeaderState( 
                        new MultiColumnHeaderState.Column[]
                        {
                            new ()
                            {
                                    //headerContent         = new GUIContent( "Tree" ), 
                                    width                 = 30,
                                    minWidth = 30,
                                    maxWidth = 30,
                                    allowToggleVisibility = false,
                                    autoResize            = false,
                            },
                            new ()
                            {
                                    headerContent         = new GUIContent( "Value" ),
                                    width                 = position.width - 50,
                                    allowToggleVisibility = false,
                                    autoResize            = true,
                            },
                        
                        } );

            }
            else
            {
                _multiColumnHeaderState.columns[1].width = position.width - 50;
            }

            if ( _tree == null )
            {
                _tree = new MyTreeView( _treeViewState, new MyMultiColumnHeader(_multiColumnHeaderState), property.FindPropertyRelative( "SerializableNodes" )) ;
                _tree.Reload();
            }

            
            _tree.OnGUI( position );

            // var labelX = position.x;
            // position    = EditorGUI.PrefixLabel( position, label );
            //
            // SerializedProperty array = property.FindPropertyRelative( "SerializableNodes" );
            // position.height = EditorGUIUtility.singleLineHeight;
            //
            // if( array.arraySize == 0 )
            // {
            //     GUI.Label( position, $"Empty {property.type}" );
            //     return;
            // }
            //
            // var index = 0;
            // DrawLevel( labelX, ref position, 0, array, ref index );
        }


        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            if( _tree == null )
                return EditorGUIUtility.singleLineHeight;

            return Mathf.Clamp( _tree.GetContentHeight() + _tree.multiColumnHeader.height + 5, EditorGUIUtility.singleLineHeight, 500 );
        }
    }
}