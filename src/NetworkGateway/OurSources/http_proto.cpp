#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
#include "worker_db_interface.hpp"
#include "worker.hpp"

namespace starcounter {
namespace network {

const char* const kHttpGatewayPongResponse =
    "HTTP/1.1 200 OK\r\n"
    "Content-Type: text/html; charset=UTF-8\r\n"
    "Content-Length: 5\r\n"
    "\r\n"
    "Pong!";

const int32_t kHttpGatewayPongResponseLength = strlen(kHttpGatewayPongResponse);

const char* const kHttpNoContent =
    "HTTP/1.1 204 No Content\r\n"
    "Content-Length: 0\r\n"
    "\r\n";

const int32_t kHttpNoContentLength = strlen(kHttpNoContent) + 1;

const char* const kHttpBadRequest =
    "HTTP/1.1 400 Bad Request\r\n"
    "Content-Length: 0\r\n"
    "\r\n";

const int32_t kHttpBadRequestLength = strlen(kHttpBadRequest) + 1;

const char* const kHttpServiceUnavailable =
    "HTTP/1.1 503 Service Unavailable\r\n"
    "Content-Length: 0\r\n"
    "\r\n";

const int32_t kHttpServiceUnavailableLength = strlen(kHttpServiceUnavailable) + 1;

const char* const kHttpTooBigUpload =
    "HTTP/1.1 413 Request Entity Too Large\r\n"
    "Content-Type: text/html; charset=UTF-8\r\n"
    "Content-Length: 50\r\n"
    "\r\n"
    "Maximum supported HTTP request body size is 32 Mb!";

const int32_t kHttpTooBigUploadLength = strlen(kHttpTooBigUpload) + 1;

// Fetches method and URI from HTTP request data.
inline uint32_t GetMethodAndUri(
    char* http_data,
    uint32_t http_data_len,
    char* out_methoduri_lower_case,
    uint32_t* out_len,
    uint32_t* out_uri_offset,
    uint32_t uri_max_len)
{
    uint32_t pos = 0;

    // Reading method.
    while (pos < http_data_len)
    {
        if (http_data[pos] == ' ')
            break;

        pos++;
    }

    // Copying offset to URI.
    *out_uri_offset = pos + 1;

    // Reading URI.
    pos++;
    while (pos < http_data_len)
    {
        if (http_data[pos] == ' ')
            break;

        pos++;
    }

    // TODO!
    // Checking that we have HTTP protocol.
    if (pos < http_data_len)
    {
        // Checking for HTTP keyword.
        if (http_data[pos + 1] != 'H')
        {
            // Wrong protocol.
            return SCERRGWNONHTTPPROTOCOL;
        }
    }
    else
    {
        // Either wrong protocol or not enough accumulated data.
        return SCERRGWNONHTTPPROTOCOL;
    }

    // Checking if method and URI has correct length.
    if (pos < uri_max_len)
    {
        // Copying string.
        strncpy_s(out_methoduri_lower_case, pos + 1, http_data, pos);

        // Converting to lower case.
        _strlwr_s(out_methoduri_lower_case, pos + 1);

        // Setting output length.
        *out_len = pos;

        return 0;
    }

    // Wrong protocol.
    return SCERRGWNONHTTPPROTOCOL;
}

inline int HttpWsProto::OnMessageBegin(http_parser* p)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "HTTPOnMessageBegin" << std::endl;
#endif

    HttpWsProto *http = (HttpWsProto *)p;
    return 0;
}

inline int HttpWsProto::OnHeadersComplete(http_parser* p)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "HTTPOnHeadersComplete" << std::endl;
#endif

    HttpWsProto *http = (HttpWsProto *)p;

    // Setting complete header flag.
    http->sd_ref_->set_complete_header_flag(true);
    
    // Setting headers length (skipping 4 bytes for \r\n\r\n).
    http->http_request_.headers_len_bytes_ = p->nread - 4 - http->http_request_.headers_len_bytes_;

    return 0;
}

inline int HttpWsProto::OnMessageComplete(http_parser* p)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "HTTPOnMessageComplete" << std::endl;
#endif

    HttpWsProto *http = (HttpWsProto *)p;
    return 0;
}

inline int HttpWsProto::OnUri(http_parser* p, const char *at, size_t length)
{
#ifdef GW_HTTP_DIAG
    char at_ref[256];
    memcpy(at_ref, at, length);
    at_ref[length] = '\0';
    GW_COUT << "HTTPOnUri: " << at_ref << std::endl;
#endif

    HttpWsProto *http = (HttpWsProto *)p;

    // Setting the reference to URI.
    http->http_request_.uri_offset_ = at - (char *)(http->sd_ref_);
    http->http_request_.uri_len_bytes_ = length;

    return 0;
}

inline int HttpWsProto::OnHeaderField(http_parser* p, const char *at, size_t length)
{
#ifdef GW_HTTP_DIAG
    char at_ref[256];
    memcpy(at_ref, at, length);
    at_ref[length] = '\0';
    GW_COUT << "HTTPOnHeaderField: " << at_ref << std::endl;
#endif

    HttpWsProto *http = (HttpWsProto *)p;

    // Determining what header field is that.
    http->last_field_ = DetermineField(at, length);

    // Saving header offset.
    http->http_request_.header_offsets_[http->http_request_.num_headers_] = at - (char*)http->sd_ref_;
    http->http_request_.header_len_bytes_[http->http_request_.num_headers_] = length;

    // Setting headers beginning.
    if (!http->http_request_.headers_offset_)
    {
        http->http_request_.headers_len_bytes_ = p->nread - length - 1;
        http->http_request_.headers_offset_ = at - (char*)http->sd_ref_;
    }

    return 0;
}

inline int HttpWsProto::OnHeaderValue(http_parser* p, const char *at, size_t length)
{
#ifdef GW_HTTP_DIAG
    char at_ref[256];
    memcpy(at_ref, at, length);
    at_ref[length] = '\0';
    GW_COUT << "HTTPOnHeaderValue: " << at_ref << std::endl;
#endif

    HttpWsProto *http = (HttpWsProto *)p;

    // Saving header length.
    http->http_request_.header_value_offsets_[http->http_request_.num_headers_] = at - (char*)http->sd_ref_;
    http->http_request_.header_value_len_bytes_[http->http_request_.num_headers_] = length;

    // Increasing number of saved headers.
    http->http_request_.num_headers_++;
    if (http->http_request_.num_headers_ >= MAX_HTTP_HEADERS)
    {
        // Too many HTTP headers.
        GW_COUT << "Too many HTTP headers detected, maximum allowed: " << MAX_HTTP_HEADERS << std::endl;
        return SCERRGWHTTPTOOMANYHEADERS;
    }

    // Processing last field type.
    switch (http->last_field_)
    {
        case COOKIE_FIELD:
        {
            // Check if its an old session from a different socket.
            const char* session_id_value = GetSessionIdValue(at, length);

            // Checking if Starcounter session id is presented.
            if (session_id_value)
            {
                // Setting the session offset.
                http->http_request_.session_string_offset_ = session_id_value - (char*)http->sd_ref_;
                http->http_request_.session_string_len_bytes_ = SC_SESSION_STRING_LEN_CHARS;

                // Reading received session index (skipping session header name and equality).
                session_index_type session_index = hex_string_to_uint64(at + kScSessionIdStringLength + 1, 8);
                if (INVALID_CONVERTED_NUMBER == session_index)
                {
                    GW_COUT << "Session index stored in the HTTP header has wrong format." << std::endl;
                    return SCERRGWHTTPWRONGSESSIONINDEXFORMAT;
                }

                // Reading received session random salt.
                uint64_t randomSalt = hex_string_to_uint64(at + kScSessionIdStringLength + 1 + 8, 16);
                if (INVALID_CONVERTED_NUMBER == randomSalt)
                {
                    GW_COUT << "Session random salt stored in the HTTP header has wrong format." << std::endl;
                    return SCERRGWHTTPWRONGSESSIONSALTFORMAT;
                }

                // Checking if we have existing session.
                ScSessionStruct session = g_gateway.GetGlobalSessionDataCopy(http->sd_ref_->get_session_index());

                // Checking if session is valid.
                if (session.IsValid())
                {
                    // Compare this session with existing one.
                    if (!session.Compare(randomSalt, session_index))
                    {
                        GW_COUT << "Session stored in the HTTP header is wrong." << std::endl;
                        return SCERRGWHTTPWRONGSESSION;
                    }
                }
                else
                {
                    // Attaching to existing or creating a new session.
                    ScSessionStruct existing_session = g_gateway.GetGlobalSessionDataCopy(session_index);
                    if ((existing_session.IsValid()) && (existing_session.Compare(randomSalt, session_index)))
                    {
                        // Attaching existing session.
                        http->sd_ref_->AttachToSession(&existing_session);
                    }
                    else
                    {
#ifdef GW_SESSIONS_DIAG
                        GW_COUT << "Given session does not exist: " << session_index << ":" << randomSalt << std::endl;
#endif
                    }
                }
            }
            else
            {
                // Checking that session is valid.
                if (g_gateway.GetGlobalSessionDataCopy(http->sd_ref_->get_session_index()).IsValid())
                {
                    GW_COUT << "Expected session cookie was not present!" << std::endl;
                    return 0;
                }
            }

            // Setting needed HttpRequest fields.
            http->http_request_.cookies_offset_ = at - (char*)http->sd_ref_;
            http->http_request_.cookies_len_bytes_ = length;

            break;
        }

        case CONTENT_LENGTH_FIELD:
        {
            // Calculating body length.
            http->http_request_.body_len_bytes_ = ParseDecimalStringToUint(at, length);

            break;
        }

        case ACCEPT_ENCODING_FIELD:
        {
            // Checking if Gzip is accepted.
            int32_t i = 0;
            while (i < length)
            {
                if (at[i] == 'g')
                {
                    if (at[i + 1] == 'z')
                    {
                        http->http_request_.gzip_accepted_ = true;
                        break;
                    }
                }
                i++;
            }

            break;
        }

        case ACCEPT_FIELD:
        {
            http->http_request_.accept_value_offset_ = at - (char*)http->sd_ref_;
            http->http_request_.accept_value_len_bytes_ = length;

            break;
        }

        case UPGRADE_FIELD:
        {
            // Double checking if its a WebSocket upgrade.
            if ((at[0] == 'w') && (length == 9))
            {
                return 0;
            }
            else
            {
                return SCERRGWHTTPNONWEBSOCKETSUPGRADE;
            }

            break;
        }

        case WS_KEY_FIELD:
        {
            http->ws_proto_.SetClientKey((char *)at, length);
            break;
        }

        case WS_PROTOCOL_FIELD:
        {
            http->ws_proto_.SetSubProtocol((char *)at, length);
            break;
        }

        case WS_VERSION_FIELD:
        {
            // Checking the WebSocket protocol version.
            if (at[0] == '1' && at[1] == '3')
            {
                return 0;
            }
            else
            {
                return SCERRGWHTTPWRONGWEBSOCKETSVERSION;
            }

            break;
        }

        case COMPRESSION_FIELD:
        {

            break;
        }
    }

    return 0;
}

inline int HttpWsProto::OnBody(http_parser* p, const char *at, size_t length)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "HTTPOnBody" << std::endl;
#endif

    HttpWsProto *http = (HttpWsProto *)p;

    // Setting body parameters.
    if (http->http_request_.body_len_bytes_ <= 0)
        http->http_request_.body_len_bytes_ = length;

    // Setting body data offset.
    http->http_request_.body_offset_ = at - (char*)http->sd_ref_;

    return 0;
}

// Global HTTP parser settings.
http_parser_settings g_httpParserSettings;

void HttpGlobalInit()
{
    // Setting HTTP callbacks.
    g_httpParserSettings.on_body = HttpWsProto::OnBody;
    g_httpParserSettings.on_header_field = HttpWsProto::OnHeaderField;
    g_httpParserSettings.on_header_value = HttpWsProto::OnHeaderValue;
    g_httpParserSettings.on_headers_complete = HttpWsProto::OnHeadersComplete;
    g_httpParserSettings.on_message_begin = HttpWsProto::OnMessageBegin;
    g_httpParserSettings.on_message_complete = HttpWsProto::OnMessageComplete;
    g_httpParserSettings.on_url = HttpWsProto::OnUri;
}

// Determines the correct HTTP handler.
uint32_t HttpWsProto::HttpUriDispatcher(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_index,
    bool* is_handled)
{
    // Not handled yet.
    *is_handled = false;

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Checking if already determined further handler.
        if (INVALID_URI_INDEX != matched_uri_index_)
        {
            // Running determined handler now.

            ServerPort* server_port = g_gateway.get_server_port(sd->get_port_index());
            RegisteredUris* reg_uris = server_port->get_registered_uris();

            *is_handled = true;

            return reg_uris->GetEntryByIndex(matched_uri_index_).RunHandlers(gw, sd);
        }

        // Checking if we are already passed the WebSockets handshake.
        if(sd->get_web_sockets_upgrade_flag())
            return ws_proto_.ProcessWsDataToDb(gw, sd, handler_index);

        // Obtaining method and URI.
        char* method_and_uri_lower_case = gw->get_uri_lower_case();
        uint32_t method_and_uri_len, uri_offset;

        // Checking for any errors.
        uint32_t err_code = GetMethodAndUri(
            (char*)(sd->get_accum_buf()->get_orig_buf_ptr()),
            sd->get_accum_buf()->get_accum_len_bytes(),
            method_and_uri_lower_case,
            &method_and_uri_len,
            &uri_offset,
            bmx::MAX_URI_STRING_LEN);

        // Checking for any errors.
        if (err_code)
        {
            // Continue receiving.
            sd->get_accum_buf()->ContinueReceive();

            // Returning socket to receiving state.
            err_code = gw->Receive(sd);
            GW_ERR_CHECK(err_code);

            // Handled successfully.
            *is_handled = true;

            return 0;
        }

        // Now we have method and URI and ready to search specific URI handler.

        // Getting the corresponding port number.
        ServerPort* server_port = g_gateway.get_server_port(sd->get_port_index());
        uint16_t port_num = server_port->get_port_number();
        RegisteredUris* reg_uris = server_port->get_registered_uris();

        // Searching matching method and URI.
        int32_t max_matched_chars_method_and_uri;
        int32_t matched_index_method_and_uri = reg_uris->SearchMatchingUriHandler(method_and_uri_lower_case, method_and_uri_len, max_matched_chars_method_and_uri);
        max_matched_chars_method_and_uri -= uri_offset;

        // Searching on URIs only now.
        int32_t max_matched_chars_just_uri;
        int32_t matched_index_just_uri = reg_uris->SearchMatchingUriHandler(method_and_uri_lower_case + uri_offset, method_and_uri_len - uri_offset, max_matched_chars_just_uri);

        // Determining which matched handler to pick.
        int32_t matched_index = -1;
        if (matched_index_method_and_uri >= 0)
        {
            matched_index = matched_index_method_and_uri;

            // Checking if pure URI was matched as well.
            if (matched_index_just_uri >= 0)
            {
                // Comparing which URI is longer.
                if (max_matched_chars_just_uri > max_matched_chars_method_and_uri)
                    matched_index = matched_index_just_uri;
            }
        }
        else if (matched_index_just_uri >= 0)
        {
            matched_index = matched_index_just_uri;
        }

        // Checking if we failed to find again.
        if (matched_index < 0)
            return SCERRREQUESTONUNREGISTEREDURI;

        // Message is handled.
        *is_handled = true;

        // Indicating that matching URI index was found.
        set_matched_uri_index(matched_index);

        // Running determined handler now.
        return reg_uris->GetEntryByIndex(matched_index).RunHandlers(gw, sd);
    }
    else
    {
        // Just running standard response processing.
        *is_handled = true;
        return HttpWsProcessData(gw, sd, handler_index, is_handled);
    }

    return 0;
}

// Sends given predefined response.
uint32_t HttpWsProto::SendPredefinedResponse(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    const char* response,
    const int32_t response_length)
{
    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Copying given response.
    memcpy(accum_buf->get_orig_buf_ptr(), response, response_length);

    // Prepare buffer to send outside.
    accum_buf->PrepareForSend(accum_buf->get_orig_buf_ptr(), response_length);

    // Sending data.
    return gw->Send(sd);
}

// Parses the HTTP request and pushes processed data to database.
uint32_t HttpWsProto::HttpWsProcessData(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled)
{
    uint32_t err_code;

    // Not handled yet.
    *is_handled = false;

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Getting reference to accumulative buffer.
        AccumBuffer* accum_buf = sd->get_accum_buf();

        // Checking if we are in fill-up mode.
        if (sd->get_accumulating_flag())
            goto ALL_DATA_ACCUMULATED;

        // Checking if we are already passed the WebSockets handshake.
        if(sd->get_web_sockets_upgrade_flag())
            return ws_proto_.ProcessWsDataToDb(gw, sd, handler_id);

        // Resetting the parsing structure.
        ResetParser();

        // Attaching a socket.
        AttachToParser(sd);

        // Executing HTTP parser.
        size_t bytes_parsed = http_parser_execute(
            (http_parser *)this,
            &g_httpParserSettings,
            (const char *)accum_buf->get_orig_buf_ptr(),
            accum_buf->get_accum_len_bytes());

        // Checking if we have complete data.
        if ((!sd->get_complete_header_flag()) && (bytes_parsed == accum_buf->get_accum_len_bytes()))
        {
            // Continue receiving.
            accum_buf->ContinueReceive();

            // Returning socket to receiving state.
            err_code = gw->Receive(sd);
            GW_ERR_CHECK(err_code);

            // Handled successfully.
            *is_handled = true;

            return 0;
        }

        // Checking if we have WebSockets upgrade.
        if (http_parser_.upgrade)
        {
            GW_COUT << "Upgrade to another protocol detected, data: " << std::endl;

            // Handled successfully.
            *is_handled = true;

            // Perform WebSockets handshake.
            return ws_proto_.DoHandshake(gw, sd);
        }
        // Handle error. Usually just close the connection.
        else if (bytes_parsed != (accum_buf->get_accum_len_bytes()))
        {
            GW_COUT << "HTTP packet has incorrect data!" << std::endl;
            return SCERRGWHTTPINCORRECTDATA;
        }
        // Standard HTTP.
        else
        {
            // Getting the HTTP method.
            http_method method = (http_method)http_parser_.method;
            switch (method)
            {
            case http_method::HTTP_GET: 
                http_request_.http_method_ = bmx::HTTP_METHODS::GET_METHOD;
                break;

            case http_method::HTTP_POST: 
                http_request_.http_method_ = bmx::HTTP_METHODS::POST_METHOD;
                break;

            case http_method::HTTP_PUT: 
                http_request_.http_method_ = bmx::HTTP_METHODS::PUT_METHOD;
                break;

            case http_method::HTTP_DELETE: 
                http_request_.http_method_ = bmx::HTTP_METHODS::DELETE_METHOD;
                break;

            case http_method::HTTP_HEAD: 
                http_request_.http_method_ = bmx::HTTP_METHODS::HEAD_METHOD;
                break;

            case http_method::HTTP_OPTIONS: 
                http_request_.http_method_ = bmx::HTTP_METHODS::OPTIONS_METHOD;
                break;

            case http_method::HTTP_TRACE: 
                http_request_.http_method_ = bmx::HTTP_METHODS::TRACE_METHOD;
                break;

            case http_method::HTTP_PATCH: 
                http_request_.http_method_ = bmx::HTTP_METHODS::PATCH_METHOD;
                break;

            default: 
                http_request_.http_method_ = bmx::HTTP_METHODS::OTHER_METHOD;
                break;
            }

			// TODO: Check when resolved with NGINX http parser.
            // Setting content length.
            //http_request_.body_len_bytes_ = http_parser_.content_length;
            // Checking if content length was determined at all.
            //if (ULLONG_MAX == http_parser_.content_length)
            //    http_request_.body_len_bytes_ = 0;

            // Checking if we have any body at all.
            if (http_request_.body_len_bytes_ > 0)
            {
                // Number of body bytes already received.
                int32_t num_body_bytes_received = accum_buf->get_accum_len_bytes() + SOCKET_DATA_BLOB_OFFSET_BYTES - http_request_.body_offset_;
                
                // Checking if body was partially received at all.
                if (http_request_.body_offset_ <= 0)
                {
                    // Setting the value for body offset.
                    http_request_.body_offset_ = SOCKET_DATA_BLOB_OFFSET_BYTES + bytes_parsed;

                    num_body_bytes_received = 0;
                }

                // Checking if we need to continue receiving the body.
                if (http_request_.body_len_bytes_ > num_body_bytes_received)
                {
                    // Checking for maximum supported HTTP request body size.
                    if (http_request_.body_len_bytes_ > MAX_HTTP_BODY_SIZE)
                    {
                        // Handled successfully.
                        *is_handled = true;

                        // Setting disconnect after send flag.
                        sd->set_disconnect_after_send_flag(true);

#ifdef GW_WARNINGS_DIAG
                        GW_COUT << "Maximum supported HTTP request body size is 32 Mb!" << std::endl;
#endif

                        // Sending corresponding HTTP response.
                        return SendPredefinedResponse(gw, sd, kHttpTooBigUpload, kHttpTooBigUploadLength);
                    }

                    // Enabling accumulative state.
                    sd->set_accumulating_flag(true);

                    // Setting the desired number of bytes to accumulate.
                    accum_buf->set_desired_accum_bytes(accum_buf->get_accum_len_bytes() + http_request_.body_len_bytes_ - num_body_bytes_received);

                    // Continue receiving.
                    accum_buf->ContinueReceive();

                    // Trying to continue accumulation.
                    bool is_accumulated;
                    uint32_t err_code = sd->ContinueAccumulation(gw, &is_accumulated);
                    GW_ERR_CHECK(err_code);

                    // Handled successfully.
                    *is_handled = true;

                    // Checking if we have not accumulated everything yet.
                    return gw->Receive(sd);
                }
            }

ALL_DATA_ACCUMULATED:

            // Posting cloning receive since all data is accumulated.
            err_code = sd->CloneToReceive(gw);
            GW_ERR_CHECK(err_code);

            // Checking special case when session is attached to socket,
            // but no session cookie is presented.
            if ((0 == http_request_.session_string_offset_) && (g_gateway.GetGlobalSessionDataCopy(sd_ref_->get_session_index()).IsValid()))
                sd->ResetSession();

            // Checking type of response.
            switch (resp_type_)
            {
                case HTTP_STANDARD_RESPONSE:
                {
                    // Setting request properties.
                    http_request_.request_offset_ = SOCKET_DATA_BLOB_OFFSET_BYTES;
                    http_request_.request_len_bytes_ = accum_buf->get_accum_len_bytes();

                    // Resetting user data parameters.
                    sd->ResetUserDataOffset();
                    sd->ResetMaxUserDataBytes();

                    // Push chunk to corresponding channel/scheduler.
                    gw->PushSocketDataToDb(sd, handler_id);

                    break;
                }
                    
                case HTTP_GATEWAY_PONG_RESPONSE:
                {
                    // Sending Pong response.
                    err_code = SendPredefinedResponse(gw, sd, kHttpGatewayPongResponse, kHttpGatewayPongResponseLength);
                    if (err_code)
                        return err_code;

                    break;
                }

                default:
                {
                    // Sending no-content response.
                    err_code = SendPredefinedResponse(gw, sd, kHttpNoContent, kHttpNoContentLength);
                    if (err_code)
                        return err_code;

                    break;
                }
            }

            // Printing the outgoing packet.
#ifdef GW_HTTP_DIAG
            GW_COUT << accum_buf->get_orig_buf_ptr() << std::endl;
#endif
        }

        // Handled successfully.
        *is_handled = true;

        return 0;
    }
    // Checking if data comes from user code.
    else
    {
        // Checking if we are already passed the WebSockets handshake.
        if(sd->get_web_sockets_upgrade_flag())
        {
            // Handled successfully.
            *is_handled = true;

            return ws_proto_.ProcessWsDataFromDb(gw, sd, handler_id);
        }

        // Correcting the session cookie.
        if (sd->get_new_session_flag())
        {
            // New session cookie was created searching for it.
            char* session_cookie = strstr((char*)sd->get_data_blob(), kScSessionIdString);
            assert(NULL != session_cookie);

            // Skipping cookie header and equality symbol.
            session_cookie += kScSessionIdStringLength + 1;

            // Writing gateway session index.
            ScSessionStruct session = g_gateway.GetGlobalSessionDataCopy(sd->get_session_index());
            if (session.IsValid())
                session.ConvertToString(session_cookie);

            // Session has been created.
            sd->set_new_session_flag(false);
        }

        // Prepare buffer to send outside.
        sd->get_accum_buf()->PrepareForSend(sd->UserDataBuffer(), sd->get_user_data_written_bytes());

        // Sending data.
        err_code = gw->Send(sd);
        GW_ERR_CHECK(err_code);

        // Handled successfully.
        *is_handled = true;

        return 0;
    }

    return SCERRGWHTTPPROCESSFAILED;
}

// Outer HTTP/WebSockets handler.
uint32_t OuterUriProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_ws_proto()->HttpUriDispatcher(gw, sd, handler_id, is_handled);
}

// HTTP/WebSockets handler.
uint32_t UriProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_ws_proto()->HttpWsProcessData(gw, sd, handler_id, is_handled);
}

} // namespace network
} // namespace starcounter
