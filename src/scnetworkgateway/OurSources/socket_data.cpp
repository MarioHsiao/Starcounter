#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
#include "worker_db_interface.hpp"
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
    proxy_sock_ = INVALID_SOCKET;

    // Obtaining corresponding port handler.
    ServerPort* server_port = g_gateway.get_server_port(port_index);
    port_index_ = port_index;
    db_index_ = db_index;
    db_unique_seq_num_ = g_gateway.GetDatabase(db_index_)->get_unique_num();
    blob_user_data_offset_ = server_port->get_blob_user_data_offset();

    // Resets data buffer offset.
    ResetUserDataOffset();

    // Calculating maximum size of user data.
    max_user_data_bytes_ = SOCKET_DATA_BLOB_SIZE_BYTES - blob_user_data_offset_;
    user_data_written_bytes_ = 0;
    fixed_handler_id_ = bmx::BMX_INVALID_HANDLER_INFO;

    session_.Reset();

    chunk_index_ = chunk_index;
    extra_chunk_index_ = INVALID_CHUNK_INDEX;

    flags_ = 0;
    set_to_database_direction_flag(true);

    type_of_network_oper_ = UNKNOWN_SOCKET_OPER;
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

    proxy_sock_ = INVALID_SOCKET;

    type_of_network_oper_ = DISCONNECT_SOCKET_OPER;

    fixed_handler_id_ = bmx::BMX_INVALID_HANDLER_INFO;

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

            WorkerDbInterface* worker_db = gw->GetWorkerDb(db_index_);

            // Getting new chunk and attaching to current one.
            shared_memory_chunk *new_smc;
            err_code = worker_db->GetOneChunkFromPrivatePool(&extra_chunk_index_, &new_smc);
            if (err_code)
                return err_code;

            // Incrementing number of chunks.
            num_chunks_++;

            // Linking new chunk to current chunk.
            shared_memory_chunk* cur_smc = (shared_memory_chunk *)(&worker_db->get_shared_int()->chunk(cur_chunk_index));
            cur_smc->set_link(extra_chunk_index_);

            // Setting new chunk as a new buffer.
            accum_buf_.Init(bmx::CHUNK_MAX_DATA_BYTES, (uint8_t*)new_smc, false);
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

// Clones existing socket data chunk for receiving.
uint32_t SocketDataChunk::CloneToReceive(GatewayWorker *gw)
{
    SocketDataChunk* sd_clone = NULL;
    uint32_t err_code = gw->CreateSocketData(sock_, port_index_, db_index_, sd_clone);
    GW_ERR_CHECK(err_code);

    // Since another socket is going to be attached.
    set_socket_representer_flag(false);

    // Copying session completely.
    sd_clone->session_ = session_;

    // Copying unique socket id.
    sd_clone->unique_socket_id_ = unique_socket_id_;

    sd_clone->set_to_database_direction_flag(true);
    sd_clone->set_web_sockets_upgrade_flag(sd_clone->get_web_sockets_upgrade_flag());
    sd_clone->set_db_unique_seq_num(db_unique_seq_num_);
    sd_clone->set_proxy_socket(proxy_sock_);

    // This socket becomes attached.
    sd_clone->set_socket_representer_flag(true);

#ifdef GW_COLLECT_SOCKET_STATISTICS
    bool active_conn = get_socket_diag_active_conn_flag();
    set_socket_diag_active_conn_flag(false);
    sd_clone->set_socket_diag_active_conn_flag(active_conn);
#endif

    // Setting the clone for the next iteration.
    gw->SetReceiveClone(sd_clone);

#ifdef GW_SOCKET_DIAG
    GW_COUT << "Cloned socket " << sock_ << ":" << chunk_index_ << " to " <<
        sd_clone->get_socket() << ":" << sd_clone->get_chunk_index() << GW_ENDL;
#endif

    return 0;
}

// Clone current socket data to another database.
uint32_t SocketDataChunk::CloneToAnotherDatabase(
    GatewayWorker*gw,
    int32_t new_db_index,
    SocketDataChunk** new_sd)
{
    // TODO: Add support for linked chunks.
    GW_ASSERT(1 == get_num_chunks());

    core::chunk_index new_chunk_index;
    shared_memory_chunk* new_smc;

    // Getting a chunk from new database.
    uint32_t err_code = gw->GetWorkerDb(new_db_index)->GetOneChunkFromPrivatePool(&new_chunk_index, &new_smc);
    if (err_code)
    {
        // New chunk can not be obtained.
        return err_code;
    }

    // Copying chunk data.
    memcpy(new_smc, get_smc(),
        bmx::BMX_HEADER_MAX_SIZE_BYTES + SOCKET_DATA_BLOB_OFFSET_BYTES + get_accum_buf()->get_accum_len_bytes());

    // Socket data inside chunk.
    (*new_sd) = (SocketDataChunk*)((uint8_t*)new_smc + bmx::BMX_HEADER_MAX_SIZE_BYTES);

    // Attaching to new database.
    (*new_sd)->AttachToDatabase(new_db_index);

    // Changing new chunk index.
    (*new_sd)->set_chunk_index(new_chunk_index);

    // Adjusting accumulative buffer.
    (*new_sd)->get_accum_buf()->CloneBasedOnNewBaseAddress((*new_sd)->get_data_blob(), get_accum_buf());

    return 0;
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
    if (cur_chunk_data_size > starcounter::bmx::CHUNK_MAX_DATA_BYTES)
        cur_chunk_data_size = starcounter::bmx::CHUNK_MAX_DATA_BYTES;

    // Getting shared interface pointer.
    core::shared_interface* shared_int = worker_db->get_shared_int();

    // Extra WSABufs storage chunk.
    shared_memory_chunk* wsa_bufs_smc;
    uint32_t err_code;

    // Getting link to the first chunk in chain.
    shared_memory_chunk* smc = head_smc;
    core::chunk_index cur_chunk_index = smc->get_link();

    // Checking if we need to obtain an extra chunk.
    GW_ASSERT(INVALID_CHUNK_INDEX == extra_chunk_index_);

    // Getting new chunk from pool.
    err_code = worker_db->GetOneChunkFromPrivatePool(&extra_chunk_index_, &wsa_bufs_smc);
    if (err_code)
        return err_code;

    // Increasing number of chunks by one.
    num_chunks_++;

    // Inserting extra chunk in linked chunks.
    head_smc->set_link(extra_chunk_index_);
    wsa_bufs_smc->set_link(cur_chunk_index);

    // Checking if head chunk is involved.
    if (head_chunk_offset_bytes)
    {
        // Pointing to current WSABUF in blob.
        WSABUF* wsa_buf = (WSABUF*) ((uint8_t*)wsa_bufs_smc + cur_wsa_buf_offset);
        wsa_buf->len = head_chunk_num_bytes;
        wsa_buf->buf = (char *)head_smc + head_chunk_offset_bytes;
        cur_wsa_buf_offset += sizeof(WSABUF);

        // Decreasing number of bytes left to be processed.
        bytes_left -= head_chunk_num_bytes;
        if (bytes_left < starcounter::bmx::CHUNK_MAX_DATA_BYTES)
            cur_chunk_data_size = bytes_left;
    }

    // Until we get the last chunk in chain.
    while (cur_chunk_index != shared_memory_chunk::link_terminator)
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
        if (bytes_left < starcounter::bmx::CHUNK_MAX_DATA_BYTES)
            cur_chunk_data_size = bytes_left;

        // Getting next chunk in chain.
        cur_chunk_index = smc->get_link();

        // Increasing number of used chunks.
        num_chunks_++;
    }

    // Checking that maximum number of WSABUFs in chunk is correct.
    // NOTE: Skipping extra and original chunk in check.
    GW_ASSERT((num_chunks_ - 2) <= starcounter::bmx::MAX_EXTRA_LINKED_WSABUFS);

    return 0;
}

// Returns all linked chunks except the main one.
uint32_t SocketDataChunk::ReturnExtraLinkedChunks(GatewayWorker* gw)
{
    // Checking if we have any linked chunks.
    if (1 == num_chunks_)
        return 0;

    // We have to return attached chunks.
    WorkerDbInterface *db = gw->GetWorkerDb(db_index_);
    GW_ASSERT(db != NULL);

    // Checking if any chunks are linked.
    shared_memory_chunk* smc = get_smc();

    // Checking that there are linked chunks.
    core::chunk_index first_linked_chunk = smc->get_link();
    GW_ASSERT(INVALID_CHUNK_INDEX != first_linked_chunk);

    // Resetting extra chunk index.
    extra_chunk_index_ = INVALID_CHUNK_INDEX;

    // Restoring accumulative buffer.
    accum_buf_.Init(SOCKET_DATA_BLOB_SIZE_BYTES, data_blob_, true);

    // Returning all linked chunks back to pool.
    db->ReturnLinkedChunksToPool(num_chunks_ - 1, first_linked_chunk);

    // Since all linked chunks have been returned.
    smc->terminate_link();

    // Since chunks have been released.
    num_chunks_ = 1;

    return 0;
}

// Checking that database and corresponding port handler exists.
bool SocketDataChunk::ForceSocketDataValidity(GatewayWorker* gw)
{
    // Checking if socket should be deleted.
    if (g_gateway.ShouldSocketBeDeleted(sock_))
        goto CORRECT_STATISTICS_AND_RELEASE_CHUNK;

    // Checking the database.
    ActiveDatabase* active_db = g_gateway.GetDatabase(db_index_);

    // Checking that attached database is correct.
    if ((active_db != NULL) && (db_unique_seq_num_ == active_db->get_unique_num()))
        return true;

CORRECT_STATISTICS_AND_RELEASE_CHUNK:

    GW_COUT << "Force cleaning socket data: " << sock_ << ":" << chunk_index_ << GW_ENDL;

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Correcting statistics based on operations.
    switch (type_of_network_oper_)
    {
        case ACCEPT_SOCKET_OPER:
        {
            gw->ChangeNumAcceptingSockets(port_index_, -1);
            break;
        }

        case CONNECT_SOCKET_OPER:
        {
            break;
        }

        case SEND_SOCKET_OPER:
        {
            // Do nothing.
            break;
        }

        case DISCONNECT_SOCKET_OPER:
        case RECEIVE_SOCKET_OPER:
        {
            if (get_socket_diag_active_conn_flag())
            {
                gw->ChangeNumActiveConnections(port_index_, -1);
                set_socket_diag_active_conn_flag(false);
            }
            break;
        }

        // Unknown operation.
    default:
        {
            // NOTE: This situation should never happen.
            GW_ASSERT(false);
        }
    }

#endif

    return false;
}

#ifdef GW_LOOPED_TEST_MODE

// Pushing given sd to network emulation queue.
// NOTE: Passing socket data as a pointer, not reference,
// since there is no need to pass a reference here
// (if something sd is converted to null outside this function).
void SocketDataChunk::PushToMeasuredNetworkEmulationQueue(GatewayWorker* gw)
{
    gw->PushToMeasuredNetworkEmulationQueue(this);
}

void SocketDataChunk::PushToPreparationNetworkEmulationQueue(GatewayWorker* gw)
{
    gw->PushToPreparationNetworkEmulationQueue(this);
}

#endif

} // namespace network
} // namespace starcounter
