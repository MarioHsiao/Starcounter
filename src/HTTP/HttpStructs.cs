// ***********************************************************************
// <copyright file="HttpStructs.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HttpStructs
{
    /// <summary>
    /// Enum HTTP_METHODS
    /// </summary>
    public enum HTTP_METHODS
    {
        /// <summary>
        /// The GE t_ METHOD
        /// </summary>
        GET_METHOD,
        /// <summary>
        /// The POS t_ METHOD
        /// </summary>
        POST_METHOD,
        /// <summary>
        /// The PU t_ METHOD
        /// </summary>
        PUT_METHOD,
        /// <summary>
        /// The DELET e_ METHOD
        /// </summary>
        DELETE_METHOD,
        /// <summary>
        /// The HEA d_ METHOD
        /// </summary>
        HEAD_METHOD,
        /// <summary>
        /// The OPTION s_ METHOD
        /// </summary>
        OPTIONS_METHOD,
        /// <summary>
        /// The TRAC e_ METHOD
        /// </summary>
        TRACE_METHOD,
        /// <summary>
        /// The PATC h_ METHOD
        /// </summary>
        PATCH_METHOD,
        /// <summary>
        /// The OTHE r_ METHOD
        /// </summary>
        OTHER_METHOD
    };

    /// <summary>
    /// Interface INetworkDataStream
    /// </summary>
    public interface INetworkDataStream
    {
        /// <summary>
        /// Inits the specified unmanaged chunk.
        /// </summary>
        /// <param name="unmanagedChunk">The unmanaged chunk.</param>
        /// <param name="isSingleChunk">The is single chunk.</param>
        /// <param name="chunkIndex">Index of the chunk.</param>
        unsafe void Init(Byte* unmanagedChunk, Boolean isSingleChunk, UInt32 chunkIndex);

        /// <summary>
        /// Reads the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        void Read(Byte[] buffer, Int32 offset, Int32 length);

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        void Write(Byte[] buffer, Int32 offset, Int32 length);
    }

    /// <summary>
    /// Struct ScSessionStruct
    /// </summary>
    public struct ScSessionStruct
    {
        // Session random salt.
        /// <summary>
        /// The session random salt.
        /// </summary>
        public UInt64 session_salt_;

        // Unique session linear index.
        // Points to the element in sessions linear array.
        /// <summary>
        /// The session_index_
        /// </summary>
        public UInt32 session_index_;

        // Scheduler ID.
        /// <summary>
        /// The scheduler_id_
        /// </summary>
        public UInt32 scheduler_id_;

        /// <summary>
        /// Unique number coming from Apps.
        /// </summary>
        public UInt64 apps_unique_session_num_;

        // Session string length in characters.
        /// <summary>
        /// The S c_ SESSIO n_ STRIN g_ LE n_ CHARS
        /// </summary>
        const Int32 SC_SESSION_STRING_LEN_CHARS = 24;

        /*
        // Hex table used for conversion.
        /// <summary>
        /// The hex_table_
        /// </summary>
        static Byte[] hex_table_ = new Byte[] { (Byte)'0', (Byte)'1', (Byte)'2', (Byte)'3', (Byte)'4', (Byte)'5', (Byte)'6', (Byte)'7', (Byte)'8', (Byte)'9', (Byte)'A', (Byte)'B', (Byte)'C', (Byte)'D', (Byte)'E', (Byte)'F' };

        // Session cookie prefix.
        /// <summary>
        /// The session cookie prefix
        /// </summary>
        const String SessionCookiePrefix = "ScSessionId=";

        // Converts uint64_t number to hexadecimal string.
        /// <summary>
        /// Uint64_to_hex_strings the specified number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="bytes_out">The bytes_out.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="num_4bits">The num_4bits.</param>
        /// <returns>Int32.</returns>
        Int32 uint64_to_hex_string(UInt64 number, Byte[] bytes_out, Int32 offset, Int32 num_4bits)
        {
            Int32 n = 0;
            while (number > 0)
            {
                bytes_out[n + offset] = hex_table_[number & 0xF];
                n++;
                number >>= 4;
            }

            // Filling with zero values if necessary.
            while (n < num_4bits)
            {
                bytes_out[n + offset] = (Byte)'0';
                n++;
            }

            // Returning length.
            return n;
        }

        // Converts session to string.
        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <param name="str_out">The str_out.</param>
        /// <returns>Int32.</returns>
        public Int32 ConvertToString(Byte[] str_out)
        {
            // Translating session index.
            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_out, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(session_salt_, str_out, sessionStringLen, 16);

            return sessionStringLen;
        }

        // Converts session to string.
        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>String.</returns>
        public String ConvertToString()
        {
            // Allocating string bytes.
            Byte[] str_bytes = new Byte[SC_SESSION_STRING_LEN_CHARS];

            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_bytes, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(session_salt_, str_bytes, sessionStringLen, 16);

            // Converting byte array to string.
            return UTF8Encoding.ASCII.GetString(str_bytes);
        }

        // Converts session to a cookie string.
        /// <summary>
        /// Converts to session cookie.
        /// </summary>
        /// <returns>String.</returns>
        public String ConvertToSessionCookie()
        {
            return SessionCookiePrefix + ConvertToString();
        }

        // Converts uint64_t number to hexadecimal string.
        /// <summary>
        /// Uint64_to_hex_strings the specified number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="bytes_out">The bytes_out.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="num_4bits">The num_4bits.</param>
        /// <returns>Int32.</returns>
        unsafe Int32 uint64_to_hex_string(UInt64 number, Byte* bytes_out, Int32 offset, Int32 num_4bits)
        {
            Int32 n = 0;
            while (number > 0)
            {
                bytes_out[n + offset] = hex_table_[number & 0xF];
                n++;
                number >>= 4;
            }

            // Filling with zero values if necessary.
            while (n < num_4bits)
            {
                bytes_out[n + offset] = (Byte)'0';
                n++;
            }

            // Returning length.
            return n;
        }

        // Converts session to string.
        /// <summary>
        /// Converts to string unsafe.
        /// </summary>
        /// <param name="str_out">The str_out.</param>
        /// <returns>Int32.</returns>
        public unsafe Int32 ConvertToStringUnsafe(Byte* str_out)
        {
            // Translating session index.
            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_out, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(session_salt_, str_out, sessionStringLen, 16);

            return sessionStringLen;
        }

        // Converts session to string.
        /// <summary>
        /// Converts to string faster.
        /// </summary>
        /// <returns>String.</returns>
        public String ConvertToStringFaster()
        {
            unsafe
            {
                // Allocating string bytes on stack.
                Byte* str_bytes = stackalloc Byte[SC_SESSION_STRING_LEN_CHARS];

                // Translating session index.
                Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_bytes, 0, 8);

                // Translating session random salt.
                sessionStringLen += uint64_to_hex_string(session_salt_, str_bytes, sessionStringLen, 16);

                // Converting byte array to string.
                return Marshal.PtrToStringAnsi((IntPtr)str_bytes, SC_SESSION_STRING_LEN_CHARS);
            }
        }

        // Converts session to a cookie string.
        /// <summary>
        /// Converts to session cookie faster.
        /// </summary>
        /// <returns>String.</returns>
        public String ConvertToSessionCookieFaster()
        {
            return SessionCookiePrefix + ConvertToStringFaster();
        }
        */
        
        /// <summary>
        /// Returns constant session cookie stub.
        /// </summary>
        /// <returns></returns>
        public String SessionCookieStubString
        {
            get
            {
                return "ScSessionId=########################";
            }
        }
    }

    /// <summary>
    /// Class HttpRequest
    /// </summary>
    public class HttpRequest
    {
        /// <summary>
        /// Offset in bytes for the session.
        /// </summary>
        const Int32 SESSION_OFFSET_BYTES = 32;

        // Internal structure with HTTP request information.
        /// <summary>
        /// The http_request_
        /// </summary>
        unsafe HttpRequestInternal* http_request_;

        /// <summary>
        /// Direct pointer to session data.
        /// </summary>
        unsafe ScSessionStruct* session_;

        // Network data stream.
        /// <summary>
        /// The data_stream_
        /// </summary>
        public INetworkDataStream data_stream_;

        // Constructor.
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequest" /> class.
        /// </summary>
        /// <param name="buf">The buf.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public HttpRequest(Byte[] buf)
        {
            // TODO: Parse the uri and so on.

            /*unsafe
            {
                Byte* pnew = (Byte*)BitsAndBytes.Alloc(buf.Length + sizeof(HttpRequestInternal));

                fixed (Byte* pbuf = buf)
                {
                    BitsAndBytes.MemCpy(pnew, pbuf, (uint)buf.Length);
                }
            }*/

            throw new NotImplementedException();
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
            INetworkDataStream data_stream)
        {
            http_request_ = (HttpRequestInternal*)http_request_begin;
            session_ = (ScSessionStruct*)(socket_data + SESSION_OFFSET_BYTES);
            http_request_->sd_ = socket_data;
            data_stream_ = data_stream;
            data_stream_.Init(chunk_data, single_chunk, chunk_index);
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

        // TODO
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
        public bool CanUseStaticResponse
        {
            get
            {
                return ViewModel == null;
            }
        }

        /// <summary>
        /// Gets the body raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetBodyRaw(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetBodyRaw(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the body as byte array.
        /// </summary>
        /// <returns>Body bytes.</returns>
        public Byte[] GetBodyByteArray()
        {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            unsafe { return http_request_->GetBodyByteArray(); }
        }

        /// <summary>
        /// Gets the body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetBodyStringUtf8()
        {
            unsafe { return http_request_->GetBodyStringUtf8(); }
        }

        /// <summary>
        /// Gets the length of the body in bytes.
        /// </summary>
        /// <value>The length of the body.</value>
        public UInt32 BodyLength
        {
            get
            {
                unsafe { return http_request_->body_len_bytes_; }
            }
        }

        /// <summary>
        /// Writes the response.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void WriteResponse(Byte[] buffer, Int32 offset, Int32 length)
        {
            unsafe { data_stream_.Write(buffer, offset, length); }
        }

        /// <summary>
        /// Gets the raw request.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRequestRaw(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRequestRaw(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawMethodAndUri(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawMethodAndUri(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw verb and URI plus space.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawVerbAndUriPlusSpace(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawMethodAndUri(out ptr, out sizeBytes); }
            sizeBytes += 1;
        }

        /// <summary>
        /// Gets the raw headers.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawHeaders(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw cookies.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawCookies(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw accept.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawAccept(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawAccept(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw session string.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawSessionString(out ptr, out sizeBytes); }
        }

        /// <summary>
        /// Gets the raw header.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeader(byte[] key, out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetHeaderValue(key, out ptr, out sizeBytes); }
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
                unsafe { return http_request_->GetHeaderValue(name); }
            }
        }

        /// <summary>
        /// Invalid session index.
        /// </summary>
        const UInt32 INVALID_SESSION_INDEX = UInt32.MaxValue;

        /// <summary>
        /// Invalid Apps unique number.
        /// </summary>
        const UInt64 INVALID_APPS_UNIQUE_SESSION_NUMBER = 0;

        /// <summary>
        /// Checks if HTTP request already has session.
        /// </summary>
        public Boolean HasSession
        {
            get
            {
                unsafe
                {
                    return INVALID_APPS_UNIQUE_SESSION_NUMBER != (session_->apps_unique_session_num_);
                }
            }
        }

        /// <summary>
        /// Generates session number and writes it to response.
        /// </summary>
        public UInt64 GenerateNewSession()
        {
            unsafe
            {
                // Resetting the session index.
                session_->session_index_ = INVALID_SESSION_INDEX;

                // Generating new session and assigning it to current.
                session_->apps_unique_session_num_ = GlobalSessions.AllSessions.GenerateUniqueNumber();

                // Returning Apps unique number.
                return session_->apps_unique_session_num_;
            }
        }

        /// <summary>
        /// Kills the existing session.
        /// </summary>
        public void KillSession()
        {
            unsafe
            {
                // Killing this session by setting invalid unique number.
                session_->apps_unique_session_num_ = INVALID_APPS_UNIQUE_SESSION_NUMBER;
            }
        }

        /// <summary>
        /// Returns unique session number.
        /// </summary>
        public UInt64 UniqueSessionNumber
        {
            get
            {
                unsafe
                {
                    return session_->apps_unique_session_num_;
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
        /// Gets the HTTP method.
        /// </summary>
        /// <value>The HTTP method.</value>
        public HTTP_METHODS HttpMethod
        {
            get
            {
                unsafe { return http_request_->http_method_; }
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
                unsafe { return http_request_->is_gzip_accepted_; }
            }
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <value>The URI.</value>
        public String Uri
        {
            get
            {
                unsafe { return http_request_->Uri; }
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override String ToString()
        {
            unsafe { return http_request_->ToString(); }
        }
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 8)]
    /// <summary>
    /// Struct HttpRequestInternal
    /// </summary>
    public unsafe struct HttpRequestInternal
    {
        /// <summary>
        /// The MA x_ HTT p_ HEADERS
        /// </summary>
        public const Int32 MAX_HTTP_HEADERS = 16;

        // Request.
        /// <summary>
        /// The request_offset_
        /// </summary>
        public UInt32 request_offset_;
        /// <summary>
        /// The request_len_bytes_
        /// </summary>
        public UInt32 request_len_bytes_;

        // Body.
        /// <summary>
        /// The body_offset_
        /// </summary>
        public UInt32 body_offset_;
        /// <summary>
        /// The body_len_bytes_
        /// </summary>
        public UInt32 body_len_bytes_;

        // Resource URI.
        /// <summary>
        /// The uri_offset_
        /// </summary>
        public UInt32 uri_offset_;
        /// <summary>
        /// The uri_len_bytes_
        /// </summary>
        public UInt32 uri_len_bytes_;

        // Key-value header.
        /// <summary>
        /// The headers_offset_
        /// </summary>
        public UInt32 headers_offset_;
        /// <summary>
        /// The headers_len_bytes_
        /// </summary>
        public UInt32 headers_len_bytes_;

        // Cookie value.
        /// <summary>
        /// The cookies_offset_
        /// </summary>
        public UInt32 cookies_offset_;
        /// <summary>
        /// The cookies_len_bytes_
        /// </summary>
        public UInt32 cookies_len_bytes_;

        // Accept value.
        /// <summary>
        /// The accept_value_offset_
        /// </summary>
        public UInt32 accept_value_offset_;
        /// <summary>
        /// The accept_value_len_bytes_
        /// </summary>
        public UInt32 accept_value_len_bytes_;

        // Session ID.
        /// <summary>
        /// The session_string_offset_
        /// </summary>
        public UInt32 session_string_offset_;
        /// <summary>
        /// The session_string_len_bytes_
        /// </summary>
        public UInt32 session_string_len_bytes_;

        // Header offsets.
        /// <summary>
        /// The header_offsets_
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

        // HTTP method.
        /// <summary>
        /// The http_method_
        /// </summary>
        public HTTP_METHODS http_method_;

        // Is Gzip accepted.
        /// <summary>
        /// The is_gzip_accepted_
        /// </summary>
        public bool is_gzip_accepted_;

        // Socket data pointer.
        /// <summary>
        /// The SD_
        /// </summary>
        public unsafe Byte* sd_;

        /// <summary>
        /// Gets the raw request.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRequestRaw(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + request_offset_);
            sizeBytes = request_len_bytes_;
        }

        /// <summary>
        /// Gets the body raw pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetBodyRaw(out IntPtr ptr, out UInt32 sizeBytes)
        {
            if (body_len_bytes_ <= 0) ptr = IntPtr.Zero;
            else { ptr = new IntPtr(sd_ + body_offset_); }
            sizeBytes = body_len_bytes_;
        }

        /// <summary>
        /// Gets the body as byte array.
        /// </summary>
        /// <returns>Body bytes.</returns>
        public Byte[] GetBodyByteArray()
        {
            // TODO: Provide a more efficient interface with existing Byte[] and offset.

            Byte[] body_bytes = new Byte[(Int32)body_len_bytes_];
            Marshal.Copy((IntPtr)(sd_ + body_offset_), body_bytes, 0, (Int32)body_len_bytes_);

            return body_bytes;
        }

        /// <summary>
        /// Gets the body as UTF8 string.
        /// </summary>
        /// <returns>UTF8 string.</returns>
        public String GetBodyStringUtf8()
        {
            return new String((SByte*)(sd_ + body_offset_), 0, (Int32)body_len_bytes_, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the raw method and URI.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawMethodAndUri(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + request_offset_);
            sizeBytes = uri_offset_ - request_offset_ + uri_len_bytes_;
        }

        /// <summary>
        /// Gets the raw headers.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + headers_offset_);
            sizeBytes = headers_len_bytes_;
        }

        /// <summary>
        /// Gets the raw cookies.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + cookies_offset_);
            sizeBytes = cookies_len_bytes_;
        }

        /// <summary>
        /// Gets the raw accept.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawAccept(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + accept_value_offset_);
            sizeBytes = accept_value_len_bytes_;
        }

        /// <summary>
        /// Gets the raw session string.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="sizeBytes">The size bytes.</param>
        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + session_string_offset_);
            sizeBytes = session_string_len_bytes_;
        }

        /// <summary>
        /// Gets the session string.
        /// </summary>
        /// <returns>String.</returns>
        public String GetSessionString()
        {
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
                                if (((Byte)headerName[k]) != *(sd_ + header_offsets[i] + k))
                                {
                                    found = false;
                                    break;
                                }
                            }

                            if (found)
                            {
                                ptr = (IntPtr)(sd_ + header_value_offsets[i]);
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
                                if (((Byte)headerName[k]) != *(sd_ + header_offsets[i] + k))
                                {
                                    found = false;
                                    break;
                                }
                            }

                            if (found)
                            {
                                // Skipping two bytes for colon and one space.
                                return Marshal.PtrToStringAnsi((IntPtr)(sd_ + header_value_offsets[i]), (Int32)header_value_len_bytes[i]);
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
        public String Uri
        {
            get
            {
                if (uri_len_bytes_ > 0)
                    return Marshal.PtrToStringAnsi((IntPtr)(sd_ + uri_offset_), (Int32)uri_len_bytes_);

                return null;
            }
        }

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>A <see cref="T:System.String" /> containing a fully qualified type name.</returns>
        public override String ToString()
        {
            return "<h1>HttpMethod: " + http_method_ + "</h1>\r\n" +
                   "<h1>URI: " + Uri + "</h1>\r\n" +
                   "<h1>BodyLength: " + body_len_bytes_ + "</h1>\r\n" +
                   "<h1>GZip accepted: " + is_gzip_accepted_ + "</h1>\r\n" +
                   "<h1>Session string: " + GetSessionString() + "</h1>\r\n"
                   ;
        }
    };
}
