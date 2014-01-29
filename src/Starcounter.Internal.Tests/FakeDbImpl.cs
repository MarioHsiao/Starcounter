using System;
using Starcounter.Advanced;

namespace Starcounter.XSON.Tests {
    internal class FakeDbImpl : IDb {
        [ThreadStatic]
        private static ITransaction current;

        void IDb.RunAsync(Action action, Byte schedId) {
            // Do nothing.
        }

        void IDb.RunSync(Action action) {
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

        void IDb.SetCurrentTransaction(ITransaction transaction) {
            current = transaction;
        }

        ITransaction IDb.GetCurrentTransaction() {
            return current;
        }
    }

    internal class FakeTransaction : ITransaction {
        public void Add(Action action) {

        }

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
    }
}
