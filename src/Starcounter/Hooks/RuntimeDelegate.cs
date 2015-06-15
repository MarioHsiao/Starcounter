using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Hooks {
    /// <summary>
    /// Encapsulates a delegate and a named database type and expose
    /// a set of events that can be manipulated to subscribe/unsubscribe
    /// to certain database triggers, fired when the triggering type is
    /// being mutated.
    /// </summary>
    /// <typeparam name="T">The host type used in the delegate signature.
    /// Must align with the triggering type.</typeparam>
    public sealed class RuntimeDelegate<T> {
        readonly string triggeringType;
        readonly bool inherited;

        private RuntimeDelegate(string trigger, bool inherit) {
            triggeringType = trigger;
            inherited = inherit;
        }

        /// <summary>
        /// Creates a <see cref="RuntimeDelegate<T>"/> triggered by
        /// <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The triggering database type.</param>
        /// <param name="inherited">Indicates if future subscriptions
        /// should be made to the triggering type only, or to all its
        /// derivatives too.</param>
        /// <returns>A <see cref="RuntimeDelegate<T>"/> that can be
        /// used to subscribe/unsubscribe to relevant database events.</returns>
        public static RuntimeDelegate<T> TriggeredBy(Type type, bool inherited = false) {
            return TriggeredBy(type.FullName, inherited);
        }

        /// <summary>
        /// Creates a <see cref="RuntimeDelegate<T>"/> triggered by
        /// <paramref name="typeName"/>.
        /// </summary>
        /// <param name="typeName">Name of triggering database type.</param>
        /// <param name="inherited">Indicates if future subscriptions
        /// should be made to the triggering type only, or to all its
        /// derivatives too.</param>
        /// <returns>A <see cref="RuntimeDelegate<T>"/> that can be
        /// used to subscribe/unsubscribe to relevant database events.</returns>
        public static RuntimeDelegate<T> TriggeredBy(string typeName, bool inherited = false) {
            // Check there is a database class with name.
            // Check its assignable from T
            // Check inherited = false
            // TODO:
            // Then instantiate it (locked).
            var binding = Binding.Bindings.GetTypeBinding(typeName);
            if (binding == null) {
                throw new ArgumentException();
            }

            var type = binding.TypeDef.TypeLoader.Load();
            if (!typeof(T).IsAssignableFrom(type)) {
                throw new ArgumentException();
            }

            if (inherited) {
                throw new NotSupportedException();
            }

            return new RuntimeDelegate<T>(typeName, inherited);
        }

        /// <summary>
        /// Occurs before an object of the triggering type is being deleted.
        /// </summary>
        public event EventHandler<T> BeforeDelete {
            add {
                throw new NotImplementedException();
            }
            remove {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Occurs when an object of the triggering type is deleted in a
        /// transaction that is being committed.
        /// </summary>
        public event EventHandler<T> CommitDelete {
            add {
                throw new NotImplementedException();
            }
            remove {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Occurs when an object of the triggering type is inserted in a
        /// transaction that is being committed.
        /// </summary>
        public event EventHandler<T> CommitInsert {
            add {
                throw new NotImplementedException();
            }
            remove {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Occurs when an object of the triggering type is updated in a
        /// transaction that is being committed.
        /// </summary>
        public event EventHandler<T> CommitUpdate {
            add {
                throw new NotImplementedException();
            }
            remove {
                throw new NotImplementedException();
            }
        }
    }
}
