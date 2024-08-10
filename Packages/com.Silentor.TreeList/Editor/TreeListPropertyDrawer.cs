using System;
using System.Reflection;
using UnityEditor;
using Object = System.Object;

namespace Silentor.TreeList.Editor
{
    /// <summary>
    ///     Common part of TreeListPropertyDrawer
    /// </summary>
    [CustomPropertyDrawer( typeof(TreeList<>), true )]
    public partial class TreeListPropertyDrawer : PropertyDrawer
    {
        private                 Int32                              _structuralHash;

        private Int32 AddItem( Int32 parentIndex, SerializedProperty nodes )
        {
            if ( parentIndex > -1 )
            {
                var parentDepth = nodes.GetArrayElementAtIndex( parentIndex ).FindPropertyRelative( "_depth" ).intValue;
                var childIndex  = nodes.arraySize;

                for ( var i = parentIndex + 1; i < nodes.arraySize; i++ )
                    if ( nodes.GetArrayElementAtIndex( i ).FindPropertyRelative( "_depth" ).intValue <= parentDepth )
                    {
                        childIndex = i;
                        break;
                    }

                nodes.InsertArrayElementAtIndex( childIndex );
                var newItem = nodes.GetArrayElementAtIndex( childIndex  );
                newItem.FindPropertyRelative( "_depth" ).intValue = parentDepth + 1;
                return childIndex;
            }
            else
            {
                nodes.InsertArrayElementAtIndex( 0 );
                return 0;
            }
        }

        private void RemoveItem( Int32 itemIndex, SerializedProperty nodes )
        {
            var subtreeSize = GetSubtree( itemIndex, nodes );
            for ( int i = 0; i < subtreeSize; i++ )
            {
                nodes.DeleteArrayElementAtIndex( itemIndex );
            }
        }

        private void MoveItem( Int32 itemIndex, Int32 newParentIndex, Int32 newChildIndex, SerializedProperty nodes )
        {
            var oldDepth = nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "_depth" ).intValue;
            var newDepth = newParentIndex == -1 ? 0 : nodes.GetArrayElementAtIndex( newParentIndex ).FindPropertyRelative( "_depth" ).intValue + 1;
            var deltaDepth = newDepth - oldDepth;
            var newItemIndex = ChildIndex2GlobalIndex( newParentIndex, newChildIndex, nodes );

            var subtreeSize = GetSubtree( itemIndex, nodes );
            for ( var i = 0; i < subtreeSize; i++ )
            {
                var movingNode = nodes.GetArrayElementAtIndex( itemIndex + i );
                movingNode.FindPropertyRelative( "_depth" ).intValue += deltaDepth;
                nodes.MoveArrayElement( itemIndex + i, newItemIndex + i );
            }
        }

        private Int32 SearchValue( String searchString, Int32 fromIndex, SerializedProperty nodes )
        {
            for ( var i = 0; i < nodes.arraySize; i++ )
            {
                var index     = ( fromIndex + i ) % nodes.arraySize;
                var node      = nodes.GetArrayElementAtIndex( index );
                var valueProp = node.FindPropertyRelative( "Value" );
                if ( valueProp == null )
                    continue;
                else if ( !valueProp.hasVisibleChildren )
                {
                    if ( CheckProperty( valueProp, searchString ) )
                        return index;
                }
                else
                {
                    var enterChildren = true;
                    var endProp       = valueProp.GetEndProperty();
                    while ( valueProp.NextVisible( enterChildren ) && !SerializedProperty.EqualContents( valueProp, endProp ) )
                    {
                        enterChildren = false;
                        if ( CheckProperty( valueProp, searchString ) )
                            return index;
                    }
                }
            }

            return -1;

            static Boolean CheckProperty( SerializedProperty prop, String searchString )
                {
                    switch ( prop.propertyType )
                    {
                        case SerializedPropertyType.Integer:
                            return prop.intValue.ToString() == searchString;
                        case SerializedPropertyType.Boolean:
                            return prop.boolValue.ToString().Equals( searchString, StringComparison.OrdinalIgnoreCase );
                        case SerializedPropertyType.Float:
                            return prop.floatValue.ToString().StartsWith( searchString );
                        case SerializedPropertyType.String:
                            return prop.stringValue.Contains( searchString, StringComparison.OrdinalIgnoreCase );
                        case SerializedPropertyType.ObjectReference:
                            return prop.objectReferenceValue 
                                   && (prop.objectReferenceValue.name.Contains( searchString, StringComparison.OrdinalIgnoreCase ) 
                                       || prop.objectReferenceValue.GetType().Name.Contains( searchString, StringComparison.OrdinalIgnoreCase ));
                        case SerializedPropertyType.Enum:
                        {
                            var enumIndex = prop.enumValueIndex;
                            if( enumIndex >= 0 && enumIndex < prop.enumDisplayNames.Length )
                                return prop.enumDisplayNames[enumIndex].Contains( searchString );
                            else
                                return prop.enumValueIndex.ToString() == searchString ;
                        }
                        case SerializedPropertyType.Vector2:
                            return prop.vector2Value.ToString().Contains( searchString );
                        case SerializedPropertyType.Vector3:
                            return prop.vector3Value.ToString().Contains( searchString );
                        case SerializedPropertyType.Vector4:
                            return prop.vector4Value.ToString().Contains( searchString );
                        case SerializedPropertyType.Rect:
                            return prop.rectValue.ToString().Contains( searchString );
                        case SerializedPropertyType.Character:
                            return ((Char)prop.intValue).ToString() == searchString;
                        case SerializedPropertyType.Bounds:
                            return prop.boundsValue.ToString().Contains( searchString );
                        case SerializedPropertyType.Quaternion:
                            return prop.quaternionValue.ToString().Contains( searchString );
                        case SerializedPropertyType.Vector2Int:
                            return prop.vector2IntValue.ToString().Contains( searchString );
                        case SerializedPropertyType.Vector3Int:
                            return prop.vector3IntValue.ToString().Contains( searchString );
                        case SerializedPropertyType.RectInt:
                            return prop.rectIntValue.ToString().Contains( searchString );
                        case SerializedPropertyType.BoundsInt:
                            return prop.boundsIntValue.ToString().Contains( searchString );
                        case SerializedPropertyType.ManagedReference:
                        {
                            if ( prop.managedReferenceValue != null )
                                return prop.managedReferenceValue.GetType().Name.Contains( searchString, StringComparison.OrdinalIgnoreCase );
                            return false;
                        }
                        case SerializedPropertyType.Hash128:
                            return prop.hash128Value.ToString().Contains( searchString );
                        default:
                            return false;
                    }
                }
        }


        /// <summary>
        /// Get index of given item relative to parent node
        /// </summary>
        /// <returns></returns>
        private Int32 GetChildIndex( Int32 itemIndex, SerializedProperty nodes )
        {
            //Not defined for root item
            if ( itemIndex == 0 )
                return -1;

            var depth   = nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "_depth" ).intValue;
            var counter = 0;
            for ( var i = itemIndex - 1; i >= 0; i-- )
            {
                var maybeParent = nodes.GetArrayElementAtIndex( i );
                if( maybeParent.FindPropertyRelative( "_depth" ).intValue == depth - 1 )
                    return counter;
                counter++;
            }

            return -1;
        }

        /// <summary>
        /// Returns the size of subtree (1 for root + all children count recursively)
        /// </summary>
        /// <param name="rootIndex"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private Int32 GetSubtree( Int32 rootIndex, SerializedProperty nodes )
        {
            if( rootIndex == 0 )
                return nodes.arraySize;

            var depth      = nodes.GetArrayElementAtIndex( rootIndex ).FindPropertyRelative( "_depth" ).intValue;
            var result     = 1;
            for ( var i = rootIndex + 1; i < nodes.arraySize; i++ )
            {
                if ( nodes.GetArrayElementAtIndex( i ).FindPropertyRelative( "_depth" ).intValue <= depth )
                    break;

                result++;
            }

            return result;
        }

        private Int32 ChildIndex2GlobalIndex( Int32 parentIndex, Int32 childIndex, SerializedProperty nodesProp )
        {
            var result      = -1;
            var parentDepth = nodesProp.GetArrayElementAtIndex( parentIndex ).FindPropertyRelative( "_depth" ).intValue;
            var i = 0;
            for ( i = parentIndex + 1; i < nodesProp.arraySize; i++ )
            {
                 var childNode = nodesProp.GetArrayElementAtIndex( i );
                 var childDepth = childNode.FindPropertyRelative( "_depth" ).intValue;

                 if( childDepth == parentDepth + 1 && childIndex-- == 0 )           //Get needed child
                 {
                     result = i;
                     break;
                 }
                 else if( childDepth <= parentDepth )                               //No more childs
                     break;
            }

            if ( result == -1 )             //childIndex is out of range, select index after last child
                result = i;

            return result;
        }

        private Int32 GetStructuralHash( SerializedProperty nodesProp )
        {
            var hash = new HashCode();

            for ( var i = 0; i < nodesProp.arraySize; i++ )
            {
                var depthProp = nodesProp.GetArrayElementAtIndex( i ).FindPropertyRelative( "_depth" );
                hash.Add( depthProp.intValue );
            }

            return hash.ToHashCode();
        }

        private String GetTreeHint( Int32 selectedItemIndex, SerializedProperty nodesProp )
        {
            if ( nodesProp.arraySize == 0 )
                return "Press + to add root item";

            if ( selectedItemIndex < 0 )
                return "Select item to operate tree";

            return String.Empty;
        }

        public static class Clipboard
        {
            private static readonly Type       ClipboardType = typeof(ClipboardUtility).Assembly.GetType( "UnityEditor.Clipboard" );

            private static readonly MethodInfo SetSerializedPropertyMethod   =
                    ClipboardType.GetMethod( "SetSerializedProperty", BindingFlags.Static | BindingFlags.Public );

            private static readonly MethodInfo GetSerializedPropertyMethod    =
                    ClipboardType.GetMethod( "GetSerializedProperty", BindingFlags.Static | BindingFlags.Public );

            private static readonly MethodInfo CheckMethod   = ClipboardType.GetMethod( "HasSerializedProperty", BindingFlags.Static | BindingFlags.Public );

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