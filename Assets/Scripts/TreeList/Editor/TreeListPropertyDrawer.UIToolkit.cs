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
            var root = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            var foldout = new Foldout( )
                          {
                                  text  = property.displayName,
                                  value = property.isExpanded,
                                  //bindingPath = property.propertyPath,
                          };

            foldout.BindProperty( property );
            root.Add( foldout );

            var elementsCounter = new TextField(){ style = { position = Position.Absolute, right = 0, top = 0, width = 50 } };
            elementsCounter.SetEnabled( false );
            elementsCounter.BindProperty( property.FindPropertyRelative( "SerializableNodes" ).FindPropertyRelative( "Array.size" ) );
            root.Add( elementsCounter );

            var removeButton = new Button( () =>
                            {
                                //AddItem(  );
                            } )
                            { text = "-", style = { position = Position.Absolute, right = 55, top = 0, width = EditorGUIUtility.singleLineHeight, height = EditorGUIUtility.singleLineHeight } };
            root.Add( removeButton );

            var addButton = new Button( () =>
                                        {
                                            //AddItem(  );
                                        } )
                            { text = "+", style = { position = Position.Absolute, right = 50 + 5 + EditorGUIUtility.singleLineHeight + 10, top = 0, width = EditorGUIUtility.singleLineHeight, height = EditorGUIUtility.singleLineHeight } };
            root.Add( addButton );

            var tree = new TreeView();
            tree.makeItem = ( ) => new InspectorElement();
            tree.bindItem = ( e, i ) =>
            {
                var nodes = property.FindPropertyRelative( "SerializableNodes" );
                var nodeProp = nodes.GetArrayElementAtIndex( i );
                var valueProp = nodeProp.FindPropertyRelative( "Value" );
                var levelProp = nodeProp.FindPropertyRelative( "Depth" );

                if ( valueProp.hasVisibleChildren )
                {
                    var enterChildren = true;
                    var endProp       = valueProp.GetEndProperty();
                    while ( valueProp.NextVisible( enterChildren ) && !SerializedProperty.EqualContents( valueProp, endProp ) )
                    {
                        var label          =  String.Concat( Enumerable.Repeat( "    ", levelProp.intValue )) + valueProp.displayName;
                        var propField      = new PropertyField( valueProp, label );
                        e.Add( propField );
                        enterChildren   =  false;
                    }
                }
                else   //Value is primitive type itself
                {
                    var label     =  String.Concat( Enumerable.Repeat( "    ", levelProp.intValue )) + valueProp.displayName;
                    var propField = new PropertyField( valueProp, label );
                    e.Add( propField );
                }
            };
            var nodes = property.FindPropertyRelative( "SerializableNodes" );
            foreach ( var VARIABLE in nodes )
            {
                
            }
            tree.SetRootItems(  );
            foldout.contentContainer.Add( tree );

            return root;
        }

        private IList<TreeViewItemData<SerializedProperty>> GetHierarchy( SerializedProperty nodesProp )
        {
            var nodes = new List<TreeViewItemData<SerializedProperty>>();
            for ( var i = 0; i < nodesProp.arraySize; i++ )
            {
                var nodeProp = nodesProp.GetArrayElementAtIndex( i );
                var depthProp = nodeProp.FindPropertyRelative( "Depth" );
                nodes.Add( new TreeViewItemData<SerializedProperty>( i, nodeProp, depthProp.intValue ) );
            }

            return nodes;
        }
    }

    

}