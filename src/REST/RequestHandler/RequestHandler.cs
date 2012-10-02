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
    /// <remarks>
    /// Incomplete, needs some love. TODO!
    /// </remarks>
    public class RequestHandler
    {
        /// <summary>
        /// TODO! Make internal with friends
        /// </summary>
        public static void Reset() {
            _RequestProcessor = null;
            UriMatcherBuilder = new RequestProcessorBuilder();
        }

        private static TopLevelRequestProcessor _RequestProcessor;
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

        public static RequestProcessorBuilder UriMatcherBuilder = new RequestProcessorBuilder();

        public static void GET(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("GET " + uri, handler);
        }
        public static void GET<T>(string uri, Func<T, object> handler) {
            UriMatcherBuilder.RegisterHandler<T>("GET " + uri, handler);
        }
        public static void GET<T1,T2>(string uri, Func<T1,T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1,T2>("GET " + uri, handler);
        }
        public static void PUT(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("PUT " + uri, handler);
        }
        public static void PUT<T1>(string uri, Func<T1, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1>("PUT " + uri, handler);
        }
        public static void PUT<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("PUT " + uri, handler);
        }
        public static void POST(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("POST " + uri, handler);
        }
        public static void POST<T1>(string uri, Func<T1, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1>("POST " + uri, handler);
        }
        public static void POST<T1, T2>(string uri, Func<T1, T2, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2>("POST " + uri, handler);
        }
        public static void POST<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            UriMatcherBuilder.RegisterHandler<T1, T2, T3>("POST " + uri, handler);
        }
        public static void DELETE(string uri, Func<object> handler) {
            UriMatcherBuilder.RegisterHandler("DELETE " + uri, handler);
        }
        public static void PATCH(string uri, Func<string, object> handler) {
            UriMatcherBuilder.RegisterHandler("PATCH " + uri, handler);
        }

//        public static T Get<T>(string uri) where T : App {
//            return (T)Get(uri);
//        }

        public static T Get<T>(string uri) {
            return (T)Get(uri);
        }
       
        public static object Get( string uri, params object[] pars ) {
            throw new NotImplementedException();
        }

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

