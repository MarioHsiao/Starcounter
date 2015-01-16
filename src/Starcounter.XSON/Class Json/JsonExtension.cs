 ﻿using System;
using System.Collections.Generic;

namespace Starcounter.Advanced.XSON {
    /// <summary>
    /// Extension class for Json. Contains advanced features that can be excluded for normal use.
    /// </summary>
    public static class JsonExtension {
        public static void AddStepSibling(this Json json, Json stepSibling) {
            if (json._stepSiblings == null)
                json._stepSiblings = new List<Json>();
            json._stepSiblings.Add(stepSibling);
            stepSibling._stepParent = json;
            MergeTransaction(json, stepSibling);
        }

        public static bool RemoveStepSibling(this Json json, Json stepSibling) {
            bool b = false;
            if (json._stepSiblings != null) {
                b = json._stepSiblings.Remove(stepSibling);
                stepSibling._stepParent = null;
            }
            return b;
        }

        public static bool HasStepSiblings(this Json json) {
            return (json._stepSiblings != null && json._stepSiblings.Count > 0);
        }

        public static IEnumerable<Json> GetStepSiblings(this Json json) {
            return json._stepSiblings;
        }

        public static string GetAppName(this Json json) {
            return json._appName;
        }

        public static void SetAppName(this Json json, string value) {
            json._appName = value;
        }

        public static void SetEnableDirtyCheck(this Json json, bool value) {
            json._dirtyCheckEnabled = value;
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope(this Json json, Action action) {
            var t = json.Transaction;
            if (t != null)
                t.Scope(action);
            else 
                action();
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope<T>(this Json json, Action<T> action, T arg) {
            var t = json.Transaction;
            if (t != null)
                t.Scope<T>(action, arg);
            else
                action(arg);
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope<T1, T2>(this Json json, Action<T1, T2> action, T1 arg1, T2 arg2) {
            var t = json.Transaction;
            if (t != null)
                t.Scope<T1, T2>(action, arg1, arg2);
            else
                action(arg1, arg2);
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope<T1, T2, T3>(this Json json, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {
            var t = json.Transaction;
            if (t != null)
                t.Scope<T1, T2, T3>(action, arg1, arg2, arg3);
            else
                action(arg1, arg2, arg3);
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static T Scope<T>(this Json json, Func<T> func) {
            var t = json.Transaction;
            if (t != null)
                return t.Scope<T>(func);
            return func();
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<T1, TResult>(this Json json, Func<T1, TResult> func, T1 arg1) {
            var t = json.Transaction;
            if (t != null)
                return t.Scope<T1, TResult>(func, arg1);
            return func(arg1);
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<T1, T2, TResult>(this Json json, Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {
            var t = json.Transaction;
            if (t != null)
                return t.Scope<T1, T2, TResult>(func, arg1, arg2);
            return func(arg1, arg2);
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<T1, T2, T3, TResult>(this Json json, Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {
            var t = json.Transaction;
            if (t != null)
                return t.Scope<T1, T2, T3, TResult>(func, arg1, arg2, arg3);
            return func(arg1, arg2, arg3);
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the function.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<T1, T2, T3, T4, TResult>(this Json json, Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            var t = json.Transaction;
            if (t != null)
                return t.Scope<T1, T2, T3, T4, TResult>(func, arg1, arg2, arg3, arg4);
            return func(arg1, arg2, arg3, arg4);
        }

        private static void MergeTransaction(Json main, Json toMerge) {
            var mainTransaction = main.ThisTransaction;
            var toMergeTransaction = toMerge.ThisTransaction;

            if (mainTransaction != null && toMergeTransaction != null && mainTransaction != toMergeTransaction) {
                mainTransaction.MergeTransaction(toMergeTransaction);

                // TODO: 
                // Reference counter to make sure commit, rollbacks and dispose works properly.
                toMerge.ThisTransaction = mainTransaction;
            }
        }
    }
}
