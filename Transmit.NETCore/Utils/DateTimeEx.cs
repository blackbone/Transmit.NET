using System;

namespace Transmit.Utils
{
    internal static class DateTimeEx
    {
        public static double GetTotalSeconds(this DateTime time)
        {
            return time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        ///     Gets the Unix timestamp of the DateTime object
        /// </summary>
        public static ulong ToUnixTimestamp(this DateTime time)
        {
            return (ulong) Math.Truncate(time.GetTotalSeconds());
        }
    }
}