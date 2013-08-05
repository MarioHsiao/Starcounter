

using Starcounter.Advanced;
using Starcounter.Internal;
using System;
namespace Starcounter {

    
    /// <summary>
    /// Allows you to register communication endpoints such as REST style handlers
    /// (GET/POST/PUT/DELETE/PATCH etc.)
    /// </summary>
    /// <remarks>
    /// Allows endpoints to be registered that corresponds to methods and URIs (paths
    /// with optional parameters). Even though REST handlers are typically associated with http, they 
    /// can also be implemented in protocols such as HTTP1.1, SPDY, HTTP2.0 (DRAFT),
    /// WebSockets and other means of communication. As long as their endpoints are defined as methods
    /// (verbs such as GET) and URI templates (i.e. /news/sports/{?}).
    /// </remarks>
    public partial class Handle {

        /// <summary>
        /// Inject REST handler function provider here
        /// </summary>
        public static volatile IREST _REST;
   
        /// <summary>
        /// Register the specified uri with a PUT verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT(string uri, Func<Response> handler)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT(ushort port, string uri, Func<Response> handler)
        {
            _REST.RegisterHandler(port, "PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a PUT verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T>(string uri, Func<T, Response> handler)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T>(ushort port, string uri, Func<T, Response> handler)
        {
            _REST.RegisterHandler<T>(port, "PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a PUT verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1, T2>(string uri, Func<T1, T2, Response> handler)
        {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T1, T2>(ushort port, string uri, Func<T1, T2, Response> handler)
        {
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
        public static void PUT<T1, T2, T3>(string uri, Func<T1, T2, T3, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, Response> handler)
        {
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
        public static void PUT<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, Response> handler)
        {
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
        public static void PUT<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PUT " + uri, handler);
        }

        public static void PUT<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a GET verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST(string uri, Func<Response> handler)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST(ushort port, string uri, Func<Response> handler)
        {
            _REST.RegisterHandler(port, "POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a POST verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T>(string uri, Func<T, Response> handler)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T>(ushort port, string uri, Func<T, Response> handler)
        {
            _REST.RegisterHandler<T>(port, "POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a POST verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2>(string uri, Func<T1, T2, Response> handler)
        {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T1, T2>(ushort port, string uri, Func<T1, T2, Response> handler)
        {
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
        public static void POST<T1, T2, T3>(string uri, Func<T1, T2, T3, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, Response> handler)
        {
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
        public static void POST<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, Response> handler)
        {
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
        public static void POST<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "POST " + uri, handler);
        }

        public static void POST<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a DELETE verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE(string uri, Func<Response> handler)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE(ushort port, string uri, Func<Response> handler)
        {
            _REST.RegisterHandler(port, "DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a DELETE verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T>(string uri, Func<T, Response> handler)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T>(ushort port, string uri, Func<T, Response> handler)
        {
            _REST.RegisterHandler<T>(port, "DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a DELETE verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T1, T2>(string uri, Func<T1, T2, Response> handler)
        {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T1, T2>(ushort port, string uri, Func<T1, T2, Response> handler)
        {
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
        public static void DELETE<T1, T2, T3>(string uri, Func<T1, T2, T3, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, Response> handler)
        {
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
        public static void DELETE<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, Response> handler)
        {
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
        public static void DELETE<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "DELETE " + uri, handler);
        }

        public static void DELETE<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a PATCH verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH(string uri, Func<Response> handler)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH(ushort port, string uri, Func<Response> handler)
        {
            _REST.RegisterHandler(port, "PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a PATCH verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T>(string uri, Func<T, Response> handler)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T>(ushort port, string uri, Func<T, Response> handler)
        {
            _REST.RegisterHandler<T>(port, "PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a PATCH verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T1, T2>(string uri, Func<T1, T2, Response> handler)
        {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T1, T2>(ushort port, string uri, Func<T1, T2, Response> handler)
        {
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
        public static void PATCH<T1, T2, T3>(string uri, Func<T1, T2, T3, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, Response> handler)
        {
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
        public static void PATCH<T1, T2, T3, T4>(string uri, Func<T1, T2, T3, T4, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T1, T2, T3, T4>(ushort port, string uri, Func<T1, T2, T3, T4, Response> handler)
        {
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
        public static void PATCH<T1, T2, T3, T4, T5>(string uri, Func<T1, T2, T3, T4, T5, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "PATCH " + uri, handler);
        }

        public static void PATCH<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, Response> handler)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "PATCH " + uri, handler);
        }
    }
}
