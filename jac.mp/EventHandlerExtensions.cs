using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace jac.mp
{
    public static class EventHandlerExtensions
    {
        public static void Raise<T>(this EventHandler<T> theEvent, object source, T args)
        {
            // this is slightly less efficient than ordinary local copy because
            // of the method overhead, and you have to pass in an event args
            // regardless of whether a subscriber is attached or not.
            if (theEvent != null)
            {
                theEvent(source, args);
            }
        }

    }

    public static class ConcurrentQueueExtensions
    {
        public static void EnqueueAll<T>(this ConcurrentQueue<T> queue, IEnumerable<T> items)
        {
            foreach (var i in items)
                queue.Enqueue(i);
        }
    }
}
