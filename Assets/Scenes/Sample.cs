using System;
using System.Linq;
using Silentor.TreeList;
using UnityEngine;

public class Sample : MonoBehaviour
{
    public TreeList<String> VectorTree;

    private void Awake( )
    {
        VectorTree = new TreeList<String>();

        //Add root node
        var rootNode = VectorTree.Add( "I am Root", null );
        
        //Add children nodes to root node
        rootNode.AddChildren( "child1", "child2", "child3", "child4" );

        //Enumerate children of root node
        var childs = rootNode.GetChildren(  ).ToArray();

        //Add child to 'child3' node
        var grandChild = childs[2].AddChild( "grand child" );

        //Move 'child1' to the grandchild node as a child
        VectorTree.Move( childs[0], grandChild );

        //Remove node from tree
        VectorTree.Remove( childs[1] );

        //Print tree structure and values
        Debug.Log( VectorTree.ToHierarchyString() );

        /*
        Value = 'I am Root' (level = 0, )
           Value = 'child3' (level = 1, parent 'I am Root')
               Value = 'grand child' (level = 2, parent 'child3')
                   Value = 'child1' (level = 3, parent 'grand child')
           Value = 'child4' (level = 1, parent 'I am Root')
        */
    }
}