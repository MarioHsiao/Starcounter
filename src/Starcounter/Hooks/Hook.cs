using Starcounter.Hooks;
using System;

namespace Starcounter {
    
    /// <summary>
    /// Principal entrypoint to the Starcounter hook API Provides
    /// a set of events allowing hooks to be registered.
    /// </summary>
    /// <typeparam name="T">The database type to hook.</typeparam>
    public static class Hook<T> {

        /// <summary>
        /// Occurs before an object of {T} is being deleted.
        /// </summary>
        public static event EventHandler<T> BeforeDelete {
            add {
                RuntimeDelegate<T>.TriggeredBy(typeof(T), false).BeforeDelete += value;
            }
            remove {
                RuntimeDelegate<T>.TriggeredBy(typeof(T), false).BeforeDelete -= value;
            }
        }

        /// <summary>
        /// Occurs when an object of the {T} is deleted in a
        /// transaction that is being committed.
        /// </summary>
        public static event EventHandler<ulong> CommitDelete {
            add {
                RuntimeDelegate<ulong>.TriggeredBy(typeof(T), false).CommitDelete += value;
            }
            remove {
                RuntimeDelegate<ulong>.TriggeredBy(typeof(T), false).CommitDelete -= value;
            }
        }

        /// <summary>
        /// Occurs when an object of the {T} is inserted in a
        /// transaction that is being committed.
        /// </summary>
        public static event EventHandler<T> CommitInsert {
            add {
                RuntimeDelegate<T>.TriggeredBy(typeof(T), false).CommitInsert += value;
            }
            remove {
                RuntimeDelegate<T>.TriggeredBy(typeof(T), false).CommitInsert -= value;
            }
        }

        /// <summary>
        /// Occurs when an object of the {T} is updated in a
        /// transaction that is being committed.
        /// </summary>
        public static event EventHandler<T> CommitUpdate {
            add {
                RuntimeDelegate<T>.TriggeredBy(typeof(T), false).CommitUpdate += value;
            }
            remove {
                RuntimeDelegate<T>.TriggeredBy(typeof(T), false).CommitUpdate -= value;
            }
        }
    }
}
