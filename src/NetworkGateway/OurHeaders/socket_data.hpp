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
    SOCKET_DATA_FLAGS_RECEIVING_STATE = 2,
    SOCKET_DATA_FLAGS_NEW_SESSION = 4,
    SOCKET_DATA_FLAGS_ACCUMULATING_STATE = 8,
    SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND = 16,
    HTTP_WS_FLAGS_UPGRADE = 32,
    HTTP_WS_FLAGS_COMPLETE_HEADER = 64
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

    // HTTP protocol instance.
    HttpWsProto http_ws_proto_;

    // Accept data.
    uint8_t accept_data_[ACCEPT_DATA_SIZE_BYTES];

    // Blob buffer itself.
    uint8_t data_blob_[SOCKET_DATA_BLOB_SIZE_BYTES];

public:

    // Checks if socket data is in correct state.
    uint32_t AssertCorrectState()
    {
        uint8_t* sd = (uint8_t*) this;

        assert((data_blob_ - sd) == SOCKET_DATA_BLOB_OFFSET_BYTES);

        assert(((uint8_t*)http_ws_proto_.get_http_request() - sd) == bmx::SOCKET_DATA_HTTP_REQUEST_OFFSET);

        assert(((uint8_t*)(&accum_buf_) - sd) == bmx::SOCKET_DATA_NUM_CLONE_BYTES);

        assert(((uint8_t*)&num_chunks_ - sd) == bmx::SOCKET_DATA_NUM_CHUNKS_OFFSET);

        assert(((uint8_t*)&max_user_data_bytes_ - sd) == (bmx::MAX_USER_DATA_BYTES_OFFSET - bmx::BMX_HEADER_MAX_SIZE_BYTES));

        assert(((uint8_t*)&user_data_written_bytes_ - sd) == (bmx::USER_DATA_WRITTEN_BYTES_OFFSET - bmx::BMX_HEADER_MAX_SIZE_BYTES));

        return 0;
    }

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

    // Getting receiving flag.
    bool get_receiving_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_RECEIVING_STATE;
    }

    // Setting receiving flag.
    void set_receiving_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_RECEIVING_STATE;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_RECEIVING_STATE;
    }

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

    // Getting session index.
    uint32_t get_session_index()
    {
        return session_.session_index_;
    }

    // Getting Apps unique session number.
    apps_unique_session_num_type get_apps_unique_session_num()
    {
        return session_.apps_unique_session_num_;
    }

    // Getting Apps session salt.
    session_salt_type get_apps_session_salt()
    {
        return session_.apps_session_salt_;
    }

    // Setting Apps unique session number.
    void set_apps_unique_session_num(apps_unique_session_num_type apps_unique_session_num)
    {
        session_.apps_unique_session_num_ = apps_unique_session_num;
    }

    // Setting Apps sessions salt.
    void set_apps_session_salt(session_salt_type apps_session_salt)
    {
        session_.apps_session_salt_ = apps_session_salt;
    }

    // Getting session salt.
    uint64_t get_session_salt()
    {
        return session_.session_salt_;
    }

    // Returns socket.
    SOCKET get_socket()
    {
        return sock_;
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

    // Blob buffer itself.
    uint8_t* data_blob()
    {
        return data_blob_;
    }

    // Overlapped structure used for WinSock.
    OVERLAPPED* get_ovl()
    {
        return &ovl_;
    }

    // Current type of network operation.
    SocketOperType type_of_network_oper()
    {
        return type_of_network_oper_;
    }

    // Accept data.
    uint8_t* const accept_data()
    {
        return accept_data_;
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

    // Socket to which this data belongs.
    SOCKET sock()
    {
        return sock_;
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
    uint64_t db_unique_seq_num()
    {
        return db_unique_seq_num_;
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

    // Does general data processing using port handlers.
    uint32_t RunHandlers(GatewayWorker *gw)
    {
        // Checking if handler id is not determined yet.
        if (bmx::INVALID_HANDLER_ID == fixed_handler_id_)
        {
            return g_gateway.get_server_port(port_index_)->get_port_handlers()->RunHandlers(gw, this);
        }
        else // We have a determined handler id.
        {
            return g_gateway.GetDatabase(db_index_)->get_user_handlers()->get_handler_list(fixed_handler_id_)->RunHandlers(gw, this);
        }
    }

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
    uint32_t ReceiveSingleChunk(uint32_t *numBytes)
    {
        type_of_network_oper_ = RECEIVE_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return WSARecv(sock_, (WSABUF *)&accum_buf_, 1, (LPDWORD)numBytes, (LPDWORD)&recv_flags_, &ovl_, NULL);
    }

    // Start receiving on socket using multiple chunks.
    uint32_t ReceiveMultipleChunks(core::shared_interface* shared_int, uint32_t *numBytes)
    {
        type_of_network_oper_ = RECEIVE_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

        // NOTE: Need to subtract two chunks from being included in receive.
        return WSARecv(sock_, (WSABUF*)&(shared_int->chunk(extra_chunk_index_)), num_chunks_ - 2, (LPDWORD)numBytes, (LPDWORD)&recv_flags_, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t SendSingleChunk(uint32_t *numBytes)
    {
        type_of_network_oper_ = SEND_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return WSASend(sock_, (WSABUF *)&accum_buf_, 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t SendMultipleChunks(core::shared_interface* shared_int, uint32_t *numBytes)
    {
        type_of_network_oper_ = SEND_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

        // NOTE: Need to subtract two chunks from being included in send.
        return WSASend(sock_, (WSABUF*)&(shared_int->chunk(extra_chunk_index_)), num_chunks_ - 2, (LPDWORD)numBytes, 0, &ovl_, NULL);
    }

    // Start accepting on socket.
    uint32_t Accept(GatewayWorker* gw)
    {
        type_of_network_oper_ = ACCEPT_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

        // Start tracking this socket.
        g_gateway.GetDatabase(db_index_)->TrackSocket(sock_, port_index_);
        g_gateway.TrackSocket(sock_, port_index_);

        // Running Windows API AcceptEx function.
        return AcceptExFunc(
            g_gateway.get_server_port(port_index_)->get_port_socket(),
            sock_,
            accept_data_,
            0,
            SOCKADDR_SIZE_EXT,
            SOCKADDR_SIZE_EXT,
            NULL,
            &ovl_);
    }

    // Start connecting on socket.
    uint32_t Connect(sockaddr_in *serverAddr)
    {
        type_of_network_oper_ = CONNECT_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return ConnectExFunc(sock_, (SOCKADDR *) serverAddr, sizeof(sockaddr_in), NULL, 0, NULL, &ovl_);
    }

    // Start disconnecting socket.
    uint32_t Disconnect()
    {
        type_of_network_oper_ = DISCONNECT_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return DisconnectExFunc(sock_, &ovl_, TF_REUSE_SOCKET, 0);
    }

    // Puts socket data to database.
    void PrepareToDb()
    {
        // Setting network operation type.
        type_of_network_oper_ = TO_DB_OPER;

        // Setting database data direction flag.
        set_to_database_direction_flag(true);
    }

    // Puts socket data from database.
    void PrepareFromDb()
    {
        // Setting network operation type.
        type_of_network_oper_ = FROM_DB_OPER;

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

    // Kills the global and  session.
    void KillGlobalAndSdSession()
    {
        // Killing global session.
        g_gateway.KillSession(session_.session_index_);

        // Resetting session data.
        ResetSdSession();
    }

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