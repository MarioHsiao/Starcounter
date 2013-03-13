

using Starcounter.Internal;
using System;

namespace Starcounter {

    
    public partial class Handle {

        /// <summary>
        /// Register a uri template (e.g. <c>/mypath</c>) to catch an
        /// incoming GET request on the default port.
        /// </summary>
        /// <example>  
        /// This sample shows how to return the string "Hello World" when
        /// a request for <c>/demo</c> is received using the GET method.
        /// <code> 
        /// class TestClass  { 
        ///    static void Main() { 
        ///       Handle.GET("/demo", () => {
        ///          return "Hello World";    
        ///       }); 
        ///    } 
        /// } 
        /// </code> 
        /// </example>
        /// <remarks>
        /// The default port is read from the <see cref="Handle.Config.DefaultPort"/> property.
        /// </remarks>
        /// <param name="uriTemplate">The uri template to register.</param>
        /// <param name="code">The code to execute when a request is received.</param>
        public static void GET(string uriTemplate, Func<object> code) {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "GET " + uriTemplate, code);
        }

        /// <summary>
        /// Register a uri template (e.g. <c>/mypath</c>) to catch an
        /// incoming GET request on a specific port.
        /// </summary>
        /// <example>  
        /// This sample shows how to return the string "Hello World" when
        /// a request for <c>/demo</c> is received using the GET method on
        /// port 80.
        /// <code> 
        /// class TestClass  { 
        ///    static void Main() { 
        ///       Handle.GET(80, "/demo", () => {
        ///          return "Hello World";    
        ///       }); 
        ///    } 
        /// } 
        /// </code> 
        /// </example>
        /// <param name="uriTemplate">The uri template to register.</param>
        /// <param name="code">The code to execute when a request is received.</param>
        /// <param name="port">The port number to listen to.</param>
        public static void GET(ushort port, string uri, Func<object> handler) {
            _REST.RegisterHandler(port, "GET " + uri, handler);
        }


        /// <summary>
        /// Register a uri template with a dynamic parameter 
        /// (e.g. <c>/things/{?}</c>) to catch an
        /// incoming GET request on the default port.
        /// </summary>
        /// <remarks>
        /// The parameter can be of any of the following types:
        /// <list type="bullet" | "table">
        ///     <listheader>
        ///         <term>int</term>
        ///         <description>/example/123</description>
        ///     </listheader>
        ///     <item>
        ///         <term>long</term>
        ///         <description>/example/123</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <example>  
        /// This sample shows how to return a minimalistic web page when
        /// a request for <c>/persons/somename</c> is received using the GET method.
        /// <code> 
        /// class TestClass  { 
        ///    static void Main() { 
        ///       Handle.GET("/persons/{?}", (string name) => {
        ///          return "<!DOCTYPE html><title>The web page of " + name + "</title>";
        ///       }); 
        ///    } 
        /// } 
        /// </code> 
        /// </example>
        /// <remarks>
        /// The default port is read from the <see cref="Handle.Config.DefaultPort"/> property.
        /// </remarks>
        /// <param name="uriTemplate">The uri template to register.</param>
        /// <param name="code">The code to execute when a request is received.</param>
        public static void GET<T>(string uri, Func<T, object> handler) {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "GET " + uri, handler);
        }


        /// <summary>
        /// Register a uri template (e.g. <c>/mypath</c>) to catch an
        /// incoming GET request on a specific port.
        /// </summary>
        /// <example>  
        /// This sample shows how to return the string "Hello World" when
        /// a request for <c>/demo</c> is received using the GET method.
        /// <code> 
        /// class TestClass  { 
        ///    static void Main() { 
        ///       Handle.GET("/persons/{?}", (string name) => {
        ///          return "<!DOCTYPE html><title>The web page of " + name + "</title>";
        ///       }); 
        ///    } 
        /// } 
        /// </code> 
        /// </example>
        /// <param name="uriTemplate">The uri template to register.</param>
        /// <param name="code">The code to execute when a request is received.</param>
        /// <param name="port">The port number to listen to.</param>
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
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,"GET " + uri, handler);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="port"></param>
        /// <param name="uri"></param>
        /// <param name="handler"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="port"></param>
        /// <param name="uri"></param>
        /// <param name="handler"></param>
        public static void GET<T1, T2, T3>(ushort port, string uri, Func<T1, T2, T3, object> handler) {
            _REST.RegisterHandler<T1, T2, T3>(port,"GET " + uri, handler);
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="port"></param>
        /// <param name="uri"></param>
        /// <param name="handler"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="port"></param>
        /// <param name="uri"></param>
        /// <param name="handler"></param>
        public static void GET<T1, T2, T3, T4, T5>(ushort port, string uri, Func<T1, T2, T3, T4, T5, object> handler) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, "GET " + uri, handler);
        }

    }
}
