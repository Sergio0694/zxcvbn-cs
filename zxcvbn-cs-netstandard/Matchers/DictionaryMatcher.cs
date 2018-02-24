using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Zxcvbn_cs.Helpers;
using Zxcvbn_cs.Models.MatchTypes;

namespace Zxcvbn_cs.Matchers
{
    /// <summary>
    /// <para>This matcher reads in a list of words (in frequency order) and matches substrings of the password against that dictionary.</para>
    /// 
    /// <para>The dictionary to be used can be specified directly by passing an enumerable of strings through the constructor (e.g. for
    /// matching agains user inputs). Most dictionaries will be in word list files.</para>
    /// 
    /// <para>Using external files is a departure from the JS version of Zxcvbn which bakes in the word lists, so the default dictionaries
    /// have been included in the Zxcvbn assembly as embedded resources (to remove the external dependency). Thus when a word list is specified
    /// by name, it is first checked to see if it matches and embedded resource and if not is assumed to be an external file. </para>
    /// 
    /// <para>Thus custom dictionaries can be included by providing the name of an external text file, but the built-in dictionaries (english.lst,
    /// female_names.lst, male_names.lst, passwords.lst, surnames.lst) can be used without concern about locating a dictionary file in an accessible
    /// place.</para>
    /// 
    /// <para>Dictionary word lists must be in decreasing frequency order and contain one word per line with no additional information.</para>
    /// </summary>
    public sealed class DictionaryMatcher : IMatcher
    {
        private const string DictionaryPattern = "dictionary";
        private readonly string DictionaryName;
        private readonly string DictionaryPath;
        private Dictionary<string, int> _RankedDictionary;

        // Semaphore to synchronize the dictionary parsing
        private readonly SemaphoreSlim DictionarySemaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Creates a new dictionary matcher. <paramref name="wordListPath"/> must be the path (relative or absolute) to a file containing one word per line,
        /// entirely in lowercase, ordered by frequency (decreasing); or <paramref name="wordListPath"/> must be the name of a built-in dictionary.
        /// </summary>
        /// <param name="name">The name provided to the dictionary used</param>
        /// <param name="wordListPath">The filename of the dictionary (full or relative path) or name of built-in dictionary</param>
        public DictionaryMatcher(string name, string wordListPath)
        {
            DictionaryName = name;
            DictionaryPath = wordListPath;
        }

        /// <summary>
        /// Match substrings of password agains the loaded dictionary
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <param name="cancellationToken">The token for the operation</param>
        /// <returns>An enumerable of dictionary matches</returns>
        /// <seealso cref="DictionaryMatch"/>
        public IEnumerable<Match> MatchPassword(string password, CancellationToken cancellationToken)
        {
            // Build the dictionary to use if necessary
            DictionarySemaphore.Wait(cancellationToken);

            // Build the dictionary
            if (_RankedDictionary == null)
            {
                _RankedDictionary = BuildRankedDictionary(DictionaryPath);
            }
            DictionarySemaphore.Release();

            // Check the token state
            cancellationToken.ThrowIfCancellationRequested();
            string passwordLower = password.ToLower();

            // Compute and return the result list
            return
                (from i in Enumerable.Range(0, password.Length)
                from j in Enumerable.Range(i, password.Length - i)
                let psub = passwordLower.Substring(i, j - i + 1)
                where _RankedDictionary.ContainsKey(psub)
                let rank = _RankedDictionary[psub]
                let token = password.Substring(i, j - i + 1) // Could have different case so pull from password
                let baseEntropy = Math.Log(rank, 2)
                let upperEntropy = PasswordScoring.CalculateUppercaseEntropy(token)
                select new DictionaryMatch
                {
                    Pattern = DictionaryPattern,
                    i = i,
                    j = j,
                    Token = token,
                    MatchedWord = psub,
                    Rank = rank,
                    DictionaryName = DictionaryName,
                    Cardinality = _RankedDictionary.Count,
                    BaseEntropy = baseEntropy,
                    UppercaseEntropy = upperEntropy,
                    Entropy = baseEntropy + upperEntropy
                }).ToList();
        }

        private Dictionary<string, int> BuildRankedDictionary(string path)
        {
            // Look first to wordlists embedded in assembly (i.e. default dictionaries) otherwise treat as file path
            HashSet<string> lines = new HashSet<string>();
            int read = 0;
            using (StreamReader reader = File.OpenText(path))
            {
                try
                {
                    do
                    {
                        if (reader.ReadLine() is string line)
                        {
                            lines.Add(line);
                            read++;
                        }
                        else break;
                    } while (Zxcvbn.DictionaryLengthLimit == null || read <= Zxcvbn.DictionaryLengthLimit);
                }
                catch
                {
                    // Who cares?
                }
            }
            return BuildRankedDictionary(lines);
        }

        private Dictionary<string, int> BuildRankedDictionary(IEnumerable<string> wordList)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            int i = 1;
            foreach (string word in wordList)
            {
                // The word list is assumed to be in increasing frequency order
                dict[word] = i++;
            }
            return dict;
        }

        /// <summary>
        /// Helps the GC to collect the resources used by this instance
        /// </summary>
        public void Dispose()
        {
            _RankedDictionary?.Clear();
            _RankedDictionary = null;
        }
    }
}
