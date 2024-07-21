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
        private static readonly Dictionary<Int32, TreeEditorState> _treeViewStates = new ();
        private                 MyTreeView                   _tree;
        private                 MultiColumnHeaderState       _multiColumnHeaderState;
        private                 Int32                        _structuralHash;
        private Single _headerHeight = EditorGUIUtility.singleLineHeight + 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var state     = GetPersistentTreeViewState( property );
            var nodesProp = property.FindPropertyRelative( "SerializableNodes" );
            if( nodesProp.arraySize == 0 )
                state.IsTreeExpanded = false;

            if ( _tree == null )
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

                _tree = new MyTreeView( state.TreeViewState, new MyMultiColumnHeader(_multiColumnHeaderState), nodesProp ) ;
            }

            position = DrawHeader( position, label, state, nodesProp );

            if ( !state.IsTreeExpanded )
            {
                return;
            }
            
            if ( _structuralHash != GetStructuralHash( nodesProp ) )
            {
                _tree.Reload();
                _structuralHash = GetStructuralHash( nodesProp );
            }

            //Adjust columns
            var expandedItems = _tree.GetRows().Where( tvi => _tree.GetExpanded().Contains( tvi.id ) ).ToArray();
            if ( expandedItems.Length > 0 )
            {
                //Expand foldouts column to fit the deepest expanded item
                var deepestExpandedItem = expandedItems.MaxBy( tvi => tvi.depth );
                _multiColumnHeaderState.columns[0].width = deepestExpandedItem.depth * 15 + 30;
            }
            else
                _multiColumnHeaderState.columns[0].width = 30;
                    
            _multiColumnHeaderState.columns[1].width = position.width - _multiColumnHeaderState.columns[0].width - 20;

            _tree.OnGUI( position );
        }

        private Rect DrawHeader( Rect totalRect, GUIContent label, TreeEditorState state, SerializedProperty nodesProp )
        {
            var headerRect  = new Rect( totalRect.x, totalRect.y,                totalRect.width, _headerHeight );
            var contentRect = new Rect( totalRect.x, totalRect.y + _headerHeight, totalRect.width, totalRect.height - _headerHeight );
            
            //Draw items count
            var itemsCountRect = new Rect( headerRect.x + headerRect.width - 50, headerRect.y, 50, _headerHeight );
            GUI.enabled = false;
            GUI.TextField( itemsCountRect, nodesProp.arraySize.ToString() );
            GUI.enabled = true;

            //Draw remove button
            var btnRect      = new Rect( itemsCountRect.x - _headerHeight - 5, headerRect.y, _headerHeight, _headerHeight );
            var isBtnEnabled = state.IsTreeExpanded && _tree.HasSelection();
            EditorGUI.BeginDisabledGroup( !isBtnEnabled );
            if ( GUI.Button( btnRect, Resources.Minus, Resources.ToolBarBtnStyle ) )
            {
                var (_, selectedIndex) = _tree.GetSelectedItem();
                RemoveItem( selectedIndex, nodesProp );
                if( nodesProp.arraySize == 0 )
                    state.IsTreeExpanded = false;
            }
            EditorGUI.EndDisabledGroup();

            //Draw add button
            btnRect = new Rect( btnRect.x - btnRect.width - 5, btnRect.y, btnRect.width, btnRect.height );
            var isEmptyTree = nodesProp.arraySize == 0;
            isBtnEnabled = isEmptyTree || _tree.HasSelection();
            EditorGUI.BeginDisabledGroup( !isBtnEnabled );
            if ( GUI.Button( btnRect, Resources.Plus, Resources.ToolBarBtnStyle  ) )
            {
                if ( isEmptyTree )
                {
                    AddItem( -1,  nodesProp );
                    state.IsTreeExpanded = true;
                }
                else
                {
                    var (_, selectedIndex) = _tree.GetSelectedItem();
                    AddItem( selectedIndex, nodesProp );
                }
            };
            EditorGUI.EndDisabledGroup();

            //Custom prefix label because TreeView with header hides it. wtf?
            var prefixLabelRect = headerRect;
            prefixLabelRect.xMax = btnRect.x - 5;
            if( nodesProp.arraySize > 0 )
                state.IsTreeExpanded = EditorGUI.Foldout( headerRect, state.IsTreeExpanded, label, true, Resources.HeaderStyle );   
            else
            {
                EditorGUI.Foldout( headerRect, false, label, true, Resources.HeaderStyle);
                state.IsTreeExpanded = false;
            }   

            

            return contentRect;
        }

        private void AddItem( Int32 parentIndex, SerializedProperty nodes )
        {
            if ( parentIndex > -1 )
            {
                var parentDepth = nodes.GetArrayElementAtIndex( parentIndex ).FindPropertyRelative( "Level" ).intValue;
                nodes.InsertArrayElementAtIndex( parentIndex + 1 );
                var newItem = nodes.GetArrayElementAtIndex( parentIndex + 1  );
                newItem.FindPropertyRelative( "Level" ).intValue = parentDepth + 1;
            }
            else
                nodes.InsertArrayElementAtIndex( 0 );
        }

        private void RemoveItem( Int32 itemIndex, SerializedProperty nodes )
        {
            //Delete item and all its children based on depth
            var depth = nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "Level" ).intValue;
            nodes.DeleteArrayElementAtIndex( itemIndex );
            while ( nodes.arraySize > itemIndex )
            {
                if( nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "Level" ).intValue > depth )
                    nodes.DeleteArrayElementAtIndex( itemIndex );
                else
                    break;
            }
        }

        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            if( _tree == null || !_tree.IsInitialized  )
                return _headerHeight;
            if( !GetPersistentTreeViewState( property ).IsTreeExpanded || property.FindPropertyRelative( "SerializableNodes" ).arraySize == 0)
                return _headerHeight;

            return Mathf.Clamp( _tree.totalHeight + _headerHeight, _headerHeight, 500 );
        }

        private TreeEditorState GetPersistentTreeViewState( SerializedProperty prop )
        {
            var key = HashCode.Combine( prop.serializedObject.targetObject.GetInstanceID(), prop.propertyPath.GetHashCode() );
            if ( !_treeViewStates.TryGetValue( key, out var result ) )
            {
                result = new TreeEditorState();
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

        [Serializable]
        private class TreeEditorState
        {
            public TreeViewState    TreeViewState = new();
            public Boolean          IsTreeExpanded;
        }

        private static class Resources
        {
             public static readonly GUIStyle HeaderStyle = new (EditorStyles.foldoutHeader) { };
            public static readonly GUIStyle ToolBarBtnStyle = new (GUI.skin.button) {       
                                                                                            alignment = TextAnchor.MiddleCenter,
                                                                                            fontSize = 16,
                                                                                            padding = new RectOffset()
                                                                                    };

            public static readonly GUIContent Plus  = new  (EditorGUIUtility.IconContent("Toolbar Plus").image, "Add child node") ;
            public static readonly GUIContent Minus = new  (EditorGUIUtility.IconContent("Toolbar Minus").image, "Remove node") ;

        }
    }
}