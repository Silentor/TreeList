
using System;
using MyBox;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Silentor.TreeControl
{
    public class TreeListTestComponent : MonoBehaviour
    {
        public TreeList<String>             PrimitiveTree;
        public TreeList<CustomNode>         CustomTree;
        public TreeList<VoidNode>           VoidTree;

#if UNITY_EDITOR
        [ButtonMethod]
        public void Test( )
        {
            PrimitiveTree = new TreeList<String>();
            PrimitiveTree.Add( "root", null )
                         .AddChildren( 
                                  "child1",
                                  "child2"
                                  );

            {
                CustomTree = new TreeList<CustomNode>();
                var root = CustomTree.Add( new CustomNode { CustomText = "root", CustomInt        = 1 }, null );
                var ch1  = root.AddChild( new CustomNode { CustomText  = "child1", CustomInt      = 2, CustomBool = true } );
                var gch1 = ch1.AddChild( new CustomNode { CustomText   = "grandChild1", CustomInt = 3, CustomBool = false } );
                var ch2  = root.AddChild( new CustomNode { CustomText  = "child1", CustomInt      = 2, CustomBool = true } );
            }
            {
                VoidTree = new TreeList<VoidNode>();
                var root = VoidTree.Add( new VoidNode { }, null );
                var ch1  = root.AddChild( new VoidNode {  } );
                var gch1 = ch1.AddChild( new VoidNode {  } );
                var ch2  = root.AddChild( new VoidNode { } );
            }
            EditorUtility.SetDirty( this );
        }

        private void Awake( )
        {
            //Debug.Log( Tree.ToHierarchyString() );
        }
#endif
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
}