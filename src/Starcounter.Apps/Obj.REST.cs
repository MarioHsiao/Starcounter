
using Starcounter.Internal.Uri;
using System;
using System.IO;
using System.Text;
namespace Starcounter {
    public partial class App {

        static string requestProcessorsTempDirPlusSlash_ = null;

        /// <summary>
        /// Initializes Starcounter REST settings.
        /// </summary>
        /// <param name="databaseTempDir">Path to database temp directory.</param>
        public static void InitREST(String databaseTempDir)
        {
            requestProcessorsTempDirPlusSlash_ = databaseTempDir + "\\rps\\";

            if (!Directory.Exists(requestProcessorsTempDirPlusSlash_))
                Directory.CreateDirectory(requestProcessorsTempDirPlusSlash_);
        }

        /// <summary>
        /// Gets path to database temp directory.
        /// </summary>
        public static string RequestProcessorsTempDirPlusSlash
        {
            get { return requestProcessorsTempDirPlusSlash_; }
        }

        /// <summary>
        /// TODO! Make internal with friends
        /// </summary>
        public static void Reset() {
            _RequestProcessor = null;
            UriMatcherBuilder = new RequestProcessorBuilder();
        }

        private static TopLevelRequestProcessor _RequestProcessor;

        /// <summary>
        /// Gets the request processor.
        /// </summary>
        /// <value>The request processor.</value>
        public static TopLevelRequestProcessor RequestProcessor {
            get {
                if (_RequestProcessor == null) {
                    _RequestProcessor = UriMatcherBuilder.InstantiateRequestProcessor();
                }
                return _RequestProcessor;
            }
        }

        /// <summary>
        /// The URI matcher builder
        /// </summary>
        public static RequestProcessorBuilder UriMatcherBuilder = new RequestProcessorBuilder();

        /// <summary>
        /// Register the specified uri with a GET verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a GET verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T>(string uri, Func<T, object> handler) {
            UriMatcherBuilder.RegisterHandler<T>("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a GET verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with three variable parameters, with a GET verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="uri">The uri to register</param>
        /// <param name="handler">The handler.</param>
        public static void GET<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler){
            UriMatcherBuilder.RegisterHandler<T1, T2, T3>("GET " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4>("GET " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4, T5>("GET " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a PUT verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a PUT verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T>(string uri, Func<T, object> handler) {
            UriMatcherBuilder.RegisterHandler<T>("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a PUT verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PUT<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("PUT " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3>("PUT " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4>("PUT " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4, T5>("PUT " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a GET verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a POST verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T>(string uri, Func<T, object> handler) {
            UriMatcherBuilder.RegisterHandler<T>("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a POST verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void POST<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("POST " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3>("POST " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4>("POST " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4, T5>("POST " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a DELETE verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a DELETE verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T>(string uri, Func<T, object> handler) {
            UriMatcherBuilder.RegisterHandler<T>("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a DELETE verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void DELETE<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("DELETE " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3>("DELETE " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4>("DELETE " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4, T5>("DELETE " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri with a PATCH verb.
        /// </summary>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with one variable parameter, with a PATCH verb
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T>(string uri, Func<T, object> handler) {
            UriMatcherBuilder.RegisterHandler<T>("PATCH " + uri, handler);
        }

        /// <summary>
        /// Register the specified uri, with two variable parameters, with a PATCH verb
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="uri">The uri to register.</param>
        /// <param name="handler">The handler.</param>
        public static void PATCH<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("PATCH " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3>("PATCH " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4>("PATCH " + uri, handler);
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
            UriMatcherBuilder.RegisterHandler<T1, T2, T3, T4, T5>("PATCH " + uri, handler);
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

