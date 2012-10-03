
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Starcounter.Internal;
using System.Text;
using System.Runtime.InteropServices;


[assembly: InternalsVisibleTo("Starcounter.WebServing")]
[assembly: InternalsVisibleTo("Fakeway.Handler.Raw2Web")]

// TODO:
// There is a (newly added) HttpRequest in Starcounter as well which uses the same
// namespace. To avoid ambiguity this class is renamed.
// I guess this class should not be used when apps is integrated properly.
namespace Starcounter {
    /*
    /// <summary>
    /// References the memory containing the complete Http request sent from the
    /// Starcounter Gateway. 
    /// </summary>
    /// <remarks>
    /// The implementation uses pointers and offsets to the http request data
    /// (such as URI, Method, headers and content). In this way, memory copying
    /// and .NET marshalling is avoided unless needed.
    /// </remarks>
    public class HttpRequest {

        public static HttpRequest GET(string uri) {
            var length = uri.Length + 3 + 1 + 1;// GET + space + URI + space
            HttpRequest r;
            unsafe {
                fixed (char* pUri = uri) {
                    byte* ptr = (byte*)BitsAndBytes.Alloc(length);
                    //            byte[] vu = new byte[length]; 
                    ptr[0] = (byte)'G';
                    ptr[1] = (byte)'E';
                    ptr[2] = (byte)'T';
                    ptr[3] = (byte)' ';
                    ptr[length - 1] = (byte)' ';
                    Encoding.ASCII.GetBytes(pUri, uri.Length, ptr, length);
                    r = new HttpRequest((IntPtr)ptr);
                }
            }
            return r;
        }

        private IntPtr Raw;
        private int Size;

        public void GetRawRequest(out IntPtr ptr, out UInt32 sizeBytes) {
            ptr = Raw;
            sizeBytes = (UInt32)Size;
        }

#if QUICKANDDIRTY
        protected readonly IDictionary<string, string> _headers = new Dictionary<string, string>();

        internal string _Uri;


        public void GetRawVerbAndUriPlusSpace( out IntPtr ptr, out int size ) {
            ptr = Raw;
            unsafe {
                byte* p = (byte*)ptr;
                int t = 0;
                int spaces = 0;
                while (spaces < 2) {
                    if (*p == 32) {
                        spaces++;
                    }
                    p++;
                }
                size = (int)(p-((byte*)ptr));
            }
        }

        public string AsString {
            get {
                byte[] buff = new byte[Size];
                unsafe {
                    fixed (byte* pbuff = buff) {
                        Intrinsics.MemCpy( pbuff, (void*)Raw,(uint)Size );
                    }
                }
                string str = Encoding.UTF8.GetString(buff);
                return str;
            }
        }

        public string Verb {
            get {
                return AsString.Split(' ')[0];
            }
        }

        public string Uri {
            get {
                return AsString.Split(' ')[1];
            }
        }

#else
        unsafe HttpRequestPointers* Pointers; // Points to a structure
#endif
        public HttpRequest(IntPtr data) {
            Raw = data;
        }

        public HttpRequest(byte[] data) {
            unsafe {
                Size = data.Length + 1;
                var pRaw = (byte*)BitsAndBytes.Alloc(Size);
                fixed (byte* pData = data) {
                    Intrinsics.MemCpy((void*)pRaw, (void*)pData, (uint)data.Length);
                }
                pRaw[data.Length] = 32; // End with a space
                Raw = (IntPtr)pRaw;
            }
        }

        public HttpRequest() {
        }

        public bool IsAppView = false;

        public bool NeedsScriptInjection {
            get {
                return ViewModel != null;
            }
        }

        private string _DebugLog;

        public void Debug(string str) {
            if (_DebugLog == null)
                _DebugLog = "";
            _DebugLog += str;
        }

        public string DebugLog {
            get {
                return _DebugLog;
            }
        }

        private bool _WantsCompressed = false;
        public bool WantsCompressed {
            get {
                return _WantsCompressed;
            }
            set {
                _WantsCompressed = value;
            }
        }

        private bool _AcceptsFasterThanJson = true;
        public bool AcceptsFasterThanJson {
            get {
                return _AcceptsFasterThanJson;
            }
            set {
                _AcceptsFasterThanJson = value;
            }
        }

        public byte[] ViewModel { get; set; }
        public bool CanUseStaticResponse {
            get {
                return ViewModel == null;
            }
        }

        public bool TryGetValue(string key, out string value) {
#if QUICKANDDIRTY
            return _headers.TryGetValue(key, out value);
#else
                throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Gets the header with the key specified by name.
        /// </summary>
        /// <param name="name">The header key</param>
        /// <returns>The value of the header</returns>
        public string this[string name] {
#if QUICKANDDIRTY
            get {
                string value;
                return _headers.TryGetValue(name, out value) ? value : default(string);
            }
            set {
                _headers[name] = value;
            }
#else
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
#endif
        }

        //        public IDictionary<string, string> Headers {
        //            get {
        //#if QUICKANDDIRTY
        //                return _headers;
        //#else
        //                throw new NotImplementedException();
        //#endif
        //            }
        //        }

        public SessionID SessionID {
            get;
            set;
        }

        //        public HttpMethod Method {
        //            get {
        //                if (Method == "GET") {
        //                    return HttpMethod.GET;
        //                }
        //                else if (Method == "POST") {
        //                    return HttpMethod.POST;
        //                }
        //                else {
        //                    throw new NotImplementedException("HTTP Method " + Method);
        //                }
        //            }
        //        }

//        public string Method;

        public string Body {
            get {
                throw new NotImplementedException();
            }
        }
#if QUICKANDDIRTY

//        public string Scheme { get; set; }
#endif

    }

    */
}

