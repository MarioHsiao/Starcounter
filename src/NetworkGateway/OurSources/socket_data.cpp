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
    max_user_data_bytes_ = SOCKET_DATA_BLOB_SIZE_BYTES - blob_user_data_offset_;
    user_data_written_bytes_ = 0;
    fixed_handler_id_ = 0;

    session_.Reset();

    chunk_index_ = chunk_index;
    extra_chunk_index_ = INVALID_CHUNK_INDEX;

    flags_ = 0;
    set_to_database_direction_flag(true);

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
    flags_ = 0;
    set_to_database_direction_flag(true);

    type_of_network_oper_ = DISCONNECT_OPER;
    fixed_handler_id_ = 0;

    // Clearing attached session.
    //g_gateway.ClearSession(sessionIndex);

    // Removing reference to/from session.
    session_.Reset();

    // Resetting HTTP/WS stuff.
    http_ws_proto_.Reset();

    // Resetting buffer data pointers.
    accum_buf_.ResetBufferForNewOperation();
}

// Continues accumulation if needed.
uint32_t SocketDataChunk::ContinueAccumulation(GatewayWorker* gw, bool* is_accumulated)
{
    uint32_t err_code;
    *is_accumulated = false;

    // Checking if we have not completely accumulated all data.
    if (!accum_buf_.IsAccumulationComplete())
    {
        // Checking if current chunk buffer is full.
        if (accum_buf_.IsBufferFilled())
        {
            // Getting current chunk index.
            core::chunk_index cur_chunk_index = extra_chunk_index_;
            if (INVALID_CHUNK_INDEX == cur_chunk_index)
                cur_chunk_index = chunk_index_;

            WorkerDbInterface* worker_db = gw->GetDatabase(db_index_);

            // Getting new chunk and attaching to current one.
            shared_memory_chunk *new_smc;
            err_code = worker_db->GetOneChunkFromPrivatePool(&extra_chunk_index_, &new_smc);
            if (err_code)
                return SCERRACQUIRELINKEDCHUNKS;

            // Incrementing number of chunks.
            num_chunks_++;

            // Linking new chunk to current chunk.
            shared_memory_chunk* cur_smc = (shared_memory_chunk *)(&worker_db->get_shared_int()->chunk(cur_chunk_index));
            cur_smc->set_link(extra_chunk_index_);

            // Setting new chunk as a new buffer.
            accum_buf_.Init(bmx::MAX_DATA_BYTES_IN_CHUNK, (uint8_t*)new_smc, false);
        }
        else
        {
            // Continue receiving in existing buffer.
            accum_buf_.ContinueReceive();
        }
    }
    else
    {
        // All data has been received.
        *is_accumulated = true;

        // Resetting extra chunk index.
        extra_chunk_index_ = INVALID_CHUNK_INDEX;
    }

    return 0;
}

// Clones existing socket data chunk.
SocketDataChunk *SocketDataChunk::CloneReceive(GatewayWorker *gw)
{
    // Since another socket is going to be attached.
    set_receiving_flag(false);

    SocketDataChunk *sd = gw->CreateSocketData(sock_, port_index_, db_index_);
    sd->session_ = session_;
    sd->set_to_database_direction_flag(true);
    sd->http_ws_proto_.set_web_sockets_upgrade_flag(http_ws_proto_.get_web_sockets_upgrade_flag());

    // This socket becomes attached.
    sd->set_receiving_flag(true);

    return sd;
}

// Create WSA buffers.
uint32_t SocketDataChunk::CreateWSABuffers(
    WorkerDbInterface* worker_db,
    shared_memory_chunk* head_smc,
    uint32_t head_chunk_offset_bytes,
    uint32_t head_chunk_num_bytes,
    uint32_t total_bytes)
{
    // Getting total user data length.
    uint32_t bytes_left = total_bytes, cur_wsa_buf_offset = 0;

    // Looping through all chunks and creating corresponding
    // WSA buffers in the first chunk data blob.
    uint32_t cur_chunk_data_size = bytes_left;
    if (cur_chunk_data_size > starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK)
        cur_chunk_data_size = starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK;

    // Getting shared interface pointer.
    core::shared_interface* shared_int = worker_db->get_shared_int();

    // Extra WSABufs storage chunk.
    shared_memory_chunk* wsa_bufs_smc;
    uint32_t err_code;

    // Checking if we need to obtain an extra chunk.
    if (INVALID_CHUNK_INDEX == extra_chunk_index_)
    {
        // Getting new chunk from pool.
        err_code = worker_db->GetOneChunkFromPrivatePool(&extra_chunk_index_, &wsa_bufs_smc);
        GW_ERR_CHECK(err_code);
    }
    else
    {
        // Obtaining data address for existing extra chunk.
        wsa_bufs_smc = (shared_memory_chunk *)(&(shared_int->chunk(extra_chunk_index_)));
    }

    // Resetting number of chunks if needed.
    if (num_chunks_ > 1)
        num_chunks_ = 1;

    // Checking if head chunk is involved.
    if (head_chunk_offset_bytes)
    {
        // Pointing to current WSABUF in blob.
        WSABUF* wsa_buf = (WSABUF*) ((uint8_t*)wsa_bufs_smc + cur_wsa_buf_offset);
        wsa_buf->len = head_chunk_num_bytes;
        wsa_buf->buf = (char *)head_smc + head_chunk_offset_bytes;
        cur_wsa_buf_offset += sizeof(WSABUF);
    }

    // Until we get the last chunk in chain.
    shared_memory_chunk* smc = head_smc;
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
    if ((active_db != NULL) && (db_unique_seq_num_ == active_db->unique_num()))
    {
        ServerPort* serverPort = g_gateway.get_server_port(port_index_);
        if (serverPort != NULL)
            return true;
    }

    // Vanishing socket data.
    gw->VanishSocketData(this);

    return false;
}

// Attaches socket data to session.
void SocketDataChunk::AttachToSession(ScSessionStruct* session)
{
    // Setting session for this socket data.
    session_ = *session;

    // Attaching new session to this socket.
    g_gateway.GetSocketData(sock_)->set_session_index(session_.session_index_);
}

// Kills the session.
void SocketDataChunk::KillSession()
{
    // Killing global session.
    g_gateway.KillSession(session_.session_index_);

    // Resetting session data.
    session_.Reset();

    // Detaching session from corresponding socket.
    g_gateway.GetSocketData(sock_)->set_session_index(session_.session_index_);
}

// Resets socket session.
void SocketDataChunk::ResetSession()
{
    // Resetting session data.
    session_.Reset();

    // Detaching session from corresponding socket.
    g_gateway.GetSocketData(sock_)->set_session_index(session_.session_index_);
}

} // namespace network
} // namespace starcounter
