
using Starcounter.Internal;

namespace Starcounter {
    /// <summary>
    /// Pairs a HookDelegateList<T> with an index, and support invoking
    /// the delegate method being adressed within a specified application
    /// context.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class RuntimeInstalledHookDelegate<T> : InvokableHook {
        /// <summary>
        /// The list of delegates the current entry reference.
        /// </summary>
        public HookDelegateList<T> Delegates { get; set; }

        /// <summary>
        /// The particular hook in the list of deleages the current
        /// entry reference.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The application name that defines in what scope the
        /// current delegate should execute when triggered.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Invokes the referenced delegate with the given argument.
        /// </summary>
        /// <param name="triggeringObject">Carry to be passed to the
        /// delegate.</param>
        public override void Invoke(object triggeringObject) {
            var d = Delegates.Get(Index);
            using (new AppScope(ApplicationName)) {
                d.Invoke(null, (T)triggeringObject);
            }
        }
    }
}
