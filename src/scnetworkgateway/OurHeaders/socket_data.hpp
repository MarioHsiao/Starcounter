#pragma once
#ifndef SOCKET_DATA_HPP
#define SOCKET_DATA_HPP

namespace starcounter {
namespace network {

class GatewayWorker;
class HttpWsProto;

enum SOCKET_DATA_FLAGS
{
    SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION = 1,
    SOCKET_DATA_FLAGS_SOCKET_REPRESENTER = 2,
    SOCKET_DATA_FLAGS_NEW_SESSION = 4,
    SOCKET_DATA_FLAGS_ACCUMULATING_STATE = 8,
    SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND = 16,
    SOCKET_DATA_FLAGS_ACTIVE_CONN = 32,
    SOCKET_DATA_FLAGS_JUST_SEND = 64,
    HTTP_WS_FLAGS_UPGRADE = 128,
    HTTP_WS_FLAGS_COMPLETE_HEADER = 256,
    HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET = 512,
    HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO = 1024
};

// Socket data chunk.
class WorkerDbInterface;
class SocketDataChunk
{
    // Overlapped structure used for WinSock
    // (must be 8-bytes aligned).
    OVERLAPPED ovl_;

    // Starcounter session information.
    ScSessionStruct session_;

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    uint32_t user_data_offset_;

    // Maximum bytes that user code can write to this chunk.
    uint32_t max_user_data_bytes_;

    // Size in bytes of written user data.
    uint32_t user_data_written_bytes_;

    // Corresponding chunk index.
    core::chunk_index chunk_index_;

    // Extra chunk index.
    core::chunk_index extra_chunk_index_;

    // Indicates how many chunks are associated with this socket data (normally 1).
    uint32_t num_chunks_;

    // Socket to which this data belongs.
    SOCKET sock_;

    // Unique number for socket.
    session_salt_type unique_socket_id_;

    // Receive flags.
    uint32_t recv_flags_;

    // Current type of network operation.
    SocketOperType type_of_network_oper_;

    // Socket data flags.
    uint32_t flags_;

    // Port handlers index.
    int32_t port_index_;

    // Determined handler id.
    BMX_HANDLER_TYPE fixed_handler_id_;

    // Index into databases array.
    int32_t db_index_;

    // Unique sequence number for the database.
    uint64_t db_unique_seq_num_;

    // Blob user data offset.
    int32_t blob_user_data_offset_;

    // Accumulative buffer chunk.
    AccumBuffer accum_buf_;

    // Proxy socket information.
    SOCKET proxy_sock_;

    // HTTP protocol instance.
    HttpWsProto http_ws_proto_;

    // Accept data.
    uint8_t accept_data_or_params_[ACCEPT_DATA_SIZE_BYTES];

    // Blob buffer itself.
    uint8_t data_blob_[SOCKET_DATA_BLOB_SIZE_BYTES];

public:

#ifdef GW_LOOPED_TEST_MODE

    // Pushing given sd to network emulation queue.
    void PushToMeasuredNetworkEmulationQueue(GatewayWorker* gw);
    void PushToPreparationNetworkEmulationQueue(GatewayWorker* gw);

#endif

    // Checks if socket data is in correct state.
    uint32_t AssertCorrectState()
    {
        uint8_t* sd = (uint8_t*) this;

        GW_ASSERT(SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_JUST_SEND == starcounter::bmx::SOCKET_DATA_FLAGS_JUST_SEND);

        GW_ASSERT((accept_data_or_params_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_PARAMS_INFO);

        GW_ASSERT((data_blob_ - sd) == SOCKET_DATA_BLOB_OFFSET_BYTES);

        GW_ASSERT(((uint8_t*)&flags_ - sd) == (bmx::CHUNK_OFFSET_SOCKET_FLAGS - bmx::BMX_HEADER_MAX_SIZE_BYTES));

        GW_ASSERT(((uint8_t*)http_ws_proto_.get_http_request() - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_HTTP_REQUEST);

        GW_ASSERT(((uint8_t*)(&accum_buf_) - sd) == bmx::SOCKET_DATA_NUM_CLONE_BYTES);

        GW_ASSERT(((uint8_t*)&num_chunks_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_NUM_CHUNKS);

        GW_ASSERT(((uint8_t*)&max_user_data_bytes_ - sd) == (bmx::CHUNK_OFFSET_MAX_USER_DATA_BYTES - bmx::BMX_HEADER_MAX_SIZE_BYTES));

        GW_ASSERT(((uint8_t*)&user_data_written_bytes_ - sd) == (bmx::CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES - bmx::BMX_HEADER_MAX_SIZE_BYTES));

        return 0;
    }

#ifdef GW_SOCKET_ID_CHECK
    // Setting new unique socket number.
    void SetUniqueSocketId()
    {
        unique_socket_id_ = g_gateway.SetUniqueSocketId(sock_);
    }

    // Checking if unique socket number is correct.
    bool CompareUniqueSocketId()
    {
        return g_gateway.CompareUniqueSocketId(sock_, unique_socket_id_);
    }
#endif

    // Returns all linked chunks except the main one.
    uint32_t ReturnExtraLinkedChunks(GatewayWorker* gw);

    // Setting fixed handler id.
    void set_fixed_handler_id(BMX_HANDLER_TYPE fixed_handler_id)
    {
        fixed_handler_id_ = fixed_handler_id;
    }

    // Getting fixed handler id.
    BMX_HANDLER_TYPE get_fixed_handler_id()
    {
        return fixed_handler_id_;
    }

    // Continues fill up if needed.
    uint32_t ContinueAccumulation(GatewayWorker* gw, bool* is_accumulated);

    // Getting to database direction flag.
    bool get_to_database_direction_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
    }

    // Setting to database direction flag.
    void set_to_database_direction_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
    }

    // Getting socket just send flag.
    bool get_socket_just_send_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_JUST_SEND;
    }

    // Setting socket just send flag.
    void set_socket_just_send_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_JUST_SEND;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_JUST_SEND;
    }

    // Getting socket representer flag.
    bool get_socket_representer_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_SOCKET_REPRESENTER;
    }

    // Setting socket representer flag.
    void set_socket_representer_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_SOCKET_REPRESENTER;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_SOCKET_REPRESENTER;
    }

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Getting socket diagnostics active connection flag.
    bool get_socket_diag_active_conn_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_ACTIVE_CONN;
    }

    // Getting socket diagnostics active connection flag.
    void set_socket_diag_active_conn_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_ACTIVE_CONN;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_ACTIVE_CONN;
    }

#endif

#ifdef GW_PROXY_MODE

    // Getting proxying flag.
    bool get_proxied_server_socket_flag()
    {
        return flags_ & HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET;
    }

    // Setting proxying flag.
    void set_proxied_server_socket_flag(bool value)
    {
        if (value)
            flags_ |= HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET;
        else
            flags_ &= ~HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET;
    }

    // Getting proxying unknown protocol flag.
    bool get_unknown_proxied_proto_flag()
    {
        return flags_ & HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO;
    }

    // Setting proxying unknown protocol flag.
    void set_unknown_proxied_proto_flag(bool value)
    {
        if (value)
            flags_ |= HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO;
        else
            flags_ &= ~HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO;
    }

#endif

    // Getting accumulating flag.
    bool get_accumulating_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_ACCUMULATING_STATE;
    }

    // Setting accumulating flag.
    void set_accumulating_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_ACCUMULATING_STATE;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_ACCUMULATING_STATE;
    }

    // Getting disconnect after send flag.
    bool get_disconnect_after_send_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND;
    }

    // Setting disconnect after send flag.
    void set_disconnect_after_send_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND;
    }

    // Getting new session flag.
    bool get_new_session_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_NEW_SESSION;
    }

    // Setting new session flag.
    void set_new_session_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_NEW_SESSION;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_NEW_SESSION;
    }

    // Getting WebSocket upgrade flag.
    bool get_web_sockets_upgrade_flag()
    {
        return flags_ & HTTP_WS_FLAGS_UPGRADE;
    }

    // Setting WebSocket upgrade flag.
    void set_web_sockets_upgrade_flag(bool value)
    {
        if (value)
            flags_ |= HTTP_WS_FLAGS_UPGRADE;
        else
            flags_ &= ~HTTP_WS_FLAGS_UPGRADE;
    }

    // Getting complete header flag.
    bool get_complete_header_flag()
    {
        return flags_ & HTTP_WS_FLAGS_COMPLETE_HEADER;
    }

    // Setting complete header flag.
    void set_complete_header_flag(bool value)
    {
        if (value)
            flags_ |= HTTP_WS_FLAGS_COMPLETE_HEADER;
        else
            flags_ &= ~HTTP_WS_FLAGS_COMPLETE_HEADER;
    }

    // Getting scheduler id.
    scheduler_id_type get_scheduler_id()
    {
        return session_.scheduler_id_;
    }

    // Setting scheduler id.
    void set_scheduler_id(scheduler_id_type scheduler_id)
    {
        session_.scheduler_id_ = scheduler_id;
    }

    // Getting session index.
    session_index_type get_session_index()
    {
        return session_.linear_index_;
    }

    // Getting session salt.
    session_salt_type get_session_salt()
    {
        return session_.random_salt_;
    }

    // Determines if this sd has active session attached.
    bool HasActiveSession()
    {
        return INVALID_SESSION_INDEX != session_.linear_index_;
    }

#ifndef GW_NEW_SESSIONS_APPROACH

    // Getting session index.
    session_index_type get_session_index()
    {
        return session_.gw_session_index_;
    }

    // Getting Apps unique session number.
    apps_unique_session_num_type get_apps_unique_session_num()
    {
        return session_.apps_unique_session_num_;
    }

    // Getting Apps session salt.
    session_salt_type get_apps_session_salt()
    {
        return session_.random_salt_;
    }

    // Setting Apps unique session number.
    void set_apps_unique_session_num(apps_unique_session_num_type apps_unique_session_num)
    {
        session_.apps_unique_session_num_ = apps_unique_session_num;
    }

    // Setting Apps sessions salt.
    void set_apps_session_salt(session_salt_type apps_session_salt)
    {
        session_.random_salt_ = apps_session_salt;
    }

    // Getting session salt.
    session_salt_type get_session_salt()
    {
        return session_.gw_session_salt_;
    }

#endif

    // Getting unique id.
    session_salt_type get_unique_socket_id()
    {
        return unique_socket_id_;
    }

    // Returns socket.
    SOCKET get_socket()
    {
        return sock_;
    }

    // Sets socket.
    void set_socket(SOCKET sock)
    {
        sock_ = sock;
    }

    // Returns proxy socket.
    SOCKET get_proxy_socket()
    {
        return proxy_sock_;
    }

    // Sets proxy socket.
    void set_proxy_socket(SOCKET sock)
    {
        proxy_sock_ = sock;
    }

    // Returns SMC representing this chunk.
    shared_memory_chunk* get_smc()
    {
        return (shared_memory_chunk*)((uint8_t*)this - bmx::BMX_HEADER_MAX_SIZE_BYTES);
    }

    // Set new chunk index.
    void set_chunk_index(core::chunk_index chunk_index)
    {
        chunk_index_ = chunk_index;
    }

    // Gets chunk index.
    core::chunk_index& get_chunk_index()
    {
        return chunk_index_;
    }

    // Getting data blob pointer.
    uint8_t* get_data_blob()
    {
        return data_blob_;
    }

    // Gets chunk data beginning.
    uint8_t* get_chunk_start()
    {
        return (uint8_t*)(this) - bmx::BMX_HEADER_MAX_SIZE_BYTES;
    }

    // Returns number of used chunks.
    uint32_t get_num_chunks()
    {
        return num_chunks_;
    }

    // Setting number of used chunks.
    void set_num_chunks(uint32_t value)
    {
        num_chunks_ = value;
    }

    // Getting and linking more receiving chunks.
    uint32_t GetReceivingChunks(GatewayWorker *gw, uint32_t num_bytes);

    // Create WSA buffers.
    uint32_t CreateWSABuffers(
        WorkerDbInterface* worker_db,
        shared_memory_chunk* head_smc,
        uint32_t head_chunk_offset_bytes,
        uint32_t head_chunk_num_bytes,
        uint32_t total_bytes);

    // Get Http protocol instance.
    HttpWsProto* get_http_ws_proto()
    {
        return &http_ws_proto_;
    }

    // Overlapped structure used for WinSock.
    OVERLAPPED* get_ovl()
    {
        return &ovl_;
    }

    // Current type of network operation.
    SocketOperType get_type_of_network_oper()
    {
        return type_of_network_oper_;
    }

    // Current type of network operation.
    void set_type_of_network_oper(SocketOperType type_of_network_oper)
    {
        type_of_network_oper_ = type_of_network_oper;
    }

    // Accept data.
    uint8_t* const get_accept_data()
    {
        return accept_data_or_params_;
    }

    // Size in bytes of written user data.
    void set_user_data_written_bytes(uint32_t user_data_written_bytes)
    {
        user_data_written_bytes_ = user_data_written_bytes;
    }

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    void set_user_data_offset(uint32_t user_data_offset)
    {
        user_data_offset_ = user_data_offset;
    }

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    uint32_t get_user_data_offset()
    {
        return user_data_offset_;
    }

    // Setting maximum user data size.
    void set_max_user_data_bytes(uint32_t max_user_data_bytes)
    {
        max_user_data_bytes_ = max_user_data_bytes;
    }

    // Size in bytes of written user data.
    uint32_t get_user_data_written_bytes()
    {
        return user_data_written_bytes_;
    }

    // Gets index of the server port.
    int32_t get_port_index()
    {
        return port_index_;
    }

    // Data buffer chunk.
    AccumBuffer* get_accum_buf()
    {
        return &accum_buf_;
    }

    // Index into databases array.
    uint16_t get_db_index()
    {
        return db_index_;
    }

    // Unique database sequence number.
    uint64_t get_db_unique_seq_num()
    {
        return db_unique_seq_num_;
    }

    // Setting unique sequence number.
    void set_db_unique_seq_num(uint64_t seq_num)
    {
        db_unique_seq_num_ = seq_num;
    }

    // Exchanges sockets during proxying.
    void ExchangeToProxySocket()
    {
        // Getting corresponding proxy socket id.
        session_salt_type proxy_unique_socket_id = g_gateway.GetUniqueSocketId(proxy_sock_);

#ifdef GW_SOCKET_DIAG
        GW_COUT << "Exchanging sockets: " << sock_ << "<->" << proxy_sock_ << " and ids " <<
            unique_socket_id_ << "<->" << proxy_unique_socket_id << GW_ENDL;
#endif

        // Switching places with current and proxy socket.
        SOCKET tmp_sock = sock_;
        sock_ = proxy_sock_;
        proxy_sock_ = tmp_sock;

        // Setting unique socket id.
        unique_socket_id_ = proxy_unique_socket_id;
    }

    // Attaching to certain database.
    void AttachToDatabase(int32_t db_index)
    {
        db_unique_seq_num_ = g_gateway.GetDatabase(db_index)->unique_num();
        db_index_ = db_index;
    }

    // Initialization.
    void Init(
        SOCKET sock,
        int32_t port_index,
        int32_t db_index,
        core::chunk_index chunk_index);

    // Resetting socket.
    void Reset();

    // Checking that database and corresponding port handler exists.
    bool ForceSocketDataValidity(GatewayWorker* gw);

    // Returns pointer to the beginning of user data.
    uint8_t* UserDataBuffer()
    {
        return ((uint8_t *)this) + user_data_offset_;
    }

    // Resets user data offset.
    void ResetUserDataOffset()
    {
        user_data_offset_ = data_blob_ - ((uint8_t *)this) + blob_user_data_offset_;
    }

    // Resets max user data buffer.
    void ResetMaxUserDataBytes()
    {
        max_user_data_bytes_ = SOCKET_DATA_BLOB_SIZE_BYTES - blob_user_data_offset_;
    }

    // Start receiving on socket.
    uint32_t ReceiveSingleChunk(GatewayWorker *gw, uint32_t *num_bytes)
    {
        type_of_network_oper_ = RECEIVE_SOCKET_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return WSA_IO_PENDING;
#endif

        return WSARecv(sock_, (WSABUF *)&accum_buf_, 1, (LPDWORD)num_bytes, (LPDWORD)&recv_flags_, &ovl_, NULL);
    }

    // Start receiving on socket using multiple chunks.
    uint32_t ReceiveMultipleChunks(GatewayWorker *gw, core::shared_interface* shared_int, uint32_t* num_bytes)
    {
        type_of_network_oper_ = RECEIVE_SOCKET_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return WSA_IO_PENDING;
#endif

        // NOTE: Need to subtract two chunks from being included in receive.
        return WSARecv(sock_, (WSABUF*)&(shared_int->chunk(extra_chunk_index_)), num_chunks_ - 1, (LPDWORD)num_bytes, (LPDWORD)&recv_flags_, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t SendSingleChunk(GatewayWorker* gw, uint32_t *numBytes)
    {
        type_of_network_oper_ = SEND_SOCKET_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return WSA_IO_PENDING;
#endif

        return WSASend(sock_, (WSABUF *)&accum_buf_, 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t SendMultipleChunks(GatewayWorker* gw, core::shared_interface* shared_int, uint32_t *numBytes)
    {
        type_of_network_oper_ = SEND_SOCKET_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return WSA_IO_PENDING;
#endif

        // NOTE: Need to subtract one chunks from being included in send.
        return WSASend(sock_, (WSABUF*)&(shared_int->chunk(extra_chunk_index_)), num_chunks_ - 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
    }

    // Start accepting on socket.
    uint32_t Accept(GatewayWorker* gw)
    {
        type_of_network_oper_ = ACCEPT_SOCKET_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToPreparationNetworkEmulationQueue(gw);
        return FALSE;
#endif

        // Running Windows API AcceptEx function.
        return AcceptExFunc(
            g_gateway.get_server_port(port_index_)->get_listening_sock(),
            sock_,
            accept_data_or_params_,
            0,
            SOCKADDR_SIZE_EXT,
            SOCKADDR_SIZE_EXT,
            NULL,
            &ovl_);
    }

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    uint32_t SetAcceptSocketOptions()
    {
        SOCKET listening_sock = g_gateway.get_server_port(port_index_)->get_listening_sock();

#ifndef GW_LOOPED_TEST_MODE
        if (setsockopt(sock_, SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT, (char *)&listening_sock, sizeof(listening_sock)))
            return SCERRGWACCEPTEXFAILED;
#endif

        return 0;
    }

    // Start connecting on socket.
    uint32_t Connect(GatewayWorker* gw, sockaddr_in *serverAddr)
    {
        type_of_network_oper_ = CONNECT_SOCKET_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToPreparationNetworkEmulationQueue(gw);
        return FALSE;
#endif

        return ConnectExFunc(sock_, (SOCKADDR *) serverAddr, sizeof(sockaddr_in), NULL, 0, NULL, &ovl_);
    }

    // Start disconnecting socket.
    uint32_t Disconnect(GatewayWorker *gw)
    {
        type_of_network_oper_ = DISCONNECT_SOCKET_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return FALSE;
#endif

        return DisconnectExFunc(sock_, &ovl_, TF_REUSE_SOCKET, 0);
    }

    // Puts socket data to database.
    void PrepareToDb()
    {
        // Setting database data direction flag.
        set_to_database_direction_flag(true);
    }

    // Puts socket data from database.
    void PrepareFromDb()
    {
        // Removing database data direction flag.
        set_to_database_direction_flag(false);
    }

    // Clones existing socket data chunk.
    uint32_t CloneToReceive(GatewayWorker *gw);

    // Attaches session to socket data.
    void AssignSession(ScSessionStruct& session)
    {
        // Setting session for this socket data.
        session_ = session;
    }

    // Getting session structure.
    ScSessionStruct* GetSessionStruct()
    {
        return &session_;
    }

#ifndef GW_NEW_SESSIONS_APPROACH

    // Kills the global and  session.
    void KillGlobalAndSdSession(bool* was_killed)
    {
        // Killing global session.
        g_gateway.KillSession(session_.gw_session_index_, was_killed);

        // Resetting session data.
        ResetSdSession();
    }

#endif

    // Resets socket data session.
    void ResetSdSession()
    {
        // Resetting session data.
        session_.Reset();
    }
};

} // namespace network
} // namespace starcounter

#endif // SOCKET_DATA_HPP