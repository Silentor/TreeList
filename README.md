# TreeList
TreeList is a simple generic tree data type with editor support for your Unity projects. It's based on a List<> so its name. 
## Features:
- Fast serialization/deserialization (only data and depth of the tree node)
- Fast iteration, children enumeration (because of the List)
- Not so fast modification (because of the List)
- UIToolkit and IMGUI editors (mostly equivalent in functionality)
- Dark/light skin compatible
## Installation
1. Install a package from a Git URL.
Please take a look at [Unity Manual](https://docs.unity3d.com/Manual/upm-ui-giturl.html) for instructions and use (https://github.com/Silentor/TreeList.git?path=/Packages/com.Silentor.TreeList) URL in Package Manager to install package
2. .unitypackage/Zip/tarball? 
## Usage
Code examples (add/remove nodes, enumerate children, access node value)
Gifs of Unity Editor with tree usage (add, remove nodes, expand/collapse, copy/paste, search, drag n drop support in UIToolkit version)

### Basic code sample
```C#
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

        //Add child to last child (Vector3.right) of root node
        var grandChild = childs[2].AddChild( "grand child" );

        //Move first child of root node (Vector3.forward) to the grandchild node as a child
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
```

