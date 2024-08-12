using System;
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
        public Boolean IsInitialized    => base.isInitialized;

        public Boolean IsItemDragged    => _isDragging;

        private readonly SerializedProperty _itemsProp;
        private          Single             _contentHeight;
        private          Single             _lastContentHeight = -1;
        private          bool               _isDragging;

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

        public event Action<Int32, Int32, Int32> MoveNode; 
        public event Action<Int32, Int32, Int32> CopyNode; 

        protected override TreeViewItem BuildRoot( )
        {
            var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };

            var itemsList   = new List<TreeViewItem>();
            for ( var i = 0; i < _itemsProp.arraySize; i++ )
            {
                var itemProp = _itemsProp.GetArrayElementAtIndex( i );
                var depth    = itemProp.FindPropertyRelative( "_depth" ).intValue;
                var id = i;
                itemsList.Add( new TreeViewItem { id = id, depth = depth } );
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

            //Debug.Log( $"Before {Event.current.type}" );
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

            if( Event.current.type == EventType.DragExited )
                _isDragging = false;
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
            return index;
        }

        private (SerializedProperty nodeProp, Int32 index) GetIndexFromId( Int32 id )
        {
            var node =  _itemsProp.GetArrayElementAtIndex( id );
            return (node, id);
        }

        protected override Boolean CanStartDrag(CanStartDragArgs args )
        {
            return args.draggedItem.id != 0;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args )
        {
            //Debug.Log( "Setup drag" );

            if (hasSearch)
                return;

            _isDragging = true;
            DragAndDrop.PrepareStartDrag();
            var draggedTreeViewItem = GetRows().First( item => args.draggedItemIDs.Contains(item.id) );
            DragAndDrop.SetGenericData( "TreeListDrag", draggedTreeViewItem);
            DragAndDrop.objectReferences = new UnityEngine.Object[] { }; // this IS required for dragging to work
            var title = draggedTreeViewItem.displayName;
            DragAndDrop.StartDrag (title);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args )
        {
            if( Event.current.type != EventType.DragUpdated && Event.current.type != EventType.DragPerform )
                return DragAndDropVisualMode.None;

            var draggedItem = DragAndDrop.GetGenericData( "TreeListDrag" ) as TreeViewItem;
            if ( draggedItem == null )
                return DragAndDropVisualMode.None;

            var dropTarget = args.parentItem;
            if ( dropTarget == null || dropTarget.depth < 0 )
                return DragAndDropVisualMode.None;

            //var draggedNode = GetIndexFromId( draggedItem.id ).nodeProp;
            //var dropNode = GetIndexFromId( dropTarget.id ).nodeProp;
            //Debug.Log( $"item {draggedNode.displayName}, parent {dropNode.displayName}, pos {args.dragAndDropPosition}, child index {args.insertAtIndex}, control {DragAndDrop.activeControlID}, frame {Time.frameCount}" );

            if ( IsParentRecursive( dropTarget, draggedItem ) )
                return DragAndDropVisualMode.None;

            if ( args.performDrop )
            {                       
                var nodeIndex   = GetIndexFromId( draggedItem.id ).index;
                var parentIndex = GetIndexFromId( dropTarget.id ).index;
                var childIndex  = args.insertAtIndex;

                if ( Event.current.control )
                    CopyNode?.Invoke( nodeIndex, parentIndex, childIndex );
                else
                    MoveNode?.Invoke( nodeIndex, parentIndex, childIndex );

                Debug.Log( $"Dropped {nodeIndex} to {parentIndex} at {childIndex}" );
                DragAndDrop.AcceptDrag();
                Event.current.Use();
                _isDragging = false;
                return DragAndDropVisualMode.None;
            }

            if ( Event.current.control )
                return DragAndDropVisualMode.Copy;
            else
                return DragAndDropVisualMode.Move;

            Boolean IsParentRecursive( TreeViewItem node, TreeViewItem parent )
            {
                if ( node == null || parent == null )
                    return false;

                if ( node.id == parent.id )
                    return true;

                return IsParentRecursive( node.parent, parent );
            }
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