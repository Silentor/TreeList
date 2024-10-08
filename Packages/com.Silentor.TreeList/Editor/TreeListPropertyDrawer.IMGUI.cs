﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Silentor.TreeList.Editor
{
    /// <summary>
    /// IMGUI version of TreeListPropertyDrawer
    /// </summary>
    public partial class TreeListPropertyDrawer
    {                                   
        private static readonly Dictionary<Int32, TreeEditorState> _treeViewStates = new ();
        private                 ImguiTreeView                      _treeIM;
        private readonly        Single                             _headerHeight = EditorGUIUtility.singleLineHeight + 2;
        private                 Boolean                            _expandTreeOnInitialize;
        private                 Int32                              _selectIndexOnInitialize = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            //Debug.Log( $"{property.displayName}, evt {Event.current.type}, hash {this.GetHashCode()}" );

            var state     = GetPersistentTreeViewState( property );
            var nodesProp = property.FindPropertyRelative( "_nodes" );
            if( nodesProp.arraySize == 0 )
                property.isExpanded = false;

            if ( _treeIM == null )
            {
                _treeIM          =  new ImguiTreeView( state.TreeViewState, nodesProp ) ;
                _treeIM.MoveNode += (nodeIndex, parentIndex, childIndex ) =>
                {
                    MoveItem( nodeIndex, parentIndex, childIndex, nodesProp );
                };
                _treeIM.CopyNode += (nodeIndex, parentIndex, childIndex ) =>
                {
                    CopyItem( nodeIndex, parentIndex, childIndex, nodesProp );
                };
            }

            position = DrawHeader( position, label, state, nodesProp, property );

            if ( !property.isExpanded )
            {
                return;
            }

            if ( !_treeIM.IsInitialized || _structuralHash != GetStructuralHash( nodesProp ) )
            {
                _treeIM.Reload();
                _structuralHash = GetStructuralHash( nodesProp );
            }

            if( _expandTreeOnInitialize )
            {
                _treeIM.ExpandAll();
                _expandTreeOnInitialize = false;
            }

            if( _selectIndexOnInitialize >= 0 )
            {
                _treeIM.SetFocusAndEnsureSelectedItem();
                _treeIM.SetSelectedItem( _selectIndexOnInitialize );
                _selectIndexOnInitialize = -1;
            }

            _treeIM.OnGUI( position );
        }

        private Rect DrawHeader( Rect totalRect, GUIContent label, TreeEditorState state, SerializedProperty nodesProp, SerializedProperty mainProperty )
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
            var isBtnEnabled = mainProperty.isExpanded && _treeIM.HasSelection();
            EditorGUI.BeginDisabledGroup( !isBtnEnabled );
            if ( GUI.Button( btnRect, ResourcesIMGUI.Minus, ResourcesIMGUI.ToolBarBtnStyle ) )
            {
                var (_, selectedIndex) = _treeIM.GetSelectedItem();
                RemoveItem( selectedIndex, nodesProp );
                if( nodesProp.arraySize == 0 )
                    mainProperty.isExpanded = false;
            }
            EditorGUI.EndDisabledGroup();

            //Draw add button
            btnRect = new Rect( btnRect.x - btnRect.width - 5, btnRect.y, btnRect.width, btnRect.height );
            var isEmptyTree = nodesProp.arraySize == 0;
            isBtnEnabled = isEmptyTree || _treeIM.HasSelection();
            EditorGUI.BeginDisabledGroup( !isBtnEnabled );
            if ( GUI.Button( btnRect, ResourcesIMGUI.Plus, ResourcesIMGUI.ToolBarBtnStyle  ) )
            {
                if ( isEmptyTree )
                {
                    AddItem( -1,  nodesProp );
                    mainProperty.isExpanded = true;
                }
                else
                {
                    var (_, selectedIndex) = _treeIM.GetSelectedItem();
                    AddItem( selectedIndex, nodesProp );
                }
            };
            EditorGUI.EndDisabledGroup();

            //Draw expand/collapse button
            btnRect = new Rect( btnRect.x - btnRect.width - 5, btnRect.y, btnRect.width, btnRect.height );
            isBtnEnabled = !isEmptyTree;
            EditorGUI.BeginDisabledGroup( !isBtnEnabled );
            if ( GUI.Button( btnRect, ResourcesIMGUI.Expand, ResourcesIMGUI.ToolBarBtnStyle  ) )
            {
                if ( !mainProperty.isExpanded )
                {
                    mainProperty.isExpanded = true;
                    if( _treeIM.IsInitialized )
                        _treeIM.ExpandAll();
                    else
                        _expandTreeOnInitialize = true;
                }
                else
                {
                    var expandable = 0;
                    var expanded = 0;
                    foreach ( var row in _treeIM.GetRows() )
                    {
                         if( !row.hasChildren )
                             continue;

                         expandable++;
                         var isExpanded = true;
                         var r          = row;
                         for ( int i = 0; i < 5 && r != null; i++ )
                         {
                             if ( !_treeIM.GetExpanded().Contains( r.id ) )
                             {
                                 isExpanded = false;
                                 break;
                             }
                             r = r.parent;
                         }

                         if ( isExpanded )
                             expanded++;
                    }

                    if ( expanded > expandable / 2 )
                        _treeIM.CollapseAll();
                    else
                        _treeIM.ExpandAll();
                }
            };
            EditorGUI.EndDisabledGroup();

            //Draw search field
            var searchStringContent = new GUIContent( state.SearchString );
            var searchStringWidth   = Mathf.Clamp( GUI.skin.textField.CalcSize( searchStringContent ).x, 30, 100 );
            var searchFieldRect     = new Rect( btnRect.x - searchStringWidth - 5, btnRect.y, searchStringWidth, btnRect.height );
            state.SearchString = GUI.TextField( searchFieldRect, state.SearchString );
            btnRect            = new Rect( searchFieldRect.x - btnRect.width, searchFieldRect.y, btnRect.height, btnRect.height );
            if ( GUI.Button( btnRect, ResourcesIMGUI.Search, ResourcesIMGUI.ToolBarBtnStyle ) )
            {
                if ( state.SearchString.Length > 0 )
                {
                    var searchFromIndex = _treeIM.HasSelection() ? _treeIM.GetSelectedItem().index : 0;
                    var newIndex        = SearchValue( state.SearchString, searchFromIndex + 1, nodesProp );
                    if ( newIndex >= 0 )
                    {
                        mainProperty.isExpanded = true;
                        //Delay selection to next frame to properly init tree if it was collapsed 
                        _selectIndexOnInitialize = newIndex;
                    }
                }
            }

            //Custom prefix label with foldout for entire tree
            var prefixLabelRect = headerRect;
            prefixLabelRect.xMax = btnRect.x - 5;
            var headerStyle = nodesProp.prefabOverride ? ResourcesIMGUI.HeaderOverridenStyle : ResourcesIMGUI.HeaderStyle;
            //var headerWidth = headerStyle.CalcSize( label ).x;
            //prefixLabelRect.width = headerWidth;
            if( nodesProp.arraySize > 0 )
                mainProperty.isExpanded = EditorGUI.Foldout( prefixLabelRect, mainProperty.isExpanded, label, true, headerStyle );   
            else
            {
                EditorGUI.Foldout( prefixLabelRect, false, label, true, headerStyle );
                mainProperty.isExpanded = false;
            }

            //Draw header hint
            var labelRect = headerRect;
            labelRect.xMin = prefixLabelRect.x + EditorGUIUtility.labelWidth;
            labelRect.xMax = btnRect.xMin;
            var isDragging = _treeIM.IsItemDragged;
            var hintString = GetTreeHint( _treeIM.HasSelection() ? 0 : -1, isDragging, nodesProp );
            var hintContent = new GUIContent( hintString, tooltip: hintString );
            GUI.Label( labelRect, hintContent, ResourcesIMGUI.HintStyle);

            //Draw blue margin if tree value is overriden
            if ( nodesProp.prefabOverride && Event.current.type == EventType.Repaint )
            {
                EditorGUI.DrawRect( new Rect(headerRect.x - 18, headerRect.y, 2, headerRect.height), ResourcesIMGUI.PrefabOverrideMarginColor);
            }

            //Simulate default property context menu
            if ( Event.current.type == EventType.ContextClick && prefixLabelRect.Contains (Event.current.mousePosition) )
            {
                ShowPropertyContextMenuIMGUI( mainProperty );
            }

            return contentRect;
        }

        internal static void ShowPropertyContextMenuIMGUI( SerializedProperty property )
        {
            property = property.Copy();             //Defend against property iteration
            var menu = new GenericMenu();
            menu.AddItem( new GUIContent( "Copy property path" ), false, () => EditorGUIUtility.systemCopyBuffer = property.propertyPath );
            if ( property.prefabOverride )
            {
                //Find prefabs where this property may be overriden
                var obj    = ((Component)property.serializedObject.targetObject).gameObject;
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource( obj );  //Make sure we in prefab space
            
                List<GameObject> prefabAssets = new();
                do
                {
                    var originalPrefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource( prefab );
                    if( originalPrefabAsset )
                        prefabAssets.Add( originalPrefabAsset );
                    prefab = prefab.transform.parent?.gameObject;
                } while ( prefab );
            
                prefabAssets.Reverse();
                foreach ( var prefabAsset in prefabAssets )
                {
                    var title = prefabAsset == prefabAssets.Last() ? $"Apply to Prefab '{prefabAsset.name}'" : $"Apply as Override in Prefab '{prefabAsset.name}'";
                    if( prefabAsset == prefabAssets.Last() )
                        menu.AddItem( new GUIContent( title ), false, 
                                () => PrefabUtility.ApplyPropertyOverride( property, AssetDatabase.GetAssetPath( prefabAsset ), InteractionMode.UserAction ) );
                    else
                        menu.AddItem( new GUIContent( title ), false, 
                                () => PrefabUtility.ApplyPropertyOverride( property, AssetDatabase.GetAssetPath( prefabAsset ), InteractionMode.UserAction ) );
                }
            
                menu.AddItem( new GUIContent( "Revert" ), false, ( ) => PrefabUtility.RevertPropertyOverride( property, InteractionMode.UserAction ) );
            }
            menu.AddSeparator( String.Empty );
            
            if( !property.hasMultipleDifferentValues )
                menu.AddItem( new GUIContent( "Copy" ), false, () => Clipboard.Copy( property ) );
            else
                menu.AddDisabledItem( new GUIContent( "Copy" ) );
            if( !property.hasMultipleDifferentValues && GUI.enabled && Clipboard.IsPropertyPresent() )
                menu.AddItem( new GUIContent( "Paste" ), false, () =>
                {
                    Clipboard.Paste( property );
                    property.serializedObject.ApplyModifiedProperties();
                } );
            else
                menu.AddDisabledItem( new GUIContent( "Paste" ) );
            
            menu.ShowAsContext();
            Event.current.Use ();
        }

        public override Single GetPropertyHeight( SerializedProperty property, GUIContent label )
        {
            if( _treeIM == null || !_treeIM.IsInitialized  )
                return _headerHeight;
            if( !property.isExpanded || property.FindPropertyRelative( "_nodes" ).arraySize == 0)
                return _headerHeight;

            return Mathf.Clamp( _treeIM.totalHeight + _headerHeight, _headerHeight, Screen.height * 2/3 );
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

        [Serializable]
        private class TreeEditorState
        {
            public TreeViewState TreeViewState = new();
            public String        SearchString;
        }      

        private static class ResourcesIMGUI
        {
            public static readonly GUIStyle HeaderStyle = new (EditorStyles.foldoutHeader)
                                                          {
                                                                  fontStyle = FontStyle.Normal,
                                                          };
            public static readonly GUIStyle HeaderOverridenStyle = new (EditorStyles.foldoutHeader)
                                                          {
                                                                  fontStyle = FontStyle.Bold,
                                                          };
            public static readonly GUIStyle ToolBarBtnStyle = new (GUI.skin.button) {       
                                                                                            alignment = TextAnchor.MiddleCenter,
                                                                                            fontSize = 16,
                                                                                            padding = new RectOffset()
                                                                                    };
            public static readonly GUIStyle HintStyle = new (EditorStyles.centeredGreyMiniLabel) 
                                                                                    {       
                                                                                            alignment = TextAnchor.LowerLeft,
                                                                                    };
           
            public static readonly GUIContent Plus  = new  (EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_Toolbar Plus").image : EditorGUIUtility.IconContent("Toolbar Plus").image, "Add child node") ;
            public static readonly GUIContent Minus = new  (EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_Toolbar Minus").image : EditorGUIUtility.IconContent("Toolbar Minus").image, "Remove node") ;
            public static readonly GUIContent Expand = new  (EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_UnityEditor.SceneHierarchyWindow").image : EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow").image, "Expand/collapse tree") ;
            public static readonly GUIContent Search = new  ( EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_Search Icon").image : EditorGUIUtility.IconContent("Search Icon").image , "Search value") ;
            // public static readonly GUIContent Depth =  EditorGUIUtility.isProSkin 
            //         ? new ("Depth", EditorGUIUtility.IconContent("d_BlendTree Icon").image) 
            //         : new ("Depth", EditorGUIUtility.IconContent("BlendTree Icon").image) ;

            

            public static readonly Color PrefabOverrideMarginColor = new (0.003921569f, 0.6f, 0.92156863f, 0.75f);
        }
    }
}