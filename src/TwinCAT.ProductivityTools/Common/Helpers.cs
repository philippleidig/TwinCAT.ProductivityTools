using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwinCAT.Remote.Helpers
{
    public static class ArrayHelpers
    {
        public static string ByteArrayToString(byte[] value)
        {
            if (value == null)
                return "";
            else
                return string.Concat(value.Select(b => b <= 0x7f ? (char)b : '?').TakeWhile(b => b > 0)) ?? "";
        }

    }

    public static class Enum<T>
    {
        public static bool IsDefined(string name)
        {
            return Enum.IsDefined(typeof(T), name);
        }

        public static bool IsDefined(T value)
        {
            return Enum.IsDefined(typeof(T), value);
        }

        public static IEnumerable<T> GetValues()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}
