using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Silentor.TreeList.Editor
{
    /// <summary>
    /// UIToolkit version of TreeListPropertyDrawer
    /// </summary>
    public partial class TreeListPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            var root = ResourcesUITk.TreeViewAsset.CloneTree();
            var foldout = root.Q<Foldout>("Header");
            foldout.text = property.displayName;
            foldout.value = property.isExpanded;
            foldout.BindProperty( property );

            // var foldout = new Foldout( )
            //               {
            //                       text  = property.displayName,
            //                       value = property.isExpanded,
            //                       style = { flexGrow = 1},
            //                       //bindingPath = property.propertyPath,
            //               };

            
            //root.Add( foldout );

            var nodesProp           = property.FindPropertyRelative( "SerializableNodes" );
            var elementsCounter = root.Q<TextField>( "Counter" );
            //elementsCounter.SetEnabled( false );
            elementsCounter.BindProperty( nodesProp.FindPropertyRelative( "Array.size" ) );
            //root.Add( elementsCounter );

            var tree = root.Q<TreeView>( "TreeView" );
            tree.style.maxHeight = Screen.height * 2/3;
            tree.makeItem             = ( ) => new VisualElement();
            tree.bindItem = ( e, i ) =>
            {
                var  valueProp   = nodesProp.GetArrayElementAtIndex( i ).FindPropertyRelative( "Value" );
                var treeItem       = e.parent;
                while ( treeItem.name != "unity-tree-view__item")                
                    treeItem = treeItem.parent;
                var toggle = treeItem.Q<Toggle>("unity-tree-view__item-toggle");
                toggle.value = valueProp.isExpanded;
                var valuePropCopy = valueProp;
                toggle.RegisterValueChangedCallback( ce => valuePropCopy.isExpanded = ce.newValue );
                
                if ( valueProp.hasVisibleChildren )
                {
                    valueProp = valueProp.Copy();
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
            };
            tree.unbindItem = ( e, i ) =>
            {
                e.Clear();
            };
            
            var hierarchy = BuildHierarchy( nodesProp ); 
            tree.SetRootItems( hierarchy );
            //foldout.contentContainer.Add( tree );

            var removeBtn = root.Q<Button>( "RemoveBtn" );
            removeBtn.clickable.clicked += () =>
            {
                if ( tree.selectedIndex > -1 )
                {
                    RemoveItem( tree.selectedIndex, nodesProp );
                    property.serializedObject.ApplyModifiedProperties();
                    tree.SetRootItems( BuildHierarchy( nodesProp ) );
                    tree.Rebuild();
                }
            };

            var addBtn = root.Q<Button>( "AddBtn" );
            addBtn.clickable.clicked += () =>
            {
                if ( nodesProp.arraySize == 0 )
                {
                    AddItem( -1, nodesProp );
                    property.serializedObject.ApplyModifiedProperties();
                    property.isExpanded = true;
                    tree.SetRootItems( BuildHierarchy( nodesProp ) );
                    tree.Rebuild();
                }
                else if( tree.selectedIndex > -1 )
                {
                    var addedIndex = AddItem( tree.selectedIndex, nodesProp );
                    property.serializedObject.ApplyModifiedProperties();
                    tree.SetRootItems( BuildHierarchy( nodesProp ) );
                    tree.Rebuild();
                }
            };

            return root;
        }

        private IList<TreeViewItemData<SerializedProperty>> BuildHierarchy( SerializedProperty nodesProp )
        {
            var result   = new List<TreeViewItemData<SerializedProperty>>();

            if ( nodesProp.arraySize > 0 )
            {
                var rootProp = nodesProp.GetArrayElementAtIndex( 0 );
                var index = 1;
                var childs = GetChildren( rootProp, ref index );
                var root = new TreeViewItemData<SerializedProperty>( 0, rootProp.FindPropertyRelative( "Value" ), childs );
                result.Add( root );
            }

            return result;

            List<TreeViewItemData<SerializedProperty>> GetChildren( SerializedProperty parentProp, ref Int32 index )
            {
                var result      = new List<TreeViewItemData<SerializedProperty>>();
                var parentDepth = parentProp.FindPropertyRelative( "Depth" ).intValue;
                while ( index < nodesProp.arraySize )
                {
                    var node       = nodesProp.GetArrayElementAtIndex( index );
                    var childDepth = node.FindPropertyRelative( "Depth" ).intValue;
                    if ( childDepth <= parentDepth )                          //Child list is ended
                        break;
                    else if ( childDepth == parentDepth + 1 )                     //Child found, add to childs list
                    {
                        result.Add( new TreeViewItemData<SerializedProperty>( index++, node.FindPropertyRelative( "Value" ) ) );
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
                        Debug.LogError( $"Unexpected item, index {index++}" );
                    }
                }

                return result;
            }
        }

        private static class ResourcesUITk
        {
            public static readonly VisualTreeAsset TreeViewAsset = Resources.Load<VisualTreeAsset>( "TreeList" );
            public static readonly Texture2D       Plus          = EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D;
            public static readonly Texture2D       Minus         = EditorGUIUtility.IconContent("Toolbar Minus").image as Texture2D;
            // public static readonly GUIContent Depth =  EditorGUIUtility.isProSkin 
            //         ? new ("Depth", EditorGUIUtility.IconContent("d_BlendTree Icon").image) 
            //         : new ("Depth", EditorGUIUtility.IconContent("BlendTree Icon").image) ;

        }

        
    }

    

    

}