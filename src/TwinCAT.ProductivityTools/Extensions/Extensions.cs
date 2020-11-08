using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class ArrayExtension
    {
        public static T[] CopySlice<T>(this T[] source, int index, int length, bool padToLength = false)
        {
            int n = length;
            T[] slice = null;

            if (source.Length < index + length)
            {
                n = source.Length - index;
                if (padToLength)
                {
                    slice = new T[length];
                }
            }

            if (slice == null) slice = new T[n];
            Array.Copy(source, index, slice, 0, n);
            return slice;
        }

        public static IEnumerable<T[]> Slices<T>(this T[] source, int count, bool padToLength = false)
        {
            for (var i = 0; i < source.Length; i += count)
                yield return source.CopySlice(i, count, padToLength);
        }
    }
    public static class ListExtension
    {
        public static async Task ForEachAsync<T>(this List<T> list, Func<T, Task> func)
        {
            foreach (var value in list)
            {
                await func(value);
            }
        }

        public static void AddIfNotExists<T>(this List<T> list, T value)
        {
            CheckListIsNull(list);
            if (!list.Contains(value))
                list.Add(value);
        }

        public static void UpdateValue<T>(this IList<T> list, T value, T newValue)
        {
            CheckListAndValueIsNull(list, value);
            CheckValueIsNull(newValue);
            var index = list.IndexOf(value);
            list[index] = newValue;
        }

        public static void DeleteIfExists<T>(this IList<T> list, T value)
        {
            CheckListAndValueIsNull(list, value);
            if (list.Contains(value))
                list.Remove(value);
        }

        public static bool AreValuesEmpty<T>(this IList<T> list)
        {
            CheckListIsNull(list);
            return list.All(x => x == null);
        }

        private static void CheckListAndValueIsNull<T>(this IList<T> list, T value)
        {
            CheckListIsNull(list);
            CheckValueIsNull(value);
        }

        private static void CheckValueIsNull<T>(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
        }

        private static void CheckListIsNull<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
        }
    }
}
