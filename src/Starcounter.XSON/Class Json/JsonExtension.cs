 using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter.Internal;

namespace Starcounter.Advanced.XSON {
    /// <summary>
    /// Extension class for Json. Contains advanced features that can be excluded for normal use.
    /// </summary>
    public static class JsonExtension {
        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope(this Json json, Action action) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                StarcounterBase.TransactionManager.Scope(handle, action);
            else 
                action();
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope<T>(this Json json, Action<T> action, T arg) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                StarcounterBase.TransactionManager.Scope<T>(handle, action, arg);
            else
                action(arg);
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope<T1, T2>(this Json json, Action<T1, T2> action, T1 arg1, T2 arg2) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                StarcounterBase.TransactionManager.Scope<T1, T2>(handle, action, arg1, arg2);
            else
                action(arg1, arg2);
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope<T1, T2, T3>(this Json json, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                StarcounterBase.TransactionManager.Scope<T1, T2, T3>(handle, action, arg1, arg2, arg3);
            else
                action(arg1, arg2, arg3);
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<TResult>(this Json json, Func<TResult> func) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                return StarcounterBase.TransactionManager.Scope<TResult>(handle, func);
            return func();
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<T, TResult>(this Json json, Func<T, TResult> func, T arg) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                return StarcounterBase.TransactionManager.Scope<T, TResult>(handle, func, arg);
            return func(arg);
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<T1, T2, TResult>(this Json json, Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                return StarcounterBase.TransactionManager.Scope<T1, T2, TResult>(handle, func, arg1, arg2);
            return func(arg1, arg2);
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<T1, T2, T3, TResult>(this Json json, Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                return StarcounterBase.TransactionManager.Scope<T1, T2, T3, TResult>(handle, func, arg1, arg2, arg3);
            return func(arg1, arg2, arg3);
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the function.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<T1, T2, T3, T4, TResult>(this Json json, Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            var handle = json.GetTransactionHandle(true);
            if (handle != TransactionHandle.Invalid)
                return StarcounterBase.TransactionManager.Scope<T1, T2, T3, T4, TResult>(handle, func, arg1, arg2, arg3, arg4);
            return func(arg1, arg2, arg3, arg4);
        }
    }
}
