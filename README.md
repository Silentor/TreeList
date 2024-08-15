# TreeList
TreeList is a simple generic tree data type with editor support (prefabs, nodes copy/paste and drag and drop) for your Unity projects. It's based on a List<> internally so its name. 
## Features:
- Fast serialization/deserialization (only data and depth of the tree node)
- Fast iteration, children enumeration (because of the List)
- Not so fast modification (because of the List)
- UIToolkit and IMGUI editors (mostly equivalent in functionality)
- Dark/light skin compatible
## Installation
Install a package from a Git URL.
Please take a look at [Unity Manual](https://docs.unity3d.com/Manual/upm-ui-giturl.html) for instructions and use `https://github.com/Silentor/TreeList.git?path=/Packages/com.Silentor.TreeList` URL in Package Manager to install package.

![Unity_sUXKbHWFpt](https://github.com/user-attachments/assets/a3b460cc-06df-4ffa-82fa-7e86a90834ee)

Or add `"com.silentor.treelist": "https://github.com/Silentor/TreeList.git?path=/Packages/com.Silentor.TreeList"` line to your `Packages/manifest.json` file

## Usage

### Basic code sample
```C#
public class Sample : MonoBehaviour
{
    public TreeList<String> StringTree;

    private void Awake( )
    {
        StringTree = new TreeList<String>();

        //Add root node
        var rootNode = StringTree.Add( "I am Root", null );
        
        //Add children nodes to root node
        rootNode.AddChildren( "child1", "child2", "child3", "child4" );

        //Enumerate children of root node
        var childs = rootNode.GetChildren(  ).ToArray();

        //Add child to 'child3' node
        var grandChild = childs[2].AddChild( "grand child" );

        //Move 'child1' to the grandchild node as a child
        StringTree.Move( childs[0], grandChild );

        //Remove node from tree
        StringTree.Remove( childs[1] );

        //Print tree structure and values
        Debug.Log( StringTree.ToHierarchyString() );

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
Methods `GetChildren()` and `GetChildrenBFS()` return IEnumerable so it allocates a little. If you want zero-allocation get-children logic for tight loops please use `GetChildrenNonAlloc()` and `GetChildrenBFSNonAlloc()` methods. They receive List<> parameter for result list and do not allocate if list capacity is enough. 

### Unity inspector

Add/remove nodes (UIToolkit, dark theme)

![AddRemoveUITkdark-ezgif com-optimize](https://github.com/user-attachments/assets/47192d82-100b-4843-ab03-2d7148fa35c5)

Search values in a tree (IMGUI, dark theme)

![SearchIMGUIdark-ezgif com-optimize](https://github.com/user-attachments/assets/5340c2c0-a518-4bd4-b943-3c09645ebe5b)

Drag and drop nodes to move around or copy ( Ctrl+drag ) (IMGUI, light theme). There is a small issue with Ctrl+dragging in UIToolkit implementation, drag icon does not change from "move" to "copy" while copy operation is working.

![DragNDropIMGUIlight-ezgif com-optimize](https://github.com/user-attachments/assets/416cd5cd-a272-4b3f-9035-cbdf4d32c056)

Work with prefabs (apply, revert) and copy/paste of entire tree property, tree node value and properties of complex node levels (UIToolkit, light theme). Sorry for the themed context menu for node values, I didn't find a way to hook up the native context menu without ruining native menu for separate node values.

![PrefabUITklight-ezgif com-optimize](https://github.com/user-attachments/assets/a00fc505-3b18-482f-abb8-81131a39fbc4)

# License
This library is under the MIT License
