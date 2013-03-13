
using Starcounter.Internal;
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
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "GET " + uri, handler);
        }

        public static void GET(ushort port, string uri, Func<object> handler) {
            _REST.RegisterHandler(port, "GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a GET verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "GET " + uri, handler);
        }

        public static void GET<T>(ushort port, string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(port, "GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a GET verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "GET " + uri, handler);
        }

        public static void GET<T1, T2>(ushort port, string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(port, "GET " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "GET " + uri, handler);
        }

        public static void GET<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>(port, "GET " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "GET " + uri, handler);
        }

        public static void GET<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, "GET " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "GET " + uri, handler);
        }

        public static void GET<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a PUT verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT(string uri, Func<object> handler) {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT(ushort port, string uri, Func<object> handler) {
            _REST.RegisterHandler(port, "PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a PUT verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T>(ushort port, string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(port, "PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a PUT verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T1, T2>(ushort port, string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(port, "PUT " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>(port, "PUT " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, "PUT " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a GET verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST(string uri, Func<object> handler) {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST(ushort port, string uri, Func<object> handler) {
            _REST.RegisterHandler(port, "POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a POST verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T>(ushort port, string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(port, "POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a POST verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T1, T2>(ushort port, string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(port, "POST " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>(port, "POST " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, "POST " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a DELETE verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE(string uri, Func<object> handler) {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE(ushort port, string uri, Func<object> handler) {
            _REST.RegisterHandler(port, "DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a DELETE verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T>(ushort port, string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(port, "DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a DELETE verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T1, T2>(ushort port, string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(port, "DELETE " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>(port, "DELETE " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, "DELETE " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a PATCH verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH(string uri, Func<object> handler) {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH(ushort port, string uri, Func<object> handler) {
            _REST.RegisterHandler(port, "PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a PATCH verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T>(ushort port, string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(port, "PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a PATCH verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T1, T2>(string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T1, T2>(ushort port, string uri, Func<T1, T2, object> handler) {
            _REST.RegisterHandler<T1, T2>(port, "PATCH " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>(port, "PATCH " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, "PATCH " + uri, handler);
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
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "PATCH " + uri, handler);
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
