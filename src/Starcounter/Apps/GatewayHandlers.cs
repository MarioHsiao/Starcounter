
using System;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Apps.Bootstrap;
using Starcounter.Internal;

namespace Starcounter
{
    public enum HTTP_METHODS
    {
        GET_METHOD,
        POST_METHOD,
        PUT_METHOD,
        DELETE_METHOD,
        HEAD_METHOD,
        OPTIONS_METHOD,
        TRACE_METHOD,
        PATCH_METHOD,
        OTHER_METHOD
    };

    public struct ScSessionStruct
    {
        // Session random salt.
        public UInt64 random_salt_;

        // Unique session linear index.
        // Points to the element in sessions linear array.
        public UInt32 session_index_;

        // Scheduler ID.
        public UInt32 scheduler_id_;

        // Hex table used for conversion.
        static Byte[] hex_table_ = new Byte[] { (Byte)'0', (Byte)'1', (Byte)'2', (Byte)'3', (Byte)'4', (Byte)'5', (Byte)'6', (Byte)'7', (Byte)'8', (Byte)'9', (Byte)'A', (Byte)'B', (Byte)'C', (Byte)'D', (Byte)'E', (Byte)'F' };

        // Session string length in characters.
        const Int32 SC_SESSION_STRING_LEN_CHARS = 24;

        // Session cookie prefix.
        const String SessionCookiePrefix = "ScSessionId=";

        // Converts uint64_t number to hexadecimal string.
        Int32 uint64_to_hex_string(UInt64 number, Byte[] bytes_out, Int32 offset, Int32 num_4bits)
        {
            Int32 n = 0;
            while(number > 0)
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
        public Int32 ConvertToString(Byte[] str_out)
        {
            // Translating session index.
            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_out, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(random_salt_, str_out, sessionStringLen, 16);

            return sessionStringLen;
        }

        // Converts session to string.
        public String ConvertToString()
        {
            // Allocating string bytes.
            Byte[] str_bytes = new Byte[SC_SESSION_STRING_LEN_CHARS];

            // Translating session index.
            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_bytes, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(random_salt_, str_bytes, sessionStringLen, 16);

            // Converting byte array to string.
            return UTF8Encoding.ASCII.GetString(str_bytes);
        }

        // Converts session to a cookie string.
        public String ConvertToSessionCookie()
        {
            return SessionCookiePrefix + ConvertToString();
        }

        // Converts uint64_t number to hexadecimal string.
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
        public unsafe Int32 ConvertToStringUnsafe(Byte* str_out)
        {
            // Translating session index.
            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_out, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(random_salt_, str_out, sessionStringLen, 16);

            return sessionStringLen;
        }

        // Converts session to string.
        public String ConvertToStringFaster()
        {
            unsafe
            {
                // Allocating string bytes on stack.
                Byte* str_bytes = stackalloc Byte[SC_SESSION_STRING_LEN_CHARS];

                // Translating session index.
                Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_bytes, 0, 8);

                // Translating session random salt.
                sessionStringLen += uint64_to_hex_string(random_salt_, str_bytes, sessionStringLen, 16);

                // Converting byte array to string.
                return Marshal.PtrToStringAnsi((IntPtr)str_bytes, SC_SESSION_STRING_LEN_CHARS);
            }
        }

        // Converts session to a cookie string.
        public String ConvertToSessionCookieFaster()
        {
            return SessionCookiePrefix + ConvertToStringFaster();
        }
    }

    public struct HttpRequest
    {
        // Internal structure with HTTP request information.
        unsafe HttpRequestInternal* http_request_;

        // Constructor.
        public unsafe HttpRequest(
            Byte* chunk_data,
            Boolean single_chunk,
            UInt32 chunk_index,
            Byte* http_request,
            Byte* socket_data)
        {
            http_request_ = (HttpRequestInternal*)http_request;
            http_request_->sd_ = socket_data;
            http_request_->data_stream_.Init(chunk_data, single_chunk, chunk_index);
        }

        public void ReadBody(Byte[] buffer, Int32 offset, Int32 length)
        {
            unsafe { http_request_->data_stream_.Read(buffer, offset, length); }
        }

        public void WriteResponse(Byte[] buffer, Int32 offset, Int32 length)
        {
            unsafe { http_request_->data_stream_.Write(buffer, offset, length); }
        }

        public void GetRawRequest(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawRequest(out ptr, out sizeBytes); }
        }

        public void GetRawContent(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawContent(out ptr, out sizeBytes); }
        }

        public void GetRawMethodAndUri(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawMethodAndUri(out ptr, out sizeBytes); }
        }

        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawHeaders(out ptr, out sizeBytes); }
        }

        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawCookies(out ptr, out sizeBytes); }
        }

        public void GetRawAccept(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawAccept(out ptr, out sizeBytes); }
        }

        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetRawSessionString(out ptr, out sizeBytes); }
        }

        public void GetRawHeader(byte[] key, out IntPtr ptr, out UInt32 sizeBytes)
        {
            unsafe { http_request_->GetHeaderValue(key, out ptr, out sizeBytes); }
        }

        public String this[String name]
        {
            get
            {
                unsafe { return http_request_->GetHeaderValue(name); }
            }
        }

        public UInt32 ContentLength
        {
            get
            {
                unsafe { return http_request_->content_len_bytes_; }
            }
        }

        public ScSessionStruct SessionStruct
        {
            get
            {
                unsafe { return http_request_->session_struct_; }
            }
        }

        public HTTP_METHODS HttpMethod
        {
            get
            {
                unsafe { return http_request_->http_method_; }
            }
        }

        public Boolean IsGzipEnabled
        {
            get
            {
                unsafe { return http_request_->gzip_enabled_; }
            }
        }

        public String Uri
        {
            get
            {
                unsafe { return http_request_->Uri; }
            }
        }

        public override String ToString()
        {
            unsafe { return http_request_->ToString(); }
        }
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 8)]
    public unsafe struct HttpRequestInternal
    {
        public const Int32 MAX_HTTP_HEADERS = 16;

        // Request.
        public UInt32 request_offset_;
        public UInt32 request_len_bytes_;

        // Content.
        public UInt32 content_offset_;
        public UInt32 content_len_bytes_;

        // Resource URI.
        public UInt32 uri_offset_;
        public UInt32 uri_len_bytes_;

        // Key-value header.
        public UInt32 headers_offset_;
        public UInt32 headers_len_bytes_;

        // Cookie value.
        public UInt32 cookies_offset_;
        public UInt32 cookies_len_bytes_;

        // Accept value.
        public UInt32 accept_value_offset_;
        public UInt32 accept_value_len_bytes_;

        // Session ID.
        public UInt32 session_string_offset_;
        public UInt32 session_string_len_bytes_;

        // Header offsets.
        public fixed UInt32 header_offsets_[MAX_HTTP_HEADERS];
        public fixed UInt32 header_len_bytes_[MAX_HTTP_HEADERS];
        public fixed UInt32 header_value_offsets_[MAX_HTTP_HEADERS];
        public fixed UInt32 header_value_len_bytes_[MAX_HTTP_HEADERS];
        public UInt32 num_headers_;

        // Session structure.
        public ScSessionStruct session_struct_;

        // HTTP method.
        public HTTP_METHODS http_method_;

        // Gzip indicator.
        public bool gzip_enabled_;

        // Network data stream.
        public NetworkDataStream data_stream_;

        // Socket data pointer.
        public unsafe Byte* sd_;

        public void GetRawRequest(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + request_offset_);
            sizeBytes = request_len_bytes_;
        }

        // TODO: Plain big buffer!
        public void GetRawContent(out IntPtr ptr, out UInt32 sizeBytes)
        {
            if (content_len_bytes_ <= 0) ptr = IntPtr.Zero;
            else { ptr = new IntPtr(sd_ + content_offset_); }
            sizeBytes = content_len_bytes_;
        }

        // TODO: Plain big buffer!
        public String GetRawContent()
        {
            return Marshal.PtrToStringAnsi((IntPtr)(sd_ + content_offset_), (Int32)content_len_bytes_);
        }

        public void GetRawMethodAndUri(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + request_offset_);
            sizeBytes = uri_offset_ - request_offset_ + uri_len_bytes_;
        }

        public void GetRawHeaders(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + headers_offset_);
            sizeBytes = headers_len_bytes_;
        }

        public void GetRawCookies(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + cookies_offset_);
            sizeBytes = cookies_len_bytes_;
        }

        public void GetRawAccept(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + accept_value_offset_);
            sizeBytes = accept_value_len_bytes_;
        }

        public void GetRawSessionString(out IntPtr ptr, out UInt32 sizeBytes)
        {
            ptr = new IntPtr(sd_ + session_string_offset_);
            sizeBytes = session_string_len_bytes_;
        }

        public String GetSessionString()
        {
            IntPtr raw_session_string;
            UInt32 len_bytes;
            GetRawSessionString(out raw_session_string, out len_bytes);

            return Marshal.PtrToStringAnsi(raw_session_string, (Int32)len_bytes);
        }

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

        public String Uri
        {
            get
            {
                if (uri_len_bytes_ > 0)
                    return Marshal.PtrToStringAnsi((IntPtr)(sd_ + uri_offset_), (Int32)uri_len_bytes_);

                return null;
            }
        }

        public override String ToString()
        {
            return "<h1>HttpMethod: " + http_method_ + "</h1>\r\n" +
                   "<h1>URI: " + Uri + "</h1>\r\n" +
                   "<h1>ContentLength: " + content_len_bytes_ + "</h1>\r\n" +
                   "<h1>GZip: " + gzip_enabled_ + "</h1>\r\n" +
                   "<h1>Session index: " + session_struct_.session_index_ + "</h1>\r\n" +
                   "<h1>Session scheduler: " + session_struct_.scheduler_id_ + "</h1>\r\n" +
                   "<h1>Session string: " + GetSessionString() + "</h1>\r\n"
                   ;
        }
    };

    public delegate Boolean UriCallback(
        HttpRequest info
    );

    public struct PortHandlerParams
    {
        public UInt64 UserSessionId;
        public NetworkDataStream DataStream;
    }

    public delegate Boolean PortCallback(
		PortHandlerParams info
	);

    public struct SubportHandlerParams
    {
        public UInt64 UserSessionId;
        public UInt32 SubportId;
        public NetworkDataStream DataStream;
    }

    public delegate Boolean SubportCallback(
        SubportHandlerParams info
    );

	public unsafe class GatewayHandlers
	{
        // Offset in bytes for HttpRequest structure.
        const Int32 HTTP_REQUEST_OFFSET_BYTES = 184;

        // Maximum size of BMX header in the beginning of the chunk
        // after which the gateway data can be placed.
        const Int32 BMX_HEADER_MAX_SIZE_BYTES = 24;

        const Int32 MAX_HANDLERS = 1024;

        private static PortCallback[] port_handlers_;
        private static SubportCallback[] subport_handlers_;
        private static UriCallback[] uri_handlers_;

		private static bmx.BMX_HANDLER_CALLBACK port_outer_handler_;
        private static bmx.BMX_HANDLER_CALLBACK subport_outer_handler_;
        private static bmx.BMX_HANDLER_CALLBACK uri_outer_handler_;

        static GatewayHandlers()
		{
            AppProcess.AssertInDatabaseOrSendStartRequest();

            port_handlers_ = new PortCallback[MAX_HANDLERS];
            subport_handlers_ = new SubportCallback[MAX_HANDLERS];
            uri_handlers_ = new UriCallback[MAX_HANDLERS];

            port_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(PortOuterHandler);
            subport_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(SubportOuterHandler);
            uri_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(UriOuterHandler);
		}

        private unsafe static UInt32 PortOuterHandler(
			UInt64 session_id,
			Byte* raw_chunk, 
			bmx.BMX_TASK_INFO* task_info,
			Boolean* is_handled)
		{
            *is_handled = false;

            UInt32 chunk_index = task_info->chunk_index;
            Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

            // Fetching the callback.
            PortCallback user_callback = port_handlers_[task_info->handler_id];
			if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & 0x01) == 0);

            // Creating parameters.
            PortHandlerParams handler_params = new PortHandlerParams
            {
                UserSessionId = *(UInt32*)(raw_chunk + NetworkDataStream.GATEWAY_CHUNK_BEGIN + NetworkDataStream.SESSION_INDEX_OFFSET),
                DataStream = new NetworkDataStream(raw_chunk, is_single_chunk, task_info->chunk_index)
            };

            // Calling user callback.
            *is_handled = user_callback(handler_params);

			return 0;
		}

        private unsafe static UInt32 SubportOuterHandler(
            UInt64 session_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            *is_handled = false;

            UInt32 chunk_index = task_info->chunk_index;
            Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

            // Fetching the callback.
            SubportCallback user_callback = subport_handlers_[task_info->handler_id];
            if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & 0x01) == 0);

            // Creating parameters.
            SubportHandlerParams handler_params = new SubportHandlerParams
            {
                UserSessionId = *(UInt32*)(raw_chunk + NetworkDataStream.GATEWAY_CHUNK_BEGIN + NetworkDataStream.SESSION_INDEX_OFFSET),
                SubportId = 0,
                DataStream = new NetworkDataStream(raw_chunk, is_single_chunk, task_info->chunk_index)
            };

            // Calling user callback.
            *is_handled = user_callback(handler_params);

            return 0;
        }

        private unsafe static UInt32 UriOuterHandler(
            UInt64 session_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            *is_handled = false;

            UInt32 chunk_index = task_info->chunk_index;
            Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

            // Fetching the callback.
            UriCallback user_callback = uri_handlers_[task_info->handler_id];
            if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & 0x01) == 0);

            // Obtaining HttpRequest structure.
            HttpRequest http_request = new HttpRequest(
                raw_chunk,
                is_single_chunk,
                task_info->chunk_index,
                raw_chunk + BMX_HEADER_MAX_SIZE_BYTES + HTTP_REQUEST_OFFSET_BYTES,
                raw_chunk + BMX_HEADER_MAX_SIZE_BYTES);
            
            // Calling user callback.
            *is_handled = user_callback(http_request);

            return 0;
        }

        // Registers port handler.
        public static void RegisterPortHandler(
			UInt16 port, 
			PortCallback portCallback,
            out UInt16 handlerId)
		{
            UInt16 handler_id;

            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_port_handler(port, port_outer_handler_, &handler_id);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode);

                port_handlers_[handler_id] = portCallback;
                handlerId = handler_id;
            }
		}

        // Registers subport handler.
        public static void RegisterSubportHandler(
            UInt16 port,
            UInt32 subport,
            SubportCallback subportCallback,
            out UInt16 handlerId)
        {
            UInt16 handler_id;

            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_subport_handler(port, subport, subport_outer_handler_, &handler_id);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode);

                subport_handlers_[handler_id] = subportCallback;
                handlerId = handler_id;
            }
        }

        // Registers URI handler.
        public static void RegisterUriHandler(
            UInt16 port,
            String uri_string,
            HTTP_METHODS http_method,
            UriCallback uriCallback,
            out UInt16 handlerId)
        {
            UInt16 handler_id;

            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_uri_handler(port, uri_string, (Byte)http_method, uri_outer_handler_, &handler_id);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode);

                uri_handlers_[handler_id] = uriCallback;
                handlerId = handler_id;
            }
        }
	}
}
