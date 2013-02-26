

using System;
namespace Starcounter.Advanced {

    public interface IDb {
        Rows SQL(string query, params object[] args);
        Rows<T> SQL<T>(string query, params object[] args);
        Rows SlowSQL(string query, params object[] args);
        Rows<T> SlowSQL<T>(string query, params object[] args);
        void Transaction(Action action);
    }
}
