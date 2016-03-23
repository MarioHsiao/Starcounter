
using Starcounter.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter {

    /// <summary>
    /// Class Request
    /// </summary>
    public sealed class Request : Finalizing
    {
        /// <summary>
        /// Parses internal HTTP request.
        /// </summary>
        [DllImport("schttpparser.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_parse_http_request(
            Byte* request_buf,
            UInt32 request_size_bytes,
            Byte* out_http_request);

        /// <summary>
        /// Initializes the Apps HTTP parser.
        /// </summary>
        [DllImport("schttpparser.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 sc_init_http_parser();

        /// <summary>
        /// Request constructor.
        /// </summary>
        public Request() {

            Headers = new HeadersAccessor(this);
            HandlerOpts = new HandlerOptions();
        }

        /// <summary>
        /// Is single chunk?
        /// </summary>
        Boolean isSingleChunk_;

        /// <summary>
        /// Socket data buffer.
        /// </summary>
        IntPtr socketDataIntPtr_;

        /// <summary>
        /// Internal structure with HTTP request information.
        /// </summary>
        unsafe HttpRequestInternal* http_request_struct_;

        /// <summary>
        /// Pointer to original chunk.
        /// </summary>
        unsafe Byte* origChunk_;

        /// <summary>
        /// Direct pointer to session data.
        /// </summary>
        unsafe ScSessionStruct* session_;

        /// <summary>
        /// Internal network data stream.
        /// </summary>
        NetworkDataStream dataStream_;

        /// <summary>
        /// Gets network data stream.
        /// </summary>
        internal NetworkDataStream DataStream
        {
            get
            {
                return dataStream_;
            }
        }

        /// <summary>
        /// Network port number.
        /// </summary>
        UInt16 portNumber_;

        /// <summary>
        /// Indicates if this Request is externally constructed.
        /// </summary>
        Boolean isExternalRequest_;

        /// <summary>
        /// Bytes for the corresponding request.
        /// </summary>
        Byte[] requestBytes_;

        /// <summary>
        /// Request bytes length.
        /// </summary>
        Int32 requestBytesLen_;

        /// <summary>
        /// Indicates if WebSocket upgrade is requested.
        /// </summary>
        Boolean webSocketUpgrade_;

        /// <summary>
        /// Indicates if request is aggregated.
        /// </summary>
        Boolean isAggregated_;

        /// <summary>
        /// Just using Request as holder for user Message instance type.
        /// </summary>
        Type messageObjectType_;

        /// <summary>
        /// Managed handler id.
        /// </summary>
        UInt16 managedHandlerId_;

        /// <summary>
        /// Indicates if user wants to send custom request.
        /// </summary>
        Boolean customFields_;

        /// <summary>
        /// Body string.
        /// </summary>
        String bodyString_;

        /// <summary>
        /// Body bytes.
        /// </summary>
        Byte[] bodyBytes_;

        /// <summary>
        /// List of cookies.
        /// </summary>
        List<String> cookies_;

        /// <summary>
        /// HTTP method.
        /// </summary>
        String methodString_;

        /// <summary>
        /// HTTP uri.
        /// </summary>
        String uriString_;

        /// <summary>
        /// Host name.
        /// </summary>
        String hostNameString_;

        /// <summary>
        /// String containing all headers.
        /// </summary>
        String headersString_;

        /// <summary>
        /// Dictionary of simple user custom headers.
        /// </summary>
        Dictionary<String, String> customHeaderFields_;

        /// <summary>
        /// Indicates if came with session originally.
        /// </summary>
        Boolean came_with_correct_session_ = false;

        /// <summary>
        /// Request application name.
        /// </summary>
        String handlerAppName_;

        /// <summary>
        /// Handler options.
        /// </summary>
        internal HandlerOptions HandlerOpts {
            get;
            set;
        }

        /// <summary>
        /// Network port number.
        /// </summary>
        public UInt16 PortNumber
        {
            get { return portNumber_; }
            set { portNumber_ = value; }
        }

        /// <summary>
        /// Returns a preferred MIME type in string format.
        /// </summary>
        public String PreferredMimeTypeString
        {
            get
            {
                var a = Headers[HttpHeadersUtf8.GetAcceptHeader];

                if (a != null)
                    return a.Split(new Char[] { ',' }, 2)[0];

                return "*/*";
            }
        }

        /// <summary>
        /// Returns a list of requested mime types in preference order as discovered in the Accept header
        /// of the request.
        /// </summary>
        public IEnumerator<String> PreferredMimeTypesStrings
        {
            get
            {
                var l = new List<String>();
                l.Add(PreferredMimeTypeString);
                return l.GetEnumerator();
            }
        }

        /// Returns the single most preferred mime type according to the Accept header of the request amongst a 
        /// set of common mime types. If the mime type is not in the enum of known common mime types, the
        /// value MimeType.Other is returned. If there is no Accept header or if the Accept header is empty,
        /// the value MimeType.Unspecified is returned.
        /// <remarks>
        /// TODO! Implement proper fast method! Include all mime types in xml file and speed up using
        /// similar code generation as the URI matcher.
        /// </remarks>
        public MimeType PreferredMimeType {
            get {
                var a = Headers[HttpHeadersUtf8.GetAcceptHeader];

                if (a != null)
                    return MimeTypeHelper.StringToMimeType(a);

                return MimeType.Unspecified;
            }
        }

        /// <summary>
        /// Returns a list of requested mime types in preference order as discovered in the Accept header
        /// of the request.
        /// </summary>
        /// <remarks>
        /// TODO! Implement! Does currently only return a single mime type and supports only a few mime types.
        /// </remarks>
        public IEnumerator<MimeType> PreferredMimeTypes {
            get {
                var l = new List<MimeType>();
                l.Add( PreferredMimeType );
                return l.GetEnumerator();
            }
        }

        /// <summary>
        /// Returns True if request was aggregated.
        /// </summary>
        internal Boolean IsAggregated {
            get { return isAggregated_; }
        }

        /// <summary>
        /// Returns protocol type.
        /// </summary>
        public Boolean WebSocketUpgrade
        {
            get { return webSocketUpgrade_; }
        }

        /// <summary>
        /// Setting message object type.
        /// </summary>
        internal Type ArgMessageObjectType
        {
            get { return messageObjectType_; }
            set { messageObjectType_ = value; }
        }

        /// <summary>
        /// Setting function for creating a new instance of the message object.
        /// </summary>
        internal Func<object> ArgMessageObjectCreate {
            get;
            set;
        }

        /// <summary>
        /// Accessors to HTTP method.
        /// </summary>
        public MixedCodeConstants.HTTP_METHODS MethodEnum { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Request" /> class.
        /// </summary>
        internal Request(Byte[] requestBytes, Int32 requestBytesLen)
        {
            unsafe {

                Headers = new HeadersAccessor(this);

                InternalInit(requestBytes, requestBytesLen);
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Request" /> class.
        /// </summary>
        internal unsafe void InitExternal(
            Byte* chunk_data,
            Boolean single_chunk,
            UInt32 chunk_index,
            UInt16 managed_handler_id,
            Byte* http_request_begin,
            Byte* socket_data,
            IntPtr socket_data_intptr,
            NetworkDataStream data_stream,
            Boolean webSocketUpgrade,
            Boolean isAggregated)
        {
            // Indicating that request is external.
            isExternalRequest_ = true;

            http_request_struct_ = (HttpRequestInternal*) http_request_begin;
            session_ = (ScSessionStruct*)(socket_data + MixedCodeConstants.SOCKET_DATA_OFFSET_SESSION);
            http_request_struct_->socket_data_ = socket_data;
            dataStream_ = data_stream;
            managedHandlerId_ = managed_handler_id;
            webSocketUpgrade_ = webSocketUpgrade;
            isAggregated_ = isAggregated;
            isSingleChunk_ = single_chunk;
            origChunk_ = chunk_data;
            socketDataIntPtr_ = socket_data_intptr;
            MethodEnum = (MixedCodeConstants.HTTP_METHODS) http_request_struct_->http_method_;

            // Checking if session is correct.
            GetAppsSessionInterface();
            came_with_correct_session_ = (INVALID_APPS_UNIQUE_SESSION_INDEX != (session_->linearIndex_));
        }

        /// <summary>
        /// Getting WebSocket UInt64 id from request information.
        /// </summary>
        /// <returns>UInt64 representing WebSocket id.</returns>
        public UInt64 GetWebSocketId() {
            return GetSocketId();
        }

        /// <summary>
        /// Getting socket UInt64 id from request information.
        /// </summary>
        /// <returns>UInt64 representing socket id.</returns>
        public UInt64 GetSocketId() {

            SocketStruct socketStruct = new SocketStruct();
            socketStruct.Init(dataStream_);

            return socketStruct.ToUInt64();
        }

        /// <summary>
        /// Sends the WebSocket upgrade HTTP response and creates a WebSocket object.
        /// </summary>
        /// <param name="groupName">WebSocket group name for subsequent events on created WebSocket.</param>
        /// <param name="cargoId">Integer identifier supplied from user that comes inside WebSocket object in subsequent events.</param>
        /// <param name="session">Session that should be attached to the created WebSocket.</param>
        /// <returns>Created WebSocket object that immediately can be used.</returns>
        public WebSocket SendUpgrade(
            String groupName,
            List<String> cookies = null,
            Dictionary<String, String> headers = null,
            IAppsSession session = null)
        {
            unsafe {

                System.Diagnostics.Debug.Assert(http_request_struct_->socket_data_ != null);

                Byte* chunk_data = http_request_struct_->socket_data_ - MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

                UInt32 upgradeResponsePartLength = *(UInt32*)(chunk_data + MixedCodeConstants.CHUNK_OFFSET_UPGRADE_PART_BYTES_TO_DB);
                UInt32 upgradeRequestLenBytes = *(UInt32*)(chunk_data + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_NUM_BYTES);
                UInt32 upgradeRequestOffset = *(UInt32*)(chunk_data + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

                // Checking if we should copy the WebSocket handshake data.
                Byte[] wsHandshakeResp = new Byte[upgradeResponsePartLength];
                Marshal.Copy(
                    new IntPtr(http_request_struct_->socket_data_ + upgradeRequestOffset + upgradeRequestLenBytes - wsHandshakeResp.Length),
                    wsHandshakeResp,
                    0,
                    wsHandshakeResp.Length);

                WsGroupInfo wsGroupInfo = AllWsGroups.WsManager.FindGroup(PortNumber, groupName);

                UInt32 groupId = 0;

                if (wsGroupInfo != null) {
                    groupId = wsGroupInfo.GroupId;
                }

                WebSocket ws = new WebSocket(dataStream_);

                Response resp = new Response();
                resp.Cookies = cookies;
                resp.SetHeadersDictionary(headers);

                ws.Session = session;
                if (session != null) {
                    ws.Session.ActiveWebSocket = ws;
                }

                resp.WsHandshakeResp = wsHandshakeResp;

                *(UInt32*)(origChunk_ + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_CHANNEL_ID) = groupId;
                *(UInt32*)(origChunk_ + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS) |=
                    (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_FLAGS_JUST_SEND | (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.HTTP_WS_FLAGS_UPGRADE_APPROVED;

                SendResponse(resp, null);

                return ws;
            }            
        }

        /// <summary>
        /// Initializes request structure.
        /// </summary>
        unsafe void InternalInit(Byte[] requestBytes, Int32 requestBytesLen)
        {
            unsafe
            {
                // Allocating space for Request contents and structure.
                Int32 alloc_size = requestBytesLen + sizeof(HttpRequestInternal);

                if (IntPtr.Zero != socketDataIntPtr_) {
                    BitsAndBytes.Free(socketDataIntPtr_);
                    socketDataIntPtr_ = IntPtr.Zero;
                }

                socketDataIntPtr_ = BitsAndBytes.Alloc(alloc_size);
                Byte* requestNativeBuf = (Byte*) socketDataIntPtr_.ToPointer();

                requestBytes_ = requestBytes;
                requestBytesLen_ = requestBytesLen;

                fixed (Byte* fixed_buf = requestBytes)
                {
                    // Copying HTTP request data.
                    BitsAndBytes.MemCpy(requestNativeBuf, fixed_buf, (UInt32)requestBytesLen);
                }

                // Pointing to HTTP request structure.
                http_request_struct_ = (HttpRequestInternal*) (requestNativeBuf + requestBytesLen);

                // Setting the request data pointer.
                http_request_struct_->socket_data_ = requestNativeBuf;

                // NOTE: No internal sessions support.
                session_ = null;

                // NOTE: No internal data stream support:
                // Simply on which socket to send this "request"?

                // Executing HTTP request parser and getting Request structure as result.
                UInt32 err_code = sc_parse_http_request(requestNativeBuf, (UInt32)requestBytesLen, (Byte*)http_request_struct_);

                // Checking if any error occurred.
                if (err_code != 0)
                {
                    // Freeing memory etc.
                    Destroy(true);

                    throw ErrorCode.ToException(err_code);
                }
            }
        }

        /// <summary>
        /// Destroys the instance of Request.
        /// </summary>
        override internal void DestroyByFinalizer() {
            Destroy(false);
        }

        /// <summary>
        /// Should the request have a finalizer?
        /// </summary>
        /// <returns></returns>
        internal Boolean ShouldBeFinalized() {

            unsafe {

                // Checking if already destroyed.
                if (http_request_struct_ != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Destroys the instance of Request.
        /// </summary>
        internal void Destroy(Boolean isStarcounterThread)
        {
            unsafe
            {
                // NOTE: Removing reference for finalizer so it does not call destroy again.
                UnLinkFinalizer();

                // Checking if already destroyed.
                if (http_request_struct_ == null)
                    return;

                // Checking if we have constructed this Request
                // internally in Apps or externally in Gateway.
                if (!isExternalRequest_)
                {
                    if (IntPtr.Zero != socketDataIntPtr_) {
                        BitsAndBytes.Free(socketDataIntPtr_);
                        socketDataIntPtr_ = IntPtr.Zero;
                    }
                }
                else
                {
                    origChunk_ = null;

                    // Releasing the plain buffer that was allocated when linked chunks were copied.
                    if (!isSingleChunk_) {

                        if (IntPtr.Zero != socketDataIntPtr_) {
                            BitsAndBytes.Free(socketDataIntPtr_);
                            socketDataIntPtr_ = IntPtr.Zero;
                        }
                    }

                    // Releasing data stream resources like chunks, etc.
                    dataStream_.Destroy(isStarcounterThread);
                    dataStream_ = null;
                }

                http_request_struct_ = null;
                session_ = null;
            }
        }

        /// <summary>
        /// Checking if its a looping host chunk.
        /// </summary>
        /// <returns></returns>
        internal unsafe Boolean IsLoopingHostChunk() {

            if (null == origChunk_)
                return false;

            return (((*(UInt32*)(origChunk_ + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) &
                (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_HOST_LOOPING_CHUNKS) != 0);
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
        /// Returns True if request is external.
        /// </summary>
        public Boolean IsExternal {
            get { return isExternalRequest_; }
        }

        /// <summary>
        /// To which application/handler this request belongs to.
        /// </summary>
        public String HandlerAppName {
            get {
                return handlerAppName_;
            }
            internal set {
                handlerAppName_ = value;
            }
        }

        /// <summary>
        /// Linear index for this handler.
        /// </summary>
        internal UInt16 ManagedHandlerId
        {
            get { return managedHandlerId_; }
            set { managedHandlerId_ = value; } 
        }

        /// <summary>
        /// Gets the raw parameters structure.
        /// </summary>
        internal IntPtr GetRawParametersInfo()
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                if (isExternalRequest_) {
                    return new IntPtr(http_request_struct_->socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_PARAMS_INFO);
                }

                throw new ArgumentException("Trying to get raw HTTP parameters in wrong way.");
            }
        }

        /// <summary>
        /// Gets the raw parameters structure.
        /// </summary>
        internal MixedCodeConstants.UserDelegateParamInfo GetParametersInfo() {
            unsafe {

                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                if (isExternalRequest_) {
                    return *(MixedCodeConstants.UserDelegateParamInfo*)(http_request_struct_->socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_PARAMS_INFO);
                }

                throw new ArgumentException("Trying to get HTTP parameters in wrong way.");
            }
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public IntPtr GetRawMethodAndUri()
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                return http_request_struct_->GetRawMethodAndUri();
            }
        }

        /// <summary>
        /// Content type.
        /// </summary>
        public String ContentType {
            get {
                return Headers[HttpHeadersUtf8.ContentTypeHeader];
            }

            set {
                customFields_ = true;
                Headers[HttpHeadersUtf8.ContentTypeHeader] = value;
            }
        }

        /// <summary>
        /// Content encoding.
        /// </summary>
        public String ContentEncoding {
            get {
                return Headers[HttpHeadersUtf8.ContentEncodingHeader];
            }

            set {
                customFields_ = true;
                Headers[HttpHeadersUtf8.ContentEncodingHeader] = value;
            }
        }

        /// <summary>
        /// Body string.
        /// </summary>
        public String Body
        {
            get
            {
                if (null == bodyString_) {

                    unsafe {

                        // First checking the incoming request data.
                        if (null != http_request_struct_) {

                            bodyString_ = http_request_struct_->GetBodyStringUtf8_Slow();

                        } else {

                            // Otherwise trying to convert from existing body bytes.
                            if (null != bodyBytes_) {
                                bodyString_ = Encoding.UTF8.GetString(bodyBytes_);
                            }
                        }
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
        /// Arbitrary object used to pass for Self calls.
        /// </summary>
        public Object BodyObject {
            get;
            set;
        }

        /// <summary>
        /// Body bytes.
        /// </summary>
        public Byte[] BodyBytes
        {
            get
            {
                if (null == bodyBytes_) {

                    unsafe {

                        // First checking the incoming request data.
                        if (null != http_request_struct_) {

                            bodyBytes_ = http_request_struct_->GetBodyByteArray_Slow();

                        } else {

                            // Otherwise trying to convert from existing body string.
                            if (null != bodyString_) {
                               bodyBytes_ = Encoding.UTF8.GetBytes(bodyString_);
                            }
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
        /// Headers string.
        /// </summary>
        public String GetAllHeaders()
        {
            unsafe
            {
                // Concatenating headers from dictionary.
                if ((null != customHeaderFields_) || (null != cookies_))
                {
                    headersString_ = "";

                    foreach (KeyValuePair<string, string> h in customHeaderFields_)
                    {
                        headersString_ += h.Key + ": " + h.Value + StarcounterConstants.NetworkConstants.CRLF;
                    }

                    if (null != cookies_)
                    {
                        foreach (String c in cookies_)
                        {
                            headersString_ += HttpHeadersUtf8.GetCookieStartString + c + StarcounterConstants.NetworkConstants.CRLF;
                        }
                    }

                    return headersString_;
                }

                if (null != http_request_struct_)
                    headersString_ = http_request_struct_->GetHeadersStringUtf8_Slow();

                return headersString_;
            }
        }

        /// <summary>
        /// List of Cookie headers.
        /// Each string is in the form of "key=value".
        /// </summary>
        public List<String> Cookies
        {
            get
            {
                if (cookies_ != null)
                    return cookies_;

                cookies_ = new List<String>();

                // Adding new cookies list from request.
                unsafe
                {
                    if (http_request_struct_ != null)
                    {
                        String allCookies = Headers[HttpHeadersUtf8.GetCookieHeader];
                        if (allCookies != null)
                        {
                            String[] splittedCookies = allCookies.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                            // Adding all trimmed cookies to list.
                            foreach (String c in splittedCookies)
                                cookies_.Add(c.Trim());
                        }
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
        /// Method string.
        /// </summary>
        public String Method
        {
            get
            {
                if (null == methodString_)
                    methodString_ = MethodEnum.ToString();

                return methodString_;
            }

            set
            {
                customFields_ = true;
                methodString_ = value;
            }
        }

        /// <summary>
        /// Uri string.
        /// </summary>
        public String Uri
        {
            get
            {
                if (null == uriString_) {

                    unsafe {

                        if (null != http_request_struct_) {

                            uriString_ = http_request_struct_->Uri;    
                        }
                    }
                }

                return uriString_;
            }

            set
            {
                customFields_ = true;
                uriString_ = value;
            }
        }

        /// <summary>
        /// HostName string.
        /// </summary>
        public String Host
        {
            get
            {
                if (hostNameString_ != null)
                    return hostNameString_;

                unsafe
                {
                    if (http_request_struct_ != null) {
                        hostNameString_ = Headers[HttpHeadersUtf8.HostHeader];
                    }
                }

                return hostNameString_;
            }

            set
            {
                customFields_ = true;
                hostNameString_ = value;
            }
        }

        /// <summary>
        /// Estimate response size in bytes.
        /// </summary>
        private int EstimateNeededSize() {

            int size = HttpHeadersUtf8.TotalByteSize;

            size += methodString_.Length;
            size += uriString_.Length;
            size += hostNameString_.Length;

            if (null != headersString_)
                size += headersString_.Length;

            if (null != customHeaderFields_) {
                foreach (KeyValuePair<string, string> h in customHeaderFields_) {
                    size += (h.Key.Length + h.Value.Length + 4);
                }
            }

            if (null != cookies_) {
                foreach (String c in cookies_) {
                    size += HttpHeadersUtf8.GetCookieStart.Length;
                    size += c.Length;
                    size += HttpHeadersUtf8.CRLF.Length;
                }
            }

            if (null != bodyString_)
                size += bodyString_.Length << 1;
            else if (null != bodyBytes_)
                size += bodyBytes_.Length;

            return size;
        }

		/// <summary>
		/// Constructs Response from fields that are set.
		/// </summary>
        public Int32 ConstructFromFields(Boolean dontModifyHeaders = false, Byte[] givenBuffer = null) {

            // Checking if we have a custom response.
            if (!customFields_)
                return 0;

            if (null == uriString_)
                throw new ArgumentException("Relative URI should be set when creating custom Request.");

            if (null == hostNameString_)
                hostNameString_ = "SC";

            if (null == methodString_)
                methodString_ = "GET";

            Utf8Writer writer;

            byte[] buf;
            Int32 estimatedNumBytes = EstimateNeededSize();

            // Checking if we can use given buffer.
            if ((null != givenBuffer) && (estimatedNumBytes <= givenBuffer.Length)) {
                buf = givenBuffer;
            } else {
                buf = new byte[estimatedNumBytes];
            }

            unsafe {
                fixed (byte* p = buf) {
                    writer = new Utf8Writer(p);

                    writer.Write(methodString_);
                    writer.Write(' ');
                    writer.Write(uriString_);
                    writer.Write(' ');
                    writer.Write(HttpHeadersUtf8.Http11NoSpace);
                    writer.Write(HttpHeadersUtf8.CRLF);

                    if (!dontModifyHeaders) {
                        writer.Write(HttpHeadersUtf8.HostStart);
                        writer.Write(hostNameString_);
                        writer.Write(HttpHeadersUtf8.CRLF);

                        if (null != headersString_) {
                            writer.Write(headersString_);
                        } else {
                            if (null != customHeaderFields_) {
                                foreach (KeyValuePair<string, string> h in customHeaderFields_) {
                                    writer.Write(h.Key);
                                    writer.Write(": ");
                                    writer.Write(h.Value);
                                    writer.Write(HttpHeadersUtf8.CRLF);
                                }
                            }
                        }

                        // Checking the cookies list.
                        if ((null != cookies_) && (cookies_.Count > 0)) {
                            writer.Write(HttpHeadersUtf8.GetCookieStart);
                            writer.Write(cookies_[0]);

                            for (Int32 i = 1; i < cookies_.Count; i++) {
                                writer.Write(HttpHeadersUtf8.SemicolonSpace);
                                writer.Write(cookies_[i]);
                            }

                            writer.Write(HttpHeadersUtf8.CRLF);
                        }
                    }

                    if (null != bodyString_) {
                        writer.Write(HttpHeadersUtf8.ContentLengthStart);
                        writer.Write(writer.GetByteCount(bodyString_));
                        writer.Write(HttpHeadersUtf8.CRLFCRLF);
                        writer.Write(bodyString_);
                    } else if (null != bodyBytes_) {
                        writer.Write(HttpHeadersUtf8.ContentLengthStart);
                        writer.Write(bodyBytes_.Length);
                        writer.Write(HttpHeadersUtf8.CRLFCRLF);
                        writer.Write(bodyBytes_);
                    } else {
                        writer.Write(HttpHeadersUtf8.ContentLengthStart);
                        writer.Write('0');
                        writer.Write(HttpHeadersUtf8.CRLFCRLF);
                    }

                    // Finally setting the request bytes.
                    requestBytes_ = buf;
                    requestBytesLen_ = writer.Written;

                    if (requestBytesLen_ > estimatedNumBytes) {
                        throw new ArgumentOutOfRangeException("Terrible situation: requestBytesLen_ > estimatedNumBytes");
                    }
                }
            }

            customFields_ = false;
            return requestBytesLen_;
        }

        /// <summary>
        /// Gets the content raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetBodyRaw(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");
                
                http_request_struct_->GetBodyRaw(out ptr, out sizeBytes);
            }
        }

        /// <summary>
        /// Obtain available request bytes.
        /// </summary>
        public Byte[] RequestBytes {

            get {
                if (requestBytes_ != null)
                    return requestBytes_;

                if (customFields_) {
                    ConstructFromFields();
                    return requestBytes_;
                }

                requestBytes_ = GetRequestByteArray_Slow();
                return requestBytes_;
            }

            set {
                requestBytes_ = value;
            }
        }

        /// <summary>
        /// Gets the request as byte array.
        /// </summary>
        /// <returns>Request bytes.</returns>
        internal Byte[] GetRequestByteArray_Slow()
        {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            unsafe
            {
                if (null == http_request_struct_) {
                    throw new ArgumentException("HTTP request not initialized.");
                }

                return http_request_struct_->GetRequestByteArray_Slow();
            }
        }

        /// <summary>
        /// Gets the client IP address.
        /// </summary>
        public IPAddress ClientIpAddress
        {
            get
            {
                unsafe
                {
                    if (null != http_request_struct_) {

                        if (isExternalRequest_)
                            return new IPAddress(*(Int64*)(http_request_struct_->socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_CLIENT_IP));
                    }

                    return IPAddress.Loopback;
                }
            }
        }

        /// <summary>
        /// Request length in bytes.
        /// </summary>
        public Int32 RequestLength {
            get {

                if (requestBytesLen_ > 0)
                    return requestBytesLen_;

                if (customFields_) {
                    ConstructFromFields();
                    return requestBytesLen_;
                }

                unsafe {

                    if (null != http_request_struct_) {

                        requestBytesLen_ = (Int32)http_request_struct_->request_len_bytes_;
                    }
                }

                return requestBytesLen_;
            }

            set {
                requestBytesLen_ = value;
            }
        }

        /// <summary>
        /// Gets the length of the content in bytes.
        /// </summary>
        public Int32 ContentLength
        {
            get
            {
                unsafe
                {
                    if (bodyString_ != null)
                        return bodyString_.Length;

                    if (bodyBytes_ != null)
                        return bodyBytes_.Length;

                    if (null != http_request_struct_)   
                        return (Int32) http_request_struct_->content_len_bytes_;

                    return 0;
                }
            }
        }

        /// <summary>
        /// Sends the response.
        /// </summary>
        void SendResponse(Byte[] buffer, Int32 offset, Int32 length, Response.ConnectionFlags connFlags)
        {
            try {

                // Simply returning if its a looping chunk.
                if (IsLoopingHostChunk())
                    return;

                unsafe {

                   // TODO: Check for the proper fix.
                   if (null != dataStream_) {
                       dataStream_.SendResponse(buffer, offset, length, connFlags);
                   }
                }

            } finally {
                Destroy(true);
            }
        }

        /// <summary>
        /// Sends response on this request.
        /// </summary>
        public void SendResponse(Response resp, Byte[] serializationBuf)
        {
            Response savedResp = resp;

            // Checking if there are any outgoing filters.
            Response filteredResp = Handle.RunResponseFilters(this, resp);
            if (null != filteredResp) {
                resp = filteredResp;
            }

            // Checking if global response status code is set.
            if (Handle.OutgoingStatusCode > 0) {
                resp.StatusCode = Handle.OutgoingStatusCode;
            }

            // Checking if global response status description is set.
            if (Handle.OutgoingStatusDescription != null) {
                resp.StatusDescription = Handle.OutgoingStatusDescription;
            }

            // Checking the global response headers list.
            if (null != Handle.OutgoingHeaders) {
                foreach (KeyValuePair<String, String> h in Handle.OutgoingHeaders) {
                    resp.Headers[h.Key] = h.Value;
                }
            }

            // Checking the global response cookies list.
            if (null != Handle.OutgoingCookies) {
                foreach (KeyValuePair<String, String> c in Handle.OutgoingCookies) {
                    resp.Cookies.Add(c.Key + "=" + c.Value);
                }
            }

            // Constructing response bytes.
            resp.ConstructFromFields(this, serializationBuf);

            // If we have a streamed response body - getting corresponding TCP socket to stream on.
            TcpSocket tcpSocket = null;
            if (resp.StreamedBody != null) {

                tcpSocket = new TcpSocket(dataStream_);

                // Setting the flag that this response body should be streamed.
                resp.ConnFlags |= Response.ConnectionFlags.StreamingResponseBody;

                StreamingInfo s = new StreamingInfo(resp.StreamedBody);

                // Adding to response streams dictionary.
                Response.responseStreams_[tcpSocket.ToUInt64()] = s;
            }

            // Sending the response.
            SendResponse(resp.BufferContainingResponse, resp.ResponseOffsetInBuffer, resp.ResponseSizeBytes, resp.ConnFlags);
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        public void GetRawMethodAndUri(out IntPtr ptr, out Int32 sizeBytes) 
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                http_request_struct_->GetRawMethodAndUri(out ptr, out sizeBytes);
            }
        }

        /// <summary>
        /// Headers dictionary accessors.
        /// </summary>
        public Dictionary<String, String> HeadersDictionary
        {
            get
            {
                if (null == customHeaderFields_)
                {
                    if (headersString_ == null)
                    {
                        unsafe
                        {
                            if (null != http_request_struct_) {
                                headersString_ = http_request_struct_->GetHeadersStringUtf8_Slow();
                            } else {
                                return null;
                            }
                        }
                    }

                    customHeaderFields_ = Response.CreateHeadersDictionaryFromHeadersString(headersString_);
                }

                return customHeaderFields_;
            }

            set
            {
                customHeaderFields_ = value;
            }
        }

        /// <summary>
        /// Accessors for headers.
        /// </summary>
        public class HeadersAccessor {

            Request requestRef_;

            public HeadersAccessor(Request req) {
                requestRef_ = req;
            }

            public String this[String name] {
                get {
                    return requestRef_.GetHeader(name);
                }

                set {
                    requestRef_.SetHeader(name, value);
                }
            }
        }

        /// <summary>
        /// Accessor to response headers.
        /// </summary>
        public HeadersAccessor Headers;

        /// <summary>
        /// Get the value of specific header.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <returns>Header value.</returns>
        internal String GetHeader(String name) {

            if (null != customHeaderFields_) {
                if (customHeaderFields_.ContainsKey(name))
                    return customHeaderFields_[name];

                return null;
            }

            unsafe
            {
                if (null == http_request_struct_) {

                    // Checking specifically for the host header.
                    if ("Host" == name) {
                        return Host;
                    }

                    return null;
                }

                return http_request_struct_->GetHeaderValue(name, ref headersString_);
            }
        }

        /// <summary>
        /// Set the value of a specific header.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        internal void SetHeader(String name, String value) {

            customFields_ = true;

            if (null == customHeaderFields_) {
                String headers = headersString_;
                if (headers == null) {
                    unsafe
                    {
                        if (null != http_request_struct_)
                            headers = http_request_struct_->GetHeadersStringUtf8_Slow();
                    }
                }

                customHeaderFields_ = Response.CreateHeadersDictionaryFromHeadersString(headers);
            }

            customHeaderFields_[name] = value;
        }

        /// <summary>
        /// Gets the <see cref="String" /> with the specified name.
        /// </summary>
        [System.Obsolete("Please use Headers[\"HeaderName\"] instead.")]
        public String this[String name] 
        {
            get {
                return GetHeader(name);
            }

            set {
                SetHeader(name, value);
            }
        }

        /// <summary>
        /// Invalid session index.
        /// </summary>
        public const UInt32 INVALID_GW_SESSION_INDEX = UInt32.MaxValue;

        /// <summary>
        /// Invalid Apps session unique number.
        /// </summary>
        public const UInt32 INVALID_APPS_UNIQUE_SESSION_INDEX = UInt32.MaxValue;

        /// <summary>
        /// Invalid Apps session salt.
        /// </summary>
        public const UInt64 INVALID_APPS_SESSION_SALT = 0;

        /// <summary>
        /// Invalid Apps session index.
        /// </summary>
        public const UInt32 INVALID_APPS_SESSION_INDEX = UInt32.MaxValue;

        /// <summary>
        /// Invalid View model index.
        /// </summary>
        public const UInt32 INVALID_VIEW_MODEL_INDEX = UInt32.MaxValue;

        /// <summary>
        /// Checks if HTTP request already came with session.
        /// </summary>
        internal Boolean CameWithCorrectSession 
        {
            get {
                return came_with_correct_session_;
            }
        }

        /// <summary>
        /// Gets certain Apps session.
        /// </summary>
        internal IAppsSession GetAppsSessionInterface()
        {
            unsafe 
            {
                // Obtaining corresponding Apps session.
                IAppsSession apps_session =
                    GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(ref *session_);

                // Destroying the session if Apps session was destroyed.
                if (apps_session == null) {
                    session_->Destroy();
                }

                return apps_session;
            }
        }

        /// <summary>
        /// Generates new session.
        /// </summary>
        internal UInt32 GenerateNewSession(IAppsSession apps_session)
        {
            unsafe
            {
                // Simply generating new session.
                return GlobalSessions.AllGlobalSessions.CreateNewSession(
                    ref *session_,
                    apps_session);
            }
        }

        // Reference to corresponding session.
        public IAppsSession Session
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the is gzip accepted.
        /// </summary>
        /// <value>The is gzip accepted.</value>
        public Boolean IsGzipAccepted
        {
            get
            {
                unsafe
                {
                    if (null != http_request_struct_) {
                        return (http_request_struct_->gzip_accepted_ != 0);
                    }

                    return false;
                }
            }
        }

        /// <summary>
        /// Returns a string that represents the request.
        /// </summary>
        /// <returns>A string that represents the request.</returns>
        public override String ToString()
        {
            unsafe
            {
                if (null != http_request_struct_) {
                    return http_request_struct_->GetRequestStringUtf8_Slow();
                }
            }

            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HttpRequestInternal {

        internal UInt32 request_len_bytes_;
        internal UInt32 content_len_bytes_;

        internal UInt16 request_offset_;
        internal UInt16 content_offset_;
        internal UInt16 uri_len_bytes_;
        internal UInt16 headers_offset_;
        internal UInt16 headers_len_bytes_;
        internal UInt16 session_string_offset_;
        internal UInt16 uri_offset_;

        internal Byte http_method_;
        internal Byte gzip_accepted_;

        // TODO: Should be changed!
        // Socket data pointer.
        public unsafe Byte* socket_data_;

        /// <summary>
        /// Gets the content raw pointer.
        /// </summary>
        public void GetBodyRaw(out IntPtr ptr, out UInt32 sizeBytes) {

            if (content_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + content_offset_);

            sizeBytes = content_len_bytes_;
        }

        /// <summary>
        /// Gets the content as byte array.
        /// </summary>
        internal Byte[] GetBodyByteArray_Slow()
        {
            // Checking if there is a content.
            if (content_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] content_bytes = new Byte[(Int32)content_len_bytes_];
            Marshal.Copy(new IntPtr(socket_data_ + content_offset_), content_bytes, 0, (Int32)content_len_bytes_);

            return content_bytes;
        }

        /// <summary>
        /// Gets body as UTF8 string.
        /// </summary>
        internal String GetRequestStringUtf8_Slow() {

            return new String((SByte*)(socket_data_ + request_offset_), 0, (Int32)request_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the request as byte array.
        /// </summary>
        internal Byte[] GetRequestByteArray_Slow()
        {
            // Checking if there is a request.
            if (request_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] request_bytes = new Byte[(Int32)request_len_bytes_];
            Marshal.Copy(new IntPtr(socket_data_ + request_offset_), request_bytes, 0, (Int32)request_len_bytes_);

            return request_bytes;
        }

        /// <summary>
        /// Gets body as UTF8 string.
        /// </summary>
        internal String GetBodyStringUtf8_Slow() {

            // Checking if there is a body.
            if (content_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + content_offset_), 0, (Int32)content_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        public void GetRawMethodAndUri(out IntPtr ptr, out Int32 sizeBytes)
        {
            // NOTE: Method and URI must always exist.

            ptr = new IntPtr(socket_data_ + request_offset_);
            sizeBytes = (uri_offset_ - request_offset_ + uri_len_bytes_);
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        public IntPtr GetRawMethodAndUri() {
            // NOTE: Method and URI must always exist.
            return new IntPtr(socket_data_ + request_offset_);
        }

        /// <summary>
        /// Gets headers as ASCII string.
        /// </summary>
        internal String GetHeadersStringUtf8_Slow()
        {
            // Checking if there are cookies.
            if (headers_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + headers_offset_), 0, (Int32)headers_len_bytes_, Encoding.ASCII);
        }

        /// <summary>
        /// Gets the raw session string.
        /// </summary>
        public void GetRawSessionString(out IntPtr ptr) {
            ptr = new IntPtr(socket_data_ + session_string_offset_);
        }

        /// <summary>
        /// Gets the session string.
        /// </summary>
        public String GetSessionString()
        {
            IntPtr raw_session_string;
            GetRawSessionString(out raw_session_string);

            return Marshal.PtrToStringAnsi(raw_session_string, MixedCodeConstants.SESSION_STRING_LEN_CHARS);
        }

        /// <summary>
        /// Gets the header value.
        /// </summary>
        public String GetHeaderValue(String headerName, ref String headersString) {

            // Constructing the string if its the first time.
            if (headersString == null)
                headersString = Marshal.PtrToStringAnsi(new IntPtr(socket_data_ + headers_offset_), (Int32)headers_len_bytes_);

            // Getting needed substring.
            Int32 hstart = headersString.IndexOf(headerName+":", StringComparison.InvariantCultureIgnoreCase);
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

        /// <summary>
        /// Gets the URI.
        /// </summary>
        public String Uri {
            get {
                if (uri_len_bytes_ > 0)
                    return Marshal.PtrToStringAnsi(new IntPtr(socket_data_ + uri_offset_), (Int32)uri_len_bytes_);

                return null;
            }
        }
    }
}
