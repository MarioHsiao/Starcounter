

using System;
namespace Starcounter.Advanced {

    internal interface IDb {
        ISqlResult SQL(string query, params object[] args);
        ISqlResult<T> SQL<T>(string query, params object[] args);
        ISqlResult SlowSQL(string query, params object[] args);
        ISqlResult<T> SlowSQL<T>(string query, params object[] args);
        void Transaction(Action action);
    }
}
