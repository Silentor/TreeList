﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Silentor.TreeControl
{
    [Serializable]
    public class TreeList<T> : ISerializationCallbackReceiver
    {
        public TreeNodeSerializable[] SerializableNodes = Array.Empty<TreeNodeSerializable>();

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
                var newNode = new TreeNode ( null, 0, this ) { Value = value };
                _nodesInternal.Add( newNode );
                return newNode;
            }
            else
            {
                var (parentIndex, childIndex) = GetIndexToAppendChild( parent );
                var newNode = new TreeNode( parent, parent.Level + 1, this ){ Value = value };
                _nodesInternal.Insert( childIndex, newNode );

                return newNode;
            }
        }

        public IEnumerable<TreeNode> GetChilds( [NotNull] TreeNode node, Boolean includeItself )
        {
            if ( node == null ) throw new ArgumentNullException( nameof(node) );

            var index = _nodesInternal.FindIndex( x => x == node );
            if ( index < 0 )
                throw new InvalidOperationException( "Node not found" );

            if ( includeItself )
                yield return node;
            for ( int i = index + 1; i < _nodesInternal.Count && _nodesInternal[ i ].Level >= node.Level + 1; i++ )
                if( _nodesInternal[i].Level == node.Level + 1 )
                    yield return _nodesInternal[ i ];
        }

        public IEnumerable<TreeNode> GetChildsRecursive( [NotNull] TreeNode node, Boolean includeItself )
        {
            if ( node == null ) throw new ArgumentNullException( nameof(node) );

            var index = _nodesInternal.FindIndex( x => x == node );
            if ( index < 0 )
                throw new InvalidOperationException( "Node not found" );

            if ( includeItself )
                yield return node;
            for ( int i = index + 1; i < _nodesInternal.Count && _nodesInternal[ i ].Level >= node.Level + 1; i++ )
                yield return _nodesInternal[ i ];
        }

        public TreeNode GetParent( [NotNull] TreeNode node )
        {
            if ( node == null ) throw new ArgumentNullException( nameof(node) );
            Assert.IsTrue( _nodesInternal.Contains( node ) );

            return node.Level == 0 ? null : node.Parent;
        }

        public IEnumerable<TreeNode> GetParents( [NotNull] TreeNode node )
        {
            if ( node == null ) throw new ArgumentNullException( nameof(node) );
            Assert.IsTrue( _nodesInternal.Contains( node ) );

            while ( node.Level != 0 )
            {
                node = GetParent( node );
                yield return node;
            }
        }

        public Int32 Move( [NotNull] TreeNode node, [NotNull] TreeNode newParent )
        {
            if ( node      == null ) throw new ArgumentNullException( nameof(node) );
            if ( newParent == null ) throw new ArgumentNullException( nameof(newParent) );

            //Check if node is not parent of newParent
            Assert.IsTrue( node != newParent );
            Assert.IsFalse( GetParents( newParent ).Contains( node ) ); //Cannot move inside of itself

            var buffer = GetChildsRecursive( node, true ).ToArray();
            var counter = Remove( node );

            var (parentIndex, childIndex) = GetIndexToAppendChild( newParent );
            var levelOffset = newParent.Level + 1 - node.Level;
            node.Parent = newParent;
            foreach ( var b in buffer )
            {
                _nodesInternal.Insert( childIndex++, b );
                b.Level += levelOffset;
            }

            return counter;
        } 

        public Int32 Remove( [NotNull] TreeNode node )
        {
            if ( node == null ) throw new ArgumentNullException( nameof(node) );

            var index = _nodesInternal.FindIndex( x => x == node );
            if ( index < 0 )
                throw new InvalidOperationException( "Node not found" );

            var childsLevel   = node.Level + 1;
            var removeCounter = 1;
            _nodesInternal.RemoveAt( index );
            while ( index <= _nodesInternal.Count - 1 && _nodesInternal[index].Level >= childsLevel )  //Remove childs
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
                sb.Append( new String( ' ', node.Level * 2 ) );
                sb.AppendLine( $"Value = '{node.Value}' (level = {node.Level}, parent {(node.Level > 0 ? GetParent( node ).Value : "")})" );
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
                var childIndex = _nodesInternal.FindIndex( parentIndex + 1, x => x.Level <= parentNode.Level );
                if ( childIndex >= 0 )
                    return ( parentIndex, childIndex );
                else
                    return (parentIndex, Nodes.Count );
            }
            else
                return (parentIndex, _nodesInternal.Count);
        }

        [Serializable]
        public class TreeNodeSerializable
        {
            public T     Value;
            public Int32 ParentIndex = 0;
            public Int32 Level;
        }

        [DebuggerDisplay("{Value}")]
        public class TreeNode
        {
            public T           Value;
            public TreeNode    Parent { get; internal set; }
            public Int32       Level  { get; internal set; }
            public TreeList<T> Owner  { get; private set; }

            public TreeNode( TreeNode parent, Int32 level, TreeList<T> owner )
            {
                Parent = parent;
                Level  = level;
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

            public void AddChilds( IEnumerable<T> values )
            {
                foreach ( var v in values )
                {
                    Owner.Add( v, this );    
                }
            }

            public IEnumerable<TreeNode> GetChilds( Boolean includeSelf )
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

                return Level == other.Level && Equals( Value, other.Value );
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
                SerializableNodes[i] = new TreeNodeSerializable { ParentIndex = _nodesInternal.IndexOf( node.Parent ), Value = node.Value, Level = node.Level };
            }

            //UnityEngine.Debug.Log( $"Serialize to {SerializableNodes.JoinToString( n => $"{n.Value}/{n.ParentIndex}" )}" );
        }

        public void OnAfterDeserialize( )
        {
            //UnityEngine.Debug.Log( $"Deserialize from {SerializableNodes.JoinToString( n => $"{n.Value}/{n.ParentIndex}" )}" );

            _nodesInternal.Clear();

            for ( var i = 0; i < SerializableNodes.Length; i++ )        
            {
                if ( i == 0 )
                {
                    Add( SerializableNodes[ i ].Value, null );
                }
                else
                {
                    var parent = _nodesInternal[ SerializableNodes[i].ParentIndex ];
                    var newNode = Add( SerializableNodes[i].Value, parent );
                    newNode.Level = parent.Level + 1;
                }
            }
        }
    }
}