
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Silentor.TreeControl
{
    public class TreeListTestComponent : MonoBehaviour
    {
        //public Int32                        FirstInspector;
        //[HideInInspector]
        public TreeList<String>             PrimitiveTree;
        //public TreeList<CustomNode>         CustomTree;
        //[HideInInspector]
        //public TreeList<VoidNode>           VoidTree;

        public Int32[] TestCollection;

        private void Awake( )
        {
            //Debug.Log( CustomTree.ToHierarchyString() );
        }

    }

    [Serializable]
    public class CustomNode
    {
        public String CustomText;
        public Boolean CustomBool;
        public Int32 CustomInt;

        public override String ToString( )
        {
            return $"{CustomText}/{CustomInt}/{CustomBool}";
        }
    }

    public class VoidNode
    {
           
    }

#if UNITY_EDITOR
    [CustomEditor( typeof(TreeListTestComponent) )]
    public class TreeListTestComponentEditor : Editor
    {
        private TreeListTestComponent _target;

        private void OnEnable( )
        {
            _target = (TreeListTestComponent)target;
        }

        public override void OnInspectorGUI( )
        {
            base.OnInspectorGUI();

            // if ( GUILayout.Button( "Fill simple tree" ) )
            // {
            //     _target.PrimitiveTree = new TreeList<String>();
            //     _target.PrimitiveTree.Add( "root", null )
            //                  .AddChildren( 
            //                           "child1",
            //                           "child2"
            //                           );
            // }


            // if ( GUILayout.Button( "Fill custom tree" ) )
            // {
            //     _target.CustomTree = new TreeList<CustomNode>();
            //     var root = _target.CustomTree.Add( new CustomNode { CustomText = "root", CustomInt        = 1 }, null );
            //     var ch1  = root.AddChild( new CustomNode { CustomText  = "child1", CustomInt      = 2, CustomBool = true } );
            //     var gch1 = ch1.AddChild( new CustomNode { CustomText   = "grandChild1", CustomInt = 3, CustomBool = false } );
            //     var ch2  = root.AddChild( new CustomNode { CustomText  = "child2", CustomInt      = 2, CustomBool = true } );
            // }
            //
            //
            // if ( GUILayout.Button( "Fill custom deep tree" ) )
            // {
            //     _target.CustomTree = new TreeList<CustomNode>();
            //     var root = _target.CustomTree.Add( new CustomNode { CustomText = "root deep", CustomInt        = 1 }, null );
            //     var ch1  = root.AddChild( new CustomNode { CustomText          = "child1", CustomInt      = 2, CustomBool = true } );
            //     var gch1 = ch1.AddChild( new CustomNode { CustomText           = "grandChild1", CustomInt = 3, CustomBool = false } );
            //     var ch2  = root.AddChild( new CustomNode { CustomText          = "child2", CustomInt      = 2, CustomBool = true } );
            //
            //     //Test deep hierarchy
            //     for ( int i = 0; i < 10; i++ )
            //     {
            //         gch1 = gch1.AddChild( new CustomNode() { CustomText = $"grandchild{i + 2}", CustomInt = i } );
            //     }
            // }

            // if ( GUILayout.Button( "Fill void tree" ) )
            // {
            //     _target.VoidTree = new TreeList<VoidNode>();
            //     var root = _target.VoidTree.Add( new VoidNode { }, null );
            //     var ch1  = root.AddChild( new VoidNode {  } );
            //     var gch1 = ch1.AddChild( new VoidNode {  } );
            //     var ch2  = root.AddChild( new VoidNode { } );
            // }

        }
    }
#endif
}

