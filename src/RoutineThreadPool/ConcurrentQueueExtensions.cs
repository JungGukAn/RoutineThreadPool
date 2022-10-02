using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Jung.Utils
{
    public static class ConcurrentQueueExtensions
    {
        public static void Enqueue<T>(this ConcurrentQueue<T> queue, IEnumerable<T> items)
        {
            foreach(var item in items)
            {
                queue.Enqueue(item);
            }
        }

        public static bool TryDequeue<T>(this ConcurrentQueue<T> queue, int dequeueCount, out List<T>? items)
        {
            items = default;

            int tryCount = Math.Min(dequeueCount, queue.Count);
            if (tryCount <= 0)
            {
                return false;
            }

            for (int i = 0; i < tryCount; i++)
            {
                if (queue.TryDequeue(out var item) == false)
                {
                    break;
                }

                if (items == null)
                {
                    items = new List<T>();
                }

                items.Add(item);
            }

            return items != null;
        }
    }
}
