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
    GatewayWorker* gw,
    socket_index_type socket_info_index)
{
    flags_ = 0;

    socket_info_index_ = INVALID_SOCKET_INDEX;

    ResetUserDataOffset();

    socket_info_index_ = socket_info_index;
    
    session_.Reset();

    ResetAccumBuffer();

    set_to_database_direction_flag();

    set_type_of_network_oper(UNKNOWN_SOCKET_OPER);
    gw->SetTypeOfNetworkProtocol(this, MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1);

    set_unique_socket_id(gw->GetUniqueSocketId(socket_info_index));
    set_bound_worker_id(gw->get_worker_id());

    // Initializing HTTP/WEBSOCKETS data structures.
    get_http_proto()->Init();
    get_ws_proto()->Init();
}

// Setting new unique socket number.
void SocketDataChunk::GenerateUniqueSocketInfoIds(GatewayWorker* gw)
{
    unique_socket_id_ = gw->GenerateUniqueSocketInfoIds(socket_info_index_);
}

// Start receiving on socket.
uint32_t SocketDataChunk::Receive(GatewayWorker *gw, uint32_t *num_bytes)
{
    // Checking correct unique socket.
    GW_ASSERT(true == gw->CompareUniqueSocketId(this));

    set_type_of_network_oper(RECEIVE_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    DWORD flags = 0;
    return WSARecv(gw->GetSocket(this), (WSABUF *)&accum_buf_, 1, (LPDWORD)num_bytes, &flags, &ovl_, NULL);
}

// Start sending on socket.
uint32_t SocketDataChunk::Send(GatewayWorker* gw, uint32_t *numBytes)
{
    // Checking correct unique socket.
    GW_ASSERT(true == gw->CompareUniqueSocketId(this));

    GW_ASSERT(accum_buf_.get_chunk_num_available_bytes() > 0);

    set_type_of_network_oper(SEND_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    return WSASend(gw->GetSocket(this), (WSABUF *)&accum_buf_, 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
}

// Start accepting on socket.
uint32_t SocketDataChunk::Accept(GatewayWorker* gw)
{
    // Checking correct unique socket.
    GW_ASSERT(true == gw->CompareUniqueSocketId(this));

    set_type_of_network_oper(ACCEPT_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    // Running Windows API AcceptEx function.
    return AcceptExFunc(
        g_gateway.get_server_port(gw->GetPortIndex(this))->get_listening_sock(),
        gw->GetSocket(this),
        accept_or_params_or_temp_data_,
        0,
        SOCKADDR_SIZE_EXT,
        SOCKADDR_SIZE_EXT,
        NULL,
        &ovl_);
}

// Setting SO_UPDATE_ACCEPT_CONTEXT.
uint32_t SocketDataChunk::SetAcceptSocketOptions(GatewayWorker* gw)
{
    SOCKET listening_sock = g_gateway.get_server_port(gw->GetPortIndex(this))->get_listening_sock();

    if (setsockopt(gw->GetSocket(this), SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT, (char *)&listening_sock, sizeof(listening_sock))) {
        uint32_t err_code = WSAGetLastError();

        return err_code;
    }

    return 0;
}

// Start connecting on socket.
uint32_t SocketDataChunk::Connect(GatewayWorker* gw, sockaddr_in *serverAddr)
{
    // Checking correct unique socket.
    GW_ASSERT(true == gw->CompareUniqueSocketId(this));

    set_type_of_network_oper(CONNECT_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    return ConnectExFunc(gw->GetSocket(this), (SOCKADDR *) serverAddr, sizeof(sockaddr_in), NULL, 0, NULL, &ovl_);
}

// Start disconnecting socket.
uint32_t SocketDataChunk::Disconnect(GatewayWorker *gw)
{
    // Checking correct unique socket.
    GW_ASSERT(true == gw->CompareUniqueSocketId(this));

    set_type_of_network_oper(DISCONNECT_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    return DisconnectExFunc(gw->GetSocket(this), &ovl_, 0, 0);
}

// Resetting socket.
void SocketDataChunk::ResetOnDisconnect(GatewayWorker *gw)
{
    // Checking if there is a proxy socket.
    if (gw->HasProxySocket(this)) {

        gw->DisconnectProxySocket(this);
    }

    set_to_database_direction_flag();

    set_type_of_network_oper(DISCONNECT_SOCKET_OPER);
    gw->SetTypeOfNetworkProtocol(this, MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1);

    // Clearing attached session.
    //g_gateway.ClearSession(sessionIndex);

    // Removing reference to/from session.
    session_.Reset();

    // Releasing socket index.
    gw->ReleaseSocketIndex(socket_info_index_);
    socket_info_index_ = INVALID_SOCKET_INDEX;

    // Resetting HTTP/WS stuff.
    get_http_proto()->Reset();
    get_ws_proto()->Reset();

    flags_ = 0;
    ResetAccumBuffer();
}

// Exchanges sockets during proxying.
void SocketDataChunk::ExchangeToProxySocket(GatewayWorker* gw)
{
    socket_index_type proxy_socket_info_index = gw->GetProxySocketIndex(this);

    // Getting corresponding proxy socket id.
    random_salt_type proxy_unique_socket_id = gw->GetUniqueSocketId(proxy_socket_info_index);

#ifdef GW_SOCKET_DIAG
    GW_COUT << "Exchanging sockets: " << socket_info_index_ << "<->" << proxy_socket_info_index << " and ids " <<
        unique_socket_id_ << "<->" << proxy_unique_socket_id << GW_ENDL;
#endif

    socket_info_index_ = proxy_socket_info_index;
    unique_socket_id_ = proxy_unique_socket_id;
}

// Initializes socket data that comes from database.
void SocketDataChunk::PreInitSocketDataFromDb(GatewayWorker* gw)
{
    type_of_network_protocol_ = gw->GetTypeOfNetworkProtocol(this);

    // NOTE: Setting global session including scheduler id.
    gw->SetGlobalSessionCopy(this, session_);

    // Checking if WebSocket handshake was approved.
    if ((get_type_of_network_protocol() == MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS) &&
        get_ws_upgrade_approved_flag())
    {
        gw->SetWebSocketChannelId(this, *(uint32_t*)accept_or_params_or_temp_data_);
    }
}

// Clones existing socket data chunk for receiving.
uint32_t SocketDataChunk::CloneToReceive(GatewayWorker *gw)
{
    // Only socket representer can clone to its receive.
    if (!get_socket_representer_flag())
        return 0;

    SocketDataChunk* sd_clone = NULL;

    // NOTE: Cloning to receive only on database 0 chunks.
    uint32_t err_code = gw->CreateSocketData(socket_info_index_, sd_clone);
    if (err_code)
        return err_code;

    // Since another socket is going to be attached.
    reset_socket_representer_flag();

    // Copying session completely.
    sd_clone->session_ = session_;

    sd_clone->set_to_database_direction_flag();
    gw->SetTypeOfNetworkProtocol(sd_clone, get_type_of_network_protocol());
    sd_clone->set_unique_socket_id(unique_socket_id_);
    sd_clone->set_socket_info_index(socket_info_index_);
    sd_clone->set_client_ip_info(client_ip_info_);
    sd_clone->set_type_of_network_oper(SocketOperType::RECEIVE_SOCKET_OPER);

    // This socket becomes attached.
    sd_clone->set_socket_representer_flag();

    // Setting the clone for the next iteration.
    gw->SetReceiveClone(sd_clone);

#ifdef GW_SOCKET_DIAG
    GW_COUT << "Cloned socket " << socket_info_index_ << ":" << GetSocket() << ":" << unique_socket_id_ << ":" << (uint64_t)this << " to socket " <<
        sd_clone->get_socket_info_index() << ":" << sd_clone->GetSocket() << ":" << sd_clone->get_unique_socket_id() << ":" << (uint64_t)sd_clone << GW_ENDL;
#endif

    return 0;
}

// Clone current socket data to simply send it.
uint32_t SocketDataChunk::CreateSocketDataFromBigBuffer(
    GatewayWorker*gw,
    socket_index_type socket_info_index,
    int32_t data_len,
    uint8_t* data,
    SocketDataChunk** new_sd)
{
    // Getting a chunk from new database.
    SocketDataChunk* sd;
    uint32_t err_code = gw->CreateSocketData(socket_info_index, sd, data_len);
    if (err_code)
    {
        // New chunk can not be obtained.
        return err_code;
    }

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Checking if data fits inside chunk.
    GW_ASSERT(data_len <= (int32_t)accum_buf->get_chunk_num_available_bytes());

    // Checking if message should be copied.
    memcpy(accum_buf->get_chunk_orig_buf_ptr(), data, data_len);

    *new_sd = sd;

    return 0;
}

// Binds current socket to some scheduler.
void SocketDataChunk::BindSocketToScheduler(GatewayWorker* gw, WorkerDbInterface *db) {

    // Checking if we need to create scheduler id for certain protocols.
    switch (get_type_of_network_protocol()) {

        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_RAW_PORT: {

            // Obtaining the current scheduler id.
            scheduler_id_type sched_id = get_scheduler_id();

            // Checking scheduler id validity.
            if (INVALID_SCHEDULER_ID == sched_id) {

                sched_id = db->GenerateSchedulerId();

                gw->SetSchedulerId(this, sched_id);
            }

            break;

        }
    }
}

// Resets session depending on protocol.
void SocketDataChunk::ResetSessionBasedOnProtocol(GatewayWorker* gw)
{
    // Processing session according to protocol.
    switch (get_type_of_network_protocol())
    {
        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1:
            ResetSdSession();
            break;

        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS:
        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_RAW_PORT:
            session_ = gw->GetGlobalSessionCopy(this);
            break;

        default:
            ResetSdSession();
    }
}

// Clone current socket data to a bigger one.
uint32_t SocketDataChunk::ChangeToBigger(
    GatewayWorker*gw,
    SocketDataChunkRef sd,
    int32_t data_size)
{
    // Checking if maximum chunk size is reached.
    if (sd->get_chunk_store_index() == NumGatewayChunkSizes)
        return SCERRGWMAXCHUNKSIZEREACHED;

    // Taking the chunk where accumulated buffer fits.
    SocketDataChunk* new_sd;
    if (data_size == 0)
        new_sd = gw->GetWorkerChunks()->ObtainChunkByStoreIndex(sd->get_chunk_store_index() + 1);
    else
        new_sd = gw->GetWorkerChunks()->ObtainChunk(data_size);

    // Checking if couldn't obtain chunk.
    if (NULL == new_sd)
        return SCERRGWMAXCHUNKSNUMBERREACHED;

    // Copying the socket data headers and accumulated buffer.
    new_sd->CopyFromAnotherSocketData(sd);

    // Releasing chunk.
    gw->GetWorkerChunks()->ReleaseChunk(sd);

    sd = new_sd;

    return 0;
}

// Clone current socket data to push it.
uint32_t SocketDataChunk::CloneToPush(GatewayWorker* gw, SocketDataChunk** new_sd)
{
    GW_ASSERT(static_cast<int32_t>(get_accum_buf()->get_accum_len_bytes()) <= GatewayChunkDataSizes[chunk_store_index_]);

    // Taking the chunk where accumulated buffer fits.
    (*new_sd) = gw->GetWorkerChunks()->ObtainChunk(get_accum_buf()->get_accum_len_bytes());

    // Checking if couldn't obtain chunk.
    if (NULL == (*new_sd))
        return SCERRGWMAXCHUNKSNUMBERREACHED;

    // Copying the socket data headers and accumulated buffer.
    (*new_sd)->CopyFromAnotherSocketData(this);

    // This socket becomes unattached.
    (*new_sd)->reset_socket_representer_flag();

    return 0;
}

// Copies IPC chunks to gateway chunk.
uint32_t SocketDataChunk::CopyIPCChunksToGatewayChunk(
    WorkerDbInterface* worker_db,
    SocketDataChunk* ipc_sd)
{
    int32_t data_bytes_offset = MixedCodeConstants::SOCKET_DATA_MAX_SIZE - ipc_sd->get_user_data_offset_in_socket_data(),
        bytes_left = ipc_sd->get_user_data_length_bytes() - data_bytes_offset;

    // Checking that number of left bytes in linked chunks is correct.
    GW_ASSERT(bytes_left <= MixedCodeConstants::MAX_BYTES_EXTRA_LINKED_IPC_CHUNKS);

    // Copying first chunk.
    CopyFromOneChunkIPCSocketData(ipc_sd, data_bytes_offset);

    // Number of bytes to copy from IPC chunk.
    uint32_t cur_chunk_data_size = bytes_left;
    if (cur_chunk_data_size > starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
        cur_chunk_data_size = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;

    // Getting link to the first chunk in chain.
    shared_memory_chunk* ipc_smc = (shared_memory_chunk*)((uint8_t*)ipc_sd - MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA);
    core::chunk_index cur_chunk_index = ipc_smc->get_link();

    // Until we get the last chunk in chain.
    while (cur_chunk_index != shared_memory_chunk::link_terminator)
    {
        // Obtaining chunk memory.
        ipc_smc = worker_db->GetSharedMemoryChunkFromIndex(cur_chunk_index);

        // Copying current IPC chunk.
        memcpy(data_blob_ + data_bytes_offset, ipc_smc, cur_chunk_data_size);

        // Decreasing number of bytes left to be processed.
        data_bytes_offset += cur_chunk_data_size;
        bytes_left -= cur_chunk_data_size;
        if (bytes_left < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
            cur_chunk_data_size = bytes_left;

        // Getting next chunk in chain.
        cur_chunk_index = ipc_smc->get_link();
    }

    // Checking that maximum number of WSABUFs in chunk is correct.
    GW_ASSERT(0 == bytes_left);

    return 0;
}

// Copies gateway chunk to IPC chunks.
uint32_t SocketDataChunk::CopyGatewayChunkToIPCChunks(
    WorkerDbInterface* worker_db,
    SocketDataChunk** new_ipc_sd,
    core::chunk_index* db_chunk_index,
    uint16_t* num_ipc_chunks)
{
    shared_memory_chunk* ipc_smc;

    uint32_t err_code = worker_db->GetOneChunkFromPrivatePool(db_chunk_index, &ipc_smc);
    if (err_code)
        return err_code;

    *new_ipc_sd = (SocketDataChunk*)((uint8_t*)ipc_smc + MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA);

    // Copying only socket data info without data buffer.
    memcpy(*new_ipc_sd, this, SOCKET_DATA_OFFSET_BLOB);

    int32_t actual_written_bytes = 0;

    // Copying data buffer separately.
    err_code = worker_db->WriteBigDataToIPCChunks(
        get_accum_buf()->get_chunk_orig_buf_ptr(),
        get_accum_buf()->get_accum_len_bytes(),
        *db_chunk_index,
        MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA + SOCKET_DATA_OFFSET_BLOB,
        &actual_written_bytes,
        num_ipc_chunks);

    if (0 != err_code) {

        // Releasing management chunks.
        worker_db->ReturnLinkedChunksToPool(*db_chunk_index);

        return err_code;
    }

    GW_ASSERT(actual_written_bytes == get_accum_buf()->get_accum_len_bytes());

    return err_code;
}

} // namespace network
} // namespace starcounter
