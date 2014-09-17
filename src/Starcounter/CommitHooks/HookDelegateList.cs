using System;
using System.Collections.Generic;

namespace Starcounter.CommitHooks {
    /// <summary>
    /// List of delegates with the same signature type.
    /// </summary>
    /// <typeparam name="T">Type of target.</typeparam>
    internal class HookDelegateList<T> {
        List<Action<T>> targets = new List<Action<T>>();

        /// <summary>
        /// Adds a new callback with the given target.
        /// </summary>
        /// <param name="callback">Delegate method.</param>
        /// <returns>The index of the added callback.</returns>
        public int Add(Action<T> callback) {
            var index = targets.IndexOf(callback);
            if (index == -1) {
                targets.Add(callback);
                index = targets.Count - 1;
            }
            return index;
        }

        /// <summary>
        /// Gets a target callback by index.
        /// </summary>
        /// <param name="index">The index of the callback to
        /// retreive</param>
        /// <returns>The callback with the given index.</returns>
        public Action<T> Get(int index) {
            return targets[index];
        }
    }
}
