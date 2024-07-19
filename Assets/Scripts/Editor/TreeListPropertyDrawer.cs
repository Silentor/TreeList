using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Silentor.TreeControl.Editor
{
    [CustomPropertyDrawer( typeof(TreeList<>), true )]
    public class TreeListPropertyDrawer : PropertyDrawer
    {                                   
        private static readonly Dictionary<Int32, TreeViewState> _treeViewStates = new ();
        private                 MyTreeView                       _tree;
        private                 MultiColumnHeaderState           _multiColumnHeaderState;
        private                 Int32                           _structuralHash;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var nodesProp = property.FindPropertyRelative( "SerializableNodes" );

            position = DrawHeader( position, label, nodesProp );

            if( _multiColumnHeaderState == null )
            {
                _multiColumnHeaderState = new MultiColumnHeaderState( 
                        new MultiColumnHeaderState.Column[]
                        {
                            new ()
                            {
                                    //headerContent         = new GUIContent( "Tree" ), 
                                    allowToggleVisibility = false,
                                    autoResize            = false,
                            },
                            new ()
                            {
                                    headerContent         = new GUIContent( "Value" ),
                                    allowToggleVisibility = false,
                                    autoResize            = true,
                            },
                        
                        } );

            }
            
            //Autoresize foldout column
            if ( _tree != null )
            {
                var expandedItems = _tree.GetRows().Where( tvi => _tree.GetExpanded().Contains( tvi.id ) ).ToArray();
                if ( expandedItems.Length > 0 )
                {
                    var deepestExpandedItem = expandedItems.MaxBy( tvi => tvi.depth );
                    _multiColumnHeaderState.columns[0].width = deepestExpandedItem.depth * 15 + 30;
                }
                else
                    _multiColumnHeaderState.columns[0].width = 30;
                    
                _multiColumnHeaderState.columns[1].width = position.width - _multiColumnHeaderState.columns[0].width - 20;
            }

            if ( _tree == null || _structuralHash != GetStructuralHash( nodesProp ) )
            {
                var state = GetPersistentTreeViewState( property );
                _tree = new MyTreeView( state, new MyMultiColumnHeader(_multiColumnHeaderState), nodesProp ) ;
                _tree.Reload();
                _structuralHash = GetStructuralHash( nodesProp );

                //Debug.Log( state.GetHashCode() );
            }

            _tree.OnGUI( position );
        }

        private Rect DrawHeader( Rect totalRect, GUIContent label, SerializedProperty nodesProp )
        {
            var headerHeight = EditorGUIUtility.singleLineHeight;
            var headerRect   = new Rect( totalRect.x, totalRect.y, totalRect.width, headerHeight );
            var contentRect  = new Rect( totalRect.x, totalRect.y + headerHeight, totalRect.width, totalRect.height - headerHeight );

            //Draw custom prefix label
            GUI.Label( headerRect, label );               //Custom prefix label because TreeView with header hides it. wtf?

            if ( _tree == null )
                return contentRect;

            //Draw items count
            var itemsCountRect = new Rect( headerRect.x + headerRect.width - 50, headerRect.y, 50, headerHeight );
            GUI.enabled = false;
            GUI.TextField( itemsCountRect, nodesProp.arraySize.ToString() );
            GUI.enabled = true;

            //Draw remove button
            var btnRect = new Rect( itemsCountRect.x - 50, headerRect.y, headerHeight, headerHeight );
            GUI.Button( btnRect, Resources.Minus, Resources.ToolBarBtnStyle );

            //Draw add button
            btnRect = new Rect( btnRect.x - btnRect.width - 5, btnRect.y, btnRect.width, btnRect.height );
            if ( GUI.Button( btnRect, Resources.Plus, Resources.ToolBarBtnStyle  ) && _tree.GetSelection().Any() )
            {
                var tvi = _tree.GetRows().FirstOrDefault( t => t.id == _tree.GetSelection().First() );
                if ( tvi != null )
                {
                    //Create instance of data type class and do an operation
                    //var treeList = new TreeList<>()
                    //nodesProp.NewArrayElementButton()
                }
            };

            

            return contentRect;
        }

        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            if( _tree == null || _tree.GetRows().Count == 0 )
                return EditorGUIUtility.singleLineHeight;

            return Mathf.Clamp( _tree.totalHeight + EditorGUIUtility.singleLineHeight, 2 * EditorGUIUtility.singleLineHeight, 500 );
        }

        private TreeViewState GetPersistentTreeViewState( SerializedProperty prop )
        {
            var key = HashCode.Combine( prop.serializedObject.targetObject.GetInstanceID(), prop.propertyPath.GetHashCode() );
            if ( !_treeViewStates.TryGetValue( key, out var result ) )
            {
                result = new TreeViewState();
                _treeViewStates.Add( key, result );
            }

            return result;
        }

        private Int32 GetStructuralHash( SerializedProperty nodesProp )
        {
            var hash = new HashCode();

            for ( int i = 0; i < nodesProp.arraySize; i++ )
            {
                var depthProp = nodesProp.GetArrayElementAtIndex( i ).FindPropertyRelative( "Level" );
                hash.Add( depthProp.intValue );
            }

            return hash.ToHashCode();
        }
        

        private static class Resources
        {
            public static readonly GUIStyle ToolBarBtnStyle = new (GUI.skin.button) {       
                                                                                            alignment = TextAnchor.MiddleCenter,
                                                                                            fontSize = 16,
                                                                                            padding = new RectOffset()
                                                                                    };

            public static readonly GUIContent Plus  = EditorGUIUtility.IconContent("Toolbar Plus");
            public static readonly GUIContent Minus = EditorGUIUtility.IconContent("Toolbar Minus");
           
        }
    }
}