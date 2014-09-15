#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
#include "worker_db_interface.hpp"
#include "worker.hpp"
#include "urimatch_codegen.hpp"

#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "winmm.lib")

namespace starcounter {

utils::Profiler* utils::Profiler::schedulers_profilers_ = NULL;

namespace network {

// Main network gateway object.
Gateway g_gateway;

// Pointers to extended WinSock functions.
LPFN_ACCEPTEX AcceptExFunc = NULL;
LPFN_CONNECTEX ConnectExFunc = NULL;
LPFN_DISCONNECTEX DisconnectExFunc = NULL;

GUID AcceptExGuid = WSAID_ACCEPTEX,
    ConnectExGuid = WSAID_CONNECTEX,
    DisconnectExGuid = WSAID_DISCONNECTEX;

std::string GetOperTypeString(SocketOperType typeOfOper)
{
    switch (typeOfOper)
    {
        case SEND_SOCKET_OPER: return "SEND_SOCKET_OPER";
        case RECEIVE_SOCKET_OPER: return "RECEIVE_SOCKET_OPER";
        case ACCEPT_SOCKET_OPER: return "ACCEPT_SOCKET_OPER";
        case CONNECT_SOCKET_OPER: return "CONNECT_SOCKET_OPER";
        case DISCONNECT_SOCKET_OPER: return "DISCONNECT_SOCKET_OPER";
        case UNKNOWN_SOCKET_OPER: return "UNKNOWN_SOCKET_OPER";
    }
    return "ERROR_SOCKET_OPER";
}

// Writing to log once object is destroyed.
ServerLoggingSafe::~ServerLoggingSafe()
{
    switch (t_)
    {
        case GW_LOGGING_ERROR_TYPE:
            g_gateway.LogWriteError(ss_.str().c_str());
            break;

        case GW_LOGGING_WARNING_TYPE:
            g_gateway.LogWriteWarning(ss_.str().c_str());
            break;

        case GW_LOGGING_NOTICE_TYPE:
            g_gateway.LogWriteNotice(ss_.str().c_str());
            break;

        case GW_LOGGING_CRITICAL_TYPE:
            g_gateway.LogWriteCritical(ss_.str().c_str());
            break;
    }
    
}

// Writing to log once object is destroyed.
CoutSafe::~CoutSafe()
{
#ifdef GW_LOGGING_ON

    std::string str = ss_.str();
    g_gateway.get_gw_log_writer()->WriteToLog(str.c_str(), static_cast<int32_t>(str.length()));

#endif
}

void GatewayLogWriter::Init(const std::wstring& log_file_path)
{
    log_write_pos_ = 0;

    log_read_pos_ = 0;

    accum_length_ = 0;

    InitializeCriticalSection(&write_lock_);

#ifdef GW_LOG_TO_FILE

    // Opening log file for writes.
    log_file_handle_ = CreateFile(
        (log_file_path).c_str(),
        GENERIC_WRITE,
        FILE_SHARE_READ,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    GW_ASSERT(INVALID_HANDLE_VALUE != log_file_handle_);

    // Seeking to the end of file.
    DWORD file_ptr = SetFilePointer(
        log_file_handle_,
        NULL,
        NULL,
        FILE_END
        );

    GW_ASSERT(INVALID_SET_FILE_POINTER != file_ptr);

#endif   
}

// Writes given string to log buffer.
#ifdef GW_LOGGING_ON
void GatewayLogWriter::WriteToLog(const char* text, int32_t text_len)
{
    EnterCriticalSection(&write_lock_);

#ifdef GW_LOG_TO_FILE

    int32_t left_space = GW_LOG_BUFFER_SIZE - log_write_pos_;

    // Checking if text fits in the buffer.
    if (text_len <= left_space)
    {
        memcpy(log_buf_ + log_write_pos_, text, text_len);
        log_write_pos_ += text_len;
    }
    // Checking if we should write to the beginning.
    else if (left_space == 0)
    {
        memcpy(log_buf_, text, text_len);
        log_write_pos_ = text_len;
    }
    // Splitted write.
    else
    {
        memcpy(log_buf_ + log_write_pos_, text, left_space);
        memcpy(log_buf_, text + left_space, text_len - left_space);
        log_write_pos_ = text_len - left_space;
    }

    // Adjusting accumulated log size.
    accum_length_ += text_len;
    GW_ASSERT(accum_length_ < GW_LOG_BUFFER_SIZE);

#endif

    // Printing everything to console as well.
#ifdef GW_LOG_TO_CONSOLE
    std::cout << text;
#endif

    LeaveCriticalSection(&write_lock_);
}

// Dump accumulated logs in buffer to file.
void GatewayLogWriter::DumpToLogFile()
{
    if (log_write_pos_ == log_read_pos_)
        return;

    // Saving current write position of log.
    int32_t log_write_pos = log_write_pos_;
    int32_t num_bytes_to_write;
    BOOL err_code;

    // Simplest case when writes are linear in log buffer.
    if (log_write_pos > log_read_pos_)
    {
        num_bytes_to_write = log_write_pos - log_read_pos_;

        err_code = WriteFile(
            log_file_handle_,
            log_buf_ + log_read_pos_,
            num_bytes_to_write,
            NULL,
            NULL
            );

        GW_ASSERT(TRUE == err_code);
    }
    else
    {
        num_bytes_to_write = GW_LOG_BUFFER_SIZE - log_read_pos_;

        if (num_bytes_to_write)
        {
            // Write log_read_pos_ to LOG_BUFFER_SIZE.
            err_code = WriteFile(
                log_file_handle_,
                log_buf_ + log_read_pos_,
                num_bytes_to_write,
                NULL,
                NULL
                );

            GW_ASSERT(TRUE == err_code);
        }

        num_bytes_to_write = log_write_pos;

        if (num_bytes_to_write)
        {
            // Write 0 to log_write_pos.
            err_code = WriteFile(
                log_file_handle_,
                log_buf_,
                num_bytes_to_write,
                NULL,
                NULL
                );

            GW_ASSERT(TRUE == err_code);
        }

        num_bytes_to_write =  GW_LOG_BUFFER_SIZE - log_read_pos_ + log_write_pos;
    }

    // Shifting log read position.
    log_read_pos_ = log_write_pos;

    // Adjusting accumulated log size.
    accum_length_ -= num_bytes_to_write;
    GW_ASSERT(accum_length_ >= 0);
}
#endif

Gateway::Gateway()
{
    // Number of worker threads.
    setting_num_workers_ = 0;

    // Maximum total number of sockets.
    setting_max_connections_per_worker_ = 0;

    // Default inactive socket timeout in seconds.
    setting_inactive_socket_timeout_seconds_ = 60 * 20;

    // Starcounter server type.
    setting_sc_server_type_upper_ = MixedCodeConstants::DefaultPersonalServerNameUpper;

    // All worker structures.
    gw_workers_ = NULL;

    // Worker thread handles.
    worker_thread_handles_ = NULL;

    // Number of active databases.
    num_dbs_slots_ = 0;

    // Init unique sequence number.
    db_seq_num_ = 0;

    // Reset gateway owner id and pid.
    gateway_owner_id_ = 0;
    gateway_pid_ = 0;

    // No reverse proxies by default.
    num_reversed_proxies_ = 0;

    // Starting linear unique socket with 0.
    unique_socket_id_ = 0;

    // Server address for testing.
    server_addr_ = NULL;

    // Initializing critical sections.
    InitializeCriticalSection(&cs_global_lock_);
    InitializeCriticalSection(&cs_statistics_);

    num_pending_sends_ = 0;
    num_aggregated_sent_messages_ = 0;
    num_aggregated_recv_messages_ = 0;
    num_aggregated_send_queued_messages_ = 0;

    // Initial number of server ports.
    num_server_ports_slots_ = 0;

    // Resetting number of processed HTTP requests.
    num_processed_http_requests_unsafe_ = 0;

    // Initializing to first bind port number.
    last_bind_port_num_unsafe_ = FIRST_BIND_PORT_NUM;

    // First bind interface number.
    last_bind_interface_num_unsafe_ = 0;

    // Resetting Starcounter log handle.
    sc_log_handle_ = MixedCodeConstants::INVALID_SERVER_LOG_HANDLE;

    // Empty global statistics.
    memcpy(global_statistics_string_, kHttpStatisticsHeader, kHttpStatisticsHeaderLength);

    codegen_uri_matcher_ = NULL;
}

// Reading command line arguments.
// 1: Server type.
// 2: Gateway configuration file path.
// 3: Starcounter server output directory path.
uint32_t Gateway::ProcessArgumentsAndInitLog(int argc, wchar_t* argv[])
{
    // Checking correct number of arguments.
    if (argc < 4)
    {
        std::wcout << GW_PROGRAM_NAME << L".exe [ServerTypeName] [PathToGatewayXmlConfig] [PathToOutputDirectory]" << std::endl;
        std::wcout << L"Example: " << GW_PROGRAM_NAME << L".exe personal \"c:\\github\\NetworkGateway\\src\\scripts\\server.xml\" \"c:\\github\\Level1\\bin\\Debug\\.db.output\"" << std::endl;

        GW_ASSERT(false);
    }

    // Reading the Starcounter log directory.
    setting_server_output_dir_ = argv[3];

    char temp[128];
    
    // Opening Starcounter log.
    uint32_t err_code = g_gateway.OpenStarcounterLog();
    if (err_code)
        return err_code;

    // Converting Starcounter server type to narrow char.
    size_t conv_chars;
    wcstombs_s(&conv_chars, temp, 128, argv[1], _TRUNCATE);

    // Copying other fields.
    setting_sc_server_type_upper_ = temp;

    // Converting to upper case.
    std::transform(
        setting_sc_server_type_upper_.begin(),
        setting_sc_server_type_upper_.end(),
        setting_sc_server_type_upper_.begin(),
        ::toupper);

    setting_config_file_path_ = argv[2];

    // Getting executable directory.
    wchar_t exe_dir[1024];
    DWORD r = GetModuleFileName(NULL, exe_dir, 1024);
    GW_ASSERT((r > 0) && (r < 1024));

    // Getting directory name from executable path.
    int32_t c = r;
    while (c > 0)
    {
        c--;

        if (exe_dir[c] == L'\\')
            break;
    }
    exe_dir[c] = L'\0';

    // Creating Starcounter bin directory property.
    setting_sc_bin_dir_ = std::wstring(exe_dir);

    // Specifying gateway output directory as sub-folder for server output.
    setting_gateway_output_dir_ = setting_server_output_dir_ + L"\\" + GW_PROGRAM_NAME;

    // Trying to create network gateway log directory.
    if ((!CreateDirectory(setting_gateway_output_dir_.c_str(), NULL)) &&
        (ERROR_ALREADY_EXISTS != GetLastError()))
    {
        std::wcout << L"Can't create network gateway log directory: " << setting_gateway_output_dir_ << std::endl;

        GW_ASSERT(false);
    }

    // Obtaining full path to log directory.
    wchar_t log_file_dir_full[1024];
    DWORD num_copied_chars = GetFullPathName(setting_gateway_output_dir_.c_str(), 1024, log_file_dir_full, NULL);
    GW_ASSERT(num_copied_chars != 0);

    // Full path to gateway log file.
    setting_log_file_path_ = std::wstring(log_file_dir_full) + L"\\" + GW_PROGRAM_NAME + L".log";

    // Deleting old log file first.
    DeleteFile(setting_log_file_path_.c_str());

    return 0;
}

// Initializes server socket.
void ServerPort::Init(port_index_type port_index, uint16_t port_number, SOCKET listening_sock)
{
    GW_ASSERT((port_index >= 0) && (port_index < MAX_PORTS_NUM));

    // Allocating needed tables.
    port_handlers_ = GwNewConstructor(PortHandlers);
    registered_uris_ = GwNewConstructor1(RegisteredUris, port_number);
    registered_ws_channels_ = GwNewConstructor1(PortWsChannels, port_number);

    listening_sock_ = listening_sock;
    port_number_ = port_number;
    port_handlers_->set_port_number(port_number_);
    port_index_ = port_index;

    memset(num_active_sockets_, 0, sizeof(num_active_sockets_));
}

// Resets the number of created sockets and active connections.
void ServerPort::Reset()
{
    InterlockedAnd64(&(num_accepting_sockets_unsafe_), 0);
}

// Removes this port.
void ServerPort::EraseDb(db_index_type db_index)
{
    // Deleting port handlers if any.
    port_handlers_->RemoveEntry(db_index);

    // Deleting URI handlers if any.
    registered_uris_->RemoveEntry(db_index);
    
    // Deleting WebSocket channels if any.
    registered_ws_channels_->RemoveEntry(db_index);
}

// Checking if port is unused by any database.
bool ServerPort::IsEmpty()
{
    // Checks port index first.
    if (INVALID_PORT_INDEX == port_index_)
        return true;

    // Checking port handlers.
    if (port_handlers_ && (!port_handlers_->IsEmpty()))
        return false;

    // Checking URIs.
    if (registered_uris_ && (!registered_uris_->IsEmpty()))
        return false;

    // Checking WebSocket channels.
    if (registered_ws_channels_ && (!registered_ws_channels_->IsEmpty()))
        return false;

    // Checking connections.
    if (NumberOfActiveConnections())
        return false;

    return true;
}

// Retrieves the number of active connections.
int64_t ServerPort::NumberOfActiveConnections()
{
    int64_t num_active_conns = 0;

    for (int32_t w = 0; w < g_gateway.setting_num_workers(); w++)
        num_active_conns += num_active_sockets_[w];

    return num_active_conns;
}

// Removes this port.
void ServerPort::Erase()
{
    // Closing socket which will results in Disconnect.
    if (INVALID_SOCKET != listening_sock_)
    {
        if (closesocket(listening_sock_))
        {
#ifdef GW_WARNINGS_DIAG
            GW_COUT << "closesocket() failed." << GW_ENDL;
            PrintLastError();
#endif
        }
        listening_sock_ = INVALID_SOCKET;
    }

    if (port_handlers_)
    {
        GwDeleteSingle(port_handlers_);
        port_handlers_ = NULL;
    }

    if (registered_uris_)
    {
        GwDeleteSingle(registered_uris_);
        registered_uris_ = NULL;
    }

    if (registered_ws_channels_)
    {
        GwDeleteSingle(registered_ws_channels_);
        registered_ws_channels_ = NULL;
    }

    port_number_ = INVALID_PORT_NUMBER;
    port_index_ = INVALID_PORT_INDEX;

    Reset();
}

worker_id_type ServerPort::GetLeastBusyWorkerId() {

    int32_t cur_least_active_sockets = num_active_sockets_[0];
    worker_id_type wi = 0;

    for (int32_t i = 1; i < g_gateway.setting_num_workers(); i++) {
        if (cur_least_active_sockets > num_active_sockets_[i]) {
            cur_least_active_sockets = num_active_sockets_[i];
            wi = i;
        }
    }

    GW_ASSERT(cur_least_active_sockets >= 0);

    return wi;
}

void UriMatcherCacheEntry::Destroy() {

    if (NULL != gen_dll_handle_) {
        BOOL success = FreeLibrary(gen_dll_handle_);
        GW_ASSERT(TRUE == success);
    }

    if (NULL != clang_engine_) {
        g_gateway.ClangDestroyEngineFunc(clang_engine_);
        clang_engine_ = NULL;
    }
    
    num_uris_ = 0;
    gen_dll_handle_ = NULL;
    gen_uri_matcher_func_ = NULL;
}

UriMatcherCacheEntry* ServerPort::TryGetUriMatcherFromCache() {

    std::string uris_list = registered_uris_->GetSortedString();

    // Going through each cache entry (note that we are going from back to front).
    for (std::list<UriMatcherCacheEntry*>::reverse_iterator it = uri_matcher_cache_.rbegin(); it != uri_matcher_cache_.rend(); it++) {

        // Comparing first the number of URIs.
        if ((*it)->get_num_uris() == registered_uris_->get_num_uris()) {

            // Comparing the sorted URIs list .
            if (uris_list == (*it)->get_sorted_uris_string()) {

                // List is the same, meaning that generated code is the same.
                return (*it);
            }
        }
    }

    return NULL;
}

int32_t ServerPort::GetNumberOfActiveSocketsForWorker(worker_id_type worker_id) {

    return num_active_sockets_[worker_id];
}

int32_t ServerPort::GetNumberOfActiveSocketsAllWorkers() {

    int32_t num_active_sockets = num_active_sockets_[0];

    for (int32_t i = 1; i < g_gateway.setting_num_workers(); i++) {
        num_active_sockets += num_active_sockets_[i];
    }

    return num_active_sockets;
}

// Printing the registered URIs.
void ServerPort::PrintInfo(std::stringstream& stats_stream)
{
    stats_stream << "{\"port\":" << get_port_number() << ",";
    stats_stream << "\"acceptingSockets\":" << get_num_accepting_sockets() << ",";

    stats_stream << "\"activeConnections\":" << NumberOfActiveConnections() << ",";

    stats_stream << "\"activeSockets\":\"";

    stats_stream << num_active_sockets_[0];
    for (int32_t i = 1; i < g_gateway.setting_num_workers(); i++) {
        stats_stream << "-" << num_active_sockets_[i];
    }

    stats_stream << "\",";

    //port_handlers_->PrintRegisteredHandlers(global_port_statistics_stream);
    registered_uris_->PrintRegisteredUris(stats_stream);
    registered_ws_channels_->PrintRegisteredChannels(stats_stream);
    stats_stream << "}";
}

// Printing the database information.
void ActiveDatabase::PrintInfo(std::stringstream& stats_stream)
{
    stats_stream << "{\"name\":\"" << db_name_ << "\",";
    stats_stream << "\"index\":" << static_cast<int32_t>(db_index_) << "}";
}

ServerPort::ServerPort()
{
    listening_sock_ = INVALID_SOCKET;
    port_handlers_ = NULL;
    registered_uris_ = NULL;
    registered_ws_channels_ = NULL;

    Erase();
}

ServerPort::~ServerPort()
{
}

// Loads configuration settings from provided XML file.
uint32_t Gateway::LoadSettings(std::wstring configFilePath)
{
    // Opening file stream.
    std::ifstream config_file_stream;
    config_file_stream.open(configFilePath);
    if (!config_file_stream.is_open())
    {
        config_file_stream.open(GW_DEFAULT_CONFIG_NAME);
        if (!config_file_stream.is_open())
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Settings file stream can't be opened.");
            return SCERRBADGATEWAYCONFIG;
        }
    }

    // Copying config contents into a string.
    std::stringstream str_stream;
    str_stream << config_file_stream.rdbuf();
    std::string tmp_str = str_stream.str();
    char* config_contents = GwNewArray(char, tmp_str.size() + 1);
    strcpy_s(config_contents, tmp_str.size() + 1, tmp_str.c_str());

    try
    {
        using namespace rapidxml;
        xml_document<> doc; // Character type defaults to char.
        doc.parse<0>(config_contents); // 0 means default parse flags.

        xml_node<> *root_elem = doc.first_node("NetworkGateway");
        if (!root_elem)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Can't read NetworkGateway property.");
            return SCERRBADGATEWAYCONFIG;
        }

        // Getting local interfaces.
        xml_node<>* node_elem = root_elem->first_node("BindingIP");
        while(node_elem)
        {
            setting_local_interfaces_.push_back(node_elem->value());
            node_elem = node_elem->next_sibling("BindingIP");
        }

        // Getting workers number.
        node_elem = root_elem->first_node("WorkersNumber");
        if (!node_elem)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Can't read WorkersNumber property.");
            return SCERRBADGATEWAYCONFIG;
        }

        setting_num_workers_ = atoi(node_elem->value());
        if (setting_num_workers_ <= 0 || setting_num_workers_ > 16)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Unsupported WorkersNumber value.");
            return SCERRBADGATEWAYCONFIG;
        }
        
        // Getting maximum connection number.
        node_elem = root_elem->first_node("MaxConnectionsPerWorker");
        if (!node_elem)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Can't read MaxConnectionsPerWorker property.");
            return SCERRBADGATEWAYCONFIG;
        }

        setting_max_connections_per_worker_ = atoi(node_elem->value());
        if (setting_max_connections_per_worker_ < 10 || setting_max_connections_per_worker_ > 1000000)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Unsupported MaxConnectionsPerWorker value.");
            return SCERRBADGATEWAYCONFIG;
        }

        // Getting maximum connection number.
        node_elem = root_elem->first_node("MaximumReceiveContentLength");
        if (!node_elem)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Can't read MaximumReceiveContentLength property.");
            return SCERRBADGATEWAYCONFIG;
        }

        setting_maximum_receive_content_length_ = atoi(node_elem->value());
        if (setting_maximum_receive_content_length_ < 4096 || setting_maximum_receive_content_length_ > 67108864)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Unsupported MaximumReceiveContentLength value.");
            return SCERRBADGATEWAYCONFIG;
        }

        // Getting inactive socket timeout.
        node_elem = root_elem->first_node("InactiveConnectionTimeout");
        if (!node_elem)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Can't read InactiveConnectionTimeout property.");
            return SCERRBADGATEWAYCONFIG;
        }

        setting_inactive_socket_timeout_seconds_ = atoi(node_elem->value());
        if (setting_inactive_socket_timeout_seconds_ <= 0 || setting_inactive_socket_timeout_seconds_ > 100000)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Unsupported InactiveConnectionTimeout value.");
            return SCERRBADGATEWAYCONFIG;
        }

        node_elem = root_elem->first_node("InternalSystemPort");
        if (!node_elem)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Can't read InternalSystemPort property.");
            return SCERRBADGATEWAYCONFIG;
        }

        setting_internal_system_port_ = (uint16_t)atoi(node_elem->value());
        if (setting_internal_system_port_ <= 0 || setting_internal_system_port_ >= 65536)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Unsupported InternalSystemPort value.");
            return SCERRBADGATEWAYCONFIG;
        }

        // Getting aggregation port number.
        node_elem = root_elem->first_node("AggregationPort");
        if (node_elem)
        {
            setting_aggregation_port_ = (uint16_t)atoi(node_elem->value());
            if (setting_aggregation_port_ <= 0 || setting_aggregation_port_ >= 65536)
            {
                g_gateway.LogWriteCritical(L"Gateway XML: Unsupported AggregationPort value.");
                return SCERRBADGATEWAYCONFIG;
            }
        }

        // Just enforcing minimum socket timeout multiplier.
        if ((setting_inactive_socket_timeout_seconds_ % SOCKET_LIFETIME_MULTIPLIER) != 0)
        {
            g_gateway.LogWriteCritical(L"Gateway XML: Inactive socket timeout is not dividable by 3.");
            return SCERRBADGATEWAYCONFIG;
        }

        // Setting minimum socket life time.
        min_inactive_socket_life_seconds_ = setting_inactive_socket_timeout_seconds_ / SOCKET_LIFETIME_MULTIPLIER;

        // Initializing global timer.
        global_timer_unsafe_ = min_inactive_socket_life_seconds_;

        // Checking if we have reverse proxies.
        xml_node<char>* proxies_node = root_elem->first_node("ReverseProxies");
        if (proxies_node)
        {
            xml_node<char>* proxy_node = proxies_node->first_node("ReverseProxy");
            if (!node_elem)
            {
                g_gateway.LogWriteCritical(L"Gateway XML: Can't read ReverseProxy property.");
                return SCERRBADGATEWAYCONFIG;
            }

            int32_t n = 0;
            while (proxy_node)
            {
                // Filling reverse proxy information.
                node_elem = proxy_node->first_node("DestinationDNS");
                if (!node_elem)
                {
                    node_elem = proxy_node->first_node("DestinationIP");
                    if (!node_elem)
                    {
                        g_gateway.LogWriteCritical(L"Gateway XML: Can't read DestinationIP property. Either DestinationDNS or DestinationIP property should be specified.");
                        return SCERRBADGATEWAYCONFIG;
                    }
                    reverse_proxies_[n].destination_ip_ = node_elem->value();
                }
                else
                {
                    addrinfo *dns_addr_info = NULL;

                    // Obtaining server IP from DNS name.
                    uint32_t err_code = GetAddrInfoA(node_elem->value(), NULL, NULL, &dns_addr_info);
                    if (err_code)
                    {
                        std::wstring temp = L"Reverse proxy: Can't obtain IP address from DNS name: ";
                        std::wstring ws_temp;
                        std::string s_temp = node_elem->value();
                        ws_temp.assign(s_temp.begin(), s_temp.end());
                        temp += ws_temp;

                        g_gateway.LogWriteCritical(temp.c_str());
                        return SCERRBADGATEWAYCONFIG;
                    }

                    // Checking if its IPv4 address.
                    if (dns_addr_info->ai_family != AF_INET)
                    {
                        std::wstring temp = L"Reverse proxy: Only resolved IPv4 addresses are supported at the moment: ";
                        std::wstring ws_temp;
                        std::string s_temp = node_elem->value();
                        ws_temp.assign(s_temp.begin(), s_temp.end());
                        temp += ws_temp;

                        g_gateway.LogWriteCritical(temp.c_str());
                        return SCERRBADGATEWAYCONFIG;
                    }

                    // Getting the first IP address.
                    reverse_proxies_[n].destination_ip_ = inet_ntoa(((struct sockaddr_in *) dns_addr_info->ai_addr)->sin_addr);
                }

                node_elem = proxy_node->first_node("DestinationPort");
                if (!node_elem)
                {
                    g_gateway.LogWriteCritical(L"Gateway XML: Can't read DestinationPort property.");
                    return SCERRBADGATEWAYCONFIG;
                }

                reverse_proxies_[n].destination_port_ = atoi(node_elem->value());
                if (reverse_proxies_[n].destination_port_ <= 0 || reverse_proxies_[n].destination_port_  >= 65536)
                {
                    g_gateway.LogWriteCritical(L"Gateway XML: Reverse proxy has incorrect DestinationPort number.");
                    return SCERRBADGATEWAYCONFIG;
                }

                node_elem = proxy_node->first_node("StarcounterProxyPort");
                if (!node_elem)
                {
                    g_gateway.LogWriteCritical(L"Gateway XML: Can't read StarcounterProxyPort property.");
                    return SCERRBADGATEWAYCONFIG;
                }

                reverse_proxies_[n].sc_proxy_port_ = atoi(node_elem->value());
                if (reverse_proxies_[n].sc_proxy_port_ <= 0 || reverse_proxies_[n].sc_proxy_port_  >= 65536)
                {
                    g_gateway.LogWriteCritical(L"Gateway XML: Reverse proxy has incorrect StarcounterProxyPort number.");
                    return SCERRBADGATEWAYCONFIG;
                }

                node_elem = proxy_node->first_node("MatchingMethodAndUri");
                reverse_proxies_[n].matching_method_and_uri_ = node_elem->value();
                reverse_proxies_[n].matching_method_and_uri_processed_ = reverse_proxies_[n].matching_method_and_uri_ + " ";

                reverse_proxies_[n].matching_method_and_uri_len_ = static_cast<int32_t> (reverse_proxies_[n].matching_method_and_uri_.length());
                reverse_proxies_[n].matching_method_and_uri_processed_len_ = reverse_proxies_[n].matching_method_and_uri_len_ + 1;

                // Loading proxied servers.
                sockaddr_in* server_addr = &reverse_proxies_[n].destination_addr_;
                memset(server_addr, 0, sizeof(sockaddr_in));
                server_addr->sin_family = AF_INET;
                server_addr->sin_addr.s_addr = inet_addr(reverse_proxies_[n].destination_ip_.c_str());
                server_addr->sin_port = htons(reverse_proxies_[n].destination_port_);

                // Getting next reverse proxy information.
                proxy_node = proxy_node->next_sibling("ReverseProxy");

                n++;
            }

            num_reversed_proxies_ = n;
        }
    }
    catch (...)
    {
        g_gateway.LogWriteCritical(L"Gateway XML: Internal error occurred when loading settings.");
        GW_COUT << "Error loading gateway XML settings!" << GW_ENDL;
        return SCERRBADGATEWAYCONFIG;
    }

    GwDeleteArray(config_contents);

    return 0;
}

// Assert some correct state parameters.
uint32_t Gateway::AssertCorrectState()
{
    SocketDataChunk* test_sdc = GwNewConstructor(SocketDataChunk);
    uint32_t err_code = 0;

    // Checking correct socket data.
    err_code = test_sdc->AssertCorrectState();
    if (err_code)
        goto FAILED;

    GW_ASSERT(core::chunk_type::link_size == MixedCodeConstants::CHUNK_LINK_SIZE);
    GW_ASSERT(sizeof(core::chunk_type::link_type) == MixedCodeConstants::CHUNK_LINK_SIZE / 2);

    GW_ASSERT(core::chunk_type::static_header_size == MixedCodeConstants::BMX_HEADER_MAX_SIZE_BYTES);
    GW_ASSERT(core::chunk_type::static_data_size == MixedCodeConstants::SOCKET_DATA_MAX_SIZE);

    GW_ASSERT(sizeof(ScSessionStruct) == MixedCodeConstants::SESSION_STRUCT_SIZE);

    GW_ASSERT(CONTENT_LENGTH_HEADER_VALUE_8BYTES == *(int64_t*)"Content-Length: ");
    GW_ASSERT(UPGRADE_HEADER_VALUE_8BYTES == *(int64_t*)"Upgrade:");
    GW_ASSERT(WEBSOCKET_HEADER_VALUE_8BYTES == *(int64_t*)"Sec-WebSocket: ");
    GW_ASSERT(REFERER_HEADER_VALUE_8BYTES == *(int64_t*)"Referer: ");
    GW_ASSERT(XREFERER_HEADER_VALUE_8BYTES == *(int64_t*)"X-Referer: ");

    GW_ASSERT(0 == (sizeof(ScSocketInfoStruct) % MEMORY_ALLOCATION_ALIGNMENT));

    GW_ASSERT(GatewayChunkSizes[NumGatewayChunkSizes - 1] > (MixedCodeConstants::MAX_EXTRA_LINKED_IPC_CHUNKS + 1) * MixedCodeConstants::CHUNK_MAX_DATA_BYTES);

    int64_t sum = 0;
    for (int32_t i = 0; i < NumGatewayChunkSizes; i++) {
        sum += GatewayChunkStoresSizes[i];
    }

    GW_ASSERT(MAX_WORKER_CHUNKS == sum);

    return 0;

FAILED:
    GwDeleteSingle(test_sdc);
    test_sdc = NULL;

    GW_ASSERT(false);
    return 0;
}

// Creates socket and binds it to server port.
uint32_t Gateway::CreateListeningSocketAndBindToPort(GatewayWorker *gw, uint16_t port_num, SOCKET& sock)
{
    // NOTE: Only first worker should be able to create sockets.
    GW_ASSERT(0 == gw->get_worker_id());

    // Creating socket.
    sock = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (sock == INVALID_SOCKET)
    {
        GW_COUT << "WSASocket() failed." << GW_ENDL;
        return PrintLastError();
    }
   
    // Special settings for aggregation sockets.
    if (port_num == setting_aggregation_port_)
    {
        int32_t OptionValue = 1;
        DWORD numberOfBytesReturned = 0;

        int status = 
            WSAIoctl(
            sock, 
            SIO_LOOPBACK_FAST_PATH,
            &OptionValue,
            sizeof(OptionValue),
            NULL,
            0,
            &numberOfBytesReturned,
            0,
            0);

        if (SOCKET_ERROR == status) {
            // Simply ignoring the error if fast loopback is not supported.
        }
    
        int32_t bufSize = 1 << 19;
        if (setsockopt(sock, SOL_SOCKET, SO_RCVBUF, (char *)&bufSize, sizeof(int)) == -1) {
            GW_ASSERT(false);
        }

        bufSize = 1 << 19;
        if (setsockopt(sock, SOL_SOCKET, SO_SNDBUF, (char *)&bufSize, sizeof(int)) == -1) {
            GW_ASSERT(false);
        }
    }
    
    // Attaching socket to IOCP.
    HANDLE temp = CreateIoCompletionPort((HANDLE) sock, gw->get_worker_iocp(), 0, 1);
    if (temp != gw->get_worker_iocp())
    {
        PrintLastError(true);
        closesocket(sock);
        sock = INVALID_SOCKET;

        GW_COUT << "Wrong IOCP returned when adding reference." << GW_ENDL;
        return SCERRGWFAILEDTOATTACHSOCKETTOIOCP;
    }

#ifdef GW_IOCP_IMMEDIATE_COMPLETION
    // Skipping completion port if operation is already successful.
    SetFileCompletionNotificationModes((HANDLE) sock, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);
#endif

    // The socket address to be passed to bind.
    sockaddr_in binding_addr;
    memset(&binding_addr, 0, sizeof(sockaddr_in));
    binding_addr.sin_family = AF_INET;
    binding_addr.sin_port = htons(port_num);

    // Checking if we have local interfaces to bind.
    if (g_gateway.setting_local_interfaces().size() > 0)
        binding_addr.sin_addr.s_addr = inet_addr(g_gateway.setting_local_interfaces().at(0).c_str());
    else
        binding_addr.sin_addr.s_addr = INADDR_ANY;

    // Binding socket to certain interface and port.
    if (bind(sock, (SOCKADDR*) &binding_addr, sizeof(binding_addr)))
    {
        PrintLastError(true);
        closesocket(sock);
        sock = INVALID_SOCKET;
       
        GW_LOG_ERROR << L"Failed to bind server port: " << port_num << L". Please check that port is not occupied by any software." << GW_WENDL;
        return SCERRGWFAILEDTOBINDPORT;
    }

    // Listening to connections.
    if (listen(sock, SOMAXCONN))
    {
        PrintLastError(true);
        closesocket(sock);
        sock = INVALID_SOCKET;

        GW_COUT << "Error listening on server socket." << GW_ENDL;
        return SCERRGWFAILEDTOLISTENONSOCKET;
    }

    return 0;
}

// Stub APC function that does nothing.
void __stdcall EmptyApcFunction(ULONG_PTR arg) {
    // Does nothing.
}

// APC function that does collect inactive sockets.
void __stdcall CollectInactiveSocketsApcFunction(ULONG_PTR arg) {

    worker_id_type worker_id = (worker_id_type) arg;

    // Going through all active connections and collecting sockets.
    g_gateway.get_worker(worker_id)->CollectInactiveSockets();
}

// APC function that is used for rebalancing sockets.
void __stdcall RebalanceSocketApcFunction(ULONG_PTR arg) {

    worker_id_type worker_id = (worker_id_type) arg;

    g_gateway.get_worker(worker_id)->ProcessRebalancedSockets();
}

// Sends an APC signal for rebalancing sockets.
void Gateway::SendRebalanceAPC(worker_id_type worker_id) {
    
    // Obtaining worker thread handle to call an APC event.
    HANDLE worker_thread_handle = g_gateway.get_worker_thread_handle(worker_id);

    QueueUserAPC(RebalanceSocketApcFunction, worker_thread_handle, worker_id);
}

// Waking up a thread using APC.
void WakeUpThreadUsingAPC(HANDLE thread_handle)
{
    // Waking up the worker thread with APC.
    QueueUserAPC(EmptyApcFunction, thread_handle, 0);
}

// Database channels events monitor thread.
uint32_t __stdcall DatabaseChannelsEventsMonitorRoutine(LPVOID params)
{
    // Index of the database as parameter.
    db_index_type db_index = *(db_index_type*)params;

    // Determine the worker by interface or channel,
    // and wake up that worker.
	HANDLE worker_thread_handle[MAX_WORKER_THREADS];
	HANDLE work_events[MAX_WORKER_THREADS];
	std::size_t work_event_index = 0;
	
    // Setting checking events for each worker.
	for (int32_t worker_id = 0; worker_id < g_gateway.setting_num_workers(); ++worker_id)
    {
		WorkerDbInterface* db_int = g_gateway.get_worker(worker_id)->GetWorkerDb(db_index);
		core::shared_interface* db_shared_int = db_int->get_shared_int();

        // Creating work event handle.
		work_events[worker_id] = db_shared_int->open_client_work_event(db_shared_int->get_client_number());

		// Waiting for all channels.
		// TODO: Unset notify flag when worker thread is spinning.
		db_shared_int->client_interface().set_notify_flag(true);

		// Sending APC on the determined worker.
		worker_thread_handle[worker_id] = g_gateway.get_worker_thread_handle(worker_id);
        
		// Waking up the worker thread with APC.
        WakeUpThreadUsingAPC(worker_thread_handle[worker_id]);
	}

    // Obtaining client interface.
    core::client_interface_type& client_int = g_gateway.get_worker(0)->GetWorkerDb(db_index)->get_shared_int()->client_interface();

#ifndef WORKER_NO_SLEEP

    // Looping until the database dies (TODO, does not work, forcedly killed).
    while (true)
    {
        // Waiting forever for more events on channels.
        client_int.wait_for_work(work_event_index, work_events, g_gateway.setting_num_workers());

        // Waking up the worker thread with APC.
        WakeUpThreadUsingAPC(worker_thread_handle[work_event_index]);
    }

#endif

    return 0;
}

uint32_t RegisterUriHandler(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    *is_handled = true;

    char* request_begin = (char*)(sd->get_accum_buf()->get_chunk_orig_buf_ptr());

    // Looking for the \r\n\r\n\r\n\r\n.
    char* end_of_message = strstr(request_begin, "\r\n\r\n\r\n\r\n");
    GW_ASSERT(NULL != end_of_message);

    // Looking for the \r\n\r\n.
    char* body_string = strstr(request_begin, "\r\n\r\n");
    GW_ASSERT(NULL != body_string);
    request_begin[sd->get_accum_buf()->get_accum_len_bytes()] = '\0';

    std::stringstream ss(body_string);
    BMX_HANDLER_TYPE handler_info;
    uint16_t port;
    std::string db_name;
    std::string app_name;
    std::string original_uri_info;
    std::string processed_uri_info;
    int32_t num_params;
    uint8_t param_types[MixedCodeConstants::MAX_URI_CALLBACK_PARAMS];

    ss >> db_name;
    ss >> app_name;
    ss >> handler_info;
    ss >> port;
    GW_ASSERT((port > 0) && (port < 65536));
    ss >> original_uri_info;
    ss >> processed_uri_info;
    ss >> num_params;

    std::replace(original_uri_info.begin(), original_uri_info.end(), '\\', ' ');
    std::replace(processed_uri_info.begin(), processed_uri_info.end(), '\\', ' ');

    for (int32_t i = 0; i < num_params; i++) {
        int32_t p;
        ss >> p;
        param_types[i] = static_cast<uint8_t>(p);
    }

    std::transform(db_name.begin(), db_name.end(), db_name.begin(), ::tolower);
    db_index_type db_index = g_gateway.FindDatabaseIndex(db_name);
    if (INVALID_DB_INDEX == db_index)
        return 0;

    GW_COUT << "Registering URI handler on " << db_name << " \"" << processed_uri_info << "\" on port " << port << " registration with handler id: " << handler_info << GW_ENDL;

    // Entering global lock.
    gw->EnterGlobalLock();

    // Registering determined URI Apps handler.
    uint32_t err_code = g_gateway.AddUriHandler(
        gw,
        port,
        app_name.c_str(),
        original_uri_info.c_str(),
        processed_uri_info.c_str(),
        param_types,
        num_params,
        handler_info,
        db_index,
        AppsUriProcessData);

    // Releasing global lock.
    gw->LeaveGlobalLock();

    if (err_code) {

        // Ignoring error code if its existing handler.
        if (SCERRHANDLERALREADYREGISTERED == err_code)
            err_code = 0;

        char temp_str[MixedCodeConstants::MAX_URI_STRING_LEN];
        sprintf_s(temp_str, MixedCodeConstants::MAX_URI_STRING_LEN, "Can't register URI handler '%S' on port %d", original_uri_info, port);

        err_code = gw->SendHttpBody(sd, temp_str, (int32_t) strlen(temp_str));
        return err_code;

    } else {

        return gw->SendPredefinedMessage(sd, kHttpOKResponse, kHttpOKResponseLength);
    }
}

uint32_t RegisterPortHandler(HandlersList* hl, GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    *is_handled = true;

    char* request_begin = (char*)(sd->get_accum_buf()->get_chunk_orig_buf_ptr());

    // Looking for the \r\n\r\n\r\n\r\n.
    char* end_of_message = strstr(request_begin, "\r\n\r\n\r\n\r\n");
    GW_ASSERT(NULL != end_of_message);

    // Looking for the \r\n\r\n.
    char* body_string = strstr(request_begin, "\r\n\r\n");
    GW_ASSERT(NULL != body_string);
    request_begin[sd->get_accum_buf()->get_accum_len_bytes()] = '\0';

    std::stringstream ss(body_string);
    BMX_HANDLER_TYPE handler_info;
    uint16_t port;
    std::string db_name;
    std::string app_name;

    ss >> db_name;
    ss >> app_name;
    ss >> handler_info;
    ss >> port;
    GW_ASSERT((port > 0) && (port < 65536));

    std::transform(db_name.begin(), db_name.end(), db_name.begin(), ::tolower);
    db_index_type db_index = g_gateway.FindDatabaseIndex(db_name);
    if (INVALID_DB_INDEX == db_index)
        return 0;

    GW_COUT << "Registering PORT handler on " << db_name << " on port " << port << " registration with handler id: " << handler_info << GW_ENDL;

    // Entering global lock.
    gw->EnterGlobalLock();

    // Registering determined URI Apps handler.
    uint32_t err_code = g_gateway.AddPortHandler(
        gw,
        port,
        app_name.c_str(),
        handler_info,
        db_index,
        AppsPortProcessData);

    // Releasing global lock.
    gw->LeaveGlobalLock();

    if (err_code) {

        // Ignoring error code if its existing handler.
        if (SCERRHANDLERALREADYREGISTERED == err_code)
            err_code = 0;

        char temp_str[MixedCodeConstants::MAX_URI_STRING_LEN];
        sprintf_s(temp_str, MixedCodeConstants::MAX_URI_STRING_LEN, "Can't register PORT handler on port %d", port);

        err_code = gw->SendHttpBody(sd, temp_str, (int32_t) strlen(temp_str));
        return err_code;

    } else {

        return gw->SendPredefinedMessage(sd, kHttpOKResponse, kHttpOKResponseLength);
    }
}

uint32_t RegisterWsHandler(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled)
{
    *is_handled = true;

    char* request_begin = (char*)(sd->get_accum_buf()->get_chunk_orig_buf_ptr());

    // Looking for the \r\n\r\n\r\n\r\n.
    char* end_of_message = strstr(request_begin, "\r\n\r\n\r\n\r\n");
    GW_ASSERT(NULL != end_of_message);

    // Looking for the \r\n\r\n.
    char* body_string = strstr(request_begin, "\r\n\r\n");
    GW_ASSERT(NULL != body_string);
    request_begin[sd->get_accum_buf()->get_accum_len_bytes()] = '\0';

    std::stringstream ss(body_string);
    BMX_HANDLER_TYPE handler_info;
    uint16_t port;
    std::string db_name;
    std::string app_name;
    ws_channel_id_type ws_channel_id;
    std::string ws_channel_name;

    ss >> db_name;
    ss >> app_name;
    ss >> handler_info;
    ss >> port;
    GW_ASSERT((port > 0) && (port < 65536));
    ss >> ws_channel_id;
    ss >> ws_channel_name;

    std::transform(db_name.begin(), db_name.end(), db_name.begin(), ::tolower);
    db_index_type db_index = g_gateway.FindDatabaseIndex(db_name);
    if (INVALID_DB_INDEX == db_index)
        return 0;

    GW_COUT << "Registering WebSocket channel handler on " << db_name << " \"" << ws_channel_name << ":" << ws_channel_id << "\" on port " << port << " registration with handler id: " << handler_info << GW_ENDL;

    // Entering global lock.
    gw->EnterGlobalLock();

    uint32_t err_code = 0;

    ServerPort* server_port = g_gateway.FindServerPort(port);

    // Checking if port exist or if its empty.
    if ((NULL == server_port) || (server_port->get_port_handlers()->IsEmpty()))
    {
        // Registering handler on active database.
        err_code = g_gateway.AddPortHandler(
            gw,
            port,
            app_name.c_str(),
            handler_info,
            0,
            OuterUriProcessData);

        GW_ASSERT(0 == err_code);

        server_port = g_gateway.FindServerPort(port);

        GW_ASSERT(NULL != server_port);
    }

    // Searching existing WebSocket handler with the same channel name.
    if (INVALID_URI_INDEX != server_port->get_registered_ws_channels()->FindRegisteredChannelName(ws_channel_name.c_str()))
        err_code = SCERRHANDLERALREADYREGISTERED;

    if (0 == err_code)
    {
        server_port->get_registered_ws_channels()->AddNewEntry(
            handler_info,
            app_name.c_str(),
            ws_channel_id,
            ws_channel_name.c_str(),
            db_index);
    }

    // Releasing global lock.
    gw->LeaveGlobalLock();

    if (err_code) {

        // Ignoring error code if its existing handler.
        if (SCERRHANDLERALREADYREGISTERED == err_code)
            err_code = 0;

        char temp_str[MixedCodeConstants::MAX_URI_STRING_LEN];
        sprintf_s(temp_str, MixedCodeConstants::MAX_URI_STRING_LEN, "Can't register WebSockets channel handler \"%S\":%d on port %d", ws_channel_name, ws_channel_id, port);

        err_code = gw->SendHttpBody(sd, temp_str, (int32_t) strlen(temp_str));
        return err_code;

    } else {

        return gw->SendPredefinedMessage(sd, kHttpOKResponse, kHttpOKResponseLength);
    }
}

// Checks for new/existing databases and updates corresponding shared memory structures.
uint32_t Gateway::CheckDatabaseChanges(const std::set<std::string>& active_databases)
{
    int32_t server_name_len = static_cast<int32_t> (setting_sc_server_type_upper_.length());

    // Enabling database down tracking flag.
    for (int32_t i = 0; i < num_dbs_slots_; i++)
        db_did_go_down_[i] = true;

    // Reading file line by line.
    uint32_t err_code = 0;
    
    // Running through all databases.
    for (std::set<std::string>::iterator it = active_databases.begin();
        it != active_databases.end();
        ++it)
    {
        // Getting current database.
        std::string current_db_name = *it;

        // Skipping incorrect database names.
        if (current_db_name.compare(0, server_name_len, setting_sc_server_type_upper_) != 0)
            continue;

        // Extracting the database name (skipping the server name and underscore).
        current_db_name = current_db_name.substr(server_name_len + 1, current_db_name.length() - server_name_len);
        std::transform(current_db_name.begin(), current_db_name.end(), current_db_name.begin(), ::tolower);

        bool is_new_database = true;
        for (int32_t i = 0; i < num_dbs_slots_; i++)
        {
            // Checking if it is an existing database.
            if (0 == active_databases_[i].get_db_name().compare(current_db_name))
            {
                // Existing database is up.
                db_did_go_down_[i] = false;
                is_new_database = false;

                // Creating new shared memory interface.
                break;
            }
        }
        
        // We have a new database being up.
        if (is_new_database)
        {

#ifdef GW_DATABASES_DIAG
            GW_PRINT_GLOBAL << "Attaching a new database: " << current_db_name << GW_ENDL;
#endif

            // Entering global lock.
            EnterGlobalLock();

            // Finding first empty slot.
            int32_t empty_db_index = 0;
            for (empty_db_index = 0; empty_db_index < num_dbs_slots_; ++empty_db_index)
            {
                // Checking if database slot is empty.
                if (active_databases_[empty_db_index].IsEmpty())
                    break;
            }

            GW_ASSERT(empty_db_index < MAX_ACTIVE_DATABASES);

            // Filling necessary fields.
            active_databases_[empty_db_index].Init(
                current_db_name,
                ++db_seq_num_,
                empty_db_index);

            db_did_go_down_[empty_db_index] = false;

            // Increasing number of active databases.
            if (empty_db_index >= num_dbs_slots_)
                num_dbs_slots_++;

            // Adding to workers database interfaces.
            bool db_init_failed = false;
            for (int32_t i = 0; i < setting_num_workers_; i++)
            {
                try
                {
                    err_code = gw_workers_[i].AddNewDatabase(empty_db_index);
                }
                catch (...)
                {
                    // Reporting a warning to server log.
                    std::wstring temp_str = std::wstring(L"Attaching new database failed: ") +
                        std::wstring(current_db_name.begin(), current_db_name.end());
                    g_gateway.LogWriteWarning(temp_str.c_str());

                    // Deleting worker database parts.
                    for (int32_t i = 0; i < setting_num_workers_; i++)
                        gw_workers_[i].DeleteInactiveDatabase(empty_db_index);

                    // Resetting newly created database.
                    active_databases_[empty_db_index].Reset(true);

                    // Removing the database slot if it was the last.
                    if (num_dbs_slots_ == empty_db_index + 1)
                        num_dbs_slots_--;

                    // Leaving global lock.
                    LeaveGlobalLock();

                    db_init_failed = true;
                    
                    break;
                }
                
                if (err_code)
                {
                    // Leaving global lock.
                    LeaveGlobalLock();
                    return err_code;
                }
            }

            // Checking if any error occurred.
            if (db_init_failed)
                continue;

            // Spawning channels events monitor.
            err_code = active_databases_[empty_db_index].SpawnChannelsEventsMonitor();
            if (err_code)
            {
                // Leaving global lock.
                LeaveGlobalLock();
                return err_code;
            }

            // Leaving global lock.
            LeaveGlobalLock();

            // Registering gateway ready on first worker.
            for (int32_t i = 0; i < setting_num_workers_; i++)
            {
                err_code = gw_workers_[i].GetWorkerDb(empty_db_index)->SetGatewayReadyForDbPushes();
                if (err_code)
                {
                    GW_ASSERT(false);
                }
            }
        }
    }

    // Checking what databases went down.
    for (int32_t s = 0; s < num_dbs_slots_; s++)
    {
        if ((db_did_go_down_[s]) && (!active_databases_[s].IsDeletionStarted()))
        {
#ifdef GW_DATABASES_DIAG
            GW_PRINT_GLOBAL << "Start detaching dead database: " << active_databases_[s].get_db_name() << GW_ENDL;
#endif

            // Entering global lock.
            EnterGlobalLock();

            // Killing channels events monitor thread.
            active_databases_[s].KillChannelsEventsMonitor();

            // Start database deletion.
            active_databases_[s].StartDeletion();

            // Leaving global lock.
            LeaveGlobalLock();
        }
    }

    return 0;
}

// Active database constructor.
ActiveDatabase::ActiveDatabase()
{
    num_holding_workers_ = 0;
    InitializeCriticalSection(&cs_db_checks_);

    StartDeletion();
}

// Initializes this active database slot.
void ActiveDatabase::Init(
    std::string db_name,
    uint64_t unique_num,
    db_index_type db_index)
{
    db_name_ = db_name;
    unique_num_unsafe_ = unique_num;
    db_index_ = db_index;

    is_empty_ = false;
    is_ready_for_cleanup_ = false;

    num_holding_workers_ = g_gateway.setting_num_workers();

    // Construct the database_shared_memory_parameters_name. The format is
    // <DATABASE_NAME_PREFIX>_<SERVER_TYPE>_<DATABASE_NAME>_0
    std::string shm_params_name = (std::string)DATABASE_NAME_PREFIX + "_" +
        g_gateway.setting_sc_server_type_upper() + "_" + StringToUpperCopy(db_name_) + "_0";

    // Open the database shared memory parameters file and obtains a pointer to
    // the shared structure.
    core::database_shared_memory_parameters_ptr db_shm_params(shm_params_name.c_str());

    // Name of the database shared memory segment.
    char seq_num_str[16];
    _itoa_s(db_shm_params->get_sequence_number(), seq_num_str, 16, 10);
    shm_seg_name_ = std::string(DATABASE_NAME_PREFIX) + "_" +
        g_gateway.setting_sc_server_type_upper() + "_" +
        StringToUpperCopy(db_name_) + "_" +
        std::string(seq_num_str);
}

// Resets database slot.
void ActiveDatabase::Reset(bool hard_reset)
{
    unique_num_unsafe_ = INVALID_UNIQUE_DB_NUMBER;
    db_name_ = "";

    if (hard_reset)
    {
        is_empty_ = true;
    }
}

// Checks if this database slot empty.
bool ActiveDatabase::IsEmpty()
{
    if (is_empty_)
        return true;

    EnterCriticalSection(&cs_db_checks_);

    // Checking if all chunks for this database were released.
    is_empty_ = (num_holding_workers_ == 0) && IsReadyForCleanup();

    LeaveCriticalSection(&cs_db_checks_);

    return is_empty_;
}

bool ActiveDatabase::IsReadyForCleanup()
{
    if (is_ready_for_cleanup_)
        return true;

    EnterCriticalSection(&cs_db_checks_);

    // Checking if all chunks for this database were released.
    is_ready_for_cleanup_ = (INVALID_UNIQUE_DB_NUMBER == unique_num_unsafe_);

    LeaveCriticalSection(&cs_db_checks_);

    return is_ready_for_cleanup_;
}

// Makes this database slot empty.
void ActiveDatabase::StartDeletion()
{
    // Deleting all associated info with this database from ports.
    uint err_code = g_gateway.EraseDatabaseFromPorts(db_index_);
    GW_ASSERT(0 == err_code);

    // Resetting slot.
    Reset(false);
}

// Initializes WinSock, all core data structures, binds server sockets.
uint32_t Gateway::Init()
{
    // Checking if already initialized.
    GW_ASSERT((gw_workers_ == NULL) && (worker_thread_handles_ == NULL));

    // Allocating workers data.
    gw_workers_ = GwNewArray(GatewayWorker, setting_num_workers_);
    worker_thread_handles_ = GwNewArray(HANDLE, setting_num_workers_);

    // Filling up worker parameters.
    for (int i = 0; i < setting_num_workers_; i++)
    {
        int32_t errCode = gw_workers_[i].Init(i);
        if (errCode != 0)
            return errCode;
    }

    // Going throw all needed ports.
    for (int32_t p = 0; p < num_server_ports_slots_; p++)
    {
        // Skipping empty port.
        if (server_ports_[p].IsEmpty())
            continue;

        SOCKET server_socket = INVALID_SOCKET;

        // Creating socket and binding to port (only on the first worker).
        uint32_t err_code = CreateListeningSocketAndBindToPort(
            &gw_workers_[0],
            server_ports_[p].get_port_number(),
            server_socket);

        GW_ERR_CHECK(err_code);
    }

    // Obtaining function pointers (AcceptEx, ConnectEx, DisconnectEx).
    uint32_t temp;
    SOCKET temp_socket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (temp_socket == INVALID_SOCKET)
    {
        GW_COUT << "WSASocket() failed." << GW_ENDL;
        return PrintLastError();
    }

    if (WSAIoctl(temp_socket, SIO_GET_EXTENSION_FUNCTION_POINTER, &AcceptExGuid, sizeof(AcceptExGuid), &AcceptExFunc, sizeof(AcceptExFunc), (LPDWORD)&temp, NULL, NULL))
    {
        GW_COUT << "Failed WSAIoctl(AcceptEx)." << GW_ENDL;
        return PrintLastError();
    }

    if (WSAIoctl(temp_socket, SIO_GET_EXTENSION_FUNCTION_POINTER, &ConnectExGuid, sizeof(ConnectExGuid), &ConnectExFunc, sizeof(ConnectExFunc), (LPDWORD)&temp, NULL, NULL))
    {
        GW_COUT << "Failed WSAIoctl(ConnectEx)." << GW_ENDL;
        return PrintLastError();
    }

    if (WSAIoctl(temp_socket, SIO_GET_EXTENSION_FUNCTION_POINTER, &DisconnectExGuid, sizeof(DisconnectExGuid), &DisconnectExFunc, sizeof(DisconnectExFunc), (LPDWORD)&temp, NULL, NULL))
    {
        GW_COUT << "Failed WSAIoctl(DisconnectEx)." << GW_ENDL;
        return PrintLastError();
    }
    closesocket(temp_socket);

    // Global HTTP init.
    HttpGlobalInit();

    // Initializing Gateway logger.
    gw_log_writer_.Init(setting_log_file_path_);
    
    // Loading URI codegen matcher.
    codegen_uri_matcher_ = GwNewConstructor(CodegenUriMatcher);
    codegen_uri_matcher_->Init();

    // Loading Clang for URI matching.
    HMODULE clang_dll = LoadLibrary(L"GatewayClang.dll");
    GW_ASSERT(clang_dll != NULL);

    typedef void (*GwClangInit)();
    GwClangInit clang_init = (GwClangInit) GetProcAddress(clang_dll, "GwClangInit");
    GW_ASSERT(clang_init != NULL);
    clang_init();

    ClangCompileAndGetFunc = (GwClangCompileCodeAndGetFuntion) GetProcAddress(
        clang_dll,
        "GwClangCompileCodeAndGetFuntion");

    GW_ASSERT(ClangCompileAndGetFunc != NULL);

    ClangDestroyEngineFunc = (ClangDestroyEngineType) GetProcAddress(
        clang_dll,
        "GwClangDestroyEngine");

    GW_ASSERT(ClangDestroyEngineFunc != NULL);

    // Running a test compilation.
    typedef int (*example_main_func_type) ();
    example_main_func_type example_main_func;
    void* clang_engine = NULL;
    void** clang_engine_addr = &clang_engine;
    example_main_func = (example_main_func_type) g_gateway.ClangCompileAndGetFunc(
        clang_engine_addr,
        "int main() { return 124; }",
        "main",
        false);

    GW_ASSERT(example_main_func != NULL);

    g_gateway.ClangDestroyEngineFunc(clang_engine);

    // Registering shared memory monitor interface.
    shm_monitor_int_name_ = setting_sc_server_type_upper_ + "_" + MONITOR_INTERFACE_SUFFIX;

    // Waiting until we can open shared memory monitor interface.
    GW_COUT << "Opening scipcmonitor interface: ";

    // Get monitor_interface_ptr for monitor_interface_name.
    shm_monitor_interface_.init(shm_monitor_int_name_.c_str());
    GW_COUT << "opened!" << GW_ENDL;

#if 0
    // Send registration request to the monitor and try to acquire an owner_id.
    // Without an owner_id we can not proceed and have to exit.
    // Get process id and store it in the monitor_interface.
    gateway_pid_.set_current();

    // Try to register gateway process pid. Wait up to 10000 ms.
    uint32_t err_code = shm_monitor_interface_->register_client_process(gateway_pid_, gateway_owner_id_, 10000/*ms*/);
    GW_ASSERT(0 == err_code);
#else
    gateway_pid_.set_current();
	gateway_owner_id_ = 3;
#endif

    // Registering all gateway handlers.
    RegisterGatewayHandlers();

    // Indicating that network gateway is ready
    // (should be first line of the output).
    GW_COUT << "Gateway is ready!" << GW_ENDL;

    // Indicating begin of new logging session.
    time_t raw_time;
    time(&raw_time);
    tm timeinfo;
    localtime_s(&timeinfo, &raw_time);
    char temp_time_str[32];
    asctime_s(temp_time_str, 32, &timeinfo);
    GW_PRINT_GLOBAL << "New logging session: " << temp_time_str << GW_ENDL;

    return 0;
}

void Gateway::RegisterGatewayHandlers() {

    uint32_t err_code = 0;

    // Registering all proxies.
    for (int32_t i = 0; i < num_reversed_proxies_; i++)
    {
        // Registering URI handlers.
        err_code = AddUriHandler(
            &gw_workers_[0],
            reverse_proxies_[i].sc_proxy_port_,
            "gateway",
            reverse_proxies_[i].matching_method_and_uri_.c_str(),
            reverse_proxies_[i].matching_method_and_uri_processed_.c_str(),
            NULL,
            0,
            bmx::BMX_INVALID_HANDLER_INFO,
            INVALID_DB_INDEX,
            GatewayUriProcessProxy,
            false,
            reverse_proxies_ + i);

        GW_ASSERT(0 == err_code);
    }

    // Registering URI handler for gateway statistics.
    err_code = AddUriHandler(
        &gw_workers_[0],
        setting_internal_system_port_,
        "gateway",
        "POST /gw/handler/uri",
        "POST /gw/handler/uri ",
        NULL,
        0,
        bmx::BMX_INVALID_HANDLER_INFO,
        INVALID_DB_INDEX,
        RegisterUriHandler,
        true);

    GW_ASSERT(0 == err_code);

    // Registering URI handler for gateway statistics.
    err_code = AddUriHandler(
        &gw_workers_[0],
        setting_internal_system_port_,
        "gateway",
        "POST /gw/handler/ws",
        "POST /gw/handler/ws ",
        NULL,
        0,
        bmx::BMX_INVALID_HANDLER_INFO,
        INVALID_DB_INDEX,
        RegisterWsHandler,
        true);

    GW_ASSERT(0 == err_code);

    // Registering URI handler for gateway statistics.
    err_code = AddUriHandler(
        &gw_workers_[0],
        setting_internal_system_port_,
        "gateway",
        "POST /gw/handler/port",
        "POST /gw/handler/port ",
        NULL,
        0,
        bmx::BMX_INVALID_HANDLER_INFO,
        INVALID_DB_INDEX,
        RegisterPortHandler,
        true);

    GW_ASSERT(0 == err_code);

    // Registering URI handler for gateway statistics.
    err_code = AddUriHandler(
        &gw_workers_[0],
        setting_internal_system_port_,
        "gateway",
        "GET /gwstats",
        "GET /gwstats ",
        NULL,
        0,
        bmx::BMX_INVALID_HANDLER_INFO,
        INVALID_DB_INDEX,
        GatewayStatisticsInfo,
        true);

    GW_ASSERT(0 == err_code);

    // Registering URI handler for gateway statistics.
    err_code = AddUriHandler(
        &gw_workers_[0],
        setting_internal_system_port_,
        "gateway",
        "GET /gwtest",
        "GET /gwtest ",
        NULL,
        0,
        bmx::BMX_INVALID_HANDLER_INFO,
        INVALID_DB_INDEX,
        GatewayTestSample,
        true);

    GW_ASSERT(0 == err_code);

    // Registering URI handler for gateway statistics.
    err_code = AddUriHandler(
        &gw_workers_[0],
        setting_internal_system_port_,
        "gateway",
        "GET /profiler/gateway",
        "GET /profiler/gateway ",
        NULL,
        0,
        bmx::BMX_INVALID_HANDLER_INFO,
        INVALID_DB_INDEX,
        GatewayProfilersInfo,
        true);

    GW_ASSERT(0 == err_code);

    if (0 != setting_aggregation_port_)
    {
        // Registering port handler for aggregation.
        err_code = AddPortHandler(
            &gw_workers_[0],
            setting_aggregation_port_,
            "gateway",
            bmx::BMX_INVALID_HANDLER_INFO,
            INVALID_DB_INDEX,
            PortAggregator);

        GW_ASSERT(0 == err_code);
    }
}

// Printing statistics for all workers.
void Gateway::PrintWorkersStatistics(std::stringstream& stats_stream)
{
    bool first = true;

    // Emptying the statistics stream.
    stats_stream.str(std::string());

    // Going through all ports.
    stats_stream << "[";
    for (int32_t w = 0; w < setting_num_workers_; w++)
    {
        if (!first)
            stats_stream << ",";
        first = false;

        gw_workers_[w].PrintInfo(stats_stream);
    }
    stats_stream << "]";
}

// Printing statistics for all ports.
void Gateway::PrintPortStatistics(std::stringstream& stats_stream)
{
    bool first = true;

    // Emptying the statistics stream.
    stats_stream.str(std::string());

    // Going through all ports.
    stats_stream << "[";
    for (int32_t p = 0; p < num_server_ports_slots_; p++)
    {
        // Checking that port is not empty.
        if (!server_ports_[p].IsEmpty())
        {
            if (!first)
                stats_stream << ",";
            first = false;

            server_ports_[p].PrintInfo(stats_stream);
        }
        
    }
    stats_stream << "]";
}

// Printing statistics for all databases.
void Gateway::PrintDatabaseStatistics(std::stringstream& stats_stream)
{
    bool first = true;

    // Emptying the statistics stream.
    stats_stream.str(std::string());

    // Going through all databases.
    stats_stream << "[";
    for (int32_t d = 0; d < num_dbs_slots_; d++)
    {
        if (!active_databases_[d].IsEmpty())
        {
            if (!first)
                stats_stream << ",";
            first = false;

            active_databases_[d].PrintInfo(stats_stream);
        }
    }
    stats_stream << "]";
}

// Current global profilers value.
std::string Gateway::GetGlobalProfilersString(int32_t* out_stats_len_bytes)
{
    *out_stats_len_bytes = 0;

    // Filing everything into one stream.
    std::stringstream ss;

    // Emptying the statistics stream.
    ss.str(std::string());

    bool first = true;

    // Going through all ports.
    ss << "{\"profilers\":[";
    for (int32_t w = 0; w < setting_num_workers_; w++)
    {
        if (!first)
            ss << ",";

        first = false;

        ss << "{\"schedulerId\":" << w << "," << utils::Profiler::GetCurrentNotHosted(w)->GetResultsInJson(false) << "}";

        // NOTE: We automatically resetting all profilers.
        utils::Profiler::GetCurrentNotHosted(w)->ResetAll();
    }
    ss << "]}";

    // Calculating final data length in bytes.
    std::string str = ss.str();

    *out_stats_len_bytes = static_cast<int32_t>(str.length());

    return str;
}

// Current global statistics value.
const char* Gateway::GetGlobalStatisticsString(int32_t* out_stats_len_bytes)
{
    *out_stats_len_bytes = 0;

    EnterCriticalSection(&cs_statistics_);

    // Printing port information.
    PrintPortStatistics(global_port_statistics_stream_);

    // Printing database statistics.
    PrintDatabaseStatistics(global_databases_statistics_stream_);

    // Printing workers statistics.
    PrintWorkersStatistics(global_workers_statistics_stream_);

    // Filing everything into one stream.
    std::stringstream all_stats_stream;

    all_stats_stream << "{\"ports\":";
    all_stats_stream << global_port_statistics_stream_.str();
    all_stats_stream << ",\"databases\":";
    all_stats_stream << global_databases_statistics_stream_.str();
    all_stats_stream << ",\"workers\":";
    all_stats_stream << global_workers_statistics_stream_.str();
    all_stats_stream << ",\"global\":";
    all_stats_stream << global_statistics_stream_.str();
    all_stats_stream << "}";

    // Total number of bytes in HTTP response.
    int32_t total_response_bytes = kHttpStatisticsHeaderLength;

    // Getting number of written bytes to the stream.
    int32_t all_stats_bytes = static_cast<int32_t> (all_stats_stream.tellp());
    total_response_bytes += all_stats_bytes;

    // Checking for not too big statistics.
    if (total_response_bytes >= MAX_STATS_LENGTH)
    {
        all_stats_bytes = MAX_STATS_LENGTH - kHttpStatisticsHeaderLength;
        total_response_bytes = MAX_STATS_LENGTH;
    }

    // Copying characters from stream to given buffer.
    all_stats_stream.seekg(0);
    all_stats_stream.rdbuf()->sgetn(global_statistics_string_ + kHttpStatisticsHeaderLength, all_stats_bytes);

    // Sealing the string.
    global_statistics_string_[total_response_bytes] = '\0';

    // Making length a white space.
    *(uint64_t*)(global_statistics_string_ + kHttpStatisticsHeaderInsertPoint) = 0x2020202020202020;
    
    // Converting content length to string.
    WriteUIntToString(global_statistics_string_ + kHttpStatisticsHeaderInsertPoint, total_response_bytes - kHttpStatisticsHeaderLength);

    LeaveCriticalSection(&cs_statistics_);

    // Calculating final data length in bytes.
    *out_stats_len_bytes = total_response_bytes;

    return global_statistics_string_;
}

// Check and wait for global lock.
void Gateway::SuspendWorker(GatewayWorker* gw)
{
    gw->set_worker_suspended(true);

    // Entering the critical section.
    EnterCriticalSection(&cs_global_lock_);

    gw->set_worker_suspended(false);
        
    // Leaving the critical section.
    LeaveCriticalSection(&cs_global_lock_);
}

// Getting specific worker information.
GatewayWorker* Gateway::get_worker(worker_id_type worker_id)
{
    return gw_workers_ + worker_id;
}

// Delete all information associated with given database from server ports.
uint32_t Gateway::EraseDatabaseFromPorts(db_index_type db_index)
{
    // Going through all ports.
    for (int32_t i = 0; i < num_server_ports_slots_; i++)
    {
        // Checking that port is not empty.
        if (!server_ports_[i].IsEmpty())
        {
            // Deleting port handlers if any.
            server_ports_[i].EraseDb(db_index);
        }
    }

    // Removing empty server ports.
    CleanUpEmptyPorts();

    return 0;
}

// Cleans up empty ports.
void Gateway::CleanUpEmptyPorts()
{
    // Going through all ports.
    for (int32_t i = 0; i < num_server_ports_slots_; i++)
    {
        // Checking if port is not used anywhere.
        if (server_ports_[i].IsEmpty())
            server_ports_[i].Erase();
    }

    // Removing deleted trailing server ports.
    for (int32_t i = (num_server_ports_slots_ - 1); i >= 0; i--)
    {
        // Removing until one server port is not empty.
        if (!server_ports_[i].IsEmpty())
            break;

        num_server_ports_slots_--;
    }
}

// Getting the total number of overflow chunks for all databases.
int64_t Gateway::NumberOverflowChunksAllWorkers()
{
    int64_t num_overflow_chunks = 0;

    for (int32_t w = 0; w < setting_num_workers_; w++)
        num_overflow_chunks += gw_workers_[w].NumOverflowChunks();

    return num_overflow_chunks;
}

// Waits for all workers to suspend.
void Gateway::WaitAllWorkersSuspended()
{
    int32_t num_worker_locked = 0;

    // First waking up all workers.
    WakeUpAllWorkers();

    // Waiting for all workers to suspend.
    while (num_worker_locked < setting_num_workers_)
    {
        Sleep(1);
        num_worker_locked = 0;
        for (int32_t i = 0; i < setting_num_workers_; i++)
        {
            if (gw_workers_[i].worker_suspended())
                num_worker_locked++;
        }
    }
}

// Waking up all workers if they are sleeping.
void Gateway::WakeUpAllWorkers()
{
    // Waking up all the workers if needed.
    for (worker_id_type i = 0; i < g_gateway.setting_num_workers(); i++)
    {
        // Obtaining worker thread handle to call an APC event.
        HANDLE worker_thread_handle = g_gateway.get_worker_thread_handle(i);

        // Waking up the worker with APC.
        WakeUpThreadUsingAPC(worker_thread_handle);
    }
}

// Waking up all workers if they are sleeping.
void Gateway::WakeUpAllWorkersToCollectInactiveSockets()
{
    // Waking up all the workers if needed.
    for (worker_id_type i = 0; i < g_gateway.setting_num_workers(); i++)
    {
        // Obtaining worker thread handle to call an APC event.
        HANDLE worker_thread_handle = g_gateway.get_worker_thread_handle(i);

        // Waking up the worker thread with APC.
        QueueUserAPC(CollectInactiveSocketsApcFunction, worker_thread_handle, i);
    }
}

// Opens active databases events with monitor.
uint32_t Gateway::OpenActiveDatabasesUpdatedEvent()
{
    // Number of characters in the multi-byte string after being converted.
    std::size_t length;

    // Construct the active_databases_updated_event_name.
    char active_databases_updated_event_name[active_databases_updated_event_name_size];

    // Format: "Local\<server_name>_ipc_monitor_cleanup_event".
    // Example: "Local\PERSONAL_ipc_monitor_cleanup_event"
    if ((length = _snprintf_s(active_databases_updated_event_name, _countof
        (active_databases_updated_event_name), active_databases_updated_event_name_size
        -1 /* null */, "Local\\%s_"ACTIVE_DATABASES_UPDATED_EVENT, setting_sc_server_type_upper_.c_str())) < 0) {
            return SCERRFORMATACTIVEDBUPDATEDEVNAME;
    }
    active_databases_updated_event_name[length] = '\0';

    wchar_t w_active_databases_updated_event_name[active_databases_updated_event_name_size];

    if ((mbstowcs_s(&length, w_active_databases_updated_event_name, active_databases_updated_event_name_size,
        active_databases_updated_event_name, _TRUNCATE)) != 0) {
            // Failed to convert active_databases_updated_event_name to multi-byte string.
            return SCERRCONVERTACTIVEDBUPDATEDEVMBS;
    }
    w_active_databases_updated_event_name[length] = L'\0';

    // Open the active_databases_updated_event_name.
    if ((active_databases_updates_event() = ::OpenEvent(SYNCHRONIZE | EVENT_MODIFY_STATE,
        FALSE, w_active_databases_updated_event_name)) == NULL) {
            // Failed to open the active_databases_updated_event.
            return SCERROPENACTIVEDBUPDATEDEV;
    }

    return 0;
}

// Entry point for monitoring databases thread.
uint32_t __stdcall MonitorDatabases(LPVOID params)
{
    uint32_t err_code = 0;
    std::set<std::string> active_databases_set;

    // Opening active databases event with monitor.
    err_code = g_gateway.OpenActiveDatabasesUpdatedEvent();
    if (err_code)
        return err_code;

    GW_COUT << "Waiting for active database update events..." << GW_ENDL;

    while (true)
    {
        // Waiting for shared monitor notification.
        switch (::WaitForSingleObject(g_gateway.active_databases_updates_event(), INFINITE))
        {
            case WAIT_OBJECT_0:
            {
                // The IPC monitor updated the active databases set.
                g_gateway.the_monitor_interface()->active_database_set()
                    .copy(active_databases_set, g_gateway.active_databases_updates_event());

                // Checking for any database changes in active databases file.
                err_code = g_gateway.CheckDatabaseChanges(active_databases_set);
                if (err_code)
                    return err_code;

                break;
            }

            case WAIT_TIMEOUT:
            {
                break;
            }
                
            case WAIT_FAILED:
            {
                break;
            }
        }
    }

    return 0;
}

// Entry point for gateway worker.
uint32_t __stdcall MonitorDatabasesRoutine(LPVOID params)
{
    uint32_t err_code = 0;

    // Catching all unhandled exceptions in this thread.
    GW_SC_BEGIN_FUNC

    // Starting routine.
    err_code = MonitorDatabases(params);

    // Catching all unhandled exceptions in this thread.
    GW_SC_END_FUNC

    wchar_t temp[256];
    wsprintf(temp, L"Databases monitoring thread exited with error code: %d", err_code);
    g_gateway.LogWriteError(temp);

    return err_code;
}

// Entry point for gateway worker.
uint32_t __stdcall GatewayWorkerRoutine(LPVOID params)
{
    uint32_t err_code = 0;

    // Catching all unhandled exceptions in this thread.
    GW_SC_BEGIN_FUNC

    // Starting routine.
    err_code = ((GatewayWorker *)params)->WorkerRoutine();

    // Catching all unhandled exceptions in this thread.
    GW_SC_END_FUNC

    wchar_t temp[256];
    wsprintf(temp, L"Gateway worker thread exited with error code: %d", err_code);
    g_gateway.LogWriteError(temp);

    return err_code;
}

// Entry point for channels events monitor.
uint32_t __stdcall AllDatabasesChannelsEventsMonitorRoutine(LPVOID params)
{
    // Catching all unhandled exceptions in this thread.
    GW_SC_BEGIN_FUNC

    while (true)
    {
        Sleep(100000);

        for (int32_t i = 0; i < g_gateway.get_num_dbs_slots(); i++)
        {

        }
    }

    return 0;

    // Catching all unhandled exceptions in this thread.
    GW_SC_END_FUNC
}

void Gateway::DisconnectSocket(SOCKET s) {

    GW_ASSERT(INVALID_SOCKET != s);

    DisconnectExFunc(s, NULL, 0, 0);
    closesocket(s);
}

// Entry point for inactive sockets cleanup.
uint32_t __stdcall InactiveSocketsCleanupRoutine(LPVOID params)
{
    // Catching all unhandled exceptions in this thread.
    GW_SC_BEGIN_FUNC

    while (true)
    {
        for (int32_t i = 0; i < SOCKET_LIFETIME_MULTIPLIER; i++) {

            // Sleeping minimum interval.
            Sleep(g_gateway.get_min_inactive_socket_life_seconds() * 1000);

            // Increasing global time by minimum number of seconds.
            g_gateway.step_global_timer_unsafe(g_gateway.get_min_inactive_socket_life_seconds());
        }

        // Waking up all workers to perform the collection of inactive sockets.
        g_gateway.WakeUpAllWorkersToCollectInactiveSockets();
    }

    return 0;

    // Catching all unhandled exceptions in this thread.
    GW_SC_END_FUNC
}


// Entry point for Gateway logging routine.
uint32_t __stdcall GatewayLoggingRoutine(LPVOID params)
{
    // Catching all unhandled exceptions in this thread.
    GW_SC_BEGIN_FUNC

    while (true)
    {
        // Sleeping some interval.
        Sleep(5000);

        // Dumping to gateway log file (if anything new was logged).
#ifdef GW_LOG_TO_FILE
        g_gateway.get_gw_log_writer()->DumpToLogFile();
#endif
    }

    return 0;

    // Catching all unhandled exceptions in this thread.
    GW_SC_END_FUNC
}

// Entry point for all threads monitor routine.
uint32_t __stdcall GatewayMonitorRoutine(LPVOID params)
{
    uint32_t err_code = 0;

    // Catching all unhandled exceptions in this thread.
    GW_SC_BEGIN_FUNC

    // Starting routine.
    err_code = g_gateway.GatewayMonitor();

    // Catching all unhandled exceptions in this thread.
    GW_SC_END_FUNC

    wchar_t temp[256];
    wsprintf(temp, L"Gateway monitoring thread exited with error code: %d", err_code);
    g_gateway.LogWriteError(temp);

    return err_code;
}

// Cleaning up all global resources.
uint32_t Gateway::GlobalCleanup()
{
    // Cleanup WinSock.
    WSACleanup();

    // Deleting critical sections.
    DeleteCriticalSection(&cs_global_lock_);
    DeleteCriticalSection(&cs_statistics_);

    // Closing the log.
    CloseStarcounterLog();

    return 0;
}

// Checks that all Gateway threads are alive.
uint32_t Gateway::GatewayMonitor()
{
    int32_t running_time_seconds = 0;

    while (1)
    {
        // Sleeping some interval.
        Sleep(GW_MONITOR_THREAD_TIMEOUT_SECONDS * 1000);

        // Checking if workers are still running.
        for (int32_t i = 0; i < setting_num_workers_; i++)
        {
            // Waking up the worker thread with APC.
            WakeUpThreadUsingAPC(g_gateway.get_worker_thread_handle(i));

            // Checking if alive.
            if (!WaitForSingleObject(worker_thread_handles_[i], 0))
            {
                // Stating explicitly that this worker is dead.
                gw_workers_[i].set_worker_suspended(true);

                // Printing diagnostics.
                GW_COUT << "Worker " << i << " is dead." << GW_ENDL;

                GW_ASSERT(false);
            }
        }

        // Checking if database monitor thread is alive.
        if (!WaitForSingleObject(db_monitor_thread_handle_, 0))
        {
            GW_COUT << "Active databases monitor thread is dead." << GW_ENDL;
            GW_ASSERT(false);
        }

        // Checking if database channels events thread is alive.
        if (!WaitForSingleObject(channels_events_thread_handle_, 0))
        {
            GW_COUT << "Channels events thread is dead." << GW_ENDL;
            GW_ASSERT(false);
        }

        // Checking if dead sockets cleanup thread is alive.
        if (!WaitForSingleObject(dead_sockets_cleanup_thread_handle_, 0))
        {
            GW_COUT << "Dead sockets cleanup thread is dead." << GW_ENDL;
            GW_ASSERT(false);
        }

        // Checking if gateway logging thread is alive.
        if (!WaitForSingleObject(gateway_logging_thread_handle_, 0))
        {
            GW_COUT << "Gateway logging thread is dead." << GW_ENDL;
            GW_ASSERT(false);
        }
    }

    return 0;
}

// Safely shutdowns the gateway.
void Gateway::ShutdownGateway(GatewayWorker* gw, int32_t exit_code)
{
    // Entering safe mode.
    if (gw)
        gw->EnterGlobalLock();
    else
        EnterGlobalLock();

    // Killing process with given exit code.
    ExitProcess(exit_code);
}

// Prints statistics, monitors all gateway threads, etc.
uint32_t Gateway::StatisticsAndMonitoringRoutine()
{
    bool first = true;

    // Previous statistics values.
    int64_t prevBytesReceivedAllWorkers = 0,
        prevBytesSentAllWorkers = 0,
        prevSentNumAllWorkers = 0,
        prevRecvNumAllWorkers = 0,
        prevProcessedHttpRequestsAllWorkers = 0;

    // New statistics values.
    int64_t newBytesReceivedAllWorkers = 0,
        newBytesSentAllWorkers = 0,
        newSentNumAllWorkers = 0,
        newRecvNumAllWorkers = 0,
        newProcessedHttpRequestsAllWorkers = 0;

    // Difference between new and previous statistics values.
    int64_t diffBytesReceivedAllWorkers = 0,
        diffBytesSentAllWorkers = 0,
        diffSentNumAllWorkers = 0,
        diffRecvNumAllWorkers = 0,
        diffProcessedHttpRequestsAllWorkers = 0;

    while(1)
    {
        // Waiting some time for statistics updates.
        Sleep(1000);

        // Checking that all gateway threads are alive.
        if (!WaitForSingleObject(all_threads_monitor_handle_, 0))
        {
            GW_COUT << "Some of the gateway threads are dead. Exiting process." << GW_ENDL;

            GW_ASSERT(false);
        }

        // Resetting new values.
        newBytesReceivedAllWorkers = 0;
        newBytesSentAllWorkers = 0;
        newSentNumAllWorkers = 0;
        newRecvNumAllWorkers = 0;
        newProcessedHttpRequestsAllWorkers = g_gateway.get_num_processed_http_requests();

        // Fetching new statistics.
        for (int32_t i = 0; i < setting_num_workers_; i++)
        {
            newBytesReceivedAllWorkers += gw_workers_[i].get_worker_stats_bytes_received();
            newBytesSentAllWorkers += gw_workers_[i].get_worker_stats_bytes_sent();
            newSentNumAllWorkers += gw_workers_[i].get_worker_stats_sent_num();
            newRecvNumAllWorkers += gw_workers_[i].get_worker_stats_recv_num();
        }

        // Calculating differences.
        diffBytesReceivedAllWorkers = newBytesReceivedAllWorkers - prevBytesReceivedAllWorkers;
        diffBytesSentAllWorkers = newBytesSentAllWorkers - prevBytesSentAllWorkers;
        diffSentNumAllWorkers = newSentNumAllWorkers - prevSentNumAllWorkers;
        diffRecvNumAllWorkers = newRecvNumAllWorkers - prevRecvNumAllWorkers;
        diffProcessedHttpRequestsAllWorkers = newProcessedHttpRequestsAllWorkers - prevProcessedHttpRequestsAllWorkers;

        // Updating previous values.
        prevBytesReceivedAllWorkers = newBytesReceivedAllWorkers;
        prevBytesSentAllWorkers = newBytesSentAllWorkers;
        prevSentNumAllWorkers = newSentNumAllWorkers;
        prevRecvNumAllWorkers = newRecvNumAllWorkers;
        prevProcessedHttpRequestsAllWorkers = newProcessedHttpRequestsAllWorkers;

        // Calculating bandwidth.
        double recv_bandwidth_mbit_total = ((diffBytesReceivedAllWorkers * 8) / 1000000.0);
        double send_bandwidth_mbit_total = ((diffBytesSentAllWorkers * 8) / 1000000.0);
        
        EnterCriticalSection(&cs_statistics_);

        // Emptying the statistics stream.
        global_statistics_stream_.str(std::string());

        // Global statistics.
        global_statistics_stream_ << "{";

        // Printing all workers stats.
        global_statistics_stream_ 
            << "\"allWorkersLastSecond\":{"
            << "\"receivedTimes\":" << diffRecvNumAllWorkers 
            << ",\"httpRequests\":" << diffProcessedHttpRequestsAllWorkers 
            << ",\"receiveBandwidth\":" << recv_bandwidth_mbit_total 
            << ",\"sentTimes\":" << diffSentNumAllWorkers 
            << ",\"sendBandwidth\":" << send_bandwidth_mbit_total
            << "}";

#ifdef GW_LOGGING_ON

        // Individual workers statistics.
        first = true;
        global_statistics_stream_ << ",\"workers\":[";
        for (int32_t worker_id_ = 0; worker_id_ < setting_num_workers_; worker_id_++)
        {
            if (!first)
                global_statistics_stream_ << ",";
            first = false;
            global_statistics_stream_ 
                << "{\"id\":" << worker_id_ 
                << ",\"receivedBytes\":" << gw_workers_[worker_id_].get_worker_stats_bytes_received() 
                << ",\"receivedTimes\":" << gw_workers_[worker_id_].get_worker_stats_recv_num() 
                << ",\"sentBytes\":" << gw_workers_[worker_id_].get_worker_stats_bytes_sent() 
                << ",\"sentTimes\":" << gw_workers_[worker_id_].get_worker_stats_sent_num() 
                << "}";
        }
        global_statistics_stream_ << "]";

        // !!!!!!!!!!!!!
        // NOTE: The following statistics can be dangerous since its not protected by global lock!
        // That's why we enable it only for tests.

        global_statistics_stream_ 
            << ",\"misc\":{"
            << "\"overflowChunks\":" << g_gateway.NumberOverflowChunksAllWorkers() 
            << ",\"activeSockets\":" << g_gateway.NumberOfActiveConnectionsOnAllPorts() 
            << "}";

        first = true;
        global_statistics_stream_ << ",\"ports\":[";
        // Printing handlers information for each attached database and gateway.
        for (int32_t p = 0; p < num_server_ports_slots_; p++)
        {
            // Checking if port is alive.
            if (!server_ports_[p].IsEmpty())
            {
                if (!first)
                    global_statistics_stream_ << ",";
                first = false;

                global_statistics_stream_ 
                    << "{\"port\":" << server_ports_[p].get_port_number() 
                    << ",\"activeConnections\":" << server_ports_[p].NumberOfActiveConnections()
                    << ",\"acceptingSockets\":" << server_ports_[p].get_num_accepting_sockets()

                    << "}";
            }
        }
        global_statistics_stream_ << "]";

#endif

        global_statistics_stream_ << "}";

        // Printing the statistics string to console.
#ifdef GW_LOG_TO_CONSOLE
        std::cout << global_statistics_stream_.str();
#endif

        LeaveCriticalSection(&cs_statistics_);

    }

    return 0;
}

// Starts gateway workers and statistics printer.
uint32_t Gateway::StartWorkerAndManagementThreads(
    LPTHREAD_START_ROUTINE workerRoutine,
    LPTHREAD_START_ROUTINE monitorDatabasesRoutine,
    LPTHREAD_START_ROUTINE channelsEventsMonitorRoutine,
    LPTHREAD_START_ROUTINE inactiveSocketsCleanupRoutine,
    LPTHREAD_START_ROUTINE gatewayLoggingRoutine,
    LPTHREAD_START_ROUTINE allThreadsMonitorRoutine)
{
    // Allocating threads-related data structures.
    uint32_t *worker_thread_ids = GwNewArray(uint32_t, setting_num_workers_);

    // Starting workers one by one.
    for (int i = 0; i < setting_num_workers_; i++)
    {
        // Creating threads.
        worker_thread_handles_[i] = CreateThread(
            NULL, // Default security attributes.
            0, // Use default stack size.
            workerRoutine, // Thread function name.
            &gw_workers_[i], // Argument to thread function.
            0, // Use default creation flags.
            (LPDWORD)&worker_thread_ids[i]); // Returns the thread identifier.

        // Checking if threads are created.
        GW_ASSERT(worker_thread_handles_[i] != NULL);
    }

    uint32_t dbScanThreadId;

    // Starting database scanning thread.
    db_monitor_thread_handle_ = CreateThread(
        NULL, // Default security attributes.
        0, // Use default stack size.
        monitorDatabasesRoutine, // Thread function name.
        NULL, // Argument to thread function.
        0, // Use default creation flags.
        (LPDWORD)&dbScanThreadId); // Returns the thread identifier.

    // Checking if thread is created.
    GW_ASSERT(db_monitor_thread_handle_ != NULL);

    uint32_t channelsEventsThreadId;

    // Starting channels events monitor thread.
    channels_events_thread_handle_ = CreateThread(
        NULL, // Default security attributes.
        0, // Use default stack size.
        channelsEventsMonitorRoutine, // Thread function name.
        NULL, // Argument to thread function.
        0, // Use default creation flags.
        (LPDWORD)&channelsEventsThreadId); // Returns the thread identifier.

    // Checking if thread is created.
    GW_ASSERT(channels_events_thread_handle_ != NULL);

    uint32_t inactiveSocketsCleanupThreadId;

    // Starting dead sockets cleanup thread.
    dead_sockets_cleanup_thread_handle_ = CreateThread(
        NULL, // Default security attributes.
        0, // Use default stack size.
        inactiveSocketsCleanupRoutine, // Thread function name.
        NULL, // Argument to thread function.
        0, // Use default creation flags.
        (LPDWORD)&inactiveSocketsCleanupThreadId); // Returns the thread identifier.

    // Checking if thread is created.
    GW_ASSERT(dead_sockets_cleanup_thread_handle_ != NULL);

    uint32_t gatewayLogRoutineThreadId;

    // Starting dead sockets cleanup thread.
    gateway_logging_thread_handle_ = CreateThread(
        NULL, // Default security attributes.
        0, // Use default stack size.
        gatewayLoggingRoutine, // Thread function name.
        NULL, // Argument to thread function.
        0, // Use default creation flags.
        (LPDWORD)&gatewayLogRoutineThreadId); // Returns the thread identifier.

    // Checking if thread is created.
    GW_ASSERT(gateway_logging_thread_handle_ != NULL);

    // Starting dead sockets cleanup thread.
    all_threads_monitor_handle_ = CreateThread(
        NULL, // Default security attributes.
        0, // Use default stack size.
        allThreadsMonitorRoutine, // Thread function name.
        NULL, // Argument to thread function.
        0, // Use default creation flags.
        (LPDWORD)&gatewayLogRoutineThreadId); // Returns the thread identifier.

    // Checking if thread is created.
    GW_ASSERT(all_threads_monitor_handle_ != NULL);

    // Printing statistics.
    uint32_t err_code = g_gateway.StatisticsAndMonitoringRoutine();

    // Close all thread handles and free memory allocations.
    for(int i = 0; i < setting_num_workers_; i++)
        CloseHandle(worker_thread_handles_[i]);

    GwDeleteArray(worker_thread_ids);
    GwDeleteArray(worker_thread_handles_);
    GwDeleteArray(gw_workers_);
    gw_workers_ = NULL;

    // Checking if any error occurred.
    GW_ERR_CHECK(err_code);

    return 0;
}

// Destructor.
Gateway::~Gateway()
{
    // Deleting only necessary stuff.
    if (gw_workers_)
    {
        GwDeleteArray(gw_workers_);
        gw_workers_ = NULL;
    }
}

int32_t Gateway::StartGateway()
{
    uint32_t err_code = 0;

    // Assert some correct state.
    err_code = AssertCorrectState();
    if (err_code)
    {
        GW_COUT << "Asserting correct state failed." << GW_ENDL;
        return err_code;
    }

    // Initialize WinSock.
    WSADATA wsaData = { 0 };
    err_code = WSAStartup(MAKEWORD(2, 2), &wsaData);
    if (err_code != 0)
    {
        GW_COUT << "WSAStartup() failed: " << err_code << GW_ENDL;
        return err_code;
    }

    // Loading configuration settings.
    err_code = LoadSettings(setting_config_file_path_);
    if (err_code)
    {
        GW_COUT << "Loading configuration settings failed." << GW_ENDL;
        return err_code;
    }

    // Creating data structures and binding sockets.
    err_code = Init();
    if (err_code)
        return err_code;

    // Initializing profilers.
    utils::Profiler::InitAll(setting_num_workers_);

    // Starting workers and statistics printer.
    err_code = StartWorkerAndManagementThreads(
        (LPTHREAD_START_ROUTINE)GatewayWorkerRoutine,
        (LPTHREAD_START_ROUTINE)MonitorDatabasesRoutine,
        (LPTHREAD_START_ROUTINE)AllDatabasesChannelsEventsMonitorRoutine,
        (LPTHREAD_START_ROUTINE)InactiveSocketsCleanupRoutine,
        (LPTHREAD_START_ROUTINE)GatewayLoggingRoutine,
        (LPTHREAD_START_ROUTINE)GatewayMonitorRoutine);

    if (err_code)
        return err_code;

    return 0;
}

// Generate the code using managed generator.
uint32_t Gateway::GenerateUriMatcher(ServerPort* server_port, RegisteredUris* port_uris)
{
    // Measuring time taken for generating matcher.
    uint64_t begin_time = timeGetTime();

    // Getting registered URIs.
    std::vector<MixedCodeConstants::RegisteredUriManaged> uris_managed = port_uris->GetRegisteredUriManaged();

    // Creating root URI matching function name.
    char root_function_name[32];
    sprintf_s(root_function_name, 32, "MatchUriForPort%d", port_uris->get_port_number());

    // Calling managed function.
    uint32_t err_code = codegen_uri_matcher_->GenerateUriMatcher(
        port_uris->get_port_number(),
        root_function_name,
        &uris_managed.front(),
        static_cast<uint32_t>(uris_managed.size()));

    // Checking that code generation always succeeds.
    GW_ASSERT(0 == err_code);

    MixedCodeConstants::MatchUriType match_uri_func;
    HMODULE gen_dll_handle;

    UriMatcherCacheEntry* new_entry = GwNewConstructor(UriMatcherCacheEntry);

    // Constructing dll name;
    std::wostringstream dll_name;
    dll_name << L"codegen_uri_matcher_" << port_uris->get_port_number();

    // Building URI matcher from generated code and loading the library.
    err_code = codegen_uri_matcher_->CompileIfNeededAndLoadDll(
        UriMatchCodegenCompilerType::COMPILER_CLANG,
        dll_name.str(),
        root_function_name,
        new_entry->GetClangEngineAddress(),
        &match_uri_func,
        &gen_dll_handle);

    // Checking that code generation always succeeds.
    GW_ASSERT(0 == err_code);

    // Printing how much time it took for generating the matcher.
    std::cout << "Total codegen time (" << uris_managed.size() << ", " << port_uris->get_port_number() << "): " << timeGetTime() - begin_time << " ms." << std::endl;

    // Setting the entry point for new URI matcher.
    new_entry->Init(match_uri_func, gen_dll_handle, port_uris->GetSortedString(), port_uris->get_num_uris());

    // Setting generated URI matcher.
    port_uris->SetGeneratedUriMatcher(new_entry);

    // Unloading existing matcher DLL if any.
    server_port->RemoveOldestCacheEntry(new_entry);

    return 0;
}

// Adds some URI handler: either Apps or Gateway.
uint32_t Gateway::AddUriHandler(
    GatewayWorker* gw,
    uint16_t port_num,
    const char* app_name_string,
    const char* original_uri_info,
    const char* processed_uri_info,
    uint8_t* param_types,
    int32_t num_params,
    BMX_HANDLER_TYPE handler_info,
    db_index_type db_index,
    GENERIC_HANDLER_CALLBACK handler_proc,
    bool is_gateway_handler,
    ReverseProxyInfo* reverse_proxy_info)
{
    uint32_t err_code;

    // Getting the port structure.
    ServerPort* server_port = g_gateway.FindServerPort(port_num);

    // Checking if port exists.
    if ((NULL == server_port) || (server_port->get_port_handlers()->IsEmpty()))
    {
        // Registering handler on active database.
        err_code = g_gateway.AddPortHandler(
            gw,
            port_num,
            app_name_string,
            handler_info,
            0,
            OuterUriProcessData);

        if (err_code)
            return err_code;

        server_port = g_gateway.FindServerPort(port_num);

        GW_ASSERT(NULL != server_port);
    }

    RegisteredUris* port_uris = server_port->get_registered_uris();

    // Searching for existing URI handler on this port.
    uri_index_type uri_index = port_uris->FindRegisteredUri(processed_uri_info);

    // Checking if there is an entry.
    if (INVALID_URI_INDEX == uri_index)
    {
        // Checking if there is a session in parameters.
        uint8_t session_param_index = INVALID_PARAMETER_INDEX;
        for (int32_t i = 0; i < num_params; i++)
        {
            // Checking for REST_ARG_SESSION.
            if (MixedCodeConstants::REST_ARG_SESSION == param_types[i])
            {
                session_param_index = i;
                break;
            }
        }

        // Registering URI handler.
        HandlersList* hl = GwNewConstructor(HandlersList);

        err_code = hl->Init(
            bmx::HANDLER_TYPE::URI_HANDLER,
            handler_info,
            port_num,
            app_name_string,
            0,
            original_uri_info,
            processed_uri_info,
            param_types,
            num_params,
            db_index,
            0,
            reverse_proxy_info
            );

        GW_ASSERT(0 == err_code);

        // Adding the actual handler procedure.
        hl->AddHandler(handler_proc);

        // Creating totally new URI entry.
        RegisteredUri new_entry(
            session_param_index,
            db_index,
            hl,
            is_gateway_handler);

        // Adding entry to global list.
        port_uris->AddNewUri(new_entry);
    }
    else
    {
        wchar_t temp[MixedCodeConstants::MAX_URI_STRING_LEN];
        wsprintf(temp, L"Attempt to register URI handler duplicate on port \"%d\" and URI \"%S\".", port_num, processed_uri_info);
        g_gateway.LogWriteError(temp);

        // Disallowing handler duplicates.
        return SCERRHANDLERALREADYREGISTERED;

        /*
        // Obtaining existing URI entry.
        RegisteredUri* reg_uri = port_uris->GetEntryByIndex(uri_index);

        // Checking if there is no database for this URI.
        if (!reg_uri->ContainsDb(db_index))
        {
            // Adding new handler list for this database to the URI.
            reg_uri->Add(handlers_table->get_handler_list(handler_index));
        }
        */
    }

    if (err_code)
        return err_code;

    return 0;
}

// Adds some port handler: either Apps or Gateway.
uint32_t Gateway::AddPortHandler(
    GatewayWorker* gw,
    uint16_t port_num,
    const char* app_name_string,
    BMX_HANDLER_TYPE handler_info,
    db_index_type db_index,
    GENERIC_HANDLER_CALLBACK handler_proc)
{
    uint32_t err_code = 0;

    // Checking if there is a corresponding server port created.
    ServerPort* server_port = g_gateway.FindServerPort(port_num);

    // Checking if there are no handlers.
    if (NULL != server_port) {

        GW_ASSERT(true == server_port->get_port_handlers()->IsEmpty());

    } else {

        SOCKET listening_sock = INVALID_SOCKET;

        // Creating socket and binding to port for all workers.
        err_code = g_gateway.CreateListeningSocketAndBindToPort(gw, port_num, listening_sock);
        if (err_code)
            return err_code;

        // Adding new server port.
        server_port = g_gateway.AddNewServerPort(port_num, listening_sock);

        // Checking if its an aggregation port.
        if (port_num == g_gateway.setting_aggregation_port())
            server_port->set_aggregating_flag();

        // Checking if we need to extend number of accepting sockets.
        if (server_port->get_num_accepting_sockets() < ACCEPT_ROOF_STEP_SIZE)
        {
            // Creating new connections if needed for this database.
            err_code = g_gateway.get_worker(0)->CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, server_port->get_port_index());
            if (err_code)
                return err_code;
        }
    }

    // Registering URI handler.
    HandlersList* hl = GwNewConstructor(HandlersList);

    err_code = hl->Init(
        bmx::HANDLER_TYPE::PORT_HANDLER,
        handler_info,
        port_num,
        app_name_string,
        0,
        NULL,
        NULL,
        NULL,
        0,
        db_index,
        0,
        NULL
        );

    GW_ASSERT(0 == err_code);

    // Adding the actual handler procedure.
    hl->AddHandler(handler_proc);

    // Adding port handler if does not exist.
    server_port->get_port_handlers()->Add(db_index, hl);

    return 0;
}

extern "C" int32_t make_sc_process_uri(const char *server_name, const char *process_name, wchar_t *buffer, size_t *pbuffer_size);
extern "C" int32_t make_sc_server_uri(const char *server_name, wchar_t *buffer, size_t *pbuffer_size);

// Opens Starcounter log for writing.
uint32_t Gateway::OpenStarcounterLog()
{
	size_t host_name_size;
	wchar_t *host_name;
	uint32_t err_code = 0;

	host_name_size = 0;
//	make_sc_server_uri(setting_sc_server_type_.c_str(), 0, &host_name_size);
	make_sc_process_uri(setting_sc_server_type_upper_.c_str(), GW_PROCESS_NAME, 0, &host_name_size);
	host_name = GwNewArray(wchar_t, host_name_size);
	if (host_name)
	{
//		make_sc_server_uri(setting_sc_server_type_.c_str(), host_name, &host_name_size);
		make_sc_process_uri(setting_sc_server_type_upper_.c_str(), GW_PROCESS_NAME, host_name, &host_name_size);

		err_code = sccorelog_init(0);
		if (err_code) goto err;

		err_code = sccorelog_connect_to_logs(host_name, setting_server_output_dir_.c_str(), NULL, &sc_log_handle_);
		if (err_code) goto err;

		goto end;
	}
	
	err_code = SCERROUTOFMEMORY;
err:
end:
	if (host_name) free(host_name);
	return err_code;
}

// Closes Starcounter log.
void Gateway::CloseStarcounterLog()
{
    uint32_t err_code = sccorelog_release_logs(sc_log_handle_);

    GW_ASSERT(0 == err_code);
}

void Gateway::LogWriteCritical(const wchar_t* msg)
{
    LogWriteGeneral(msg, SC_ENTRY_CRITICAL);
}

void Gateway::LogWriteError(const wchar_t* msg)
{
    LogWriteGeneral(msg, SC_ENTRY_ERROR);
}

void Gateway::LogWriteWarning(const wchar_t* msg)
{
    LogWriteGeneral(msg, SC_ENTRY_WARNING);
}

void Gateway::LogWriteNotice(const wchar_t* msg)
{
    LogWriteGeneral(msg, SC_ENTRY_NOTICE);
}

void Gateway::LogWriteGeneral(const wchar_t* msg, uint32_t log_type)
{
	// NOTE:
	// No asserts in critical log handler. Assertion fails calls critical log
	// handler to log.

    uint32_t err_code = 0;
	
	if (msg)
	{
	    err_code = sccorelog_kernel_write_to_logs(sc_log_handle_, log_type, 0, msg);
	
	    //GW_ASSERT(0 == err_code);
	}

    err_code = sccorelog_flush_to_logs(sc_log_handle_);

    //GW_ASSERT(0 == err_code);
}

} // namespace network
} // namespace starcounter

VOID LogGatewayCrash(VOID *pc, LPCWSTR str)
{
    starcounter::network::g_gateway.LogWriteCritical(str);
}

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
{
    // Catching all unhandled exceptions in this thread.
    GW_SC_BEGIN_FUNC

    // Setting the critical log handler.
    set_critical_log_handler(LogGatewayCrash, NULL);

    using namespace starcounter::network;
    uint32_t err_code = 0;

    // Processing arguments and initializing log file.
    err_code = g_gateway.ProcessArgumentsAndInitLog(argc, argv);
    if (err_code)
        return err_code;

    // Stating the network gateway.
    err_code = g_gateway.StartGateway();
    if (err_code)
    {
        wchar_t temp[256];
        wsprintf(temp, L"Gateway exited with error code: %d", err_code);
        g_gateway.LogWriteError(temp);

        return err_code;
    }

    // Cleaning up resources.
    err_code = g_gateway.GlobalCleanup();
    if (err_code)
        return err_code;

    //GW_COUT << "Press any key to exit." << GW_ENDL;
    //_getch();

    return 0;

    // Catching all unhandled exceptions in this thread.
    GW_SC_END_FUNC
}