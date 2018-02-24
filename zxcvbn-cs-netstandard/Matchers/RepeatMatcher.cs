using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Zxcvbn_cs.Helpers;
using Zxcvbn_cs.Models.MatchTypes;

namespace Zxcvbn_cs.Matchers
{
    /// <summary>
    /// Match repeated characters in the password (repeats must be more than two characters long to count)
    /// </summary>
    public sealed class RepeatMatcher : IMatcher
    {
        const string RepeatPattern = "repeat";

        /// <summary>
        /// Find repeat matches in <paramref name="password"/>
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <param name="token"></param>
        /// <returns>List of repeat matches</returns>
        /// <seealso cref="RepeatMatch"/>
        public IEnumerable<Match> MatchPassword(string password, CancellationToken token)
        {
            // Be sure to not count groups of one or two characters
            token.ThrowIfCancellationRequested();
            return password.GroupAdjacent(c => c).Where(g => g.Count() > 2).Select(g => new RepeatMatch {
                Pattern = RepeatPattern,
                Token = password.Substring(g.StartIndex, g.EndIndex - g.StartIndex + 1),
                i = g.StartIndex,
                j = g.EndIndex,
                Entropy = CalculateEntropy(password.Substring(g.StartIndex, g.EndIndex - g.StartIndex + 1)),
                RepeatChar = g.Key
            });
        }

        private double CalculateEntropy(string match)
        {
            return Math.Log(PasswordScoring.PasswordCardinality(match) * match.Length, 2);
        }
    }
}
