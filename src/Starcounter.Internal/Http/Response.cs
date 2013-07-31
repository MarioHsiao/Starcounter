
using HttpStructs;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Starcounter.Advanced
{
    public class ResponseSchedulerStore
    {
        LinkedList<Response> cachedResponses_ = new LinkedList<Response>();

        public Response GetNewInstance()
        {
            if (cachedResponses_.Count > 0)
            {
                LinkedListNode<Response> respNode = cachedResponses_.Last;
                cachedResponses_.RemoveLast();
                return respNode.Value;
            }

            Response resp = new Response();
            resp.AttachToCache(cachedResponses_);
            return resp;
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
    public partial class Response
    {
/*        /// <summary>
        /// If true, the Uncompressed and/or compressed byte arrays contains content AND header.
        /// If false, the Uncompressed and/or compressed byte arrays contain only content.
        /// </summary>
        /// <remarks>
        /// This is used when constructing responses from texts where the implicit conversion from
        /// string to response does not know what content type to use by default. The default content
        /// type will depend on the request. I.e. if the user agent makes a request with an Accept header
        /// telling the server that it wants "text/html" and the handler return a string, the default
        /// content type will be "text/html". As the implicit conversion from string to Response does
        /// not have fast and easy access to the request context, the response is created without a
        /// header. The header is constructed by Starcounter just 
        /// </remarks>
        private bool _ByteArrayContainsHeader = true;
 */

        // From which cache list this response came from.
        protected LinkedList<Response> responseCacheListFrom_ = null;

        // Node with this response.
        protected LinkedListNode<Response> responseListNode_ = null;

        /// <summary>
        /// Returns the enumerator back to the cache.
        /// </summary>
        internal void ReturnToCache()
        {
            // Returning this enumerator back to the cache.
            responseCacheListFrom_.AddLast(responseListNode_);
        }

        /// <summary>
        /// Should be called when attached to a cache.
        /// </summary>
        /// <param name="fromCache">Cache where this enumerator should be returned.</param>
        internal void AttachToCache(LinkedList<Response> fromCache)
        {
            // Attaching to the specified cache.
            responseCacheListFrom_ = fromCache;

            // Creating cache node from this response.
            responseListNode_ = new LinkedListNode<Response>(this);
        }

        /// <summary>
        /// The _ uncompressed
        /// </summary>
        private byte[] uncompressed_response_ = null;

        /// <summary>
        /// The _ compressed
        /// </summary>
        private byte[] compressed_response_ = null;

        /// <summary>
        /// UncompressedBodyOffset_
        /// </summary>
        public int UncompressedBodyOffset_ = -1;

        /// <summary>
        /// CompressedBodyOffset_
        /// </summary>
        public int CompressedBodyOffset_ = -1;

        /// <summary>
        /// UncompressedBodyLength_
        /// </summary>
        public int UncompressedBodyLength_ = -1;

        /// <summary>
        /// CompressedBodyLength_
        /// </summary>
        public int CompressedBodyLength_ = -1;

        /// <summary>
        /// The uris
        /// </summary>
        public List<string> Uris = new List<string>();

        /// <summary>
        /// The file path
        /// </summary>
        public string FilePath;

        /// <summary>
        /// The file directory
        /// </summary>
        public string FileDirectory;

        /// <summary>
        /// The file name
        /// </summary>
        public string FileName;

        /// <summary>
        /// The file exists
        /// </summary>
        public bool FileExists;

        /// <summary>
        /// The file modified
        /// </summary>
        public DateTime FileModified;

        /// <summary>
        /// As the session id is a fixed size field, the session id of a cached
        /// response can easily be replaced with a current session id.
        /// </summary>
        /// <value>The session id offset.</value>
        /// <remarks>The offset is only valid in the uncompressed response.</remarks>
        public int SessionIdOffset { get; set; }

        #region BodyInjection
        /// <summary>
        /// Used for content injection.
        /// Where to insert the View Model assignment into the html document.
        /// </summary>
        /// <remarks>
        /// The injection offset (injection point) is only valid in the uncompressed
        /// response.
        /// 
        /// Insertion is made at one of these points (in order of priority).
        /// ======================================
        /// 1. The point after the &lt;head&gt; tag.
        /// 2. The point after the &lt;!doctype&gt; tag.
        /// 3. The beginning of the html document.
        /// </remarks>
        /// <value>The script injection point.</value>
        public int ScriptInjectionPoint { get; set; }

        /// <summary>
        /// Used for content injection.
        /// When injecting content into the response, the content length header
        /// needs to be altered. Used together with the ContentLengthLength property.
        /// </summary>
        /// <value>The content length injection point.</value>
        public int ContentLengthInjectionPoint { get; set; } // Used for injection

        /// <summary>
        /// Used for content injection.
        /// When injecting content into the response, the content length header
        /// needs to be altered. The existing previous number of bytes used for the text
        /// integer length value starting at ContentLengthInjectionPoint is stored here.
        /// </summary>
        /// <value>The length of the content length.</value>
        public int ContentLengthLength { get; set; } // Used for injection

        /// <summary>
        /// Used for injecting headers. Specifies where to insert additional
        /// headers that might be needed.
        /// </summary>
        public int HeaderInjectionPoint { get; set; }

        #endregion

        /// <summary>
        /// Indicates if user wants to send custom response.
        /// </summary>
        Boolean customFields_;

        UInt16 statusCode_;

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        public UInt16 StatusCode
        {
            get
            {
                if (0 == statusCode_)
                {
                    unsafe
                    {
                        if (null == http_response_struct_)
                            throw new ArgumentException("HTTP response not initialized.");

                        statusCode_ = http_response_struct_->status_code_;
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

        String statusDescription_;

        /// <summary>
        /// Status description.
        /// </summary>
        public String StatusDescription
        {
            get
            {
                if (null == statusDescription_)
                {
                    unsafe
                    {
                        if (null == http_response_struct_)
                            throw new ArgumentException("HTTP response not initialized.");

                        statusDescription_ = http_response_struct_->GetStatusDescription();
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

        /// <summary>
        /// Body string.
        /// </summary>
        public Object Content
        {
            get
            {
                if (null != Hypermedia)
                    return Hypermedia;

                if (null != bodyBytes_)
                    return bodyBytes_;

                if (null == bodyString_)
                    bodyString_ = GetBodyStringUtf8_Slow();

                return bodyString_;
            }

            set
            {
                if (value is String) {
                    bodyString_ = (String) value;
                } else if (value is Byte[]) {
                    bodyBytes_ = (Byte[]) value;
                } else if (value is IHypermedia) {
                    _Hypermedia = (IHypermedia) value;
                } else {
                    throw new ArgumentException("Wrong content type assigned!");
                }

                customFields_ = true;
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

        private IHypermedia _Hypermedia;

        /// <summary>
        /// The response can be contructed in one of the following ways:
        /// 
        /// 1) using a complete binary HTTP response
        /// in the Uncompressed or Compressed propery (this includes the header)
        /// 
        /// 2) using a IHypermedia object in the Hypermedia property
        /// 
        /// 3) using the BodyString property (does not include the header)
        /// 
        /// 4) using the BodyBytes property (does not include the header)
        /// </summary>
        public IHypermedia Hypermedia {
            get {
                return _Hypermedia;
            }
            set {
                customFields_ = true;
                _Hypermedia = value;
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

        String setCookiesString_;

        /// <summary>
        /// Set-Cookie string.
        /// </summary>
        public String SetCookie
        {
            get
            {
                if (null == setCookiesString_)
                    setCookiesString_ = this["Set-Cookie"];

                return setCookiesString_;
            }

            set
            {
                customFields_ = true;
                setCookiesString_ = value;
            }
        }

        /// <summary>
        /// Resets all custom fields.
        /// </summary>
        public void ResetAllCustomFields()
        {
            customFields_ = false;

            setCookiesString_ = null;
            headersString_ = null;
            bodyString_ = null;
            contentType_ = null;
            contentEncoding_ = null;
            statusDescription_ = null;
            statusCode_ = 0;
            AppsSession = null;
        }

        /// <summary>
        /// Constructs Response from fields that are set.
        /// </summary>
        public void ConstructFromFields()
        {
            // Checking if we have a custom response.
            if (!customFields_)
                return;

            byte[] bytes = bodyBytes_;
            if (_Hypermedia != null) {
                var mimetype = http_request_.PreferredMimeType;
                bytes = _Hypermedia.AsMimeType(mimetype);
                if (bytes == null) {
                    // The preferred requested mime type was not supported, try to see if there are
                    // other options.
                    IEnumerator<MimeType> secondaryChoices = http_request_.PreferredMimeTypes;
                    secondaryChoices.MoveNext(); // The first one is already accounted for
                    while (bytes == null && secondaryChoices.MoveNext()) {
                        bytes = _Hypermedia.AsMimeType(secondaryChoices.Current);
                    }
                    if (bytes == null) {
                        // None of the requested mime types were supported.
                        // We will have to respond with a "Not Acceptable" message.
                        statusCode_ = 406;
                    }
                }
                // We have our precious bytes. Let's wrap them up in a response.
            }


            String str = "HTTP/1.1 ";
            
            if (statusCode_ > 0)
            {
                str += statusCode_;
                str += " ";

                // Checking if Status Description is set.
                if (null != statusDescription_)
                    str += statusDescription_ + StarcounterConstants.NetworkConstants.CRLF;
                else
                    str += "OK" + StarcounterConstants.NetworkConstants.CRLF;
            }
            else
            {
                // Checking if Status Description is set.
                if (null != statusDescription_)
                    str += "200 " + statusDescription_ + StarcounterConstants.NetworkConstants.CRLF;
                else
                    str += "200 OK" + StarcounterConstants.NetworkConstants.CRLF;
            }

            str += "Server: SC" + StarcounterConstants.NetworkConstants.CRLF;

            if (null != headersString_)
                str += headersString_;

            if (null != contentType_)
                str += "Content-Type: " + contentType_ + StarcounterConstants.NetworkConstants.CRLF;

            if (null != contentEncoding_)
                str += "Content-Encoding: " + contentEncoding_ + StarcounterConstants.NetworkConstants.CRLF;

            if (null != setCookiesString_)
                str += "Set-Cookie: " + setCookiesString_ + StarcounterConstants.NetworkConstants.CRLF;

            if (null != AppsSession)
                str += "Location: /scsssn/" + AppsSession.ToAsciiString() + StarcounterConstants.NetworkConstants.CRLF;

            if (null != bodyString_)
            {
                if (null != bytes)
                    throw new ArgumentException("Either body string, body bytes or hypermedia can be set for Response.");

                str += "Content-Length: " + bodyString_.Length + StarcounterConstants.NetworkConstants.CRLF;
                str += StarcounterConstants.NetworkConstants.CRLF;

                // Adding the body.
                str += bodyString_;

                // Finally setting the uncompressed bytes.
                Uncompressed = Encoding.UTF8.GetBytes(str);
            }
            else if (null != bytes)
            {
                str += "Content-Length: " + bytes.Length + StarcounterConstants.NetworkConstants.CRLF;
                str += StarcounterConstants.NetworkConstants.CRLF;

                Byte[] headersBytes = Encoding.UTF8.GetBytes(str);

                // Adding the body.
                Byte[] responseBytes = new Byte[headersBytes.Length + bytes.Length];

                System.Buffer.BlockCopy(headersBytes, 0, responseBytes, 0, headersBytes.Length);
                System.Buffer.BlockCopy(bytes, 0, responseBytes, headersBytes.Length, bytes.Length);

                // Finally setting the uncompressed bytes.
                Uncompressed = responseBytes;
            }
            else {
                str += "Content-Length: 0" + StarcounterConstants.NetworkConstants.CRLF;
                str += StarcounterConstants.NetworkConstants.CRLF;

                // Finally setting the uncompressed bytes.
                Uncompressed = Encoding.UTF8.GetBytes(str);
            }

            customFields_ = false;
        }

        /// <summary>
        /// Checks if status code is resembling success.
        /// </summary>
        public Boolean IsSuccessStatusCode
        {
            get
            {
                UInt16 statusCode = StatusCode;
                return (statusCode >= 200) && (statusCode <= 226);
            }
        }

        /// <summary>
        /// The number of bytes containing the http header in the uncompressed response. This is also
        /// the offset of the first byte of the content.
        /// </summary>
        /// <value>The length of the header.</value>
        public Int32 HeadersLength { get; set; }

        /// <summary>
        /// The number of bytes of the content (i.e. the resource) of the uncompressed http response.
        /// </summary>
        /// <value>The length of the content.</value>
        public Int32 ContentLength
        {
            get
            {
                if (UncompressedBodyLength_ > 0)
                    return UncompressedBodyLength_;

                unsafe
                {
                    if (null == http_response_struct_)
                        throw new ArgumentException("HTTP response not initialized.");

                    return http_response_struct_->content_len_bytes_;
                }
            }
            set
            {
                UncompressedBodyLength_ = value;
            }
        }

        /// <summary>
        /// The uncompressed cached response
        /// </summary>
        /// <value>The uncompressed.</value>
        public Byte[] Uncompressed
        {
            get
            {
                return uncompressed_response_;
            }
            set
            {
                uncompressed_response_ = value;
            }
        }

        /// <summary>
        /// Getting full response length.
        /// </summary>
        public Int32 ResponseLength
        {
            get
            {
                if (uncompressed_response_ != null)
                    return uncompressed_response_.Length;

                unsafe
                {
                    if (null == http_response_struct_)
                        throw new ArgumentException("HTTP response not initialized.");

                    return (Int32)http_response_struct_->response_len_bytes_;
                }
            }
        }

        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Byte[][].</returns>
        public byte[] GetBytes(Request request)
        {
            // TODO: Re-enable once uncompressed resources are fixed.
            if (/*request.IsGzipAccepted && */Compressed != null)
                return Compressed;

            return Uncompressed;
        }

        /// <summary>
        /// The compressed (gzip) cached resource
        /// </summary>
        /// <value>The compressed.</value>
        public byte[] Compressed
        {
            get
            {
                if (!WorthWhileCompressing)
                    return uncompressed_response_;
                else
                    return compressed_response_;
            }
            set
            {
                compressed_response_ = value;
            }
        }

        /// <summary>
        /// The _ worth while compressing
        /// </summary>
        private bool _WorthWhileCompressing = true;

        /// <summary>
        /// If false, it was found that the compressed version of the response was
        /// insignificantly smaller, equally large or even larger than the original version.
        /// </summary>
        /// <value><c>true</c> if [worth while compressing]; otherwise, <c>false</c>.</value>
        public bool WorthWhileCompressing
        {
            get
            {
                return _WorthWhileCompressing;
            }
            set
            {
                _WorthWhileCompressing = value;
            }
        }

        /// <summary>
        /// Internal structure with HTTP response information.
        /// </summary>
        unsafe HttpResponseInternal* http_response_struct_ = null;

        /// <summary>
        /// Direct pointer to session data.
        /// </summary>
        unsafe ScSessionStruct* session_;

        /// <summary>
        /// Internal network data stream.
        /// </summary>
        public INetworkDataStream data_stream_;

        /// <summary>
        /// Indicates if this Response is internally constructed from Apps.
        /// </summary>
        Boolean is_internal_response_ = false;

        /// <summary>
        /// Parses internal HTTP response.
        /// </summary>
        [DllImport("schttpparser.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_parse_http_response(
            Byte* response_buf,
            UInt32 response_size_bytes,
            Byte* out_http_response);

        /// <summary>
        /// Initializes a new instance of the <see cref="Response" /> class.
        /// </summary>
        public Response()
        {
            HeaderInjectionPoint = -1;
        }

        /// <summary>
        /// Reference to corresponding HTTP request.
        /// </summary>
        Request http_request_ = null;

        // Type of network protocol.
        //MixedCodeConstants.NetworkProtocolType protocol_type_;

        /// <summary>
        /// A response may be associated with a request. If a response is created without a content type,
        /// the primary content type of the Accept header in the request will be assumed.
        /// </summary>
        public Request Request {
            get {
                return http_request_;
            }
            set {
                http_request_ = value;
            }
        }

        /// <summary>
        /// Underlying memory stream.
        /// </summary>
        MemoryStream mem_stream_ = null;

        /// <summary>
        /// Setting the response buffer.
        /// </summary>
        /// <param name="response_buf"></param>
        /// <param name="response_len_bytes"></param>
        public void SetResponseBuffer(Byte[] response_buf, MemoryStream mem_stream, Int32 response_len_bytes)
        {
            uncompressed_response_ = response_buf;

            mem_stream_ = mem_stream;

            unsafe
            {
                // Checking if have not allocated anything yet.
                if (null != http_response_struct_->socket_data_)
                {
                    // Releasing internal resources here.
                    BitsAndBytes.Free((IntPtr)http_response_struct_->socket_data_);
                    http_response_struct_->socket_data_ = null;
                }

                // Setting the response data pointer.
                http_response_struct_->socket_data_ = (Byte*) BitsAndBytes.Alloc(response_buf.Length);

                // Copying HTTP response data.
                fixed (Byte* fixed_response_buf = response_buf)
                    BitsAndBytes.MemCpy(http_response_struct_->socket_data_, fixed_response_buf, (UInt32)response_buf.Length);

                // Setting the final response length.
                http_response_struct_->response_len_bytes_ = (UInt32)response_len_bytes;
            }
        }

        /// <summary>
        /// Parses the HTTP response from uncompressed buffer.
        /// </summary>
        public void ParseResponseFromUncompressed()
        {
            if (uncompressed_response_ != null)
                TryParseResponse(uncompressed_response_, 0, uncompressed_response_.Length, true);
        }

        /// <summary>
        /// Parses HTTP response from buffer.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="bufLenBytes"></param>
        /// <param name="complete"></param>
        public void TryParseResponse(Byte[] buf, Int32 offsetBytes, Int32 bufLenBytes, Boolean complete)
        {
            UInt32 err_code;
            unsafe
            {
                // First destroying.
                Destroy();

                // Indicating that we internally constructing Response.
                is_internal_response_ = true;

                // Allocating space just for response structure.
                http_response_struct_ = (HttpResponseInternal*) BitsAndBytes.Alloc(sizeof(HttpResponseInternal));
                http_response_struct_->socket_data_ = null;

                // Checking if we have a complete response.
                if (complete)
                {
                    // Setting the internal buffer.
                    SetResponseBuffer(buf, null, bufLenBytes);

                    // Executing HTTP response parser and getting Response structure as result.
                    err_code = sc_parse_http_response(http_response_struct_->socket_data_, (UInt32)bufLenBytes, (Byte*)http_response_struct_);
                }
                else
                {
                    fixed (Byte* pbuf = buf)
                    {
                        // Executing HTTP response parser and getting Response structure as result.
                        err_code = sc_parse_http_response(pbuf + offsetBytes, (UInt32)bufLenBytes, (Byte*)http_response_struct_);
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

                // NOTE: No internal sessions support.
                session_ = null;

                //protocol_type_ = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Response" /> class.
        /// </summary>
        /// <param name="buf">The buf.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public Response(Byte[] buf, Int32 offset, Int32 lenBytes, Request httpRequest = null, Boolean complete = true)
        {
            unsafe
            {
                // Parsing given buffer.
                TryParseResponse(buf, offset, lenBytes, complete);

                // Setting corresponding HTTP request.
                http_request_ = httpRequest;
            }
        }

        /// <summary>
        /// Destroys the instance of Response.
        /// </summary>
        public void Destroy()
        {
            unsafe
            {
                // Checking if already destroyed.
                if (http_response_struct_ == null)
                    return;

                // Closing the memory stream if any.
                if (null != mem_stream_)
                {
                    mem_stream_.Close();
                    mem_stream_ = null;
                }

                // Checking if we have constructed this Response
                // internally in Apps or externally in Gateway.
                if (is_internal_response_)
                {
                    // Checking if have not allocated anything yet.
                    if (null != http_response_struct_->socket_data_)
                    {
                        // Releasing response data.
                        BitsAndBytes.Free((IntPtr)http_response_struct_->socket_data_);
                        http_response_struct_->socket_data_ = null;
                    }

                    // Releasing internal resources here.
                    BitsAndBytes.Free((IntPtr)http_response_struct_);
                }
                else
                {
                    // Releasing data stream resources like chunks, etc.
                    data_stream_.Destroy();
                }

                http_response_struct_ = null;
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
                return (http_response_struct_ == null) && (session_ == null);
            }
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        ~Response()
        {
            Destroy();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Response" /> class.
        /// </summary>
        /// <param name="chunk_data">The chunk_data.</param>
        /// <param name="single_chunk">The single_chunk.</param>
        /// <param name="chunk_index">The chunk_index.</param>
        /// <param name="http_response_begin">The http_response_begin.</param>
        /// <param name="socket_data">The socket_data.</param>
        /// <param name="data_stream">The data_stream.</param>
        public unsafe Response(
            Byte* chunk_data,
            Boolean single_chunk,
            UInt32 chunk_index,
            UInt16 handler_id,
            Byte* http_response_begin,
            Byte* socket_data,
            INetworkDataStream data_stream)
        {
            http_response_struct_ = (HttpResponseInternal*) http_response_begin;
            session_ = (ScSessionStruct*)(socket_data + MixedCodeConstants.SOCKET_DATA_OFFSET_SESSION);
            http_response_struct_->socket_data_ = socket_data;
            data_stream_ = data_stream;
            data_stream_.Init(chunk_data, single_chunk, chunk_index);
            handlerId_ = handler_id;
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
        /// The needs script injection_
        /// </summary>
        bool needsScriptInjection_ = false;

        /// <summary>
        /// Gets or sets a value indicating whether [needs script injection].
        /// </summary>
        /// <value><c>true</c> if [needs script injection]; otherwise, <c>false</c>.</value>
        public bool NeedsScriptInjection
        {
            get { return needsScriptInjection_; }
            set { needsScriptInjection_ = value; }
        }

        /// <summary>
        /// Linear index for this handler.
        /// </summary>
        UInt16 handlerId_;
        public UInt16 HandlerId
        {
            get { return handlerId_; }
        }

        /// <summary>
        /// The is app view_
        /// </summary>
        bool isAppView_ = false;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is app view.
        /// </summary>
        /// <value><c>true</c> if this instance is app view; otherwise, <c>false</c>.</value>
        public bool IsAppView
        {
            get { return isAppView_; }
            set { isAppView_ = value; }
        }

        /// <summary>
        /// The gzip advisable_
        /// </summary>
        bool gzipAdvisable_ = false;

        /// <summary>
        /// Gets or sets the gzip advisable.
        /// </summary>
        /// <value>The gzip advisable.</value>
        public Boolean GzipAdvisable
        {
            get { return gzipAdvisable_; }
            set { gzipAdvisable_ = value; }
        }

        /// <summary>
        /// Gets or sets the view model.
        /// </summary>
        /// <value>The view model.</value>
        public byte[] ViewModel { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance can use static response.
        /// </summary>
        /// <value><c>true</c> if this instance can use static response; otherwise, <c>false</c>.</value>
        public bool CanUseStaticResponse
        {
            get
            {
                return ViewModel == null;
            }
        }

        /// <summary>
        /// Gets the raw headers length.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public UInt32 GetHeadersLength()
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                return http_response_struct_->GetHeadersLength();
            }
        }

        /// <summary>
        /// Gets the whole response size.
        /// </summary>
        public UInt32 GetResponseLength()
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                return http_response_struct_->response_len_bytes_;
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
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                http_response_struct_->GetBodyRaw(out ptr, out sizeBytes);
            }
        }

        /// <summary>
        /// Gets the content as byte array.
        /// </summary>
        /// <returns>content bytes.</returns>
        public Byte[] GetBodyByteArray_Slow()
        {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                return http_response_struct_->GetBodyByteArray_Slow();
            }
        }

        /// <summary>
        /// Gets the response as byte array.
        /// </summary>
        /// <returns>Response bytes.</returns>
        public Byte[] ResponseBytes
        {
            get { return uncompressed_response_; }
        }

        /// <summary>
        /// Byte array of the response.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static implicit operator Byte[](Response r)
        {
            return r.ResponseBytes;
        }

        /// <summary>
        /// Gets body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetBodyStringUtf8_Slow()
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                return http_response_struct_->GetBodyStringUtf8_Slow();
            }
        }

        /// <summary>
        /// Gets body bytes.
        /// </summary>
        public Byte[] GetBodyBytes_Slow()
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP request not initialized.");

                return http_response_struct_->GetBodyByteArray_Slow();
            }
        }

        /// <summary>
        /// Gets headers as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetHeadersStringUtf8_Slow()
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                return http_response_struct_->GetHeadersStringUtf8_Slow();
            }
        }

        /// <summary>
        /// Gets cookies as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetCookiesStringUtf8_Slow()
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                return http_response_struct_->GetCookiesStringUtf8_Slow();
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
        /// Gets the raw response.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetResponseRaw(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                http_response_struct_->GetResponseRaw(out ptr, out sizeBytes);
            }
        }

        /// <summary>
        /// Gets response as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetResponseStringUtf8_Slow()
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                return http_response_struct_->GetResponseStringUtf8_Slow();
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
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                http_response_struct_->GetRawHeaders(out ptr, out sizeBytes);
            }
        }

        /// <summary>
        /// Gets the raw cookies.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                http_response_struct_->GetRawSetCookies(out ptr, out sizeBytes);
            }
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
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                http_response_struct_->GetRawSessionString(out ptr, out sizeBytes);
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
            unsafe
            {
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                http_response_struct_->GetHeaderValue(key, out ptr, out sizeBytes);
            }
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
                unsafe
                {
                    if (null == http_response_struct_)
                        throw new ArgumentException("HTTP response not initialized.");

                    return http_response_struct_->GetHeaderValue(name);
                }
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
                    return Request.INVALID_APPS_UNIQUE_SESSION_INDEX != (session_->linear_index_);
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
                    return session_->linear_index_;
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
                    return session_->random_salt_;
                }
            }
        }

        /// <summary>
        /// Getting internal Apps session.
        /// </summary>
        public ScSessionClass AppsSession { get; set; }

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
                if (null == http_response_struct_)
                    throw new ArgumentException("HTTP response not initialized.");

                return http_response_struct_->ToString();
            }
        }
    }

    /// <summary>
    /// Struct HttpResponseInternal
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HttpResponseInternal
    {
        // Response offset.
        public UInt32 response_offset_;
        public UInt32 response_len_bytes_;

        // Content offset.
        public UInt32 content_offset_;
        public Int32 content_len_bytes_;

        // Key-value header offset.
        public UInt32 headers_offset_;
        public UInt32 headers_len_bytes_;

        // Cookie value offset.
        public UInt32 set_cookies_offset_;
        public UInt32 set_cookies_len_bytes_;

        // Session ID string offset.
        public UInt32 session_string_offset_;
        public UInt32 session_string_len_bytes_;

        // Header offsets.
        public fixed UInt32 header_offsets_[MixedCodeConstants.MAX_PREPARSED_HTTP_RESPONSE_HEADERS];
        public fixed UInt32 header_len_bytes_[MixedCodeConstants.MAX_PREPARSED_HTTP_RESPONSE_HEADERS];
        public fixed UInt32 header_value_offsets_[MixedCodeConstants.MAX_PREPARSED_HTTP_RESPONSE_HEADERS];
        public fixed UInt32 header_value_len_bytes_[MixedCodeConstants.MAX_PREPARSED_HTTP_RESPONSE_HEADERS];

        // The num_headers_
        public UInt32 num_headers_;

        // HTTP response status code.
        public UInt16 status_code_;

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
        public String GetResponseStringUtf8_Slow()
        {
            return new String((SByte*)(socket_data_ + response_offset_), 0, (Int32)response_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the raw parameters structure.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public IntPtr GetRawParametersInfo()
        {
            return (IntPtr)(socket_data_ + MixedCodeConstants.SOCKET_DATA_OFFSET_PARAMS_INFO);
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
        public Byte[] GetBodyByteArray_Slow()
        {
            // Checking if there is a content.
            if (content_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] content_bytes = new Byte[content_len_bytes_];
            Marshal.Copy((IntPtr)(socket_data_ + content_offset_), content_bytes, 0, content_len_bytes_);

            return content_bytes;
        }

        /// <summary>
        /// Gets body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetBodyStringUtf8_Slow()
        {
            // Checking if there is a content.
            if (content_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + content_offset_), 0, content_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the cookies as byte array.
        /// </summary>
        /// <returns>Cookies bytes.</returns>
        public Byte[] GetCookiesByteArray_Slow()
        {
            // Checking if there are cookies.
            if (set_cookies_len_bytes_ <= 0)
                return null;

            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] cookies_bytes = new Byte[(Int32)set_cookies_len_bytes_];
            Marshal.Copy((IntPtr)(socket_data_ + set_cookies_offset_), cookies_bytes, 0, (Int32)set_cookies_len_bytes_);

            return cookies_bytes;
        }

        /// <summary>
        /// Gets cookies as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetCookiesStringUtf8_Slow()
        {
            // Checking if there are cookies.
            if (set_cookies_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + set_cookies_offset_), 0, (Int32)set_cookies_len_bytes_, Encoding.ASCII);
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
        public String GetHeadersStringUtf8_Slow()
        {
            // Checking if there are cookies.
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
        /// Gets the raw set cookies.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawSetCookies(out IntPtr ptr, out UInt32 sizeBytes)
        {
            if (set_cookies_len_bytes_ <= 0)
                ptr = IntPtr.Zero;
            else
                ptr = new IntPtr(socket_data_ + set_cookies_offset_);

            sizeBytes = set_cookies_len_bytes_;
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
            unsafe
            {
                fixed (UInt32* header_offsets = header_offsets_,
                    header_len_bytes = header_len_bytes_,
                    header_value_offsets = header_value_offsets_,
                    header_value_len_bytes = header_value_len_bytes_)
                {
                    // Going through all headers.
                    for (Int32 i = 0; i < num_headers_; i++)
                    {
                        Boolean found = true;

                        // Checking that length is correct.
                        if (headerName.Length == header_len_bytes[i])
                        {
                            // Going through all characters in current header.
                            for (Int32 k = 0; k < headerName.Length; k++)
                            {
                                // Comparing each character.
                                if (((Byte)headerName[k]) != *(socket_data_ + header_offsets[i] + k))
                                {
                                    found = false;
                                    break;
                                }
                            }

                            if (found)
                            {
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
        public String GetHeaderValue(String headerName)
        {
            unsafe
            {
                fixed (UInt32* header_offsets = header_offsets_,
                    header_len_bytes = header_len_bytes_,
                    header_value_offsets = header_value_offsets_,
                    header_value_len_bytes = header_value_len_bytes_)
                {
                    // Going through all headers.
                    for (Int32 i = 0; i < num_headers_; i++)
                    {
                        Boolean found = true;

                        // Checking that length is correct.
                        if (headerName.Length == header_len_bytes[i])
                        {
                            // Going through all characters in current header.
                            for (Int32 k = 0; k < headerName.Length; k++)
                            {
                                // Comparing each character.
                                if (((Byte)headerName[k]) != *(socket_data_ + header_offsets[i] + k))
                                {
                                    found = false;
                                    break;
                                }
                            }

                            if (found)
                            {
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
    }
}
