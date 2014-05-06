using System;
using System.Collections.Generic;

namespace Starcounter.Advanced {
    public interface ITransaction : IDisposable {
        void Commit();
        void Rollback();
        void Add(Action action);
        TResult Add<TResult>(Func<TResult> func);
        TResult Add<T, TResult>(Func<T, TResult> func, T arg);
        TResult Add<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2);
        TResult Add<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3);
        Boolean IsDirty { get; }
    }
}
