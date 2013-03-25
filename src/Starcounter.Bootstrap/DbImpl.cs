using System;
using Starcounter.Advanced;

namespace Starcounter.Internal {
    internal class DbImpl : IDb {
        Rows<dynamic> IDb.SQL(string query, params object[] args) {
            return Db.SQL(query, args);
        }

        Rows<T> IDb.SQL<T>(string query, params object[] args) {
            return Db.SQL<T>(query, args);
        }

        Rows<dynamic> IDb.SlowSQL(string query, params object[] args) {
            return Db.SlowSQL(query, args);
        }

        Rows<T> IDb.SlowSQL<T>(string query, params object[] args) {
            return Db.SlowSQL<T>(query, args);
        }

        void IDb.Transaction(Action action) {
            Db.Transaction(action);
        }

        ITransaction IDb.NewCurrent() {
            return Transaction.NewCurrent();
        }

        void IDb.SetCurrentTransaction(ITransaction transaction) {
            Transaction.SetCurrent((Transaction)transaction);
        }

        ITransaction IDb.GetCurrentTransaction() {
            return Transaction.GetCurrent();
        }
    }
}
