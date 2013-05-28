
using HttpStructs;
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;
using System.Text;
namespace Starcounter.Advanced {
    /// <summary>
    /// Class Request
    /// </summary>
    public class Request {
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

        public static Request GET(string uri) {
            return new Request(RawGET(uri));
        }

        public Request()
        {

        }

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
        /// Network port number.
        /// </summary>
        UInt16 port_number_ = 0;

        /// <summary>
        /// Network port number.
        /// </summary>
        public UInt16 PortNumber
        {
            get { return port_number_; }
            set { port_number_ = value; }
        }

        /// <summary>
        /// Indicates if this Request is internally constructed from Apps.
        /// </summary>
        Boolean is_internal_request_ = false;

        /// <summary>
        /// Just using Request as holder for user Message instance type.
        /// </summary>
        Type message_object_type_ = null;

        // Type of network protocol.
        MixedCodeConstants.NetworkProtocolType protocol_type_;

        /// <summary>
        /// Returns protocol type.
        /// </summary>
        public MixedCodeConstants.NetworkProtocolType ProtocolType
        {
            get { return protocol_type_; }
        }

        /// <summary>
        /// Setting message object type.
        /// </summary>
        public Type ArgMessageObjectType
        {
            get { return message_object_type_; }
            set { message_object_type_ = value; }
        }

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
        /// Initializes a new instance of the <see cref="Request" /> class.
        /// </summary>
        /// <param name="buf">The buf.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public Request(Byte[] buf)
        {
            unsafe
            {
                Init(buf, null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Request" /> class.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="params_info"></param>
        public unsafe Request(Byte[] buf, Byte* params_info_ptr)
        {
            Init(buf, params_info_ptr);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Request" /> class.
        /// </summary>
        /// <param name="chunk_data">The chunk_data.</param>
        /// <param name="single_chunk">The single_chunk.</param>
        /// <param name="chunk_index">The chunk_index.</param>
        /// <param name="handler_id">The handler id.</param>
        /// <param name="http_request_begin">The http_request_begin.</param>
        /// <param name="socket_data">The socket_data.</param>
        /// <param name="data_stream">The data_stream.</param>
        /// <param name="protocol_type">Type of network protocol.</param>
        public unsafe Request(
            Byte* chunk_data,
            Boolean single_chunk,
            UInt32 chunk_index,
            UInt16 handler_id,
            Byte* http_request_begin,
            Byte* socket_data,
            INetworkDataStream data_stream,
            MixedCodeConstants.NetworkProtocolType protocol_type)
        {
            http_request_struct_ = (HttpRequestInternal*)http_request_begin;
            session_ = (ScSessionStruct*)(socket_data + MixedCodeConstants.SOCKET_DATA_OFFSET_SESSION);
            http_request_struct_->socket_data_ = socket_data;
            data_stream_ = data_stream;
            data_stream_.Init(chunk_data, single_chunk, chunk_index);
            handler_id_ = handler_id;
            protocol_type_ = protocol_type;
        }

        /// <summary>
        /// Initializes request structure.
        /// </summary>
        /// <param name="buf"></param>
        unsafe void Init(Byte[] buf, Byte* params_info_ptr)
        {
            unsafe
            {
                // Allocating space for Request contents and structure.
                Int32 alloc_size = buf.Length + sizeof(HttpRequestInternal);
                if (params_info_ptr != null)
                    alloc_size += MixedCodeConstants.PARAMS_INFO_MAX_SIZE_BYTES;

                Byte* request_native_buf = (Byte*) BitsAndBytes.Alloc(alloc_size);
                fixed (Byte* fixed_buf = buf)
                {
                    // Copying HTTP request data.
                    BitsAndBytes.MemCpy(request_native_buf, fixed_buf, (UInt32)buf.Length);
                }

                // Pointing to HTTP request structure.
                http_request_struct_ = (HttpRequestInternal*)(request_native_buf + buf.Length);

                // Setting the request data pointer.
                http_request_struct_->socket_data_ = request_native_buf;

                // Checking if we have parameters info supplied.
                if (params_info_ptr != null)
                {
                    http_request_struct_->params_info_ptr_ = request_native_buf + buf.Length + sizeof(HttpRequestInternal);
                    BitsAndBytes.MemCpy(http_request_struct_->params_info_ptr_, params_info_ptr, MixedCodeConstants.PARAMS_INFO_MAX_SIZE_BYTES);
                }
                
                // Indicating that we internally constructing Request.
                is_internal_request_ = true;
                protocol_type_ = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1;

                // NOTE: No internal sessions support.
                session_ = null;

                // NOTE: No internal data stream support:
                // Simply on which socket to send this "request"?

                // Executing HTTP request parser and getting Request structure as result.
                UInt32 err_code = sc_parse_http_request(request_native_buf, (UInt32)buf.Length, (Byte*)http_request_struct_);

                // Checking if any error occurred.
                if (err_code != 0)
                {
                    // Freeing memory etc.
                    Destroy();

                    throw ErrorCode.ToException(err_code);
                }
            }
        }

        /// <summary>
        /// Destroys the instance of Request.
        /// </summary>
        public void Destroy()
        {
            unsafe
            {
                // Checking if already destroyed.
                if (http_request_struct_ == null)
                    return;

                // Checking if we have constructed this Request
                // internally in Apps or externally in Gateway.
                if (is_internal_request_)
                {
                    // Releasing internal resources here.
                    // NOTE: Socket_data_ points to complete memory including http_request_struct_,
                    // so freeing it frees everything.
                    BitsAndBytes.Free((IntPtr)http_request_struct_->socket_data_);
                }
                else
                {
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
        public bool IsDestroyed()
        {
            unsafe
            {
                return (http_request_struct_ == null) && (session_ == null);
            }
        }
        
        /// <summary>
        /// Releases resources.
        /// </summary>
        ~Request()
        {
            Destroy();
        }

        // TODO
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
        /// The needs script injection.
        /// </summary>
        bool needs_script_injection_ = false;

        /// <summary>
        /// Gets or sets a value indicating whether [needs script injection].
        /// </summary>
        /// <value><c>true</c> if [needs script injection]; otherwise, <c>false</c>.</value>
        public bool NeedsScriptInjection
        {
            get { return needs_script_injection_; }
            set { needs_script_injection_ = value; }
        }

        /// <summary>
        /// Linear index for this handler.
        /// </summary>
        UInt16 handler_id_;
        public UInt16 HandlerId
        {
            get { return handler_id_; }
            set { handler_id_ = value; } 
        }

        /// <summary>
        /// Gets the raw parameters structure.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public IntPtr GetRawParametersInfo()
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                if (!is_internal_request_)
                    return (IntPtr)(http_request_struct_->socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_PARAMS_INFO);

                return (IntPtr)http_request_struct_->params_info_ptr_;
            }
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public IntPtr GetRawMethodAndUri()
        {
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");

                        return http_request_struct_->GetRawMethodAndUri();
                    }
                }

                default:
                {
                    return IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// The is app view_
        /// </summary>
        bool is_app_view_ = false;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is app view.
        /// </summary>
        /// <value><c>true</c> if this instance is app view; otherwise, <c>false</c>.</value>
        public bool IsAppView 
        {
            get { return is_app_view_; }
            set { is_app_view_ = value; }
        }

        /// <summary>
        /// The gzip advisable_
        /// </summary>
        bool gzip_advisable_ = false;

        /// <summary>
        /// Gets or sets the gzip advisable.
        /// </summary>
        /// <value>The gzip advisable.</value>
        public Boolean GzipAdvisable 
        {
            get { return gzip_advisable_; }
            set { gzip_advisable_ = value; }
        }

        /// <summary>
        /// Gets or sets the view model.
        /// </summary>
        /// <value>The view model.</value>
        public byte[] ViewModel { get; set; }

        /// <summary>
        /// Indicates if user wants to send custom request.
        /// </summary>
        Boolean customFields_;

        /// <summary>
        /// Bytes that represent custom request.
        /// </summary>
        Byte[] customBytes_;

        String contentType_;

        /// <summary>
        /// Content type.
        /// </summary>
        public String ContentType
        {
            get
            {
                if (null == contentType_)
                    contentType_ = this["Content-Type"];

                return contentType_;
            }

            set
            {
                customFields_ = true;
                contentType_ = value;
            }
        }

        String contentEncoding_;

        /// <summary>
        /// Content encoding.
        /// </summary>
        public String ContentEncoding
        {
            get
            {
                if (null == contentEncoding_)
                    contentEncoding_ = this["Content-Encoding"];

                return contentEncoding_;
            }

            set
            {
                customFields_ = true;
                contentEncoding_ = value;
            }
        }

        String bodyString_;

        /// <summary>
        /// Body string.
        /// </summary>
        public String Body
        {
            get
            {
                if (null == bodyString_)
                    bodyString_ = GetBodyStringUtf8_Slow();

                return bodyString_;
            }

            set
            {
                customFields_ = true;
                bodyString_ = value;
            }
        }

        Byte[] bodyBytes_;

        /// <summary>
        /// Body bytes.
        /// </summary>
        public Byte[] BodyBytes
        {
            get
            {
                if (null == bodyBytes_)
                    bodyBytes_ = GetBodyBytes_Slow();

                return bodyBytes_;
            }

            set
            {
                customFields_ = true;
                bodyBytes_ = value;
            }
        }

        String headersString_;

        /// <summary>
        /// Headers string.
        /// </summary>
        public String Headers
        {
            get
            {
                if (null == headersString_)
                    throw new ArgumentException("Headers field is not set.");

                return headersString_;
            }

            set
            {
                customFields_ = true;
                headersString_ = value;
            }
        }

        String cookieString_;

        /// <summary>
        /// Cookie string.
        /// </summary>
        public String Cookie
        {
            get
            {
                if (null == cookieString_)
                    cookieString_ = GetCookiesStringUtf8_Slow();

                return cookieString_;
            }

            set
            {
                customFields_ = true;
                cookieString_ = value;
            }
        }

        String methodString_;

        /// <summary>
        /// Method string.
        /// </summary>
        public String Method
        {
            get
            {
                return methodString_;
            }

            set
            {
                customFields_ = true;
                methodString_ = value;
            }
        }

        String uriString_;

        /// <summary>
        /// Uri string.
        /// </summary>
        public String Uri
        {
            get
            {
                if (null == uriString_)
                {
                    switch (protocol_type_)
                    {
                        case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                        {
                            unsafe
                            {
                                if (null == http_request_struct_)
                                    throw new ArgumentException("HTTP request not initialized.");

                                uriString_ = http_request_struct_->Uri;
                            }

                            return uriString_;
                        }
                    }

                    throw new NotSupportedException("Network protocol does not support this method call.");
                }

                return uriString_;
            }

            set
            {
                customFields_ = true;
                uriString_ = value;
            }
        }

        String hostNameString_;

        /// <summary>
        /// HostName string.
        /// </summary>
        public String HostName
        {
            get
            {
                return hostNameString_;
            }

            set
            {
                customFields_ = true;
                hostNameString_ = value;
            }
        }

        /// <summary>
        /// Resets all custom fields.
        /// </summary>
        public void ResetAllCustomFields()
        {
            customFields_ = false;

            cookieString_ = null;
            headersString_ = null;
            bodyString_ = null;
            contentType_ = null;
            contentEncoding_ = null;
            hostNameString_ = null;
            uriString_ = null;
            methodString_ = null;
        }

        /// <summary>
        /// Constructs Response from fields that are set.
        /// </summary>
        public void ConstructFromFields()
        {
            // Checking if we have a custom response.
            if (!customFields_)
                return;

            if (null == uriString_)
                throw new ArgumentException("Relative URI should be set when creating custom Request.");

            if (null == hostNameString_)
                throw new ArgumentException("Host name should be set when creating custom Request.");

            if (null == methodString_)
                methodString_ = "GET";

            String str = methodString_ + " " + uriString_ + " HTTP/1.1" + StarcounterConstants.NetworkConstants.CRLF;
            str += "Host:" + hostNameString_ + StarcounterConstants.NetworkConstants.CRLF;

            if (null != headersString_)
                str += headersString_;

            if (null != contentType_)
                str += "Content-Type: " + contentType_ + StarcounterConstants.NetworkConstants.CRLF;

            if (null != contentEncoding_)
                str += "Content-Encoding: " + contentEncoding_ + StarcounterConstants.NetworkConstants.CRLF;

            if (null != cookieString_)
                str += "Cookie: " + cookieString_ + StarcounterConstants.NetworkConstants.CRLF;

            Int32 contentLength = 0;

            if (null != bodyString_)
                contentLength = bodyString_.Length;

            str += "Content-Length: " + contentLength + StarcounterConstants.NetworkConstants.CRLF;

            str += StarcounterConstants.NetworkConstants.CRLF;

            if (null != bodyString_)
                str += bodyString_;

            // Finally setting the request bytes.
            customBytes_ = Encoding.UTF8.GetBytes(str);
            customFields_ = false;
        }

        /// <summary>
        /// Gets a value indicating whether this instance can use static response.
        /// </summary>
        /// <value><c>true</c> if this instance can use static response; otherwise, <c>false</c>.</value>
        public bool CanUseStaticResponse 
        {
            get { return ViewModel == null; }
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
        /// Gets the content as byte array.
        /// </summary>
        /// <returns></returns>
        public Byte[] GetBodyByteArray_Slow()
        {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                return http_request_struct_->GetBodyByteArray_Slow();
            }
        }

        /// <summary>
        /// Gets the request as byte array.
        /// </summary>
        /// <returns>Request bytes.</returns>
        public Byte[] GetRequestByteArray_Slow()
        {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                return http_request_struct_->GetRequestByteArray_Slow();
            }
        }

        /// <summary>
        /// Byte array of the request.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static implicit operator Byte[](Request r)
        {
            return r.GetRequestByteArray_Slow();
        }

        /// <summary>
        /// Gets body as UTF8 string.
        /// </summary>
        public String GetBodyStringUtf8_Slow()
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                return http_request_struct_->GetBodyStringUtf8_Slow();
            }
        }

        /// <summary>
        /// Gets body bytes.
        /// </summary>
        public Byte[] GetBodyBytes_Slow()
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                return http_request_struct_->GetBodyByteArray_Slow();
            }
        }

        /// <summary>
        /// Gets the length of the content in bytes.
        /// </summary>
        /// <value>The length of the content.</value>
        public UInt32 ContentLength
        {
            get
            {
                unsafe
                {
                    if (null == http_request_struct_)
                        throw new ArgumentException("HTTP request not initialized.");

                    return http_request_struct_->content_len_bytes_;
                }
            }
        }

        /// <summary>
        /// Writes the response.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void SendResponse(Byte[] buffer, Int32 offset, Int32 length)
        {
            unsafe { data_stream_.SendResponse(buffer, offset, length); }
        }

        /// <summary>
        /// Gets the raw request.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRequestRaw(out IntPtr ptr, out UInt32 sizeBytes) 
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                http_request_struct_->GetRequestRaw(out ptr, out sizeBytes);
            }
        }

        /// <summary>
        /// Gets request as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetRequestStringUtf8_Slow() 
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                return http_request_struct_->GetRequestStringUtf8_Slow();
            }
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawMethodAndUri(out IntPtr ptr, out UInt32 sizeBytes) 
        {
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");

                        http_request_struct_->GetRawMethodAndUri(out ptr, out sizeBytes);
                    }

                    return;
                }
            }

            throw new NotSupportedException("Network protocol does not support this method call.");
        }

        /// <summary>
        /// Gets the raw method and URI plus extra character.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawMethodAndUriPlusAnExtraCharacter(out IntPtr ptr, out UInt32 sizeBytes) 
        {
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");

                        http_request_struct_->GetRawMethodAndUri(out ptr, out sizeBytes);
                    }

                    sizeBytes += 1;
                    return;
                }
            }

            throw new NotSupportedException("Network protocol does not support this method call.");
        }

        /// <summary>
        /// Gets the raw headers.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes) 
        {
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");

                        http_request_struct_->GetRawHeaders(out ptr, out sizeBytes);
                    }

                    return;
                }
            }

            throw new NotSupportedException("Network protocol does not support this method call.");
        }

        /// <summary>
        /// Gets headers as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetHeadersStringUtf8_Slow()
        {
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");

                        return http_request_struct_->GetHeadersStringUtf8_Slow();
                    }
                }
            }

            throw new NotSupportedException("Network protocol does not support this method call.");
        }

        /// <summary>
        /// Gets the raw cookies.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes) 
        {
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");
                        
                        http_request_struct_->GetRawCookies(out ptr, out sizeBytes);
                    }

                    return;
                }
            }

            throw new NotSupportedException("Network protocol does not support this method call.");
        }

        /// <summary>
        /// Gets cookies as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetCookiesStringUtf8_Slow()
        {
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");

                        return http_request_struct_->GetCookiesStringUtf8_Slow();
                    }
                }
            }

            throw new NotSupportedException("Network protocol does not support this method call.");
        }

        /// <summary>
        /// Gets the raw accept.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawAccept(out IntPtr ptr, out UInt32 sizeBytes) 
        {
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");

                        http_request_struct_->GetRawAccept(out ptr, out sizeBytes);
                    }

                    return;
                }
            }

            throw new NotSupportedException("Network protocol does not support this method call.");
        }

        /// <summary>
        /// Gets the raw session string.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes) 
        {
            unsafe
            {
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                http_request_struct_->GetRawSessionString(out ptr, out sizeBytes);
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
            switch (protocol_type_)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    unsafe
                    {
                        if (null == http_request_struct_)
                            throw new ArgumentException("HTTP request not initialized.");

                        http_request_struct_->GetHeaderValue(key, out ptr, out sizeBytes);
                    }

                    return;
                }
            }

            throw new NotSupportedException("Network protocol does not support this method call.");
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
                switch (protocol_type_)
                {
                    case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                    {
                        unsafe
                        {
                            if (null == http_request_struct_)
                                throw new ArgumentException("HTTP request not initialized.");

                            return http_request_struct_->GetHeaderValue(name);
                        }
                    }
                }

                throw new NotSupportedException("Network protocol does not support this method call.");
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
        /// Checks if HTTP request already has session.
        /// </summary>
        public Boolean HasSession 
        {
            get
            {
                unsafe
                {
                    if (session_ != null)
                        return INVALID_APPS_UNIQUE_SESSION_INDEX != (session_->linear_index_);

                    return false;
                }
            }
        }

        /// <summary>
        /// Gets certain Apps session.
        /// </summary>
        public IAppsSession AppsSessionInterface 
        {
            get 
            {
                unsafe 
                {

                    // Obtaining corresponding Apps session.
                    IAppsSession apps_session = GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(
                        session_->scheduler_id_,
                        session_->linear_index_,
                        session_->random_salt_);

                    // Destroying the session if Apps session was destroyed.
                    if (apps_session == null)
                        session_->Destroy();

                    return apps_session;
                }
            }
        }

        /// <summary>
        /// Generates session number and writes it to response.
        /// </summary>
        public UInt32 GenerateNewSession(IAppsSession apps_session)
        {
            unsafe
            {
                // Simply generating new session.
                return GlobalSessions.AllGlobalSessions.CreateNewSession(
                    session_->scheduler_id_,
                    ref session_->linear_index_,
                    ref session_->random_salt_,
                    ref session_->view_model_index_,
                    apps_session);
            }
        }

        /// <summary>
        /// Update session details.
        /// </summary>
        public void UpdateSessionDetails()
        {
            // Don't do anything on internal requests.
            if (is_internal_request_)
                return;

            unsafe
            {
                // Fetching session information.
                ScSessionClass s = GlobalSessions.AllGlobalSessions.GetSessionClass(
                    session_->scheduler_id_,
                    session_->linear_index_,
                    session_->random_salt_);

                // Checking that session exists.
                if (null != s)
                {
                    s.socket_num_ = *(UInt64*)(http_request_struct_->socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_NUMBER);
                    s.socket_unique_id_ = *(UInt64*)(http_request_struct_->socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID);
                    s.port_index_ = *(Int32*)(http_request_struct_->socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_PORT_INDEX);

                    switch (protocol_type_)
                    {
                        case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                            break;

                        case MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS:
                            s.ws_opcode_ = *(Byte*)(http_request_struct_->socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Attaches existing session on this HTTP request.
        /// </summary>
        /// <param name="apps_session"></param>
        public void AttachSession(IAppsSession apps_session)
        {
            unsafe
            {
                *session_ = apps_session.InternalSession.session_struct_;
            }
        }

        /// <summary>
        /// Kills existing session.
        /// </summary>
        public UInt32 DestroySession() 
        {
            UInt32 err_code;

            unsafe 
            {
                // Simply generating new session.
                err_code = GlobalSessions.AllGlobalSessions.DestroySession(
                    session_->scheduler_id_,
                    session_->linear_index_,
                    session_->random_salt_);

                // Killing this session by setting invalid unique number and salt.
                session_->Destroy();
            }

            return err_code;
        }

        /// <summary>
        /// Returns unique session number.
        /// </summary>
        public UInt64 UniqueSessionIndex 
        {
            get 
            {
                unsafe { return session_->linear_index_; }
            }
        }

        /// <summary>
        /// Returns session salt.
        /// </summary>
        public UInt64 SessionSalt
        {
            get
            {
                unsafe { return session_->random_salt_; }
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
        /// Gets the is gzip accepted.
        /// </summary>
        /// <value>The is gzip accepted.</value>
        public Boolean IsGzipAccepted
        {
            get
            {
                switch (protocol_type_)
                {
                    case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                    {
                        unsafe
                        {
                            if (null == http_request_struct_)
                                throw new ArgumentException("HTTP request not initialized.");

                            return http_request_struct_->is_gzip_accepted_;
                        }
                    }
                }

                throw new NotSupportedException("Network protocol does not support this method call.");
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
                if (null == http_request_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                return http_request_struct_->ToString();
            }
        }
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 8)]
    /// <summary>
    /// Struct HttpRequestInternal
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct HttpRequestInternal {

        // Request offset.
        public UInt32 request_offset_;
        public UInt32 request_len_bytes_;

        // Content offset.
        public UInt32 content_offset_;
        public UInt32 content_len_bytes_;

        // Resource URI offset.
        public UInt32 uri_offset_;
        public UInt32 uri_len_bytes_;

        // Key-value header offset.
        public UInt32 headers_offset_;
        public UInt32 headers_len_bytes_;

        // Cookie value offset.
        public UInt32 cookies_offset_;
        public UInt32 cookies_len_bytes_;

        // Accept value offset.
        public UInt32 accept_value_offset_;
        public UInt32 accept_value_len_bytes_;

        // Session ID string offset.
        public UInt32 session_string_offset_;
        public UInt32 session_string_len_bytes_;

        // Header offsets.
        public fixed UInt32 header_offsets_[MixedCodeConstants.MAX_PREPARSED_HTTP_REQUEST_HEADERS];
        public fixed UInt32 header_len_bytes_[MixedCodeConstants.MAX_PREPARSED_HTTP_REQUEST_HEADERS];
        public fixed UInt32 header_value_offsets_[MixedCodeConstants.MAX_PREPARSED_HTTP_REQUEST_HEADERS];
        public fixed UInt32 header_value_len_bytes_[MixedCodeConstants.MAX_PREPARSED_HTTP_REQUEST_HEADERS];

        // The num_headers_
        public UInt32 num_headers_;

        // HTTP method.
        public HTTP_METHODS http_method_;

        // Is Gzip accepted.
        public bool is_gzip_accepted_;

        // Socket data pointer.
        public unsafe Byte* socket_data_;

        // Pointer to parameters structure.
        public unsafe Byte* params_info_ptr_;

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
        /// Gets the content raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
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
        /// <returns>Content bytes.</returns>
        public Byte[] GetBodyByteArray_Slow()
        {
            // Checking if there is a content.
            if (content_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] content_bytes = new Byte[(Int32)content_len_bytes_];
            Marshal.Copy((IntPtr)(socket_data_ + content_offset_), content_bytes, 0, (Int32)content_len_bytes_);

            return content_bytes;
        }

        /// <summary>
        /// Gets the request as byte array.
        /// </summary>
        /// <returns>Request bytes.</returns>
        public Byte[] GetRequestByteArray_Slow()
        {
            // Checking if there is a request.
            if (request_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] request_bytes = new Byte[(Int32)request_len_bytes_];
            Marshal.Copy((IntPtr)(socket_data_ + request_offset_), request_bytes, 0, (Int32)request_len_bytes_);

            return request_bytes;
        }

        /// <summary>
        /// Gets the request as UTF8 string.
        /// </summary>
        /// <returns>Request string.</returns>
        public String GetRequestStringUtf8_Slow()
        {
            return new String((SByte*)(socket_data_ + request_offset_), 0, (Int32)request_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetBodyStringUtf8_Slow() {
            // Checking if there is a body.
            if (content_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + content_offset_), 0, (Int32)content_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawMethodAndUri(out IntPtr ptr, out UInt32 sizeBytes)
        {
            // NOTE: Method and URI must always exist.

            ptr = new IntPtr(socket_data_ + request_offset_);
            sizeBytes = uri_offset_ - request_offset_ + uri_len_bytes_;
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public IntPtr GetRawMethodAndUri() {
            // NOTE: Method and URI must always exist.
            return (IntPtr)(socket_data_ + request_offset_);
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
        /// Gets headers as ASCII string.
        /// </summary>
        /// <returns>ASCII string.</returns>
        public String GetHeadersStringUtf8_Slow()
        {
            // Checking if there are cookies.
            if (headers_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + headers_offset_), 0, (Int32)headers_len_bytes_, Encoding.ASCII);
        }

        /// <summary>
        /// Gets the cookies as byte array.
        /// </summary>
        /// <returns>Cookies bytes.</returns>
        public Byte[] GetCookiesByteArray_Slow()
        {
            // Checking if there are cookies.
            if (cookies_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] cookies_bytes = new Byte[(Int32)cookies_len_bytes_];
            Marshal.Copy((IntPtr)(socket_data_ + cookies_offset_), cookies_bytes, 0, (Int32)cookies_len_bytes_);

            return cookies_bytes;
        }

        /// <summary>
        /// Gets cookies as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetCookiesStringUtf8_Slow()
        {
            // Checking if there are cookies.
            if (cookies_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + cookies_offset_), 0, (Int32)cookies_len_bytes_, Encoding.ASCII);
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
        /// Gets the HTTP method.
        /// </summary>
        /// <value>The method.</value>
        public String Method
        {
            get
            {
                // TODO: Pre-calculate the method length!
                unsafe
                {
                    // Calculate number of characters in method.
                    Int32 method_len = 0;
                    Byte* begin = socket_data_ + request_offset_;
                    while (*begin != ' ')
                    {
                        method_len++;
                        begin++;

                        // TODO: Security check.
                        if (method_len > 16)
                        {
                            method_len = 0;
                            break;
                        }
                    }

                    if (method_len > 0)
                        return Marshal.PtrToStringAnsi((IntPtr)(socket_data_ + request_offset_), (Int32)method_len);

                }

                return null;
            }
        }

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>A <see cref="T:System.String" /> containing a fully qualified type name.</returns>
        public override String ToString() {
            return "<h1>HttpMethod: " + http_method_ + "</h1>" + StarcounterConstants.NetworkConstants.CRLF +
                   "<h1>URI: " + Uri + "</h1>" + StarcounterConstants.NetworkConstants.CRLF +
                   "<h1>GZip accepted: " + is_gzip_accepted_ + "</h1>" + StarcounterConstants.NetworkConstants.CRLF +
                   "<h1>Host: " + GetHeaderValue("Host") + "</h1>" + StarcounterConstants.NetworkConstants.CRLF +
                   "<h1>Session string: " + GetSessionString() + "</h1>" + StarcounterConstants.NetworkConstants.CRLF +
                   "<h1>ContentLength: " + content_len_bytes_ + "</h1>" + StarcounterConstants.NetworkConstants.CRLF +
                   "<h1>Body: " + GetBodyStringUtf8_Slow() + "</h1>" + StarcounterConstants.NetworkConstants.CRLF
                   ;
        }
    }
}
