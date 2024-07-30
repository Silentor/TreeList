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
                var parentDepth = nodes.GetArrayElementAtIndex( parentIndex ).FindPropertyRelative( "Depth" ).intValue;
                var childIndex  = nodes.arraySize;

                for ( var i = parentIndex + 1; i < nodes.arraySize; i++ )
                    if ( nodes.GetArrayElementAtIndex( i ).FindPropertyRelative( "Depth" ).intValue <= parentDepth )
                    {
                        childIndex = i;
                        break;
                    }

                nodes.InsertArrayElementAtIndex( childIndex );
                var newItem = nodes.GetArrayElementAtIndex( childIndex  );
                newItem.FindPropertyRelative( "Depth" ).intValue = parentDepth + 1;
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
            var oldDepth = nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "Depth" ).intValue;
            var newDepth = newParentIndex == -1 ? 0 : nodes.GetArrayElementAtIndex( newParentIndex ).FindPropertyRelative( "Depth" ).intValue + 1;
            var deltaDepth = newDepth - oldDepth;

            var subtreeSize = GetSubtree( itemIndex, nodes );
            for ( var i = 0; i < subtreeSize; i++ )
            {
                var movingNode = nodes.GetArrayElementAtIndex( itemIndex + i );
                movingNode.FindPropertyRelative( "Depth" ).intValue += deltaDepth;
                nodes.MoveArrayElement( itemIndex + i, newParentIndex + newChildIndex + i + 1 );
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

            var depth   = nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "Depth" ).intValue;
            var counter = 0;
            for ( var i = itemIndex - 1; i >= 0; i-- )
            {
                var maybeParent = nodes.GetArrayElementAtIndex( i );
                if( maybeParent.FindPropertyRelative( "Depth" ).intValue == depth - 1 )
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

            var depth      = nodes.GetArrayElementAtIndex( rootIndex ).FindPropertyRelative( "Depth" ).intValue;
            var result     = 1;
            for ( var i = rootIndex + 1; i < nodes.arraySize; i++ )
            {
                if ( nodes.GetArrayElementAtIndex( i ).FindPropertyRelative( "Depth" ).intValue <= depth )
                    break;

                result++;
            }

            return result;
        }

        private Int32 GetStructuralHash( SerializedProperty nodesProp )
        {
            var hash = new HashCode();

            for ( var i = 0; i < nodesProp.arraySize; i++ )
            {
                var depthProp = nodesProp.GetArrayElementAtIndex( i ).FindPropertyRelative( "Depth" );
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