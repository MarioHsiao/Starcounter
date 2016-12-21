using Starcounter.Internal;
using System;
using Starcounter.XSON;

namespace Starcounter.Advanced.XSON {
    /// <summary>
    /// Extension class for Json. Contains advanced features that can be excluded for normal use.
    /// </summary>
    public static class JsonExtension {
        /// <summary>
        /// Returns the name of the app that created this instance.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string GetAppName(this Json json) {
            return json.appName;
        }

        /// <summary>
        /// Connects the json with an existing json as siblings.
        /// </summary>
        /// <remarks>
        /// Using this method bypasses the standard merger (that merges
        /// json in responses from Self.GET) and does not trigger the 
        /// merge-callback.
        /// 
        /// </remarks>
        /// <param name="json"></param>
        /// <param name="toWrap"></param>
        public static void MergeJson(this Json main, Json json) {
            SiblingList siblings = json.Siblings;

            if (siblings == null) {
                siblings = new SiblingList();
                siblings.Add(json);
                json.Siblings = siblings;
            }

            var oldSiblings = main.Siblings;
            
            siblings.Add(main);
            siblings.CompareAndInvalidate(oldSiblings);

            if (oldSiblings != null) {
                oldSiblings.Remove(main);
            }

            main.wrapInAppName = true;
            main.Siblings = siblings;
        }
        
        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        public static void Scope(this Json json, Action action) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    StarcounterBase.TransactionManager.Scope(handle, action);
                else
                    action();

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        public static TResult Scope<TResult>(this Json json, Func<TResult> func) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    return StarcounterBase.TransactionManager.Scope<TResult>(handle, func);
                return func();

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static void Scope<T>(this Json json, Action<T> action, T arg) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    StarcounterBase.TransactionManager.Scope<T>(handle, action, arg);
                else
                    action(arg);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static void Scope<T1, T2>(this Json json, Action<T1, T2> action, T1 arg1, T2 arg2) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    StarcounterBase.TransactionManager.Scope<T1, T2>(handle, action, arg1, arg2);
                else
                    action(arg1, arg2);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Action either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="action">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static void Scope<T1, T2, T3>(this Json json, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    StarcounterBase.TransactionManager.Scope<T1, T2, T3>(handle, action, arg1, arg2, arg3);
                else
                    action(arg1, arg2, arg3);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }
        
        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static TResult Scope<T, TResult>(this Json json, Func<T, TResult> func, T arg) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    return StarcounterBase.TransactionManager.Scope<T, TResult>(handle, func, arg);
                return func(arg);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static TResult Scope<T1, T2, TResult>(this Json json, Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    return StarcounterBase.TransactionManager.Scope<T1, T2, TResult>(handle, func, arg1, arg2);
                return func(arg1, arg2);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the action.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static TResult Scope<T1, T2, T3, TResult>(this Json json, Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    return StarcounterBase.TransactionManager.Scope<T1, T2, T3, TResult>(handle, func, arg1, arg2, arg3);
                return func(arg1, arg2, arg3);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the function.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static TResult Scope<T1, T2, T3, T4, TResult>(this Json json, Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    return StarcounterBase.TransactionManager.Scope<T1, T2, T3, T4, TResult>(handle, func, arg1, arg2, arg3, arg4);
                return func(arg1, arg2, arg3, arg4);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the function.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static TResult Scope<T1, T2, T3, T4, T5, TResult>(this Json json, Func<T1, T2, T3, T4, T5, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    return StarcounterBase.TransactionManager.Scope<T1, T2, T3, T4, T5, TResult>(handle, func, arg1, arg2, arg3, arg4, arg5);
                return func(arg1, arg2, arg3, arg4, arg5);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Executes the specifed Func either in the scope of a transaction
        /// on the object or if no transaction is found, just executes the function.
        /// </summary>
        /// <param name="func">The delegate to execute</param>
        [Obsolete("This extensionmethod is obsolete and will be removed in a later version.")]
        public static TResult Scope<T1, T2, T3, T4, T5, T6, TResult>(this Json json, Func<T1, T2, T3, T4, T5, T6, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) {

            // @chrhol please review and probably correct this workaround.
            String origAppName = StarcounterEnvironment.AppName;

            try {

                StarcounterEnvironment.AppName = json.appName;

                var handle = json.GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    return StarcounterBase.TransactionManager.Scope<T1, T2, T3, T4, T5, T6, TResult>(handle, func, arg1, arg2, arg3, arg4, arg5, arg6);
                return func(arg1, arg2, arg3, arg4, arg5, arg6);

            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }
    }
}
