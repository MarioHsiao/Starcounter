
using HttpStructs;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace Starcounter.Advanced
{
    /// <summary>
    /// The Starcounter Web Server caches resources as complete http responses.
    /// As the exact same response can often not be used, the cashed response also
    /// include useful offsets and injection points to facilitate fast transitions
    /// to individual http responses. The cached response is also used to cache resources
    /// (compressed or uncompressed content) even if the consumer wants to embedd the content
    /// in a new http response.
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// The _ uncompressed
        /// </summary>
        private byte[] UncompressedResponse_ = null;

        /// <summary>
        /// The _ compressed
        /// </summary>
        private byte[] CompressedResponse_ = null;

        public int UncompressedContentOffset_ = -1;
        public int CompressedContentOffset_ = -1;

        public int UncompressedContentLength_ = -1;
        public int CompressedContentLength_ = -1;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponse" /> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <exception cref="System.Exception"></exception>
        public HttpResponse(string content) {
            throw new Exception();
        }

        #region ContentInjection
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
        /// The number of bytes containing the http header in the uncompressed response. This is also
        /// the offset of the first byte of the content.
        /// </summary>
        /// <value>The length of the header.</value>
        public int HeadersLength { get; set; }

        /// <summary>
        /// The number of bytes of the content (i.e. the resource) of the uncompressed http response.
        /// </summary>
        /// <value>The length of the content.</value>
        public int ContentLength
        {
            get
            {
                if (UncompressedContentLength_ > 0)
                    return UncompressedContentLength_;

                unsafe { return (Int32)http_response_struct_->content_len_bytes_; }
            }
            set
            {
                UncompressedContentLength_ = value;

                unsafe
                { 
                    if (http_response_struct_ != null)
                        http_response_struct_->content_len_bytes_ = (UInt32)value;
                }
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
                return UncompressedResponse_;
            }
            set
            {
                UncompressedResponse_ = value;
            }
        }

        /// <summary>
        /// Getting full response length.
        /// </summary>
        public Int32 ResponseLength
        {
            get
            {
                unsafe
                {
                    if (http_response_struct_ != null)
                        return (Int32)http_response_struct_->response_len_bytes_;
                }

                if (UncompressedResponse_ != null)
                    return UncompressedResponse_.Length;

                return 0;
            }
        }

        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Byte[][].</returns>
        public byte[] GetBytes(HttpRequest request)
        {
            if (request.IsGzipAccepted && Compressed != null)
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
                    return UncompressedResponse_;
                else
                    return CompressedResponse_;
            }
            set
            {
                CompressedResponse_ = value;
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
        unsafe HttpResponseInternal* http_response_struct_;

        /// <summary>
        /// Internal structure with HTTP response information.
        /// </summary>
        public unsafe HttpResponseInternal* HttpResponseInternalStruct
        {
            get { return http_response_struct_; }
        }

        /// <summary>
        /// Direct pointer to session data.
        /// </summary>
        unsafe ScSessionStruct* session_;

        /// <summary>
        /// Internal network data stream.
        /// </summary>
        public INetworkDataStream data_stream_;

        /// <summary>
        /// Indicates if this HttpResponse is internally constructed from Apps.
        /// </summary>
        Boolean isInternalResponse = false;

        /// <summary>
        /// Parses internal HTTP response.
        /// </summary>
        [DllImport("HttpParser.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_parse_http_response(
            Byte* response_buf,
            UInt32 response_size_bytes,
            Byte* out_http_response);

        /// Initializes a new instance of the <see cref="HttpResponse" /> class.
        /// </summary>
        public HttpResponse() {
            HeaderInjectionPoint = -1;
        }

        // Pointer to corresponding HTTP request.
        HttpRequest httpRequest_ = null;

        // Setting corresponding HTTP request.
        public void SetHttpRequest(HttpRequest httpRequest)
        {
            httpRequest_ = httpRequest;
        }

        // Underlying memory stream.
        MemoryStream mem_stream_ = null;

        // Setting the response bytes.
        public void SetResponseBytes(MemoryStream mem_stream, Int32 response_len_bytes)
        {
            mem_stream_ = mem_stream;
            UncompressedResponse_ = mem_stream_.GetBuffer();

            SetResponseBytes(UncompressedResponse_, response_len_bytes);
        }

        // Setting the response bytes.
        public void SetResponseBytes(Byte[] response_buf, Int32 response_len_bytes)
        {
            UncompressedResponse_ = response_buf;

            unsafe
            {
                // Setting the final response length.
                http_response_struct_->response_len_bytes_ = (UInt32)response_len_bytes;

                fixed (Byte* pbuf = response_buf)
                {
                    // Setting the response data pointer.
                    http_response_struct_->socket_data_ = pbuf;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponse" /> class.
        /// </summary>
        /// <param name="buf">The buf.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public HttpResponse(Byte[] buf, Int32 lenBytes, HttpRequest httpRequest = null)
        {
            UInt32 err_code;
            unsafe
            {
                // Setting uncompressed response reference.
                UncompressedResponse_ = buf;

                // Allocating space for HttpResponse contents and structure.
                /*Byte* response_native_buf = (Byte*)BitsAndBytes.Alloc(buf.Length + sizeof(HttpResponseInternal));
                fixed (Byte* fixed_buf = buf)
                {
                    // Copying HTTP response data.
                    BitsAndBytes.MemCpy(response_native_buf, fixed_buf, (UInt32)buf.Length);
                }

                // Pointing to HTTP response structure.
                http_response_struct_ = (HttpResponseInternal*)(response_native_buf + buf.Length);
                
                // Setting the response data pointer.
                http_response_struct_->socket_data_ = response_native_buf;
                */

                Byte* response_native_buf = (Byte*)BitsAndBytes.Alloc(sizeof(HttpResponseInternal));

                // Pointing to HTTP response structure.
                http_response_struct_ = (HttpResponseInternal*)response_native_buf;

                fixed (Byte* pbuf = buf)
                {
                    // Setting the response data pointer.
                    http_response_struct_->socket_data_ = pbuf;

                    // Indicating that we internally constructing HttpResponse.
                    isInternalResponse = true;

                    // NOTE: No internal sessions support.
                    session_ = null;

                    // NOTE: No internal data stream support:
                    // Simply on which socket to send this "response"?

                    // Executing HTTP response parser and getting HttpResponse structure as result.
                    err_code = sc_parse_http_response(pbuf, (UInt32)lenBytes, (Byte*)http_response_struct_);
                }

                // Checking if any error occurred.
                if (err_code != 0)
                {
                    // Freeing memory etc.
                    Destroy();

                    throw ErrorCode.ToException(err_code);
                }

                // Setting corresponding HTTP request.
                httpRequest_ = httpRequest;
            }
        }

        /// <summary>
        /// Destroys the instance of HttpResponse.
        /// </summary>
        public void Destroy()
        {
            unsafe
            {
                // Checking if we have underlying memory stream.
                if (mem_stream_ != null)
                    mem_stream_.Close();

                // Checking if already destroyed.
                if (http_response_struct_ == null)
                    return;

                // Checking if we have constructed this HttpResponse
                // internally in Apps or externally in Gateway.
                if (isInternalResponse)
                {
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
        /// Called when GC destroys this object.
        /// </summary>
        ~HttpResponse()
        {
            // TODO: Consult what is better for Apps auto-destructor or manual call to Destroy.
            Destroy();
        }

        // Constructor.
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponse" /> class.
        /// </summary>
        /// <param name="chunk_data">The chunk_data.</param>
        /// <param name="single_chunk">The single_chunk.</param>
        /// <param name="chunk_index">The chunk_index.</param>
        /// <param name="http_response_begin">The http_response_begin.</param>
        /// <param name="socket_data">The socket_data.</param>
        /// <param name="data_stream">The data_stream.</param>
        public unsafe HttpResponse(
            Byte* chunk_data,
            Boolean single_chunk,
            UInt32 chunk_index,
            UInt16 handler_id,
            Byte* http_response_begin,
            Byte* socket_data,
            INetworkDataStream data_stream)
        {
            http_response_struct_ = (HttpResponseInternal*)http_response_begin;
            session_ = (ScSessionStruct*)(socket_data + MixedCodeConstants.SOCKET_DATA_OFFSET_SESSION);
            http_response_struct_->socket_data_ = socket_data;
            data_stream_ = data_stream;
            data_stream_.Init(chunk_data, single_chunk, chunk_index);
            handlerId_ = handler_id;
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

        // TODO
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

        // TODO
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
            unsafe { return http_response_struct_->GetHeadersLength(); }
        }

        /// <summary>
        /// Gets the content raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetContentRaw(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_response_struct_->GetContentRaw(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the content as byte array.
        /// </summary>
        /// <returns>content bytes.</returns>
        public Byte[] GetContentByteArray_Slow()
        {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            unsafe { return http_response_struct_->GetContentByteArray_Slow(); }
        }

        /// <summary>
        /// Gets the response as byte array.
        /// </summary>
        /// <returns>Response bytes.</returns>
        /*public Byte[] GetResponseByteArray_Slow()
        {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            unsafe { return http_response_struct_->GetResponseByteArray_Slow(); }
        }*/

        /// <summary>
        /// Gets the response as byte array.
        /// </summary>
        /// <returns>Response bytes.</returns>
        public Byte[] ResponseBytes
        {
            get { return UncompressedResponse_; }
        }

        /// <summary>
        /// Byte array of the response.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static implicit operator Byte[](HttpResponse r)
        {
            return r.ResponseBytes;
        }

        /// <summary>
        /// Gets the content as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetContentStringUtf8_Slow()
        {
            unsafe { return http_response_struct_->GetContentStringUtf8_Slow(); }
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
            unsafe { http_response_struct_->GetResponseRaw(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw headers.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_response_struct_->GetRawHeaders(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw cookies.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_response_struct_->GetRawSetCookies(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw session string.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_response_struct_->GetRawSessionString(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw header.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeader(byte[] key, out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_response_struct_->GetHeaderValue(key, out ptr, out sizeBytes); }
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
                unsafe { return http_response_struct_->GetHeaderValue(name); }
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
        /// Checks if HTTP response already has session.
        /// </summary>
        public Boolean HasSession
        {
            get
            {
                unsafe
                {
                    return INVALID_APPS_UNIQUE_SESSION_NUMBER != (session_->apps_unique_session_index_);
                }
            }
        }

        // New session creation indicator.
        Boolean newSession_ = false;

        /// <summary>
        /// Indicates if new session was created.
        /// </summary>
        public Boolean HasNewSession
        {
            get
            {
                return newSession_;
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
                    return session_->apps_unique_session_index_;
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
                    return session_->apps_session_salt_;
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
            unsafe { return http_response_struct_->ToString(); }
        }
    }

    /// <summary>
    /// Struct HttpResponseInternal
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HttpResponseInternal
    {
        /// <summary>
        /// Response offset.
        /// </summary>
        public UInt32 response_offset_;

        /// <summary>
        /// The response_len_bytes_
        /// </summary>
        public UInt32 response_len_bytes_;

        /// <summary>
        /// Content offset.
        /// </summary>
        public UInt32 content_offset_;

        /// <summary>
        /// The content_len_bytes_
        /// </summary>
        public UInt32 content_len_bytes_;

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
        public UInt32 set_cookies_offset_;

        /// <summary>
        /// The cookies_len_bytes_
        /// </summary>
        public UInt32 set_cookies_len_bytes_;

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
        public fixed UInt32 header_offsets_[MixedCodeConstants.MAX_PREPARSED_HTTP_RESPONSE_HEADERS];

        /// <summary>
        /// The header_len_bytes_
        /// </summary>
        public fixed UInt32 header_len_bytes_[MixedCodeConstants.MAX_PREPARSED_HTTP_RESPONSE_HEADERS];

        /// <summary>
        /// The header_value_offsets_
        /// </summary>
        public fixed UInt32 header_value_offsets_[MixedCodeConstants.MAX_PREPARSED_HTTP_RESPONSE_HEADERS];

        /// <summary>
        /// The header_value_len_bytes_
        /// </summary>
        public fixed UInt32 header_value_len_bytes_[MixedCodeConstants.MAX_PREPARSED_HTTP_RESPONSE_HEADERS];

        /// <summary>
        /// The num_headers_
        /// </summary>
        public UInt32 num_headers_;

        /// <summary>
        /// Socket data pointer.
        /// </summary>
        public unsafe Byte* socket_data_;

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
        public void GetContentRaw(out IntPtr ptr, out UInt32 sizeBytes)
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
        public Byte[] GetContentByteArray_Slow()
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
        /// Gets the content as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetContentStringUtf8_Slow()
        {
            // Checking if there is a content.
            if (content_len_bytes_ <= 0)
                return null;

            return new String((SByte*)(socket_data_ + content_offset_), 0, (Int32)content_len_bytes_, Encoding.UTF8);
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

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>A <see cref="T:System.String" /> containing a fully qualified type name.</returns>
        public override String ToString()
        {
            return "<h1>Host: " + GetHeaderValue("Host") + "</h1>\r\n" +
                   "<h1>Session string: " + GetSessionString() + "</h1>\r\n" +
                   "<h1>ContentLength: " + content_len_bytes_ + "</h1>\r\n" +
                   "<h1>Content: " + GetContentStringUtf8_Slow() + "</h1>\r\n"
                   ;
        }
    }
}
