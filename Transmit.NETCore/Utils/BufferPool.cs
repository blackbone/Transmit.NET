using System;
using System.Collections.Generic;

namespace Transmit.Utils
{
    /// <summary>
    ///     Helper methods for allocating temporary buffers
    /// </summary>
    internal static class BufferPool
    {
        private static readonly Dictionary<int, Queue<byte[]>> Pool = new Dictionary<int, Queue<byte[]>>();

        /// <summary>
        ///     Retrieve a buffer of the given size
        /// </summary>
        public static byte[] GetBuffer(int size)
        {
            lock (Pool)
            {
                if (Pool.ContainsKey(size))
                    if (Pool[size].Count > 0)
                        return Pool[size].Dequeue();
            }

            return new byte[size];
        }

        /// <summary>
        ///     Return a buffer to the pool
        /// </summary>
        public static void ReturnBuffer(byte[] buffer)
        {
            lock (Pool)
            {
                if (!Pool.ContainsKey(buffer.Length))
                    Pool.Add(buffer.Length, new Queue<byte[]>());

                Array.Clear(buffer, 0, buffer.Length);
                Pool[buffer.Length].Enqueue(buffer);
            }
        }
    }
}