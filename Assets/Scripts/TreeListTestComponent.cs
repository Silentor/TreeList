
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
        public TreeList<String>     Tree;
        public TreeList<CustomNode> Tree2;

        public Int32 Test3;

#if UNITY_EDITOR
        [ButtonMethod]
        public void Test( )
        {
            Tree = new TreeList<String>();
            Tree.Add( "root", null )
                .AddChild( "child1" )
                .AddSibling( "child2" );

            Tree2 = new TreeList<CustomNode>();
            Tree2.Add( new CustomNode {CustomText       = "root", CustomInt = 1}, null )
                    .AddChild( new CustomNode {CustomText   = "child1", CustomInt = 2, CustomBool = true} )
                        .AddChild( new CustomNode() {CustomText = "grandChild1", CustomInt = 3, CustomBool = false} ).GetParent()
                    .AddSibling( new CustomNode {CustomText = "child2", CustomInt = 4, CustomBool = true} );

            EditorUtility.SetDirty( this );
        }

        private void Awake( )
        {
            Debug.Log( Tree.ToHierarchyString() );
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
}