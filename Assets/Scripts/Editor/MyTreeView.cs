using System;
using System.Collections.Generic;
using System.Linq;
using MyBox.EditorTools;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = System.Object;

namespace Silentor.TreeControl.Editor
{
    public class MyTreeView : TreeView
    {
        private readonly SerializedProperty _itemsProp;
        private          Single              _totalContentHeight;

        public MyTreeView(TreeViewState state, SerializedProperty itemsProp ) : base( state )
        {
            _itemsProp = itemsProp;
        }

        public MyTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, SerializedProperty itemsProp  ) : base( state, multiColumnHeader )
        {
            _itemsProp                   = itemsProp;
            showBorder                   = true;
        }

        protected override TreeViewItem BuildRoot( )
        {
            var root = new TreeViewItem {id = -1, depth = -1, displayName = "Root"};

            var itemsList = new List<TreeViewItem>();
            for ( int i = 0; i < _itemsProp.arraySize; i++ )
            {
                var itemProp = _itemsProp.GetArrayElementAtIndex( i );
                var depth    = itemProp.FindPropertyRelative( "Level" ).intValue;
                //var value    = itemProp.FindPropertyRelative( "Value" ).GetValue().ToString();

                itemsList.Add( new TreeViewItem {id = i, depth = depth, /*displayName = value */} );
            }
            
            
            // Utility method that initializes the TreeViewItem.children and .parent for all items.
            SetupParentsAndChildrenFromDepths (root, itemsList);
            
            // Return root of the tree
            return root;
        }

        protected override void BeforeRowsGUI( )
        {
            base.BeforeRowsGUI();
            _totalContentHeight = 0;
        }

        protected override void RowGUI(RowGUIArgs args )
        {
            var item = args.item;

            for (int i = 0; i < args.GetNumVisibleColumns (); ++i)
            {
                var colIndex = args.GetColumn(i);

                if ( colIndex == 0 )
                {
                    base.RowGUI( args );      //Foldout
                }
                else
                {
                    //Draw serialized property value
                    var levelProp = _itemsProp.GetArrayElementAtIndex( item.id ).FindPropertyRelative( "Level" );
                    var valueProp = _itemsProp.GetArrayElementAtIndex( item.id ).FindPropertyRelative( "Value" );
                    var totalRect = args.GetCellRect( i );

                    if ( valueProp == null )
                    {
                        var label =  String.Concat( Enumerable.Repeat( "    ", levelProp.intValue )) + "Value is not serializable";
                        GUI.Label( totalRect, label );
                        return;
                    }

                    var oneLineRect = totalRect;
                    oneLineRect.height = EditorGUIUtility.singleLineHeight;

                    if ( valueProp.hasVisibleChildren )
                    {
                        var enterChildren = true;
                        var endProp       = valueProp.GetEndProperty();
                        while ( valueProp.NextVisible( enterChildren ) && !SerializedProperty.EqualContents( valueProp, endProp ) )
                        {
                            var label =  String.Concat( Enumerable.Repeat( "    ", levelProp.intValue )) + valueProp.displayName;
                            EditorGUI.PropertyField( oneLineRect, valueProp, new GUIContent( label ) );
                            enterChildren =  false;
                            oneLineRect.y += EditorGUIUtility.singleLineHeight;
                        }
                    }
                    else   //Value is primitive type itself
                    {
                        var label =  String.Concat( Enumerable.Repeat( "    ", levelProp.intValue )) + valueProp.displayName;
                        EditorGUI.PropertyField( oneLineRect, valueProp, new GUIContent( label ) );
                    }

                    _totalContentHeight += totalRect.y;

                }
            }
        }

        protected override Single GetCustomRowHeight(Int32 row, TreeViewItem item )
        {
            var valueProp     = _itemsProp.GetArrayElementAtIndex( item.id ).FindPropertyRelative( "Value" );
            if( valueProp == null )              //Value is not serializable
                return EditorGUIUtility.singleLineHeight;

            var enterChildren = true;
            var endProp       = valueProp.GetEndProperty();
            var count         = 0;
            while ( valueProp.NextVisible( enterChildren ) && !SerializedProperty.EqualContents( valueProp, endProp ) )
            {
                count++;
                enterChildren =  false;
            }
            return Math.Max( count, 1) * EditorGUIUtility.singleLineHeight;
        }

        public Single GetExpandedItemHeight( )
        {
            if( _itemsProp.arraySize == 0 || GetRows().Count == 0 )
                return EditorGUIUtility.singleLineHeight;

            return GetCustomRowHeight( 0, GetRows()[ 0 ] );
        }

        public Single GetContentHeight( )
        {
            var expandedItemsHeight = GetExpandedItemHeight();
            var totalContentHeight  = 0f;
            foreach ( var treeViewItem in GetRows() )
            {
                if( treeViewItem == rootItem )
                    continue;

                //Root items and expanded items should draw all content
                if ( treeViewItem.depth == 0 || state.expandedIDs.Contains( treeViewItem.parent.id ) ) 
                    totalContentHeight += expandedItemsHeight;
            }
            return totalContentHeight;
        }
    }

    public class MyMultiColumnHeader : MultiColumnHeader
    {
        public MyMultiColumnHeader(MultiColumnHeaderState state) : base(state)
        {
            height                        = 16;
            canSort                       = false;
            allowDraggingColumnsToReorder = false;
        }
    }
}