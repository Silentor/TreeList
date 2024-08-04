using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Silentor.TreeList
{
    [Serializable]
    public class TreeList<T> : ISerializationCallbackReceiver, IEnumerable<TreeList<T>.Node>
    {
        public IReadOnlyList<Node> Nodes => _nodes;
        public Node                Root  => _nodes.Count > 0 ? _nodes[ 0 ] : null;
        public Int32               Count => _nodes.Count;

        [SerializeField]
        private List<Node> _nodes = new();

        public Node Add( T value, Node parent )
        {
            if ( parent == null )
            {
                if ( _nodes.Count > 0 )
                    throw new InvalidOperationException( "Root node already exists" );
                var newNode = new Node ( 0, 0, this ) { Value = value };
                _nodes.Add( newNode );
                return newNode;
            }
            else
            {
                CheckNodeBelongsTree( parent, nameof(parent) );
                var newChildIndex      = parent._index + GetSubtreeSize( parent );
                var newNode            = new Node( newChildIndex, parent.Depth + 1, this ){ Value = value };
                _nodes.Insert( newChildIndex, newNode );
                FixIndices( newChildIndex + 1 );

                return newNode;
            }
        }

        public IEnumerable<Node> GetChilds( [NotNull] Node node, Boolean includeItself = false, Boolean recursive = false )
        {
            CheckNodeBelongsTree( node, nameof(node) );
            
            if ( includeItself )
                yield return node;

            if ( recursive )
            {
                for ( int i = node._index + 1; i < _nodes.Count && _nodes[ i ].Depth > node.Depth; i++ )
                    yield return _nodes[ i ];
            }
            else
            {
                for ( int i = node._index + 1; i < _nodes.Count && _nodes[ i ].Depth > node.Depth; i++ )
                    if( _nodes[i].Depth == node.Depth + 1 )
                        yield return _nodes[ i ];
            }
        }

        public IEnumerable<Node> GetChildsBreadthFirst( [NotNull] Node node, Boolean includeItself = false )
        {
            CheckNodeBelongsTree( node, nameof(node) );

            if ( includeItself )
                yield return node;

            var indexFrom = node._index + 1;
            var indexTo   = GetSubtreeSize( node ) + node._index;

            Boolean isAnyChildFinded ;
            var  childDepth  = node.Depth + 1;
            do
            {
                isAnyChildFinded = false;
                for ( int i = indexFrom; i < indexTo; i++ )
                {
                    if ( _nodes[ i ].Depth == childDepth )
                    {
                        isAnyChildFinded = true;
                        yield return _nodes[ i ];
                    }
                }
                childDepth++;

            } while ( isAnyChildFinded );
        }

        public Node  GetParent( [NotNull] Node node )
        {
            CheckNodeBelongsTree( node, nameof(node) );

            if ( node == Root )
                return null;

            for ( var i = node._index - 1; i >= 0; i-- )
            {
                if( _nodes[i].Depth == node.Depth - 1 )
                    return _nodes[i];
            }

            throw new InvalidOperationException( "Parent not found, invalid tree" );
        }

        public IEnumerable<Node> GetParents( [NotNull] Node node )
        {
            CheckNodeBelongsTree( node, nameof(node) );

            while ( node.Depth != 0 )
            {
                node = GetParent( node );
                yield return node;
            }
        }

        public Int32 Move( [NotNull] Node node, [NotNull] Node newParent, Int32 newChildIndex = -1 )
        {
            CheckNodeBelongsTree( node, nameof(node) );
            CheckNodeBelongsTree( newParent, nameof(newParent) );

            //Cannot move inside of itself
            Assert.IsTrue( node != newParent );
            Assert.IsFalse( GetParents( newParent ).Contains( node ) ); 

            Node childToReplace = null;
            if ( newChildIndex >= 0 )
            {
                using var childsEnumerator = newParent.GetChildren(  ).GetEnumerator();
                for ( int i = 0; childsEnumerator.MoveNext(); i++ )
                {
                    if( i == newChildIndex )        //Find child to replace
                    {
                        childToReplace = childsEnumerator.Current;
                        break;
                    }
                }
            }

            var buffer = GetChilds( node, true, true ).ToArray();
            Remove( node );

            var toIndex = childToReplace != null ? childToReplace._index : newParent._index + GetSubtreeSize( newParent );
            var counter = 0;
            var depthDiff = newParent.Depth + 1 - node.Depth;
            foreach ( var n in buffer )
            {
                n._depth += depthDiff;
                _nodes.Insert( toIndex + counter++, n );
            }

            FixIndices( toIndex );
            return buffer.Length;
        } 

        public Int32 Remove( [NotNull] Node node )
        {
            CheckNodeBelongsTree( node, nameof(node) );

            var removeCount = GetSubtreeSize( node );
            var i  = removeCount;
            while ( i-- > 0 )
            {
                _nodes.RemoveAt( node._index );                
            }

            FixIndices( node._index );

            return removeCount;
        }

        public void Clear( )
        {
            _nodes.Clear();
        }

        public String ToHierarchyString( )
        {
            var sb = new StringBuilder();
            foreach ( var node in _nodes )
            {
                sb.Append( new String( ' ', node.Depth * 2 ) );
                sb.AppendLine( $"Value = '{node.Value}' (level = {node.Depth}, parent {(node.Depth > 0 ? GetParent( node ).Value : "")})" );
            }

            return sb.ToString();
        }

        public Boolean Equals( TreeList<T> other )
        {
            if ( ReferenceEquals( null, other ) ) return false;
            if ( ReferenceEquals( this, other ) ) return true;

            if ( Count != other.Count ) return false;

            for ( int i = 0; i < Nodes.Count; i++ )
            {
                if( _nodes[i].Depth != other._nodes[i].Depth || !Equals( _nodes[i].Value, other._nodes[i].Value ) )
                    return false;
            }

            return true;
        }

        private void CheckNodeBelongsTree( Node node, String paramName )
        {
            if ( node == null )
                throw new ArgumentNullException( $"{paramName} is null" );

            if( node.Owner != this )
                throw new ArgumentException( $"Node {paramName} is not belongs this tree" );
        }

        /// <summary>
        /// Get subtree count for given node (node itself + count of all children recursively)
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private Int32 GetSubtreeSize( Node root )
        {
            if ( root == Root )
                return _nodes.Count;

            var result = 1;
            for ( var i = root._index + 1; i < _nodes.Count; i++ )
            {
                if( _nodes[i].Depth > root.Depth )
                    result++;
                else
                    break;
            }

            return result;
        }

        private void FixIndices( Int32 fromIndex )
        {
            for ( var i = fromIndex; i < _nodes.Count; i++ )
            {
                _nodes[ i ]._index = i;
            }
        }

        [DebuggerDisplay("#{_index}: [{Depth}] {Value}")]
        [Serializable]
        public class Node
        {
            public T        Value;

            public Int32    Depth => _depth;

            public Node     Parent => Owner.GetParent( this );

            public readonly TreeList<T> Owner;

            public Node AddChild( T value )
            {
                return Owner.Add( value, this );
            }

            public Node AddSibling( T value )
            {
                return Parent.AddChild( value );
            }

            public void AddChildren( params T[] values )
            {
                foreach ( var v in values )
                {
                    Owner.Add( v, this );    
                }
            }

            public IEnumerable<Node> GetChildren( Boolean includeSelf = false )
            {
                return Owner.GetChilds( this, includeSelf );
            }

            public Boolean StructuralEqual( Node other )
            {
                if ( ReferenceEquals( null, other ) ) return false;
                if ( ReferenceEquals( this, other ) ) return true;

                return Depth == other.Depth && Equals( Value, other.Value );
            }

            public Boolean ValueEqual( Node other )
            {
                if ( ReferenceEquals( null, other ) ) return false;
                if ( ReferenceEquals( this, other ) ) return true;

                return Equals( Value, other.Value );
            }

            internal Node( Int32 index, Int32 depth, TreeList<T> owner )
            {
                Assert.IsTrue( index >= 0 );
                Assert.IsTrue( depth >= 0 );

                _depth = depth;
                _index = index;
                Owner  = owner;
            }

            [SerializeField]
            internal Int32 _depth;

            internal Int32 _index;
        }
      
        public void OnBeforeSerialize( )
        {
        }

        public void OnAfterDeserialize( )
        {
           FixIndices( 0 );
        }

        public IEnumerator<Node> GetEnumerator( )
        {
            return _nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator( )
        {
            return GetEnumerator();
        }
    }
}