using Starcounter.Internal;
using System;
using System.Collections.Generic;
using Starcounter.Hooks;

namespace Starcounter {
    
    internal static class HookLock {
        public static readonly object Sync = new object();
    }

    /// <summary>
    /// Principal entrypoint to the Starcounter hook API Provides
    /// a set of events allowing hooks to be registered.
    /// </summary>
    /// <typeparam name="T">The database type to hook.</typeparam>
    public static class Hook2<T> {

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

    /// <summary>
    /// Principal entrypoint to the Commit Hook API for developers. Provides
    /// a set of methods allowing hooks to be registered.
    /// </summary>
    /// <typeparam name="T">Type of the signature defining the delegate.</typeparam>
    public static class Hook<T> {
        static Dictionary<string, HookDelegateList<T>> hooksPerSignature = new Dictionary<string, HookDelegateList<T>>();

        /// <summary>
        /// Register <c>callback</c> to be invoked when a new instance of
        /// <typeparamref name="T"/> is inserted.
        /// </summary>
        /// <param name="callback">The delegate to be invoked.</param>
        public static void OnInsert(Action<T> callback) {
            OnInsert(typeof(T), callback);
        }

        /// <summary>
        /// Register <c>callback</c> to be invoked when a new instance of
        /// <paramref name="t"/> is inserted.
        /// </summary>
        /// <param name="t">Database type that triggers the delegate to
        /// be invoked when instances are inserted.</param>
        /// <param name="callback">The delegate to be invoked.</param>
        public static void OnInsert(Type t, Action<T> callback) {
            if (!typeof(T).IsAssignableFrom(t)) {
                throw new ArgumentException();
            }
            lock (HookLock.Sync) {
                InstallHook(t, CommitHookConfiguration.Insert, AddDelegate(callback));
            }
        }

        /// <summary>
        /// Register <c>callback</c> to be invoked when an instance of
        /// <typeparamref name="T"/> is updated.
        /// </summary>
        /// <param name="callback">The delegate to be invoked.</param>
        public static void OnUpdate(Action<T> callback) {
            OnUpdate(typeof(T), callback);
        }

        /// <summary>
        /// Register <c>callback</c> to be invoked when an instance of
        /// <paramref name="t"/> is updated.
        /// </summary>
        /// <param name="t">Database type that triggers the delegate to
        /// be invoked when instances are updated.</param>
        /// <param name="callback">The delegate to be invoked.</param>
        public static void OnUpdate(Type t, Action<T> callback) {
            if (!typeof(T).IsAssignableFrom(t)) {
                throw new ArgumentException();
            }
            lock (HookLock.Sync) {
                InstallHook(t, CommitHookConfiguration.Update, AddDelegate(callback));
            }
        }

        /// <summary>
        /// Register <c>callback</c> to be invoked when an instance of
        /// <typeparamref name="T"/> is deleted.
        /// </summary>
        /// <param name="callback">The delegate to be invoked.</param>
        public static void OnDelete(Action<ulong> callback) {
            lock (HookLock.Sync) {
                var delegateRef = Hook<ulong>.AddDelegate(callback);
                InstallHook(typeof(T), CommitHookConfiguration.Delete, delegateRef);
            }
        }

        static RuntimeInstalledHookDelegate<T> AddDelegate(Action<T> callback) {
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

        static void InstallHook(Type t, uint hookConfiguration, InvokableHook entry) {
            sccoredb.SCCOREDB_TABLE_INFO tableInfo;
            List<InvokableHook> installed;

            var result = sccoredb.sccoredb_get_table_info_by_name(t.FullName, out tableInfo);
            if (result != 0) throw ErrorCode.ToException(result);

            var hookType = CommitHookConfiguration.ToHookType(hookConfiguration);
            var key = HookKey.FromTable(tableInfo.table_id, hookType);
            if (!InvokableHook.HooksPerTrigger.TryGetValue(key, out installed)) {
                var hookConfigMask = CommitHookConfiguration.CalculateEffectiveConfiguration(key);
                hookConfigMask |= hookConfiguration;

                result = sccoredb.star_set_commit_hooks(0, tableInfo.table_id, hookConfigMask);
                if (result != 0) {
                    throw ErrorCode.ToException(result);
                }

                installed = new List<InvokableHook>();
                InvokableHook.HooksPerTrigger[key] = installed;
            }

            installed.Add(entry);
        }
    }
}
