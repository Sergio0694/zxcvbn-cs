using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Zxcvbn_cs.Helpers;
using Zxcvbn_cs.Models.MatchTypes;

namespace Zxcvbn_cs.Matchers
{
    /// <summary>
    /// This matcher detects lexicographical sequences (and in reverse) e.g. abcd, 4567, PONML etc.
    /// </summary>
    public sealed class SequenceMatcher : IMatcher
    {
        // Sequences should not overlap, sequences here must be ascending, their reverses will be checked automatically
        readonly string[] Sequences = { 
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            "01234567890"
        };

        readonly string[] SequenceNames = {
            "lower",
            "upper",
            "digits"
        };

        const string SequencePattern = "sequence";

        /// <summary>
        /// Find matching sequences in <paramref name="password"/>
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <param name="token"></param>
        /// <returns>Enumerable of sqeunec matches</returns>
        /// <seealso cref="SequenceMatch"/>
        public IEnumerable<Match> MatchPassword(string password, CancellationToken token)
        {
            // Sequences to check should be the set of sequences and their reverses (i.e. want to match "abcd" and "dcba")
            List<string> seqs = Sequences.Union(Sequences.Select(s => s.StringReverse())).ToList();

            List<Match> matches = new List<Match>();

            int i = 0;
            while (i < password.Length - 1)
            {
                token.ThrowIfCancellationRequested();
                int j = i + 1;

                // Find a sequence that the current and next characters could be part of 
                string seq =
                    (from s in seqs
                    let ixI = s.IndexOf(password[i])
                    let ixJ = s.IndexOf(password[j])
                    where ixJ == ixI + 1 // i.e. two consecutive letters in password are consecutive in sequence
                    select s).FirstOrDefault();

                // This isn't an ideal check, but we want to know whether the sequence is ascending/descending to keep entropy
                //   calculation consistent with zxcvbn
                bool ascending = Sequences.Contains(seq);

                // seq will be null when there are no matching sequences
                if (seq != null)
                {
                    int startIndex = seq.IndexOf(password[i]);

                    // Find length of matching sequence (j should be the character after the end of the matching subsequence)
                    for (; j < password.Length && startIndex + j - i < seq.Length && seq[startIndex + j - i] == password[j]; j++) { }

                    int length = j - i;
                    
                    // Only want to consider sequences that are longer than two characters
                    if (length > 2)
                    {
                        // Find the sequence index so we can match it up with its name
                        int seqIndex = seqs.IndexOf(seq);
                        if (seqIndex >= Sequences.Length) seqIndex -= Sequences.Length; // match reversed sequence with its original

                        string match = password.Substring(i, j - i);
                        matches.Add(new SequenceMatch {
                            i = i,
                            j = j - 1,
                            Token = match,
                            Pattern = SequencePattern,
                            Entropy = CalculateEntropy(match, ascending),
                            Ascending = ascending,
                            SequenceName = SequenceNames[seqIndex],
                            SequenceSize = Sequences[seqIndex].Length
                        });
                    }
                }

                i = j;
            }

            return matches;
        }

        private double CalculateEntropy(string match, bool ascending)
        {
            char firstChar = match[0];

            // XXX: This entropy calculation is hard coded, ideally this would (somehow) be derived from the sequences above
            double baseEntropy;
            if (firstChar == 'a' || firstChar == '1') baseEntropy = 1;
            else if ('0' <= firstChar && firstChar <= '9') baseEntropy = Math.Log(10, 2); // Numbers
            else if ('a' <= firstChar && firstChar <= 'z') baseEntropy = Math.Log(26, 2); // Lowercase
            else baseEntropy = Math.Log(26, 1) + 1; // + 1 for uppercase

            if (!ascending) baseEntropy += 1; // Descending instead of ascending give + 1 bit of entropy

            return baseEntropy + Math.Log(match.Length, 2);
        }
    }
}
