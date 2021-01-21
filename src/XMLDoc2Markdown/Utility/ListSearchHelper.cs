using System;
using System.Collections.Generic;
using System.Text;

namespace XMLDoc2Markdown.Utility
{
    internal static class ListSearchHelper
    {
        // Source: https://source.dot.net/#System.Private.CoreLib/ArraySortHelper.cs,f3d6c6df965a8a86
        // Modified for IReadOnlyList<T> 
        public static int BinarySearch<T>(this IReadOnlyList<T> collection, int index, int length, T value, IComparer<T> comparer)
        {
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (collection.Count < index + length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            int lo = index;
            int hi = index + length - 1;

            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(collection[i], value);
                if (order == 0)
                {
                    return i;
                }

                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }
        public static int BinarySearch<T>(this IReadOnlyList<T> collection, int index, int length, T value) where T : IComparable<T> => BinarySearch(collection, index, length, value, Comparer<T>.Default);
        
        public static int BinarySearch<T>(this IReadOnlyList<T> collection, T value, IComparer<T> comparer) => BinarySearch(collection, 0, collection.Count, value, comparer);

        public static int BinarySearch<T>(this IReadOnlyList<T> collection, T value) where T : IComparable<T> => BinarySearch(collection, 0, collection.Count, value, Comparer<T>.Default);
    }
}
