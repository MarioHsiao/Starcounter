// ***********************************************************************
// <copyright file="RequestHandler.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal.Uri;
using System;
using System.Text;

namespace Starcounter
{
    /// <summary>
    /// Accepts registrations of Rest style user handlers. These handlers
    /// allows the user to catch restful calls using http verbs such as
    /// for example GET, POST, PATCH and DELETE using uri templates with paramters
    /// such as GET /persons/{name}.
    /// </summary>
    /// <remarks>Incomplete, needs some love. TODO!</remarks>
    public class RequestHandler
    {
        /// <summary>
        /// TODO! Make internal with friends
        /// </summary>
        public static void Reset() {
            _RequestProcessor = null;
            UriMatcherBuilder = new RequestProcessorBuilder();
        }

        /// <summary>
        /// The _ request processor
        /// </summary>
        private static TopLevelRequestProcessor _RequestProcessor;
        /// <summary>
        /// Gets the request processor.
        /// </summary>
        /// <value>The request processor.</value>
        public static TopLevelRequestProcessor RequestProcessor {
            get {
                if (_RequestProcessor == null) {
                    var compiler = UriMatcherBuilder.CreateCompiler();
                    var pt = ParseTreeGenerator.BuildParseTree(UriMatcherBuilder.Handlers);
                    var ast = AstTreeGenerator.BuildAstTree( pt );
                    _RequestProcessor = compiler.CreateMatcher( ast );
                }
                return _RequestProcessor;
            }
        }

        /// <summary>
        /// The URI matcher builder
        /// </summary>
        public static RequestProcessorBuilder UriMatcherBuilder = new RequestProcessorBuilder();

        /// <summary>
        /// GETs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void GET(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("GET " + uri, handler);
        }
        /// <summary>
        /// GETs the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T>(string uri, Func<T, object> handler) {
            UriMatcherBuilder.RegisterHandler<T>("GET " + uri, handler);
        }
        /// <summary>
        /// GETs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T1,T2>(string uri, Func<T1,T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1,T2>("GET " + uri, handler);
        }
        /// <summary>
        /// PUTs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("PUT " + uri, handler);
        }
        /// <summary>
        /// PUTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1>(string uri, Func<T1, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1>("PUT " + uri, handler);
        }
        /// <summary>
        /// PUTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("PUT " + uri, handler);
        }
        /// <summary>
        /// POSTs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void POST(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("POST " + uri, handler);
        }
        /// <summary>
        /// POSTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1>(string uri, Func<T1, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1>("POST " + uri, handler);
        }
        /// <summary>
        /// POSTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("POST " + uri, handler);
        }
        /// <summary>
        /// POSTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <typeparam name="T3">The type of the t3.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2, T3>("POST " + uri, handler);
        }
        /// <summary>
        /// DELETEs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("DELETE " + uri, handler);
        }
        /// <summary>
        /// PATCHs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH(string uri, Func<string, object> handler) {
            UriMatcherBuilder.RegisterHandler("PATCH " + uri, handler);
        }

//        public static T Get<T>(string uri) where T : App {
//            return (T)Get(uri);
//        }

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
        public static object Get( string uri, params object[] pars ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>System.Object.</returns>
        public static object Get(string uri) {
            var length = uri.Length + 3 + 1 + 1;// GET + space + URI + space
            byte[] vu = new byte[length]; 
            vu[0] = (byte)'G';
            vu[1] = (byte)'E';
            vu[2] = (byte)'T';
            vu[3] = (byte)' ';
            vu[length-1] = (byte)' ';
            Encoding.ASCII.GetBytes(uri, 0, uri.Length, vu, 4);
            object ret;
            SingleRequestProcessorBase handler;
            unsafe {
                fixed (byte* pvu = vu) {
                    RequestHandler.RequestProcessor.Process((IntPtr)pvu, vu.Length, true, null, out handler, out ret);
                }
            }
            return ret;
        }   

    }
}

