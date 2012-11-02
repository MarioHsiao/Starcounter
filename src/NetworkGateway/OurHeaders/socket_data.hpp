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
    SOCKET_DATA_FLAGS_RECEIVING = 2,
    SOCKET_DATA_FLAGS_NEW_SESSION = 4
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

    // Indicates if its a multi-chunk data.
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

    // Data buffer chunk.
    AccumBuffer data_buf_;

    // HTTP protocol instance.
    HttpWsProto http_ws_proto_;

    // Accept data.
    uint8_t accept_data_[ACCEPT_DATA_SIZE_BYTES];

    // Blob buffer itself.
    uint8_t data_blob_[DATA_BLOB_SIZE_BYTES];

public:

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
        return flags_ & SOCKET_DATA_FLAGS_RECEIVING;
    }

    // Setting receiving flag.
    void set_receiving_flag(bool value)
    {
        if (value)
            flags_ |= SOCKET_DATA_FLAGS_RECEIVING;
        else
            flags_ &= ~SOCKET_DATA_FLAGS_RECEIVING;
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

    // Getting session index.
    uint32_t get_session_index()
    {
        return session_.session_index_;
    }

    // Getting Apps unique number.
    apps_unique_session_num_type get_apps_unique_session_num()
    {
        return session_.apps_unique_session_num_;
    }

    // Setting Apps unique number.
    void set_apps_unique_session_num(apps_unique_session_num_type apps_unique_session_num)
    {
        session_.apps_unique_session_num_ = apps_unique_session_num;
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

    // Set new chunk index.
    void set_chunk_index(core::chunk_index chunk_index)
    {
        chunk_index_ = chunk_index;
    }

    // Getting data blob pointer.
    uint8_t* get_data_blob()
    {
        return data_blob_;
    }

    // Returns number of chunks flag.
    uint32_t get_num_chunks()
    {
        return num_chunks_;
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
        return &data_buf_;
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

    // Getting and linking more receiving chunks.
    uint32_t GetChunks(GatewayWorker *gw, uint32_t num_bytes);

    // Pointer to the attached session.
    ScSessionStruct* GetAttachedSession()
    {
        return g_gateway.GetSessionData(session_.session_index_);
    }

    // Extra chunk index.
    core::chunk_index& extra_chunk_index()
    {
        return extra_chunk_index_;
    }

    // Corresponding chunk index.
    core::chunk_index& chunk_index()
    {
        return chunk_index_;
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
        if (0 == fixed_handler_id_)
        {
            return g_gateway.get_server_port(port_index_)->get_port_handlers()->RunHandlers(gw, this);
        }
        else // We have a determined handler id.
        {
            return g_gateway.GetDatabase(db_index_)->get_user_handlers()->get_handler_list(fixed_handler_id_)->RunHandlers(gw, this);
        }
    }

    // Checking that database and corresponding port handler exists.
    bool CheckSocketIsValid(GatewayWorker* gw);

    // Resets socket session.
    void ResetSession();

    // Kills the session.
    void KillSession();

    // Attaches socket data to session.
    void AttachToSession(ScSessionStruct* session);

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
        max_user_data_bytes_ = DATA_BLOB_SIZE_BYTES - blob_user_data_offset_;
    }

    // Start receiving on socket.
    uint32_t ReceiveSingleChunk(uint32_t *numBytes)
    {
        type_of_network_oper_ = RECEIVE_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return WSARecv(sock_, (WSABUF *)&data_buf_, 1, (LPDWORD)numBytes, (LPDWORD)&recv_flags_, &ovl_, NULL);
    }

    // Start receiving on socket using multiple chunks.
    uint32_t ReceiveMultipleChunks(core::shared_interface* shared_int, uint32_t *numBytes)
    {
        type_of_network_oper_ = RECEIVE_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return WSARecv(sock_, (WSABUF*)&(shared_int->chunk(extra_chunk_index_)), num_chunks_ - 1, (LPDWORD)numBytes, (LPDWORD)&recv_flags_, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t SendSingleChunk(uint32_t *numBytes)
    {
        type_of_network_oper_ = SEND_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return WSASend(sock_, (WSABUF *)&data_buf_, 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t SendMultipleChunks(core::shared_interface* shared_int, uint32_t *numBytes)
    {
        type_of_network_oper_ = SEND_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return WSASend(sock_, (WSABUF*)&(shared_int->chunk(extra_chunk_index_)), num_chunks_ - 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
    }

    // Start accepting on socket.
    uint32_t Accept(GatewayWorker* gw)
    {
        type_of_network_oper_ = ACCEPT_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);

        // Start tracking this socket.
        g_gateway.GetDatabase(db_index_)->TrackSocket(sock_);

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
    SocketDataChunk *CloneReceive(GatewayWorker *gw);
};

} // namespace network
} // namespace starcounter

#endif // SOCKET_DATA_HPP