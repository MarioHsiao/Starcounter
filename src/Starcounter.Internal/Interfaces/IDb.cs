

using Starcounter.Internal;
using System;
namespace Starcounter.Advanced {

    public interface IDb {
        void RunAsync(Action action, Byte schedId = StarcounterEnvironment.InvalidSchedulerId);
        void RunSync(Action action, Byte schedId = StarcounterEnvironment.InvalidSchedulerId);
        Rows<dynamic> SQL(string query, params object[] args);
        Rows<T> SQL<T>(string query, params object[] args);
        Rows<dynamic> SlowSQL(string query, params object[] args);
        Rows<T> SlowSQL<T>(string query, params object[] args);
        void Transact(Action action);
        void Scope(Action action);
        bool HasDatabase { get; }
    }
}
