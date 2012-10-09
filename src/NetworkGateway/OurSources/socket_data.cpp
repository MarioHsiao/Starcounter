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

// Initialization.
void SocketDataChunk::Init(
    SOCKET sock,
    int32_t port_index,
    int32_t db_index,
    core::chunk_index chunk_index)
{
    sock_ = sock;

    // Obtaining corresponding port handler.
    ServerPort* server_port = g_gateway.get_server_port(port_index);
    port_index_ = port_index;
    db_index_ = db_index;
    db_unique_seq_num_ = g_gateway.GetDatabase(db_index_)->unique_num();
    blob_user_data_offset_ = server_port->get_blob_user_data_offset();

    // Resets data buffer offset.
    ResetUserDataOffset();

    // Calculating maximum size of user data.
    max_user_data_bytes_ = DATA_BLOB_SIZE_BYTES - blob_user_data_offset_;
    user_data_written_bytes_ = 0;
    fixed_handler_id_ = 0;

    sock_stamp_ = 0;
    chunk_index_ = chunk_index;
    data_to_user_flag_ = true;

    socket_attached_ = false;
    session_index_ = INVALID_SESSION_INDEX;

    type_of_network_oper_ = UNKNOWN_OPER;
    recv_flags_ = 0;
    num_chunks_ = 1;

    // Initializing HTTP/WEBSOCKETS data structures.
    http_ws_proto_.Init();
}

// Resetting socket.
void SocketDataChunk::Reset()
{
    // Resetting flags.
    data_to_user_flag_ = true;

    socket_attached_ = false;

    type_of_network_oper_ = DISCONNECT_OPER;

    // Clearing attached session.
    //g_gateway.ClearSession(sessionIndex);

    // Removing reference to/from session.
    session_index_ = INVALID_SESSION_INDEX;

    // Resetting HTTP/WS stuff.
    http_ws_proto_.Reset();

    // Resetting buffer data pointers.
    data_buf_.ResetBufferForNewOperation();
}

// Clones existing socket data chunk.
SocketDataChunk *SocketDataChunk::CloneReceive(GatewayWorker *gw)
{
    // Since another socket is going to be attached.
    socket_attached_ = false;

    SocketDataChunk *sd = gw->CreateSocketData(sock_, port_index_, db_index_);
    sd->sock_stamp_ = sock_stamp_;
    sd->session_index_ = session_index_;
    sd->data_to_user_flag_ = true;
    sd->http_ws_proto_.set_web_sockets_upgrade(http_ws_proto_.get_web_sockets_upgrade());

    // This socket becomes attached.
    sd->socket_attached_ = true;

    return sd;
}

// Checking that database and corresponding port handler exists.
bool SocketDataChunk::CheckSocketIsValid(GatewayWorker* gw)
{
    // Checking if socket should be deleted.
    if (g_gateway.ShouldSocketBeDeleted(sock_))
    {
        // Vanishing socket.
        gw->VanishSocketData(this);

        return false;
    }

    // Checking the database.
    ActiveDatabase* active_db = g_gateway.GetDatabase(db_index_);

    // Checking that attached database is correct.
    if ((active_db != NULL) && (active_db->unique_num() == db_unique_seq_num_))
    {
        ServerPort* serverPort = g_gateway.get_server_port(port_index_);
        if (serverPort != NULL)
            return true;
    }

    // Vanishing socket.
    gw->VanishSocketData(this);

    return false;
}

// Attaches socket data to session.
void SocketDataChunk::AttachToSession(SessionData *session, GatewayWorker *gw)
{
    g_gateway.AttachSocketToSession(session, sock_, gw->Random->uint64());
    session_index_ = session->session_index();
    sock_stamp_ = session->socket_stamp();
}

} // namespace network
} // namespace starcounter
