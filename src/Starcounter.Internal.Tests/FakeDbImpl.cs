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

        void IDb.Transaction(Action action) {

        }
    }

    internal class FakeTransaction : ITransaction {
        public void Commit() {
        
        }

        public void Rollback() {
        
        }

        public void Dispose() {
        
        }

        public Boolean IsDirty
        {
            get { return false; }
        }

        public void Add(Action action) {

        }

        public TResult Add<TResult>(Func<TResult> func) {
            return func();
        }

        public TResult Add<T, TResult>(Func<T, TResult> func, T arg) {
            return func(arg);
        }

        public TResult Add<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {
            return func(arg1, arg2);
        }

        public TResult Add<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {
            return func(arg1, arg2, arg3);
        }
    }
}
