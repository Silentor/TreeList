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
        public Boolean IsInitialized => base.isInitialized;

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
            var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };

            var idLevelList = new List<Int32>( 16 );
            var itemsList   = new List<TreeViewItem>();
            for ( var i = 0; i < _itemsProp.arraySize; i++ )
            {
                var itemProp = _itemsProp.GetArrayElementAtIndex( i );
                var depth    = itemProp.FindPropertyRelative( "Depth" ).intValue;

                var semiPermanentId = (depth << 16) | GetIndexForDepth( depth );
                itemsList.Add( new TreeViewItem { id = semiPermanentId, depth = depth } );
            }
            
            // Utility method that initializes the TreeViewItem.children and .parent for all items.
            SetupParentsAndChildrenFromDepths (root, itemsList);

            Debug.Log( "Reload tree" );

            // Return root of the tree
            return root;

            Int32 GetIndexForDepth( Int32 depth )
            {
                while( idLevelList.Count <= depth )
                    idLevelList.Add( 0 );
                
                var id = idLevelList[ depth ];
                idLevelList[ depth ] += 1;
                return id;
            }
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
                    var (nodeProp, _)  = GetNodePropForId( item.id );
                    var levelProp = nodeProp.FindPropertyRelative( "Depth" );
                    var valueProp = nodeProp.FindPropertyRelative( "Value" );
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
            var (nodeProp, _) = GetNodePropForId( item.id );
            var valueProp     = nodeProp.FindPropertyRelative( "Value" );
            if( valueProp == null )              //Value is not serializable
                return EditorGUIUtility.singleLineHeight;

            var enterChildren = true;
            var endProp       = valueProp.GetEndProperty();
            //var count         = 0;
            var height        = 0f;
            while ( valueProp.NextVisible( enterChildren ) && !SerializedProperty.EqualContents( valueProp, endProp ) )
            {
                height += EditorGUI.GetPropertyHeight( valueProp, true );
                //count++;
                enterChildren =  false;
            }
            //return Math.Max( count, 1) * EditorGUIUtility.singleLineHeight;
            return Math.Max( height, EditorGUIUtility.singleLineHeight );
        }

        public (SerializedProperty nodeProp, Int32 index) GetSelectedItem( )
        {
            if ( HasSelection() )
            {
                var id       = GetSelection().First();
                var nodeProp = GetNodePropForId( id );
                return nodeProp;
            }

            return (null, -1);
        }

        private (SerializedProperty nodeProp, Int32 index) GetNodePropForId( Int32 id )
        {
            var depth        = id >> 16;
            var indexInDepth = id & 0xFFFF;
            var localIndex        = 0;
            for ( int i = 0; i < _itemsProp.arraySize; i++ )
            {
                var nodeProp = _itemsProp.GetArrayElementAtIndex( i );
                var nodeDepth = nodeProp.FindPropertyRelative( "Depth" ).intValue;
                if ( nodeDepth == depth )
                {
                    if ( localIndex++ == indexInDepth )
                        return ( nodeProp, i );
                }
            }

            return (null, -1);
        }

        private static class Resources
        {
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