using System;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// List of delegates with the same signature type.
    /// </summary>
    /// <typeparam name="T">Type of target.</typeparam>
    internal class HookDelegateList<T> {
        List<EventHandler<T>> handlers = new List<EventHandler<T>>();

        /// <summary>
        /// Adds a new handler with the given target.
        /// </summary>
        /// <param name="h">The handler to add.</param>
        /// <returns>The index of the added handler.</returns>
        public int Add(EventHandler<T> h) {
            handlers.Add(h);
            return handlers.Count - 1;
        }

        /// <summary>
        /// Gets a handler/delegate by index.
        /// </summary>
        /// <param name="index">The index of the handler to
        /// retreive</param>
        /// <returns>The handler with the given index.</returns>
        public EventHandler<T> Get(int index) {
            return handlers[index];
        }
    }
}
