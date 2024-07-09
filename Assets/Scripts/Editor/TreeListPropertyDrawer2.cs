using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Silentor.TreeControl.Editor
{
    //[CustomPropertyDrawer( typeof(TreeList<String>), true )]
    public class TreeListPropertyDrawer2 : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new Label( "Test" );

            var treeWidget = Resources.TreeListWidget.CloneTree();
            return treeWidget;
        }

        private static class Resources
        {
            public static readonly VisualTreeAsset TreeListWidget = UnityEngine.Resources.Load<VisualTreeAsset>( "TreeWidget" );
        }
    }
}