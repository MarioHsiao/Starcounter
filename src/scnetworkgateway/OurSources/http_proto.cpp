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

const int32_t kHttpGatewayPongResponseLength = static_cast<int32_t> (strlen(kHttpGatewayPongResponse));

const char* const kHttpServiceUnavailable =
    "HTTP/1.1 503 Service Unavailable\r\n"
    "Content-Length: 0\r\n"
    "\r\n";

const int32_t kHttpServiceUnavailableLength = static_cast<int32_t> (strlen(kHttpServiceUnavailable));

const char* const kHttpTooBigUpload =
    "HTTP/1.1 413 Request Entity Too Large\r\n"
    "Content-Type: text/html; charset=UTF-8\r\n"
    "Content-Length: 50\r\n"
    "\r\n"
    "Maximum supported HTTP request content size is 32 Mb!";

const int32_t kHttpTooBigUploadLength = static_cast<int32_t> (strlen(kHttpTooBigUpload));

const char* const kHttpNoContent =
    "HTTP/1.1 204 No Content\r\n"
    "Content-Length: 0\r\n"
    "\r\n";

const int32_t kHttpNoContentLength = static_cast<int32_t> (strlen(kHttpNoContent));

const char* const kHttpBadRequest =
    "HTTP/1.1 400 Bad Request\r\n"
    "Content-Length: 0\r\n"
    "\r\n";

const int32_t kHttpBadRequestLength = static_cast<int32_t> (strlen(kHttpBadRequest));

const char* const kHttpNotFoundPrefix =
    "HTTP/1.1 404 Not Found\r\n"
    "Content-Type: text/html; charset=UTF-8\r\n"
    "Content-Length: ";

const int32_t kHttpNotFoundPrefixLength = static_cast<int32_t> (strlen(kHttpNotFoundPrefix));

const char* const kHttpNotFoundMessage = "URI not found: ";
const int32_t kHttpNotFoundMessageLength = static_cast<int32_t> (strlen(kHttpNotFoundMessage));

//////////////////////////////////////////////////////////
/////////////////THREAD STATIC DATA///////////////////////
//////////////////////////////////////////////////////////

__declspec(thread) http_parser g_ts_http_parser_;
__declspec(thread) uint8_t g_ts_last_field_;
__declspec(thread) bool g_xhreferer_read_;
__declspec(thread) SocketDataChunk* g_ts_sd_;
__declspec(thread) GatewayWorker* g_ts_gw_;
__declspec(thread) HttpRequest* g_ts_http_request_;

// Constructs HTTP 404 response.
inline int32_t ConstructHttp404(uint8_t* const dest, const int32_t dest_max_bytes, const char* uri, const int32_t uri_len)
{
    GW_ASSERT(dest_max_bytes > 128);

    int32_t content_len = kHttpNotFoundMessageLength + uri_len;

    if (content_len >= dest_max_bytes - 128)
        content_len = kHttpNotFoundMessageLength;

    char cont_len_string[16];
    _itoa_s(content_len, cont_len_string, 16, 10);

    int32_t offset = 0;
    offset = InjectData(dest, offset, kHttpNotFoundPrefix, kHttpNotFoundPrefixLength);
    offset = InjectData(dest, offset, cont_len_string, static_cast<int32_t>(strlen(cont_len_string)));
    offset = InjectData(dest, offset, "\r\n\r\n", 4);
    offset = InjectData(dest, offset, kHttpNotFoundMessage, kHttpNotFoundMessageLength);

    if (content_len > kHttpNotFoundMessageLength)
        offset = InjectData(dest, offset, uri, uri_len);

    return offset;
}

// Destructor.
RegisteredUris::~RegisteredUris()
{
    uri_matcher_entry_ = NULL;
}

// Running all registered handlers.
uint32_t RegisteredUri::RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd, bool* is_handled)
{
    uint32_t err_code;

    // Going through all handler list.
    for (int32_t i = 0; i < handler_lists_.get_num_entries(); i++)
    {
        // Ensuring that initial database is zero.
        //GW_ASSERT(0 == sd->get_db_index());

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

    // Checking if method and URI has correct length.
    if (pos > uri_max_len)
        return SCERRGWWRONGHTTPDATA;

    // Checking that we have HTTP protocol.
    if (pos < (http_data_len - 4))
    {
        // Checking for HTTP keyword.
        if (*(uint32_t*)(http_data + pos) != *(uint32_t*)"HTTP")
        {
            // Wrong protocol.
            return SCERRGWWRONGHTTPDATA;
        }
    }
    else
    {
        // Either wrong protocol or not enough accumulated data.
        return SCERRGWRECEIVEMORE;
    }

    // Setting output length.
    *out_method_and_uri_len = pos;

    return 0;
}

// Fetches method and URI from HTTP request data.
inline uint32_t GetMethodAndUriLowerCase(
    char* http_data,
    uint32_t http_data_len,
    char* out_methoduri_lower_case,
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

    // Checking if method and URI has correct length.
    if (pos > uri_max_len)
        return SCERRGWWRONGHTTPDATA;

    // Checking that we have HTTP protocol.
    if (pos < (http_data_len - 4))
    {
        // Checking for HTTP keyword.
        if (*(uint32_t*)(http_data + pos) != *(uint32_t*)"HTTP")
        {
            // Wrong protocol.
            return SCERRGWWRONGHTTPDATA;
        }
    }
    else
    {
        // Either wrong protocol or not enough accumulated data.
        return SCERRGWRECEIVEMORE;
    }

    // Copying string.
    strncpy_s(out_methoduri_lower_case, pos + 1, http_data, pos);

    // Converting to lower case.
    _strlwr_s(out_methoduri_lower_case + (*out_uri_offset), pos + 1 - (*out_uri_offset));

    // Setting output length.
    *out_method_and_uri_len = pos;

    return 0;
}

inline int HttpProto::OnMessageBegin(http_parser* p)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "OnMessageBegin" << GW_ENDL;
#endif

    return 0;
}

inline int HttpProto::OnHeadersComplete(http_parser* p)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "OnHeadersComplete" << GW_ENDL;
#endif

    // Setting complete header flag.
    g_ts_sd_->set_complete_header_flag();
    
    // NOTE: p->nread already points to the content after additional \r\n
    // thats why we subtracting 2 bytes.

    // Setting headers length (skipping 2 bytes for \r\n).
    g_ts_http_request_->headers_len_bytes_ = p->nread - 2 - g_ts_http_request_->headers_len_bytes_;

    // Setting content data offset.
    g_ts_http_request_->content_offset_ = g_ts_sd_->GetAccumOrigBufferSocketDataOffset() + p->nread;

    return 0;
}

inline int HttpProto::OnMessageComplete(http_parser* p)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "OnMessageComplete" << GW_ENDL;
#endif

    return 0;
}

inline int HttpProto::OnUri(http_parser* p, const char *at, size_t length)
{
#ifdef GW_HTTP_DIAG
    char at_ref[256];
    memcpy(at_ref, at, length);
    at_ref[length] = '\0';
    GW_COUT << "OnUri: " << at_ref << GW_ENDL;
#endif

    // Setting the reference to URI.
    g_ts_http_request_->uri_offset_ = static_cast<uint16_t>(at - (char *)g_ts_sd_);
    g_ts_http_request_->uri_len_bytes_ = static_cast<uint16_t>(length);

    return 0;
}

inline int HttpProto::OnHeaderField(http_parser* p, const char *at, size_t length)
{
#ifdef GW_HTTP_DIAG
    char at_ref[256];
    memcpy(at_ref, at, length);
    at_ref[length] = '\0';
    GW_COUT << "OnHeaderField: " << at_ref << GW_ENDL;
#endif

    // Determining what header field is that.
    g_ts_last_field_ = DetermineField(at, length);

    // Setting headers beginning.
    if (!g_ts_http_request_->headers_offset_)
    {
        g_ts_http_request_->headers_len_bytes_ = static_cast<uint16_t>(p->nread - length - 1);
        g_ts_http_request_->headers_offset_ = static_cast<uint16_t>(at - (char*)g_ts_sd_);
    }

    return 0;
}

// Processes the session information.
inline void HttpProto::ProcessSessionString(SocketDataChunkRef sd, const char* session_id_start)
{
    // Parsing the session.
    sd->GetSessionStruct()->FillFromString(session_id_start, MixedCodeConstants::SESSION_STRING_LEN_CHARS);

    // Setting the session offset.
    http_request_.session_string_offset_ = static_cast<uint16_t>(session_id_start - (char*)sd);

    // Checking if we have session related socket.
    sd->SetGlobalSessionIfEmpty();
}

inline int HttpProto::OnHeaderValue(http_parser* p, const char *at, size_t length)
{
#ifdef GW_HTTP_DIAG
    char at_ref[256];
    memcpy(at_ref, at, length);
    at_ref[length] = '\0';
    GW_COUT << "OnHeaderValue: " << at_ref << GW_ENDL;
#endif

    // Processing last field type.
    switch (g_ts_last_field_)
    {
        case REFERRER_FIELD:
        {
            // Do nothing if X-Referer field is already processed.
            if (g_xhreferer_read_)
                break;
        }

        case XREFERRER_FIELD:
        {
            // Pointing to the actual value of a session.
            const char* session_id_start = at + length - MixedCodeConstants::SESSION_STRING_LEN_CHARS;

            // Checking if Starcounter session id is presented.
            if ((MixedCodeConstants::SESSION_STRING_LEN_CHARS < length) &&
                (*(session_id_start - 1) == '/'))
            {
               g_ts_sd_->get_http_proto()->ProcessSessionString(g_ts_sd_, session_id_start);

               // Checking if X-Referer field is read.
               if (XREFERRER_FIELD == g_ts_last_field_)
                   g_xhreferer_read_ = true;
            }

            break;
        }

        case COOKIE_FIELD:
        {
            // Null terminating the string.
            char c = at[length - 1];
            ((char*)at)[length - 1] = '\0';

            // Checking if session is contained within cookies.
            const char* session_cookie = strstr(at, MixedCodeConstants::ScSessionCookieName);
            ((char*)at)[length - 1] = c;

            // XReferer has higher priority and overrides session value.
            if (session_cookie && !g_xhreferer_read_)
            {
                // Skipping session cookie name and equals character.
                int32_t offset = MixedCodeConstants::ScSessionCookieNameLength + 1;
                while (session_cookie[offset] == ' ')
                    offset++;

                g_ts_sd_->get_http_proto()->ProcessSessionString(g_ts_sd_, session_cookie + offset);
            }

            break;
        }

        case CONTENT_LENGTH_FIELD:
        {
            // Calculating content length.
            g_ts_http_request_->content_len_bytes_ = (int32_t) p->content_length;

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
                g_ts_http_request_->gzip_accepted_ = true;

            // Restoring old character.
            atnull[length] = c;

            break;
        }

        case UPGRADE_FIELD:
        {
            // Double checking if its a WebSocket upgrade.
            if (*(uint32_t*)(at + 4) != *(uint32_t*)"ocke")
                return SCERRGWHTTPNONWEBSOCKETSUPGRADE;

            break;
        }

        case WS_KEY_FIELD:
        {
            GW_ASSERT_DEBUG(24 == length);
            g_ts_sd_->get_ws_proto()->SetClientKey((char *)at, static_cast<int32_t>(length));
            break;
        }

        case WS_PROTOCOL_FIELD:
        {
            if (length > 32)
                return SCERRGWHTTPINCORRECTDATA;

            g_ts_sd_->get_ws_proto()->SetSubProtocol((char *)at, static_cast<int32_t>(length));
            break;
        }

        case WS_VERSION_FIELD:
        {
            // Checking the WebSocket protocol version.
            if (*(uint16_t*)at != *(uint16_t*)"13")
                return SCERRGWHTTPWRONGWEBSOCKETSVERSION;

            break;
        }

        case SCHEDULER_ID_FIELD:
        {
            uint8_t sched_id = atoi(at);
            g_ts_sd_->set_scheduler_id(sched_id);

            break;
        }

        case LOOP_HOST_FIELD:
        {
            // Checking if value is true.
            if (*(uint32_t*)at != *(uint32_t*)"True")
                break;

            g_ts_sd_->set_chunk_looping_host_flag();

            SocketDataChunk* sd_send_clone = NULL;
            uint32_t err_code = g_ts_sd_->CloneToPush(g_ts_gw_, &sd_send_clone);
            GW_ASSERT(0 == err_code);

            // Sending OK response to the client so it does not wait.
            err_code = g_ts_gw_->SendPredefinedMessage(sd_send_clone, kHttpOKResponse, kHttpOKResponseLength);
            GW_ASSERT(0 == err_code);

            break;
        }
    }

    return 0;
}

inline int HttpProto::OnBody(http_parser* p, const char *at, size_t length)
{
#ifdef GW_HTTP_DIAG
    GW_COUT << "OnBody" << GW_ENDL;
#endif

    // Setting content parameters.
    if (g_ts_http_request_->content_len_bytes_ < 0)
        g_ts_http_request_->content_len_bytes_ = static_cast<uint32_t>(length);

    return 0;
}

// Global HTTP parser settings.
http_parser_settings g_httpParserSettings;

void HttpGlobalInit()
{
    // Setting HTTP callbacks.
    g_httpParserSettings.on_body = HttpProto::OnBody;
    g_httpParserSettings.on_header_field = HttpProto::OnHeaderField;
    g_httpParserSettings.on_header_value = HttpProto::OnHeaderValue;
    g_httpParserSettings.on_headers_complete = HttpProto::OnHeadersComplete;
    g_httpParserSettings.on_message_begin = HttpProto::OnMessageBegin;
    g_httpParserSettings.on_message_complete = HttpProto::OnMessageComplete;
    g_httpParserSettings.on_url = HttpProto::OnUri;
}

// Determines the correct HTTP handler.
uint32_t HttpProto::HttpUriDispatcher(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_index,
    bool* is_handled)
{
    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Checking if we are already passed the WebSockets handshake.
        if (sd->is_web_socket())
            return sd->get_ws_proto()->ProcessWsDataToDb(gw, sd, handler_index, is_handled);

        // Obtaining method and URI.
        char* method_and_uri = (char*) sd->get_accum_buf()->get_chunk_orig_buf_ptr();
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
            (char*)sd->get_accum_buf()->get_orig_buf_ptr(),
            sd->get_accum_buf()->get_accum_len_bytes(),
            method_and_uri_lower_case,
            &method_and_uri_len,
            &uri_offset,
            bmx::MAX_URI_STRING_LEN);
        */

        // Checking for any errors.
        if (err_code) {

            if (SCERRGWRECEIVEMORE == err_code) {

                // Checking if we are proxying.
                if (sd->HasProxySocket()) {

                    // Set the unknown proxied protocol here.
                    sd->set_unknown_proxied_proto_flag();

                    // Just running proxy processing.
                    return GatewayHttpWsReverseProxy(NULL, gw, sd, handler_index, is_handled);
                }

                // Returning socket to receiving state.
                err_code = gw->Receive(sd);
                GW_ERR_CHECK(err_code);

                // Handled successfully.
                *is_handled = true;

                return 0;

            } else {

                return err_code;
            }
        }

        // Now we have method and URI and ready to search specific URI handler.

        // Getting the corresponding port number.
        ServerPort* server_port = g_gateway.get_server_port(sd->GetPortIndex());
        uint16_t port_num = server_port->get_port_number();
        RegisteredUris* port_uris = server_port->get_registered_uris();

        // Determining which matched handler to pick.
        uri_index_type matched_index = INVALID_URI_INDEX;

        // Checking if URI matching code is generated.
        if (false == port_uris->HasGeneratedUriMatcher())
        {
            // Checking if there are any port URIs registered,
            if (port_uris->IsEmpty())
                return SCERRREQUESTONUNREGISTEREDURI;

            // Checking if its gateway handlers.
            if (server_port->get_port_number() == g_gateway.get_setting_internal_system_port()) {

                // Checking if its a gateway handler.
                matched_index = g_gateway.CheckIfGatewayHandler(method_and_uri, method_and_uri_len);

                // Checking that its correct index for URI.
                if (INVALID_URI_INDEX != matched_index) {
                    GW_ASSERT(0 == strncmp(method_and_uri, port_uris->GetEntryByIndex(matched_index)->get_processed_uri_info(), method_and_uri_len));
                }
            }

            // Checking if we failed to find again.
            if (INVALID_URI_INDEX == matched_index)
            {
                // Entering global lock.
                gw->EnterGlobalLock();

                // Checking once again since maybe it was already generated.
                if (false == port_uris->HasGeneratedUriMatcher())
                {
                    // Trying to get cached URI matcher.
                    UriMatcherCacheEntry* cached_uri_matcher = server_port->TryGetUriMatcherFromCache();

                    if (NULL != cached_uri_matcher) {

                        // Setting URI matcher from cache.
                        port_uris->SetGeneratedUriMatcher(cached_uri_matcher);

                    } else {
                        // Generating and loading URI matcher.
                        err_code = g_gateway.GenerateUriMatcher(server_port, port_uris);
                    }
                }

                // Releasing global lock.
                gw->LeaveGlobalLock();

                if (err_code)
                    return err_code;
            }
            else
            {
                goto HANDLER_MATCHED;
            }
        }

        // Getting the matched uri index.
        matched_index = port_uris->RunCodegenUriMatcher(method_and_uri, method_and_uri_len, sd->get_accept_or_params_data());

        // Checking if we failed to find again.
        if (matched_index < 0)
        {
            // Handled successfully.
            *is_handled = true;

            // Sending resource not found and closing the connection.
            sd->set_disconnect_after_send_flag();

            // Creating 404 message.
            char stack_temp_mem[512];
            int32_t resp_len_bytes = ConstructHttp404((uint8_t*)stack_temp_mem, 512, method_and_uri, method_and_uri_len);

            return gw->SendPredefinedMessage(sd, stack_temp_mem, resp_len_bytes);
        }

HANDLER_MATCHED:

        // Getting matched URI index.
        RegisteredUri* matched_uri = port_uris->GetEntryByIndex(matched_index);

        // Setting matched URI index.
        sd->SetDestDbIndex(matched_uri->GetFirstDbIndex());

        // Checking if we have a session parameter.
        if (matched_uri->get_session_param_index() != INVALID_PARAMETER_INDEX)
        {
            MixedCodeConstants::UserDelegateParamInfo* p = ((MixedCodeConstants::UserDelegateParamInfo*)sd->get_accept_or_params_data()) + matched_uri->get_session_param_index();
            ProcessSessionString(sd, method_and_uri + p->offset_);
        }

        // Setting determined HTTP URI settings (e.g. for reverse proxy).
        sd->get_http_proto()->http_request_.uri_offset_ = sd->GetAccumOrigBufferSocketDataOffset() + uri_offset;
        sd->get_http_proto()->http_request_.uri_len_bytes_ = method_and_uri_len - uri_offset;

        // Running determined handler now.
        return matched_uri->RunHandlers(gw, sd, is_handled);
    }
    else
    {
        // Just running standard response processing.
        return AppsHttpWsProcessData(hl, gw, sd, handler_index, is_handled);
    }

    return 0;
}

// Resets the parser related fields.
void HttpProto::ResetParser(GatewayWorker *gw, SocketDataChunkRef sd)
{
    g_ts_last_field_ = UNKNOWN_FIELD;
    g_ts_http_request_ = sd->get_http_proto()->get_http_request();
    g_ts_sd_ = sd;
    g_ts_gw_ = gw;
    g_xhreferer_read_ = false;

    http_request_.Reset();

    http_parser_init(&g_ts_http_parser_, HTTP_REQUEST);
}

// Parses the HTTP request and pushes processed data to database.
uint32_t HttpProto::AppsHttpWsProcessData(
    HandlersList* hl,
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
        if (sd->get_complete_header_flag())
            goto ALL_DATA_ACCUMULATED;

        // Checking if we are already passed the WebSockets handshake.
        if (sd->is_web_socket())
            return sd->get_ws_proto()->ProcessWsDataToDb(gw, sd, handler_id, is_handled);

        // Resetting the parsing structure.
        ResetParser(gw, sd);

        // We can immediately set the request offset.
        http_request_.request_offset_ = sd->GetAccumOrigBufferSocketDataOffset();

        // Executing HTTP parser.
        size_t bytes_parsed = http_parser_execute(
            &g_ts_http_parser_,
            &g_httpParserSettings,
            (const char *)accum_buf->get_chunk_orig_buf_ptr(),
            accum_buf->get_accum_len_bytes());

        // Checking if we should continue receiving the headers.
        if (!sd->get_complete_header_flag())
        {
            // NOTE: At this point we don't really know here what the final size of the request will be.
            // That is why we just extend the chunk to next bigger one, without specifying the size.

            // Checking if any space left in chunk.
            if (sd->get_accum_buf()->get_chunk_num_available_bytes() <= 0)
            {
                err_code = SocketDataChunk::ChangeToBigger(gw, sd);

                if (err_code)
                    return err_code;
            }

            // Returning socket to receiving state.
            err_code = gw->Receive(sd);
            if (err_code)
                return err_code;

            // Handled successfully.
            *is_handled = true;

            return 0;
        }

        // Calculating total request size.
        http_request_.request_len_bytes_ = http_request_.content_offset_ - http_request_.request_offset_ + http_request_.content_len_bytes_;

        // Checking if we have WebSockets upgrade.
        if (g_ts_http_parser_.upgrade)
        {
#ifdef GW_WEBSOCKET_DIAG
            GW_COUT << "Upgrade to another protocol detected, data: " << GW_ENDL;
#endif

            // Perform WebSockets handshake.
            return WsProto::DoHandshake(gw, sd, handler_id, is_handled);
        }
        // Handle error. Usually just close the connection.
        else if (bytes_parsed != (accum_buf->get_accum_len_bytes()))
        {
            GW_ASSERT(bytes_parsed < accum_buf->get_accum_len_bytes());

            GW_COUT << "HTTP packet has incorrect data!" << GW_ENDL;
            return SCERRGWHTTPINCORRECTDATA;
        }
        // Standard HTTP.
        else
        {
            // Getting the HTTP method.
            http_method method = (http_method)g_ts_http_parser_.method;
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

            // Checking if we have any content at all.
            if (http_request_.content_len_bytes_ > 0)
            {
                GW_ASSERT(http_request_.content_offset_ > http_request_.headers_offset_);

                // Checking if we need to continue receiving the content.
                if (http_request_.request_len_bytes_ > accum_buf->get_accum_len_bytes())
                {
                    // Checking for maximum supported HTTP request content size.
                    if (http_request_.content_len_bytes_ > g_gateway.setting_maximum_receive_content_length())
                    {
                        // Handled successfully.
                        *is_handled = true;

                        // Setting disconnect after send flag.
                        sd->set_disconnect_after_send_flag();

#ifdef GW_WARNINGS_DIAG
                        GW_COUT << "Maximum supported HTTP request content size is 32 Mb!" << GW_ENDL;
#endif

                        // Sending corresponding HTTP response.
                        return gw->SendPredefinedMessage(sd, kHttpTooBigUpload, kHttpTooBigUploadLength);
                    }

                    // Setting the desired number of bytes to accumulate.
                    err_code = gw->StartAccumulation(
                        sd,
                        http_request_.request_len_bytes_,
                        accum_buf->get_accum_len_bytes());

                    if (err_code)
                        return err_code;

                    // Handled successfully.
                    *is_handled = true;

                    // Checking if we have not accumulated everything yet.
                    return gw->Receive(sd);
                }
            }

ALL_DATA_ACCUMULATED:

            // We don't need complete header flag anymore.
            sd->reset_complete_header_flag();

            g_gateway.IncrementNumProcessedHttpRequests();

            // Aggregation is done separately.
            if (!sd->GetSocketAggregatedFlag())
            {
                // Posting cloning receive since all data is accumulated.
                err_code = sd->CloneToReceive(gw);
                if (err_code)
                    return err_code;
            }

            // Checking type of response.
#ifdef GW_PONG_MODE

            // Sending Pong response.
            err_code = gw->SendPredefinedMessage(sd, kHttpGatewayPongResponse, kHttpGatewayPongResponseLength);
            if (err_code)
                return err_code;

#else

            // Resetting user data parameters.
            sd->ResetUserDataOffset();

            // Push chunk to corresponding channel/scheduler.
            err_code = gw->PushSocketDataToDb(sd, handler_id);
            if (err_code)
                return err_code;

#endif

            // Printing the outgoing packet.
#ifdef GW_HTTP_DIAG
            GW_COUT << sd->get_accum_buf()->get_chunk_orig_buf_ptr() << GW_ENDL;
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
        if (sd->is_web_socket())
            return sd->get_ws_proto()->ProcessWsDataFromDb(gw, sd, handler_id, is_handled);

        // Handled successfully.
        *is_handled = true;

        // Checking if we want to disconnect the socket.
        if (sd->get_disconnect_socket_flag())
            return SCERRGWDISCONNECTFLAG;

        // Prepare buffer to send outside.
        sd->PrepareForSend(sd->UserDataBuffer(), sd->get_user_data_length_bytes());

        // Sending data.
        err_code = gw->Send(sd);
        GW_ERR_CHECK(err_code);

        return 0;
    }

    return SCERRGWHTTPPROCESSFAILED;
}

// Outer HTTP/WebSockets handler.
uint32_t OuterUriProcessData(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_proto()->HttpUriDispatcher(hl, gw, sd, handler_id, is_handled);
}

// HTTP/WebSockets handler for Apps.
uint32_t AppsUriProcessData(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_proto()->AppsHttpWsProcessData(hl, gw, sd, handler_id, is_handled);
}

// HTTP/WebSockets handler for Gateway proxy.
uint32_t GatewayUriProcessProxy(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    return sd->get_http_proto()->GatewayHttpWsReverseProxy(hl, gw, sd, handler_id, is_handled);
}

// Reverse proxies the HTTP traffic.
uint32_t HttpProto::GatewayHttpWsReverseProxy(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled)
{
    uint32_t err_code;

    // Not handled yet.
    *is_handled = true;

    // Checking if already in proxy mode.
    if (sd->HasProxySocket())
    {
        // Aggregation is done separately.
        if (!sd->GetSocketAggregatedFlag())
        {
            // Posting cloning receive for client.
            err_code = sd->CloneToReceive(gw);
            if (err_code)
                return err_code;
        }

        // Making sure that sd is just send.
        sd->reset_socket_representer_flag();

        // Finished receiving from proxied server,
        // now sending to the original user.
        sd->ExchangeToProxySocket(gw);

        // Setting number of bytes to send.
        sd->get_accum_buf()->PrepareToSendOnProxy();

        // Sending data to user.
        return gw->Send(sd);
    }
    else // We have not started a proxy mode yet.
    {
        // Creating new socket to proxied server.
        err_code = gw->CreateProxySocket(sd);
        if (err_code)
            return err_code;

#ifdef GW_SOCKET_DIAG
        GW_COUT << "Created proxy socket: " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

        // Setting number of bytes to send.
        sd->get_accum_buf()->PrepareToSendOnProxy();

        // Getting proxy information.
        ReverseProxyInfo* proxy_info = hl->get_reverse_proxy_info();

        // Connecting to the server.
        return gw->Connect(sd, &proxy_info->destination_addr_);
    }

    return SCERRGWHTTPPROCESSFAILED;
}

// HTTP/WebSockets statistics for Gateway.
uint32_t GatewayStatisticsInfo(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    int32_t resp_len_bytes;
    const char* stats_page_string = g_gateway.GetGlobalStatisticsString(&resp_len_bytes);
    *is_handled = true;

    return gw->SendPredefinedMessage(sd, stats_page_string, resp_len_bytes);
}

// Profilers statistics for Gateway.
uint32_t GatewayTestSample(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
#ifdef GW_DEV_DEBUG
    std::stringstream str_stream;
    str_stream << "Number of allocations: " << g_NumAllocationsCounter << " " << g_NumAlignedAllocationsCounter;
    std::string tmp_str = str_stream.str();

    const char* test_msg = tmp_str.c_str(); //"Starcounter gateway test response :)";

#else

    const char* test_msg = "Starcounter gateway test response :)";

#endif

    *is_handled = true;

    return gw->SendHttpBody(sd, test_msg, (int32_t)strlen(test_msg));
}

// Profilers statistics for Gateway.
uint32_t GatewayProfilersInfo(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    gw->EnterGlobalLock();

    int32_t resp_len_bytes;
    std::string s = g_gateway.GetGlobalProfilersString(&resp_len_bytes);
    *is_handled = true;

    gw->LeaveGlobalLock();

    return gw->SendHttpBody(sd, s.c_str(), resp_len_bytes);
}

} // namespace network
} // namespace starcounter
