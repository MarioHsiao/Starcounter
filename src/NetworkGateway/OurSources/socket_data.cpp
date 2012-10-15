#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
#include "worker.hpp"
#include "worker_db_interface.hpp"

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
    extra_chunk_index_ = INVALID_CHUNK_INDEX;
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

// Getting and linking more receiving chunks.
uint32_t SocketDataChunk::GetChunks(GatewayWorker *gw, uint32_t num_bytes)
{
    WorkerDbInterface* worker_db = gw->GetDatabase(db_index_);
    if (!worker_db)
        return SCERRUNSPECIFIED;

    // Getting shared interface instance.
    core::shared_interface* shared_int = worker_db->get_shared_int();

    // Obtaining needed amount of chunks.
    core::chunk_index chunk_index = worker_db->GetLinkedChunksFromPrivatePool(num_bytes);
    if (INVALID_CHUNK_INDEX == chunk_index)
        return SCERRUNSPECIFIED;

    return 0;
}

// Create WSA buffers.
uint32_t SocketDataChunk::CreateWSABuffers(WorkerDbInterface* worker_db, shared_memory_chunk* smc)
{
    // Getting total user data length.
    uint32_t bytes_left = user_data_written_bytes_;
    uint32_t cur_wsa_buf_offset = 0;

    // Looping through all chunks and creating corresponding
    // WSA buffers in the first chunk data blob.
    uint32_t cur_chunk_data_size = bytes_left;
    if (cur_chunk_data_size > starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK)
        cur_chunk_data_size = starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK;

    // Getting shared interface pointer.
    core::shared_interface* shared_int = worker_db->get_shared_int();

    // Extra WSABufs storage chunk.
    shared_memory_chunk* wsa_bufs_smc;
    extra_chunk_index_ = worker_db->GetOneChunkFromPrivatePool(&wsa_bufs_smc);

    // Until we get the last chunk in chain.
    core::chunk_index cur_chunk_index = smc->get_link();
    while (cur_chunk_index != shared_memory_chunk::LINK_TERMINATOR)
    {
        // Obtaining chunk memory.
        smc = (shared_memory_chunk*) &(shared_int->chunk(cur_chunk_index));

        // Pointing to current WSABUF in blob.
        WSABUF* wsa_buf = (WSABUF*) ((uint8_t*)wsa_bufs_smc + cur_wsa_buf_offset);
        wsa_buf->len = cur_chunk_data_size;
        wsa_buf->buf = (char *)smc;
        cur_wsa_buf_offset += sizeof(WSABUF);

        // Decreasing number of bytes left to be processed.
        bytes_left -= cur_chunk_data_size;
        if (bytes_left < starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK)
            cur_chunk_data_size = bytes_left;

        // Getting next chunk in chain.
        cur_chunk_index = smc->get_link();

        // Increasing number of used chunks.
        num_chunks_++;
    }

    // Checking that maximum number of WSABUFs in chunk is correct.
    assert ((num_chunks_ - 1) <= starcounter::bmx::MAX_NUM_LINKED_WSABUFS);

    return 0;
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
