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
    session_index_type socket_info_index,
    db_index_type db_index,
    core::chunk_index chunk_index)
{
    socket_info_index_ = INVALID_SESSION_INDEX;

    // Obtaining corresponding port handler.
    db_index_ = db_index;

    // Resets data buffer offset.
    ResetUserDataOffset();

    // Calculating maximum size of user data.
    max_user_data_bytes_ = SOCKET_DATA_BLOB_SIZE_BYTES;
    user_data_written_bytes_ = 0;
    socket_info_index_ = socket_info_index;
    
    session_.Reset();

    chunk_index_ = chunk_index;

    // Sealing the next chunk.
    next_chunk_db_index_ = INVALID_DB_INDEX;
    get_smc()->set_next(INVALID_CHUNK_INDEX);

    flags_ = 0;
    set_to_database_direction_flag();
    align_16bytes.target_db_index_ = INVALID_DB_INDEX;

    set_type_of_network_oper(UNKNOWN_SOCKET_OPER);
    SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1);

    // Setting unique socket id.
    set_unique_socket_id(g_gateway.GetUniqueSocketId(socket_info_index));

    num_chunks_ = 1;

    // Initializing HTTP/WEBSOCKETS data structures.
    http_ws_proto_.Init();

    // Configuring data buffer.
    ResetAccumBuffer();
}

// Resetting socket.
void SocketDataChunk::Reset()
{
    flags_ = 0;

    set_to_database_direction_flag();

    set_type_of_network_oper(DISCONNECT_SOCKET_OPER);
    SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1);

    // Clearing attached session.
    //g_gateway.ClearSession(sessionIndex);

    // Removing reference to/from session.
    session_.Reset();

    // Resetting HTTP/WS stuff.
    http_ws_proto_.Reset();

    // Configuring data buffer.
    ResetAccumBuffer();
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
            core::chunk_index cur_chunk_index = get_extra_chunk_index(), temp_chunk_index_;
            if (INVALID_CHUNK_INDEX == cur_chunk_index)
                cur_chunk_index = chunk_index_;

            WorkerDbInterface* worker_db = gw->GetWorkerDb(db_index_);

            // Getting new chunk and attaching to current one.
            shared_memory_chunk *new_smc;
            err_code = worker_db->GetOneChunkFromPrivatePool(&temp_chunk_index_, &new_smc);
            if (err_code)
                return err_code;

            // Incrementing number of chunks.
            num_chunks_++;

            // Linking new chunk to current chunk.
            shared_memory_chunk* cur_smc = gw->GetSmcFromChunkIndex(db_index_, cur_chunk_index);
            cur_smc->set_link(temp_chunk_index_);

            // Setting new chunk as a new buffer.
            accum_buf_.Init(MixedCodeConstants::CHUNK_MAX_DATA_BYTES, (uint8_t*)new_smc, false);
        }
    }
    else
    {
        // All data has been received.
        *is_accumulated = true;

        // Restoring the socket data link.
        accum_buf_.RestoreOrigBufPtr();
    }

    return 0;
}

// Clones existing socket data chunk for receiving.
uint32_t SocketDataChunk::CloneToReceive(GatewayWorker *gw)
{
    // Only socket representer can clone to its receive.
    if (!get_socket_representer_flag())
        return 0;

    SocketDataChunk* sd_clone = NULL;

    // NOTE: Cloning to receive only on database 0 chunks.
    uint32_t err_code = gw->CreateSocketData(socket_info_index_, 0, sd_clone);
    GW_ERR_CHECK(err_code);

    // Since another socket is going to be attached.
    reset_socket_representer_flag();

    // Copying session completely.
    sd_clone->session_ = session_;

    sd_clone->set_to_database_direction_flag();
    sd_clone->SetTypeOfNetworkProtocol(get_type_of_network_protocol());
    sd_clone->set_unique_socket_id(unique_socket_id_);
    sd_clone->set_socket_info_index(socket_info_index_);
    sd_clone->set_client_ip_info(client_ip_info_);

    // This socket becomes attached.
    sd_clone->set_socket_representer_flag();

#ifdef GW_COLLECT_SOCKET_STATISTICS
    bool active_conn = get_socket_diag_active_conn_flag();
    reset_socket_diag_active_conn_flag();
    if (active_conn)
        sd_clone->set_socket_diag_active_conn_flag();
    else
        sd_clone->reset_socket_diag_active_conn_flag();
#endif

    // Setting the clone for the next iteration.
    gw->SetReceiveClone(sd_clone);

#ifdef GW_SOCKET_DIAG
    GW_COUT << "Cloned socket " << socket_ << ":" << unique_socket_id_ << ":" << chunk_index_ << " to socket index " <<
        sd_clone->get_socket_info_index() << ":" << sd_clone->get_chunk_index() << GW_ENDL;
#endif

    return 0;
}

// Start sending on socket.
uint32_t SocketDataChunk::SendMultipleChunks(GatewayWorker* gw, uint32_t *num_sent_bytes)
{
    set_type_of_network_oper(SEND_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
    PushToMeasuredNetworkEmulationQueue(gw);
    return WSA_IO_PENDING;
#endif
    int32_t num_wsa_bufs = num_chunks_ - 1;

    // Checking if its aggregating socket data.
    if (get_aggregation_sd_flag())
        num_wsa_bufs--;

    // Getting the contents of WSABUFs chunk.
    WSABUF* wsa_bufs = (WSABUF*) gw->GetSmcFromChunkIndex(db_index_, get_extra_chunk_index());

    // Getting socket number.
    SOCKET s = GetSocket();

    // NOTE: Need to subtract one chunks from being included in send.
    return WSASend(s, wsa_bufs, num_wsa_bufs, (LPDWORD)num_sent_bytes, 0, &ovl_, NULL);
}

// Clone current socket data to push it.
uint32_t SocketDataChunk::CloneToPush(
    GatewayWorker*gw,
    SocketDataChunk** new_sd)
{
    // TODO: Add support for linked chunks.
    GW_ASSERT(1 == get_num_chunks());

    core::chunk_index new_chunk_index;
    shared_memory_chunk* new_smc;

    // Getting a chunk from new database.
    uint32_t err_code = gw->GetWorkerDb(db_index_)->GetOneChunkFromPrivatePool(&new_chunk_index, &new_smc);
    if (err_code)
    {
        // New chunk can not be obtained.
        return err_code;
    }

    // Copying chunk data.
    memcpy(new_smc, get_smc(), MixedCodeConstants::SHM_CHUNK_SIZE);

    // Socket data inside chunk.
    (*new_sd) = (SocketDataChunk*)((uint8_t*)new_smc + MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA);

    // Changing new chunk index.
    (*new_sd)->set_chunk_index(new_chunk_index);

    // Sealing the chunk.
    (*new_sd)->set_next_chunk_db_index(INVALID_DB_INDEX);
    new_smc->set_next(INVALID_CHUNK_INDEX);

    // Adjusting accumulative buffer.
    int32_t offset_bytes_from_sd = static_cast<int32_t> (get_accum_buf()->get_chunk_orig_buf_ptr() - (uint8_t*) this);
    (*new_sd)->get_accum_buf()->CloneBasedOnNewBaseAddress((uint8_t*) (*new_sd) + offset_bytes_from_sd, get_accum_buf());

    // This socket becomes unattached.
    (*new_sd)->reset_socket_representer_flag();
    (*new_sd)->reset_socket_diag_active_conn_flag();

    return 0;
}

// Clone current socket data to another database.
uint32_t SocketDataChunk::CloneToAnotherDatabase(
    GatewayWorker*gw,
    int32_t new_db_index,
    SocketDataChunk** new_sd)
{
    core::chunk_index new_db_chunk_index;
    shared_memory_chunk* new_db_smc;

    // Getting a chunk from new database.
    WorkerDbInterface* new_worker_db = gw->GetWorkerDb(new_db_index);
    GW_ASSERT(NULL != new_worker_db);

    uint32_t err_code = new_worker_db->GetOneChunkFromPrivatePool(&new_db_chunk_index, &new_db_smc);
    if (err_code)
        return err_code;

    // Copying chunk data.
    memcpy(new_db_smc, get_smc(), MixedCodeConstants::SHM_CHUNK_SIZE);

    // Socket data inside chunk.
    (*new_sd) = (SocketDataChunk*)((uint8_t*)new_db_smc + MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA);

    // Attaching to new database.
    (*new_sd)->AttachToDatabase(new_db_index);

    // Changing new chunk index.
    (*new_sd)->set_chunk_index(new_db_chunk_index);
    (*new_sd)->reset_socket_representer_flag();
    (*new_sd)->reset_socket_diag_active_conn_flag();

    // Sealing the chunk.
    (*new_sd)->set_next_chunk_db_index(INVALID_DB_INDEX);
    new_db_smc->set_next(INVALID_CHUNK_INDEX);

    // Adjusting accumulative buffer.
    int32_t offset_bytes_from_sd = static_cast<int32_t> (get_accum_buf()->get_chunk_orig_buf_ptr() - (uint8_t*) this);
    (*new_sd)->get_accum_buf()->CloneBasedOnNewBaseAddress((uint8_t*) (*new_sd) + offset_bytes_from_sd, get_accum_buf());

    core::chunk_index prev_db_chunk_index = get_smc()->get_link();
    shared_memory_chunk* prev_db_smc;

    // Copying all linked chunks.
    for (int32_t i = 0; i < static_cast<int32_t>(num_chunks_ - 1); i++)
    {
        shared_memory_chunk* cur_new_db_smc = new_db_smc;

        err_code = new_worker_db->GetOneChunkFromPrivatePool(&new_db_chunk_index, &new_db_smc);
        if (err_code)
            return err_code;

        // Getting link to previous database linked chunk.
        prev_db_smc = gw->GetSmcFromChunkIndex(db_index_, prev_db_chunk_index);

        // Copying chunk data.
        memcpy(new_db_smc, prev_db_smc, MixedCodeConstants::SHM_CHUNK_SIZE);
        new_db_smc->set_link(INVALID_CHUNK_INDEX);
        new_db_smc->set_next(INVALID_CHUNK_INDEX);

        cur_new_db_smc->set_link(new_db_chunk_index);

        prev_db_chunk_index = prev_db_smc->get_link();
    }

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
    if (cur_chunk_data_size > starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
        cur_chunk_data_size = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;

    // Getting shared interface pointer.
    core::shared_interface* shared_int = worker_db->get_shared_int();

    // Extra WSABufs storage chunk.
    shared_memory_chunk* wsa_bufs_smc;
    uint32_t err_code;

    // Getting link to the first chunk in chain.
    shared_memory_chunk* smc = head_smc;
    core::chunk_index cur_chunk_index = smc->get_link();

    core::chunk_index wsa_bufs_chunk_index;

    // Getting new chunk from pool.
    err_code = worker_db->GetOneChunkFromPrivatePool(&wsa_bufs_chunk_index, &wsa_bufs_smc);
    if (err_code)
        return err_code;

    // Increasing number of chunks by one.
    num_chunks_++;

    // Inserting extra chunk in linked chunks.
    head_smc->set_link(wsa_bufs_chunk_index);
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
        if (bytes_left < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
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
        if (bytes_left < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
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
    core::chunk_index first_linked_chunk = get_extra_chunk_index();
    GW_ASSERT(INVALID_CHUNK_INDEX != first_linked_chunk);

    // Restoring accumulative buffer.
    ResetAccumBuffer();

    // Returning all linked chunks back to pool.
    db->ReturnLinkedChunksToPool(num_chunks_ - 1, first_linked_chunk);

    // Since all linked chunks have been returned.
    smc->terminate_link();

    // Since chunks have been released.
    num_chunks_ = 1;

    return 0;
}

// Deletes global session and sends message to database to delete session there.
uint32_t SocketDataChunk::SendDeleteSession(GatewayWorker* gw)
{
    // Verifying that session is correct and sending delete session to database.
    if (CompareUniqueSocketId() && CompareGlobalSessionSalt())
    {
        ScSessionStruct s = g_gateway.GetGlobalSessionCopy(socket_info_index_);
        return (gw->GetWorkerDb(db_index_)->PushSessionDestroy(s.linear_index_, s.random_salt_, s.scheduler_id_));
    }

    return 0;
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
