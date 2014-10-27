using System;
using System.Collections.Generic;

namespace Starcounter.Advanced {
    public interface ITransaction : IDisposable {
        void Commit();
        void Rollback();
        void Add(Action action);
        void Add<T>(Action<T> action, T arg);
        void Add<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2);
        void Add<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3);
        TResult AddAndReturn<TResult>(Func<TResult> func);
        TResult AddAndReturn<T, TResult>(Func<T, TResult> func, T arg);
        TResult AddAndReturn<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2);
        TResult AddAndReturn<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3);
        Boolean IsDirty { get; }
        void MergeTransaction(ITransaction toMerge);
    }
}
