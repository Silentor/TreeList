using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Silentor.TreeControl.Editor
{
    public class MyTreeView : TreeView
    {
        private readonly SerializedProperty _itemsProp;

        public MyTreeView(TreeViewState state, SerializedProperty itemsProp ) : base( state )
        {
            _itemsProp = itemsProp;
        }

        public MyTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, SerializedProperty itemsProp  ) : base( state , multiColumnHeader  )
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

                itemsList.Add( new TreeViewItem { id = i, depth = depth } );
            }
            
            // Utility method that initializes the TreeViewItem.children and .parent for all items.
            SetupParentsAndChildrenFromDepths (root, itemsList);
            
            // Return root of the tree
            return root;
        }

        protected override void RowGUI(RowGUIArgs args )
        {
            var item = args.item;

            for (int i = 0; i < args.GetNumVisibleColumns (); ++i)
            {
                var colIndex = args.GetColumn(i);

                if ( colIndex == 0 )
                {
                    base.RowGUI( args );                    //Foldout

                    //Print depth also
                    var rect = args.GetCellRect( i );
                    
                    if( item.depth > 0 )
                        DefaultGUI.Label( rect, item.depth.ToString(), args.selected, args.focused );     
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

        private static class Resources
        {
            public static readonly GUIStyle DepthLabelStyle0 = new (GUI.skin.label) {alignment = TextAnchor.UpperRight, 
                                                                                            normal = new GUIStyleState(){textColor = Color.gray},
                                                                                            hover = new GUIStyleState(){textColor = Color.gray},
                                                                                    };
            public static readonly GUIStyle DepthLabelStyle = new (GUI.skin.label) {alignment = TextAnchor.UpperLeft, 
                                                                                           normal = new GUIStyleState(){textColor = Color.gray},
                                                                                           hover = new GUIStyleState(){textColor = Color.gray},
                                                                                   };
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