using System;
using Starcounter.Advanced;

namespace Starcounter.Client.Tests.Application {
    internal class FakeDbImpl : IDb {
        [ThreadStatic]
        private static ITransaction current;

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

        ITransaction IDb.NewCurrent() {
            current = FakeTransaction.NewCurrent();
            return current;
        }

        void IDb.SetCurrentTransaction(ITransaction transaction) {
            current = transaction;
        }

        ITransaction IDb.GetCurrentTransaction() {
            return current;
        }
    }

    internal class FakeTransaction : ITransaction {
        internal static ITransaction NewCurrent() {
            return new FakeTransaction();
        }

        public void Commit() {
        
        }

        public void Rollback() {
        
        }

        public void Dispose() {
        
        }
    }
}
