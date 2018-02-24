using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Zxcvbn_cs.Helpers;
using Zxcvbn_cs.Models;
using Zxcvbn_cs.Models.MatchTypes;

namespace Zxcvbn_cs
{
    /// <summary>
    /// <para>Zxcvbn is used to estimate the strength of passwords. </para>
    /// 
    /// <para>This implementation is a port of the Zxcvbn JavaScript library by Dan Wheeler:
    /// https://github.com/lowe/zxcvbn</para>
    /// 
    /// <para>To quickly evaluate a password, use the <see cref="MatchPassword"/> static function.</para>
    /// 
    /// <para>To evaluate a number of passwords, create an instance of this object and repeatedly call the <see cref="EvaluatePassword"/> function.
    /// Reusing the the Zxcvbn instance will ensure that pattern matchers will only be created once rather than being recreated for each password
    /// e=being evaluated.</para>
    /// </summary>
    public sealed class Zxcvbn : IDisposable
    {
        // The bruteforce pattern
        private const string BruteforcePattern = "bruteforce";

        // The matchers factory to use to match the passwords
        [NotNull]
        private readonly MatchersFactory Factory;

        private static int? _DictionaryLengthLimit;

        /// <summary>
        /// Gets or sets the current limit for the length of each dictionary
        /// </summary>
        public static int? DictionaryLengthLimit
        {
            get => _DictionaryLengthLimit;
            set => _DictionaryLengthLimit = value <= 0
                ? throw new ArgumentOutOfRangeException(nameof(DictionaryLengthLimit), "The limit must be a positive value")
                : value;
        }

        /// <summary>
        /// <para>A static function to match a password against the default matchers without having to create
        /// an instance of Zxcvbn yourself, with supplied user data. </para>
        /// </summary>
        /// <param name="password">the password to test</param>
        /// <param name="dictionaryPaths">The list of dictionaries to use</param>
        /// <param name="token">The token for the operation</param>
        /// <returns>The results of the password evaluation</returns>
        [CanBeNull]
        public static Result MatchPassword([NotNull] string password, [NotNull] IReadOnlyList<string> dictionaryPaths, CancellationToken token = default(CancellationToken))
        {
            using (Zxcvbn zx = new Zxcvbn(dictionaryPaths))
            {
                return zx.EvaluatePassword(password, token);
            }
        }

        /// <summary>
        /// Create an instance of Zxcvbn that will use the given matcher factory to create matchers to use
        /// to find password weakness.
        /// </summary>
        /// <param name="dictionaryPaths">The list of dictionaries to use</param>
        public Zxcvbn([NotNull] IReadOnlyList<string> dictionaryPaths)
        {
            Factory = new MatchersFactory(dictionaryPaths);
        }

        /// <summary>
        /// <para>Perform the password matching on the given password and user inputs, returing the result structure with information
        /// on the lowest entropy match found.</para>
        /// </summary>
        /// <param name="password">Password</param>
        /// <param name="token">The token for the operation</param>
        /// <returns>Result for lowest entropy match</returns>
        public Result EvaluatePassword(string password, CancellationToken token)
        {
            // Start the timer
            if (token.IsCancellationRequested) return null;
            Stopwatch timer = Stopwatch.StartNew();

            // Process the password
            try
            {
                IEnumerable<Match>[] results = new IEnumerable<Match>[Factory.Matchers.Count];
                Parallel.For(0, Factory.Matchers.Count, i =>
                {
                    results[i] = Factory.Matchers[i].MatchPassword(password, token);
                });
                IEnumerable<Match> matches = results.Aggregate<IEnumerable<Match>, IEnumerable<Match>>(new List<Match>(), (l1, l2) => l1.Union(l2));
                return FindMinimumEntropyMatch(password, matches, timer);
            }
            catch (OperationCanceledException)
            {
                // Nevermind...
                return null;
            }
        }

        /// <summary>
        /// Returns a new result structure initialised with data for the lowest entropy result of all of the matches passed in, adding brute-force
        /// matches where there are no lesser entropy found pattern matches.
        /// </summary>
        /// <param name="matches">Password being evaluated</param>
        /// <param name="password">List of matches found against the password</param>
        /// <param name="stopwatch">The timer to calculate the elapsed time</param>
        /// <returns>A result object for the lowest entropy match sequence</returns>
        private static Result FindMinimumEntropyMatch(string password, IEnumerable<Match> matches, Stopwatch stopwatch)
        {
            // Minimum entropy up to position k in the password
            int bruteforce_cardinality = PasswordScoring.PasswordCardinality(password);
            double[] minimumEntropyToIndex = new double[password.Length];
            Match[] bestMatchForIndex = new Match[password.Length];
 
            for (int k = 0; k < password.Length; k++)
            {
                // Start with bruteforce scenario added to previous sequence to beat
                minimumEntropyToIndex[k] = (k == 0 ? 0 : minimumEntropyToIndex[k - 1]) + Math.Log(bruteforce_cardinality, 2);

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (Match match in matches.Where(m => m.j == k))
                {
                    // All matches that end at the current character, test to see if the entropy is less
                    double candidate_entropy = (match.i <= 0 ? 0 : minimumEntropyToIndex[match.i - 1]) + match.Entropy;
                    if (candidate_entropy < minimumEntropyToIndex[k])
                    {
                        minimumEntropyToIndex[k] = candidate_entropy;
                        bestMatchForIndex[k] = match;
                    }
                }
            }

            // Walk backwards through lowest entropy matches, to build the best password sequence
            List<Match> matchSequence = new List<Match>();
            for (int k = password.Length - 1; k >= 0; k--)
            {
                if (bestMatchForIndex[k] != null)
                {
                    matchSequence.Add(bestMatchForIndex[k]);
                    k = bestMatchForIndex[k].i; // Jump back to start of match
                }
            }
            matchSequence.Reverse();

            // The match sequence might have gaps, fill in with bruteforce matching
            // After this the matches in matchSequence must cover the whole string (i.e. match[k].j == match[k + 1].i - 1)
            if (matchSequence.Count == 0)
            {
                // To make things easy, we'll separate out the case where there are no matches so everything is bruteforced
                matchSequence.Add(new Match
                {
                    i = 0,
                    j = password.Length,
                    Token = password,
                    Cardinality = bruteforce_cardinality,
                    Pattern = BruteforcePattern,
                    Entropy = Math.Log(Math.Pow(bruteforce_cardinality, password.Length), 2)
                });
            }
            else
            {
                // There are matches, so find the gaps and fill them in
                List<Match> matchSequenceCopy = new List<Match>();
                for (int k = 0; k < matchSequence.Count; k++)
                {
                    Match m1 = matchSequence[k];
                    Match m2 = k < matchSequence.Count - 1 ? matchSequence[k + 1] : new Match { i = password.Length }; // Next match, or a match past the end of the password

                    matchSequenceCopy.Add(m1);
                    if (m1.j < m2.i - 1)
                    {
                        // Fill in gap
                        int ns = m1.j + 1;
                        int ne = m2.i - 1;
                        matchSequenceCopy.Add(new Match
                        {
                            i = ns,
                            j = ne,
                            Token = password.Substring(ns, ne - ns + 1),
                            Cardinality = bruteforce_cardinality,
                            Pattern = BruteforcePattern,
                            Entropy = Math.Log(Math.Pow(bruteforce_cardinality, ne - ns + 1), 2)
                        });
                    }
                }
                matchSequence = matchSequenceCopy;
            }
            
            // Finalize the entropy value
            double minEntropy = password.Length == 0 ? 0 : minimumEntropyToIndex[password.Length - 1];
            double crackTime = PasswordScoring.EntropyToCrackTime(minEntropy);
            stopwatch.Stop();
            long elapsed = stopwatch.ElapsedMilliseconds;
            return new Result(Math.Round(minEntropy, 3), elapsed, Math.Round(crackTime, 3), PasswordScoring.CrackTimeToScore(crackTime), matchSequence, password);
        }

        /// <summary>
        /// Helps the GC to collect the resources used by this instance
        /// </summary>
        public void Dispose() => Factory.Dispose();
    }
}
