using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods for <see cref="DateTime"/> operations including validation,
    /// date range helpers, and human-readable formatting.
    /// </summary>
    public static class DateTimeExtension
    {
        /// <summary>
        /// The minimum valid date (January 1, 1900) used by <see cref="IsValid"/>.
        /// </summary>
        private static readonly DateTime MinDate = new DateTime(1900, 1, 1);

        /// <summary>
        /// The maximum valid date (December 31, 9999 23:59:59.999) used by <see cref="IsValid"/>.
        /// </summary>
        private static readonly DateTime MaxDate = new DateTime(9999, 12, 31, 23, 59, 59, 999);

        /// <summary>
        /// Determines whether the <see cref="DateTime"/> falls within the valid range (1900-01-01 to 9999-12-31).
        /// </summary>
        /// <param name="target">The <see cref="DateTime"/> to validate.</param>
        /// <returns><c>true</c> if the date is within the valid range; otherwise, <c>false</c>.</returns>
        [DebuggerStepThrough]
        public static bool IsValid(this DateTime target)
        {
            return (target >= MinDate) && (target <= MaxDate);
        }

        /// <summary>
        /// Returns a <see cref="DateTime"/> representing the first day of the month for the given date.
        /// </summary>
        /// <param name="dt">The source date.</param>
        /// <returns>A <see cref="DateTime"/> set to the first day of the month.</returns>
        public static DateTime FirstDayOfMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        /// <summary>
        /// Returns a <see cref="DateTime"/> representing the last day of the month for the given date.
        /// </summary>
        /// <param name="dt">The source date.</param>
        /// <returns>A <see cref="DateTime"/> set to the last day of the month.</returns>
        public static DateTime LastDayOfMonth(this DateTime dt)
        {

            return new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to a human-readable relative time string (e.g., "5 minutes ago").
        /// For future dates, delegates to <see cref="HumanizeDate"/>.
        /// </summary>
        /// <param name="target">The <see cref="DateTime"/> to humanize.</param>
        /// <returns>A human-readable string representing the relative time from now.</returns>
        public static string Humanize(this DateTime target)
        {

            TimeSpan _timeSpan = DateTime.Now.Subtract(target);
            StringBuilder sb = new StringBuilder();
            if (_timeSpan.TotalSeconds < 0)
                sb.AppendFormat(HumanizeDate(target));
            else if (_timeSpan.TotalSeconds == 0)
                return "just now";
            else if (_timeSpan.TotalSeconds <= 60 && _timeSpan.TotalSeconds >= 1)
                sb.AppendFormat(" {0} {1} ago", _timeSpan.TotalSeconds.ToString("0"), "second".Pluralize());//(int)_timeSpan.TotalSeconds));
            else if (_timeSpan.TotalSeconds > 60 && _timeSpan.TotalMinutes <= 60)
                sb.AppendFormat(" {0} {1} ago", _timeSpan.TotalMinutes.ToString("0"), "minute".Pluralize());// (int)_timeSpan.TotalMinutes));
            else if (_timeSpan.TotalMinutes > 60 && _timeSpan.TotalHours <= 24)
                sb.AppendFormat(" {0} {1} ago", _timeSpan.TotalHours.ToString("0"), "hour".Pluralize());// (int)_timeSpan.TotalHours));
            else if (_timeSpan.TotalHours > 24 && _timeSpan.TotalDays < 7)
                sb.AppendFormat(" {0} {1} ago", _timeSpan.TotalDays.ToString("0"), "day".Pluralize());// (int)_timeSpan.TotalDays));
            else if (_timeSpan.TotalDays > 7 && _timeSpan.TotalDays <= 30)
                sb.AppendFormat(" {0} {1} ago", Math.Ceiling(_timeSpan.TotalDays / 7), "week".Pluralize());// (int)Math.Ceiling(_timeSpan.TotalDays / 7)));
            else if (_timeSpan.TotalDays > 30 && _timeSpan.TotalDays <= 365)
                sb.AppendFormat(" {0} {1} ago", Math.Floor(_timeSpan.TotalDays / 30).ToString("0"), "month".Pluralize());// (int)Math.Floor(_timeSpan.TotalDays / 30)));
            else if (_timeSpan.TotalDays > 365)
                sb.AppendFormat(" {0} {1} ago", Math.Ceiling(_timeSpan.TotalDays / 365).ToString("0"), "year".Pluralize());// (int)Math.Ceiling(_timeSpan.TotalDays / 365)));

            return sb.ToString();
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to a human-readable relative date group string
        /// (e.g., "Today", "Yesterday", "Next week", "Last month").
        /// </summary>
        /// <param name="date">The <see cref="DateTime"/> to humanize.</param>
        /// <returns>A descriptive string indicating the relative date grouping.</returns>
        public static string HumanizeDate(this DateTime date)
        {
            DateTime dateNow = DateTime.Now;

            // Determine the start of the date's week
            DateTime startOfWeek = (date.Date - TimeSpan.FromDays((double)date.DayOfWeek));

            // We'll use this variable for monthly comparison
            int intMonthCompare = ((date.Year * 12 + date.Month) - (dateNow.Year * 12 + dateNow.Month));

            // Do the monthly comparison first, as that's the biggest possible way to group
            if (intMonthCompare > 1)
            {
                return "Beyond next month";
            }

            if (intMonthCompare < -1)
            {
                return "Older";
            }

            if (intMonthCompare == 1)
            {
                return "Next month";
            }

            if (intMonthCompare == -1)
            {
                return "Last month";
            }

            // Now do the same, but in weeks
            TimeSpan ts = (startOfWeek - dateNow.Date);
            if (ts.Days > 28)
            {
                return "Five weeks from now";
            }

            if (ts.Days > 21)
            {
                return "Four weeks from now";
            }

            if (ts.Days > 14)
            {
                return "Three weeks from now";
            }

            if (ts.Days > 7)
            {
                return "Two weeks from now";
            }

            if (ts.Days > 0)
            {
                return "Next week";
            }

            if (ts.Days < -35)
            {
                return "Five weeks ago";
            }

            if (ts.Days < -28)
            {
                return "Four weeks ago";
            }

            if (ts.Days < -21)
            {
                return "Three weeks ago";
            }

            if (ts.Days < -14)
            {
                return "Two weeks ago";
            }

            if (ts.Days < -7)
            {
                return "Last week";
            }

            // Nothing found so far. Let's see if it's tomorrow, today, or yesterday
            ts = date.Date - dateNow.Date;

            if (ts.Days == 1)
            {
                return "Tomorrow";
            }

            if (ts.Days == 0)
            {
                return "Today";
            }

            if (ts.Days == -1)
            {
                return "Yesterday";
            }

            // Still nothing? Must be a different day in this week then
            return date.DayOfWeek.ToString();
        }
    }
}
