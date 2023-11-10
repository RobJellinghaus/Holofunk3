using System.Linq;

namespace Holofunk.Core
{
    public static class ArrayExtensions
    {
        public static string ArrayToString<T>(this T[] array)
        {
            return $"[{string.Join(", ", array.Select(t => t.ToString()))}]";
        }
    }
}
