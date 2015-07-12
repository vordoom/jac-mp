using System;

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
}
