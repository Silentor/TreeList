
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Silentor.TreeControl
{
    public class TreeListTestComponent2 : MonoBehaviour
    {
        //public Int32                        FirstInspector;
        //[HideInInspector]
        //public TreeList<String>             PrimitiveTree;
        //public TreeList<String>             PrimitiveTree2;
        public TreeList<CustomNode>         CustomTree;
        //public TreeList<CustomNode>         CustomTree2;
        //[HideInInspector]
        //public TreeList<VoidNode>           VoidTree;

        //public CustomNode[] TestCollection;
        //public CustomNode[] TestCollection2;

        private void Awake( )
        {
            //Debug.Log( CustomTree.ToHierarchyString() );
        }

    }

}

