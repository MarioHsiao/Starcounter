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
    g_gateway.get_gw_log_writer()->WriteToLog(str.c_str(), str.length());

#endif
}

void GatewayLogWriter::Init(std::wstring& log_file_path)
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
    setting_max_connections_ = 0;

    // Default inactive session timeout in seconds.
    setting_inactive_session_timeout_seconds_ = 60 * 20;

    // Starcounter server type.
    setting_sc_server_type_upper_ = MixedCodeConstants::DefaultPersonalServerNameUpper;

    // All worker structures.
    gw_workers_ = NULL;

    // Worker thread handles.
    worker_thread_handles_ = NULL;

    // IOCP handle.
    iocp_ = INVALID_HANDLE_VALUE;

    // Number of active databases.
    num_dbs_slots_ = 0;

    // Init unique sequence number.
    db_seq_num_ = 0;

    // No reverse proxies by default.
    num_reversed_proxies_ = 0;

    // Starting linear unique socket with 0.
    unique_socket_id_ = 0;

    // Server address for testing.
    server_addr_ = NULL;

    // Initializing critical sections.
    InitializeCriticalSection(&cs_session_);
    InitializeCriticalSection(&cs_global_lock_);
    InitializeCriticalSection(&cs_sessions_cleanup_);
    InitializeCriticalSection(&cs_statistics_);

    // Creating gateway handlers table.
    gw_handlers_ = new HandlersTable();

    // Initial number of server ports.
    num_server_ports_unsafe_ = 0;

    // Resetting number of processed HTTP requests.
    num_processed_http_requests_unsafe_ = 0;

    // Initializing to first bind port number.
    last_bind_port_num_unsafe_ = FIRST_BIND_PORT_NUM;

    // First bind interface number.
    last_bind_interface_num_unsafe_ = 0;

    // Resetting Starcounter log handle.
    sc_log_handle_ = INVALID_LOG_HANDLE;

    // Empty global statistics.
    global_statistics_stream_ << "Empty string!";
    memcpy(global_statistics_string_, kHttpStatsHeader, kHttpStatsHeaderLength);

    codegen_uri_matcher_ = NULL;

#ifdef GW_TESTING_MODE

    // Master IP address.
    setting_master_ip_ = "";

    // Indicates if this node is a master node.
    setting_is_master_ = true;

    // Number of connections to establish to master.
    setting_num_connections_to_master_ = 0;
    setting_num_connections_to_master_per_worker_ = 0;

    // Number of echoes to send to master node from clients.
    setting_num_echoes_to_master_ = 0;

    // Setting operational mode.
    setting_mode_ = GatewayTestingMode::MODE_GATEWAY_SMC_HTTP;

    // Number of operations per second.
    num_gateway_ops_per_second_ = 0;

    // Number of measures.
    num_ops_measures_ = 0;

    cmd_setting_num_workers_ = 0;
    cmd_setting_mode_ = MODE_GATEWAY_UNKNOWN;
    cmd_setting_num_connections_to_master_ = 0;
    cmd_setting_num_echoes_to_master_ = 0;
    cmd_setting_max_test_time_seconds_ = 0;

#endif    
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

        return SCERRGWWRONGARGS;
    }

    // Reading the Starcounter log directory.
    setting_server_output_dir_ = argv[3];

    char temp[128];

#ifdef GW_TESTING_MODE
    if (argc > 4)
    {
        GW_ASSERT(argc == 10);

        cmd_setting_num_workers_ = _wtoi(argv[4]);
        
        wcstombs(temp, argv[5], 128);
        cmd_setting_mode_ = GetGatewayTestingMode(temp);
        cmd_setting_num_connections_to_master_ = _wtoi(argv[6]);
        cmd_setting_num_echoes_to_master_ = _wtoi(argv[7]);
        cmd_setting_max_test_time_seconds_ = _wtoi(argv[8]);

        wcstombs(temp, argv[9], 128);
        cmd_setting_stats_name_ = std::string(temp);
    }
#endif

    // Opening Starcounter log.
    uint32_t err_code = g_gateway.OpenStarcounterLog();
    if (err_code)
        return err_code;

    // Converting Starcounter server type to narrow char.
    wcstombs(temp, argv[1], 128);

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

    // Specifying gateway output directory as subfolder for server output.
    setting_gateway_output_dir_ = setting_server_output_dir_ + L"\\" + GW_PROGRAM_NAME;

    // Trying to create network gateway log directory.
    if ((!CreateDirectory(setting_gateway_output_dir_.c_str(), NULL)) &&
        (ERROR_ALREADY_EXISTS != GetLastError()))
    {
        std::wcout << L"Can't create network gateway log directory: " << setting_gateway_output_dir_ << std::endl;

        return SCERRGWCANTCREATELOGDIR;
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
void ServerPort::Init(int32_t port_index, uint16_t port_number, SOCKET listening_sock, int32_t blob_user_data_offset)
{
    GW_ASSERT((port_index >= 0) && (port_index < MAX_PORTS_NUM));

    // Allocating needed tables.
    port_handlers_ = new PortHandlers();
    registered_uris_ = new RegisteredUris(port_number);
    registered_subports_ = new RegisteredSubports();

    listening_sock_ = listening_sock;
    port_number_ = port_number;
    blob_user_data_offset_ = blob_user_data_offset;
    port_handlers_->set_port_number(port_number_);
    port_index_ = port_index;
}

// Resets the number of created sockets and active connections.
void ServerPort::Reset()
{
    InterlockedAnd64(&(num_accepting_sockets_unsafe_), 0);

#ifdef GW_COLLECT_SOCKET_STATISTICS

    for (int32_t w = 0; w < g_gateway.setting_num_workers(); w++)
    {
        g_gateway.get_worker(w)->SetNumActiveConnections(port_index_, 0);
    }

    InterlockedAnd64(&num_allocated_accept_sockets_unsafe_, 0);
    InterlockedAnd64(&num_allocated_connect_sockets_unsafe_, 0);

#endif
}

// Removes this port.
void ServerPort::EraseDb(int32_t db_index)
{
    // Deleting port handlers if any.
    port_handlers_->RemoveEntry(db_index);

    // Deleting URI handlers if any.
    registered_uris_->RemoveEntry(db_index);

    // Deleting subport handlers if any.
    registered_subports_->RemoveEntry(db_index);
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

    // Checking subports.
    if (registered_subports_ && (!registered_subports_->IsEmpty()))
        return false;

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Checking connections.
    if (NumberOfActiveConnections())
        return false;

#endif

    return true;
}

#ifdef GW_COLLECT_SOCKET_STATISTICS

// Retrieves the number of active connections.
int64_t ServerPort::NumberOfActiveConnections()
{
    return g_gateway.NumberOfActiveConnectionsPerPort(port_index_);
}

#endif

// Removes this port.
void ServerPort::Erase()
{
    // Closing socket which will results in Disconnect.
    if (INVALID_SOCKET != listening_sock_)
    {
        if (closesocket(listening_sock_))
        {
#ifdef GW_ERRORS_DIAG
            GW_COUT << "closesocket() failed." << GW_ENDL;
#endif
            PrintLastError();
        }
        listening_sock_ = INVALID_SOCKET;
    }

    if (port_handlers_)
    {
        delete port_handlers_;
        port_handlers_ = NULL;
    }

    if (registered_uris_)
    {
        delete registered_uris_;
        registered_uris_ = NULL;
    }

    if (registered_subports_)
    {
        delete registered_subports_;
        registered_subports_ = NULL;
    }

    port_number_ = INVALID_PORT_NUMBER;
    blob_user_data_offset_ = -1;
    port_index_ = INVALID_PORT_INDEX;

    Reset();
}

// Printing the registered URIs.
void ServerPort::Print()
{
    port_handlers_->Print();
    registered_uris_->Print(port_number_);
}

ServerPort::ServerPort()
{
    listening_sock_ = INVALID_SOCKET;
    port_handlers_ = NULL;
    registered_uris_ = NULL;
    registered_subports_ = NULL;

    Erase();
}

ServerPort::~ServerPort()
{
}

// Loads configuration settings from provided XML file.
uint32_t Gateway::LoadSettings(std::wstring configFilePath)
{
    uint32_t err_code;

    // Opening file stream.
    std::ifstream config_file_stream(configFilePath);
    if (!config_file_stream.is_open())
        return SCERRGWCANTLOADXMLSETTINGS;

    // Copying config contents into a string.
    std::stringstream str_stream;
    str_stream << config_file_stream.rdbuf();
    std::string tmp_str = str_stream.str();
    char* config_contents = new char[tmp_str.size() + 1];
    strcpy_s(config_contents, tmp_str.size() + 1, tmp_str.c_str());

    using namespace rapidxml;
    xml_document<> doc; // Character type defaults to char.
    doc.parse<0>(config_contents); // 0 means default parse flags.

    xml_node<> *rootElem = doc.first_node("NetworkGateway");

    // Getting local interfaces.
    xml_node<> *localIpElem = rootElem->first_node("BindingIP");
    while(localIpElem)
    {
        setting_local_interfaces_.push_back(localIpElem->value());
        localIpElem = localIpElem->next_sibling("BindingIP");
    }

    // Getting workers number.
    setting_num_workers_ = atoi(rootElem->first_node("WorkersNumber")->value());
#ifdef GW_TESTING_MODE
    if (cmd_setting_num_workers_)
        setting_num_workers_ = cmd_setting_num_workers_;
#endif

    // Getting maximum connection number.
    setting_max_connections_ = atoi(rootElem->first_node("MaxConnections")->value());

    // Getting inactive session timeout.
    setting_inactive_session_timeout_seconds_ = atoi(rootElem->first_node("InactiveSessionTimeout")->value());

    // Getting gateway statistics port.
    setting_gw_stats_port_ = (uint16_t)atoi(rootElem->first_node("GatewayStatisticsPort")->value());

    // Just enforcing minimum session timeout multiplier.
    if ((setting_inactive_session_timeout_seconds_ % SESSION_LIFETIME_MULTIPLIER) != 0)
        return SCERRGWWRONGMAXIDLESESSIONLIFETIME;

    // Setting minimum session time.
    min_inactive_session_life_seconds_ = setting_inactive_session_timeout_seconds_ / SESSION_LIFETIME_MULTIPLIER;

    // Initializing global timer.
    global_timer_unsafe_ = min_inactive_session_life_seconds_;

#ifdef GW_TESTING_MODE

    // Getting master node IP address.
    setting_master_ip_ = rootElem->first_node("MasterIP")->value();

    // Master node does not need its own IP.
    if (setting_master_ip_ == "")
        setting_is_master_ = true;
    else
        setting_is_master_ = false;

    // Number of connections to establish to master.
    setting_num_connections_to_master_ = atoi(rootElem->first_node("NumConnectionsToMaster")->value());
    if (cmd_setting_num_connections_to_master_)
        setting_num_connections_to_master_ = cmd_setting_num_connections_to_master_;

    GW_ASSERT((setting_num_connections_to_master_ % (setting_num_workers_ * ACCEPT_ROOF_STEP_SIZE)) == 0);
    setting_num_connections_to_master_per_worker_ = setting_num_connections_to_master_ / setting_num_workers_;

    // Number of echoes to send to master node from clients.
    setting_num_echoes_to_master_ = atoi(rootElem->first_node("NumEchoesToMaster")->value());
    if (cmd_setting_num_echoes_to_master_)
        setting_num_echoes_to_master_ = cmd_setting_num_echoes_to_master_;

    GW_ASSERT(setting_num_echoes_to_master_ <= MAX_TEST_ECHOES);
    ResetEchoTests();

    // Obtaining testing mode.
    setting_mode_ = GetGatewayTestingMode(rootElem->first_node("TestingMode")->value());
    if (cmd_setting_mode_ != GatewayTestingMode::MODE_GATEWAY_UNKNOWN)
        setting_mode_ = cmd_setting_mode_;

    // Maximum running time for tests.
    setting_max_test_time_seconds_ = atoi(rootElem->first_node("MaxTestTimeSeconds")->value());
    if (cmd_setting_max_test_time_seconds_)
        setting_max_test_time_seconds_ = cmd_setting_max_test_time_seconds_;

    GW_ASSERT((setting_max_test_time_seconds_ % GW_MONITOR_THREAD_TIMEOUT_SECONDS) == 0);

    // Loading statistics name.
    setting_stats_name_ = rootElem->first_node("ReportStatisticsName")->value();
    if (cmd_setting_stats_name_.length())
        setting_stats_name_ = cmd_setting_stats_name_;

#ifdef GW_LOOPED_TEST_MODE
    switch (setting_mode_)
    {
        case GatewayTestingMode::MODE_GATEWAY_HTTP:
        case GatewayTestingMode::MODE_GATEWAY_SMC_HTTP:
        case GatewayTestingMode::MODE_GATEWAY_SMC_APPS_HTTP:
        {
            looped_echo_request_creator_ = DefaultHttpEchoRequestCreator;
            looped_echo_response_processor_ = DefaultHttpEchoResponseProcessor;
            
            break;
        }
        
        case GatewayTestingMode::MODE_GATEWAY_RAW:
        case GatewayTestingMode::MODE_GATEWAY_SMC_RAW:
        case GatewayTestingMode::MODE_GATEWAY_SMC_APPS_RAW:
        {
            looped_echo_request_creator_ = DefaultRawEchoRequestCreator;
            looped_echo_response_processor_ = DefaultRawEchoResponseProcessor;

            break;
        }
    }
#endif

#endif

#ifdef GW_PROXY_MODE

    // Checking if we have reverse proxies.
    xml_node<char>* proxies_node = rootElem->first_node("ReverseProxies");
    if (proxies_node)
    {
        xml_node<char>* proxy_node = proxies_node->first_node("ReverseProxy");
        int32_t n = 0;
        while (proxy_node)
        {
            // Filling reverse proxy information.
            reverse_proxies_[n].server_ip_ = proxy_node->first_node("ServerIP")->value();
            reverse_proxies_[n].server_port_ = atoi(proxy_node->first_node("ServerPort")->value());
            reverse_proxies_[n].gw_proxy_port_ = atoi(proxy_node->first_node("GatewayProxyPort")->value());
            reverse_proxies_[n].service_uri_ = proxy_node->first_node("ServiceUri")->value();
            reverse_proxies_[n].service_uri_len_ = reverse_proxies_[n].service_uri_.length();

            // Loading proxied servers.
            sockaddr_in* server_addr = &reverse_proxies_[n].addr_;
            memset(server_addr, 0, sizeof(sockaddr_in));
            server_addr->sin_family = AF_INET;
            server_addr->sin_addr.s_addr = inet_addr(reverse_proxies_[n].server_ip_.c_str());
            server_addr->sin_port = htons(reverse_proxies_[n].server_port_);

            // Getting next reverse proxy information.
            proxy_node = proxy_node->next_sibling("ReverseProxy");

            n++;
        }

        num_reversed_proxies_ = n;
    }

#endif

    // Predefined ports constants.
    PortType portTypes[NUM_PREDEFINED_PORT_TYPES] = { HTTP_PORT, HTTPS_PORT, WEBSOCKETS_PORT, GENSOCKETS_PORT, AGGREGATION_PORT };
    std::string portNames[NUM_PREDEFINED_PORT_TYPES] = { "HttpPort", "HttpsPort", "WebSocketsPort", "GenSocketsPort", "AggregationPort" };
    GENERIC_HANDLER_CALLBACK portHandlerTypes[NUM_PREDEFINED_PORT_TYPES] = { AppsUriProcessData, HttpsProcessData, AppsUriProcessData, AppsPortProcessData, AppsPortProcessData };
    uint16_t portNumbers[NUM_PREDEFINED_PORT_TYPES] = { 80, 443, 80, 123, 12345 };
    uint32_t userDataOffsetsInBlob[NUM_PREDEFINED_PORT_TYPES] = { HTTP_BLOB_USER_DATA_OFFSET, HTTPS_BLOB_USER_DATA_OFFSET, WS_BLOB_USER_DATA_OFFSET, RAW_BLOB_USER_DATA_OFFSET, AGGR_BLOB_USER_DATA_OFFSET };

    // Going through all ports.
    for (int32_t i = 0; i < NUM_PREDEFINED_PORT_TYPES; i++)
    {
        portNumbers[i] = atoi(rootElem->first_node(portNames[i].c_str())->value());
        if (portNumbers[i] <= 0)
            continue;

        GW_COUT << portNames[i] << ": " << portNumbers[i] << GW_ENDL;

        // Checking if several protocols are on the same port.
        int32_t samePortIndex = INVALID_PORT_INDEX;
        for (int32_t k = 0; k < i; k++)
        {
            if ((portNumbers[i] > 0) && (portNumbers[i] == portNumbers[k]))
            {
                samePortIndex = k;
                break;
            }
        }

        // Creating a new port entry.
        err_code = g_gateway.get_gw_handlers()->RegisterPortHandler(NULL, portNumbers[i], 0, portHandlerTypes[i], -1);
        GW_ERR_CHECK(err_code);
    }

    // Allocating data for worker sessions.
    all_sessions_unsafe_ = (ScSessionStructPlus*)_aligned_malloc(sizeof(ScSessionStructPlus) * setting_max_connections_, 64);
    free_session_indexes_unsafe_ = new session_index_type[setting_max_connections_];
    sessions_to_cleanup_unsafe_ = new session_index_type[setting_max_connections_];
    num_active_sessions_unsafe_ = 0;
    num_sessions_to_cleanup_unsafe_ = 0;

    // Cleaning all sessions and free session indexes.
    for (int32_t i = 0; i < setting_max_connections_; i++)
    {
        // Filling up indexes linearly.
        free_session_indexes_unsafe_[i] = i;

        // Resetting all sessions.
        all_sessions_unsafe_[i].session_.Reset();

        // Resetting sessions time stamps.
        all_sessions_unsafe_[i].session_timestamp_ = 0;

        // Resetting active sockets flags.
        all_sessions_unsafe_[i].active_socket_flag_ = false;
    }

    delete [] config_contents;

    return 0;
}

// Assert some correct state parameters.
uint32_t Gateway::AssertCorrectState()
{
    SocketDataChunk* test_sdc = new SocketDataChunk();
    uint32_t err_code;

    // Checking correct socket data.
    err_code = test_sdc->AssertCorrectState();
    if (err_code)
        goto FAILED;

    GW_ASSERT(core::chunk_type::static_header_size == bmx::BMX_HEADER_MAX_SIZE_BYTES);

    // Checking overall gateway stuff.
    GW_ASSERT(sizeof(ScSessionStruct) == MixedCodeConstants::SESSION_STRUCT_SIZE);

    GW_ASSERT(sizeof(ScSessionStructPlus) == 64);

    // Checking HTTP related fields.
    GW_ASSERT(kFullSessionIdStringLength == (SC_SESSION_STRING_LEN_CHARS + kScSessionIdStringWithExtraCharsLength));
    GW_ASSERT(kSetCookieStringPrefixLength == 20);
    GW_ASSERT(kFullSessionIdSetCookieStringLength == 46);

    int64_t accept_8bytes = *(int64_t*)"Accept: ";
    int64_t accept_enc_8bytes = *(int64_t*)"Accept-E";
    int64_t cookie_8bytes = *(int64_t*)"Cookie: ";
    int64_t set_cookie_8bytes = *(int64_t*)"Set-Cook";
    int64_t content_len_8bytes = *(int64_t*)"Content-Length";
    int64_t upgrade_8bytes = *(int64_t*)"Upgrade:";
    int64_t websocket_8bytes = *(int64_t*)"Sec-WebSocket";
    int64_t scsessionid_8bytes = *(int64_t*)kScSessionIdStringWithExtraChars;

    GW_ASSERT(ACCEPT_HEADER_VALUE_8BYTES == accept_8bytes);
    GW_ASSERT(ACCEPT_ENCODING_HEADER_VALUE_8BYTES == accept_enc_8bytes);
    GW_ASSERT(COOKIE_HEADER_VALUE_8BYTES == cookie_8bytes);
    GW_ASSERT(SET_COOKIE_HEADER_VALUE_8BYTES == set_cookie_8bytes);
    GW_ASSERT(CONTENT_LENGTH_HEADER_VALUE_8BYTES == content_len_8bytes);
    GW_ASSERT(UPGRADE_HEADER_VALUE_8BYTES == upgrade_8bytes);
    GW_ASSERT(WEBSOCKET_HEADER_VALUE_8BYTES == websocket_8bytes);
    GW_ASSERT(SCSESSIONID_HEADER_VALUE_8BYTES == scsessionid_8bytes);

    return 0;

FAILED:
    delete test_sdc;

    return SCERRGWFAILEDASSERTCORRECTSTATE;
}

// Creates socket and binds it to server port.
uint32_t Gateway::CreateListeningSocketAndBindToPort(GatewayWorker *gw, uint16_t port_num, SOCKET& sock)
{
    // Creating socket.
    sock = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (sock == INVALID_SOCKET)
    {
        GW_COUT << "WSASocket() failed." << GW_ENDL;
        return PrintLastError();
    }

    // Getting IOCP handle.
    HANDLE iocp = NULL;
    if (!gw)
        iocp = g_gateway.get_iocp();
    else
        iocp = gw->get_worker_iocp();

    // Attaching socket to IOCP.
    HANDLE temp = CreateIoCompletionPort((HANDLE) sock, iocp, 0, setting_num_workers_);
    if (temp != iocp)
    {
        PrintLastError(true);
        closesocket(sock);

        GW_COUT << "Wrong IOCP returned when adding reference." << GW_ENDL;
        return SCERRGWFAILEDTOATTACHSOCKETTOIOCP;
    }

    // Skipping completion port if operation is already successful.
    SetFileCompletionNotificationModes((HANDLE) sock, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);

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
       
        GW_LOG_ERROR << L"Failed to bind server port: " << port_num << L". Please check that port is not occupied by any software." << GW_WENDL;
        return SCERRGWFAILEDTOBINDPORT;
    }

    // Listening to connections.
    if (listen(sock, SOMAXCONN))
    {
        PrintLastError(true);
        closesocket(sock);

        GW_COUT << "Error listening on server socket." << GW_ENDL;
        return SCERRGWFAILEDTOLISTENONSOCKET;
    }

    return 0;
}

// Creates new connections on all workers.
uint32_t Gateway::CreateNewConnectionsAllWorkers(int32_t how_many, uint16_t port_num, int32_t db_index)
{
    // Getting server port index.
    int32_t port_index = g_gateway.FindServerPortIndex(port_num);

    // Creating new connections for each worker.
    for (int32_t i = 0; i < setting_num_workers_; i++)
    {
        uint32_t errCode = gw_workers_[i].CreateNewConnections(how_many, port_index, db_index);
        GW_ERR_CHECK(errCode);
    }

    return 0;
}

// Stub APC function that does nothing.
void __stdcall EmptyApcFunction(ULONG_PTR arg) {
    // Does nothing.
}

// Database channels events monitor thread.
uint32_t __stdcall DatabaseChannelsEventsMonitorRoutine(LPVOID params)
{
    // Index of the database as parameter.
    int32_t db_index = *(int32_t*)params;

    // Determine the worker by interface or channel,
    // and wake up that worker.
	HANDLE worker_thread_handle[MAX_WORKER_THREADS];
	HANDLE work_events[MAX_WORKER_THREADS];
	std::size_t number_of_work_events = 0;
	core::shared_interface* db_shared_int = 0;
	std::size_t work_event_index = 0;
	
    // Setting checking events for each worker.
	for (std::size_t worker_id = 0; worker_id < g_gateway.setting_num_workers(); ++worker_id)
    {
		WorkerDbInterface* db_int = g_gateway.get_worker(worker_id)->GetWorkerDb(db_index);
		db_shared_int = db_int->get_shared_int();

		work_events[worker_id] = db_shared_int->open_client_work_event(db_shared_int->get_client_number());
		++number_of_work_events;

		// Waiting for all channels.
		// TODO: Unset notify flag when worker thread is spinning.
		db_shared_int->client_interface().set_notify_flag(true);

		// Sending APC on the determined worker.
		worker_thread_handle[worker_id] = g_gateway.get_worker_thread_handle(worker_id);
        
		// Waking up the worker thread with APC.
		QueueUserAPC(EmptyApcFunction, worker_thread_handle[worker_id], 0);
	}
	
    // Looping until the database dies (TODO, does not work, forcedly killed).
    while (!g_gateway.GetDatabase(db_index)->IsEmpty())
    {
        // Waiting forever for more events on channels.
        db_shared_int->client_interface().wait_for_work(work_event_index, work_events, number_of_work_events);

		// Waking up the worker thread with APC.
		QueueUserAPC(EmptyApcFunction, worker_thread_handle[work_event_index], 0);
    }

    return 0;
}

// Checks for new/existing databases and updates corresponding shared memory structures.
uint32_t Gateway::CheckDatabaseChanges(const std::set<std::string>& active_databases)
{
    int32_t server_name_len = setting_sc_server_type_upper_.length();

    // Enabling database down tracking flag.
    for (int32_t i = 0; i < num_dbs_slots_; i++)
        db_did_go_down_[i] = true;

    // Reading file line by line.
    uint32_t err_code;
    
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

        bool is_new_database = true;
        for (int32_t i = 0; i < num_dbs_slots_; i++)
        {
            // Checking if it is an existing database.
            if (0 == active_databases_[i].db_name().compare(current_db_name))
            {
                // Existing database is up.
                db_did_go_down_[i] = false;
                is_new_database = false;

                // Creating new shared memory interface.
                break;
            }
        }

#ifdef GW_TESTING_MODE

        // Checking certain database names.
        if (g_gateway.setting_is_master())
        {
            if (current_db_name == "MYDB_CLIENT")
            {
                GW_COUT << "Ignoring database '" << current_db_name << "' since we are in server mode." << GW_ENDL;
                is_new_database = false;
            }
        }
        else
        {
            if (current_db_name == "MYDB_SERVER")
            {
                GW_COUT << "Ignoring database '" << current_db_name << "' since we are in client mode." << GW_ENDL;
                is_new_database = false;
            }
        }
#endif

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

            // Workers shared interface instances.
            core::shared_interface *workers_shared_ints =
                new core::shared_interface[setting_num_workers_];
            
            // Adding to the databases list.
            err_code = InitSharedMemory(current_db_name, workers_shared_ints);
            if (err_code != 0)
            {
                // Leaving global lock.
                LeaveGlobalLock();
                return err_code;
            }

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
            for (int32_t i = 0; i < setting_num_workers_; i++)
            {
                err_code = gw_workers_[i].AddNewDatabase(empty_db_index, workers_shared_ints[i]);
                if (err_code)
                {
                    // Leaving global lock.
                    LeaveGlobalLock();
                    return err_code;
                }
            }

            // Spawning channels events monitor.
            err_code = active_databases_[empty_db_index].SpawnChannelsEventsMonitor();
            if (err_code)
            {
                // Leaving global lock.
                LeaveGlobalLock();
                return err_code;
            }

#ifdef GW_TESTING_MODE

            uint16_t port_number = GATEWAY_TEST_PORT_NUMBER_SERVER;
            if (!g_gateway.setting_is_master())
                port_number++;

            // Checking if we are in Gateway HTTP mode.
            switch (g_gateway.setting_mode())
            {
                // Registering pure gateway handler here.
                case GatewayTestingMode::MODE_GATEWAY_RAW:
                {
                    // Registering port handler.
                    err_code = AddPortHandler(
                        &gw_workers_[0],
                        gw_handlers_,
                        port_number,
                        bmx::BMX_INVALID_HANDLER_INFO,
                        empty_db_index,
                        GatewayPortProcessEcho);

                    if (err_code)
                    {
                        // Leaving global lock.
                        LeaveGlobalLock();

                        return err_code;
                    }

                    break;
                }

                // Registering pure gateway handler here.
                case GatewayTestingMode::MODE_GATEWAY_HTTP:
                {
                    // Registering URI handlers.
                    err_code = AddUriHandler(
                        &gw_workers_[0],
                        gw_handlers_,
                        port_number,
                        http_tests_information_[g_gateway.setting_mode()].method_and_uri_info,
                        http_tests_information_[g_gateway.setting_mode()].method_and_uri_info_len,
                        http_tests_information_[g_gateway.setting_mode()].method_and_uri_info,
                        http_tests_information_[g_gateway.setting_mode()].method_and_uri_info_len,
                        bmx::HTTP_METHODS::OTHER_METHOD,
                        NULL,
                        0,
                        bmx::BMX_INVALID_HANDLER_INFO,
                        empty_db_index,
                        GatewayUriProcessEcho);

                    if (err_code)
                    {
                        // Leaving global lock.
                        LeaveGlobalLock();

                        return err_code;
                    }

                    break;
                }
            }

#else

#ifdef GW_PROXY_MODE

            // Registering all proxies.
            for (int32_t i = 0; i < num_reversed_proxies_; i++)
            {
                // Registering URI handlers.
                err_code = AddUriHandler(
                    &gw_workers_[0],
                    gw_handlers_,
                    reverse_proxies_[i].gw_proxy_port_,
                    reverse_proxies_[i].service_uri_.c_str(),
                    reverse_proxies_[i].service_uri_len_,
                    reverse_proxies_[i].service_uri_.c_str(),
                    reverse_proxies_[i].service_uri_len_,
                    bmx::HTTP_METHODS::OTHER_METHOD,
                    NULL,
                    0,
                    bmx::BMX_INVALID_HANDLER_INFO,
                    empty_db_index,
                    GatewayUriProcessProxy);

                if (err_code)
                {
                    // Leaving global lock.
                    LeaveGlobalLock();

                    return err_code;
                }
            }

#endif

#ifdef GW_GLOBAL_STATISTICS

            // Registering URI handler for gateway statistics.
            err_code = AddUriHandler(
                &gw_workers_[0],
                gw_handlers_,
                setting_gw_stats_port_,
                "GET /gwstats",
                12,
                "GET /gwstats ",
                13,
                bmx::HTTP_METHODS::OTHER_METHOD,
                NULL,
                0,
                bmx::BMX_INVALID_HANDLER_INFO,
                empty_db_index,
                GatewayStatisticsInfo);

            if (err_code)
            {
                // Leaving global lock.
                LeaveGlobalLock();

                return err_code;
            }
#endif

#endif

            // Leaving global lock.
            LeaveGlobalLock();

            // Registering push channel on first worker.
            err_code = gw_workers_[0].GetWorkerDb(empty_db_index)->RegisterAllPushChannels();
            GW_ERR_CHECK(err_code);
        }
    }

    // Checking what databases went down.
    for (int32_t s = 0; s < num_dbs_slots_; s++)
    {
        if ((db_did_go_down_[s]) && (!active_databases_[s].IsDeletionStarted()))
        {
#ifdef GW_DATABASES_DIAG
            GW_PRINT_GLOBAL << "Start detaching dead database: " << active_databases_[s].db_name() << GW_ENDL;
#endif

            // Entering global lock.
            EnterGlobalLock();

            // Start database deletion.
            active_databases_[s].StartDeletion();

            // Killing channels events monitor thread.
            active_databases_[s].KillChannelsEventsMonitor();

            // Leaving global lock.
            LeaveGlobalLock();
        }
    }

    return 0;
}

// Active database constructor.
ActiveDatabase::ActiveDatabase()
{
    apps_unique_session_numbers_unsafe_ = NULL;
    apps_session_salts_unsafe_ = NULL;
    user_handlers_ = NULL;

    StartDeletion();
}

// Initializes this active database slot.
void ActiveDatabase::Init(
    std::string db_name,
    uint64_t unique_num,
    int32_t db_index)
{
    // Creating new Apps sessions up to maximum number of connections.
    if (!apps_unique_session_numbers_unsafe_)
    {
        apps_unique_session_numbers_unsafe_ = new apps_unique_session_num_type[g_gateway.setting_max_connections()];
        apps_session_salts_unsafe_ = new session_salt_type[g_gateway.setting_max_connections()];
    }

    // Cleaning all Apps session numbers.
    for (int32_t i = 0; i < g_gateway.setting_max_connections(); i++)
    {
        apps_unique_session_numbers_unsafe_[i] = INVALID_APPS_UNIQUE_SESSION_NUMBER;
        apps_session_salts_unsafe_[i] = INVALID_SESSION_SALT;
    }

    // Creating fresh handlers table.
    user_handlers_ = new HandlersTable();

    db_name_ = db_name;
    unique_num_unsafe_ = unique_num;
    db_index_ = db_index;
    were_sockets_closed_ = false;

    num_confirmed_push_channels_ = 0;
    is_empty_ = false;
}

// Checks if this database slot empty.
bool ActiveDatabase::IsEmpty()
{
    if (is_empty_)
        return true;

    // Checking if all chunks for this database were released.
    is_empty_ = (INVALID_UNIQUE_DB_NUMBER == unique_num_unsafe_) &&
        (0 == g_gateway.NumberUsedChunksPerDatabase(db_index_));

    return is_empty_;
}

// Makes this database slot empty.
void ActiveDatabase::StartDeletion()
{
    // Closing all database sockets/sessions data.
    CloseSocketData();

    // Removing handlers table.
    if (user_handlers_)
    {
        delete user_handlers_;
        user_handlers_ = NULL;
    }

    unique_num_unsafe_ = INVALID_UNIQUE_DB_NUMBER;
    db_name_ = "";
}

// Closes all tracked sockets.
void ActiveDatabase::CloseSocketData()
{
    // Checking if sockets were already closed.
    if (were_sockets_closed_)
        return;

    // Marking closure.
    were_sockets_closed_ = true;

    // Checking if just marking for deletion.
    for (SOCKET s = 0; s < g_gateway.setting_max_connections(); s++)
    {
        bool needs_deletion = false;

        // Checking if socket was active in any workers.
        for (int32_t w = 0; w < g_gateway.setting_num_workers(); w++)
        {
            WorkerDbInterface* worker_db = g_gateway.get_worker(w)->GetWorkerDb(db_index_);

            if (worker_db->GetSocketState(s))
                needs_deletion = true;

           worker_db->UntrackSocket(s);
        }

        // Checking if socket is active.
        if (needs_deletion)
        {
            // Marking deleted socket.
            g_gateway.MarkSocketDelete(s);

            // NOTE: Can't kill the session here, because it can be used by other databases.

            // NOTE: Closing socket which will results in stop of all pending operations on that socket.
            if (closesocket(s))
            {
#ifdef GW_ERRORS_DIAG
                GW_COUT << "closesocket() failed." << GW_ENDL;
#endif
                PrintLastError();
            }
        }
    }
}

// Initializes WinSock, all core data structures, binds server sockets.
uint32_t Gateway::Init()
{
    // Checking if already initialized.
    GW_ASSERT((gw_workers_ == NULL) && (worker_thread_handles_ == NULL));

    // Initialize WinSock.
    WSADATA wsaData = { 0 };
    int32_t errCode = WSAStartup(MAKEWORD(2, 2), &wsaData);
    if (errCode != 0)
    {
        GW_COUT << "WSAStartup() failed: " << errCode << GW_ENDL;
        return errCode;
    }

    // Allocating workers data.
    gw_workers_ = new GatewayWorker[setting_num_workers_];
    worker_thread_handles_ = new HANDLE[setting_num_workers_];

    // Creating IO completion port.
    iocp_ = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, setting_num_workers_);
    if (iocp_ == NULL)
    {
        GW_COUT << "Failed to create IOCP." << GW_ENDL;
        return PrintLastError();
    }

    // Filling up worker parameters.
    for (int i = 0; i < setting_num_workers_; i++)
    {
        int32_t errCode = gw_workers_[i].Init(i);
        if (errCode != 0)
            return errCode;
    }

#ifdef GW_TESTING_MODE
    // Creating and activating server sockets.
    if (setting_is_master_)
    {
#endif

        // Going throw all needed ports.
        for (int32_t p = 0; p < num_server_ports_unsafe_; p++)
        {
            // Skipping empty port.
            if (server_ports_[p].IsEmpty())
                continue;

            SOCKET server_socket = INVALID_SOCKET;

            // Creating socket and binding to port (only on the first worker).
            uint32_t errCode = CreateListeningSocketAndBindToPort(
                &gw_workers_[0],
                server_ports_[p].get_port_number(),
                server_socket);

            GW_ERR_CHECK(errCode);
        }

#ifdef GW_TESTING_MODE
    }

#endif

    // Obtaining function pointers (AcceptEx, ConnectEx, DisconnectEx).
    uint32_t temp;
    SOCKET tempSocket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (tempSocket == INVALID_SOCKET)
    {
        GW_COUT << "WSASocket() failed." << GW_ENDL;
        return PrintLastError();
    }

    if (WSAIoctl(tempSocket, SIO_GET_EXTENSION_FUNCTION_POINTER, &AcceptExGuid, sizeof(AcceptExGuid), &AcceptExFunc, sizeof(AcceptExFunc), (LPDWORD)&temp, NULL, NULL))
    {
        GW_COUT << "Failed WSAIoctl(AcceptEx)." << GW_ENDL;
        return PrintLastError();
    }

    if (WSAIoctl(tempSocket, SIO_GET_EXTENSION_FUNCTION_POINTER, &ConnectExGuid, sizeof(ConnectExGuid), &ConnectExFunc, sizeof(ConnectExFunc), (LPDWORD)&temp, NULL, NULL))
    {
        GW_COUT << "Failed WSAIoctl(ConnectEx)." << GW_ENDL;
        return PrintLastError();
    }

    if (WSAIoctl(tempSocket, SIO_GET_EXTENSION_FUNCTION_POINTER, &DisconnectExGuid, sizeof(DisconnectExGuid), &DisconnectExFunc, sizeof(DisconnectExFunc), (LPDWORD)&temp, NULL, NULL))
    {
        GW_COUT << "Failed WSAIoctl(DisconnectEx)." << GW_ENDL;
        return PrintLastError();
    }
    closesocket(tempSocket);

    // Global HTTP init.
    HttpGlobalInit();

    // Initializing Gateway logger.
    gw_log_writer_.Init(setting_log_file_path_);

#ifdef GW_TESTING_MODE

    // Creating server address if we are on client.
    if (!g_gateway.setting_is_master())
    {
        server_addr_ = new sockaddr_in;
        memset(server_addr_, 0, sizeof(sockaddr_in));
        server_addr_->sin_family = AF_INET;
        server_addr_->sin_addr.s_addr = inet_addr(g_gateway.setting_master_ip().c_str());
        server_addr_->sin_port = htons(GATEWAY_TEST_PORT_NUMBER_SERVER);
    }

    InitTestHttpEchoRequests();

#endif

#ifdef GW_URI_MATCHING_CODEGEN
    // Loading URI codegen matcher.
    codegen_uri_matcher_ = new CodegenUriMatcher();
    codegen_uri_matcher_->Init();
#endif

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

    // Indicating that network gateway is ready
    // (should be first line of the output).
    GW_COUT << "Gateway is ready!" << GW_ENDL;

    // Indicating begin of new logging session.
    time_t raw_time;
    time(&raw_time);
    tm *timeinfo = localtime(&raw_time);
    GW_PRINT_GLOBAL << "New logging session: " << asctime(timeinfo) << GW_ENDL;

    return 0;
}

// Initializes everything related to shared memory.
uint32_t Gateway::InitSharedMemory(
    std::string setting_databaseName,
    core::shared_interface* shared_int)
{
    using namespace core;

    // Construct the database_shared_memory_parameters_name. The format is
    // <DATABASE_NAME_PREFIX>_<SERVER_TYPE>_<DATABASE_NAME>_0
    std::string shm_params_name = (std::string)DATABASE_NAME_PREFIX + "_" +
        setting_sc_server_type_upper_ + "_" + StringToUpperCopy(setting_databaseName) + "_0";

    // Open the database shared memory parameters file and obtains a pointer to
    // the shared structure.
    database_shared_memory_parameters_ptr db_shm_params(shm_params_name.c_str());

    // Send registration request to the monitor and try to acquire an owner_id.
    // Without an owner_id we can not proceed and have to exit.
    // Get process id and store it in the monitor_interface.
    pid_type pid;
    pid.set_current();
    owner_id the_owner_id;
    uint32_t error_code;

    // Try to register this client process pid. Wait up to 10000 ms.
    if ((error_code = shm_monitor_interface_->register_client_process(pid,
        the_owner_id, 10000/*ms*/)) != 0)
    {
        // Failed to register this client process pid.
        GW_COUT << "Can't register client process, error code: " << error_code << GW_ENDL;
        return error_code;
    }

    // Open the database shared memory segment.
    if (db_shm_params->get_sequence_number() == 0)
    {
        // Cannot open the database shared memory segment, because it is not
        // initialized yet.
        GW_COUT << "Cannot open the database shared memory segment!" << GW_ENDL;
    }

    // Name of the database shared memory segment.
    char seq_num_str[16];
    itoa(db_shm_params->get_sequence_number(), seq_num_str, 10);
    std::string shm_seg_name = std::string(DATABASE_NAME_PREFIX) + "_" +
        setting_sc_server_type_upper_ + "_" +
        StringToUpperCopy(setting_databaseName) + "_" +
        std::string(seq_num_str);

    // Construct a shared_interface.
    for (int32_t i = 0; i < setting_num_workers_; i++)
    {
        shared_int[i].init(shm_seg_name.c_str(), shm_monitor_int_name_.c_str(), pid, the_owner_id);
    }

    return 0;
}

// Current global statistics value.
const char* Gateway::GetGlobalStatisticsString(int32_t* out_len)
{
    *out_len = 0;

    EnterCriticalSection(&cs_statistics_);

    // Getting number of written characters.
    int32_t n = global_statistics_stream_.tellp();
    GW_ASSERT(n < MAX_STATS_LENGTH);
    *out_len = kHttpStatsHeaderLength + n;

    // Copying characters from stream to given buffer.
    global_statistics_stream_.seekg(0);
    global_statistics_stream_.rdbuf()->sgetn(global_statistics_string_ + kHttpStatsHeaderLength, n);
    global_statistics_string_[kHttpStatsHeaderLength + n] = '\0';

    // Making length a white space.
    *(uint64_t*)(global_statistics_string_ + kHttpStatsHeaderInsertPoint) = 0x2020202020202020;
    
    // Converting content length to string.
    WriteUIntToString(global_statistics_string_ + kHttpStatsHeaderInsertPoint, n);

    LeaveCriticalSection(&cs_statistics_);

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
GatewayWorker* Gateway::get_worker(int32_t worker_id)
{
    return gw_workers_ + worker_id;
}

// Delete all information associated with given database from server ports.
uint32_t Gateway::DeletePortsForDb(int32_t db_index)
{
    // Going through all ports.
    for (int32_t i = 0; i < num_server_ports_unsafe_; i++)
    {
        // Checking that port is not empty.
        if (!server_ports_[i].IsEmpty())
        {
            // Deleting port handlers if any.
            server_ports_[i].EraseDb(db_index);

            // Checking if port is not used anywhere.
            if (server_ports_[i].IsEmpty())
                server_ports_[i].Erase();
        }
    }

    // Removing deleted trailing server ports.
    for (int32_t i = (num_server_ports_unsafe_ - 1); i >= 0; i--)
    {
        // Removing until one server port is not empty.
        if (!server_ports_[i].IsEmpty())
            break;

        num_server_ports_unsafe_--;
    }

    return 0;
}

// Getting the number of used sockets.
int64_t Gateway::NumberUsedSocketsAllWorkersAndDatabases()
{
    int64_t num_used_sockets = 0;

    for (int32_t d = 0; d < num_dbs_slots_; d++)
    {
        for (int32_t w = 0; w < setting_num_workers_; w++)
        {
            num_used_sockets += gw_workers_[w].NumberUsedSocketPerDatabase(d);
        }
    }

    return num_used_sockets;
}

// Getting the number of reusable connect sockets.
int64_t Gateway::NumberOfReusableConnectSockets()
{
    int64_t num_reusable_connect_sockets = 0;

    for (int32_t w = 0; w < setting_num_workers_; w++)
    {
        num_reusable_connect_sockets += gw_workers_[w].NumberOfReusableConnectSockets();
    }

    return num_reusable_connect_sockets;
}

// Getting the number of used sockets per database.
int64_t Gateway::NumberUsedSocketsPerDatabase(int32_t db_index)
{
    int64_t num_used_sockets = 0;

    for (int32_t w = 0; w < setting_num_workers_; w++)
    {
        num_used_sockets += gw_workers_[w].NumberUsedSocketPerDatabase(db_index);
    }

    return num_used_sockets;
}

// Getting the number of used sockets per worker.
int64_t Gateway::NumberUsedSocketsPerWorker(int32_t worker_id)
{
    int64_t num_used_sockets = 0;

    for (int32_t d = 0; d < num_dbs_slots_; d++)
    {
        num_used_sockets += gw_workers_[worker_id].NumberUsedSocketPerDatabase(d);
    }

    return num_used_sockets;
}

// Getting the total number of used chunks for all databases.
int64_t Gateway::NumberUsedChunksAllWorkersAndDatabases()
{
    int64_t total_used_chunks = 0;
    for (int32_t d = 0; d < num_dbs_slots_; d++)
    {
        for (int32_t w = 0; w < setting_num_workers_; w++)
        {
            total_used_chunks += (gw_workers_[w].NumberUsedChunksPerDatabasePerWorker(d));
        }
    }

    return total_used_chunks;
}

// Getting the number of used chunks per database.
int64_t Gateway::NumberUsedChunksPerDatabase(int32_t db_index)
{
    int64_t num_used_chunks = 0;

    for (int32_t w = 0; w < setting_num_workers_; w++)
    {
        num_used_chunks += gw_workers_[w].NumberUsedChunksPerDatabasePerWorker(db_index);
    }

    return num_used_chunks;
}

// Getting the number of used chunks per worker.
int64_t Gateway::NumberUsedChunksPerWorker(int32_t worker_id)
{
    int64_t num_used_chunks = 0;

    for (int32_t d = 0; d < num_dbs_slots_; d++)
    {
        num_used_chunks += gw_workers_[worker_id].NumberUsedChunksPerDatabasePerWorker(d);
    }

    return num_used_chunks;
}

#ifdef GW_COLLECT_SOCKET_STATISTICS

// Getting the number of active connections per port.
int64_t Gateway::NumberOfActiveConnectionsPerPort(int32_t port_index)
{
    int64_t num_active_conns = 0;

    for (int32_t w = 0; w < setting_num_workers_; w++)
    {
        num_active_conns += gw_workers_[w].NumberOfActiveConnectionsPerPortPerWorker(port_index);
    }

    return num_active_conns;
}

#endif

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
    for (int32_t i = 0; i < g_gateway.setting_num_workers(); i++)
    {
        // Obtaining worker thread handle to call an APC event.
        HANDLE worker_thread_handle = g_gateway.get_worker_thread_handle(i);

        // Waking up the worker with APC.
        QueueUserAPC(EmptyApcFunction, worker_thread_handle, 0);
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

    /// TODO: Fix insecure
    if ((length = mbstowcs(w_active_databases_updated_event_name,
        active_databases_updated_event_name, core::segment_name_size)) < 0) {
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

#ifndef GW_OLD_ACTIVE_DATABASES_DISCOVER 

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

#else

    // Creating path to IPC monitor directory active databases.
    std::wstring active_databases_dir = g_gateway.get_setting_server_output_dir() + L"\\"+ W_DEFAULT_MONITOR_DIR_NAME + L"\\" + W_DEFAULT_MONITOR_ACTIVE_DATABASES_FILE_NAME + L"\\";

    // Obtaining full path to IPC monitor directory.
    wchar_t active_databases_dir_full[1024];
    if (!GetFullPathName(active_databases_dir.c_str(), 1024, active_databases_dir_full, NULL))
    {
        GW_PRINT_GLOBAL << "Can't obtain full path for IPC monitor output directory: " << PrintLastError() << GW_ENDL;
        return SCERRGWPATHTOIPCMONITORDIR;
    }

    // Waiting until active databases directory is up.
    while (GetFileAttributes(active_databases_dir_full) == INVALID_FILE_ATTRIBUTES)
    {
        GW_PRINT_GLOBAL << "Please start the IPC monitor process first!" << GW_ENDL;
        Sleep(100);
    }

    // Creating path to active databases file.
    std::wstring active_databases_file_path = active_databases_dir_full;
    active_databases_file_path += W_DEFAULT_MONITOR_ACTIVE_DATABASES_FILE_NAME;

    // Setting listener on monitor output directory.
    HANDLE dir_changes_hook_handle = FindFirstChangeNotification(active_databases_dir_full, FALSE, FILE_NOTIFY_CHANGE_LAST_WRITE);
    if ((INVALID_HANDLE_VALUE == dir_changes_hook_handle) || (NULL == dir_changes_hook_handle))
    {
        GW_PRINT_GLOBAL << "Can't listen for active databases directory changes: " << PrintLastError() << GW_ENDL;
        FindCloseChangeNotification(dir_changes_hook_handle);

        return SCERRGWACTIVEDBLISTENPROBLEM;
    }

    GW_PRINT_GLOBAL << "Waiting for databases..." << GW_ENDL;
    while (1)
    {
        // Waiting infinitely on directory changes.
        DWORD wait_status = WaitForSingleObject(dir_changes_hook_handle, INFINITE);
        GW_PRINT_GLOBAL << "Changes in active databases directory detected." << GW_ENDL;

        switch (wait_status)
        {
            case WAIT_OBJECT_0:
            {
                std::ifstream ad_file(active_databases_file_path);

                // Just quiting if file can't be opened.
                if (ad_file.is_open() == false)
                    break;

                // Populating the active databases set.
                std::string current_db_name;
                while (getline(ad_file, current_db_name))
                    active_databases_set.insert(current_db_name);

                // Checking for any database changes in active databases file.
                err_code = g_gateway.CheckDatabaseChanges(active_databases_set);

                if (err_code)
                {
                    FindCloseChangeNotification(dir_changes_hook_handle);
                    return err_code;
                }

                // Requests that the operating system signal a change notification
                // handle the next time it detects an appropriate change.
                if (FindNextChangeNotification(dir_changes_hook_handle) == FALSE)
                {
                    GW_PRINT_GLOBAL << "Failed to find next change notification on monitor active databases directory: " << PrintLastError() << GW_ENDL;
                    FindCloseChangeNotification(dir_changes_hook_handle);

                    return SCERRGWFAILEDFINDNEXTCHANGENOTIFICATION;
                }

                break;
            }

            default:
            {
                GW_PRINT_GLOBAL << "Error listening for active databases directory changes: " << PrintLastError() << GW_ENDL;
                FindCloseChangeNotification(dir_changes_hook_handle);

                return SCERRGWACTIVEDBLISTENPROBLEM;
            }
        }
    }

#endif

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

#ifndef GW_NEW_SESSIONS_APPROACH

// Cleans up all collected inactive sessions.
uint32_t Gateway::CleanupInactiveSessions(GatewayWorker* gw)
{
    uint32_t err_code;

    EnterCriticalSection(&cs_sessions_cleanup_);

    // Going through all collected inactive sessions.
    int64_t num_sessions_to_cleanup = num_sessions_to_cleanup_unsafe_;
    for (int32_t i = 0; i < num_sessions_to_cleanup; i++)
    {
        // Getting existing session copy.
        ScSessionStruct global_session_copy = GetGlobalSessionCopy(sessions_to_cleanup_unsafe_[i]);

        // Checking if session is valid.
        if (global_session_copy.IsValid())
        {
            // Killing the session.
            bool session_was_killed;
            KillSession(sessions_to_cleanup_unsafe_[i], &session_was_killed);

            // Sending notification only if session was killed.
            if (session_was_killed)
            {
                // Pushing dead session message to all databases.
                for (int32_t d = 0; d < num_dbs_slots_; d++)
                {
                    ActiveDatabase* global_db = g_gateway.GetDatabase(d);

                    // Checking if database was already deleted.
                    if (global_db->IsDeletionStarted())
                        continue;

                    WorkerDbInterface *worker_db = gw->GetWorkerDb(d);

                    // Checking if database was already deleted.
                    if (!worker_db)
                        continue;

                    // Getting and checking Apps unique session number.
                    apps_unique_session_num_type apps_unique_session_num = global_db->GetAppsUniqueSessionNumber(global_session_copy.gw_session_index_);

                    // Checking if Apps session information is correct.
                    if (apps_unique_session_num != INVALID_APPS_UNIQUE_SESSION_NUMBER)
                    {
                        // Getting Apps session salt.
                        session_salt_type apps_session_salt = global_db->GetAppsSessionSalt(global_session_copy.gw_session_index_);

                        // Sending session destroyed message.
                        err_code = worker_db->PushDeadSession(
                            apps_unique_session_num,
                            apps_session_salt,
                            global_session_copy.scheduler_id_);

                        if (err_code)
                        {
                            LeaveCriticalSection(&cs_sessions_cleanup_);
                            return err_code;
                        }
                    }
                }

#ifdef GW_SESSIONS_DIAG
                GW_COUT << "Inactive session " << global_session_copy.gw_session_index_ << ":"
                    << global_session_copy.gw_session_salt_ << " was destroyed." << GW_ENDL;
#endif
            }
        }

        // Inactive session was successfully cleaned up.
        num_sessions_to_cleanup_unsafe_--;
    }

    GW_ASSERT(0 == num_sessions_to_cleanup_unsafe_);

    LeaveCriticalSection(&cs_sessions_cleanup_);

    return 0;
}

// Collects outdated sessions if any.
uint32_t Gateway::CollectInactiveSessions()
{
    // Checking if collected sessions cleanup is not finished yet.
    if (num_sessions_to_cleanup_unsafe_)
        return 0;

    EnterCriticalSection(&cs_sessions_cleanup_);

    GW_ASSERT(0 == num_sessions_to_cleanup_unsafe_);

    int32_t num_inactive = 0;

    // TODO: Optimize scanning range.
    for (int32_t i = 0; i < setting_max_connections_; i++)
    {
        // Checking if session touch time is older than inactive session timeout.
        if ((all_sessions_unsafe_[i].session_timestamp_) &&
            (global_timer_unsafe_ - all_sessions_unsafe_[i].session_timestamp_) >= setting_inactive_session_timeout_seconds_)
        {
            sessions_to_cleanup_unsafe_[num_inactive] = i;
            ++num_inactive;
        }

        // Checking if we have checked all active sessions.
        if (num_inactive >= num_active_sessions_unsafe_)
            break;
    }

    num_sessions_to_cleanup_unsafe_ = num_inactive;

    LeaveCriticalSection(&cs_sessions_cleanup_);

    if (num_active_sessions_unsafe_)
    {
        // Waking up the worker 0 thread with APC.
        QueueUserAPC(EmptyApcFunction, worker_thread_handles_[0], 0);
    }

    return 0;
}
#endif

// Entry point for inactive sessions cleanup.
uint32_t __stdcall InactiveSessionsCleanupRoutine(LPVOID params)
{
    // Catching all unhandled exceptions in this thread.
    GW_SC_BEGIN_FUNC

    while (true)
    {
#ifndef GW_NEW_SESSIONS_APPROACH
        // Sleeping minimum interval.
        Sleep(g_gateway.get_min_inactive_session_life_seconds() * 1000);

        // Increasing global time by minimum number of seconds.
        g_gateway.step_global_timer_unsafe(g_gateway.get_min_inactive_session_life_seconds());

        // Collecting inactive sessions if any.
        g_gateway.CollectInactiveSessions();

#else
        Sleep(100000);
#endif
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
    // Closing IOCP.
    CloseHandle(iocp_);

    // Cleanup WinSock.
    WSACleanup();

    // Deleting critical sections.
    DeleteCriticalSection(&cs_session_);
    DeleteCriticalSection(&cs_global_lock_);
    DeleteCriticalSection(&cs_sessions_cleanup_);
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
            if (!WaitForSingleObject(worker_thread_handles_[i], 0))
            {
                // Stating explicitly that this worker is dead.
                gw_workers_[i].set_worker_suspended(true);

                // Printing diagnostics.
                GW_COUT << "Worker " << i << " is dead." << GW_ENDL;

                return SCERRGWWORKERISDEAD;
            }
        }

        // Checking if database monitor thread is alive.
        if (!WaitForSingleObject(db_monitor_thread_handle_, 0))
        {
            GW_COUT << "Active databases monitor thread is dead." << GW_ENDL;
            return SCERRGWDATABASEMONITORISDEAD;
        }

        // Checking if database channels events thread is alive.
        if (!WaitForSingleObject(channels_events_thread_handle_, 0))
        {
            GW_COUT << "Channels events thread is dead." << GW_ENDL;
            return SCERRGWCHANNELSEVENTSTHREADISDEAD;
        }

        // Checking if dead sessions cleanup thread is alive.
        if (!WaitForSingleObject(dead_sessions_cleanup_thread_handle_, 0))
        {
            GW_COUT << "Dead sessions cleanup thread is dead." << GW_ENDL;
            return SCERRGWSESSIONSCLEANUPTHREADISDEAD;
        }

        // Checking if gateway logging thread is alive.
        if (!WaitForSingleObject(gateway_logging_thread_handle_, 0))
        {
            GW_COUT << "Gateway logging thread is dead." << GW_ENDL;
            return SCERRGWGATEWAYLOGGINGTHREADISDEAD;
        }

        // Checking if we are still running when we should not.
#ifdef GW_TESTING_MODE

        running_time_seconds += GW_MONITOR_THREAD_TIMEOUT_SECONDS;
        if (running_time_seconds >= setting_max_test_time_seconds_)
        {
            GW_COUT << "Test timed out. Exiting process." << GW_ENDL;

            ShutdownGateway(NULL, SCERRGWTESTTIMEOUT);
        }

#endif
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

            ShutdownGateway(NULL, SCERRGWSOMETHREADDIED);
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

#ifdef GW_TESTING_MODE

        // Checking if we have started measurements.
        if (started_measured_test_)
        {
            switch (g_gateway.setting_mode())
            {
                case GatewayTestingMode::MODE_GATEWAY_HTTP:
                case GatewayTestingMode::MODE_GATEWAY_SMC_HTTP:
                case GatewayTestingMode::MODE_GATEWAY_SMC_APPS_HTTP:
                {
                    num_gateway_ops_per_second_ += diffProcessedHttpRequestsAllWorkers;
                    break;
                }

                case GatewayTestingMode::MODE_GATEWAY_RAW:
                case GatewayTestingMode::MODE_GATEWAY_SMC_RAW:
                case GatewayTestingMode::MODE_GATEWAY_SMC_APPS_RAW:
                {
                    num_gateway_ops_per_second_ += diffRecvNumAllWorkers;
                    break;
                }
            }

            num_ops_measures_++;
        }

#endif

#ifdef GW_GLOBAL_STATISTICS

        EnterCriticalSection(&cs_statistics_);

        // Emptying the statistics stream.
        global_statistics_stream_.clear();
        global_statistics_stream_.seekp(0);

        // Global statistics.
        global_statistics_stream_ << "Global: " <<
            "Active chunks " << g_gateway.NumberUsedChunksAllWorkersAndDatabases() <<
#ifndef GW_NEW_SESSIONS_APPROACH
            ", active sessions " << g_gateway.get_num_active_sessions_unsafe() <<
#endif
            ", used sockets " << g_gateway.NumberUsedSocketsAllWorkersAndDatabases() <<
            ", reusable conn socks " << g_gateway.NumberOfReusableConnectSockets() <<
            "<br>" << GW_ENDL;

        // Individual workers statistics.
        for (int32_t worker_id_ = 0; worker_id_ < setting_num_workers_; worker_id_++)
        {
            global_statistics_stream_ << "[" << worker_id_ << "]: " <<
                "recv_bytes " << gw_workers_[worker_id_].get_worker_stats_bytes_received() <<
                ", recv_times " << gw_workers_[worker_id_].get_worker_stats_recv_num() <<
                ", sent_bytes " << gw_workers_[worker_id_].get_worker_stats_bytes_sent() <<
                ", sent_times " << gw_workers_[worker_id_].get_worker_stats_sent_num() <<
                "<br>" << GW_ENDL;
        }

        // Printing handlers information for each attached database and gateway.
        for (int32_t p = 0; p < num_server_ports_unsafe_; p++)
        {
            // Checking if port is alive.
            if (!server_ports_[p].IsEmpty())
            {
                global_statistics_stream_ << "Port " << server_ports_[p].get_port_number() <<

#ifdef GW_COLLECT_SOCKET_STATISTICS
                    ": active conns " << server_ports_[p].NumberOfActiveConnections() <<
#endif

                    ": accepting socks " << server_ports_[p].get_num_accepting_sockets() <<

#ifdef GW_COLLECT_SOCKET_STATISTICS
                    ", alloc acc-socks " << server_ports_[p].get_num_allocated_accept_sockets() <<
                    ", alloc conn-socks " << server_ports_[p].get_num_allocated_connect_sockets() <<
#endif                        
                    "<br>" << GW_ENDL;
            }
        }

        // Printing all workers stats.
        global_statistics_stream_ << "All workers last sec " <<
            "recv_times " << diffRecvNumAllWorkers <<
            ", http_requests " << diffProcessedHttpRequestsAllWorkers <<
            ", recv_bandwidth " << recv_bandwidth_mbit_total << " mbit/sec" <<
            ", sent_times " << diffSentNumAllWorkers <<
            ", send_bandwidth " << send_bandwidth_mbit_total << " mbit/sec" <<
            "<br>" << GW_ENDL;

#ifdef GW_TESTING_MODE
        global_statistics_stream_ << "Perf Test Info: num confirmed echoes " << num_confirmed_echoes_unsafe_ <<
            "(" << setting_num_echoes_to_master_ << "), num sent echoes " << (current_echo_number_unsafe_ + 1) <<
            "(" << setting_num_echoes_to_master_ << ")" << "<br>" << GW_ENDL;
#endif

        LeaveCriticalSection(&cs_statistics_);

        // Printing the statistics string.
#ifdef GW_TESTING_MODE
        int32_t len;
        std::cout << GetGlobalStatisticsString(&len);
#endif

#endif

    }

    return 0;
}

// Starts gateway workers and statistics printer.
uint32_t Gateway::StartWorkerAndManagementThreads(
    LPTHREAD_START_ROUTINE workerRoutine,
    LPTHREAD_START_ROUTINE monitorDatabasesRoutine,
    LPTHREAD_START_ROUTINE channelsEventsMonitorRoutine,
    LPTHREAD_START_ROUTINE deadSessionsCleanupRoutine,
    LPTHREAD_START_ROUTINE gatewayLoggingRoutine,
    LPTHREAD_START_ROUTINE allThreadsMonitorRoutine)
{
    // Allocating threads-related data structures.
    uint32_t *worker_thread_ids = new uint32_t[setting_num_workers_];

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

    uint32_t deadSessionsCleanupThreadId;

    // Starting dead sessions cleanup thread.
    dead_sessions_cleanup_thread_handle_ = CreateThread(
        NULL, // Default security attributes.
        0, // Use default stack size.
        deadSessionsCleanupRoutine, // Thread function name.
        NULL, // Argument to thread function.
        0, // Use default creation flags.
        (LPDWORD)&deadSessionsCleanupThreadId); // Returns the thread identifier.

    // Checking if thread is created.
    GW_ASSERT(dead_sessions_cleanup_thread_handle_ != NULL);

    uint32_t gatewayLogRoutineThreadId;

    // Starting dead sessions cleanup thread.
    gateway_logging_thread_handle_ = CreateThread(
        NULL, // Default security attributes.
        0, // Use default stack size.
        gatewayLoggingRoutine, // Thread function name.
        NULL, // Argument to thread function.
        0, // Use default creation flags.
        (LPDWORD)&gatewayLogRoutineThreadId); // Returns the thread identifier.

    // Checking if thread is created.
    GW_ASSERT(gateway_logging_thread_handle_ != NULL);

    // Starting dead sessions cleanup thread.
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

    delete worker_thread_ids;
    delete worker_thread_handles_;
    delete [] gw_workers_;
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
        delete [] gw_workers_;
        gw_workers_ = NULL;
    }
}

int32_t Gateway::StartGateway()
{
    uint32_t errCode;

    // Assert some correct state.
    errCode = AssertCorrectState();
    if (errCode)
    {
        GW_COUT << "Asserting correct state failed." << GW_ENDL;
        return errCode;
    }

    // Loading configuration settings.
    errCode = LoadSettings(setting_config_file_path_);
    if (errCode)
    {
        GW_COUT << "Loading configuration settings failed." << GW_ENDL;
        return errCode;
    }

    // Creating data structures and binding sockets.
    errCode = Init();
    if (errCode)
        return errCode;

    // Starting workers and statistics printer.
    errCode = StartWorkerAndManagementThreads(
        (LPTHREAD_START_ROUTINE)GatewayWorkerRoutine,
        (LPTHREAD_START_ROUTINE)MonitorDatabasesRoutine,
        (LPTHREAD_START_ROUTINE)AllDatabasesChannelsEventsMonitorRoutine,
        (LPTHREAD_START_ROUTINE)InactiveSessionsCleanupRoutine,
        (LPTHREAD_START_ROUTINE)GatewayLoggingRoutine,
        (LPTHREAD_START_ROUTINE)GatewayMonitorRoutine);

    if (errCode)
        return errCode;

    return 0;
}

#ifdef GW_TESTING_MODE

// Calculates number of created connections for all workers.
int64_t Gateway::GetNumberOfCreatedConnectionsAllWorkers()
{
    int64_t num_created_conns = 0;
    for (int32_t w = 0; w < setting_num_workers_; w++)
    {
        num_created_conns += gw_workers_[w].get_num_created_conns_worker();
    }
    return num_created_conns;
}

#ifdef GW_LOOPED_TEST_MODE

// Getting number of preparation network events.
int64_t Gateway::GetNumberOfPreparationNetworkEventsAllWorkers()
{
    int64_t num_preparation_events = 0;
    for (int32_t w = 0; w < setting_num_workers_; w++)
    {
        num_preparation_events += gw_workers_[w].GetNumberOfPreparationNetworkEvents();
    }
    return num_preparation_events;
}

#endif

// Checks that echo responses are correct.
bool Gateway::CheckConfirmedEchoResponses(GatewayWorker* gw)
{
    // First checking if we have confirmed all echoes.
    if (num_confirmed_echoes_unsafe_ != setting_num_echoes_to_master_)
        return false;

    // Running through all echoes.
    for (int32_t i = 0; i < setting_num_echoes_to_master_; i++)
    {
        // Checking that each echo is confirmed.
        if (confirmed_echoes_shared_[i] != true)
        {
            GW_COUT << "Incorrect echo: " << i << GW_ENDL;

            // Failing test if echo is not confirmed.
            ShutdownTest(gw, false);

            return false;
        }
    }

    // Gracefully finishing the test.
    ShutdownTest(gw, true);

    return true;
}

// Gracefully shutdowns all needed processes after test is finished.
uint32_t Gateway::ShutdownTest(GatewayWorker* gw, bool success)
{
    // Checking if we are on the build server.
    char* envvar_str = std::getenv("SC_RUNNING_ON_BUILD_SERVER");
    bool is_on_build_server = false;
    if (envvar_str)
    {
        // Checking for exactly True value.
        if (0 == strcmp(envvar_str, "True"))
            is_on_build_server = true;
    }

    if (success)
    {
        GW_COUT << "Echo test finished successfully!" << GW_ENDL;

        int64_t test_finish_time = timeGetTime();

        // Test finished successfully, printing the results.
        int64_t ops_per_second = GetAverageOpsPerSecond();

        GW_COUT << "Average number of ops per second: " << ops_per_second <<
            ". Took " << test_finish_time - test_begin_time_ << " ms." << GW_ENDL;

        GW_COUT << "##teamcity[buildStatisticValue key='" << setting_stats_name_ << "' value='" << ops_per_second << "']" << GW_ENDL;

        if (is_on_build_server)
            ShutdownGateway(gw, 0);

        return 0;
    }

    // This is a test failure.
    GW_COUT << "ERROR with echo testing!" << GW_ENDL;

    if (is_on_build_server)
        ShutdownGateway(gw, SCERRGWTESTFAILED);

    return 0;
}

#endif

// Generate the code using managed generator.
uint32_t Gateway::GenerateUriMatcher(RegisteredUris* port_uris)
{
    // Getting registered URIs.
    std::vector<MixedCodeConstants::RegisteredUriManaged> uris_managed = port_uris->GetRegisteredUriManaged();

    // Creating root URI matching function name.
    char root_function_name[32];
    sprintf(root_function_name, "MatchUriForPort%d", port_uris->get_port_number());

    // Calling managed function.
    uint32_t err_code = codegen_uri_matcher_->GenerateUriMatcher(
        root_function_name,
        &uris_managed.front(),
        uris_managed.size());

    if (err_code)
        return err_code;

    MixedCodeConstants::MatchUriType match_uri_func;
    HMODULE gen_dll_handle;

    // Unloading existing matcher DLL if any.
    port_uris->UnloadLatestUriMatcherDllIfAny();

    // Constructing dll name;
    std::wostringstream dll_name;
    dll_name << L"codegen_uri_matcher_" << port_uris->get_port_number();

    // Building URI matcher from generated code and loading the library.
    err_code = codegen_uri_matcher_->CompileIfNeededAndLoadDll(
        UriMatchCodegenCompilerType::COMPILER_CLANG,
        dll_name.str(),
        root_function_name,
        port_uris->get_clang_engine_addr(),
        &match_uri_func,
        &gen_dll_handle);

    if (err_code)
        return err_code;

    // Setting the entry point for new URI matcher.
    port_uris->set_latest_match_uri_func(match_uri_func);
    port_uris->set_latest_gen_dll_handle(gen_dll_handle);

    return 0;
}

// Adds some URI handler: either Apps or Gateway.
uint32_t Gateway::AddUriHandler(
    GatewayWorker* gw,
    HandlersTable* handlers_table,
    uint16_t port,
    const char* original_uri_info,
    uint32_t original_uri_info_len_chars,
    const char* processed_uri_info,
    uint32_t processed_uri_info_len_chars,
    bmx::HTTP_METHODS http_method,
    uint8_t* param_types,
    int32_t num_params,
    BMX_HANDLER_TYPE handler_id,
    int32_t db_index,
    GENERIC_HANDLER_CALLBACK handler_proc)
{
    uint32_t err_code;

    // Registering URI handler.
    err_code = handlers_table->RegisterUriHandler(
        gw,
        port,
        original_uri_info,
        original_uri_info_len_chars,
        processed_uri_info,
        processed_uri_info_len_chars,
        http_method,
        param_types,
        num_params,
        handler_id,
        handler_proc,
        db_index);

    GW_ERR_CHECK(err_code);

    // Search for handler index by URI string.
    BMX_HANDLER_TYPE handler_index = handlers_table->FindUriHandlerIndex(
        port,
        processed_uri_info,
        processed_uri_info_len_chars);

    // Getting the port structure.
    ServerPort* server_port = g_gateway.FindServerPort(port);

    // Registering URI on port.
    RegisteredUris* all_port_uris = server_port->get_registered_uris();
    int32_t index = all_port_uris->FindRegisteredUri(processed_uri_info, processed_uri_info_len_chars);

    // Checking if there is an entry.
    if (index < 0)
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

        // Creating totally new URI entry.
        RegisteredUri new_entry(
            original_uri_info,
            original_uri_info_len_chars,
            processed_uri_info,
            processed_uri_info_len_chars,
            session_param_index,
            db_index,
            handlers_table->get_handler_list(handler_index));

        // Adding entry to global list.
        all_port_uris->AddEntry(new_entry);
    }
    else
    {
        // Obtaining existing URI entry.
        RegisteredUri* reg_uri = all_port_uris->GetEntryByIndex(index);

        // Checking if there is no database for this URI.
        if (!reg_uri->ContainsDb(db_index))
        {
            // Creating new unique handlers list for this database.
            UniqueHandlerList uhl(db_index, handlers_table->get_handler_list(handler_index));

            // Adding new handler list for this database to the URI.
            reg_uri->Add(uhl);
        }
    }
    GW_ERR_CHECK(err_code);

    // Invalidating URI matching codegen.
    all_port_uris->InvalidateUriMatcherFunction();

    // Printing port information.
    //server_port->Print();

    return 0;
}

// Adds some port handler: either Apps or Gateway.
uint32_t Gateway::AddPortHandler(
    GatewayWorker* gw,
    HandlersTable* handlers_table,
    uint16_t port,
    BMX_HANDLER_TYPE handler_info,
    int32_t db_index,
    GENERIC_HANDLER_CALLBACK handler_proc)
{
    return handlers_table->RegisterPortHandler(
        gw,
        port,
        handler_info,
        handler_proc,
        db_index);
}

// Adds some sub-port handler: either Apps or Gateway.
uint32_t Gateway::AddSubPortHandler(
    GatewayWorker* gw,
    HandlersTable* handlers_table,
    uint16_t port,
    bmx::BMX_SUBPORT_TYPE subport,
    BMX_HANDLER_TYPE handler_id,
    int32_t db_index,
    GENERIC_HANDLER_CALLBACK handler_proc)
{
    return handlers_table->RegisterSubPortHandler(
        gw,
        port,
        subport,
        handler_id,
        handler_proc,
        db_index);
}

extern "C" int32_t make_sc_process_uri(const char *server_name, const char *process_name, wchar_t *buffer, size_t *pbuffer_size);
extern "C" int32_t make_sc_server_uri(const char *server_name, wchar_t *buffer, size_t *pbuffer_size);

// Opens Starcounter log for writing.
uint32_t Gateway::OpenStarcounterLog()
{
	size_t host_name_size;
	wchar_t *host_name;
	uint32_t err_code;

	host_name_size = 0;
//	make_sc_server_uri(setting_sc_server_type_.c_str(), 0, &host_name_size);
	make_sc_process_uri(setting_sc_server_type_upper_.c_str(), GW_PROCESS_NAME, 0, &host_name_size);
	host_name = (wchar_t *)malloc(host_name_size * sizeof(wchar_t));
	if (host_name)
	{
//		make_sc_server_uri(setting_sc_server_type_.c_str(), host_name, &host_name_size);
		make_sc_process_uri(setting_sc_server_type_upper_.c_str(), GW_PROCESS_NAME, host_name, &host_name_size);

		err_code = sccorelog_init(0);
		if (err_code) goto err;

		err_code = sccorelog_connect_to_logs(host_name, NULL, &sc_log_handle_);
		if (err_code) goto err;

		err_code = sccorelog_bind_logs_to_dir(sc_log_handle_, setting_server_output_dir_.c_str());
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
    uint32_t err_code = sccorelog_kernel_write_to_logs(sc_log_handle_, log_type, 0, msg);

    GW_ASSERT(0 == err_code);

    err_code = sccorelog_flush_to_logs(sc_log_handle_);

    GW_ASSERT(0 == err_code);
}

#ifdef GW_TESTING_MODE

HttpTestInformation g_test_http_echo_requests[kNumTestHttpEchoRequests] =
{
    {
        "POST /gw-http-echo", 0,

        "POST /gw-http-echo HTTP/1.1\r\n"
        "Content-Type: text/html\r\n"
        "Content-Length: 8\r\n"
        "\r\n"
        "@@@@@@@@", 0, 0
    },

    {
        "POST /smc-http-echo", 0,

        "POST /smc-http-echo HTTP/1.1\r\n"
        "Content-Type: text/html\r\n"
        "Content-Length: 8\r\n"
        "\r\n"
        "@@@@@@@@", 0, 0
    },
    {
        "POST /apps-http-echo", 0,

        "POST /apps-http-echo HTTP/1.1\r\n"
        "Content-Type: text/html\r\n"
        "Content-Length: 8\r\n"
        "\r\n"
        "@@@@@@@@", 0, 0
    }
};

void Gateway::InitTestHttpEchoRequests()
{
    http_tests_information_ = g_test_http_echo_requests;

    for (int32_t i = 0; i < kNumTestHttpEchoRequests; i++)
    {
        http_tests_information_[i].method_and_uri_info_len = strlen(http_tests_information_[i].method_and_uri_info);
        http_tests_information_[i].http_request_len = strlen(http_tests_information_[i].http_request_str);
        http_tests_information_[i].http_request_insert_point = strstr(http_tests_information_[i].http_request_str, "@") - http_tests_information_[i].http_request_str;
    }
}

#endif

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
    _SetCriticalLogHandler(LogGatewayCrash, NULL);

    using namespace starcounter::network;
    uint32_t err_code;

    // Processing arguments and initializing log file.
    err_code = g_gateway.ProcessArgumentsAndInitLog(argc, argv);
    if (err_code)
        return err_code;

    // Setting I/O as low priority.
    SetPriorityClass(GetCurrentProcess(), PROCESS_MODE_BACKGROUND_BEGIN);

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