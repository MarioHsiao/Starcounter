using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net;
using Starcounter.Advanced;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Starcounter
{
    /// <summary>
    /// Handler response status.
    /// </summary>
    public enum HandlerStatus
    {
        /// <summary>
        /// The response is handled explicitly by some other means.
        /// </summary>
        Handled = 1
    }

    /// <summary>
    /// Handler response status.
    /// </summary>
    internal enum HandlerStatusInternal
    {
        /// <summary>
        /// The response is already sent.
        /// </summary>
        Done,

        /// <summary>
        /// The response was handled by some explicit means.
        /// </summary>
        AlreadyHandled,

        /// <summary>
        /// For example static resource server returns this if resource is not found.
        /// </summary>
        ResourceNotFound
    }

    /// <summary>
    /// Response exception class which encapsulates the response thrown.
    /// </summary>
    public class ResponseException : Exception
    {
        Response responseObject_;

        Object userObject_;

        /// <summary>
        /// Returns encapsulated Response object.
        /// </summary>
        public Response ResponseObject
        {
            get { return responseObject_; }
            set { responseObject_ = value; }
        }

        /// <summary>
        /// Returns encapsulated User object.
        /// </summary>
        public Object UserObject
        {
            get { return userObject_; }
            set { userObject_ = value; }
        }

        /// <summary>
        /// Response exception constructor.
        /// </summary>
        /// <param name="respObject"></param>
        public ResponseException(Response responseObject) {
            responseObject_ = responseObject;
        }

        /// <summary>
        /// Response exception constructor.
        /// </summary>
        /// <param name="respObject"></param>
        /// <param name="userObject"></param>
        public ResponseException(Response responseObject, Object userObject) {
            responseObject_ = responseObject;
            userObject_ = userObject;
        }
    }

    /// <summary>
    /// Represents an HTTP response.
    /// </summary>
    /// <remarks>
    /// The Starcounter Web Server caches resources as complete http responses.
    /// As the exact same response can often not be used, the cashed response also
    /// include useful offsets and injection points to facilitate fast transitions
    /// to individual http responses. The cached response is also used to cache resources
    /// (compressed or uncompressed content) even if the consumer wants to embed the content
    /// in a new http response.
    /// </remarks>
    public sealed partial class Response : Finalizing
    {
        /// <summary>
        /// Pointer to the merging routine.
        /// </summary>
        internal static Func<Request, Response, List<Response>, Response> ResponsesMergerRoutine_ = null;

        /// <summary>
        /// Current time bytes.
        /// </summary>
        static Byte[] CurrentDateHeaderBytes = null;

        /// <summary>
        /// Update current time.
        /// </summary>
        internal static void HttpDateUpdateProcedure(Object state) {
            CurrentDateHeaderBytes = Encoding.ASCII.GetBytes("Date: " + DateTime.Now.ToUniversalTime().ToString("r") + StarcounterConstants.NetworkConstants.CRLF);
        }

        /// <summary>
        /// Parses internal HTTP response.
        /// </summary>
        [DllImport("schttpparser.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_parse_http_response(
            Byte* response_buf,
            UInt32 response_size_bytes,
            Byte* out_http_response);

        /// <summary>
        /// Application name this response came from.
        /// </summary>
        String appName_;

        /// <summary>
        /// The plain response bytes.
        /// </summary>
        byte[] responseBytes_;

        /// <summary>
        /// The plain response size bytes.
        /// </summary>
        int responseSizeBytes_;

        /// <summary>
        /// Response body offset.
        /// </summary>
        int responseBodyOffset_;

        /// <summary>
        /// Response body size.
        /// </summary>
        int responseBodySizeBytes_;

        /// <summary>
        /// URIs related to this static resource.
        /// </summary>
        List<string> uris_;

        /// <summary>
        /// File path to static resource.
        /// </summary>
        string filePath_;

        /// <summary>
        /// File directory for this file resource.
        /// </summary>
        string fileDirectory_;

        /// <summary>
        /// File name for this file resource.
        /// </summary>
        string fileName_;

        /// <summary>
        /// Does file for this static resource exist?
        /// </summary>
        bool fileExists_;

        /// <summary>
        /// Date of file modification for this file resource.
        /// </summary>
        DateTime fileModified_;

        /// <summary>
        /// Indicates if user wants to send custom response.
        /// </summary>
        Boolean customFields_;

        /// <summary>
        /// Status code.
        /// </summary>
        UInt16 statusCode_;

        /// <summary>
        /// Handling status.
        /// </summary>
        HandlerStatusInternal handlingStatus_;

        /// <summary>
        /// Connection flags.
        /// </summary>
        ConnectionFlags connectionFlags_;

        /// <summary>
        /// Body string.
        /// </summary>
        String bodyString_;

        /// <summary>
        /// Body stream.
        /// </summary>
        Stream bodyStream_;

        /// <summary>
        /// Body bytes.
        /// </summary>
        Byte[] bodyBytes_;

        /// <summary>
        /// Status description string.
        /// </summary>
        String statusDescription_;

        /// <summary>
        /// Resource representation.
        /// </summary>
        IResource resource_;

        /// <summary>
        /// List of cookies.
        /// </summary>
        List<String> cookies_;

        /// <summary>
        /// Web-Socket handshake response bytes.
        /// </summary>
        Byte[] wsHandshakeResp_;

        /// <summary>
        /// Socket data buffer pointer.
        /// </summary>
        IntPtr socketDataIntPtr_;

        /// <summary>
        /// Response buffer pointer.
        /// </summary>
        IntPtr responseStructIntPtr_;

        /// <summary>
        /// Internal structure with HTTP response information.
        /// </summary>
        unsafe HttpResponseInternal* httpResponseStruct_;

        /// <summary>
        /// Direct pointer to session data.
        /// </summary>
        unsafe ScSessionStruct* session_;

        /// <summary>
        /// Indicates if this Response is internally constructed from Apps.
        /// </summary>
        Boolean isInternalResponse_;

        /// <summary>
        /// Underlying memory stream.
        /// </summary>
        MemoryStream memStream_;

        /// <summary>
        /// Dictionary of simple user custom headers.
        /// </summary>
        Dictionary<String, String> customHeaderFields_;

        /// <summary>
        /// String containing all headers.
        /// </summary>
        String headersString_;

        /// <summary>
        /// Dictionary with response streams.
        /// </summary>
        internal static ConcurrentDictionary<UInt64, Stream> ResponseStreams_ = new ConcurrentDictionary<UInt64, Stream>();

        /// <summary>
        /// Dictionary with response streams tasks.
        /// </summary>
        internal static ConcurrentDictionary<UInt64, Task> ResponseStreamsTasks_ = new ConcurrentDictionary<UInt64, Task>();

        /// <summary>
        /// Clones existing static resource response object.
        /// </summary>
        internal Response CloneStaticResourceResponse() {

            Response resp = new Response() {
                handlingStatus_ = handlingStatus_,
                statusCode_ = statusCode_,
				statusDescription_ = statusDescription_,
                uris_ = uris_,
                filePath_ = filePath_,
                fileDirectory_ = fileDirectory_,
                fileName_ = fileName_,
                fileExists_ = fileExists_,
                fileModified_ = fileModified_,
                responseBodySizeBytes_ = responseBodySizeBytes_,
                responseBodyOffset_ = responseBodyOffset_,
                responseBytes_ = responseBytes_,
                responseSizeBytes_ = responseSizeBytes_,
                customFields_ = customFields_,
                bodyString_ = bodyString_,
                bodyBytes_ = bodyBytes_,
                customHeaderFields_ = customHeaderFields_,
                headersString_ = headersString_
            };

            System.Diagnostics.Debug.Assert(null == memStream_);
            System.Diagnostics.Debug.Assert(IntPtr.Zero == responseStructIntPtr_);
            System.Diagnostics.Debug.Assert(IntPtr.Zero == socketDataIntPtr_);
            System.Diagnostics.Debug.Assert(null == wsHandshakeResp_);
            System.Diagnostics.Debug.Assert((null == cookies_) || (0 == cookies_.Count));
            System.Diagnostics.Debug.Assert(null == resource_);

            return resp;
        }

        /// <summary>
        /// Application by which the response was produced.
        /// </summary>
        public String AppName {
            get { return appName_; }
            set { appName_ = value; }
        }

        /// <summary>
        /// The URIs.
        /// </summary>
        internal List<string> Uris {
            get { return uris_; }
            set { uris_ = value; }
        }

        /// <summary>
        /// The file path
        /// </summary>
        public string FilePath {
            get { return filePath_; }
            internal set { filePath_ = value; }
        }

        /// <summary>
        /// The file directory
        /// </summary>
        public string FileDirectory {
            get { return fileDirectory_; }
            internal set { fileDirectory_ = value; }
        }

        /// <summary>
        /// The file name
        /// </summary>
        public string FileName {
            get { return fileName_; }
            internal set { fileName_ = value; }
        } 

        /// <summary>
        /// The file exists
        /// </summary>
        public bool FileExists {
            get { return fileExists_; }
            internal set { fileExists_ = value; }
        } 

        /// <summary>
        /// File modification date.
        /// </summary>
        public DateTime FileModified {
            get { return fileModified_; }
            internal set { fileModified_ = value; }
        } 

        /// <summary>
        /// Handling status for this response.
        /// </summary>
        internal HandlerStatusInternal HandlingStatus
        {
            get { return handlingStatus_; }
            set { handlingStatus_ = value; }
        }

        /// <summary>
        /// Special connection flags.
        /// </summary>
        public enum ConnectionFlags
        {
            NoSpecialFlags = 0,
            DisconnectAfterSend = MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND,
            DisconnectImmediately = MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_FLAGS_JUST_DISCONNECT,
            GracefullyCloseConnection = MixedCodeConstants.SOCKET_DATA_FLAGS.HTTP_WS_FLAGS_GRACEFULLY_CLOSE,
            StreamingResponseBody = MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_STREAMING_RESPONSE_BODY
        }

        /// <summary>
        /// Indicates if corresponding connection should be shut down.
        /// </summary>
        internal ConnectionFlags ConnFlags
        {
            get
            {
                return connectionFlags_;
            }

            set
            {
                customFields_ = true;
                connectionFlags_ = value;
            }
        }

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        public UInt16 StatusCode
        {
            get
            {
                if (0 == statusCode_) {

                    unsafe {

                        if (null != httpResponseStruct_)
                            statusCode_ = httpResponseStruct_->status_code_;
                    }
                }

                return statusCode_;
            }

            set
            {
                customFields_ = true;
                statusCode_ = value;
            }
        }

        /// <summary>
        /// Status description.
        /// </summary>
        public String StatusDescription
        {
            get
            {
                if (null == statusDescription_) {

                    unsafe {

                        if (null != httpResponseStruct_)
                            statusDescription_ = httpResponseStruct_->GetStatusDescription();
                    }
                }

                return statusDescription_;
            }

            set
            {
                customFields_ = true;
                statusDescription_ = value;
            }
        }

        /// <summary>
        /// Content type.
        /// </summary>
        public String ContentType
        {
            get
            {
                return this[HttpHeadersUtf8.ContentTypeHeader];
            }

            set
            {
                customFields_ = true;
                this[HttpHeadersUtf8.ContentTypeHeader] = value;
            }
        }

		/// <summary>
		/// Cache control header string.
		/// </summary>
		public String CacheControl
        {
			get
            {
                return this[HttpHeadersUtf8.CacheControlHeader];
            }

			set
            {
				customFields_ = true;
                this[HttpHeadersUtf8.CacheControlHeader] = value;
			}
		}

        /// <summary>
        /// Content encoding.
        /// </summary>
        public String ContentEncoding
        {
            get
            {
                return this[HttpHeadersUtf8.ContentEncodingHeader];
            }

            set
            {
                customFields_ = true;
                this[HttpHeadersUtf8.ContentEncodingHeader] = value;
            }
        }

        /// <summary>
        /// Streamed body.
        /// </summary>
        public Stream StreamedBody {

            get
            {
                return bodyStream_;
            }
            set
            {
                customFields_ = true;
                bodyStream_ = value;
            }
        }

        /// <summary>
        /// Body string.
        /// </summary>
        public String Body
        {
            get
            {
                if (null == bodyString_)
                {
                    if (null != bodyBytes_) {

                        bodyString_ = Encoding.UTF8.GetString(bodyBytes_);

                    } else if (resource_ != null) {

                        bodyString_ = resource_.AsMimeType(MimeType.Unspecified);

                    } else {

                        bodyString_ = GetBodyStringUtf8_Slow();
                    }
                }

                return bodyString_;
            }

            set
            {
                customFields_ = true;
                bodyString_ = value;
            }
        }

        /// <summary>
        /// Getting content by type.
        /// </summary>
        public T GetContent<T>() {

            if (typeof(T) == typeof(String)) {
                return (T)(object)GetContentString(MimeType.Unspecified);
            }

            if (typeof(T) == typeof(byte[])) {
                return (T)(object)GetContentBytes();
            }
            
            if (Content is T)
                return (T) Content;

            return default(T);
        }

        /// <summary>
        /// Gets content as a string.
        /// </summary>
        /// <returns></returns>
        public string GetContentString(MimeType mimeType) {

            if (null != resource_)
                return resource_.AsMimeType(mimeType);

            if (null != bodyString_)
                return bodyString_;

            if (null != bodyBytes_)
                return Encoding.UTF8.GetString(bodyBytes_);

            return GetBodyStringUtf8_Slow();
        }

        /// <summary>
        /// Gets content as bytes.
        /// </summary>
        /// <returns></returns>
        private byte[] GetContentBytes() {

            if (bodyBytes_ != null)
                return bodyBytes_;

            if (bodyString_ != null)
                return Encoding.UTF8.GetBytes(bodyString_);

            if (resource_ != null) {
                MimeType discard;
                return resource_.AsMimeType(MimeType.Unspecified, out discard);
            }

            if (responseBytes_ != null) {
                return ExtractBodyFromByteHttpResponse();
            }

            unsafe {
                if (null != httpResponseStruct_)
                    bodyBytes_ = httpResponseStruct_->GetBodyByteArray_Slow();
            }

            return bodyBytes_;
        }

        /// <summary>
        /// Should be made faster using pointers copying directly to the output buffer.
        /// </summary>
        /// <returns></returns>
        private byte[] ExtractBodyFromByteHttpResponse() {
            var bytes = new byte[this.responseBodySizeBytes_];
            Array.Copy(responseBytes_, responseBodyOffset_, bytes, 0, responseBodySizeBytes_);
            return bytes;
        }

        /// <summary>
        /// Body string.
        /// </summary>
        Object Content
        {
            get
            {
                if (null != resource_)
                    return resource_;

                if (null != bodyBytes_)
                    return bodyBytes_;

                if (null != bodyString_)
                    return bodyString_;

                return GetBodyStringUtf8_Slow();
            }

            set
            {
                if (value is String) {
                    bodyString_ = (String) value;
                } else if (value is Byte[]) {
                    bodyBytes_ = (Byte[]) value;
                } else if (value is IResource) {
                    resource_ = (IResource) value;
                } else {
                    throw new ArgumentException("Wrong content type assigned!");
                }

                customFields_ = true;
            }
        }

        /// <summary>
        /// Body bytes.
        /// </summary>
        public Byte[] BodyBytes
        {
            get
            {
                if (null == bodyBytes_) {

                    if (null != bodyString_) {

                        bodyBytes_ = Encoding.UTF8.GetBytes(bodyString_);

                    } else if (null != resource_) {

                        MimeType discard;
                        bodyBytes_ = resource_.AsMimeType(MimeType.Unspecified, out discard);

                    } else {

                        unsafe {
                            if (null != httpResponseStruct_)
                                bodyBytes_ = httpResponseStruct_->GetBodyByteArray_Slow();
                        }
                    }
                }

                return bodyBytes_;
            }

            set
            {
                customFields_ = true;
                bodyBytes_ = value;
            }
        }

        /// <summary>
        /// Resets all custom fields.
        /// </summary>
        internal void ResetAllCustomFields()
        {
            customFields_ = false;

            bodyStream_ = null;
            customHeaderFields_ = null;
            bodyString_ = null;
            statusDescription_ = null;
            statusCode_ = 0;
            resource_ = null;
        }

        /// <summary>
        /// The response can be constructed in one of the following ways:
        /// 
        /// 1) using a complete binary HTTP response
        /// in the Uncompressed or Compressed property (this includes the header)
        /// 
        /// 2) using a IResource object in the Resource property
        /// 
        /// 3) using the BodyString property (does not include the header)
        /// 
        /// 4) using the BodyBytes property (does not include the header)
        /// </summary>
        public IResource Resource {
            get {
                return resource_;
            }
            set {
                customFields_ = true;
                resource_ = value;
            }
        }

        /// <summary>
        /// List of Set-Cookie headers.
        /// Each string is in the form of "key=value; options..."
        /// </summary>
        public List<String> Cookies
        {
            get
            {
                if (cookies_ != null)
                    return cookies_;

                cookies_ = new List<String>();

                // Adding new cookies list from response.
                unsafe
                {
                    if (httpResponseStruct_ != null)
                    {
                        cookies_ = httpResponseStruct_->GetHeadersValues(HttpHeadersUtf8.SetCookieHeader, ref headersString_);
                    }
                }

                return cookies_;
            }

            set
            {
                customFields_ = true;
                cookies_ = value;
            }
        }

        /// <summary>
        /// Saved WebSocket handshake response.
        /// </summary>
        internal Byte[] WsHandshakeResp {
            get
            {
                return wsHandshakeResp_;
            }

            set
            {
                customFields_ = true;
                wsHandshakeResp_ = value;
            }
        }

		/// <summary>
		/// Constructs response buffer from fields that are set.
		/// </summary>
        internal void ConstructFromFields(Request req, Byte[] givenBuffer) {

            // Checking if we have a custom response.
            if (!customFields_)
                return;

            byte[] buf;
            Utf8Writer writer;

            byte[] bytes = bodyBytes_;

            if (resource_ != null) {

                Profiler.Current.Start(ProfilerNames.GetPreferredMimeType);

                MimeType mimetype = MimeType.Unspecified;
                if (req != null) {
                    mimetype = req.PreferredMimeType;
                }

                Profiler.Current.Stop(ProfilerNames.GetPreferredMimeType);

                try {

                    bytes = resource_.AsMimeType(mimetype, out mimetype);

                    // Checking if Content-Type header is set already.
                    if (null == ContentType) {
                        String mt = MimeTypeHelper.MimeTypeAsString(mimetype);

                        if (mt.StartsWith("text/")) {
                            ContentType = mt + ";charset=utf-8";
                        } else {
                            ContentType = mt;
                        }
                    }

                } catch (UnsupportedMimeTypeException) {

                    // MIME type is not strictly supported, below is a check for alternative MIME types.
                    bytes = null;
                }

                if (bytes == null) {

                    // The preferred requested mime type was not supported, try to see if there are
                    // other options.
                    IEnumerator<MimeType> secondaryChoices = null;
                    if (req != null) {
                        secondaryChoices = req.PreferredMimeTypes;
                    } else {
                        var l = new List<MimeType>();
                        l.Add(mimetype);
                        secondaryChoices = l.GetEnumerator();
                    }

                    secondaryChoices.MoveNext(); // The first one is already accounted for

                    while (bytes == null && secondaryChoices.MoveNext()) {
                        mimetype = secondaryChoices.Current;
                        bytes = resource_.AsMimeType(mimetype, out mimetype);
                    }

                    if (bytes == null) {
                        // None of the requested mime types were supported.
                        // We will have to respond with a "Not Acceptable" message.
                        statusCode_ = 406;
                        statusDescription_ = "Not acceptable";
                    } else {

                        // Checking if Content-Type header is set already.
                        if (null == ContentType) {
                            String mt = MimeTypeHelper.MimeTypeAsString(mimetype);

                            if (mt.StartsWith("text/")) {
                                ContentType = mt + ";charset=utf-8";
                            } else {
                                ContentType = mt;
                            }
                        }
                    }
                }
                // We have our precious bytes. Let's wrap them up in a response.
            }

            Int32 estimatedNumBytes = EstimateNeededSize(bytes);

            // Checking if we have a given buffer.
            if (givenBuffer != null) {

                if (estimatedNumBytes > givenBuffer.Length) {
                    buf = new Byte[estimatedNumBytes];
                } else {
                    buf = givenBuffer;
                }
            } else {

                buf = new Byte[estimatedNumBytes];
            }

            unsafe {
                fixed (byte* p = buf) {
                    writer = new Utf8Writer(p);

                    if (wsHandshakeResp_ == null) {

                        writer.Write(HttpHeadersUtf8.Http11);

                        UInt16 statusCode = statusCode_;
                        String statusDescription = statusDescription_;

                        if (statusCode > 0) {
                            writer.Write(statusCode);
                            writer.Write(' ');

                            // Checking if Status Description is set.
                            if (null != statusDescription)
                                writer.Write(statusDescription);
                            else
                                writer.Write("OK");

                            writer.Write(HttpHeadersUtf8.CRLF);
                        } else {
                            // Checking if Status Description is set.
                            if (null != statusDescription) {
                                writer.Write(200);
                                writer.Write(' ');
                                writer.Write(statusDescription);
                            } else {
                                writer.Write("200 OK");
                            }
                            writer.Write(HttpHeadersUtf8.CRLF);
                        }
                    } else {
                        writer.Write(wsHandshakeResp_);
                    }

                    writer.Write(HttpHeadersUtf8.ServerSc);

                    Boolean addSetCookie = true;
                    Boolean cacheControl = false;
                    if (null != customHeaderFields_) {

                        foreach (KeyValuePair<string, string> h in customHeaderFields_) {

                            if (h.Key == HttpHeadersUtf8.CacheControlHeader)
                                cacheControl = true;
                            else if (h.Key == HttpHeadersUtf8.SetCookieHeader)
                                addSetCookie = false;

                            writer.Write(h.Key);
                            writer.Write(": ");
                            writer.Write(h.Value);
                            writer.Write(HttpHeadersUtf8.CRLF);
                        }
                    }

                    Byte[] date = CurrentDateHeaderBytes;
                    if (date != null) {
                        writer.Write(date);
                    }

                    if (!cacheControl) {
                        writer.Write(HttpHeadersUtf8.CacheControlNoCache);
                    }

                    // Checking if session is defined.
                    ScSessionClass session = ScSessionClass.GetCurrent();
                    if (addSetCookie && (null != session) && ((req == null) || (!req.CameWithCorrectSession))) {

                        if (session.use_session_cookie_) {
                            writer.Write(HttpHeadersUtf8.SetSessionCookieStart);
                            writer.Write(session.ToAsciiString());
                        } else {
                            writer.Write(HttpHeadersUtf8.SetCookieLocationStart);
                            writer.Write(ScSessionClass.DataLocationUriPrefixEscaped);
                            writer.Write(session.ToAsciiString());
                        }
                        writer.Write(HttpHeadersUtf8.SetCookiePathEnd);
                    }

                    // Checking the cookies list.
                    if (null != cookies_) {
                        foreach (String c in cookies_) {
                            writer.Write(HttpHeadersUtf8.SetCookieStart);
                            writer.Write(c);
                            writer.Write(HttpHeadersUtf8.CRLF);
                        }
                    }

                    if (null != bodyString_) {

                        if (null != bytes || null != bodyStream_) {
                            throw new ArgumentException("Either body string, body bytes, body stream or resource can be set for Response.");
                        }

                        writer.Write(HttpHeadersUtf8.ContentLengthStart);
                        writer.Write(writer.GetByteCount(bodyString_));
                        writer.Write(HttpHeadersUtf8.CRLFCRLF);

                        writer.Write(bodyString_);

                    } else if (null != bytes) {

                        if (null != bodyStream_) {
                            throw new ArgumentException("Either body string, body bytes, body stream or resource can be set for Response.");
                        }

                        writer.Write(HttpHeadersUtf8.ContentLengthStart);
                        writer.Write(bytes.Length);
                        writer.Write(HttpHeadersUtf8.CRLFCRLF);
                        writer.Write(bytes);

                    } else if (null != bodyStream_) {

                        // NOTE: We are assuming that stream size can be fully determined.
                        // However we don't send the body immediately in this response.
                        // Body is streamed separately.
                        writer.Write(HttpHeadersUtf8.ContentLengthStart);
                        writer.Write(bodyStream_.Length);
                        writer.Write(HttpHeadersUtf8.CRLFCRLF);

                    } else {

                        // NOTE: When we do WebSocket upgrade by some reason we can't send "Content-Length: 0" header.
                        if (wsHandshakeResp_ == null) {

                            writer.Write(HttpHeadersUtf8.ContentLengthStart);
                            writer.Write('0');
                            writer.Write(HttpHeadersUtf8.CRLF);
                        }

                        writer.Write(HttpHeadersUtf8.CRLF);
                    }
                }
            }

            // Finally setting the response bytes.
            responseBytes_ = buf;
            responseSizeBytes_ = writer.Written;

            if (responseSizeBytes_ > estimatedNumBytes) {
                throw new ArgumentOutOfRangeException("Terrible situation: responseSizeBytes_ > estimatedNumBytes");
            }

            customFields_ = false;
        }

        /// <summary>
        /// Estimates the amount of bytes needed to represent this resource.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private int EstimateNeededSize(byte[] bytes) {

            // The sizes of the strings here is not accurate. We are mainly interested in making sure
            // that we will never have a buffer overrun so we take the length of the strings * 2.
            int size = HttpHeadersUtf8.TotalByteSize;

            if (wsHandshakeResp_ != null) {
                size += wsHandshakeResp_.Length;
            }

            String statusDescription = statusDescription_;

            if (statusDescription != null)
                size += statusDescription.Length;

            if (null != customHeaderFields_) {
                foreach (KeyValuePair<string, string> h in customHeaderFields_) {
                    size += (h.Key.Length + h.Value.Length + 4); // 4 for colon, space, CRLF
                }
            }

            if (null != ScSessionClass.GetCurrent()) {
                size += ScSessionClass.DataLocationUriPrefixEscaped.Length;
                size += ScSessionClass.GetCurrent().ToAsciiString().Length;
            }

            if (null != cookies_) {
                foreach (String c in cookies_) {
                    size += HttpHeadersUtf8.SetCookieStart.Length;
                    size += c.Length;
                    size += HttpHeadersUtf8.CRLF.Length;
                }
            }

            if (null != bodyString_) {
                size += (bodyString_.Length << 1); // Multiplying by 2 for possible UTF8.
            } else if (null != bytes) {
                size += bytes.Length;
            }

            return size;
        }

        /// <summary>
        /// Checks if status code is resembling success.
        /// </summary>
        public Boolean IsSuccessStatusCode
        {
            get
            {
                UInt16 statusCode = StatusCode;

                return (0 == statusCode) || ((statusCode >= 200) && (statusCode <= 226));
            }
        }

        /// <summary>
        /// The number of bytes of the content (i.e. the resource) of the uncompressed http response.
        /// </summary>
        /// <value>The length of the content.</value>
        public Int32 ContentLength
        {
            get
            {
                if (responseBodySizeBytes_ > 0)
                    return responseBodySizeBytes_;

                if (null != bodyBytes_)
                    return bodyBytes_.Length;

                if (null != bodyString_)
                    return bodyString_.Length;

                unsafe {

                    if (null != httpResponseStruct_)
                        return httpResponseStruct_->content_len_bytes_;
                }

                return 0;
            }
            set
            {
                responseBodySizeBytes_ = value;
            }
        }

        /// <summary>
        /// Response plain bytes.
        /// </summary>
        internal Byte[] ResponseBytes
        {
            get
            {
                if (null == responseBytes_) {

                    unsafe {

                        if (null != httpResponseStruct_)
                            responseBytes_ = httpResponseStruct_->GetResponseBytes_Slow();
                    }
                }

                return responseBytes_;
            }

            set
            {
                responseBytes_ = value;
				if (value != null)
					responseSizeBytes_ = value.Length;
				else
					responseSizeBytes_ = 0;
            }
        }

        /// <summary>
        /// Getting full response length.
        /// </summary>
        internal Int32 ResponseSizeBytes
        {
            get
            {
                if (responseSizeBytes_ <= 0) {

                    unsafe {

                        if (null != httpResponseStruct_) {
                            responseSizeBytes_ = (Int32) httpResponseStruct_->response_len_bytes_;
                        }
                    }
                }

                return responseSizeBytes_;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Response" /> class.
        /// </summary>
        public Response() {
            
        }

        /// <summary>
        /// Setting the response buffer.
        /// </summary>
        internal void SetResponseBuffer(Byte[] response_buf, MemoryStream mem_stream, Int32 response_len_bytes)
        {
            // Creating finalizer if needed.
            CreateFinalizer();

            responseBytes_ = response_buf;
			responseSizeBytes_ = response_len_bytes;

            memStream_ = mem_stream;

            unsafe
            {
                // Checking if have not allocated anything yet.
                if (IntPtr.Zero != socketDataIntPtr_) {

                    // Releasing internal resources here.
                    BitsAndBytes.Free(socketDataIntPtr_);
                    socketDataIntPtr_ = IntPtr.Zero;
                }

                // Setting the response data pointer.
                socketDataIntPtr_ = BitsAndBytes.Alloc(responseSizeBytes_);
                System.Diagnostics.Debug.Assert(null != httpResponseStruct_);
                httpResponseStruct_->socket_data_ = (Byte*) socketDataIntPtr_.ToPointer();

                // Copying HTTP response data.
                fixed (Byte* fixed_response_buf = response_buf) {
                    BitsAndBytes.MemCpy(httpResponseStruct_->socket_data_, fixed_response_buf, (UInt32)responseSizeBytes_);
                }
            }
        }

        /// <summary>
        /// Parses HTTP response from buffer.
        /// </summary>
        internal void TryParseResponseFromPlainBuffer(Byte[] buf, Int32 offsetBytes, Int32 bufLenBytes, Boolean complete)
        {
            UInt32 err_code;
            unsafe
            {
                // First destroying.
                Destroy();

                // Creating finalizer if needed.
                CreateFinalizer();

                // Indicating that we internally constructing Response.
                isInternalResponse_ = true;

                if (IntPtr.Zero != responseStructIntPtr_) {
                    BitsAndBytes.Free(responseStructIntPtr_);
                    responseStructIntPtr_ = IntPtr.Zero;
                }

                // Allocating space just for response structure.
                responseStructIntPtr_ = BitsAndBytes.Alloc(sizeof(HttpResponseInternal));
                httpResponseStruct_ = (HttpResponseInternal*) responseStructIntPtr_.ToPointer();
                httpResponseStruct_->socket_data_ = null;

                // Checking if we have a complete response.
                if (complete)
                {
                    // Setting the internal buffer.
                    SetResponseBuffer(buf, null, bufLenBytes);

                    // Executing HTTP response parser and getting Response structure as result.
                    err_code = sc_parse_http_response(httpResponseStruct_->socket_data_, (UInt32)bufLenBytes, (Byte*)httpResponseStruct_);
                }
                else
                {
                    fixed (Byte* pbuf = buf)
                    {
                        // Executing HTTP response parser and getting Response structure as result.
                        err_code = sc_parse_http_response(pbuf + offsetBytes, (UInt32)bufLenBytes, (Byte*)httpResponseStruct_);
                    }
                }

                // Checking if any error occurred.
                if (err_code != 0)
                {
                    // Freeing memory etc.
                    Destroy();

                    // Throwing the concrete error code exception.
                    throw ErrorCode.ToException(err_code);
                }

                responseBodySizeBytes_ = httpResponseStruct_->content_len_bytes_;
                responseBodyOffset_ = (int)httpResponseStruct_->content_offset_;

                // NOTE: No internal sessions support.
                session_ = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Response" /> class.
        /// </summary>
        public Response(Byte[] buf, Int32 offset, Int32 lenBytes, Boolean complete = true)
        {
            unsafe {

                // Parsing given buffer.
                TryParseResponseFromPlainBuffer(buf, offset, lenBytes, complete);
            }
        }

        /// <summary>
        /// Destroys the instance of Request.
        /// </summary>
        override internal void DestroyByFinalizer() {
            Destroy();
        }

        /// <summary>
        /// Destroys the instance of Response.
        /// </summary>
        internal void Destroy()
        {
            unsafe
            {
                // NOTE: Removing reference for finalizer so it does not call destroy again.
                UnLinkFinalizer();

                // Checking if already destroyed.
                if (httpResponseStruct_ == null)
                    return;

                // Closing the memory stream if any.
                if (null != memStream_)
                {
                    memStream_.Close();
                    memStream_ = null;
                }

                // Checking if we have constructed this Response
                // internally in Apps or externally in Gateway.
                if (isInternalResponse_)
                {
                    // Checking if have not allocated anything yet.
                    if (IntPtr.Zero != socketDataIntPtr_)
                    {
                        // Releasing response data.
                        BitsAndBytes.Free(socketDataIntPtr_);
                        socketDataIntPtr_ = IntPtr.Zero;
                    }

                    // Checking if response structure is allocated.
                    if (IntPtr.Zero != responseStructIntPtr_) {

                        // Releasing internal resources here.
                        BitsAndBytes.Free(responseStructIntPtr_);
                        responseStructIntPtr_ = IntPtr.Zero;
                    }
                }

                httpResponseStruct_ = null;
                session_ = null;
            }
        }

        /// <summary>
        /// Debugs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        public void Debug(string message, Exception ex = null)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Splits headers string into dictionary.
        /// </summary>
        internal static Dictionary<String, String> CreateHeadersDictionaryFromHeadersString(String headersString)
        {
            Dictionary<String, String> customHeaderFields = new Dictionary<String, String>();

            if (headersString == null)
                return customHeaderFields;

            // Splitting individual headers by CRLF.
            String[] headersAndValues = headersString.Split(new String[] { StarcounterConstants.NetworkConstants.CRLF }, StringSplitOptions.RemoveEmptyEntries);
            foreach (String headerAndValue in headersAndValues)
            {
                for (Int32 i = 0; i < headerAndValue.Length; i++)
                {
                    // Looking for colon to get headers value.
                    if (headerAndValue[i] == ':')
                    {
                        // Getting the header name.
                        String headerName = headerAndValue.Substring(0, i);

                        // Skipping preceding whitespace.
                        Int32 k = i + 1;
                        while ((k < headerAndValue.Length) && Char.IsWhiteSpace(headerAndValue[k])) {
                            k++;
                        }

                        // Checking if there is any space left.
                        if (k < headerAndValue.Length) {
                            customHeaderFields[headerName] = headerAndValue.Substring(k);
                        } else {
                            customHeaderFields[headerName] = "";
                        }

                        break;
                    }
                }
            }

            return customHeaderFields;
        }

        /// <summary>
        /// Headers string.
        /// </summary>
        public String Headers
        {
            get
            {
                unsafe
                {
                    // Concatenating headers from dictionary.
                    if ((null != customHeaderFields_) || (null != cookies_))
                    {
                        headersString_ = "";

                        // Adding each header.
                        foreach (KeyValuePair<string, string> h in customHeaderFields_)
                        {
                            headersString_ += h.Key + ": " + h.Value + StarcounterConstants.NetworkConstants.CRLF;
                        }

                        // Checking the cookies list.
                        if (null != cookies_)
                        {
                            foreach (String c in cookies_)
                            {
                                headersString_ += HttpHeadersUtf8.SetCookieStartString + c + StarcounterConstants.NetworkConstants.CRLF;
                            }
                        }

                        return headersString_;
                    }

                    if (null != httpResponseStruct_)
                        headersString_ = httpResponseStruct_->GetHeadersStringUtf8_Slow();

                    return headersString_;
                }
            }
        }

        /// <summary>
        /// Gets the content raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetBodyRaw(out IntPtr ptr, out Int32 sizeBytes)
        {
            unsafe
            {
                if (null == httpResponseStruct_)
                    throw new ArgumentException("HTTP response not initialized.");

                httpResponseStruct_->GetBodyRaw(out ptr, out sizeBytes);
            }
        }

        /// <summary>
        /// Gets body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        String GetBodyStringUtf8_Slow()
        {
            unsafe
            {
                if (null == httpResponseStruct_)
                    return null;

                return httpResponseStruct_->GetBodyStringUtf8_Slow();
            }
        }

        /// <summary>
        /// Gets the raw headers.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe
            {
                if (null == httpResponseStruct_)
                    throw new ArgumentException("HTTP response not initialized.");

                httpResponseStruct_->GetRawHeaders(out ptr, out sizeBytes);
            }
        }

        /// <summary>
        /// Gets the raw header.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeader(byte[] key, out IntPtr ptr, out UInt32 sizeBytes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Setting new headers dictionary for response.
        /// </summary>
        public void SetHeadersDictionary(Dictionary<String, String> headersDict) {
            customHeaderFields_ = headersDict;
            customFields_ = true;
        }

        /// <summary>
        /// Gets the <see cref="String" /> with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>String.</returns>
        public String this[String name]
        {
            get
            {
                if (null != customHeaderFields_)
                {
                    if (customHeaderFields_.ContainsKey(name))
                        return customHeaderFields_[name];

                    return null;
                }

                unsafe
                {
                    if (null == httpResponseStruct_)
                        return null;

                    return httpResponseStruct_->GetHeaderValue(name, ref headersString_);
                }
            }

            set
            {
                customFields_ = true;

                if (null == customHeaderFields_)
                {
                    String headers = headersString_;
                    if (headers == null)
                    {
                        unsafe
                        {
                            if (null != httpResponseStruct_)
                                headers = httpResponseStruct_->GetHeadersStringUtf8_Slow();
                        }
                    }

                    customHeaderFields_ = CreateHeadersDictionaryFromHeadersString(headers);
                }

                customHeaderFields_[name] = value;
            }
        }

        /// <summary>
        /// Checks if HTTP response already has session.
        /// </summary>
        public Boolean HasSession
        {
            get
            {
                unsafe
                {
                    return Request.INVALID_APPS_UNIQUE_SESSION_INDEX != (session_->linearIndex_);
                }
            }
        }

        /// <summary>
        /// Returns unique session number.
        /// </summary>
        public UInt64 UniqueSessionIndex
        {
            get
            {
                unsafe
                {
                    return session_->linearIndex_;
                }
            }
        }

        /// <summary>
        /// Returns session salt.
        /// </summary>
        public UInt64 SessionSalt
        {
            get
            {
                unsafe
                {
                    return session_->randomSalt_;
                }
            }
        }

        /// <summary>
        /// Gets the session struct.
        /// </summary>
        /// <value>The session struct.</value>
        public ScSessionStruct SessionStruct
        {
            get
            {
                unsafe { return *session_; }
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override String ToString()
        {
            unsafe
            {
                if (Body != null)
                    return Body;

                return null;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HttpResponseInternal
    {
        internal UInt32 response_len_bytes_;
        internal Int32 content_len_bytes_;

        internal UInt16 response_offset_;
        internal UInt16 content_offset_;
        internal UInt16 headers_offset_;
        internal UInt16 headers_len_bytes_;
        internal UInt16 session_string_offset_;
        internal UInt16 status_code_;

        internal Byte session_string_len_bytes_;

        // TODO: Should be changed!
        // Socket data pointer.
        public unsafe Byte* socket_data_;

        /// <summary>
        /// Get status description.
        /// </summary>
        public String GetStatusDescription()
        {
            SByte* cur = (SByte*) socket_data_ + response_offset_;
            cur += 12; // Skipping "HTTP/1.1 XXX"

            // Skipping until first space after StatusCode.
            while (*cur != (Byte)' ') cur++;
            cur++;
            SByte* status_descr_start = cur;

            // Skipping until first character return.
            while (*cur != (Byte)'\r') cur++;

            // Calculating length of the status string.
            Int32 len = (Int32)(cur - status_descr_start);

            return new String(status_descr_start, 0, len, Encoding.ASCII);
        }

        /// <summary>
        /// Gets the raw response.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetResponseRaw(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(socket_data_ + response_offset_);

            sizeBytes = response_len_bytes_;
        }

        /// <summary>
        /// Gets the response as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        internal String GetResponseStringUtf8_Slow()
        {
            return new String((SByte*)(socket_data_ + response_offset_), 0, (Int32)response_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the response as bytes.
        /// </summary>
        /// <returns>Byte array.</returns>
        internal Byte[] GetResponseBytes_Slow()
        {
            Byte[] respBytes = new Byte[response_len_bytes_];
            Marshal.Copy(new IntPtr(socket_data_ + response_offset_), respBytes, 0, (int)response_len_bytes_);
            return respBytes;
        }

        /// <summary>
        /// Gets the raw parameters structure.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public IntPtr GetRawParametersInfo()
        {
            return new IntPtr(socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_PARAMS_INFO);
        }

        /// <summary>
        /// Gets the content raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetBodyRaw(out IntPtr ptr, out Int32 sizeBytes)
        {
            if (content_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + content_offset_);

            sizeBytes = content_len_bytes_;
        }

        /// <summary>
        /// Gets the content as byte array.
        /// </summary>
        /// <returns>Content bytes.</returns>
        internal Byte[] GetBodyByteArray_Slow()
        {
            // Checking if there is a content.
            if (content_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] content_bytes = new Byte[content_len_bytes_];
            Marshal.Copy(new IntPtr(socket_data_ + content_offset_), content_bytes, 0, content_len_bytes_);

            return content_bytes;
        }

        /// <summary>
        /// Gets body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        internal String GetBodyStringUtf8_Slow()
        {
            // Checking if there is a content.
            if (content_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + content_offset_), 0, content_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the raw headers.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes)
        {
            if (headers_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + headers_offset_);

            sizeBytes = headers_len_bytes_;
        }

        /// <summary>
        /// Gets headers as ASCII string.
        /// </summary>
        /// <returns>ASCII string.</returns>
        internal String GetHeadersStringUtf8_Slow()
        {
            if (headers_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + headers_offset_), 0, (Int32)headers_len_bytes_, Encoding.ASCII);
        }

        /// <summary>
        /// Gets the raw headers length.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public UInt32 GetHeadersLength()
        {
            return headers_len_bytes_;
        }

        /// <summary>
        /// Gets the raw session string.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes)
        {
            if (session_string_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + session_string_offset_);

            sizeBytes = session_string_len_bytes_;
        }

        /// <summary>
        /// Gets the session string.
        /// </summary>
        /// <returns>String.</returns>
        public String GetSessionString()
        {
            // Checking if there is any session.
            if (session_string_len_bytes_ <= 0)
                return null;

            IntPtr raw_session_string;
            UInt32 len_bytes;
            GetRawSessionString(out raw_session_string, out len_bytes);

            return Marshal.PtrToStringAnsi(raw_session_string, (Int32)len_bytes);
        }

        /// <summary>
        /// Gets the header value.
        /// </summary>
        /// <param name="headerName">Name of the header.</param>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetHeaderValue(byte[] headerName, out IntPtr ptr, out UInt32 sizeBytes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the header value.
        /// </summary>
        /// <param name="headerName">Name of the header.</param>
        /// <param name="headersString">Reference of the header string.</param>
        /// <returns>String.</returns>
        public List<String> GetHeadersValues(String headerName, ref String headersString)
        {
            // Constructing the string if its the first time.
            if (headersString == null)
                headersString = Marshal.PtrToStringAnsi(new IntPtr(socket_data_ + headers_offset_), (Int32)headers_len_bytes_);

            List<String> headerValues = new List<String>();
            Int32 hend = 0;

            while (true)
            {
                // Getting needed substring.
                Int32 hstart = headersString.IndexOf(headerName, hend, StringComparison.InvariantCultureIgnoreCase); 
                if (hstart < 0)
                    break;

                // Skipping header name and colon.
                hstart += headerName.Length + 1;

                // Skipping header name.
                while (headersString[hstart] == ' ' || headersString[hstart] == ':')
                    hstart++;

                // Going until end of line.
                hend = headersString.IndexOf(StarcounterConstants.NetworkConstants.CRLF, hstart, StringComparison.InvariantCultureIgnoreCase);
                if (hend <= 0)
                    throw new ArgumentException("HTTP header is corrupted!");

                headerValues.Add(headersString.Substring(hstart, hend - hstart));

                hend += 2; // Skipping \r\n
            }

            return headerValues;
        }

        /// <summary>
        /// Gets the header value.
        /// </summary>
        /// <param name="headerName">Name of the header.</param>
        /// <param name="headersString">Reference of the header string.</param>
        /// <returns>String.</returns>
        public String GetHeaderValue(String headerName, ref String headersString)
        {
            // Constructing the string if its the first time.
            if (headersString == null)
                headersString = Marshal.PtrToStringAnsi(new IntPtr(socket_data_ + headers_offset_), (Int32)headers_len_bytes_);

            // Getting needed substring.
            Int32 hstart = headersString.IndexOf(headerName, StringComparison.InvariantCultureIgnoreCase);
            if (hstart < 0)
                return null;

            // Skipping header name and colon.
            hstart += headerName.Length + 1;

            // Skipping header name.
            while (headersString[hstart] == ' ' || headersString[hstart] == ':')
                hstart++;

            // Going until end of line.
            Int32 hend = headersString.IndexOf(StarcounterConstants.NetworkConstants.CRLF, hstart, StringComparison.InvariantCultureIgnoreCase);
            if (hend <= 0)
                throw new ArgumentException("HTTP header is corrupted!");

            return headersString.Substring(hstart, hend - hstart);
        }
    }
}
