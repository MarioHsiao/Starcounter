#pragma once
#ifndef SOCKET_DATA_HPP
#define SOCKET_DATA_HPP

namespace starcounter {
namespace network {

class GatewayWorker;
class HttpWsProto;

// Socket data chunk.
class SocketDataChunk
{
    // Unique socket stamp for the session.
    uint64_t sock_stamp_;

    // Index into a cell in session array that contains the session information.
    int32_t session_index_;

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    uint32_t user_data_offset_;

    // Maximum bytes that user code can write to this chunk.
    uint32_t max_user_data_bytes_;

    // Size in bytes of written user data.
    uint32_t user_data_written_bytes_;

    // Overlapped structure used for WinSock
    // (must be 8-bytes aligned).
    OVERLAPPED ovl_;

    // Corresponding chunk index.
    core::chunk_index chunk_index_;

    // Data buffer chunk.
    AccumBuffer data_buf_;

    // Socket to which this data belongs.
    SOCKET sock_;

    // Receive flags.
    uint32_t recv_flags_;

    // Current type of network operation.
    SocketOperType type_of_network_oper_;

    // Specific socket data direction.
    bool data_to_user_flag_;

    // Is attached to socket.
    bool socket_attached_;

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

    // HTTP protocol instance.
    HttpWsProto http_ws_proto_;

    // Accept data.
    uint8_t accept_data_[ACCEPT_DATA_SIZE_BYTES];

    // Blob buffer itself.
    uint8_t data_blob_[DATA_BLOB_SIZE_BYTES];

public:

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
    void SetUserDataWrittenBytes(uint32_t newUserDataWrittenBytes)
    {
        user_data_written_bytes_ = newUserDataWrittenBytes;
    }

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    void SetUserDataOffset(uint32_t newUserDataOffset)
    {
        user_data_offset_ = newUserDataOffset;
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
    AccumBuffer* get_data_buf()
    {
        return &data_buf_;
    }

    // Getting data to user flag.
    bool data_to_user_flag()
    {
        return data_to_user_flag_;
    }

    // Getting socket attached flag.
    bool socket_attached()
    {
        return socket_attached_;
    }

    // Setting socket attached flag.
    void set_socket_attached(bool value)
    {
        socket_attached_ = value;
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

    // Returns session index.
    int32_t session_index()
    {
        return session_index_;
    }

    // Pointer to the attached session.
    SessionData* GetAttachedSession()
    {
        return g_gateway.GetSessionData(session_index_);
    }

    // Unique socket stamp for the session.
    uint64_t sock_stamp()
    {
        return sock_stamp_;
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

    // Attaches socket data to session.
    void AttachToSession(SessionData *session, GatewayWorker *gw);

    // Returns pointer to the beginning of user data.
    uint8_t* UserDataBuffer()
    {
        return ((uint8_t *)this) + user_data_offset_;
    }

    // Resets data buffer offset.
    void ResetDataBufferOffset()
    {
        user_data_offset_ = data_blob_ - ((uint8_t *)this) + blob_user_data_offset_;
    }

    // Start receiving on socket.
    uint32_t Receive(uint32_t *numBytes)
    {
        type_of_network_oper_ = RECEIVE_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return WSARecv(sock_, (WSABUF *)&data_buf_, 1, (LPDWORD)numBytes, (LPDWORD)&recv_flags_, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t Send(uint32_t *numBytes)
    {
        type_of_network_oper_ = SEND_OPER;
        memset(&ovl_, 0, OVERLAPPED_SIZE);
        return WSASend(sock_, (WSABUF *)&data_buf_, 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
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

        // Setting data direction flag.
        data_to_user_flag_ = true;
    }

    // Puts socket data from database.
    void PrepareFromDb()
    {
        // Setting network operation type.
        type_of_network_oper_ = FROM_DB_OPER;

        // Removing data direction flag.
        data_to_user_flag_ = false;
    }

    // Clones existing socket data chunk.
    SocketDataChunk *CloneReceive(GatewayWorker *gw);
};

} // namespace network
} // namespace starcounter

#endif // SOCKET_DATA_HPP