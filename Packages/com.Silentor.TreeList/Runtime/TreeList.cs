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
    public class TreeList<T> : ISerializationCallbackReceiver, IEnumerable<TreeList<T>.TreeNode>
    {
        [SerializeField]
        private TreeNodeSerializable[] SerializableNodes = Array.Empty<TreeNodeSerializable>();

        public IReadOnlyList<TreeNode> Nodes => _nodesInternal;
        public TreeNode                Root  => _nodesInternal.Count > 0 ? _nodesInternal[ 0 ] : null;
        public Int32                   Count => _nodesInternal.Count;

        private List<TreeNode> _nodesInternal = new ();

        public TreeNode Add( T value, TreeNode parent )
        {
            if ( parent == null )
            {
                if ( _nodesInternal.Count > 0 )
                    throw new InvalidOperationException( "Root node already exists" );
                var newNode = new TreeNode ( null, this ) { Value = value };
                _nodesInternal.Add( newNode );
                return newNode;
            }
            else
            {
                CheckNodeBelongsTree( parent, nameof(parent) );
                var (parentIndex, childIndex) = GetIndexToAppendChild( parent );
                var newNode = new TreeNode( parent, this ){ Value = value };
                _nodesInternal.Insert( childIndex, newNode );

                return newNode;
            }
        }

        public IEnumerable<TreeNode> GetChilds( [NotNull] TreeNode node, Boolean includeItself )
        {
            var index = CheckNodeBelongsTree( node, nameof(node) );
            
            if ( includeItself )
                yield return node;
            for ( int i = index + 1; i < _nodesInternal.Count && _nodesInternal[ i ].Depth >= node.Depth + 1; i++ )
                if( _nodesInternal[i].Depth == node.Depth + 1 )
                    yield return _nodesInternal[ i ];
        }

        public IEnumerable<TreeNode> GetChildsRecursive( [NotNull] TreeNode node, Boolean includeItself )
        {
            var index = CheckNodeBelongsTree( node, nameof(node) );

            if ( includeItself )
                yield return node;
            for ( int i = index + 1; i < _nodesInternal.Count && _nodesInternal[ i ].Depth >= node.Depth + 1; i++ )
                yield return _nodesInternal[ i ];
        }

        public TreeNode GetParent( [NotNull] TreeNode node )
        {
            CheckNodeBelongsTree( node, nameof(node) );

            return node.Depth == 0 ? null : node.Parent;
        }

        public IEnumerable<TreeNode> GetParents( [NotNull] TreeNode node )
        {
            CheckNodeBelongsTree( node, nameof(node) );

            while ( node.Depth != 0 )
            {
                node = GetParent( node );
                yield return node;
            }
        }

        public Int32 Move( [NotNull] TreeNode node, [NotNull] TreeNode newParent )
        {
            CheckNodeBelongsTree( node, nameof(node) );
            CheckNodeBelongsTree( newParent, nameof(newParent) );

            //Check if node is not parent of newParent
            Assert.IsTrue( node != newParent );
            Assert.IsFalse( GetParents( newParent ).Contains( node ) ); //Cannot move inside of itself

            var buffer = GetChildsRecursive( node, true ).ToArray();
            var counter = Remove( node );

            var (parentIndex, childIndex) = GetIndexToAppendChild( newParent );
            var levelOffset = newParent.Depth + 1 - node.Depth;
            node.Parent = newParent;
            foreach ( var b in buffer )
            {
                _nodesInternal.Insert( childIndex++, b );
                b.Depth += levelOffset;
            }

            return counter;
        } 

        public Int32 Remove( [NotNull] TreeNode node )
        {
            var index = CheckNodeBelongsTree( node, nameof(node) );

            var childsLevel   = node.Depth + 1;
            var removeCounter = 1;
            _nodesInternal.RemoveAt( index );
            while ( index <= _nodesInternal.Count - 1 && _nodesInternal[index].Depth >= childsLevel )  //Remove childs
            {
                _nodesInternal.RemoveAt( index );
                removeCounter++;
            }

            return removeCounter;
        }

        public void Clear( )
        {
            _nodesInternal.Clear();
        }

        public String ToHierarchyString( )
        {
            var sb = new StringBuilder();
            foreach ( var node in _nodesInternal )
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

            if ( Nodes.Count != other.Nodes.Count ) return false;

            for ( int i = 0; i < Nodes.Count; i++ )
            {
                if(! Nodes[i].StructuralEqual( other.Nodes[i] ) )
                   return false;
            }

            return true;
        }

        private (Int32 parentIndex, Int32 childIndex) GetIndexToAppendChild( TreeNode parentNode )
        {
            var parentIndex = _nodesInternal.FindIndex( x => x == parentNode );
            if ( parentIndex < 0 )
                throw new InvalidOperationException( "Parent node not found" );

            //To protect childs order
            if ( parentIndex < _nodesInternal.Count - 1 )
            {
                var childIndex = _nodesInternal.FindIndex( parentIndex + 1, x => x.Depth <= parentNode.Depth );
                if ( childIndex >= 0 )
                    return ( parentIndex, childIndex );
            }

            return (parentIndex, _nodesInternal.Count);
        }

        private Int32 CheckNodeBelongsTree( TreeNode node, String paramName )
        {
            if ( node == null )
                throw new ArgumentNullException( $"{paramName} is null" );

            var index = _nodesInternal.FindIndex( x => x == node );
            if ( index < 0 )
                throw new ArgumentException( $"TreeList node {paramName} not found" );

            return index;
        }

        [Serializable]
        public class TreeNodeSerializable
        {
            public T     Value;
            public Int32 Depth;
        }

        [DebuggerDisplay("{Value}")]
        public class TreeNode
        {
            public T           Value;
            public TreeNode    Parent { get; internal set; }
            public Int32       Depth  { get; internal set; }
            public TreeList<T> Owner  { get;  }

            public TreeNode( TreeNode parent, TreeList<T> owner )
            {
                Parent = parent;
                Depth  = parent != null ? parent.Depth + 1 : 0;
                Owner  = owner;
            }

            public TreeNode AddChild( T value )
            {
                return Owner.Add( value, this );
            }

            public TreeNode AddSibling( T value )
            {
                return GetParent().AddChild( value );
            }

            public void AddChildren( params T[] values )
            {
                foreach ( var v in values )
                {
                    Owner.Add( v, this );    
                }
            }

            public IEnumerable<TreeNode> GetChildren( Boolean includeSelf )
            {
                return Owner.GetChilds( this, includeSelf );
            }

            public TreeNode GetParent( )
            {
                return Owner.GetParent( this );
            }

            public Boolean StructuralEqual( TreeNode other )
            {
                if ( ReferenceEquals( null, other ) ) return false;
                if ( ReferenceEquals( this, other ) ) return true;

                return Depth == other.Depth && Equals( Value, other.Value );
            }

            public Boolean ValueEqual( TreeNode other )
            {
                if ( ReferenceEquals( null, other ) ) return false;
                if ( ReferenceEquals( this, other ) ) return true;

                return Equals( Value, other.Value );
            }

            // public override Int32 GetHashCode( )
            // {
            //     return HashCode.Combine( Owner.GetHashCode(), ParentIndex, Level, Value.GetHashCode() );
            // }
        }

        public void OnBeforeSerialize( )
        {
            SerializableNodes = new TreeNodeSerializable[ Nodes.Count ];
            for ( var i = 0; i < Nodes.Count; i++ )
            {
                var node = Nodes[ i ];
                SerializableNodes[i] = new TreeNodeSerializable { Value = node.Value, Depth = node.Depth };
            }

            //UnityEngine.Debug.Log( $"Serialize to {SerializableNodes.JoinToString( n => $"{n.Value}/{n.ParentIndex}" )}" );
        }

        public void OnAfterDeserialize( )
        {
            //UnityEngine.Debug.Log( $"Deserialize from {SerializableNodes.JoinToString( n => $"{n.Value}/{n.ParentIndex}" )}" );

            _nodesInternal.Clear();
            var parentsList = new List<TreeNode>();

            for ( var i = 0; i < SerializableNodes.Length; i++ )        
            {
                //Reconstruct parent from level
                var      serializedNode = SerializableNodes[ i ];
                TreeNode newNode;
                if( serializedNode.Depth == 0 )
                {
                    newNode = new TreeNode( null, this ) { Value = serializedNode.Value };
                    _nodesInternal.Add( newNode );
                }
                else
                {
                    var parent = parentsList[ serializedNode.Depth - 1 ];
                    newNode = new TreeNode( parent, this ) { Value = serializedNode.Value };
                    _nodesInternal.Add( newNode );
                }

                if( parentsList.Count == newNode.Depth )
                    parentsList.Add( newNode );                          //New level
                else 
                    parentsList[ newNode.Depth ] = newNode;              //Existing level
            }
        }

        public IEnumerator<TreeNode> GetEnumerator( )
        {
            return _nodesInternal.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator( )
        {
            return GetEnumerator();
        }
    }
}