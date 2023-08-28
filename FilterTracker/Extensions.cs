using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FilterTracker
{
	public static class Extensions
	{
        public static double StdDev<T>(this IEnumerable<T> list, Func<T, double> values)
        {
            // ref: https://stackoverflow.com/questions/2253874/linq-equivalent-for-standard-deviation
            // ref: http://warrenseen.com/blog/2006/03/13/how-to-calculate-standard-deviation/ 
            var mean = 0.0;
            var sum = 0.0;
            var stdDev = 0.0;
            var n = 0;
            foreach (var value in list.Select(values))
            {
                n++;
                var delta = value - mean;
                mean += delta / n;
                sum += delta * (value - mean);
            }
            if (1 < n)
                stdDev = Math.Sqrt(sum / (n - 1));

            return stdDev;

        }

        public static bool IsNumeric(this string target)
        {
            if (string.IsNullOrEmpty(target))
                return false;

            foreach (char c in target.ToCharArray())
            {
                if (!char.IsNumber(c))
                    return false;
            }

            return true;
        }

        public static string TrimAndLower(this string target)
        {
            return target.Trim().ToLower();
        }

        public static string TruncateWithElipsis(this string target, int length)
        {
            if (string.IsNullOrEmpty(target))
                return target;

            if (target.Length > length + 3)
            {
                return target.Substring(0, length) + "...";
            }

            return target;
        }

        public static string RemoveLeadingCharacters(this string haystack, char needle)
        {
            if (string.IsNullOrEmpty(haystack))
                return haystack;

            int i = 0;
            while (haystack[i] == needle)
                i++;

            if (i > 0)
                return haystack.Substring(i);

            return haystack;
        }
    }
}