#pragma once
#ifndef HTTP_PROTO_HPP
#define HTTP_PROTO_HPP

namespace starcounter {
namespace network {

// HTTP/WebSockets fields.
enum HttpWsFields
{
    GET_FIELD,
    POST_FIELD,
    COOKIE_FIELD,
    CONTENT_LENGTH,
    ACCEPT_FIELD,
    ACCEPT_ENCODING_FIELD,
    COMPRESSION_FIELD,
    UPGRADE_FIELD,
    WS_KEY_FIELD,
    WS_VERSION_FIELD,
    WS_PROTOCOL_FIELD,
    UNKNOWN_FIELD
};

// Type of HTTP/WebSockets response.
enum HttpWsResponseType
{
    HTTP_NO_CONTENT_RESPONSE,
    HTTP_STANDARD_RESPONSE,
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
        char* cur_uri,
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

        return -1;
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
    RegisteredUri(char* uri, uint32_t uri_len_chars, int32_t db_index, HandlersList* handlers_list)
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
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunk *sd)
    {
        uint32_t err_code;

        // Going through all handler list.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); i++)
        {
            err_code = handler_lists_[i].get_handlers_list()->RunHandlers(gw, sd);

            // Checking if information was handled and no errors occurred.
            if (err_code)
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
    int32_t FindRegisteredUri(char* uri, uint32_t uri_len_chars)
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
        return -1;
    }

    // Find certain URI entry.
    int32_t SearchMatchingUriHandler(char* uri, uint32_t uri_len_chars, int32_t& out_max_matched_chars)
    {
        // Going through all entries.
        int32_t same_chars = 0;
        out_max_matched_chars = 0;

        int32_t matched_index = -1;
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

// Maximum number of pre-parsed HTTP headers.
const int32_t MAX_HTTP_HEADERS = 16;

struct HttpRequest
{
    // Request.
    uint32_t request_offset_;
    uint32_t request_len_bytes_;

    // Body.
    uint32_t body_offset_;
    uint32_t body_len_bytes_;

    // Resource URI.
    uint32_t uri_offset_;
    uint32_t uri_len_bytes_;

    // Key-value header.
    uint32_t headers_offset_;
    uint32_t headers_len_bytes_;

    // Cookie value.
    uint32_t cookies_offset_;
    uint32_t cookies_len_bytes_;

    // Accept value.
    uint32_t accept_value_offset_;
    uint32_t accept_value_len_bytes_;

    // Session ID.
    uint32_t session_string_offset_;
    uint32_t session_string_len_bytes_;

    // Header offsets.
    uint32_t header_offsets_[MAX_HTTP_HEADERS];
    uint32_t header_len_bytes_[MAX_HTTP_HEADERS];
    uint32_t header_value_offsets_[MAX_HTTP_HEADERS];
    uint32_t header_value_len_bytes_[MAX_HTTP_HEADERS];
    uint32_t num_headers_;

    // HTTP method.
    bmx::HTTP_METHODS http_method_;

    // Is Gzip accepted.
    bool gzip_accepted_;
};

class HttpWsProto
{
    // HttpProto is also an http_parser.
    http_parser http_parser_;

    // Structure that holds HTTP request.
    HttpRequest http_request_;

    // WebSockets upgrade flag.
    bool web_sockets_upgrade_;

    // To which socket this instance belongs.
    SocketDataChunk *sd_ref_;

    // Gateway worker reference.
    GatewayWorker *gw_ref_temp_;

    // WebSocket related data.
    HttpWsFields last_field_;
    HttpWsResponseType resp_type_;
    WsProto ws_proto_;
    bool complete_data_;

    // Indicates if URI was parsed already.
    bool uri_parsed_;

public:

    // Getting WebSocket upgrade flag.
    bool get_web_sockets_upgrade()
    {
        return web_sockets_upgrade_;
    }

    // Setting WebSocket upgrade flag.
    void set_web_sockets_upgrade(bool value)
    {
        web_sockets_upgrade_ = value;
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
        sd_ref_ = NULL;
        gw_ref_temp_ = NULL;
        web_sockets_upgrade_ = false;
        ResetParser();
    }

    // Resets the HTTP/WS structure.
    void Reset()
    {
        web_sockets_upgrade_ = false;
        ws_proto_.Reset();
    }

    // Resets the parser related fields.
    void ResetParser()
    {
        last_field_ = UNKNOWN_FIELD;
        resp_type_ = HTTP_STANDARD_RESPONSE;
        complete_data_ = false;

        memset(&http_request_, 0, sizeof(http_request_));
        uri_parsed_ = false;

        http_parser_init((http_parser *)this, HTTP_REQUEST);
    }

    // Entry point for outer data processing.
    uint32_t HttpUriDispatcher(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled);

    // Standard HTTP/WS handler once URI is determined.
    uint32_t HttpWsProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled);

    // Attaching socket data and gateway worker to parser.
    void AttachToParser(GatewayWorker *gw, SocketDataChunk *sd)
    {
        gw_ref_temp_ = gw;
        sd_ref_ = sd;
    }
};

} // namespace network
} // namespace starcounter

#endif // HTTP_PROTO_HPP
