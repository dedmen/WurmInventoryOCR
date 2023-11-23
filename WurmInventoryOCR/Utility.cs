using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WurmInventoryOCR
{
    public static class Utility
    {
        public static IEnumerable<T> Merge<T>(this IEnumerable<T> col, Func<T, T, bool> canBeMerged, Func<T, T, T> mergeItems)
        {
            using IEnumerator<T> iter = col.GetEnumerator();
            if (iter.MoveNext())
            {
                T lhs = iter.Current;
                while (iter.MoveNext())
                {
                    T rhs = iter.Current;
                    if (canBeMerged(lhs, rhs))
                        lhs = mergeItems(lhs, rhs);
                    else
                    {
                        yield return lhs;
                        lhs = rhs;
                    }
                }
                yield return lhs;
            }
        }

        public static int CountOccurencesOf(this string str, string subString)
        {
            int count = 0, n = 0;

            if (subString != "")
            {
                while ((n = str.IndexOf(subString, n, StringComparison.InvariantCulture)) != -1)
                {
                    n += subString.Length;
                    ++count;
                }
            }

            return count;
        }

        public static double DistanceTo(this System.Drawing.Point p1, System.Drawing.Point p2)
        {
            return Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)), 1);
        }



    }
}
