using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Zxcvbn_cs.Matchers;

namespace Zxcvbn_cs
{
    /// <summary>
    /// <para>This matcher factory will use all of the default password matchers.</para>
    /// 
    /// <para>Default dictionary matchers use the built-in word lists: passwords, english, male_names, female_names, surnames</para>
    /// <para>Also matching against: user data, all dictionaries with l33t substitutions</para>
    /// <para>Other default matchers: repeats, sequences, digits, years, dates, spatial</para>
    /// </summary>
    internal sealed class MatchersFactory : IDisposable
    {
        /// <summary>
        /// Gets the list of matchers to use
        /// </summary>
        public List<IMatcher> Matchers { get; private set; }

        /// <summary>
        /// Create a matcher factory that uses the default list of pattern matchers
        /// </summary>
        public MatchersFactory([NotNull] IReadOnlyList<string> dictionaryPaths)
        {
            // Default dictionaries
            DictionaryMatcher[] dictionaryMatchers =
                (from path in dictionaryPaths
                let name = Path.GetFileName(path)
                let fullPath = Path.GetFullPath(path)
                select new DictionaryMatcher(name, fullPath)).ToArray();

            // Default matchers
            Matchers = new List<IMatcher>
            {
                new RepeatMatcher(),
                new SequenceMatcher(),
                new RegexMatcher("\\d{3,}", 10, true, "digits"),
                new RegexMatcher("19\\d\\d|200\\d|201\\d", 119, false, "year"),
                new DateMatcher(),
                new SpatialMatcher()
            };

            // Merge the dictionaries
            Matchers.AddRange(dictionaryMatchers);
            Matchers.Add(new L33tMatcher(dictionaryMatchers));
        }

        /// <summary>
        /// Helps the GC to collect the resources used by this instance
        /// </summary>
        public void Dispose()
        {
            foreach (IDisposableMatcher matcher in Matchers.Where(m => m is IDisposableMatcher).Cast<IDisposableMatcher>())
            {
                matcher.Dispose();
            }
            Matchers.Clear();
            Matchers = null;
        }
    }
}
