using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = System.Object;

namespace Silentor.TreeControl.Editor
{
    [CustomPropertyDrawer( typeof(TreeList<>), true )]
    public class TreeListPropertyDrawer : PropertyDrawer
    {                                   
        private static readonly Dictionary<Int32, TreeEditorState> _treeViewStates = new ();
        private                 MyTreeView                         _tree;
        private                 MultiColumnHeaderState             _multiColumnHeaderState;
        private readonly        Single                             _headerHeight = EditorGUIUtility.singleLineHeight + 2;
        private Int32 _structuralHash;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            //Debug.Log( $"{property.displayName}, evt {Event.current.type}, hash {this.GetHashCode()}" );

            var state     = GetPersistentTreeViewState( property );
            var nodesProp = property.FindPropertyRelative( "SerializableNodes" );
            if( nodesProp.arraySize == 0 )
                property.isExpanded = false;

            if ( _tree == null )
            {
                _multiColumnHeaderState = new MultiColumnHeaderState( 
                        new MultiColumnHeaderState.Column[]
                        {
                                new ()
                                {
                                        autoResize            = false,
                                },
                                new ()
                                {
                                        autoResize            = false,
                                },
                        
                        } );

                _tree = new MyTreeView( state.TreeViewState, new MyMultiColumnHeader(_multiColumnHeaderState), nodesProp ) ;
            }

            position = DrawHeader( position, label, state, nodesProp, property );

            if ( !property.isExpanded )
            {
                return;
            }

            if ( !_tree.IsInitialized || _structuralHash != GetStructuralHash( nodesProp ) )
            {
                _tree.Reload();
                _structuralHash = GetStructuralHash( nodesProp );
            }

            //Adjust columns
            var expandedItems = _tree.GetRows().Where( tvi => _tree.GetExpanded().Contains( tvi.id ) ).ToArray();
            if ( expandedItems.Length > 0 )
            {
                //Expand foldouts column to fit the deepest expanded item
                var deepestExpandedItem = expandedItems.MaxBy( tvi => tvi.depth );
                var depth               = deepestExpandedItem.depth * 15 + 30;
                _multiColumnHeaderState.columns[0].width = depth;
            }
            else
            {
                _multiColumnHeaderState.columns[0].width           = 30;
            }
                    
            _multiColumnHeaderState.columns[1].width = position.width - _multiColumnHeaderState.columns[0].width - 20;  

            _tree.OnGUI( position );
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
            var isBtnEnabled = mainProperty.isExpanded && _tree.HasSelection();
            EditorGUI.BeginDisabledGroup( !isBtnEnabled );
            if ( GUI.Button( btnRect, Resources.Minus, Resources.ToolBarBtnStyle ) )
            {
                var (_, selectedIndex) = _tree.GetSelectedItem();
                RemoveItem( selectedIndex, nodesProp );
                if( nodesProp.arraySize == 0 )
                    mainProperty.isExpanded = false;
            }
            EditorGUI.EndDisabledGroup();

            //Draw add button
            btnRect = new Rect( btnRect.x - btnRect.width - 15, btnRect.y, btnRect.width, btnRect.height );
            var isEmptyTree = nodesProp.arraySize == 0;
            isBtnEnabled = isEmptyTree || _tree.HasSelection();
            EditorGUI.BeginDisabledGroup( !isBtnEnabled );
            if ( GUI.Button( btnRect, Resources.Plus, Resources.ToolBarBtnStyle  ) )
            {
                if ( isEmptyTree )
                {
                    AddItem( -1,  nodesProp );
                    mainProperty.isExpanded = true;
                }
                else
                {
                    var (_, selectedIndex) = _tree.GetSelectedItem();
                    AddItem( selectedIndex, nodesProp );
                }
            };
            EditorGUI.EndDisabledGroup();

            //Custom prefix label because TreeView with header hides it. wtf?
            var prefixLabelRect = headerRect;
            prefixLabelRect.xMax = btnRect.x - 5;
            var headerStyle = nodesProp.prefabOverride ? Resources.HeaderOverridenStyle : Resources.HeaderStyle;
            if( nodesProp.arraySize > 0 )
                mainProperty.isExpanded = EditorGUI.Foldout( prefixLabelRect, mainProperty.isExpanded, label, true, headerStyle );   
            else
            {
                EditorGUI.Foldout( prefixLabelRect, false, label, true, headerStyle );
                mainProperty.isExpanded = false;
            }

            //Draw blue margin if tree value is overriden
            if ( nodesProp.prefabOverride && Event.current.type == EventType.Repaint )
            {
                EditorGUI.DrawRect( new Rect(headerRect.x - 18, headerRect.y, 2, headerRect.height), Resources.PrefabOverrideMarginColor);
            }

            //Simulate default property context menu
            if (Event.current.type == EventType.MouseUp && prefixLabelRect.Contains (Event.current.mousePosition) && Event.current.button == 1)
            {
                var menu = new GenericMenu();
                menu.AddItem( new GUIContent( "Copy property path" ), false, () => EditorGUIUtility.systemCopyBuffer = mainProperty.propertyPath );
                if ( nodesProp.prefabOverride )
                {
                    //Find prefabs where this property may be overriden
                    var  obj     = ((Component)nodesProp.serializedObject.targetObject).gameObject;
                    var  prefab  = PrefabUtility.GetCorrespondingObjectFromSource( obj );  //Make sure we in prefab space

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
                                    () => PrefabUtility.ApplyPropertyOverride( nodesProp, AssetDatabase.GetAssetPath( prefabAsset ), InteractionMode.UserAction ) );
                        else
                            menu.AddItem( new GUIContent( title ), false, 
                                    () => PrefabUtility.ApplyPropertyOverride( nodesProp, AssetDatabase.GetAssetPath( prefabAsset ), InteractionMode.UserAction ) );
                    }

                    menu.AddItem( new GUIContent( "Revert" ), false, ( ) => nodesProp.prefabOverride = false );
                }
                menu.AddSeparator( String.Empty );

                if( !mainProperty.hasMultipleDifferentValues )
                    menu.AddItem( new GUIContent( "Copy" ), false, () => Resources.Clipboard.Copy( mainProperty ) );
                else
                    menu.AddDisabledItem( new GUIContent( "Copy" ) );
                if( !mainProperty.hasMultipleDifferentValues && GUI.enabled && Resources.Clipboard.IsPropertyPresent() )
                    menu.AddItem( new GUIContent( "Paste" ), false, () =>
                    {
                        Resources.Clipboard.Paste( mainProperty );
                        mainProperty.serializedObject.ApplyModifiedProperties();
                    } );
                else
                    menu.AddDisabledItem( new GUIContent( "Paste" ) );

                menu.ShowAsContext();
                Event.current.Use ();
            }

            return contentRect;
        }

        private void AddItem( Int32 parentIndex, SerializedProperty nodes )
        {
            if ( parentIndex > -1 )
            {
                var parentDepth = nodes.GetArrayElementAtIndex( parentIndex ).FindPropertyRelative( "Level" ).intValue;
                nodes.InsertArrayElementAtIndex( parentIndex + 1 );
                var newItem = nodes.GetArrayElementAtIndex( parentIndex + 1  );
                newItem.FindPropertyRelative( "Level" ).intValue = parentDepth + 1;
            }
            else
                nodes.InsertArrayElementAtIndex( 0 );
        }

        private void RemoveItem( Int32 itemIndex, SerializedProperty nodes )
        {
            //Delete item and all its children based on depth
            var depth = nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "Level" ).intValue;
            nodes.DeleteArrayElementAtIndex( itemIndex );
            while ( nodes.arraySize > itemIndex )
            {
                if( nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "Level" ).intValue > depth )
                    nodes.DeleteArrayElementAtIndex( itemIndex );
                else
                    break;
            }
        }

        public override Single GetPropertyHeight( SerializedProperty property, GUIContent label )
        {
            if( _tree == null || !_tree.IsInitialized  )
                return _headerHeight;
            if( !property.isExpanded || property.FindPropertyRelative( "SerializableNodes" ).arraySize == 0)
                return _headerHeight;

            return Mathf.Clamp( _tree.totalHeight + _headerHeight, _headerHeight, 500 );
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

        private Int32 GetStructuralHash( SerializedProperty nodesProp )
        {
            var hash = new HashCode();

            for ( int i = 0; i < nodesProp.arraySize; i++ )
            {
                var depthProp = nodesProp.GetArrayElementAtIndex( i ).FindPropertyRelative( "Level" );
                hash.Add( depthProp.intValue );
            }

            return hash.ToHashCode();
        }

        [Serializable]
        private class TreeEditorState
        {
            public TreeViewState TreeViewState = new();
            //public Boolean       IsTreeExpanded;
        }

        private static class Resources
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

            public static readonly GUIContent Plus  = new  (EditorGUIUtility.IconContent("Toolbar Plus").image, "Add child node") ;
            public static readonly GUIContent Minus = new  (EditorGUIUtility.IconContent("Toolbar Minus").image, "Remove node") ;
            public static readonly GUIContent Depth =  EditorGUIUtility.isProSkin 
                    ? new ("Depth", EditorGUIUtility.IconContent("d_BlendTree Icon").image) 
                    : new ("Depth", EditorGUIUtility.IconContent("BlendTree Icon").image) ;

            

            public static readonly Color PrefabOverrideMarginColor = new (0.003921569f, 0.6f, 0.92156863f, 0.75f);

            public static class Clipboard
            {
                private static readonly Type       ClipboardType = typeof(ClipboardUtility).Assembly.GetType("UnityEditor.Clipboard");
                private static readonly MethodInfo SetSerializedPropertyMethod   = ClipboardType.GetMethod("SetSerializedProperty", BindingFlags.Static | BindingFlags.Public);
                private static readonly MethodInfo GetSerializedPropertyMethod    = ClipboardType.GetMethod("GetSerializedProperty", BindingFlags.Static | BindingFlags.Public);
                private static readonly MethodInfo CheckMethod   = ClipboardType.GetMethod("HasSerializedProperty", BindingFlags.Static | BindingFlags.Public);

                public static void Copy( SerializedProperty property )
                {
                    SetSerializedPropertyMethod.Invoke( null, new Object[] { property } );
                }

                public static void Paste( SerializedProperty property )
                {
                    GetSerializedPropertyMethod.Invoke( null, new Object[] { property } );
                }

                public static Boolean IsPropertyPresent( )
                {
                    return (Boolean)CheckMethod.Invoke( null, Array.Empty<Object>() );
                }
            }

        }
    }
}