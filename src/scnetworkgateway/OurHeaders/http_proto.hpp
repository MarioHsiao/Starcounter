#pragma once
#ifndef HTTP_PROTO_HPP
#define HTTP_PROTO_HPP

namespace starcounter {
namespace network {

// Type of HTTP/WebSockets response.
enum HttpWsResponseType
{
    HTTP_NO_CONTENT_RESPONSE,
    HTTP_STANDARD_RESPONSE,
    HTTP_GATEWAY_PONG_RESPONSE,
    WS_HANDSHAKE_RESPONSE,
    WS_BAD_REQUEST_RESPONSE
};

void HttpGlobalInit();
class WsProto;
class GatewayWorker;

class RegisteredUri
{
    // Registered URI.
    char uri_[bmx::MAX_URI_STRING_LEN];

    // Length of URI in characters.
    uint32_t uri_len_chars_;

    // Number of same characters from previous entry.
    uint32_t num_same_prev_chars_;

    // Unique handler lists.
    LinearList<UniqueHandlerList, bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST> handler_lists_;

public:

    // Getting number of handler lists.
    uint32_t GetHandlersListsNumber()
    {
        return handler_lists_.get_num_entries();
    }

    // Checking if one string starts after another.
    uint32_t StartsWith(
        const char* cur_uri,
        uint32_t cur_uri_chars,
        uint32_t skip_chars)
    {
        uint32_t same_chars = skip_chars;

        // Simply comparing strings by characters until they are the same.
        while(uri_[same_chars] == cur_uri[same_chars])
        {
            // Checking that we don't exceed string sizes.
            if ((same_chars >= uri_len_chars_) ||
                (same_chars >= cur_uri_chars))
            {
                break;
            }

            same_chars++;
        }

        // Returning number of matched characters plus skipped characters (assuming that they are the same).
        return same_chars;
    }

    // Comparison operator.
    bool operator<(const RegisteredUri& r)
    {
        // Slash is always on top.
        if ((uri_len_chars_ == 1) && (uri_[0] == '/'))
            return true;

        // Using normal comparison.
        bool comp_value = (strcmp(uri_, r.uri_) < 0);

        // But checking the length of strings.
            
        return comp_value;
    }

    // Getting registered URI.
    char* get_uri()
    {
        return uri_;
    }

    // Getting URI length in characters.
    uint32_t get_uri_len_chars()
    {
        return uri_len_chars_;
    }

    // Getting number of same characters.
    uint32_t get_num_same_prev_chars()
    {
        return num_same_prev_chars_;
    }

    // Setting number of same characters.
    void set_num_same_prev_chars(uint32_t value)
    {
        num_same_prev_chars_ = value;
    }

    // Constructor.
    RegisteredUri()
    {
        Reset();
    }

    // Checking if handlers list is empty.
    bool IsEmpty()
    {
        return (handler_lists_.IsEmpty()) || (0 == uri_len_chars_);
    }

    // Removes certain entry.
    bool Remove(HandlersList* handlers_list)
    {
        // Going through all handler list.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); i++)
        {
            // Checking if handlers list is the same.
            if (handler_lists_[i].get_handlers_list() == handlers_list)
            {
                handler_lists_.RemoveByIndex(i);
                break;
            }
        }

        // Checking if list is empty.
        if (IsEmpty())
            return true;

        return false;
    }

    // Removes certain entry.
    bool ContainsDb(int32_t db_index)
    {
        return (FindDb(db_index) >= 0);
    }

    // Removes certain entry.
    int32_t FindDb(int32_t db_index)
    {
        // Going through all handler list.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); i++)
        {
            if (handler_lists_[i].get_db_index() == db_index)
            {
                return i;
            }
        }

        return INVALID_DB_INDEX;
    }

    // Removes certain entry.
    bool Remove(int32_t db_index)
    {
        // Going through all handler list.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); i++)
        {
            // Checking if database index is the same.
            if (handler_lists_[i].get_db_index() == db_index)
            {
                handler_lists_.RemoveByIndex(i);
                break;
            }
        }

        // Checking if list is empty.
        if (IsEmpty())
            return true;
            
        return false;
    }

    // Adding new handlers list.
    void Add(UniqueHandlerList& new_handlers_list)
    {
        handler_lists_.Add(new_handlers_list);
    }

    // Initializing the entry.
    RegisteredUri(const char* uri, uint32_t uri_len_chars, int32_t db_index, HandlersList* handlers_list)
    {
        // Creating and pushing new handlers list.
        UniqueHandlerList new_entry(db_index, handlers_list);
        handler_lists_.Add(new_entry);

        // Copying the URI.
        strncpy_s(uri_, bmx::MAX_URI_STRING_LEN, uri, _TRUNCATE);
        uri_len_chars_ = uri_len_chars;

        num_same_prev_chars_ = 0;
    }

    // Resetting entry.
    void Reset()
    {
        // Removing all handlers lists.
        handler_lists_.Clear();

        uri_[0] = '\0';
        uri_len_chars_ = 0;

        num_same_prev_chars_ = 0;
    }

    // Running all registered handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunk *sd, bool* is_handled)
    {
        uint32_t err_code;

        // Going through all handler list.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); i++)
        {
            err_code = handler_lists_[i].get_handlers_list()->RunHandlers(gw, sd, is_handled);

            // Checking if information was handled and no errors occurred.
            if (*is_handled || err_code)
                return err_code;
        }

        return 0;
    }
};

class RegisteredUris
{
    // Array of all registered URIs.
    LinearList<RegisteredUri, bmx::MAX_TOTAL_NUMBER_OF_HANDLERS> reg_uris_;

public:

    // Constructor.
    RegisteredUris()
    {
    }

    // Sorting all entries.
    void Sort()
    {
        // Sorting the URIs.
        reg_uris_.Sort();

        // Setting zero same previous characters for the first entry.
        reg_uris_[0].set_num_same_prev_chars(0);

        // Going through sorted URIs and detecting same starts.
        for (int32_t i = 0; i < (reg_uris_.get_num_entries() - 1); i++)
        {
            // Checking if second URI starts with first.
            uint32_t same_chars = reg_uris_[i].StartsWith(reg_uris_[i + 1].get_uri(), reg_uris_[i + 1].get_uri_len_chars(), 0);

            // Setting number of same previous characters.
            reg_uris_[i + 1].set_num_same_prev_chars(same_chars);
        }
    }

    // Printing the registered URIs.
    void Print(uint16_t port)
    {
        GW_PRINT_GLOBAL << "Port " << port << " has following URIs registered: " << std::endl;
        for (int32_t i = 0; i < reg_uris_.get_num_entries(); i++)
        {
            GW_COUT << "    \"" << reg_uris_[i].get_uri() << "\" with handlers lists: " <<
                reg_uris_[i].GetHandlersListsNumber() << std::endl;
        }
    }

    // Checking if entry already exists.
    bool ContainsEntry(char* uri, uint32_t uri_len_chars)
    {
        int32_t index = FindRegisteredUri(uri, uri_len_chars);
        return (index >= 0);
    }

    // Checking if entry already exists.
    bool ContainsEntry(char* uri, uint32_t uri_len_chars, int32_t db_index)
    {
        int32_t index = FindRegisteredUri(uri, uri_len_chars);

        // Checking if entry found.
        if (index >= 0)
        {
            // Checking database index.
            if (reg_uris_[index].ContainsDb(db_index))
                return true;
        }

        return false;
    }

    // Getting entry by index.
    RegisteredUri& GetEntryByIndex(int32_t index)
    {
        return reg_uris_[index];
    }

    // Adding new entry.
    void AddEntry(RegisteredUri& new_entry)
    {
        // Adding new entry to the back.
        reg_uris_.Add(new_entry);

        // Sorting out all entries.
        Sort();
    }

    // Checking if registered URIs is empty.
    bool IsEmpty()
    {
        return reg_uris_.IsEmpty();
    }

    // Removing certain entry.
    bool RemoveEntry(HandlersList* handlers_list)
    {
        // Going through all entries.
        for (int32_t i = 0; i < reg_uris_.get_num_entries(); i++)
        {
            // Removing corresponding entry.
            if (reg_uris_[i].Remove(handlers_list))
            {
                // Removing entry.
                reg_uris_.RemoveByIndex(i);
                --i;
            }

            // Checking all entries.
        }

        // Checking if empty.
        if (reg_uris_.IsEmpty())
            return true;

        // Sorting all entries.
        Sort();

        return false;
    }

    // Removing certain entry.
    bool RemoveEntry(int32_t db_index)
    {
        // Going through all entries.
        for (int32_t i = 0; i < reg_uris_.get_num_entries(); ++i)
        {
            // Removing corresponding entry.
            if (reg_uris_[i].Remove(db_index))
            {
                // Removing entry.
                reg_uris_.RemoveByIndex(i);
                --i;
            }

            // Checking all entries.
        }

        // Checking if empty.
        if (reg_uris_.IsEmpty())
            return true;

        // Sorting all entries.
        Sort();

        return false;
    }

    // Removing certain entry.
    bool RemoveEntry(char* uri, uint32_t uri_len_chars)
    {
        int32_t index = FindRegisteredUri(uri, uri_len_chars);

        // Checking if entry found.
        if (index >= 0)
            reg_uris_.RemoveByIndex(index);

        // Checking if empty.
        if (reg_uris_.IsEmpty())
            return true;

        // Sorting all entries.
        Sort();

        return false;
    }

    // Adding new entry.
    bool RemoveEntry(char* uri, uint32_t uri_len_chars, int32_t db_index)
    {
        // Trying to find entry first.
        int32_t index = FindRegisteredUri(uri, uri_len_chars);

        // If entry was found.
        if (index >= 0)
        {
            // Removing corresponding database entry.
            if (reg_uris_[index].Remove(db_index))
            {
                // Removing entry.
                reg_uris_.RemoveByIndex(index);
            }
        }

        // Checking if empty.
        if (reg_uris_.IsEmpty())
            return true;

        return false;
    }

    // Find certain URI entry.
    int32_t FindRegisteredUri(const char* uri, uint32_t uri_len_chars)
    {
        // Going through all entries.
        for (int32_t i = 0; i < reg_uris_.get_num_entries(); i++)
        {
            // Doing exact comparison.
            if (!strcmp(uri, reg_uris_[i].get_uri()))
            {
                return i;
            }
        }

        // Returning negative if nothing is found.
        return INVALID_URI_INDEX;
    }

    // Find certain URI entry.
    int32_t SearchMatchingUriHandler(const char* uri, uint32_t uri_len_chars, int32_t& out_max_matched_chars)
    {
        // Going through all entries.
        int32_t same_chars = 0;
        out_max_matched_chars = 0;

        int32_t matched_index = INVALID_URI_INDEX;
        for (int32_t i = 0; i < reg_uris_.get_num_entries(); i++)
        {
            // Checking if this row is similar to previous more than same chars.
            if (reg_uris_[i].get_num_same_prev_chars() > same_chars)
                continue;

            // Checking URI starts with registered one.
            same_chars = reg_uris_[i].StartsWith(uri, uri_len_chars, reg_uris_[i].get_num_same_prev_chars());

            // Checking if there are any matched characters.
            if (same_chars > 0)
            {
                // Checking if we have a longer match.
                if ((same_chars >= reg_uris_[i].get_uri_len_chars()) && // Checking full registered URI match.
                    (same_chars >= out_max_matched_chars)) // Checking for longer string match.
                {
                    // Checking if we have correct URI start with.
                    if ((uri_len_chars > same_chars) &&
                        (reg_uris_[i].get_uri()[same_chars - 1] != '/'))
                    {
                        if (uri[same_chars] != '/')
                            continue;
                    }

                    // That was a correct match.
                    matched_index = i;
                    out_max_matched_chars = same_chars;
                }
                else if (same_chars < out_max_matched_chars)
                {
                    // By this search has finished.
                    break;
                }
            }
        }

        // Returning negative if nothing is found.
        return matched_index;
    }
};

class HttpWsProto
{
    // HttpProto is also an http_parser.
    http_parser http_parser_;

    // Structure that holds HTTP request.
    HttpRequest http_request_;

    // To which socket this instance belongs.
    SocketDataChunk *sd_ref_;

    // Index to already determined URI.
    uint32_t matched_uri_index_;

    // WebSocket related data.
    HttpWsFields last_field_;
    HttpWsResponseType resp_type_;
    WsProto ws_proto_;

public:

    // Getting matched URI index.
    uint32_t get_matched_uri_index()
    {
        return matched_uri_index_;
    }

    // Setting matching URI index.
    void set_matched_uri_index(uint32_t value)
    {
        matched_uri_index_ = value;
    }

    // Getting HTTP request.
    HttpRequest* get_http_request()
    {
        return &http_request_;
    }

    // HTTP/WS parser callbacks.
    static int OnMessageComplete(http_parser* p);
    static int OnMessageBegin(http_parser* p);
    static int OnBody(http_parser* p, const char *at, size_t length);
    static int OnHeadersComplete(http_parser* p);
    static int OnUri(http_parser* p, const char *at, size_t length);
    static int OnHeaderField(http_parser* p, const char *at, size_t length);
    static int OnHeaderValue(http_parser* p, const char *at, size_t length);

    // Initializes HTTP structure.
    void Init()
    {
        // Initializing WebSocket data.
        ws_proto_.Init();
        Reset();
        sd_ref_ = NULL;
    }

    // Resets the HTTP/WS structure.
    void Reset()
    {
        matched_uri_index_ = INVALID_URI_INDEX;
        ws_proto_.Reset();
    }

    // Resets the parser related fields.
    void ResetParser()
    {
        last_field_ = UNKNOWN_FIELD;
        resp_type_ = HTTP_STANDARD_RESPONSE;

#ifdef GW_PONG_MODE
        resp_type_ = HTTP_GATEWAY_PONG_RESPONSE;
#endif

        memset(&http_request_, 0, sizeof(http_request_));
        http_parser_init((http_parser *)this, HTTP_REQUEST);
    }

    // Entry point for outer data processing.
    uint32_t HttpUriDispatcher(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled);

    // Standard HTTP/WS handler once URI is determined.
    uint32_t AppsHttpWsProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled);

    // Parses the HTTP request and pushes processed data to database.
    uint32_t GatewayHttpWsProcessEcho(
        GatewayWorker *gw,
        SocketDataChunk *sd,
        BMX_HANDLER_TYPE handler_id,
        bool* is_handled);

#ifdef GW_PROXY_MODE

    // Reverse proxies the HTTP traffic.
    uint32_t GatewayHttpWsReverseProxy(
        GatewayWorker *gw,
        SocketDataChunk *sd,
        BMX_HANDLER_TYPE handler_id,
        bool* is_handled);
#endif

    // Attaching socket data and gateway worker to parser.
    void AttachToParser(SocketDataChunk *sd)
    {
        sd_ref_ = sd;
    }
};

const char* const kHttpEchoUrl = "/echo";
const int32_t kHttpEchoUrlLength = strlen(kHttpEchoUrl);

const char* const kHttpEchoRequest =
    "POST /echo HTTP/1.1\r\n"
    "Content-Type: text/html\r\n"
    "Content-Length: 8\r\n"
    "\r\n"
    "@@@@@@@@";

const int32_t kHttpEchoRequestLength = strlen(kHttpEchoRequest);

const int32_t kHttpEchoRequestInsertPoint = strstr(kHttpEchoRequest, "@") - kHttpEchoRequest;

const int32_t kHttpEchoBodyLength = 8;

const char* const kHttpEchoResponse =
    "HTTP/1.1 200 OK\r\n"
    "Content-Type: text/html\r\n"
    "Content-Length: 8\r\n"
    "\r\n"
    "@@@@@@@@";

const int32_t kHttpEchoResponseLength = strlen(kHttpEchoResponse);

const int32_t kHttpEchoResponseInsertPoint = strstr(kHttpEchoResponse, "@") - kHttpEchoResponse;

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

} // namespace network
} // namespace starcounter

#endif // HTTP_PROTO_HPP
