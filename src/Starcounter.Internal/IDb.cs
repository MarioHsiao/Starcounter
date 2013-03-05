

using System;
namespace Starcounter.Advanced {

    public interface IDb {
        Rows<dynamic> SQL(string query, params object[] args);
        Rows<T> SQL<T>(string query, params object[] args);
        Rows<dynamic> SlowSQL(string query, params object[] args);
        Rows<T> SlowSQL<T>(string query, params object[] args);
        void Transaction(Action action);
    }
}
