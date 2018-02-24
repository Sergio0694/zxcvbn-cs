using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Zxcvbn_cs.Helpers;
using Zxcvbn_cs.Models.MatchTypes;

namespace Zxcvbn_cs.Matchers
{
    /// <summary>
    /// This matcher applies some known l33t character substitutions and then attempts to match against passed in dictionary matchers.
    /// This detects passwords like 4pple which has a '4' substituted for an 'a'
    /// </summary>
    public sealed class L33tMatcher : IMatcher
    {
        /// <summary>
        /// Gets the list of linked dictionaries
        /// </summary>
        private readonly IReadOnlyList<DictionaryMatcher> DictionaryMatchers;

        /// <summary>
        /// Gets the substitution characters to use to test the passwords
        /// </summary>
        private static readonly Dictionary<char, string> SubstitutionsMap = new Dictionary<char, string>
        {
            ['a'] = "4@",
            ['b'] = "8",
            ['c'] = "({[<",
            ['e'] = "3",
            ['g'] = "69",
            ['i'] = "1!|",
            ['l'] = "1|7",
            ['o'] = "0",
            ['s'] = "$5",
            ['t'] = "+7",
            ['x'] = "%",
            ['z'] = "2"
        };

        /// <summary>
        /// Create a l33t matcher that applies substitutions and then matches agains the passed in list of dictionary matchers.
        /// </summary>
        /// <param name="dictionaryMatchers">The list of dictionary matchers to check transformed passwords against</param>
        public L33tMatcher(IReadOnlyList<DictionaryMatcher> dictionaryMatchers)
        {
            DictionaryMatchers = dictionaryMatchers;
        }

        /// <summary>
        /// Apply applicable l33t transformations and check <paramref name="password"/> against the dictionaries. 
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <param name="cancellationToken">The token for the operation</param>
        /// <returns>A list of match objects where l33t substitutions match dictionary words</returns>
        /// <seealso cref="L33tDictionaryMatch"/>
        public IEnumerable<Match> MatchPassword(string password, CancellationToken cancellationToken)
        {
            // Get the subsitutions
            List<Dictionary<char, char>> subs = EnumerateSubtitutions(GetRelevantSubstitutions(password));
            cancellationToken.ThrowIfCancellationRequested();

            // Find the matches
            List<L33tDictionaryMatch> output = new List<L33tDictionaryMatch>();
            foreach (Dictionary<char, char> subDict in subs)
            {
                // Get the current substitution
                string subPassword = TranslateString(subDict, password);
                foreach (DictionaryMatcher matcher in DictionaryMatchers)
                {
                    // Match using the current dictionary
                    cancellationToken.ThrowIfCancellationRequested();
                    IEnumerable<DictionaryMatch> matches = matcher.MatchPassword(subPassword, cancellationToken).OfType<DictionaryMatch>();

                    // Compute the results and add them to the output
                    output.AddRange(
                        from match in matches
                        let token = password.Substring(match.i, match.j - match.i + 1)
                        let usedSubs = subDict.Where(kv => token.Contains(kv.Key)) // Count subs ised in matched token
                        where usedSubs.Any() // Only want matches where substitutions were used
                        select new L33tDictionaryMatch(match)
                        {
                            Token = token,
                            Subs = usedSubs.ToDictionary(kv => kv.Key, kv => kv.Value)
                        });
                }
            }

            // Calculate the entropy for the results
            foreach (L33tDictionaryMatch match in output) CalulateL33tEntropy(match);
            return output;
        }

        private void CalulateL33tEntropy(L33tDictionaryMatch match)
        {
            // I'm a bit dubious about this function, but I have duplicated zxcvbn functionality regardless
            int possibilities = 0;
            foreach (KeyValuePair<char, char> kvp in match.Subs)
            {
                int subbedChars = match.Token.Count(c => c == kvp.Key);
                int unsubbedChars = match.Token.Count(c => c == kvp.Value); // Won't this always be zero?
                possibilities += Enumerable.Range(0, Math.Min(subbedChars, unsubbedChars) + 1).Sum(i => (int)PasswordScoring.Binomial(subbedChars + unsubbedChars, i));
            }

            // Calculate the base entropy
            double entropy = Math.Log(possibilities, 2);

            // In the case of only a single subsitution (e.g. 4pple) this would otherwise come out as zero, so give it one bit
            match.L33tEntropy = entropy < 1 ? 1 : entropy;
            match.Entropy += match.L33tEntropy;

            // We have to recalculate the uppercase entropy -- the password matcher will have used the subbed password not the original text
            match.Entropy -= match.UppercaseEntropy;
            match.UppercaseEntropy = PasswordScoring.CalculateUppercaseEntropy(match.Token);
            match.Entropy += match.UppercaseEntropy;
        }

        private string TranslateString(Dictionary<char, char> charMap, string str)
        {
            // Make substitutions from the character map wherever possible
            return new String(str.Select(c => charMap.ContainsKey(c) ? charMap[c] : c).ToArray());
        }

        private Dictionary<char, string> GetRelevantSubstitutions(string password)
        {
            // Return a map of only the useful substitutions, i.e. only characters that the password contains a substituted form of   
            return SubstitutionsMap.Where(kv => kv.Value.Any(password.Contains)).ToDictionary(kv => kv.Key, kv => new String(kv.Value.Where(password.Contains).ToArray()));
        }

        private List<Dictionary<char, char>> EnumerateSubtitutions(Dictionary<char, string> table)
        {
            // Produce a list of maps from l33t character to normal character. Some substitutions can be more than one normal character though,
            //  so we have to produce an entry that maps from the l33t char to both possibilities

            //XXX: This function produces different combinations to the original in zxcvbn. It may require some more work to get identical.

            //XXX: The function is also limited in that it only ever considers one substitution for each l33t character (e.g. ||ke could feasibly
            //     match 'like' but this method would never show this). My understanding is that this is also a limitation in zxcvbn and so I
            //     feel no need to correct it here.

            List<Dictionary<char, char>> subs = new List<Dictionary<char, char>> { new Dictionary<char, char>() }; // Must be at least one mapping dictionary to work
            foreach (KeyValuePair<char, string> mapPair in table)
            {
                char normalChar = mapPair.Key;
                foreach (char l33tChar in mapPair.Value)
                {
                    // Can't add while enumerating so store here
                    List<Dictionary<char, char>> addedSubs = new List<Dictionary<char, char>>();
                    foreach (Dictionary<char, char> subDict in subs)
                    {
                        if (subDict.ContainsKey(l33tChar))
                        {
                            // This mapping already contains a corresponding normal character for this character, so keep the existing one as is
                            //   but add a duplicate with the mappring replaced with this normal character
                            Dictionary<char, char> newSub = new Dictionary<char, char>(subDict) { [l33tChar] = normalChar };
                            addedSubs.Add(newSub);
                        }
                        else subDict[l33tChar] = normalChar;
                    }
                    subs.AddRange(addedSubs);
                }
            }
            return subs;
        }
    }
}
