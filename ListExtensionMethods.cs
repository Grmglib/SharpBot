using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBot
{
    public static class ListExtensionMethods
    {
        public static T PopAt<T>(this List<T> list, int index)
        {
            var r = list[index];
            list.RemoveAt(index);
            return r;
        }

        public static T PopFirst<T>(this List<T> list)
        {
            var r = list.FirstOrDefault();
            list.RemoveAt(0);
            return r;
        }

        public static T PopFirstOrDefault<T>(this List<T> list, Predicate<T> predicate) where T : class
        {
            var index = list.FindIndex(predicate);
            if (index > -1)
            {
                var r = list[index];
                list.RemoveAt(index);
                return r;
            }
            return null;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }
}
