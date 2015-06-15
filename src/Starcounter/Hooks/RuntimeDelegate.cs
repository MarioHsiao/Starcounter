using Starcounter.Internal;
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
        static Dictionary<string, HookDelegateList<T>> hooksPerSignature = new Dictionary<string, HookDelegateList<T>>();
        readonly ushort triggeringTable;
        readonly bool inherited;

        private RuntimeDelegate(ushort trigger, bool inherit) {
            triggeringTable = trigger;
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
            // Optimize, assuring we dont repeatadly create instances,
            // and improve error information (i.e. add code, message).
            // TODO:

            var binding = Binding.Bindings.GetTypeBinding(typeName);
            if (binding == null) {
                throw new ArgumentException();
            }

            if (inherited) {
                throw new NotSupportedException();
            }

            return new RuntimeDelegate<T>(binding.TableId, inherited);
        }

        /// <summary>
        /// Occurs before an object of the triggering type is being deleted.
        /// </summary>
        public event EventHandler<T> BeforeDelete {
            add {
                VerifyHandlerMatchTriggeringType();
                // Not a commit hook, but rather a code host hook. We need
                // to support this.
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
        public event EventHandler<ulong> CommitDelete {
            add {
                // We need a special treatment of these, since the type is
                // force to be ULONG.
                // TODO:
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
                VerifyHandlerMatchTriggeringType();
                lock (HookLock.Sync) {
                    InstallCommitHook(triggeringTable, CommitHookConfiguration.Insert, AddDelegate(value));
                }
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
                VerifyHandlerMatchTriggeringType();
                lock (HookLock.Sync) {
                    InstallCommitHook(triggeringTable, CommitHookConfiguration.Update, AddDelegate(value));
                }
            }
            remove {
                throw new NotImplementedException();
            }
        }

        void VerifyHandlerMatchTriggeringType() {
            var type = Binding.Bindings.GetTypeBinding(triggeringTable).TypeDef.TypeLoader.Load();
            if (!typeof(T).IsAssignableFrom(type)) {
                // Better error information
                // TODO:
                throw new ArgumentException();
            }
        }

        static void InstallCommitHook(ushort tableId, uint hookConfiguration, InvokableHook entry) {
            List<InvokableHook> installed;

            var hookType = CommitHookConfiguration.ToHookType(hookConfiguration);
            var key = HookKey.FromTable(tableId, hookType);
            if (!InvokableHook.HooksPerTrigger.TryGetValue(key, out installed)) {
                var hookConfigMask = CommitHookConfiguration.CalculateEffectiveConfiguration(key);
                hookConfigMask |= hookConfiguration;

                var result = sccoredb.star_set_commit_hooks(0, tableId, hookConfigMask);
                if (result != 0) {
                    throw ErrorCode.ToException(result);
                }

                installed = new List<InvokableHook>();
                InvokableHook.HooksPerTrigger[key] = installed;
            }

            installed.Add(entry);
        }

        static RuntimeInstalledHookDelegate<T> AddDelegate(EventHandler<T> callback) {
            HookDelegateList<T> delegates;
            if (!hooksPerSignature.TryGetValue(typeof(T).FullName, out delegates)) {
                delegates = new HookDelegateList<T>();
                hooksPerSignature[typeof(T).FullName] = delegates;
            }

            var index = delegates.Add2(callback);
            return new RuntimeInstalledHookDelegate<T>() {
                Delegates = delegates,
                Index = index,
                ApplicationName = StarcounterEnvironment.AppName
            };
        }
    }
}
