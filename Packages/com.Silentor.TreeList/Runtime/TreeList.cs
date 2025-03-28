﻿using System;
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
    /// <summary>
    /// Generic tree data type, based on List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class TreeList<T> : ISerializationCallbackReceiver, IEnumerable<TreeList<T>.Node>
    {
        /// <summary>
        /// All nodes of the tree
        /// </summary>
        public IReadOnlyList<Node> Nodes => _nodes;
        /// <summary>
        /// Root node or null if tree is empty
        /// </summary>
        public Node                Root  => _nodes.Count > 0 ? _nodes[ 0 ] : null;
        /// <summary>
        /// Count of the nodes
        /// </summary>
        public Int32               Count => _nodes.Count;

        [SerializeField]
        private List<Node>         _nodes;

        /// <summary>
        /// Create TreeList with default capacity
        /// </summary>
        public TreeList( )
        {
            _nodes = new List<Node>();
        }

        /// <summary>
        /// Create TreeList with specified capacity
        /// </summary>
        /// <param name="capacity"></param>
        public TreeList( Int32 capacity )
        {
            _nodes = new List<Node>( capacity );
        }

        /// <summary>
        /// Create TreeList with root node
        /// </summary>
        /// <param name="rootValue"></param>
        public TreeList( T rootValue )
        {
            _nodes = new List<Node> { new ( 0, 0, this ) { Value = rootValue } };
        } 

        /// <summary>
        /// Add node to the tree. If parent is null, node will be added as root. If parent is not null, node will be added as child of parent
        /// </summary>
        /// <param name="value">Value of the new node</param>
        /// <param name="parent"></param>
        /// <returns>Newly added node</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Node Add( T value, [CanBeNull] Node parent )
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
                var newChildIndex = parent._index + GetSubtreeSize( parent );
                var newNode       = new Node( newChildIndex, parent.Depth + 1, this ){ Value = value };
                _nodes.Insert( newChildIndex, newNode );
                FixIndices( newChildIndex + 1 );

                return newNode;
            }
        }

        /// <summary>
        /// Get children of given node, order is depth-first. Return enumerator
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="includeItself">Include parent node</param>
        /// <param name="recursive">Include all children of children</param>
        /// <returns>Enumerator over children</returns>
        public IEnumerable<Node> GetChildren( [NotNull] Node parent, Boolean includeItself = false, Boolean recursive = false )
        {
            CheckNodeBelongsTree( parent, nameof(parent) );
            
            if ( includeItself )
                yield return parent;

            if ( recursive )
            {
                for ( int i = parent._index + 1; i < _nodes.Count && _nodes[ i ].Depth > parent.Depth; i++ )
                    yield return _nodes[ i ];
            }
            else
            {
                for ( int i = parent._index + 1; i < _nodes.Count && _nodes[ i ].Depth > parent.Depth; i++ )
                    if( _nodes[i].Depth == parent.Depth + 1 )
                        yield return _nodes[ i ];
            }
        }

        /// <summary>
        /// Get children of given node, order is depth-first. Fills the given list, doesn't allocate is list capacity is enough
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="result">Fill that list with children nodes, must be non null</param>
        /// <param name="includeItself">Include parent node</param>
        /// <param name="recursive">Include all children of children</param>
        /// <exception cref="ArgumentNullException">If parent node is null or result list is null</exception>
        public void GetChildrenNonAlloc( [NotNull] Node parent, [NotNull] List<Node> result, Boolean includeItself = false, Boolean recursive = false )
        {
            CheckNodeBelongsTree( parent, nameof(parent) );
            if( result == null )  throw new ArgumentNullException( nameof(result) );
            
            result.Clear();

            if ( includeItself )
                result.Add( parent );

            if ( recursive )
            {
                for ( int i = parent._index + 1; i < _nodes.Count && _nodes[ i ].Depth > parent.Depth; i++ )
                    result.Add( _nodes[ i ] );
            }
            else
            {
                for ( int i = parent._index + 1; i < _nodes.Count && _nodes[ i ].Depth > parent.Depth; i++ )
                    if( _nodes[i].Depth == parent.Depth + 1 )
                        result.Add( _nodes[ i ] );
            }
        }

        /// <summary>
        /// Get children of given node, in breadth-first order
        /// </summary>
        /// <param name="parent">Given node</param>
        /// <param name="includeItself">Should include parent node</param>
        /// <returns>Enumerator over children</returns>
        public IEnumerable<Node> GetChildrenBFS( [NotNull] Node parent, Boolean includeItself = false )
        {
            CheckNodeBelongsTree( parent, nameof(parent) );

            if ( includeItself )
                yield return parent;

            var indexFrom = parent._index            + 1;
            var indexTo   = GetSubtreeSize( parent ) + parent._index;

            Boolean isAnyChildFinded ;
            var     childDepth  = parent.Depth + 1;
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

        /// <summary>
        /// Get children of given node, in breadth-first order. Fills the given list, doesn't allocate is list capacity is enough
        /// </summary>
        /// <param name="parent">Given node</param>
        /// <param name="result">Fill that list with children nodes, must be non null</param>
        /// <param name="includeItself">Should include parent node</param>
        /// <exception cref="ArgumentNullException">Thrown if result list is null</exception>
        public void GetChildrenBFSNonAlloc( [NotNull] Node parent, [NotNull] List<Node> result, Boolean includeItself = false )
        {
            CheckNodeBelongsTree( parent, nameof(parent) );
            if( result == null )  throw new ArgumentNullException( nameof(result) );

            result.Clear();
            if ( includeItself )
                result.Add( parent );

            var indexFrom = parent._index            + 1;
            var indexTo   = GetSubtreeSize( parent ) + parent._index;

            Boolean isAnyChildFinded ;
            var     childDepth  = parent.Depth + 1;
            do
            {
                isAnyChildFinded = false;
                for ( int i = indexFrom; i < indexTo; i++ )
                {
                    if ( _nodes[ i ].Depth == childDepth )
                    {
                        isAnyChildFinded = true;
                        result.Add( _nodes[ i ] );
                    }
                }
                childDepth++;

            } while ( isAnyChildFinded );
        }

        /// <summary>
        /// Get parent node of given node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Cannot find parent node, tree is invalid</exception>
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

        /// <summary>
        /// Get all parents of given node, starting from nearest parent and ending with root
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Returns enumerator over parents</returns>
        public IEnumerable<Node> GetParents( [NotNull] Node node )
        {
            CheckNodeBelongsTree( node, nameof(node) );

            while ( node.Depth != 0 )
            {
                node = GetParent( node );
                yield return node;
            }
        }

        /// <summary>
        /// Move node to new parent. If newChildIndex is specified, node will be inserted at that index, otherwise node will be placed after last child
        /// </summary>
        /// <param name="node"></param>
        /// <param name="newParent"></param>
        /// <param name="newChildIndex"></param>
        /// <returns>Moved node</returns>
        public Node Move( [NotNull] Node node, [NotNull] Node newParent, Int32 newChildIndex = -1 )
        {
            CheckNodeBelongsTree( node,      nameof(node) );
            CheckNodeBelongsTree( newParent, nameof(newParent) );

            //Cannot move inside of itself
            if( node == newParent || GetParents( newParent ).Contains( node ) )
                return node;

            var oldDepth     = node.Depth;
            var newDepth     = newParent.Depth + 1;
            var deltaDepth   = newDepth - oldDepth;
            var newItemIndex = ChildIndex2GlobalIndex( newParent, newChildIndex );
            var oldItemIndex = node._index;

            var subtreeSize = GetSubtreeSize( node );
            if ( oldItemIndex < newItemIndex )
            {
                for ( var i = 0; i < subtreeSize; i++ )
                {
                    var movedNode = _nodes[ oldItemIndex ];
                    movedNode._depth += deltaDepth;
                    _nodes.Insert( newItemIndex, movedNode );
                    _nodes.RemoveAt( oldItemIndex );
                }
            }
            else
            {
                for ( var i = 0; i < subtreeSize; i++ )
                {
                    var movedNode = _nodes[ oldItemIndex + i ];
                    movedNode._depth += deltaDepth;
                    _nodes.Insert( newItemIndex + i, movedNode );
                    _nodes.RemoveAt( oldItemIndex + i + 1 );
                }
            }

            FixIndices( Math.Min( oldItemIndex, newItemIndex ) );
            return node;
        }
        
        /// <summary>
        /// Copy node to new parent. If newChildIndex is specified, node will be inserted at that index, otherwise node will be placed after last child
        /// </summary>
        /// <param name="node"></param>
        /// <param name="newParent"></param>
        /// <param name="newChildIndex"></param>
        /// <returns>Copied node</returns>
        public Node Copy( [NotNull] Node node, [NotNull] Node newParent, Int32 newChildIndex = -1 )
        {
            CheckNodeBelongsTree( node,      nameof(node) );
            CheckNodeBelongsTree( newParent, nameof(newParent) );

            //Cannot copy inside of itself
            if( node == newParent || GetParents( newParent ).Contains( node ) )
                return node;

            var oldDepth     = node.Depth;
            var newDepth     = newParent.Depth + 1;
            var deltaDepth   = newDepth - oldDepth;
            var newItemIndex = ChildIndex2GlobalIndex( newParent, newChildIndex );
            var oldItemIndex = node._index;

            Node result      = null;
            var  subtreeSize = GetSubtreeSize( node );
            if ( oldItemIndex < newItemIndex )
            {
                for ( var i = 0; i < subtreeSize; i++ )
                {
                    var oldNode      = _nodes[ oldItemIndex + i ];
                    var oldNodeValue = oldNode.Value;
                    var oldNodeDepth = oldNode.Depth;
                    _nodes.Insert( newItemIndex + i, new Node( 0, oldNodeDepth + deltaDepth, this ) );
                    var newNode = _nodes[ newItemIndex + i ];
                    newNode.Value = oldNodeValue;
                    if( result == null )
                        result = newNode;
                }
            }
            else
            {
                for ( var i = 0; i < subtreeSize; i++ )
                {
                    var oldNode      = _nodes[ oldItemIndex + 2*i ];
                    var oldNodeValue = oldNode.Value;
                    var oldNodeDepth = oldNode.Depth;
                    _nodes.Insert( newItemIndex + i, new Node( 0, oldNodeDepth + deltaDepth, this ) );
                    var newNode = _nodes[ newItemIndex + i ];
                    newNode.Value = oldNodeValue;
                    if( result == null )
                        result = newNode;

                }
            }

            FixIndices( Math.Min( oldItemIndex, newItemIndex ) );
            return result;
        } 

        /// <summary>
        /// Remove node and all its children from tree
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Count of removed nodes</returns>
        public Int32 Remove( [NotNull] Node node )
        {
            CheckNodeBelongsTree( node, nameof(node) );

            var removeCount = GetSubtreeSize( node );
            var i           = removeCount;
            while ( i-- > 0 )
            {
                _nodes.RemoveAt( node._index );                
            }

            FixIndices( node._index );

            return removeCount;
        }

        /// <summary>
        /// Clear entire tree
        /// </summary>
        public void Clear( )
        {
            _nodes.Clear();
        }

        /// <summary>
        /// Generate string representation of tree hierarchy
        /// </summary>
        /// <returns></returns>
        public String ToHierarchyString( )
        {
            var sb = new StringBuilder();
            foreach ( var node in _nodes )
            {
                sb.Append( new String( ' ', node.Depth * 2 ) );
                sb.AppendLine( $"Value = '{node.Value}' (level = {node.Depth}, {(node.Depth > 0 ? $"parent '{GetParent( node ).Value}'" : "")})" );
            }

            return sb.ToString();
        }

        public override String ToString( )
        {
            return $"{GetType()} ({_nodes.Count})";
        }

        /// <summary>
        /// Compare to trees by structure and values of nodes
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check undelying list consistency, used as postcondition for tests
        /// </summary>
        /// <param name="errorIndex"></param>
        /// <returns></returns>
        public Boolean IsTreeConsistent( out Int32 errorIndex )
        {
            if( _nodes.Count == 0 )
            {
                errorIndex = -1;
                return true;
            }

            for ( var i = 0; i < _nodes.Count; i++ )
            {
                if ( i != _nodes[ i ]._index )
                {
                    errorIndex = i;
                    return false;
                }
            }

            if ( _nodes[ 0 ]._depth != 0 )
            {
                errorIndex = 0;
                return false;
            }

            for ( var i = 1; i < _nodes.Count; i++ )
            {
                if( _nodes[i]._depth > _nodes[i - 1]._depth + 1 || _nodes[i]._depth <= 0 )
                {
                    errorIndex = i;
                    return false;
                }
            }

            errorIndex = -1;
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

        private Int32 ChildIndex2GlobalIndex( Node parent, Int32 childIndex )
        {
            var result      = -1;
            var parentDepth = parent.Depth;
            var i           = 0;
            for ( i = parent._index + 1; i < _nodes.Count; i++ )
            {
                var childNode  = _nodes[ i ];
                var childDepth = childNode.Depth;

                if( childDepth == parentDepth + 1 && childIndex-- == 0 )           //Get needed child
                {
                    result = i;
                    break;
                }
                else if( childDepth <= parentDepth )                               //No more childs
                    break;
            }

            if ( result == -1 )             //childIndex is out of range, select index after last child
                result = i;

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

            [SerializeField]
            internal Int32 _depth;

            /// <summary>
            /// Depth of the node in the tree, root node has 0 depth
            /// </summary>
            public Int32    Depth => _depth;

            public Node     Parent => Owner.GetParent( this );

            public readonly TreeList<T> Owner;

            /// <summary>
            /// Add child node to this node
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public Node AddChild( T value )
            {
                return Owner.Add( value, this );
            }

            /// <summary>
            /// Add node to the parent of given node
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public Node AddSibling( T value )
            {
                return Parent.AddChild( value );
            }

            /// <summary>
            /// Add children to this node
            /// </summary>
            /// <param name="values"></param>
            public void AddChildren( params T[] values )
            {
                foreach ( var v in values )
                {
                    Owner.Add( v, this );    
                }
            }

            /// <summary>
            /// Get children of this node. See <see cref="TreeList{T}.GetChildren"/> for non alloc method overload
            /// </summary>
            /// <param name="includeSelf">Include this node</param>
            /// <param name="recursive">Include all children of children</param>
            /// <returns>Enumerator over children nodes</returns>
            public IEnumerable<Node> GetChildren( Boolean includeSelf = false, Boolean recursive = false )
            {
                return Owner.GetChildren( this, includeSelf, recursive );
            }

            /// <summary>
            /// Compare depth and value of nodes
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public Boolean StructuralEqual( Node other )
            {
                if ( ReferenceEquals( null, other ) ) return false;
                if ( ReferenceEquals( this, other ) ) return true;

                return Depth == other.Depth && Equals( Value, other.Value );
            }

            /// <summary>
            /// Compare values of nodes
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
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
            
            internal Int32 _index;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize( )
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize( )
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