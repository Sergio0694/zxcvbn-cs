namespace Zxcvbn_cs.Models.MatchTypes
{
    /// <summary>
    /// A match found with the RepeatMatcher
    /// </summary>
    internal sealed class RepeatMatch : Match
    {
        /// <summary>
        /// The character that was repeated
        /// </summary>
        public char RepeatChar { get; set; }
    }
}
