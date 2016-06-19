#pragma once
#ifndef HTTP_PROTO_HPP
#define HTTP_PROTO_HPP

namespace starcounter {
namespace network {

void HttpGlobalInit();
class WsProto;
class GatewayWorker;

class RegisteredUri
{
    // Indicates index of session parameter in this URI.
    uint8_t session_param_index_;

    // Is a gateway URI handler.
    bool is_gateway_uri_;

    // URI handler.
    HandlersList* handler_;

public:

    // Is gateway URI.
    bool get_is_gateway_uri()
    {
        return is_gateway_uri_;
    }

    // Indicates index of session parameter in this URI.
    uint8_t get_session_param_index()
    {
        return session_param_index_;
    }

    // Getting first handler entry.
    void WriteUserParameters(
        uint8_t* param_types,
        uint8_t* num_params)
    {
        GW_ASSERT(NULL != handler_);

        memcpy(param_types, handler_->get_param_types(), MixedCodeConstants::MAX_URI_CALLBACK_PARAMS);

        *num_params = handler_->get_num_params();
    }

    // Getting registered URI.
    char* get_method_space_uri()
    {
        GW_ASSERT(NULL != handler_);

        return handler_->get_method_space_uri();
    }

    // Getting application name.
    char* get_app_name()
    {
        GW_ASSERT(NULL != handler_);

        return handler_->get_app_name();
    }

    // Constructor.
    RegisteredUri()
    {
        handler_ = NULL;
        session_param_index_ = INVALID_PARAMETER_INDEX;
        is_gateway_uri_ = false;
    }

    // Checking if handlers list is empty.
    bool IsEmpty()
    {
        if (NULL == handler_)
            return true;

        return handler_->IsEmpty();
    }

    // Removes certain entry.
    bool ContainsDb(db_index_type db_index) {

        GW_ASSERT(NULL != handler_);

        return handler_->get_db_index() == db_index;
    }

    // Removes certain entry.
    db_index_type GetFirstDbIndex() {

        GW_ASSERT(NULL != handler_);

        return handler_->get_db_index();
    }

    // Removes certain entry.
    bool RemoveEntry(const db_index_type db_index)
    {
        GW_ASSERT(NULL != handler_);

        // Checking if database index is the same.
        if (db_index == handler_->get_db_index()) {

            // Deleting the entry.
            GwDeleteSingle(handler_);
            handler_ = NULL;

            return true;
        }

        return false;
    }

    // Initializing the entry.
    RegisteredUri(
        uint8_t session_param_index,
        db_index_type db_index,
        HandlersList* handlers_list,
        bool is_gateway_uri)
    {
        // Creating and pushing new handlers list.
        handler_ = handlers_list;

        session_param_index_ = session_param_index;

        is_gateway_uri_ = is_gateway_uri;
    }

    // Resetting entry.
    void Reset()
    {
        GW_ASSERT(NULL != handler_);

        GwDeleteSingle(handler_);
        handler_ = NULL;

        session_param_index_ = INVALID_PARAMETER_INDEX;

        is_gateway_uri_ = false;
    }

    // Running all registered handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd);
};

class RegisteredUris
{
    // Array of all registered URIs.
    LinearList<RegisteredUri, MixedCodeConstants::MAX_TOTAL_NUMBER_OF_HANDLERS> reg_uris_;

    // URI matcher entry.
    UriMatcherCacheEntry* uri_matcher_entry_;

    // Port to which this URI matcher belongs.
    uint16_t port_number_;
    
public:

    // Gets all URIs in a list (in the order as they are).
    std::string GetUriListString() {
        
        std::string s = "";

        // Going through all URIs in order.
        for (int32_t i = 0; i < reg_uris_.get_num_entries(); i++) {

            if (!reg_uris_[i].IsEmpty()) {
                s.append(reg_uris_[i].get_method_space_uri());
            }

            s += "\n";
        }

        return s;
    }

    int32_t get_num_uris() {
        return reg_uris_.get_num_entries();
    }

    uint16_t get_port_number()
    {
        return port_number_;
    }

    bool HasGeneratedUriMatcher()
    {
        return (NULL != uri_matcher_entry_);
    }

    void SetGeneratedUriMatcher(UriMatcherCacheEntry* uri_matcher_entry)
    {
        uri_matcher_entry_ = uri_matcher_entry;
    }

    // Getting array of RegisteredUriManaged.
    std::vector<MixedCodeConstants::RegisteredUriManaged> GetRegisteredUriManaged()
    {
        std::vector<MixedCodeConstants::RegisteredUriManaged> uris_vec;

        // Going through all URIs.
        for (int32_t i = 0; i < reg_uris_.get_num_entries(); i++)
        {
            if (!reg_uris_[i].IsEmpty())
            {
                MixedCodeConstants::RegisteredUriManaged reg_uri;

                reg_uri.method_space_uri = reg_uris_[i].get_method_space_uri();

                reg_uris_[i].WriteUserParameters(reg_uri.param_types, &reg_uri.num_params);

                reg_uri.handler_id = i;

                uris_vec.push_back(reg_uri);
            }
        }

        return uris_vec;
    }

    // Constructor.
    RegisteredUris(uint16_t port_number)
    {
        uri_matcher_entry_ = NULL;
        port_number_ = port_number;
    }

    // Destructor.
    ~RegisteredUris();

    // Invalidates code generation.
    void InvalidateUriMatcher()
    {
        uri_matcher_entry_ = NULL;
    }

    // Runs the generated URI matcher and gets handler information as a result.
    uri_index_type RunCodegenUriMatcher(char* method_space_uri_space, uint32_t method_space_uri_space_len, uint8_t* params_storage)
    {
        // Pointing to parameters storage.
        MixedCodeConstants::UserDelegateParamInfo** out_params = (MixedCodeConstants::UserDelegateParamInfo**)&params_storage;

        return uri_matcher_entry_->get_uri_matcher_func()(method_space_uri_space, method_space_uri_space_len, out_params);
    }

    // Printing the registered URIs.
    void PrintRegisteredUris(std::stringstream& stats_stream)
    {
        bool first = true;
        stats_stream << "\"registeredUris\":[";

        for (int32_t i = 0; i < reg_uris_.get_num_entries(); i++)
        {
            if (!first) 
                stats_stream << ",";
            first = false;

            stats_stream << "{";

            std::string method_space_uri = std::string(reg_uris_[i].get_method_space_uri());
            std::string method = method_space_uri.substr(0, method_space_uri.find(' '));
            std::string uri = method_space_uri.substr(method_space_uri.find(' ') + 1);

            stats_stream << "\"method\":\"" << method << "\",";
            stats_stream << "\"uri\":\"" << uri << "\",";
            stats_stream << "\"location\":";

            // Checking if its gateway or database URI.
            if (!reg_uris_[i].get_is_gateway_uri())
            {
                // Database handler.
                db_index_type db_index = reg_uris_[i].GetFirstDbIndex();
                if (INVALID_DB_INDEX == db_index) {
                    stats_stream << "\"gateway\"";
                } else {
                    stats_stream << '"' << g_gateway.GetDatabase(db_index)->get_db_name() << '"';
                }
                
                stats_stream << ",\"application\":\"" << reg_uris_[i].get_app_name() << "\""; 
            }
            else
            {
                // Gateway handler.
                stats_stream << "\"gateway\"";
                stats_stream << ",\"application\":" << "\"\"";
            }

            stats_stream << "}";
        }

        stats_stream << "]";
    }

    // Checking if entry already exists.
    bool ContainsEntry(char* uri)
    {
        int32_t index = FindRegisteredUri(uri);
        return (index >= 0);
    }

    // Checking if entry already exists.
    bool ContainsEntry(char* uri, db_index_type db_index)
    {
        int32_t index = FindRegisteredUri(uri);

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
    RegisteredUri* GetEntryByIndex(int32_t index)
    {
        return reg_uris_.GetElemPtr(index);
    }

    // Adding new entry.
    void AddNewUri(RegisteredUri& new_entry);

    // Checking if registered URIs is empty.
    bool IsEmpty()
    {
        return reg_uris_.IsEmpty();
    }

    // Removes certain URI.
    void RemoveUriByIndex(int32_t index)
    {
        // Removing entry.
        reg_uris_.RemoveByIndex(index);

        // Invalidating URI matcher.
        InvalidateUriMatcher();
    }

    // Removing certain entry.
    bool RemoveEntry(db_index_type db_index)
    {
        bool removed = false;

        // Going through all entries.
        for (int32_t i = 0; i < reg_uris_.get_num_entries(); ++i)
        {
            if (reg_uris_[i].RemoveEntry(db_index))
            {
                if (reg_uris_[i].IsEmpty())
                {
                    // Removing entry.
                    RemoveUriByIndex(i);
                    --i;
                }

                removed = true;

                // Not stopping, going through all entries.
            }
        }

        return removed;
    }

    // Removing certain entry.
    bool RemoveEntry(const char* method_space_uri)
    {
        int32_t index = FindRegisteredUri(method_space_uri);

        // Checking if entry found.
        if (index >= 0)
        {
            RemoveUriByIndex(index);
            return true;
        }

        return false;
    }

    // Removing certain entry.
    bool RemoveEntry(db_index_type db_index, char* method_space_uri)
    {
        bool removed = false;

        // Trying to find entry first.
        int32_t index = FindRegisteredUri(method_space_uri);

        // If entry was found.
        if (index >= 0)
        {
            if (reg_uris_[index].RemoveEntry(db_index))
            {
				if (reg_uris_[index].IsEmpty()) {
					RemoveUriByIndex(index);
				}
                
                removed = true;

                // Not stopping, going through all entries.
            }
        }

        return removed;
    }

    // Find certain URI entry.
    uri_index_type FindRegisteredUri(const char* method_space_uri);

    // Find certain URI entry.
    uri_index_type FindRegisteredUri(const char* method_space_uri, const int32_t method_space_uri_len)
    {
        // Going through all entries.
        for (uri_index_type i = 0; i < reg_uris_.get_num_entries(); i++) {

            // Doing exact comparison.
            if (0 == strncmp(method_space_uri, reg_uris_[i].get_method_space_uri(), method_space_uri_len)) {
                return i;
            }
        }

        // Returning negative if nothing is found.
        return INVALID_URI_INDEX;
    }
};

class HttpProto
{
    // Structure that holds HTTP request.
    HttpRequest http_request_;

public:

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

    // Processes the session information.
    uint32_t ProcessSessionString(SocketDataChunkRef sd, const char* session_id_start);

    // Resets the HTTP/WS structure.
    void Reset()
    {
        
    }

    // Initializes the HTTP/WS structure.
    void Init()
    {
        http_request_.Reset();
    }

    // Resets the parser related fields.
    void ResetParser(GatewayWorker *gw, SocketDataChunkRef sd);

    // Entry point for outer data processing.
    uint32_t HttpUriDispatcher(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id);

    // Standard HTTP/WS handler once URI is determined.
    uint32_t AppsHttpWsProcessData(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id);

    // Parses the HTTP request and pushes processed data to database.
    uint32_t GatewayHttpWsProcessEcho(
        HandlersList* hl,
        GatewayWorker *gw,
        SocketDataChunkRef sd,
        BMX_HANDLER_TYPE handler_id);

    // Reverse proxies the HTTP traffic.
    uint32_t GatewayHttpWsReverseProxy(
        HandlersList* hl,
        GatewayWorker *gw,
        SocketDataChunkRef sd,
        BMX_HANDLER_TYPE handler_id,
        int32_t reverse_proxy_index);
};

int32_t ConstructHttp400(
    char* const dest_buf,
    const int32_t dest_buf_max_bytes,
    const std::string& body,
    const int32_t err_code);

const char* const kHttp200Header =
    "HTTP/1.1 200 OK\r\n"
    "Content-Type: text/html\r\n"
    "Cache-control: no-store\r\n"
    "Content-Length: @@@@@@@@\r\n"
    "\r\n";

const int32_t kHttp200HeaderLength = static_cast<int32_t> (strlen(kHttp200Header));

const int32_t kHttp200HeaderInsertPoint = static_cast<int32_t> (strstr(kHttp200Header, "@") - kHttp200Header);

const char* const kHttp500Header =
    "HTTP/1.1 500 Internal Server Error\r\n"
    "Content-Type: text/html\r\n"
    "Cache-control: no-store\r\n"
    "Content-Length: @@@@@@@@\r\n"
    "\r\n";

const int32_t kHttp500HeaderLength = static_cast<int32_t> (strlen(kHttp500Header));

const int32_t kHttp500HeaderInsertPoint = static_cast<int32_t> (strstr(kHttp500Header, "@") - kHttp500Header);

const char* const kHttpStatisticsHeader =
    "HTTP/1.1 200 OK\r\n"
    "Content-Type: application/json\r\n"
    "Cache-control: no-store\r\n"
    "Content-Length: @@@@@@@@\r\n"
    "\r\n";

const int32_t kHttpStatisticsHeaderLength = static_cast<int32_t> (strlen(kHttpStatisticsHeader));

const int32_t kHttpStatisticsHeaderInsertPoint = static_cast<int32_t> (strstr(kHttpStatisticsHeader, "@") - kHttpStatisticsHeader);

const char* const kHttpEchoResponse =
    "HTTP/1.1 200 OK\r\n"
    "Content-Type: text/html\r\n"
    "Content-Length: 8\r\n"
    "\r\n"
    "@@@@@@@@";

const int32_t kHttpEchoResponseLength = static_cast<int32_t> (strlen(kHttpEchoResponse));

const int32_t kHttpEchoResponseInsertPoint = static_cast<int32_t> (strstr(kHttpEchoResponse, "@") - kHttpEchoResponse);

const int32_t kHttpEchoContentLength = 8;

} // namespace network
} // namespace starcounter

#endif // HTTP_PROTO_HPP
