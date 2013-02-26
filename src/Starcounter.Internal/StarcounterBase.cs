
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Starcounter.Bootstrap")]


namespace Starcounter.Advanced {

    /// <summary>
    /// This class does not define any instance properties or behaviour. It just makes
    /// available the global static functions SQL, Transaction and communication handler functions
    /// GET,POST, etc. in various Starcounter classes for convenience.
    /// </summary>
    public class StarcounterBase {

        /// <summary>
        /// Inject database function provider here
        /// </summary>
        public static IDb _DB;

        /// <summary>
        /// Inject REST handler function provider here
        /// </summary>
        public static IREST _REST;

        /// <summary>
        /// 
        /// </summary>
        public static IHttpRestServer Fileserver;

        /// <summary>
        /// Runs code as an ACID database transaction in the embedding database.
        /// </summary>
        /// <param name="action"></param>
        public static void Transaction(Action action) {
            _DB.Transaction(action);
        }

        /// <summary>
        /// Executes a query on the embedding database.
        /// </summary>
        /// <param name="query">The SQL query string excluding parameters. Parameters are supplied as ? marks.</param>
        /// <param name="args">The parameters corresponding to the ? marks in the query string.</param>
        /// <returns></returns>
        public static Rows SQL(string query, params object[] args) {
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
        public static Rows SlowSQL(string str, params object[] pars) {
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
        /// <param name="uri"></param>
        /// <returns></returns>
        public static object Get(string uri) {
            return _REST.Get(uri);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static object Request(HttpRequest request) {
            return _REST.Request(request);
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
        /// Register the specified uri with a GET verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET(string uri, Func<object> handler) {
            _REST.RegisterHandler("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a GET verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a GET verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with three variable parameters, with a GET verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="uri">The uri to register</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with four variable parameters, with a GET verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with five variable parameters, with a GET verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a PUT verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT(string uri, Func<object> handler) {
            _REST.RegisterHandler("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a PUT verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a PUT verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with three variable parameters, with a PUT verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="uri">The uri to register</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with four variable parameters, with a PUT verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with five variable parameters, with a PUT verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a GET verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST(string uri, Func<object> handler) {
            _REST.RegisterHandler("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a POST verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a POST verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with three variable parameters, with a POST verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="uri">The uri to register</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with four variable parameters, with a POST verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with five variable parameters, with a POST verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a DELETE verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE(string uri, Func<object> handler) {
            _REST.RegisterHandler("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a DELETE verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a DELETE verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with three variable parameters, with a DELETE verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="uri">The uri to register</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with four variable parameters, with a DELETE verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with five variable parameters, with a DELETE verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a PATCH verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH(string uri, Func<object> handler) {
            _REST.RegisterHandler("PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a PATCH verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>("PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a PATCH verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>("PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with three variable parameters, with a PATCH verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="uri">The uri to register</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>("PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with four variable parameters, with a PATCH verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>("PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with five variable parameters, with a PATCH verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>("PATCH " + uri, handler);
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
