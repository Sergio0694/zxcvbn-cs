using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zxcvbn_cs.Helpers
{
    /// <summary>
    /// A single grouping from the GroupAdjacent function, includes start and end indexes for the grouping in addition to standard IGrouping bits
    /// </summary>
    /// <typeparam name="TElement">Type of grouped elements</typeparam>
    /// <typeparam name="TKey">Type of key used for grouping</typeparam>
    public sealed class AdjacentGrouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        /// <summary>
        /// The key value for this grouping
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// The start index in the source enumerable for this group (i.e. index of first element)
        /// </summary>
        public int StartIndex { get; private set; }

        /// <summary>
        /// The end index in the enumerable for this group (i.e. the index of the last element)
        /// </summary>
        public int EndIndex { get; private set; }

        private readonly IEnumerable<TElement> m_groupItems;

        internal AdjacentGrouping(TKey key, IEnumerable<TElement> groupItems, int startIndex, int endIndex)
        {
            Key = key;
            StartIndex = startIndex;
            EndIndex = endIndex;
            m_groupItems = groupItems;
        }

        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => m_groupItems.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_groupItems.GetEnumerator();
    }
}
