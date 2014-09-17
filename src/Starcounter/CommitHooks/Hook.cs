using System;
using System.Collections.Generic;

namespace Starcounter {
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
            InstallHook(t, InvokableHook.Insert, AddDelegate(callback));
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
            InstallHook(t, InvokableHook.Update, AddDelegate(callback));
        }

        /// <summary>
        /// Register <c>callback</c> to be invoked when an instance of
        /// <typeparamref name="T"/> is deleted.
        /// </summary>
        /// <param name="callback">The delegate to be invoked.</param>
        public static void OnDelete(Action<ulong> callback) {
            // For deletes, we need to track methods with a single
            // ulong argument (being the object identifier)
            // TODO:

            throw new NotImplementedException();
        }

        static HookDelegateListEntry<T> AddDelegate(Action<T> callback) {
            HookDelegateList<T> delegates;
            if (!hooksPerSignature.TryGetValue(typeof(T).FullName, out delegates)) {
                delegates = new HookDelegateList<T>();
                hooksPerSignature[typeof(T).FullName] = delegates;
            }

            var index = delegates.Add(callback);
            return new HookDelegateListEntry<T>() { Delegates = delegates, Index = index };
        }

        static void InstallHook(Type t, int operation, InvokableHook entry) {
            List<InvokableHook> installed;

            var key = t.FullName + operation;
            if (!InvokableHook.HooksPerTrigger.TryGetValue(key, out installed)) {
                installed = new List<InvokableHook>();
                InvokableHook.HooksPerTrigger[key] = installed;
            }

            installed.Add(entry);
        }
    }
}
