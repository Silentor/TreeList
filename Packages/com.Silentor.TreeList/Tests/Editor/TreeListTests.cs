using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = System.Object;

namespace Silentor.TreeList.Tests.Editor
{
    [TestFixture]
    public class TreeListTests
    {
        private TreeString _tree;

        [SetUp]
        public void PrepareTree( )
        {
            _tree = new TreeString();
            _tree.Add( "root", null );

            var ch1 = _tree.Add( "child1", _tree.Root );
            var ch2 = _tree.Add( "child2", _tree.Root );

            var ch1_1 = _tree.Add( "child1_1", ch1 );
            var ch1_2 = _tree.Add( "child1_2", ch1 );

            var ch2_1 = _tree.Add( "child2_1", ch2 );
            var ch2_2 = _tree.Add( "child2_2", ch2 );

            var ch1_1_1 = _tree.Add( "child1_1_1", ch1_1 );
        }


        [Test]
        public void TestAdd( )
        {
            var newTree = new TreeString();

            Assert.IsTrue( newTree.Root == null );
            Assert.IsTrue( newTree.Count == 0);

            Assert.IsTrue( _tree.Root  != null );
            Assert.IsTrue( _tree.Count != 0 );
            var child2_1 = _tree.Nodes.First( n => n.Value.Equals( "child2_1" ) );
            var child2 = _tree.Nodes.First( n => n.Value.Equals( "child2" ) );
            Assert.IsTrue( _tree.GetParent( child2_1 ) == child2 );
            Assert.IsTrue( child2.Parent == _tree.Root );
        }

        [Test]
        public void TestGetChilds( )
        {
            var root = _tree.Root;
            Assert.IsTrue( _tree.GetChildren( root, false ).Count() == 2 );
            var childsOfRoot = _tree.GetChildren( root, false ).Select( n => n.Value ).ToArray();
            CollectionAssert.AreEqual( childsOfRoot, new Object[]{"child1", "child2"} );

            var child1 = _tree.Nodes.First( n => n.Value.Equals( "child1" ) );
            Assert.IsTrue( _tree.GetChildren( child1, false ).Count() == 2 );
            CollectionAssert.AreEqual( _tree.GetChildren( child1, false ).Select( n => n.Value ), new Object[]{"child1_1", "child1_2"} );
        }

        [Test]
        public void TestGetChildsRecursive( )
        {
            var child1          = _tree.Nodes.First( n => n.Value.Equals( "child1" ) );
            var childsRecursive = _tree.GetChildren( child1, false, true ).ToArray();
            Assert.IsTrue( childsRecursive.Count() == 3 );
            CollectionAssert.AreEqual( childsRecursive.Select( n => n.Value ), new Object[]{"child1_1", "child1_1_1", "child1_2" } );
        }

        [Test]
        public void TestGetParent( )
        {
            var child1_1_1          = _tree.Nodes.First( n => n.Value.Equals( "child1_1_1" ) );
            Assert.IsTrue( (String)_tree.GetParent( child1_1_1 ).Value == "child1_1" );

            var parents = _tree.GetParents( child1_1_1 ).ToArray();
            CollectionAssert.AreEqual( parents.Select( n => n.Value ), new Object[]{"child1_1", "child1", "root" } );

            var child2_1 = _tree.Nodes.First( n => n.Value.Equals( "child2_1" ) );
            parents  = _tree.GetParents( child2_1 ).ToArray();
            CollectionAssert.AreEqual( parents.Select( n => n.Value ), new Object[]{"child2", "root" } );
        }

        [Test]
        public void TestRemove( )
        {
            var lastChild = _tree.Nodes.First( n => n.Value.Equals( "child1_1_1" ) );
            Assert.IsTrue( _tree.Remove( lastChild ) == 1 );
            CollectionAssert.AreEqual( _tree.Nodes.Select( n => n.Value ), new Object[]{"root", "child1", "child1_1", "child1_2", "child2", "child2_1", "child2_2" } );

            var middleChild =_tree.Nodes.First( n => n.Value.Equals( "child1" ) );
            Assert.IsTrue( _tree.Remove( middleChild ) == 3 );
            CollectionAssert.AreEqual( _tree.Nodes.Select( n => n.Value ), new Object[]{"root", "child2", "child2_1", "child2_2" } );

            Assert.IsTrue( _tree.Remove( _tree.Root ) == 4 );
            Assert.IsTrue( _tree.Nodes.Count == 0 );
        }

        [Test]
        public void TestMove( )
        {
            var child1 = _tree.Nodes.First( n => n.Value.Equals( "child1" ) );
            var child2_1 = _tree.Nodes.First( n => n.Value.Equals( "child2_1" ) );

            Assert.IsTrue( _tree.Move( child1, child2_1 ) == 4 );
            Assert.IsTrue( child1.Depth == 3 );
            Assert.IsTrue( _tree.GetParent( child1) == child2_1 );
            Assert.IsTrue( _tree.GetChildren( child2_1, false, true ).Count() == 4 );
            Assert.IsTrue( _tree.GetChildren( _tree.Root ).Count() == 1 );
            Assert.IsTrue( _tree.GetChildren( _tree.GetParent( child2_1 ) ).Count() == 2 );

        }

        [Test]
        public void TestMove2( )
        {
            var child1   = _tree.Nodes.First( n => n.Value.Equals( "child1" ) );
            var child2_1 = _tree.Nodes.First( n => n.Value.Equals( "child2_1" ) );

            Assert.IsTrue( _tree.Move( child2_1, child1 )                 == 1 );
            Assert.IsTrue( child2_1.Depth                                 == 2 );
            Assert.IsTrue( _tree.GetParent( child2_1)                     == child1 );
            Assert.IsTrue( _tree.GetChildren( child1, false, true ).Count() == 4 );
            Assert.IsTrue( _tree.GetChildren( _tree.Root ).Count()          == 2 );
        }

        [Test]
        public void TestMove3( )
        {
            var child1   = _tree.Nodes.First( n => n.Value.Equals( "child1" ) );
            var child2 = _tree.Nodes.First( n => n.Value.Equals( "child2" ) );

            Assert.IsTrue( _tree.Move( child1, child2, 0 )                == 4 );
            Assert.IsTrue( child1.Depth                                   == 2 );
            Assert.IsTrue( _tree.GetParent( child1)                       == child2 );
            Assert.IsTrue( _tree.GetChildren( child2 ).Count() == 3 );
            Assert.IsTrue( _tree.GetChildren( _tree.Root ).Count()          == 1 );
        }

        [Test]
        public void TestCompareAndFluentSyntax( )
        {
            var newTree = new TreeString();
            newTree.Add( "root", null )
                   .AddChild( "child1" )
                        .AddChild( "child1_1" )
                            .AddChild( "child1_1_1" ).Parent
                        .AddSibling( "child1_2" ).Parent.Parent
                   .AddChild( "child2" )
                        .AddChild( "child2_1" )
                        .AddSibling( "child2_2" );

            var newTree2 = new TreeString();
            newTree.Add( "root_another", null )
                   .AddChild( "child1" )
                        .AddChild( "child1_1" )
                            .AddChild( "child1_1_1" ).Parent
                        .AddSibling( "child1_2" ).Parent.Parent
                   .AddChild( "child2" )
                        .AddChild( "child2_1" )
                        .AddSibling( "child2_2" );

            Debug.Log( newTree.ToHierarchyString() );

            Assert.IsTrue( _tree.Equals( newTree ) );
            Assert.IsFalse( _tree.Equals( newTree2 ) );
        }

        [Test]
        public void TestToHierarchyString( )
        {
            var output = _tree.ToHierarchyString();
            Console.WriteLine( output );
            Debug.Log( output );
        }

        [Test]
        public void TestIEnumerable( )
        {
            var nodes = _tree.ToArray();
            Assert.IsTrue( nodes.Length == _tree.Nodes.Count );
            Assert.IsTrue( nodes.Distinct().Count() == _tree.Count );
        }

        [Test]
        public void TestBFS( )
        {
            var result = _tree.GetChildsBreadthFirst( _tree.Root ).ToArray();
            CollectionAssert.AreEqual( result.Select( n => n.Value ), new Object[]{"child1", "child2", "child1_1", "child1_2", "child2_1", "child2_2", "child1_1_1" } );
        }
    }

    public class TreeString : TreeList<String>
    {

    }
}