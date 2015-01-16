using System;
using System.Collections.Generic;

namespace Starcounter.Advanced {
    public interface ITransaction : IDisposable {
        void Commit();
        void Rollback();
        void Scope(Action action);
        void Scope<T>(Action<T> action, T arg);
        void Scope<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2);
        void Scope<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3);
        TResult Scope<TResult>(Func<TResult> func);
        TResult Scope<T, TResult>(Func<T, TResult> func, T arg);
        TResult Scope<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2);
        TResult Scope<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3);
        TResult Scope<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        Boolean IsDirty { get; }
        void MergeTransaction(ITransaction toMerge);
    }
}
