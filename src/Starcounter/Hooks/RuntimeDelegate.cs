
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Hooks {
    internal static class HookLock {
        public static readonly object Sync = new object();
    }

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
            // Optimize, assuring we dont repeatadly create instances.
            // TODO:

            var typeDef = Binding.Bindings.GetTypeDef(typeName);
            if (typeDef == null) {
                throw ErrorCode.ToException(Error.SCERRTABLENOTFOUND, string.Format("Table: {0}", typeName));
            }

            if (inherited) {
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Inherited hooks are not yet supported");
            }

            return new RuntimeDelegate<T>(typeDef.TableDef.TableId, inherited);
        }

        /// <summary>
        /// Occurs before an object of the triggering type is being deleted.
        /// </summary>
        public event EventHandler<T> BeforeDelete {
            add {
                VerifyHandlerMatchTriggeringType();
                InstallBeforeDeleteHook(triggeringTable, AddDelegate(value));
            }
            remove {
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Detaching hooks is not yet supported");
            }
        }

        /// <summary>
        /// Occurs when an object of the triggering type is deleted in a
        /// transaction that is being committed.
        /// </summary>
        public event EventHandler<T> CommitDelete {
            add {
                if (!(this is RuntimeDelegate<ulong>)) {
                    throw new Exception();
                }
                lock (HookLock.Sync) {
                    InstallCommitHook(triggeringTable, CommitHookConfiguration.Delete, AddDelegate(value));
                }
            }
            remove {
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Detaching hooks is not yet supported");
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
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Detaching hooks is not yet supported");
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
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Detaching hooks is not yet supported");
            }
        }

        void VerifyHandlerMatchTriggeringType() {
            var type = Binding.Bindings.GetTypeBinding(triggeringTable).TypeDef.TypeLoader.Load();
            if (!typeof(T).IsAssignableFrom(type)) {
                var error = string.Format("Types {0} and {1} are not compatible", typeof(T).FullName, type.FullName);
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, error);
            }
        }

        static void InstallCommitHook(ushort tableId, uint hookConfiguration, InvokableHook entry) {
            List<InvokableHook> installed;

            var hookType = CommitHookConfiguration.ToHookType(hookConfiguration);
            var key = HookKey.FromTable(tableId, hookType);
            if (!InvokableHook.HooksPerTrigger.TryGetValue(key, out installed)) {
                var hookConfigMask = CommitHookConfiguration.CalculateEffectiveConfiguration(key);
                hookConfigMask |= hookConfiguration;
                installed = new List<InvokableHook>();
                InvokableHook.HooksPerTrigger[key] = installed;
            }

            installed.Add(entry);
        }

        static void InstallBeforeDeleteHook(ushort tableId, InvokableHook entry) {
            List<InvokableHook> installed;

            var key = HookKey.FromTable(tableId, HookType.BeforeDelete);
            if (!InvokableHook.HooksPerTrigger.TryGetValue(key, out installed)) {
                installed = new List<InvokableHook>();
                InvokableHook.HooksPerTrigger[key] = installed;
            }

            // Optimization. Should probably be reviewed and considered
            // further, preferably when we implement detaching of hooks.

            var binding = Bindings.GetTypeBinding(tableId);
            binding.Flags |= TypeBindingFlags.Hook_OnDelete;

            installed.Add(entry);
        }

        static RuntimeInstalledHookDelegate<T> AddDelegate(EventHandler<T> callback) {
            HookDelegateList<T> delegates;
            if (!hooksPerSignature.TryGetValue(typeof(T).FullName, out delegates)) {
                delegates = new HookDelegateList<T>();
                hooksPerSignature[typeof(T).FullName] = delegates;
            }

            var index = delegates.Add(callback);
            return new RuntimeInstalledHookDelegate<T>() {
                Delegates = delegates,
                Index = index,
                ApplicationName = StarcounterEnvironment.AppName
            };
        }
    }
}
