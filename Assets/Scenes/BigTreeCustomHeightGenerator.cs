using System;
using Silentor.TreeList;
using UnityEngine;
using UnityEngine.UIElements;


    public class BigTreeCustomHeightGenerator : MonoBehaviour
    {
        public TreeList<CustomNodeWithDrawer> Tree;
        public Int32                          NodesCount = 1000;

        public void GenerateBigTree( )
        {
            Tree = new TreeList<CustomNodeWithDrawer> { { new CustomNodeWithDrawer(){}, null } };

            for ( var i = 1; i < NodesCount; i++ )
            {
                var parent = Tree.Nodes[UnityEngine.Random.Range( 0, Tree.Count )];
                var randomBool = UnityEngine.Random.value > 0.5f;
                Tree.Add( new CustomNodeWithDrawer(){Int = i, Bool = randomBool}, parent );
            }
        }
    }

    #if UNITY_EDITOR

    [UnityEditor.CustomEditor( typeof(BigTreeCustomHeightGenerator) )]
    class BigTreeGeneratorEditor2 : UnityEditor.Editor
    {
        private BigTreeCustomHeightGenerator _target;

        private void OnEnable( )
        {
            _target = (BigTreeCustomHeightGenerator)target;
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
