﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = System.Object;

namespace Silentor.TreeList.Editor
{
    public class ImguiTreeView : TreeView
    {
        public Boolean IsInitialized => base.isInitialized;

        private readonly SerializedProperty _itemsProp;
        private          Single             _contentHeight;
        private          Single             _lastContentHeight = -1;
        private readonly List<Int32>        _idLevelList       = new ( 16 );

        public ImguiTreeView(TreeViewState state, SerializedProperty itemsProp ) : base( state )
        {
            _itemsProp = itemsProp;
        }

        public ImguiTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, SerializedProperty itemsProp  ) : base( state , multiColumnHeader  )
        {
            _itemsProp                    = itemsProp;
            showBorder                    = true;
            showAlternatingRowBackgrounds = true;
        }

        protected override TreeViewItem BuildRoot( )
        {
            var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };

            var itemsList   = new List<TreeViewItem>();
            _idLevelList.Clear();
            for ( var i = 0; i < _itemsProp.arraySize; i++ )
            {
                var itemProp = _itemsProp.GetArrayElementAtIndex( i );
                var depth    = itemProp.FindPropertyRelative( "_depth" ).intValue;

                var semiPermanentId = (depth << 16) | GetIndexForDepth( depth );
                itemsList.Add( new TreeViewItem { id = semiPermanentId, depth = depth } );
            }
            
            // Utility method that initializes the TreeViewItem.children and .parent for all items.
            SetupParentsAndChildrenFromDepths (root, itemsList);

            //Debug.Log( "Reload tree" );

            // Return root of the tree
            return root;
        }

        protected override void BeforeRowsGUI( )
        {
            base.BeforeRowsGUI();

            _contentHeight = 0;
        }

        protected override void RowGUI(RowGUIArgs args )
        {
            var item = args.item;

            for (var i = 0; i < args.GetNumVisibleColumns (); ++i)
            {
                var colIndex = args.GetColumn(i);

                if ( colIndex == 0 )
                {
                    base.RowGUI( args );                    //Draw foldout toggle

                    //Print depth label
                    var totalRect = args.GetCellRect( i );
                    
                    if( item.depth > 0 && Event.current.type == EventType.Repaint ) 
                        Resources.DepthLabelStyle.Draw( totalRect,  new GUIContent(item.depth.ToString()), false, false, args.selected, args.focused );

                    //Draw  value
                    var (nodeProp, _)  = GetIndexFromId( item.id );
                    var levelProp = nodeProp.FindPropertyRelative( "_depth" );
                    var valueProp = nodeProp.FindPropertyRelative( "Value" );

                    //Make indent for content
                    totalRect.xMin += (14 * levelProp.intValue) + 30;

                    if ( valueProp == null )
                    {
                        GUI.Label( totalRect, "Value is not serializable" );
                        return;
                    }

                    var valuePropRect = totalRect;
                    if ( valueProp.hasVisibleChildren )
                    {
                        var enterChildren = true;
                        var endProp       = valueProp.GetEndProperty();
                        while ( valueProp.NextVisible( enterChildren ) && !SerializedProperty.EqualContents( valueProp, endProp ) )
                        {
                            enterChildren   =  false;
                            var valuePropLabel = new GUIContent( valueProp.displayName );
                            var propHeight     = EditorGUI.GetPropertyHeight( valueProp, valuePropLabel );
                            valuePropRect.height = propHeight;
                            EditorGUI.PropertyField( valuePropRect, valueProp, valuePropLabel, valueProp.isExpanded );
                            valuePropRect.y += propHeight;
                            _contentHeight  += propHeight;
                        }
                    }
                    else   //Value is primitive type itself
                    {
                        EditorGUI.PropertyField( valuePropRect, valueProp, new GUIContent( valueProp.displayName ) );
                    }
                }
            }
        }

        protected override void AfterRowsGUI( )
        {
            base.AfterRowsGUI();

            //To react on dynamic content height changes
            if( _lastContentHeight >= 0 )
            {
                if( Math.Abs( _lastContentHeight - _contentHeight ) > 1 )
                    RefreshCustomRowHeights();
            }

            _lastContentHeight = _contentHeight;
            _contentHeight     = 0;
        }

        protected override Single GetCustomRowHeight(Int32 row, TreeViewItem item )
        {
            var (nodeProp, _) = GetIndexFromId( item.id );
            var valueProp     = nodeProp.FindPropertyRelative( "Value" );
            if( valueProp == null )              //Value is not serializable
                return EditorGUIUtility.singleLineHeight;

            var enterChildren = true;
            var endProp       = valueProp.GetEndProperty();
            var height        = 0f;
            while ( valueProp.NextVisible( enterChildren ) && !SerializedProperty.EqualContents( valueProp, endProp ) )
            {
                height += EditorGUI.GetPropertyHeight( valueProp, valueProp.isExpanded );
                enterChildren =  false;
            }

            return Math.Max( height, EditorGUIUtility.singleLineHeight );
        }

        public (SerializedProperty nodeProp, Int32 index) GetSelectedItem( )
        {
            if ( HasSelection() )
            {
                var id       = GetSelection().First();
                var nodeProp = GetIndexFromId( id );
                return nodeProp;
            }

            return (null, -1);
        }

        public void SetSelectedItem( Int32 index )
        {
            var id = GetIdFromIndex( index );
            SetSelection( new[] { id } );
            FrameItem( id );
        }

        private Int32 GetIdFromIndex( Int32 index )
        {
            _idLevelList.Clear();
            for ( var i = 0; i < _itemsProp.arraySize; i++ )
            {
                var itemProp = _itemsProp.GetArrayElementAtIndex( i );
                var depth    = itemProp.FindPropertyRelative( "_depth" ).intValue;

                var semiPermanentId = (depth << 16) | GetIndexForDepth( depth );
                if ( i == index )
                    return semiPermanentId;
            }

            return -1;
        }

        private (SerializedProperty nodeProp, Int32 index) GetIndexFromId( Int32 id )
        {
            var depth        = id >> 16;
            var indexInDepth = id & 0xFFFF;
            var localIndex        = 0;
            for ( int i = 0; i < _itemsProp.arraySize; i++ )
            {
                var nodeProp = _itemsProp.GetArrayElementAtIndex( i );
                var nodeDepth = nodeProp.FindPropertyRelative( "_depth" ).intValue;
                if ( nodeDepth == depth )
                {
                    if ( localIndex++ == indexInDepth )
                        return ( nodeProp, i );
                }
            }

            return (null, -1);
        }

        private Int32 GetIndexForDepth( Int32 depth )
        {
            while( _idLevelList.Count <= depth )
                _idLevelList.Add( 0 );
                
            var id = _idLevelList[ depth ];
            _idLevelList[ depth ] += 1;
            return id;
        }

        private static class Resources
        {
            public static readonly GUIStyle DepthLabelStyle = new (TreeView.DefaultStyles.label)
                                                              {
                                                                      fontSize = 9,
                                                                      normal = {textColor = Color.gray}
                                                              };

            // public static readonly GUIStyle DepthLabelStyle0 = new (GUI.skin.label) {alignment = TextAnchor.UpperRight, 
            //                                                                                 normal = new GUIStyleState(){textColor = Color.gray},
            //                                                                                 hover = new GUIStyleState(){textColor = Color.gray},
            //                                                                         };
            // public static readonly GUIStyle DepthLabelStyle = new (GUI.skin.label) {alignment = TextAnchor.UpperLeft, 
            //                                                                                normal = new GUIStyleState(){textColor = Color.gray},
            //                                                                                hover = new GUIStyleState(){textColor = Color.gray},
            //                                                                       };
        }
    }

    public class MyMultiColumnHeader : MultiColumnHeader
    {
        public MyMultiColumnHeader(MultiColumnHeaderState state) : base(state)
        {
            height                        = 0;              //Hide header
            canSort                       = false;
            allowDraggingColumnsToReorder = false;
        }
    }
}