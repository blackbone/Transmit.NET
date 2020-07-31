using System.Collections.Generic;

namespace ReliableNetcode.Utils
{
    internal static class ObjPool<T> where T : new()
    {
        private static readonly Queue<T> Pool = new Queue<T>();

        public static T Get()
        {
            lock (Pool)
            {
                if (Pool.Count > 0)
                    return Pool.Dequeue();
            }

            return new T();
        }

        public static void Return(T val)
        {
            lock (Pool)
            {
                Pool.Enqueue(val);
            }
        }
    }
}