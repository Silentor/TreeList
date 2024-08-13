using System;
using Silentor.TreeList;
using UnityEngine;
using UnityEngine.UIElements;


    public class BigTreeCustomNodeGenerator : MonoBehaviour
    {
        public TreeList<CustomNode> Tree;
        public Int32           NodesCount = 1000;

        public void GenerateBigTree( )
        {
            Tree = new TreeList<CustomNode> { { new CustomNode(){CustomText = "Root"}, null } };

            for ( var i = 1; i < NodesCount; i++ )
            {
                var parent = Tree.Nodes[UnityEngine.Random.Range( 0, Tree.Count )];
                var randomString = Guid.NewGuid().ToString().Substring( 0, 5 );
                var randomBool = UnityEngine.Random.value > 0.5f;
                Tree.Add( new CustomNode(){CustomText = randomString, CustomBool = randomBool, CustomInt = i}, parent );
            }
        }
    }

    #if UNITY_EDITOR

    [UnityEditor.CustomEditor( typeof(BigTreeCustomNodeGenerator) )]
    class BigTreeGeneratorEditor : UnityEditor.Editor
    {
        private BigTreeCustomNodeGenerator _target;

        private void OnEnable( )
        {
            _target = (BigTreeCustomNodeGenerator)target;
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
