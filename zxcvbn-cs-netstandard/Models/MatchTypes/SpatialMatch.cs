namespace Zxcvbn_cs.Models.MatchTypes
{
    /// <summary>
    /// A match made with the <see cref="Matchers.SpatialMatcher"/>. Contains additional information specific to spatial matches.
    /// </summary>
    internal sealed class SpatialMatch : Match
    {
        /// <summary>
        /// The name of the keyboard layout used to make the spatial match
        /// </summary>
        public string Graph { get; set; }

        /// <summary>
        /// The number of turns made (i.e. when diretion of adjacent keys changes)
        /// </summary>
        public int Turns { get; set; }

        /// <summary>
        /// The number of shifted characters matched in the pattern (adds to entropy)
        /// </summary>
        public int ShiftedCount { get; set; }
    }
}
