﻿
using HttpStructs;
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;
using System.Text;
namespace Starcounter.Advanced {
    /// <summary>
    /// Class HttpRequest
    /// </summary>
    public class HttpRequest {


        /// <summary>
        /// Creates a minimalistic Http 1.0 GET request with the given uri without any headers or even protocol version specifier.
        /// </summary>
        /// <remarks>
        /// Calling RawGET("/test") will return the Http request "GET /test" in Ascii/UTF8 encoding.
        /// </remarks>
        /// <param name="uri">The URI.</param>
        /// <returns>System.Object.</returns>
        public static byte[] RawGET(string uri) {
            var length = uri.Length + 3 + 1 + 4;// GET + space + URI + CRLFCRLF
            byte[] vu = new byte[length];
            vu[0] = (byte)'G';
            vu[1] = (byte)'E';
            vu[2] = (byte)'T';
            vu[3] = (byte)' ';
            vu[length - 4] = (byte)'\r';
            vu[length - 3] = (byte)'\n';
            vu[length - 2] = (byte)'\r';
            vu[length - 1] = (byte)'\n';
            Encoding.ASCII.GetBytes(uri, 0, uri.Length, vu, 4);
            return vu;
        }

        public static HttpRequest GET(string uri) {
            return new HttpRequest(RawGET(uri));
        }

        /// <summary>
        /// Offset in bytes for the session.
        /// </summary>
        const Int32 SESSION_OFFSET_BYTES = 32;

        /// <summary>
        /// Internal structure with HTTP request information.
        /// </summary>
        unsafe HttpRequestInternal* http_request_struct_;

        /// <summary>
        /// Direct pointer to session data.
        /// </summary>
        unsafe ScSessionStruct* session_;

        /// <summary>
        /// Internal network data stream.
        /// </summary>
        public INetworkDataStream data_stream_;

        /// <summary>
        /// Indicates if this HttpRequest is internally constructed from Apps.
        /// </summary>
        Boolean isInternalRequest = false;

        /// <summary>
        /// Parses internal HTTP request.
        /// </summary>
        [DllImport("HttpParser.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_parse_http_request(
            Byte* request_buf,
            UInt32 request_size_bytes,
            Byte* out_http_request);

        /// <summary>
        /// Initializes the Apps HTTP parser.
        /// </summary>
        [DllImport("HttpParser.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 sc_init_http_parser();

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequest" /> class.
        /// </summary>
        /// <param name="buf">The buf.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public HttpRequest(Byte[] buf) {
            unsafe {
                // Allocating space for HttpRequest contents and structure.
                Byte* request_native_buf = (Byte*)BitsAndBytes.Alloc(buf.Length + sizeof(HttpRequestInternal));
                fixed (Byte* fixed_buf = buf) {
                    // Copying HTTP request data.
                    BitsAndBytes.MemCpy(request_native_buf, fixed_buf, (UInt32)buf.Length);
                }

                // Pointing to HTTP request structure.
                http_request_struct_ = (HttpRequestInternal*)(request_native_buf + buf.Length);

                // Setting the request data pointer.
                http_request_struct_->socket_data_ = request_native_buf;

                // Indicating that we internally constructing HttpRequest.
                isInternalRequest = true;

                // NOTE: No internal sessions support.
                session_ = null;

                // NOTE: No internal data stream support:
                // Simply on which socket to send this "request"?

                // Executing HTTP request parser and getting HttpRequest structure as result.
                UInt32 err_code = sc_parse_http_request(request_native_buf, (UInt32)buf.Length, (Byte*)http_request_struct_);

                // Checking if any error occurred.
                if (err_code != 0) {
                    // Freeing memory etc.
                    Destroy();

                    throw ErrorCode.ToException(err_code);
                }
            }
        }

        /// <summary>
        /// Destroys the instance of HttpRequest.
        /// </summary>
        public void Destroy() {
            unsafe {
                // Checking if already destroyed.
                if (http_request_struct_ == null)
                    return;

                // Checking if we have constructed this HttpRequest
                // internally in Apps or externally in Gateway.
                if (isInternalRequest) {
                    // Releasing internal resources here.
                    BitsAndBytes.Free((IntPtr)http_request_struct_->socket_data_);
                }
                else {
                    // Releasing data stream resources like chunks, etc.
                    data_stream_.Destroy();
                }

                http_request_struct_ = null;
                session_ = null;
            }
        }

        /// <summary>
        /// Checks if HttpStructs is destroyed already.
        /// </summary>
        /// <returns>True if destroyed.</returns>
        public bool IsDestroyed() {
            unsafe {
                return (http_request_struct_ == null) && (session_ == null);
            }
        }

        /// <summary>
        /// Called when GC destroys this object.
        /// </summary>
        ~HttpRequest() {
            // TODO: Consult what is better for Apps auto-destructor or manual call to Destroy.
            Destroy();
        }

        // Constructor.
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequest" /> class.
        /// </summary>
        /// <param name="chunk_data">The chunk_data.</param>
        /// <param name="single_chunk">The single_chunk.</param>
        /// <param name="chunk_index">The chunk_index.</param>
        /// <param name="http_request_begin">The http_request_begin.</param>
        /// <param name="socket_data">The socket_data.</param>
        /// <param name="data_stream">The data_stream.</param>
        public unsafe HttpRequest(
            Byte* chunk_data,
            Boolean single_chunk,
            UInt32 chunk_index,
            Byte* http_request_begin,
            Byte* socket_data,
            INetworkDataStream data_stream) {
            http_request_struct_ = (HttpRequestInternal*)http_request_begin;
            session_ = (ScSessionStruct*)(socket_data + SESSION_OFFSET_BYTES);
            http_request_struct_->socket_data_ = socket_data;
            data_stream_ = data_stream;
            data_stream_.Init(chunk_data, single_chunk, chunk_index);
        }

        // TODO
        /// <summary>
        /// Debugs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        public void Debug(string message, Exception ex = null) {
            Console.WriteLine(message);
        }

        // TODO
        /// <summary>
        /// The needs script injection_
        /// </summary>
        bool needsScriptInjection_ = false;
        /// <summary>
        /// Gets or sets a value indicating whether [needs script injection].
        /// </summary>
        /// <value><c>true</c> if [needs script injection]; otherwise, <c>false</c>.</value>
        public bool NeedsScriptInjection {
            get { return needsScriptInjection_; }
            set { needsScriptInjection_ = value; }
        }

        // TODO
        /// <summary>
        /// The is app view_
        /// </summary>
        bool isAppView_ = false;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is app view.
        /// </summary>
        /// <value><c>true</c> if this instance is app view; otherwise, <c>false</c>.</value>
        public bool IsAppView {
            get { return isAppView_; }
            set { isAppView_ = value; }
        }

        // TODO
        /// <summary>
        /// The gzip advisable_
        /// </summary>
        bool gzipAdvisable_ = false;
        /// <summary>
        /// Gets or sets the gzip advisable.
        /// </summary>
        /// <value>The gzip advisable.</value>
        public Boolean GzipAdvisable {
            get { return gzipAdvisable_; }
            set { gzipAdvisable_ = value; }
        }

        // TODO
        /// <summary>
        /// Gets or sets the view model.
        /// </summary>
        /// <value>The view model.</value>
        public byte[] ViewModel { get; set; }
        /// <summary>
        /// Gets a value indicating whether this instance can use static response.
        /// </summary>
        /// <value><c>true</c> if this instance can use static response; otherwise, <c>false</c>.</value>
        public bool CanUseStaticResponse {
            get {
                return ViewModel == null;
            }
        }

        /// <summary>
        /// Gets the body raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetBodyRaw(out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetBodyRaw(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the body as byte array.
        /// </summary>
        /// <returns>Body bytes.</returns>
        public Byte[] GetBodyByteArray_Slow() {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            unsafe { return http_request_struct_->GetBodyByteArray_Slow(); }
        }

        /// <summary>
        /// Gets the body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetBodyStringUtf8_Slow() {
            unsafe { return http_request_struct_->GetBodyStringUtf8_Slow(); }
        }

        /// <summary>
        /// Gets the length of the body in bytes.
        /// </summary>
        /// <value>The length of the body.</value>
        public UInt32 BodyLength {
            get {
                unsafe { return http_request_struct_->body_len_bytes_; }
            }
        }

        /// <summary>
        /// Writes the response.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void SendResponse(Byte[] buffer, Int32 offset, Int32 length) {
            unsafe { data_stream_.SendResponse(buffer, offset, length); }
        }

        /// <summary>
        /// Gets the raw request.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRequestRaw(out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetRequestRaw(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawMethodAndUri(out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetRawMethodAndUri(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw method and URI plus extra character.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawMethodAndUriPlusAnExtraCharacter(out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetRawMethodAndUri(out ptr, out sizeBytes); }
            sizeBytes += 1;
        }

        /// <summary>
        /// Gets the raw headers.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetRawHeaders(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw cookies.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetRawCookies(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw accept.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawAccept(out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetRawAccept(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw session string.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetRawSessionString(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw header.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeader(byte[] key, out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe { http_request_struct_->GetHeaderValue(key, out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the <see cref="String" /> with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>String.</returns>
        public String this[String name] {
            get {
                unsafe { return http_request_struct_->GetHeaderValue(name); }
            }
        }

        /// <summary>
        /// Invalid session index.
        /// </summary>
        public const UInt32 INVALID_GW_SESSION_INDEX = UInt32.MaxValue;

        /// <summary>
        /// Invalid Apps session unique number.
        /// </summary>
        public const UInt64 INVALID_APPS_UNIQUE_SESSION_NUMBER = UInt64.MaxValue;

        /// <summary>
        /// Invalid Apps session salt.
        /// </summary>
        public const UInt64 INVALID_APPS_SESSION_SALT = 0;

        /// <summary>
        /// Checks if HTTP request already has session.
        /// </summary>
        public Boolean HasSession {
            get {
                unsafe {
                    return INVALID_APPS_UNIQUE_SESSION_NUMBER != (session_->apps_unique_session_index_);
                }
            }
        }

        /// <summary>
        /// Gets certain Apps session.
        /// </summary>
        public IAppsSession AppsSessionInterface {
            get {
                unsafe {
                    return GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(
                        session_->scheduler_id_,
                        session_->apps_unique_session_index_);
                }
            }
        }

        // New session creation indicator.
        Boolean newSession_ = false;

        /// <summary>
        /// Indicates if new session was created.
        /// </summary>
        public Boolean HasNewSession {
            get {
                return newSession_;
            }
        }

        /// <summary>
        /// Generates session number and writes it to response.
        /// </summary>
        public UInt32 GenerateNewSession(IAppsSession apps_session) {
            unsafe {
                // Indicating that new session was created.
                newSession_ = true;

                // Resetting the session index.
                session_->gw_session_index_ = INVALID_GW_SESSION_INDEX;

                // Simply generating new session.
                return GlobalSessions.AllGlobalSessions.CreateNewSession(
                    apps_session,
                    session_->scheduler_id_,
                    ref session_->apps_unique_session_index_,
                    ref session_->apps_session_salt_);
            }
        }

        /// <summary>
        /// Kills existing session.
        /// </summary>
        public UInt32 DestroySession() {
            UInt32 err_code;

            unsafe {
                // Indicating that session was destroyed.
                newSession_ = false;

                // Simply generating new session.
                err_code = GlobalSessions.AllGlobalSessions.DestroySession(
                    session_->apps_unique_session_index_,
                    session_->apps_session_salt_,
                    session_->scheduler_id_);

                // Killing this session by setting invalid unique number and salt.
                session_->apps_unique_session_index_ = INVALID_APPS_UNIQUE_SESSION_NUMBER;
                session_->apps_session_salt_ = INVALID_APPS_SESSION_SALT;
            }

            return err_code;
        }

        /// <summary>
        /// Returns unique session number.
        /// </summary>
        public UInt64 UniqueSessionIndex {
            get {
                unsafe {
                    return session_->apps_unique_session_index_;
                }
            }
        }

        /// <summary>
        /// Returns session salt.
        /// </summary>
        public UInt64 SessionSalt {
            get {
                unsafe {
                    return session_->apps_session_salt_;
                }
            }
        }

        /// <summary>
        /// Gets the session struct.
        /// </summary>
        /// <value>The session struct.</value>
        public ScSessionStruct SessionStruct {
            get {
                unsafe { return *session_; }
            }
        }

        /// <summary>
        /// Gets the HTTP method.
        /// </summary>
        /// <value>The HTTP method.</value>
        public HTTP_METHODS HttpMethod {
            get {
                unsafe { return http_request_struct_->http_method_; }
            }
        }

        /// <summary>
        /// Gets the is gzip accepted.
        /// </summary>
        /// <value>The is gzip accepted.</value>
        public Boolean IsGzipAccepted {
            get {
                unsafe { return http_request_struct_->is_gzip_accepted_; }
            }
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <value>The URI.</value>
        public String Uri {
            get {
                unsafe { return http_request_struct_->Uri; }
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override String ToString() {
            unsafe { return http_request_struct_->ToString(); }
        }
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 8)]
    /// <summary>
    /// Struct HttpRequestInternal
    /// </summary>
    public unsafe struct HttpRequestInternal {
        /// <summary>
        /// The MA x_ HTT p_ HEADERS
        /// </summary>
        public const Int32 MAX_HTTP_HEADERS = 16;

        /// <summary>
        /// Request offset.
        /// </summary>
        public UInt32 request_offset_;

        /// <summary>
        /// The request_len_bytes_
        /// </summary>
        public UInt32 request_len_bytes_;

        /// <summary>
        /// Body offset.
        /// </summary>
        public UInt32 body_offset_;

        /// <summary>
        /// The body_len_bytes_
        /// </summary>
        public UInt32 body_len_bytes_;

        /// <summary>
        /// Resource URI offset.
        /// </summary>
        public UInt32 uri_offset_;

        /// <summary>
        /// The uri_len_bytes_
        /// </summary>
        public UInt32 uri_len_bytes_;

        /// <summary>
        /// Key-value header offset.
        /// </summary>
        public UInt32 headers_offset_;

        /// <summary>
        /// The headers_len_bytes_
        /// </summary>
        public UInt32 headers_len_bytes_;

        /// <summary>
        /// Cookie value offset.
        /// </summary>
        public UInt32 cookies_offset_;

        /// <summary>
        /// The cookies_len_bytes_
        /// </summary>
        public UInt32 cookies_len_bytes_;

        /// <summary>
        /// Accept value offset.
        /// </summary>
        public UInt32 accept_value_offset_;

        /// <summary>
        /// The accept_value_len_bytes_
        /// </summary>
        public UInt32 accept_value_len_bytes_;

        /// <summary>
        /// Session ID string offset.
        /// </summary>
        public UInt32 session_string_offset_;

        /// <summary>
        /// The session_string_len_bytes_
        /// </summary>
        public UInt32 session_string_len_bytes_;

        /// <summary>
        /// Header offsets.
        /// </summary>
        public fixed UInt32 header_offsets_[MAX_HTTP_HEADERS];

        /// <summary>
        /// The header_len_bytes_
        /// </summary>
        public fixed UInt32 header_len_bytes_[MAX_HTTP_HEADERS];

        /// <summary>
        /// The header_value_offsets_
        /// </summary>
        public fixed UInt32 header_value_offsets_[MAX_HTTP_HEADERS];

        /// <summary>
        /// The header_value_len_bytes_
        /// </summary>
        public fixed UInt32 header_value_len_bytes_[MAX_HTTP_HEADERS];

        /// <summary>
        /// The num_headers_
        /// </summary>
        public UInt32 num_headers_;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public HTTP_METHODS http_method_;

        /// <summary>
        /// Is Gzip accepted.
        /// </summary>
        public bool is_gzip_accepted_;

        /// <summary>
        /// Socket data pointer.
        /// </summary>
        public unsafe Byte* socket_data_;

        /// <summary>
        /// Gets the raw request.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRequestRaw(out IntPtr ptr, out UInt32 sizeBytes) {
            ptr = new IntPtr(socket_data_ + request_offset_);

            sizeBytes = request_len_bytes_;
        }

        /// <summary>
        /// Gets the body raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetBodyRaw(out IntPtr ptr, out UInt32 sizeBytes) {
            if (body_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + body_offset_);

            sizeBytes = body_len_bytes_;
        }

        /// <summary>
        /// Gets the body as byte array.
        /// </summary>
        /// <returns>Body bytes.</returns>
        public Byte[] GetBodyByteArray_Slow() {
            // Checking if there is a body.
            if (body_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] body_bytes = new Byte[(Int32)body_len_bytes_];
            Marshal.Copy((IntPtr)(socket_data_ + body_offset_), body_bytes, 0, (Int32)body_len_bytes_);

            return body_bytes;
        }

        /// <summary>
        /// Gets the body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetBodyStringUtf8_Slow() {
            // Checking if there is a body.
            if (body_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + body_offset_), 0, (Int32)body_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawMethodAndUri(out IntPtr ptr, out UInt32 sizeBytes) {
            // NOTE: Method and URI must always exist.

            ptr = new IntPtr(socket_data_ + request_offset_);
            sizeBytes = uri_offset_ - request_offset_ + uri_len_bytes_;
        }

        /// <summary>
        /// Gets the raw headers.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes) {
            if (headers_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + headers_offset_);

            sizeBytes = headers_len_bytes_;
        }

        /// <summary>
        /// Gets the raw cookies.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes) {
            if (cookies_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + cookies_offset_);

            sizeBytes = cookies_len_bytes_;
        }

        /// <summary>
        /// Gets the raw accept.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawAccept(out IntPtr ptr, out UInt32 sizeBytes) {
            if (accept_value_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + accept_value_offset_);

            sizeBytes = accept_value_len_bytes_;
        }

        /// <summary>
        /// Gets the raw session string.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes) {
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
        public String GetSessionString() {
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
        public void GetHeaderValue(byte[] headerName, out IntPtr ptr, out UInt32 sizeBytes) {
            unsafe {
                fixed (UInt32* header_offsets = header_offsets_,
                    header_len_bytes = header_len_bytes_,
                    header_value_offsets = header_value_offsets_,
                    header_value_len_bytes = header_value_len_bytes_) {
                    // Going through all headers.
                    for (Int32 i = 0; i < num_headers_; i++) {
                        Boolean found = true;

                        // Checking that length is correct.
                        if (headerName.Length == header_len_bytes[i]) {
                            // Going through all characters in current header.
                            for (Int32 k = 0; k < headerName.Length; k++) {
                                // Comparing each character.
                                if (((Byte)headerName[k]) != *(socket_data_ + header_offsets[i] + k)) {
                                    found = false;
                                    break;
                                }
                            }

                            if (found) {
                                ptr = (IntPtr)(socket_data_ + header_value_offsets[i]);
                                sizeBytes = header_value_len_bytes[i];

                                return;
                            }
                        }
                    }
                }
            }

            // In case if header is not found.
            ptr = IntPtr.Zero;
            sizeBytes = 0;
        }

        /// <summary>
        /// Gets the header value.
        /// </summary>
        /// <param name="headerName">Name of the header.</param>
        /// <returns>String.</returns>
        public String GetHeaderValue(String headerName) {
            unsafe {
                fixed (UInt32* header_offsets = header_offsets_,
                    header_len_bytes = header_len_bytes_,
                    header_value_offsets = header_value_offsets_,
                    header_value_len_bytes = header_value_len_bytes_) {
                    // Going through all headers.
                    for (Int32 i = 0; i < num_headers_; i++) {
                        Boolean found = true;

                        // Checking that length is correct.
                        if (headerName.Length == header_len_bytes[i]) {
                            // Going through all characters in current header.
                            for (Int32 k = 0; k < headerName.Length; k++) {
                                // Comparing each character.
                                if (((Byte)headerName[k]) != *(socket_data_ + header_offsets[i] + k)) {
                                    found = false;
                                    break;
                                }
                            }

                            if (found) {
                                // Skipping two bytes for colon and one space.
                                return Marshal.PtrToStringAnsi((IntPtr)(socket_data_ + header_value_offsets[i]), (Int32)header_value_len_bytes[i]);
                            }
                        }
                    }
                }
            }

            return null;

            /*
            // Constructing the string if its the first time.
            String headers_and_values = Marshal.PtrToStringAnsi((IntPtr)(sd_ + headers_offset_), (Int32)headers_len_bytes_);

            // Getting needed substring.
            Int32 index = headers_and_values.IndexOf(name);
            if (index < 0)
                return null;

            // Going until end of line.
            Int32 k = index + name.Length;
            while ((headers_and_values[k] != '\r') && (k < (headers_and_values.Length - 1)))
                k++;

            return headers_and_values.Substring(index + name.Length, k - index - name.Length);
            */
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <value>The URI.</value>
        public String Uri {
            get {
                if (uri_len_bytes_ > 0)
                    return Marshal.PtrToStringAnsi((IntPtr)(socket_data_ + uri_offset_), (Int32)uri_len_bytes_);

                return null;
            }
        }

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>A <see cref="T:System.String" /> containing a fully qualified type name.</returns>
        public override String ToString() {
            return "<h1>HttpMethod: " + http_method_ + "</h1>\r\n" +
                   "<h1>URI: " + Uri + "</h1>\r\n" +
                   "<h1>GZip accepted: " + is_gzip_accepted_ + "</h1>\r\n" +
                   "<h1>Host: " + GetHeaderValue("Host") + "</h1>\r\n" +
                   "<h1>Session string: " + GetSessionString() + "</h1>\r\n" +
                   "<h1>BodyLength: " + body_len_bytes_ + "</h1>\r\n" +
                   "<h1>Body: " + GetBodyStringUtf8_Slow() + "</h1>\r\n"
                   ;
        }
    }
}
