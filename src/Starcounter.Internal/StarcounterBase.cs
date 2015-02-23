
using System;
using Starcounter.Internal;

namespace Starcounter.Advanced {

    /// <summary>
    /// This class does not define any instance properties or behaviour. It just makes
    /// available the global static functions SQL, Transaction and communication handler functions
    /// GET,POST, etc. in various Starcounter classes for convenience.
    /// </summary>
    public class StarcounterBase : Handle {

        /// <summary>
        /// Inject database function provider here
        /// </summary>
        public static IDb _DB;

        /// <summary>
        /// 
        /// </summary>
        public static IRestServer Fileserver;

        /// <summary>
        /// 
        /// </summary>
        internal static ITransactionManager TransactionManager;

        /// <summary>
        /// Executes a query on the embedding database.
        /// </summary>
        /// <param name="query">The SQL query string excluding parameters. Parameters are supplied as ? marks.</param>
        /// <param name="args">The parameters corresponding to the ? marks in the query string.</param>
        /// <returns></returns>
        public static Rows<dynamic> SQL(string query, params object[] args) {
            return _DB.SQL(query, args);
        }

        /// <summary>
        /// Executes a query on the embedding database.
        /// </summary>
        /// <typeparam name="T">The class of each entity in the result."/></typeparam>
        /// <param name="query">The SQL query string excluding parameters. Parameters are supplied as ? marks.</param>
        /// <param name="args">The parameters corresponding to the ? marks in the query string.</param>
        /// <returns></returns>
        public static Rows<T> SQL<T>(string query, params object[] args) {
            return _DB.SQL<T>(query, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="pars"></param>
        /// <returns></returns>
        public static Rows<dynamic> SlowSQL(string str, params object[] pars) {
            return _DB.SlowSQL(str, pars);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="pars"></param>
        /// <returns></returns>
        public static Rows<T> SlowSQL<T>(string str, params object[] pars) {
            return _DB.SlowSQL<T>(str, pars);
        }

        /// <summary>
        /// 
        /// </summary>
        public static IREST REST {
            get {
                return _REST;
            }
        }


        /// <summary>
        /// Gets the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <returns>``0.</returns>
        public static T Get<T>(string uri) {
            return (T)Get(uri);
        }


        /// <summary>
        /// Gets the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="pars">The pars.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static object Get(string uri, params object[] pars) {
            throw new NotImplementedException();
        }

    }
}
