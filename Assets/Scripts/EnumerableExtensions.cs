using System;
using System.Collections.Generic;
using System.Linq;

namespace Silentor.TreeControl
{
    public static class EnumerableExtensions
    {
        public static T MaxBy<T, TKey> ( this IReadOnlyList<T> @this, Func<T, TKey> selector) where TKey : IComparable<TKey>
        {
            if( @this == null )
                throw new ArgumentNullException();

            if ( @this.Count == 0 )
                throw new InvalidCastException();

            var maxElement = @this[ 0 ];
            var maxKey     = selector( @this[0] );

            for ( int i = 1; i < @this.Count; i++ )
            {
                var key = selector( @this[ i ] );
                if ( key.CompareTo( maxKey ) > 0 )
                {
                    maxKey     = key;
                    maxElement = @this[ i ];
                }
            }

            return maxElement;
        }

        public static String JoinToString<T>( this IReadOnlyList<T> @this, Func<T, String> toString = null )
        {
            if( toString != null )
                return String.Join( ", ", @this.Select( toString ) );

            return String.Join( ", ", @this );
        }

    }
}