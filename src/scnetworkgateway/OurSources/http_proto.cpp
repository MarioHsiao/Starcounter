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

// Destructor.
RegisteredUris::~RegisteredUris()
{
    if (clang_engine_)
    {
        g_gateway.ClangDestroyEngineFunc(clang_engine_);
        clang_engine_ = NULL;
    }
}

// Running all registered handlers.
uint32_t RegisteredUri::RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd, bool* is_handled)
{
    uint32_t err_code;

    // Going through all handler list.
    for (int32_t i = 0; i < handler_lists_.get_num_entries(); i++)
    {
        // Checking if chunk belongs to the destination database.
        if (sd->get_db_index() != handler_lists_[i]->get_db_index())
        {
            // Getting new chunk and copy contents from old one.
            SocketDataChunk* new_sd = NULL;
            err_code = gw->CloneChunkForAnotherDatabase(sd, handler_lists_[i]->get_db_index(), &new_sd);
            if (err_code)
                return err_code;

            // Setting new chunk reference.
            sd = new_sd;
        }

        // Running handlers.
        err_code = handler_lists_[i]->RunHandlers(gw, sd, is_handled);

        // Checking if information was handled and no errors occurred.
        if (*is_handled || err_code)
            return err_code;
    }

    return 0;
}

// Fetches method and URI from HTTP request data.
inline uint32_t GetMethodAndUri(
    char* http_data,
    uint32_t http_data_len,
    uint32_t* out_method_and_uri_len,
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
        {
            pos++;

            break;
        }

        pos++;
    }

    // TODO!
    // Checking that we have HTTP protocol.
    if (pos < http_data_len)
    {
        // Checking for HTTP keyword.
        if (*(uint32_t*)(http_data + pos) != *(uint32_t*)"HTTP")
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
        // Setting output length.
        *out_method_and_uri_len = pos;

        return 0;
    }

    // Wrong protocol.
    return SCERRGWNONHTTPPROTOCOL;
}

// Fetches method and URI from HTTP request data.
inline uint32_t GetMethodAndUriLowerCase(
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
        {
            pos++;
            break;
        }

        pos++;
    }

    // TODO!
    // Checking that we have HTTP protocol.
    if (pos < http_data_len)
    {
        // Checking for HTTP keyword.
        if (*(uint32_t*)(http_data + pos) != *(uint32_t*)"HTTP")
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
        _strlwr_s(out_methoduri_lower_case + (*out_uri_offset), pos + 1 - (*out_uri_offset));

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
    GW_COUT << "OnMessageBegin" << GW_ENDL;
#endif

    HttpWsProto *http = (HttpWsProto *)p;
    return 0;
}

inline int HttpWsProto::OnHeadersComplete(http_parser* p)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "OnHeadersComplete" << GW_ENDL;
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
    GW_COUT << "OnMessageComplete" << GW_ENDL;
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
    GW_COUT << "OnUri: " << at_ref << GW_ENDL;
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
    GW_COUT << "OnHeaderField: " << at_ref << GW_ENDL;
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
    GW_COUT << "OnHeaderValue: " << at_ref << GW_ENDL;
#endif

    HttpWsProto *http = (HttpWsProto *)p;

    // Saving header length.
    http->http_request_.header_value_offsets_[http->http_request_.num_headers_] = at - (char*)http->sd_ref_;
    http->http_request_.header_value_len_bytes_[http->http_request_.num_headers_] = length;

    // Increasing number of saved headers.
    http->http_request_.num_headers_++;
    if (http->http_request_.num_headers_ >= MixedCodeConstants::MAX_PREPARSED_HTTP_REQUEST_HEADERS)
    {
        // Too many HTTP headers.
        GW_COUT << "Too many HTTP headers detected, maximum allowed: " << MixedCodeConstants::MAX_PREPARSED_HTTP_REQUEST_HEADERS << GW_ENDL;
        return SCERRGWHTTPTOOMANYHEADERS;
    }

    // Processing last field type.
    switch (http->last_field_)
    {
        case COOKIE_FIELD:
        {
            // Setting needed HttpRequest fields.
            http->http_request_.cookies_offset_ = at - (char*)http->sd_ref_;
            http->http_request_.cookies_len_bytes_ = length;

            break;
        }

#ifndef GW_NEW_SESSIONS_APPROACH
        case SCSESSIONID_FIELD:
        {
            // Checking if Starcounter session id is presented.
            if (SC_SESSION_STRING_LEN_CHARS == length)
            {
                // Setting the session offset.
                http->http_request_.session_string_offset_ = at - (char*)http->sd_ref_;
                http->http_request_.session_string_len_bytes_ = SC_SESSION_STRING_LEN_CHARS;

                // Reading received session index (skipping session header name and equality).
                session_index_type cookie_session_index = hex_string_to_uint64(at, SC_SESSION_STRING_INDEX_LEN_CHARS);
                if (INVALID_CONVERTED_NUMBER == cookie_session_index)
                {
                    GW_COUT << "Session index stored in the HTTP header has wrong format." << GW_ENDL;
                    return SCERRGWHTTPWRONGSESSIONINDEXFORMAT;
                }

                // Reading received session random salt.
                uint64_t cookie_random_salt = hex_string_to_uint64(at + SC_SESSION_STRING_INDEX_LEN_CHARS, SC_SESSION_STRING_SALT_LEN_CHARS);
                if (INVALID_CONVERTED_NUMBER == cookie_random_salt)
                {
                    GW_COUT << "Session random salt stored in the HTTP header has wrong format." << GW_ENDL;
                    return SCERRGWHTTPWRONGSESSIONSALTFORMAT;
                }

                // Checking if we have existing session.
                ScSessionStruct global_session_copy = g_gateway.GetGlobalSessionCopy(cookie_session_index);

                // Compare this session with existing one.
                if (!global_session_copy.CompareSalts(cookie_random_salt))
                {
#ifdef GW_SESSIONS_DIAG
                    GW_COUT << "Session stored in the HTTP header is wrong/outdated." << GW_ENDL;
#endif

                    // Resetting the session information.
                    http->http_request_.session_string_offset_ = 0;
                    http->http_request_.session_string_len_bytes_ = 0;
                    http->sd_ref_->ResetSdSession();
                }
                else
                {
                    // Attaching existing global session.
                    http->sd_ref_->AssignSession(global_session_copy);
                }
            }

            break;
        }
#endif

        case CONTENT_LENGTH_FIELD:
        {
            // Calculating content length.
            http->http_request_.content_len_bytes_ = ParseDecimalStringToUint(at, length);

            break;
        }

        case ACCEPT_ENCODING_FIELD:
        {
            // Null-terminating the string.
            char* atnull = (char*)at;
            char c = atnull[length];
            atnull[length] = '\0';

            // Checking if Gzip is accepted.
            if (strstr(atnull, "gzip"))
                http->http_request_.gzip_accepted_ = true;

            // Restoring old character.
            atnull[length] = c;

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
            if (*(uint64_t*)at != *(uint64_t*)"websocket")
                return SCERRGWHTTPNONWEBSOCKETSUPGRADE;

            break;
        }

        case WS_KEY_FIELD:
        {
            GW_ASSERT_DEBUG(24 == length);
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
            if (*(uint16_t*)at != *(uint16_t*)"13")
                return SCERRGWHTTPWRONGWEBSOCKETSVERSION;

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
    GW_COUT << "OnBody" << GW_ENDL;
#endif

    HttpWsProto *http = (HttpWsProto *)p;

    // Setting content parameters.
    if (http->http_request_.content_len_bytes_ < 0)
        http->http_request_.content_len_bytes_ = length;

    // Setting content data offset.
    http->http_request_.content_offset_ = at - (char*)http->sd_ref_;

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
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_index,
    bool* is_handled)
{

#ifdef GW_TESTING_MODE

    // Checking if we are in gateway HTTP mode.
    if (MODE_GATEWAY_HTTP == g_gateway.setting_mode())
        return GatewayHttpWsProcessEcho(gw, sd, handler_index, is_handled);

#endif

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Checking if already determined further handler.
        if (INVALID_URI_INDEX != matched_uri_index_)
        {
            // Running determined handler now.

            ServerPort* server_port = g_gateway.get_server_port(sd->get_port_index());
            RegisteredUris* reg_uris = server_port->get_registered_uris();

            return reg_uris->GetEntryByIndex(matched_uri_index_)->RunHandlers(gw, sd, is_handled);
        }

        // Checking if we are already passed the WebSockets handshake.
        if (sd->IsWebSocket())
            return ws_proto_.ProcessWsDataToDb(gw, sd, handler_index, is_handled);

        // Obtaining method and URI.
        char* method_and_uri = (char*)(sd->get_data_blob());
        uint32_t method_and_uri_len, uri_offset;

        // Getting method and URI information.
        uint32_t err_code = GetMethodAndUri(
            method_and_uri,
            sd->get_accum_buf()->get_accum_len_bytes(),
            &method_and_uri_len,
            &uri_offset,
            MixedCodeConstants::MAX_URI_STRING_LEN);

        /*
        // TODO: Support alternative lower-case strategy when URI didn't match.
        method_and_uri_lower_case = gw->get_uri_lower_case();

        // Converting URI to lower case.
        err_code = GetMethodAndUriLowerCase(
            (char*)(sd->get_data_blob()),
            sd->get_accum_buf()->get_accum_len_bytes(),
            method_and_uri_lower_case,
            &method_and_uri_len,
            &uri_offset,
            bmx::MAX_URI_STRING_LEN);
        */

        // Checking for any errors.
        if (err_code)
        {
#ifdef GW_PROXY_MODE

            // Checking if we are proxying.
            if (sd->get_proxied_server_socket_flag())
            {
                // Set the unknown proxied protocol here.
                sd->set_unknown_proxied_proto_flag(true);

                // Just running proxy processing.
                return GatewayHttpWsReverseProxy(gw, sd, handler_index, is_handled);
            }
#endif

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
        RegisteredUris* port_uris = server_port->get_registered_uris();

        // Determining which matched handler to pick.
        int32_t matched_index = INVALID_URI_INDEX;

        // Checking if URI matching code is generated.
        if (NULL == port_uris->get_latest_match_uri_func())
        {
            // Checking if there are any port URIs registered,
            if (port_uris->IsEmpty())
                return SCERRREQUESTONUNREGISTEREDURI;

            // Entering global lock.
            gw->EnterGlobalLock();

            // Checking once again since maybe it was already generated.
            if (NULL == port_uris->get_latest_match_uri_func())
            {
                // Generating and loading URI matcher.
                err_code = g_gateway.GenerateUriMatcher(port_uris);
            }

            // Releasing global lock.
            gw->LeaveGlobalLock();

            if (err_code)
                return err_code;
        }

        // Getting the matched uri index.
        matched_index = port_uris->RunCodegenUriMatcher(method_and_uri, method_and_uri_len, sd->get_accept_data());

        // Checking if we failed to find again.
        if (matched_index < 0)
            return SCERRREQUESTONUNREGISTEREDURI;

        // Getting matched URI index.
        RegisteredUri* matched_uri = port_uris->GetEntryByIndex(matched_index);

        // Checking if we have a session parameter.
        if (matched_uri->get_session_param_index() != INVALID_PARAMETER_INDEX)
        {
            MixedCodeConstants::UserDelegateParamInfo* p = ((MixedCodeConstants::UserDelegateParamInfo*)sd->get_accept_data()) + matched_uri->get_session_param_index();
            sd->GetSessionStruct()->FillFromString(method_and_uri + p->offset_, p->len_);
        }

        // Indicating that matching URI index was found.
        //set_matched_uri_index(matched_index);

        // Setting determined HTTP URI settings (e.g. for reverse proxy).
        sd->get_http_ws_proto()->http_request_.uri_offset_ = SOCKET_DATA_BLOB_OFFSET_BYTES + uri_offset;
        sd->get_http_ws_proto()->http_request_.uri_len_bytes_ = method_and_uri_len - uri_offset;

        // Running determined handler now.
        return matched_uri->RunHandlers(gw, sd, is_handled);
    }
    else
    {
        // Just running standard response processing.
        return AppsHttpWsProcessData(gw, sd, handler_index, is_handled);
    }

    return 0;
}

// Parses the HTTP request and pushes processed data to database.
uint32_t HttpWsProto::AppsHttpWsProcessData(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled)
{
    uint32_t err_code;

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Getting reference to accumulative buffer.
        AccumBuffer* accum_buf = sd->get_accum_buf();

        // Checking if we are in fill-up mode.
        if (sd->get_accumulating_flag())
            goto ALL_DATA_ACCUMULATED;

        // Checking if we are already passed the WebSockets handshake.
        if (sd->IsWebSocket())
            return ws_proto_.ProcessWsDataToDb(gw, sd, handler_id, is_handled);

        // Resetting the parsing structure.
        ResetParser();

        // Attaching a socket.
        AttachToParser(sd);

        // Executing HTTP parser.
        size_t bytes_parsed = http_parser_execute(
            (http_parser *)this,
            &g_httpParserSettings,
            (const char *)sd->get_data_blob(),
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
#ifdef GW_WEBSOCKET_DIAG
            GW_COUT << "Upgrade to another protocol detected, data: " << GW_ENDL;
#endif

            // Perform WebSockets handshake.
            return ws_proto_.DoHandshake(gw, sd, handler_id, is_handled);
        }
        // Handle error. Usually just close the connection.
        else if (bytes_parsed != (accum_buf->get_accum_len_bytes()))
        {
            GW_COUT << "HTTP packet has incorrect data!" << GW_ENDL;
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
            //http_request_.content_len_bytes_ = http_parser_.content_length;
            // Checking if content length was determined at all.
            //if (ULLONG_MAX == http_parser_.content_length)
            //    http_request_.content_len_bytes_ = 0;

            // Checking if we have any content at all.
            if (http_request_.content_len_bytes_ > 0)
            {
                // Number of content bytes already received.
                int32_t num_content_bytes_received = accum_buf->get_accum_len_bytes() + SOCKET_DATA_BLOB_OFFSET_BYTES - http_request_.content_offset_;
                
                // Checking if content was partially received at all.
                if (http_request_.content_offset_ <= 0)
                {
                    // Setting the value for content offset.
                    http_request_.content_offset_ = SOCKET_DATA_BLOB_OFFSET_BYTES + bytes_parsed;

                    num_content_bytes_received = 0;
                }

                // Checking if we need to continue receiving the content.
                if (http_request_.content_len_bytes_ > num_content_bytes_received)
                {
                    // Checking for maximum supported HTTP request content size.
                    if (http_request_.content_len_bytes_ > MAX_HTTP_CONTENT_SIZE)
                    {
                        // Handled successfully.
                        *is_handled = true;

                        // Setting disconnect after send flag.
                        sd->set_disconnect_after_send_flag(true);

#ifdef GW_WARNINGS_DIAG
                        GW_COUT << "Maximum supported HTTP request content size is 32 Mb!" << GW_ENDL;
#endif

                        // Sending corresponding HTTP response.
                        return gw->SendPredefinedMessage(sd, kHttpTooBigUpload, kHttpTooBigUploadLength);
                    }

                    // Enabling accumulative state.
                    sd->set_accumulating_flag(true);

                    // Setting the desired number of bytes to accumulate.
                    accum_buf->set_desired_accum_bytes(accum_buf->get_accum_len_bytes() + http_request_.content_len_bytes_ - num_content_bytes_received);

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

#ifdef GW_COLLECT_SOCKET_STATISTICS
            g_gateway.IncrementNumProcessedHttpRequests();
#endif

            // Skipping cloning when in testing mode.
#ifndef GW_TESTING_MODE
            // Posting cloning receive since all data is accumulated.
            err_code = sd->CloneToReceive(gw);
            GW_ERR_CHECK(err_code);
#endif

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
                    // TODO: Deal with situation when not able to push.
                    gw->PushSocketDataToDb(sd, handler_id);

                    break;
                }
                    
                case HTTP_GATEWAY_PONG_RESPONSE:
                {
                    // Sending Pong response.
                    err_code = gw->SendPredefinedMessage(sd, kHttpGatewayPongResponse, kHttpGatewayPongResponseLength);
                    if (err_code)
                        return err_code;

                    break;
                }

                default:
                {
                    // Sending no-content response.
                    err_code = gw->SendPredefinedMessage(sd, kHttpNoContent, kHttpNoContentLength);
                    if (err_code)
                        return err_code;

                    break;
                }
            }

            // Printing the outgoing packet.
#ifdef GW_HTTP_DIAG
            GW_COUT << sd->get_data_blob() << GW_ENDL;
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
        if (sd->IsWebSocket())
            return ws_proto_.ProcessWsDataFromDb(gw, sd, handler_id, is_handled);

#ifndef GW_NEW_SESSIONS_APPROACH
        // Correcting the session cookie.
        if (sd->get_new_session_flag())
        {
            // New session cookie was created so searching for it.
            char* session_cookie = strstr((char*)sd->get_data_blob(), kFullSessionIdSetCookieString);
            if (NULL == session_cookie)
            {
                // Destroying the socket data since session cookie is not embedded.
                return SCERRGWHTTPCOOKIEISMISSING;
            }

            // Skipping cookie header and equality symbol.
            session_cookie += kSetCookieStringPrefixLength;

            // Getting session global copy.
            ScSessionStruct global_session_copy = g_gateway.GetGlobalSessionCopy(sd->get_session_index());

            // Comparing session salts for correctness.
            bool correct_session = global_session_copy.CompareSalts(sd->get_session_salt());
            GW_ASSERT(true == correct_session);
            
            // Writing gateway session to response cookie.
            global_session_copy.ConvertToString(session_cookie);

            // Session has been created.
            sd->set_new_session_flag(false);
        }
#endif

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

#ifdef GW_TESTING_MODE

// Parses the HTTP request and pushes processed data to database.
uint32_t HttpWsProto::GatewayHttpWsProcessEcho(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled)
{
    uint32_t err_code;

    // Getting reference to accumulative buffer.
    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Checking in what mode we are.
    if (g_gateway.setting_is_master())
    {
        // Checking if we are in fill-up mode.
        if (sd->get_accumulating_flag())
            goto ALL_DATA_ACCUMULATED;

        // Checking if we are already passed the WebSockets handshake.
        if (sd->IsWebSocket())
            return ws_proto_.ProcessWsDataToDb(gw, sd, handler_id, is_handled);

        // Resetting the parsing structure.
        ResetParser();

        // Attaching a socket.
        AttachToParser(sd);

        // Executing HTTP parser.
        size_t bytes_parsed = http_parser_execute(
            (http_parser *)this,
            &g_httpParserSettings,
            (const char *)sd->get_data_blob(),
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
#ifdef GW_WEBSOCKET_DIAG
            GW_COUT << "Upgrade to another protocol detected, data: " << GW_ENDL;
#endif

            // Perform WebSockets handshake.
            return ws_proto_.DoHandshake(gw, sd, handler_id, is_handled);
        }
        // Handle error. Usually just close the connection.
        else if (bytes_parsed != (accum_buf->get_accum_len_bytes()))
        {
            GW_COUT << "HTTP packet has incorrect data!" << GW_ENDL;
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
            //http_request_.content_len_bytes_ = http_parser_.content_length;
            // Checking if content length was determined at all.
            //if (ULLONG_MAX == http_parser_.content_length)
            //    http_request_.content_len_bytes_ = 0;

            // Checking if we have any content at all.
            if (http_request_.content_len_bytes_ > 0)
            {
                // Number of content bytes already received.
                int32_t num_content_bytes_received = accum_buf->get_accum_len_bytes() + SOCKET_DATA_BLOB_OFFSET_BYTES - http_request_.content_offset_;

                // Checking if content was partially received at all.
                if (http_request_.content_offset_ <= 0)
                {
                    // Setting the value for content offset.
                    http_request_.content_offset_ = SOCKET_DATA_BLOB_OFFSET_BYTES + bytes_parsed;

                    num_content_bytes_received = 0;
                }

                // Checking if we need to continue receiving the content.
                if (http_request_.content_len_bytes_ > num_content_bytes_received)
                {
                    // Checking for maximum supported HTTP request content size.
                    if (http_request_.content_len_bytes_ > MAX_HTTP_CONTENT_SIZE)
                    {
                        // Handled successfully.
                        *is_handled = true;

                        // Setting disconnect after send flag.
                        sd->set_disconnect_after_send_flag(true);

#ifdef GW_WARNINGS_DIAG
                        GW_COUT << "Maximum supported HTTP request content size is 32 Mb!" << GW_ENDL;
#endif

                        // Sending corresponding HTTP response.
                        return gw->SendPredefinedMessage(sd, kHttpTooBigUpload, kHttpTooBigUploadLength);
                    }

                    // Enabling accumulative state.
                    sd->set_accumulating_flag(true);

                    // Setting the desired number of bytes to accumulate.
                    accum_buf->set_desired_accum_bytes(accum_buf->get_accum_len_bytes() + http_request_.content_len_bytes_ - num_content_bytes_received);

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

#ifdef GW_COLLECT_SOCKET_STATISTICS
            g_gateway.IncrementNumProcessedHttpRequests();
#endif

            // Translating HTTP content.
            GW_ASSERT(http_request_.content_len_bytes_ == kHttpEchoContentLength);

            // Converting the string to number.
            //echo_id_type echo_id = hex_string_to_uint64((char*)sd + http_request_.content_offset_, kHttpGatewayEchoRequestBodyLength);

            // Saving echo string into temporary buffer.
            char copied_echo_string[kHttpEchoContentLength];
            memcpy(copied_echo_string, (char*)sd + http_request_.content_offset_, kHttpEchoContentLength);

            // Coping echo HTTP response.
            memcpy(sd->get_data_blob(), kHttpEchoResponse, kHttpEchoResponseLength);

            // Inserting echo id into HTTP response.
            memcpy(sd->get_data_blob() + kHttpEchoResponseInsertPoint, copied_echo_string, kHttpEchoContentLength);

            // Sending echo response.
            err_code = gw->SendPredefinedMessage(sd, NULL, kHttpEchoResponseLength);
            if (err_code)
                return err_code;

            // Handled successfully.
            *is_handled = true;

            return 0;
        }
    }
    else
    {
        // Asserting correct number of bytes received.
        GW_ASSERT(sd->get_accum_buf()->get_accum_len_bytes() == kHttpEchoResponseLength);

        // Obtaining original echo number.
        //echo_id_type echo_id = *(int32_t*)sd->get_data_blob();
        echo_id_type echo_id = hex_string_to_uint64((char*)sd->get_data_blob() + kHttpEchoResponseInsertPoint, kHttpEchoContentLength);

#ifdef GW_ECHO_STATISTICS
        GW_COUT << "Received echo: " << echo_id << GW_ENDL;
#endif

#ifdef GW_LIMITED_ECHO_TEST
        // Confirming received echo.
        g_gateway.ConfirmEcho(echo_id);
#endif

        // Handled successfully.
        *is_handled = true;

        // Checking if all echo responses are returned.
        if (g_gateway.CheckConfirmedEchoResponses(gw))
        {
            return SCERRGWTESTFINISHED;
                        
            /*
            EnterGlobalLock();
            g_gateway.ResetEchoTests();
            LeaveGlobalLock();
            return 0;
            */
        }
        else
        {
            goto SEND_HTTP_ECHO_TO_MASTER;
        }

SEND_HTTP_ECHO_TO_MASTER:

        // Checking that not all echoes are sent.
        if (!g_gateway.AllEchoesSent())
        {
            // Generating echo number.
            echo_id_type new_echo_num = 0;

#ifdef GW_LIMITED_ECHO_TEST
            new_echo_num = g_gateway.GetNextEchoNumber();
#endif

            // Sending echo request to server.
            return gw->SendHttpEcho(sd, new_echo_num);
        }
    }

    return SCERRGWHTTPPROCESSFAILED;
}

// HTTP/WebSockets handler for Gateway.
uint32_t GatewayUriProcessEcho(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_ws_proto()->GatewayHttpWsProcessEcho(gw, sd, handler_id, is_handled);
}

#endif

// Outer HTTP/WebSockets handler.
uint32_t OuterUriProcessData(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_ws_proto()->HttpUriDispatcher(gw, sd, handler_id, is_handled);
}

// HTTP/WebSockets handler for Apps.
uint32_t AppsUriProcessData(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_ws_proto()->AppsHttpWsProcessData(gw, sd, handler_id, is_handled);
}

#ifdef GW_PROXY_MODE

// HTTP/WebSockets handler for Gateway proxy.
uint32_t GatewayUriProcessProxy(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_ws_proto()->GatewayHttpWsReverseProxy(gw, sd, handler_id, is_handled);
}

// Reverse proxies the HTTP traffic.
uint32_t HttpWsProto::GatewayHttpWsReverseProxy(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled)
{
    uint32_t err_code;

    // Not handled yet.
    *is_handled = true;

    // Checking if already in proxy mode.
    if (sd->get_proxy_socket() != INVALID_SOCKET)
    {
        // Posting cloning receive for client.
        err_code = sd->CloneToReceive(gw);
        GW_ERR_CHECK(err_code);

        // Finished receiving from proxied server,
        // now sending to the original user.
        sd->ExchangeToProxySocket();

        // Enabling proxy mode.
        sd->set_proxied_server_socket_flag(true);

        // Setting number of bytes to send.
        sd->get_accum_buf()->PrepareForSend();

        // Sending data to user.
        return gw->Send(sd);
    }
    else // We have not started a proxy mode yet.
    {
        // Posting cloning receive for client.
        uint32_t err_code = sd->CloneToReceive(gw);
        GW_ERR_CHECK(err_code);

        // Creating new socket to proxied server.
        err_code = gw->CreateProxySocket(sd);
        GW_ERR_CHECK(err_code);

#ifdef GW_SOCKET_DIAG
        GW_COUT << "Created proxy socket: " << gw->get_sd_receive_clone()->get_socket() << ":" << gw->get_sd_receive_clone()->get_chunk_index() <<
            " -> " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

        // Setting client proxy socket.
        gw->get_sd_receive_clone()->set_proxy_socket(sd->get_socket());

        // Re-enabling socket representer flag.
        sd->set_socket_representer_flag(true);

        // Setting proxy mode.
        sd->set_proxied_server_socket_flag(true);

        // Setting number of bytes to send.
        sd->get_accum_buf()->PrepareForSend();

        // Getting proxy information.
        ReverseProxyInfo* proxy_info = g_gateway.SearchProxiedServerAddress((char*)sd + sd->get_http_ws_proto()->http_request_.uri_offset_);

        // Connecting to the server.
        return gw->Connect(sd, &proxy_info->addr_);
    }

    return SCERRGWHTTPPROCESSFAILED;
}

#endif

// HTTP/WebSockets statistics for Gateway.
uint32_t GatewayStatisticsInfo(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    int32_t resp_len_bytes;
    const char* stats_page_string = g_gateway.GetGlobalStatisticsString(&resp_len_bytes);
    *is_handled = true;
    return gw->SendPredefinedMessage(sd, stats_page_string, resp_len_bytes);
}

} // namespace network
} // namespace starcounter
