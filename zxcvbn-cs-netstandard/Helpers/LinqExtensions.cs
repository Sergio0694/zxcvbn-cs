using System;
using System.Collections.Generic;

namespace Zxcvbn_cs.Helpers
{
    /// <summary>
    /// Useful shared Linq extensions
    /// </summary>
    internal static class LinqExtensions
    {
        /// <summary>
        /// Used to group elements by a key function, but only where elements are adjacent
        /// </summary>
        /// <param name="keySelector">Function used to choose the key for grouping</param>
        /// <param name="source">THe enumerable being grouped</param>
        /// <returns>An enumerable of <see cref="AdjacentGrouping{TKey, TElement}"/> </returns>
        /// <typeparam name="TKey">Type of key value used for grouping</typeparam>
        /// <typeparam name="TSource">Type of elements that are grouped</typeparam>
        public static IEnumerable<AdjacentGrouping<TKey, TSource>> GroupAdjacent<TKey, TSource>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            // Initialization
            TKey prevKey = default(TKey);
            int prevStartIndex = 0;
            bool prevInit = false;
            List<TSource> itemsList = new List<TSource>();

            // Iterate over the source collection
            int i = 0;
            foreach (TSource item in source)
            {
                TKey key = keySelector(item);
                if (prevInit)
                {
                    if (!prevKey.Equals(key))
                    {
                        yield return new AdjacentGrouping<TKey, TSource>(key, itemsList, prevStartIndex, i - 1);
                        prevKey = key;
                        itemsList = new List<TSource> { item };
                        prevStartIndex = i;
                    }
                    else itemsList.Add(item);
                }
                else
                {
                    prevKey = key;
                    itemsList.Add(item);
                    prevInit = true;
                }
                i++;
            }
            if (prevInit) yield return new AdjacentGrouping<TKey, TSource>(prevKey, itemsList, prevStartIndex, i - 1);
        }
    }
}
