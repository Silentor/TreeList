using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
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

            var nodesProp           = property.FindPropertyRelative( "SerializableNodes" );
            var elementsCounter = root.Q<TextField>( "Counter" );
            elementsCounter.BindProperty( nodesProp.FindPropertyRelative( "Array.size" ) );
            
            var tree = root.Q<TreeView>( "TreeView" );
            tree.style.maxHeight               =  Screen.height * 2/3;
            tree.viewDataKey                   =  GetPropertyPersistentString( property );
            tree.selectionChanged              += objs => Debug.Log( $"selection changed: {objs.Count()}" );
            tree.showAlternatingRowBackgrounds =  AlternatingRowBackground.All;
            tree.makeItem                      =  ( ) => new VisualElement(){style = { borderBottomColor = Color.black, borderBottomWidth = 1 }};
            tree.bindItem = ( e, i ) =>
            {
                var id        = tree.GetIdForIndex( i );
                e.Add( new Label($"id {id}") );
                var valueProp = nodesProp.GetArrayElementAtIndex( i ).FindPropertyRelative( "Value" );
                
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
            tree.itemIndexChanged += ( itemid, parentid ) =>
            {
                Debug.Log( $"moved {itemid} to parent {parentid}" );
                tree.GetChildrenIdsForIndex(  )
            };
            
            var hierarchy = BuildHierarchy( nodesProp ); 
            tree.SetRootItems( hierarchy );
                    
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