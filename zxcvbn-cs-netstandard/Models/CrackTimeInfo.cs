using System;

namespace Zxcvbn_cs.Models
{
    /// <summary>
    /// Indicates the estimated crack time for a password
    /// </summary>
    public struct CrackTimeInfo
    {
        /// <summary>
        /// Gets whether or not the password is impossible to crack
        /// </summary>
        public bool IsInfinite { get; }

        /// <summary>
        /// Gets whether or not the password can be cracked in under a minute
        /// </summary>
        public bool IsInstant { get; }

        /// <summary>
        /// Gets the estimated minutes necessary to crack the password
        /// </summary>
        public int Minutes { get; }

        /// <summary>
        /// Gets the estimated hours necessary to crack the password
        /// </summary>
        public int Hours { get; }

        /// <summary>
        /// Gets the estimated days necessary to crack the password
        /// </summary>
        public int Days { get; }

        /// <summary>
        /// Gets the estimated months necessary to crack the password
        /// </summary>
        public int Months { get; }

        /// <summary>
        /// Gets the estimated years necessary to crack the password
        /// </summary>
        public int Years { get; }

        /// <summary>
        /// Gets the estimated centuries necessary to crack the password
        /// </summary>
        public int Centuries { get; }

        /// <summary>
        /// Gets the estimated millenniums necessary to crack the password
        /// </summary>
        public int Millenniums { get; }

        /// <summary>
        /// Gets the estimated million years necessary to crack the password
        /// </summary>
        public int MillionYears { get; }

        internal CrackTimeInfo(double seconds) : this()
        {
            // Default durations
            long
                minute = 60,
                hour = minute * 60,
                day = hour * 24,
                month = day * 31,
                year = month * 12,
                century = year * 100,
                thousand = century * 10,
                million = thousand * 1000,
                billion = million * 1000;

            // Calculate the current interval
            if (seconds < minute) IsInstant = true;
            else if (seconds < hour) Minutes = (int)(1 + Math.Ceiling(seconds / minute));
            else if (seconds < day) Hours = (int)(1 + Math.Ceiling(seconds / hour));
            else if (seconds < month) Days = (int)(1 + Math.Ceiling(seconds / day));
            else if (seconds < year) Months = (int)(1 + Math.Ceiling(seconds / month));
            else if (seconds < century) Years = (int)(1 + Math.Ceiling(seconds / year));
            else if (seconds < thousand) Centuries = (int)(1 + Math.Ceiling(seconds / century));
            else if (seconds < million) Millenniums = (int)(1 + Math.Ceiling(seconds / thousand));
            else if (seconds >= million && seconds < billion) MillionYears = (int)(1 + Math.Ceiling(seconds / million));
            else IsInfinite = true;
        }
    }
}
