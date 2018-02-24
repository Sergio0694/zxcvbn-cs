using System.Collections.Generic;
using Zxcvbn_cs.Models.MatchTypes;

namespace Zxcvbn_cs.Models
{
    /// <summary>
    /// The results of zxcvbn's password analysis
    /// </summary>
    public sealed class Result
    {
        public Result(double entropy, long calcTime, double crackTime, int score, IList<Match> matches, string password)
        {
            Entropy = entropy;
            CalcTime = calcTime;
            CrackTime = crackTime;
            Score = score;
            MatchSequence = matches;
            Password = password;
        }

        /// <summary>
        /// A calculated estimate of how many bits of entropy the password covers, rounded to three decimal places.
        /// </summary>
        public double Entropy { get; }

        /// <summary>
        /// The number of milliseconds that zxcvbn took to calculate results for this password
        /// </summary>
        public long CalcTime { get; }

        /// <summary>
        /// An estimation of the crack time for this password in seconds
        /// </summary>
        public double CrackTime { get; }

        /// <summary>
        /// Gets the estimated crack time interval
        /// </summary>
        public CrackTimeInfo CrackTimeInfo => new CrackTimeInfo(CrackTime);

        /// <summary>
        /// A score from 0 to 4 (inclusive), with 0 being least secure and 4 being most secure calculated from crack time:
        /// [0,1,2,3,4] if crack time is less than [10**2, 10**4, 10**6, 10**8, Infinity] seconds.
        /// Useful for implementing a strength meter
        /// </summary>
        public int Score { get; }
        
        /// <summary>
        /// The sequence of matches that were used to create the entropy calculation
        /// </summary>
        public IList<Match> MatchSequence { get; }

        /// <summary>
        /// The password that was used to generate these results
        /// </summary>
        public string Password { get; }
    }
}
