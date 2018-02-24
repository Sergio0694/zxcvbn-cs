using System;
using System.Linq;

namespace Zxcvbn_cs.Helpers
{
    /// <summary>
    /// A few useful extension methods used through the Zxcvbn project
    /// </summary>
    internal static class Utility
    {
        /// <summary>
        /// Reverse a string in one call
        /// </summary>
        /// <param name="str">String to reverse</param>
        /// <returns>String in reverse</returns>
        public static string StringReverse(this string str)
        {
            return new String(str.Reverse().ToArray());
        }

        /// <summary>
        /// A convenience for parsing a substring as an int and returning the results. Uses TryParse, and so returns zero where there is no valid int
        /// </summary>
        /// <param name="r">Substring parsed as int or zero</param>
        /// <param name="length">Length of substring to parse</param>
        /// <param name="startIndex">Start index of substring to parse</param>
        /// <param name="str">String to get substring of</param>
        /// <returns>True if the parse succeeds</returns>
        public static bool IntParseSubstring(this string str, int startIndex, int length, out int r)
        {
            return int.TryParse(str.Substring(startIndex, length), out r);
        }

        /// <summary>
        /// Quickly convert a string to an integer, uses TryParse so any non-integers will return zero
        /// </summary>
        /// <param name="str">String to parse into an int</param>
        /// <returns>Parsed int or zero</returns>
        public static int ToInt(this string str)
        {
            int.TryParse(str, out int r);
            return r;
        }
    }
}
