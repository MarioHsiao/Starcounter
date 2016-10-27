using System;
using Starcounter.Advanced;

namespace Starcounter.XSON.Tests {
    internal class FakeDbImpl : IDb {
        void IDb.RunAsync(Action action, Byte schedId) {
            // Do nothing.
        }

        void IDb.RunSync(Action action, Byte schedId) {
            // Do nothing.
        }

        Rows<dynamic> IDb.SQL(string query, params object[] args) {
            return null;
        }

        Rows<T> IDb.SQL<T>(string query, params object[] args) {
            return null;
        }

        Rows<dynamic> IDb.SlowSQL(string query, params object[] args) {
            return null;
        }

        Rows<T> IDb.SlowSQL<T>(string query, params object[] args) {
            return null;
        }

        void IDb.Transact(Action action) {

        }

        void IDb.Scope(Action action) {

        }

        bool IDb.HasDatabase {
            get { return false; }
        }

//        ITransaction IDb.CurrentTransaction { get { return null; } }
    }

    internal class FakeTransaction : ITransaction {
        public void Commit() {
        
        }

        public void Rollback() {
        
        }

        public void Dispose() {
        
        }

        public Boolean IsDirty {
            get { return false; }
        }

        public Boolean IsReadOnly {
            get { return false; }
        }

        public void Scope(Action action) {

        }

        public TResult Scope<TResult>(Func<TResult> func) {
            return func();
        }

        public TResult Scope<T, TResult>(Func<T, TResult> func, T arg) {
            return func(arg);
        }

        public TResult Scope<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {
            return func(arg1, arg2);
        }

        public TResult Scope<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {
            return func(arg1, arg2, arg3);
        }

        public TResult Scope<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            return func(arg1, arg2, arg3, arg4);
        }

        public void Scope<T>(Action<T> action, T arg) {
            action(arg);
        }

        public void Scope<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2) {
            action(arg1, arg2);
        }

        public void Scope<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {
            action(arg1, arg2, arg3);
        }

        public void MergeTransaction(ITransaction toMerge) {
            toMerge.Dispose();
        }
    }
}
