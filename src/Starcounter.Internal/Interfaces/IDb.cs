

using System;
namespace Starcounter.Advanced {

    public interface IDb {
        void RunAsync(Action action, Byte schedId = Byte.MaxValue);
        void RunSync(Action action, Byte schedId = Byte.MaxValue);
        Rows<dynamic> SQL(string query, params object[] args);
        Rows<T> SQL<T>(string query, params object[] args);
        Rows<dynamic> SlowSQL(string query, params object[] args);
        Rows<T> SlowSQL<T>(string query, params object[] args);
        void Transaction(Action action);
        void Scope(Action action);
//        ITransaction CurrentTransaction { get; }
    }
}
