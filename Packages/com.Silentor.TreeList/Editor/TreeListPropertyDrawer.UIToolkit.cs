using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Silentor.TreeList.Editor
{
    /// <summary>
    /// UIToolkit version of TreeListPropertyDrawer
    /// </summary>
    public partial class TreeListPropertyDrawer
    {
        private Button    _removeBtn;
        private Button    _addBtn;
        private TreeView  _treeUI;
        private Label     _hint;
        private Button    _expandBtn;
        private Foldout   _foldout;
        private Button    _searchBtn;
        private TextField _searchValue;

        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            var root = ResourcesUITk.TreeViewAsset.CloneTree();
            _foldout = root.Q<Foldout>("Header");
            _foldout.text = property.displayName;
            _foldout.value = property.isExpanded;
            _foldout.BindProperty( property );

            var nodesProp           = property.FindPropertyRelative( "_nodes" );
            var elementsCounter = root.Q<TextField>( "Counter" );
            elementsCounter.BindProperty( nodesProp.FindPropertyRelative( "Array.size" ) );
            
            _treeUI = root.Q<TreeView>( "TreeView" );
            _treeUI.style.maxHeight               =  Screen.height * 2/3;
            _treeUI.viewDataKey                   =  GetPropertyPersistentString( property );
            _treeUI.showAlternatingRowBackgrounds =  AlternatingRowBackground.All;
            _treeUI.makeItem                      =  ( ) =>
            {
                var result =  new VisualElement() { };
                result.AddToClassList( "value-container" );
                return result;
            };
            _treeUI.bindItem = ( e, i ) =>
            {
                var treeItemProp = _treeUI.GetItemDataForIndex<SerializedProperty>( i );
                var valueProp    = treeItemProp.FindPropertyRelative( "Value" );

                //Draw content
                if ( valueProp == null )
                {
                    var notSerializableValueLbl = new Label( "Value is not serializable" );
                    e.Add( notSerializableValueLbl );
                }
                else if ( valueProp.hasVisibleChildren )
                {
                    var enterChildren = true;            
                    var endProp       = valueProp.GetEndProperty();
                    while ( valueProp.NextVisible( enterChildren ) && !SerializedProperty.EqualContents( valueProp, endProp ) )
                    {
                        var label     =  valueProp.displayName;
                        var propField = new PropertyField( valueProp, label );
                        propField.BindProperty( valueProp );
                        e.Add( propField );
                        enterChildren   =  false;
                    }
                }
                else   //Value is primitive type itself
                {
                    var label     =  valueProp.displayName;
                    var propField = new PropertyField( valueProp, label );
                    propField.BindProperty( valueProp );
                    e.Add( propField );
                }

                //Add node depth label
                var nodeDepth = treeItemProp.FindPropertyRelative( "_depth" ).intValue;
                var viewItem  = e;
                while ( viewItem.name != "unity-tree-view__item" )                
                    viewItem = viewItem.parent;
                var depthLabel = viewItem.Q<Label>( "DepthLabel" );
                if ( depthLabel == null )
                {
                    depthLabel = new Label( )
                                 {
                                         name = "DepthLabel",
                                         pickingMode = PickingMode.Ignore,
                                         style =
                                         {
                                                 position = Position.Absolute,
                                                 left     = 2,
                                                 top      = 3,
                                                 minWidth = 20,
                                                 maxWidth = 20,
                                         }
                                 };
                    depthLabel.AddToClassList( "unity-base-field__label" );
                    depthLabel.AddToClassList( "hint-label" );
                    viewItem.Add( depthLabel );
                }
                depthLabel.text = nodeDepth > 0 ? nodeDepth.ToString() : String.Empty;
            };
            _treeUI.unbindItem = ( e, i ) =>
            {
                e.Clear();
            };
            _treeUI.itemIndexChanged += ( oldIndex, newParentIndex ) =>          //Id is equal to index in unmodified source tree
            {
                var newChildIndex = _treeUI.viewController.GetChildIndexForId( oldIndex );  //viewController already modified, use old child index/id to get new child index

                //Debug.Log( $"[{nameof(TreeListPropertyDrawer)}] [itemIndexChanged] oldIndex {oldIndex}, new parent index {newParentIndex}, new child index {newChildIndex}" );

                if( Event.current.control )
                    CopyItem( oldIndex, newParentIndex, newChildIndex, nodesProp );
                else
                    MoveItem( oldIndex, newParentIndex, newChildIndex, nodesProp );
                nodesProp.serializedObject.ApplyModifiedProperties();
                RebuildTree( nodesProp );
            };
            _treeUI.selectionChanged += _ => { RefreshButtons( nodesProp ); };
            
            _treeUI.RegisterCallback<DragUpdatedEvent>(  _  => RefreshButtons( nodesProp, true ) );
            _treeUI.RegisterCallback<DragExitedEvent>(  _  => RefreshButtons( nodesProp ) );

            var hierarchy = BuildHierarchy( nodesProp ); 
            _treeUI.SetRootItems( hierarchy );
            _treeUI.Rebuild();
                    
            _removeBtn = root.Q<Button>( "RemoveBtn" );
            _removeBtn.style.backgroundImage = new StyleBackground( ResourcesUITk.Minus );
            _removeBtn.clickable.clicked += () =>
            {
                if ( _treeUI.selectedIndex > -1 )
                {
                    RemoveItem( _treeUI.selectedIndex, nodesProp );
                    property.serializedObject.ApplyModifiedProperties();
                    RebuildTree( nodesProp );
                    RefreshButtons( nodesProp );
                }
            };

            _addBtn = root.Q<Button>( "AddBtn" );
            _addBtn.style.backgroundImage = new StyleBackground( ResourcesUITk.Plus );
            _addBtn.clickable.clicked += () =>
            {
                if ( nodesProp.arraySize == 0 )
                {
                    AddItem( -1, nodesProp );
                    property.serializedObject.ApplyModifiedProperties();
                    property.isExpanded = true;
                    RebuildTree( nodesProp );
                    _treeUI.selectedIndex = 0;
                    RefreshButtons( nodesProp );
                }
                else if( _treeUI.selectedIndex > -1 )
                {
                    var addedIndex = AddItem( _treeUI.selectedIndex, nodesProp );
                    property.serializedObject.ApplyModifiedProperties();
                    RebuildTree( nodesProp );
                    RefreshButtons( nodesProp );
                }
            };

            _expandBtn = root.Q<Button>( "ExpandBtn" );
            _expandBtn.style.backgroundImage = new StyleBackground( ResourcesUITk.Expand );
            _expandBtn.clickable.clicked += () =>
            {
                if( nodesProp.arraySize == 0 )
                    return;

                if ( !property.isExpanded )
                {
                    property.isExpanded = true;
                    _foldout.value = true;
                    _treeUI.ExpandAll();
                }
                else
                {
                    var expandedCount = 0;
                    foreach ( var id in _treeUI.viewController.GetAllItemIds(  ) )
                    {
                        //Count fully expanded items
                        var isExpanded = true;
                        var checkId    = id;
                        for ( var i = 0; i < 5 && checkId >= 0; i++ )
                        {
                            if(  !_treeUI.IsExpanded( checkId ) )
                            {
                                isExpanded = false;
                                break;
                            }
                            checkId = _treeUI.viewController.GetParentId( checkId );
                        }
                        if ( isExpanded )
                        {
                            expandedCount++;
                        }
                    }
                    if(  expandedCount > _treeUI.GetTreeCount() / 2 )
                        _treeUI.CollapseAll();
                    else
                        _treeUI.ExpandAll();
                }
            };

            _searchValue = root.Q<TextField>( "SearchValue" );
            _searchBtn = root.Q<Button>( "SearchBtn" );
            _searchBtn.style.backgroundImage = new StyleBackground( ResourcesUITk.Search );
            _searchBtn.clickable.clicked += () =>
            {
                if( _searchValue.value.Length == 0 )
                    return;

                var startIndex = _treeUI.selectedIndex < 0 ? 0 : _treeUI.selectedIndex + 1;
                var itemId   = SearchValue( _searchValue.value, startIndex, nodesProp );
                if ( itemId >= 0 )
                {
                    property.isExpanded = true;
                    _foldout.value      = true;

                    //Expand finded item hierarchy to the root
                    var parentId = itemId;
                    while ( parentId != 0 )
                    {
                        parentId = _treeUI.viewController.GetParentId( parentId );
                        _treeUI.ExpandItem( parentId );                        
                    }

                    _treeUI.Focus();
                    var treeIndex = _treeUI.viewController.GetIndexForId( itemId );
                    _treeUI.selectedIndex = treeIndex;

                    //Delay scrolling because if tree was not expanded and actually binded, it will not scroll properly to the item
                    _foldout.schedule.Execute( ( ) =>
                    {
                        _treeUI.ScrollToItem( treeIndex );
                    } ).StartingIn( 33 );
                }
            };

            _hint = root.Q<Label>( "Hint" );

            root.RegisterCallback<GeometryChangedEvent>( _ => RefreshButtons( nodesProp )  );

            //Catch background tree structural changes (for example by undo or prefab revert)
            _structuralHash = GetStructuralHash( nodesProp );
            root.schedule.Execute( _ =>
            {
                var actualHash = GetStructuralHash( nodesProp );
                if ( actualHash != _structuralHash )
                {
                    //Debug.Log( $"hash mismatch, rebuild tree" );
                    _structuralHash = actualHash;
                    _treeUI.SetRootItems( BuildHierarchy( nodesProp ) );
                    _treeUI.RefreshItems();
                    RefreshButtons( nodesProp );
                }
            } ).Every( 500 );

            return root;
        }

        private void RebuildTree( SerializedProperty nodesProp )
        {
            _treeUI.SetRootItems( BuildHierarchy( nodesProp ) );
            _treeUI.RefreshItems();
            _structuralHash = GetStructuralHash( nodesProp );
        }

        /// <summary>
        /// Convert TreeNodeSerializable's to TreeViewItemData's
        /// </summary>
        /// <param name="nodesProp"></param>
        /// <returns></returns>
        private IList<TreeViewItemData<SerializedProperty>> BuildHierarchy( SerializedProperty nodesProp )
        {
            var result   = new List<TreeViewItemData<SerializedProperty>>();

            if ( nodesProp.arraySize > 0 )
            {
                var rootProp = nodesProp.GetArrayElementAtIndex( 0 );
                var index = 1;
                var childs = GetChildren( rootProp, ref index );
                var root = new TreeViewItemData<SerializedProperty>( 0, rootProp, childs );
                result.Add( root );
            }

            return result;

            List<TreeViewItemData<SerializedProperty>> GetChildren( SerializedProperty parentProp, ref Int32 index )
            {
                var result      = new List<TreeViewItemData<SerializedProperty>>();
                var parentDepth = parentProp.FindPropertyRelative( "_depth" ).intValue;
                while ( index < nodesProp.arraySize )
                {
                    var node       = nodesProp.GetArrayElementAtIndex( index );
                    var childDepth = node.FindPropertyRelative( "_depth" ).intValue;
                    if ( childDepth <= parentDepth )                          //Child list is ended
                        break;
                    else if ( childDepth == parentDepth + 1 )                     //Child found, add to childs list
                    {
                        result.Add( new TreeViewItemData<SerializedProperty>( index++, node ) );
                    }
                    else if ( childDepth == parentDepth + 2 && result.Count > 0 )              //Grandchild found, get grandchilds list and add to last child
                    {
                        var lastChild   = result.Last();
                        var lastChildProp = nodesProp.GetArrayElementAtIndex( index - 1 );
                        var grandChilds = GetChildren( lastChildProp, ref index );
                        result[ ^1 ] = new TreeViewItemData<SerializedProperty>( lastChild.id, lastChild.data, grandChilds );
                    }
                    else
                    {
                        Debug.LogError( $"Unexpected item depth {childDepth} after parent depth {parentDepth}, property path {node.propertyPath}, you can take a look using Unity inspector debug mode. Skipped item." );
                        index++;
                    }
                }

                return result;
            }
        }

        private String GetPropertyPersistentString( SerializedProperty treeListProperty )
        {
            var target = treeListProperty.serializedObject.targetObject;
            if ( target is MonoBehaviour mb )
            {
                var go = mb.gameObject;
                return $"{GetType().FullName}.{SearchUtils.GetHierarchyPath( go )}.{mb.GetType().Name}.{treeListProperty.propertyPath}";
            }
            else
                return $"{GetType().FullName}.{target.name}.{treeListProperty.propertyPath}";
        }

        private void RefreshButtons( SerializedProperty nodesProp, Boolean dragging = false )
        {
             _removeBtn.SetEnabled( _treeUI.selectedIndex > -1 );
             _addBtn.SetEnabled( _treeUI.selectedIndex > -1 || _treeUI.GetTreeCount() == 0 );
             _expandBtn.SetEnabled( _treeUI.GetTreeCount() > 0 );

             var foldoutLabelWidth = _foldout.Q<Toggle>().Q<VisualElement>(  ).Q<Label>(  ).worldBound.width;
             var totalWidth        = _foldout.worldBound.width;
             if( totalWidth - foldoutLabelWidth > 375 )
                _hint.text = GetTreeHint( _treeUI.selectedIndex, dragging, nodesProp  );
             else
                 _hint.text = String.Empty;
        }

        private static class ResourcesUITk
        {
            public static readonly VisualTreeAsset TreeViewAsset = Resources.Load<VisualTreeAsset>( "TreeList" );

            public static readonly Texture2D Plus  = (Texture2D)( EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_Toolbar Plus").image : EditorGUIUtility.IconContent("Toolbar Plus").image );
            public static readonly Texture2D Minus =  (Texture2D) (EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_Toolbar Minus").image : EditorGUIUtility.IconContent("Toolbar Minus").image);
            public static readonly Texture2D Expand =  (Texture2D) (EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_UnityEditor.SceneHierarchyWindow").image : EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow").image);
            public static readonly Texture2D Search =   (Texture2D) (EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_Search Icon").image : EditorGUIUtility.IconContent("Search Icon").image );
        }
    }
}