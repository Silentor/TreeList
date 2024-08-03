using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Silentor.TreeList.Tests
{
    public class BigTreeGenerator : MonoBehaviour
    {
        public TreeList<Int32> Tree;
        public Int32           NodesCount = 1000;

        public void GenerateBigTree( )
        {
            Tree = new TreeList<Int32> { { 0, null } };

            for ( var i = 1; i < NodesCount; i++ )
            {
                var parent = Tree.Nodes[UnityEngine.Random.Range( 0, Tree.Count )];
                Tree.Add( i, parent );
            }
        }
    }

#if UNITY_EDITOR

    [UnityEditor.CustomEditor( typeof(BigTreeGenerator) )]
    class BigTreeGeneratorEditor : UnityEditor.Editor
    {
        private BigTreeGenerator _target;

        private void OnEnable( )
        {
            _target = (BigTreeGenerator)target;
        }

        public override void OnInspectorGUI( )
        {
            base.OnInspectorGUI();

            if ( GUILayout.Button( "Generate tree" ) )
            {
                _target.GenerateBigTree();
            }
        }

        public override VisualElement CreateInspectorGUI( )
        {
            var root = new VisualElement();
            UnityEditor.UIElements.InspectorElement.FillDefaultInspector( root, serializedObject, UnityEditor.Editor.CreateEditor( _target ) );
            var generateBtn = new Button(() => _target.GenerateBigTree()){text = "Generate tree"};
            root.Add( generateBtn );
            return root;
        }
    }

#endif
}
