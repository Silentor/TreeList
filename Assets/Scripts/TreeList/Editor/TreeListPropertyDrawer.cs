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
        private                 String                             _treeTypeHint;

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
            //Delete item and all its children based on depth
            var depth = nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "Depth" ).intValue;
            nodes.DeleteArrayElementAtIndex( itemIndex );
            while ( nodes.arraySize > itemIndex )
                if ( nodes.GetArrayElementAtIndex( itemIndex ).FindPropertyRelative( "Depth" ).intValue > depth )
                    nodes.DeleteArrayElementAtIndex( itemIndex );
                else
                    break;
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

        private String GetTreeHint( SerializedProperty nodesProp )
        {
            if ( nodesProp.arraySize == 0 )
            {
                return "Press + to add root item";
            }

            if ( _treeTypeHint == null )
            {
                var node      = nodesProp.GetArrayElementAtIndex( 0 );
                var valueProp = node.FindPropertyRelative( "Value" );
                if ( valueProp == null )
                {
                    _treeTypeHint = "Value is not serializable";
                }
                else
                {
                    var valueType = valueProp.type;
                    _treeTypeHint =  $"Tree of {valueType}";
                }
            }

            return _treeTypeHint;
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