 using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter.Internal;

namespace Starcounter.Advanced.XSON {
    /// <summary>
    /// Extension class for Json. Contains advanced features that can be excluded for normal use.
    /// </summary>
    public static class JsonExtension {
        public static void AddStepSibling(this Json json, Json stepSibling) {
            if (json._stepSiblings == null)
                json._stepSiblings = new List<Json>();
            if (stepSibling._refFromStepSiblings == null)
                stepSibling._refFromStepSiblings = new List<Json>();
            
            json._stepSiblings.Add(stepSibling);
            stepSibling._refFromStepSiblings.Add(json);
        }

        public static bool RemoveStepSibling(this Json json, Json stepSibling) {
            bool b = false;
            if (json._stepSiblings != null) {
                b = json._stepSiblings.Remove(stepSibling);
                if (b)
                    b = stepSibling._refFromStepSiblings.Remove(json);
            }
            return b;
        }

        public static bool HasStepSiblings(this Json json) {
            return (json._stepSiblings != null && json._stepSiblings.Count > 0);
        }

        /// <summary>
        /// Getting recursively all sibling for the given Json.
        /// </summary>
        public static void GetAllStepSiblings(Json obj, ref List<Json> stepSiblingsList) {
            if (obj._stepSiblings != null) {
                foreach (Json s in obj.GetStepSiblings()) {
                    GetAllStepSiblings(s, ref stepSiblingsList);
                    stepSiblingsList.Add(s);
                }
            }
        }

        public static IEnumerable<Json> GetStepSiblings(this Json json) {
            if (json._stepSiblings != null)
                return json._stepSiblings;
            return Enumerable.Empty<Json>();
        }

        public static void RemoveAllStepSiblings(this Json json) {
            if (json._stepSiblings != null) {
                foreach (var sibling in json._stepSiblings) {
                    sibling._refFromStepSiblings.Remove(json);
                }
                json._stepSiblings.Clear();
            }
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
            var handle = json.TransactionHandle;
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
            var handle = json.TransactionHandle;
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
            var handle = json.TransactionHandle;
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
            var handle = json.TransactionHandle;
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
            var handle = json.TransactionHandle;
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
            var handle = json.TransactionHandle;
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
            var handle = json.TransactionHandle;
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
            var handle = json.TransactionHandle;
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
            var handle = json.TransactionHandle;
            if (handle != TransactionHandle.Invalid)
                return StarcounterBase.TransactionManager.Scope<T1, T2, T3, T4, TResult>(handle, func, arg1, arg2, arg3, arg4);
            return func(arg1, arg2, arg3, arg4);
        }
    }
}
