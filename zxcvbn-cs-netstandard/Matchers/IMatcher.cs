using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Zxcvbn_cs.Models.MatchTypes;

namespace Zxcvbn_cs.Matchers
{
    /// <summary>
    /// All pattern matchers must implement the IMatcher interface.
    /// </summary>
    public interface IMatcher
    {
        /// <summary>
        /// This function is called once for each matcher for each password being evaluated. It should perform the matching process and return
        /// an enumerable of Match objects for each match found.
        /// </summary>
        /// <param name="password">The input password</param>
        /// <param name="token">The token for the operation</param>
        IEnumerable<Match> MatchPassword([NotNull] string password, CancellationToken token);
    }

    /// <summary>
    /// An interface for matchers that can manually free some of their resource
    /// </summary>
    public interface IDisposableMatcher : IMatcher, IDisposable { }
}
