#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
#include "worker.hpp"

namespace starcounter {
namespace network {

char *kHttpResponse[3] = {
    "HTTP/1.1 200 OK\r\n"
    //"Cache-Control: private, no-cache\r\n"
    "Content-Type: text/html; charset=UTF-8\r\n"
    "Content-Length: @          \r\n",

    "Set-Cookie: ScSessionId=@                           ; HttpOnly\r\n",

    "\r\n"
    "<html>\r\n"
    "<body>\r\n"
    "<h1>Your unique session: @                           </h1>\r\n"
    "<h1>Number of visits during one session: #              </h1>\r\n"
    "</body>\r\n"
    "</html>\r\n" };

const int32_t kHttpContentLengthOffset = abs(kHttpResponse[0] - strstr(kHttpResponse[0], "@"));
const int32_t kHttpCookieOffset = abs(kHttpResponse[1] - strstr(kHttpResponse[1], "@"));
const int32_t kHttpSessionOffset = abs(kHttpResponse[2] - strstr(kHttpResponse[2], "@"));
const int32_t kHttpNumVisitsOffset = abs(kHttpResponse[2] - strstr(kHttpResponse[2], "#"));
char kHttpContentLength[16];

const char* ScSessionIdString = "ScSessionId";
const int32_t ScSessionIdStringLen = strlen(ScSessionIdString);

const int32_t kHttpResponseLen[3] =
{
    strlen(kHttpResponse[0]),
    strlen(kHttpResponse[1]),
    strlen(kHttpResponse[2])
};

const char* kHttpNoContent =
    "HTTP/1.1 204 No Content\r\n"
    "Content-Length: 0\r\n"
    "Pragma: no-cache\r\n"
    //"Cache-Control: private, no-cache\r\n"
    //"Content-Type: image/ico\r\n"
    "\r\n";

const char* kHttpBadRequest =
    "HTTP/1.1 400 Bad Request\r\n"
    "Content-Length: 0\r\n"
    "Pragma: no-cache\r\n"
    //"Cache-Control: private, no-cache\r\n"
    "\r\n";

const int32_t kHttpNoContentLen = strlen(kHttpNoContent) + 1;

// Fetches method and URI from HTTP request data.
inline uint32_t GetMethodAndUri(char* http_data, uint32_t http_data_len, char* out_uri_lower_case, uint32_t* out_len, uint32_t uri_max_len)
{
    int32_t pos = 0;

    // Reading method.
    while (pos < http_data_len)
    {
        if (http_data[pos] == ' ')
            break;

        pos++;
    }

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
            return 1;
        }
    }
    else
    {
        // Either wrong protocol or not enough accumulated data.
        return 2;
    }

    // Checking if method and URI has correct length.
    if (pos < uri_max_len)
    {
        // Copying string.
        strncpy_s(out_uri_lower_case, pos + 1, http_data, pos);

        // Converting to lower case.
        _strlwr_s(out_uri_lower_case, pos + 1);

        // Setting output length.
        *out_len = pos;

        return 0;
    }

    // Wrong protocol.
    return 1;
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

    // Setting complete data flag.
    http->complete_data_ = true;
    
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

    // Checking if we already took the URI.
    if (http->uri_parsed_)
        return 0;

    // Immediately stopping parsing when URI is determined.
    return 1;
}

// Fast way to determine field type.
inline HttpWsFields DetermineField(const char *at, size_t length)
{
    switch(at[0])
    {
        case 'A':
        {
            switch(at[2])
            {
                case 'c':
                {
                    switch(length)
                    {
                        case 6: return ACCEPT_FIELD; // Accept
                        case 15: return ACCEPT_ENCODING_FIELD; // Accept-Encoding
                    }
                    break;
                }
            }

            break;
        }

        case 'C':
        {
            switch(at[2])
            {
                case 'o':
                {
                    switch(length)
                    {
                        case 6: return COOKIE_FIELD; // Cookie
                    }
                    break;
                }

                case 'n':
                {
                    switch(length)
                    {
                        case 14: return CONTENT_LENGTH; // Content-Length
                    }
                    break;
                }
            }

            break;
        }

        case 'U':
        {
            switch(at[2])
            {
                case 'g':
                {
                    switch(length)
                    {
                        case 7: return UPGRADE_FIELD; // Upgrade
                    }
                    break;
                }
            }

            break;
        }
        
        case 'S':
        {
            switch(at[2])
            {
                case 'c':
                {
                    switch(length)
                    {
                        case 17: return WS_KEY_FIELD; // Sec-WebSocket-Key
                        case 22: return WS_PROTOCOL_FIELD; // Sec-WebSocket-Protocol
                        case 21: return WS_VERSION_FIELD; // Sec-WebSocket-Version
                    }
                    break;
                }
            }

            break;
        }
    }

    return UNKNOWN_FIELD;
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

// Searching for the Starcounter session cookie among other cookies.
// Returns pointer to Starcounter session cookie value.
inline const char* GetSessionIdValue(const char *at, size_t length)
{
    int32_t i = 0;
    if (length >= ScSessionIdStringLen)
    {
        while(i < length)
        {
            // Checking if this cookie is Starcounter session cookie.
            if ((ScSessionIdString[0] == at[i]) &&
                (ScSessionIdString[1] == at[i + 1]) &&
                ('=' == at[i + ScSessionIdStringLen]))
            {
                // Skipping session header name and equality symbol.
                return at + i + ScSessionIdStringLen + 1;
            }
            i++;
        }
    }

    return NULL;
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
        return 1;
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
                uint32_t sessionIndex = hex_string_to_uint64(at + ScSessionIdStringLen + 1, 8);
                if (sessionIndex == INVALID_CONVERTED_NUMBER)
                {
                    GW_COUT << "Session index stored in the HTTP header has wrong format." << std::endl;
                    return 1;
                }

                // Reading received session random salt.
                uint64_t randomSalt = hex_string_to_uint64(at + ScSessionIdStringLen + 1 + 8, 16);
                if (randomSalt == INVALID_CONVERTED_NUMBER)
                {
                    GW_COUT << "Session random salt stored in the HTTP header has wrong format." << std::endl;
                    return 1;
                }

                // Checking if we have existing session.
                if (http->sd_ref_->GetAttachedSession() != NULL)
                {
                    // Compare this session with existing one.
                    if (!http->sd_ref_->GetAttachedSession()->Compare(randomSalt, sessionIndex))
                    {
                        GW_COUT << "Session stored in the HTTP header is wrong." << std::endl;
                        return 1;
                    }
                }
                else
                {
                    // Attaching to existing or creating a new session.
                    SessionData *existingSession = g_gateway.GetSessionData(sessionIndex);
                    if ((existingSession != NULL) && (existingSession->Compare(randomSalt, sessionIndex)))
                    {
                        // Attaching existing session.
                        http->sd_ref_->AttachToSession(existingSession, http->gw_ref_temp_);

                        // Just increase number of visits.
                        if (http->resp_type_ == HTTP_STANDARD_RESPONSE)
                            http->sd_ref_->GetAttachedSession()->IncreaseVisits();
                    }
                    else
                    {
                        GW_COUT << "Given session does not exist: " << sessionIndex << ":" << randomSalt << std::endl;

                        // Given session does not exist, dropping the connection.
                        //return 1;
                    }
                }
            }
            else
            {
                // Checking that session exists.
                if (http->sd_ref_->GetAttachedSession() != NULL)
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

        case CONTENT_LENGTH:
        {
            // Setting body size parameter.
            // TODO: Fix the length parsing.
            //http->http_request_.body_len_bytes_ = length;

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
                return 1;
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
                return 1;
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
    // Calculating the content length.
    itoa(strlen(kHttpResponse[2]) - 2, kHttpContentLength, 10);

    // Setting HTTP callbacks.
    g_httpParserSettings.on_body = HttpWsProto::OnBody;
    g_httpParserSettings.on_header_field = HttpWsProto::OnHeaderField;
    g_httpParserSettings.on_header_value = HttpWsProto::OnHeaderValue;
    g_httpParserSettings.on_headers_complete = HttpWsProto::OnHeadersComplete;
    g_httpParserSettings.on_message_begin = HttpWsProto::OnMessageBegin;
    g_httpParserSettings.on_message_complete = HttpWsProto::OnMessageComplete;
    g_httpParserSettings.on_url = HttpWsProto::OnUri;
}

uint32_t HttpWsProto::HttpUriDispatcher(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_index,
    bool* is_handled)
{
    // Not handled yet.
    *is_handled = false;

    // Checking if data goes to user code.
    if (sd->data_to_user_flag())
    {
        // Getting reference to accumulative buffer.
        AccumBuffer* socketDataBuf = sd->get_data_buf();

        // Attaching a socket.
        AttachSocket(gw, sd);

        // Checking if we are already passed the WebSockets handshake.
        if(sd->get_http_ws_proto()->get_web_sockets_upgrade() == true)
            return ws_proto_.ProcessWsDataToDb(gw, sd, handler_index);

        // Checking that buffer has space for response data.
        if (socketDataBuf->get_accum_len_bytes() >= (DATA_BLOB_SIZE_BYTES - HTTP_WS_MIN_RESPONSE_SIZE))
        {
            GW_COUT << "HTTP/WebSockets header is too big!" << std::endl;
            return 1;
        }

        // Obtaining method and URI.
        char* uri_lower_case = gw->get_uri_lower_case();
        uint32_t uri_len;

        // Checking for any errors.
        uint32_t err_code = GetMethodAndUri(
            (char*)(sd->get_data_buf()->get_orig_buf_ptr()),
            sd->get_data_buf()->get_accum_len_bytes(),
            uri_lower_case,
            &uri_len,
            bmx::MAX_URI_STRING_LEN);

        // Checking for any errors.
        if (err_code)
        {
            // Disconnecting this socket.
            gw->Disconnect(sd);

            // Returning error.
            return 1;

            // TODO!

            // Continue receiving.
            socketDataBuf->ContinueReceive();

            // Returning socket to receiving state.
            gw->Receive(sd);

            // Handled successfully.
            *is_handled = true;

            return 0;
        }

        // Now we have URI and ready to search specific URI handler.

        // Getting the corresponding port number.
        ServerPort* server_port = g_gateway.get_server_port(sd->get_port_index());
        uint16_t port_num = server_port->get_port_number();
        RegisteredUris* reg_uris = server_port->get_registered_uris();

        // Searching matching URI.
        int32_t uri_index = reg_uris->SearchMatchingUriHandler(uri_lower_case, uri_len);

        // Checking if user handler was not found.
        if (uri_index < 0)
        {
            // Disconnecting this socket.
            gw->Disconnect(sd);

            // Returning error.
            return 1;
        }

        // Running determined handler now.
        *is_handled = true;
        return reg_uris->GetEntryByIndex(uri_index).RunHandlers(gw, sd);
    }
    else
    {
        // Just running standard response processing.
        *is_handled = true;
        return HttpWsProcessData(gw, sd, handler_index, is_handled);
    }

    return 0;
}

uint32_t HttpWsProto::HttpWsProcessData(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled)
{
    // Not handled yet.
    *is_handled = false;

    // Checking if data goes to user code.
    if (sd->data_to_user_flag())
    {
        // Getting reference to accumulative buffer.
        AccumBuffer* socketDataBuf = sd->get_data_buf();

        // Attaching a socket.
        AttachSocket(gw, sd);

        // Checking if we are already passed the WebSockets handshake.
        if(sd->get_http_ws_proto()->get_web_sockets_upgrade() == true)
            return ws_proto_.ProcessWsDataToDb(gw, sd, handler_id);

        // Checking that buffer has space for response data.
        if ((socketDataBuf->get_accum_len_bytes()) >= (DATA_BLOB_SIZE_BYTES - HTTP_WS_MIN_RESPONSE_SIZE))
        {
            GW_COUT << "HTTP/WebSockets header is too big!" << std::endl;
            return 1;
        }

        // Resetting the parsing structure.
        ResetParser();

        // Indicating that we have already parsed the URI.
        uri_parsed_ = true;

        // Executing HTTP parser.
        size_t bytes_parsed = http_parser_execute(
            (http_parser *)this,
            &g_httpParserSettings,
            (const char *)socketDataBuf->get_orig_buf_ptr(),
            socketDataBuf->get_accum_len_bytes());

        // Flag indicating if its a WebSockets upgrade.
        bool isWebSocket = http_parser_.upgrade;

        // Checking if we have complete data.
        if ((!complete_data_) && (bytes_parsed == socketDataBuf->get_accum_len_bytes()))
        {
            // Continue receiving.
            socketDataBuf->ContinueReceive();

            // Returning socket to receiving state.
            gw->Receive(sd);

            // Handled successfully.
            *is_handled = true;

            return 0;
        }

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

        // Pointing to the beginning of the response data.
        uint8_t *respDataBegin = socketDataBuf->ResponseDataStart();
        uint32_t respDataSize = 0;

        // Checking if we have upgrade.
        if (isWebSocket)
        {
            GW_COUT << "Upgrade to another protocol detected, data: " << std::endl;

            // Handled successfully.
            *is_handled = true;

            // Perform WebSockets handshake.
            return ws_proto_.DoHandshake(gw, sd);
        }
        // Handle error. Usually just close the connection.
        else if (bytes_parsed != (socketDataBuf->get_accum_len_bytes()))
        {
            GW_COUT << "HTTP packet has incorrect data!" << std::endl;
            return 1;
        }
        // Standard HTTP.
        else
        {
            // Checking if we have complete body.
            if (http_request_.body_len_bytes_ > (DATA_BLOB_SIZE_BYTES - bytes_parsed))
            {
                // We need to continue receiving up to certain accumulation point.

                

                GW_COUT << "HTTP packet has incorrect data!" << std::endl;
            }

            // Data is complete, posting parallel receive.
            gw->Receive(sd->CloneReceive(gw));

            // Checking special case when session is attached to socket,
            // but no session cookie is presented.
            if ((0 == http_request_.session_string_offset_) && (sd->GetAttachedSession() != NULL))
            {
                GW_COUT << "HTTP packet does not contain session cookie!" << std::endl;
                return 1;
            }

            if (resp_type_ == HTTP_STANDARD_RESPONSE)
            {
                // Checking if already visited before.
                if (sd->GetAttachedSession() == NULL)
                {
                    // Generating and attaching new session.
                    sd->AttachToSession(g_gateway.GenerateNewSession(gw), gw);
                }

                // Setting session structure fields.
                http_request_.request_offset_ = socketDataBuf->get_orig_buf_ptr() - (uint8_t*)sd;
                http_request_.session_struct_.Copy(sd->GetAttachedSession()->get_session_struct());
                http_request_.request_len_bytes_ = socketDataBuf->get_accum_len_bytes();

                // Getting the payload pointer.
                uint8_t *payload = sd->get_data_buf()->ResponseDataStart();
                uint32_t uri_len = http_request_.uri_len_bytes_;

                // Copying needed resource URI.
                memcpy(payload, &uri_len, 2);
                memcpy(payload + 2, (uint8_t*)sd + http_request_.uri_offset_, uri_len);

                // Setting user data length and pointer.
                sd->set_user_data_written_bytes(uri_len + 2);

                // TODO: Decide if lower 2 lines are needed.
                //sd->set_user_data_offset(payload - ((uint8_t *)sd));
                //sd->set_max_user_data_bytes(DATA_BLOB_SIZE_BYTES - (payload - sd->get_data_blob()));

                // Resetting user data parameters.
                sd->ResetUserDataOffset();
                sd->ResetMaxUserDataBytes();

                // Push chunk to corresponding channel/scheduler.
                gw->PushSocketDataToDb(sd, handler_id);
            }
            else
            {
                // Copying no-content response.
                memcpy(respDataBegin + respDataSize, kHttpNoContent, kHttpNoContentLen);

                // Prepare buffer to send outside.
                socketDataBuf->PrepareForSend(respDataBegin, kHttpNoContentLen);

                // Sending data.
                gw->Send(sd);
            }

            // Printing the outgoing packet.
#ifdef GW_HTTP_DIAG
            GW_COUT << respDataBegin << std::endl;
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
        if(sd->get_http_ws_proto()->get_web_sockets_upgrade() == true)
        {
            // Handled successfully.
            *is_handled = true;

            return ws_proto_.ProcessWsDataFromDb(gw, sd, handler_id);
        }

        // Getting user data.
        uint8_t *payload = sd->UserDataBuffer();

        // Length of user data in bytes.
        uint64_t payloadLen = sd->get_user_data_written_bytes();

        /*
        TODO: Remove if not needed in future.

        // Prefixing user data payload with HTTP header.
        int32_t httpHeaderLen = kHttpResponseLen[0] + 2;

        // Checking if we also need to embed the session cookie.
        if (0 == http_request_.session_string_offset_)
            httpHeaderLen += kHttpResponseLen[1];

        // Copying the base HTTP header.
        memcpy(payload - httpHeaderLen, kHttpResponse[0], kHttpResponseLen[0]);

        // Embedding data length.
        itoa(payloadLen, (char *)payload - httpHeaderLen + kHttpContentLengthOffset, 10);

        // Checking if we also need to embed the session cookie.
        if (0 == http_request_.session_string_offset_)
        {
            // Copying the session cookie header.
            memcpy(payload - httpHeaderLen + kHttpResponseLen[0], kHttpResponse[1], kHttpResponseLen[1]);

            // Embedding the session cookie.
            char temp[32];

            // Converting session to string.
            int32_t sessionStringLen = sd->GetAttachedSession()->ConvertToString(temp);

            // Copying the cookie string.
            memcpy(payload - httpHeaderLen + kHttpResponseLen[0] + kHttpCookieOffset, temp, sessionStringLen);
        }

        // Adding /r/n right before the user payload.
        payload[-2] = '\r';
        payload[-1] = '\n';

        // Prepare buffer to send outside.
        sd->get_data_buf()->PrepareForSend(payload - httpHeaderLen, payloadLen + httpHeaderLen);
        */

        // Prepare buffer to send outside.
        sd->get_data_buf()->PrepareForSend(payload, payloadLen);

        // Sending data.
        gw->Send(sd);

        // Handled successfully.
        *is_handled = true;

        return 0;
    }

    return 1;
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
