using System;
using Starcounter.Advanced;

namespace Starcounter.Internal {
    internal class DbImpl : IDb {
        void IDb.RunAsync(Action action, Byte schedId) {
            Scheduling.ScheduleTask(action, false, schedId);
        }

        void IDb.RunSync(Action action, Byte schedId) {
            Scheduling.ScheduleTask(action, true, schedId);
        }

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

        void IDb.Transact(Action action) {
            Db.Transact(action);
        }

        void IDb.Scope(Action action) {
            Db.Scope(action);
        }

        bool IDb.HasDatabase {
            get { return Db.Environment.HasDatabase; }
        }

        //ITransaction IDb.CurrentTransaction {
        //    get { return Transaction.Current; }
        //}
    }
}
