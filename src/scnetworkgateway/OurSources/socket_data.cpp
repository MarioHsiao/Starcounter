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

    SetUserData(get_data_blob_start(), get_accumulated_len_bytes());

    set_socket_info_index(gw, socket_info_index);
    
    session_.Reset();

    ResetAccumBuffer();

    set_to_database_direction_flag();

    set_type_of_network_oper(UNKNOWN_SOCKET_OPER);
    SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1);

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
uint32_t SocketDataChunk::ReceiveTcp(GatewayWorker *gw, uint32_t *num_bytes)
{
    // Checking correct unique socket.
    GW_ASSERT(true == CompareUniqueSocketId());

    set_type_of_network_oper(RECEIVE_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    DWORD* flags = (DWORD*)(accept_or_params_or_temp_data_ + MixedCodeConstants::PARAMS_INFO_MAX_SIZE_BYTES - sizeof(DWORD));
    *flags = 0;

    CheckSpaceLeftForReceive();

    return WSARecv(GetSocket(), GetWSABUF(), 1, (LPDWORD)num_bytes, flags, &ovl_, NULL);
}

// Start sending on socket.
uint32_t SocketDataChunk::SendTcp(GatewayWorker* gw, uint32_t *numBytes)
{
    // Checking correct unique socket.
    GW_ASSERT(true == CompareUniqueSocketId());

    GW_ASSERT(get_num_available_network_bytes() > 0);

    set_type_of_network_oper(SEND_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    return WSASend(GetSocket(), GetWSABUF(), 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
}

// Start receiving on socket.
uint32_t SocketDataChunk::ReceiveUdp(GatewayWorker *gw, uint32_t *num_bytes)
{
    set_type_of_network_oper(RECEIVE_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    DWORD* flags = (DWORD*)(accept_or_params_or_temp_data_ + MixedCodeConstants::PARAMS_INFO_MAX_SIZE_BYTES - sizeof(DWORD));
    *flags = 0;

    int32_t* from_length = (int32_t*)(accept_or_params_or_temp_data_ + MixedCodeConstants::PARAMS_INFO_MAX_SIZE_BYTES - sizeof(DWORD) - sizeof(int32_t));
    *from_length = sizeof(SOCKADDR);

    CheckSpaceLeftForReceive();

    return WSARecvFrom(GetSocket(), GetWSABUF(), 1, (LPDWORD)num_bytes, flags, (SOCKADDR*) accept_or_params_or_temp_data_, from_length, &ovl_, NULL);
}

// Start sending on UDP socket.
uint32_t SocketDataChunk::SendUdp(GatewayWorker* gw, uint32_t *numBytes)
{
    GW_ASSERT(get_num_available_network_bytes() > 0);

    set_type_of_network_oper(SEND_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    SOCKADDR* sockaddr = (SOCKADDR*) accept_or_params_or_temp_data_;

    return WSASendTo(GetSocket(), GetWSABUF(), 1, (LPDWORD)numBytes, 0, sockaddr, sizeof(SOCKADDR), &ovl_, NULL);
}

// Start accepting on socket.
uint32_t SocketDataChunk::Accept(GatewayWorker* gw)
{
    // Checking correct unique socket.
    GW_ASSERT(true == CompareUniqueSocketId());

    set_type_of_network_oper(ACCEPT_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    // Running Windows API AcceptEx function.
    return AcceptExFunc(
        g_gateway.get_server_port(GetPortIndex())->get_listening_sock(),
        GetSocket(),
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
    SOCKET listening_sock = g_gateway.get_server_port(GetPortIndex())->get_listening_sock();

    if (setsockopt(GetSocket(), SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT, (char *)&listening_sock, sizeof(listening_sock))) {
        uint32_t err_code = WSAGetLastError();

        return err_code;
    }

    return 0;
}

// Start connecting on socket.
uint32_t SocketDataChunk::Connect(GatewayWorker* gw, sockaddr_in *serverAddr)
{
    // Checking correct unique socket.
    GW_ASSERT(true == CompareUniqueSocketId());

    set_type_of_network_oper(CONNECT_SOCKET_OPER);

    memset(&ovl_, 0, OVERLAPPED_SIZE);

    return ConnectExFunc(GetSocket(), (SOCKADDR *) serverAddr, sizeof(sockaddr_in), NULL, 0, NULL, &ovl_);
}

// Resetting socket.
void SocketDataChunk::ResetWhenDisconnectIsDone(GatewayWorker *gw)
{
    // Checking if there is a proxy socket.
    if (HasProxySocket()) {

        gw->DisconnectProxySocket(this);
    }

    set_to_database_direction_flag();

    set_type_of_network_oper(DISCONNECT_SOCKET_OPER);
    SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1);

    // Removing reference to/from session.
    session_.Reset();

    // Releasing socket index.
    gw->ReleaseSocketIndex(socket_info_index_);
    socket_info_index_ = INVALID_SOCKET_INDEX;
    socket_info_ = NULL;

    // Resetting HTTP/WS stuff.
    get_http_proto()->Reset();
    get_ws_proto()->Reset();

    flags_ = 0;
    ResetAccumBuffer();
}

// Exchanges sockets during proxying.
void SocketDataChunk::ExchangeToProxySocket(GatewayWorker* gw)
{
    socket_index_type proxy_socket_info_index = GetProxySocketIndex();

    // Getting corresponding proxy socket id.
    random_salt_type proxy_unique_socket_id = gw->GetUniqueSocketId(proxy_socket_info_index);

#ifdef GW_SOCKET_DIAG
    GW_COUT << "Exchanging sockets: " << socket_info_index_ << "<->" << proxy_socket_info_index << " and ids " <<
        unique_socket_id_ << "<->" << proxy_unique_socket_id << GW_ENDL;
#endif

    set_socket_info_index(gw, proxy_socket_info_index);

    unique_socket_id_ = proxy_unique_socket_id;
}

// Initializes socket data that comes from database.
void SocketDataChunk::PreInitSocketDataFromDb(GatewayWorker* gw)
{
    type_of_network_protocol_ = GetTypeOfNetworkProtocol();

    // Checking if WebSocket handshake was approved.
    if ((get_type_of_network_protocol() == MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS) &&
        get_ws_upgrade_approved_flag())
    {
        SetWebSocketGroupId(*(ws_group_id_type*)accept_or_params_or_temp_data_);
    }
}

// Pre-init UDP socket.
uint32_t SocketDataChunk::PreInitUdpSocket(GatewayWorker* gw)
{
    // Getting port from which we should send the UDP datagram.
    uint16_t from_port = *(uint16_t*)((uint8_t*)&accept_or_params_or_temp_data_ + sizeof(sockaddr_in));

    // Trying to find server port from the port number.
    ServerPort* sp = g_gateway.FindServerPort(from_port);

    // Checking that port exists and its a UDP port.
    if ((NULL == sp) || (!sp->is_udp()))
        return SCERRGWWRONGUDPFROMPORT;

    // Getting corresponding socket info.
    ScSocketInfoStruct* s = sp->GetUdpSocketInfo(gw->get_worker_id());

    // Setting found socket info as reference.
    set_socket_info_index(gw, s->read_only_index_);
    set_unique_socket_id(s->unique_socket_id_);

    return 0;
}

void SocketDataChunk::set_socket_info_reference(GatewayWorker* gw)
{
    socket_info_ = gw->GetSocketInfoReference(socket_info_index_);
}

// Clones existing socket data chunk for receiving.
uint32_t SocketDataChunk::CloneToReceive(GatewayWorker *gw)
{
    // Only socket representer can clone to its receive.
    if (!get_socket_representer_flag())
        return 0;

    SocketDataChunk* sd_clone = NULL;

    uint32_t err_code;
    if (IsUdp()) {
        err_code = gw->CreateSocketData(socket_info_index_, sd_clone, MAX_UDP_DATAGRAM_SIZE);
    } else {
        err_code = gw->CreateSocketData(socket_info_index_, sd_clone);
    }
    
    if (err_code)
        return err_code;

    // Since another socket is going to be attached.
    reset_socket_representer_flag();

    // Copying session completely.
    sd_clone->session_ = session_;

    sd_clone->set_to_database_direction_flag();
    sd_clone->set_unique_socket_id(unique_socket_id_);
    sd_clone->set_socket_info_index(gw, socket_info_index_);
    sd_clone->set_client_ip_info(client_ip_info_);
    sd_clone->set_type_of_network_oper(SocketOperType::RECEIVE_SOCKET_OPER);

    sd_clone->SetTypeOfNetworkProtocol(get_type_of_network_protocol());
    
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

    // Checking if data fits inside chunk.
    GW_ASSERT(data_len <= (int32_t)sd->get_num_available_network_bytes());

    // Checking if message should be copied.
    memcpy(sd->get_data_blob_start(), data, data_len);

    *new_sd = sd;

    return 0;
}

// Binds current socket to some scheduler.
void SocketDataChunk::BindSocketToScheduler(GatewayWorker* gw, WorkerDbInterface *db) {

    // Obtaining the current scheduler id.
    scheduler_id_type sched_id = GetSchedulerId();

    // Checking if we need to create scheduler id for certain protocols.
    switch (get_type_of_network_protocol()) {

        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_TCP:
        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS:
        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_RAW_PORT:
        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP2: {

            // Checking scheduler id validity.
            if (INVALID_SCHEDULER_ID == sched_id) {
                sched_id = db->GenerateSchedulerId();
            }

            break;
        }
    }

    // Setting private scheduler id.
    SetSchedulerId(sched_id);
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
            session_ = GetGlobalSessionCopy();
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
    GW_ASSERT(static_cast<int32_t>(get_accumulated_len_bytes()) <= GatewayChunkDataSizes[chunk_store_index_]);

    // Taking the chunk where accumulated buffer fits.
    (*new_sd) = gw->GetWorkerChunks()->ObtainChunk(get_accumulated_len_bytes());

    // Checking if couldn't obtain chunk.
    if (NULL == (*new_sd))
        return SCERRGWMAXCHUNKSNUMBERREACHED;

    // Copying the socket data headers and accumulated buffer.
    (*new_sd)->CopyFromAnotherSocketData(this);

    // This socket becomes unattached.
    (*new_sd)->reset_socket_representer_flag();

    return 0;
}

// Clone current socket data to simply send it.
uint32_t SocketDataChunk::CreateWebSocketDataFromBigBuffer(
    GatewayWorker*gw,
    uint8_t* payload,
    int32_t payload_len,
    SocketDataChunk** new_sd)
{
    // Taking the chunk where accumulated buffer fits.
    SocketDataChunk* sd = gw->GetWorkerChunks()->ObtainChunk(payload_len);

    // Checking if couldn't obtain chunk.
    if (NULL == sd)
        return SCERRGWMAXCHUNKSNUMBERREACHED;

    // First copying socket data headers.
    sd->PlainCopySocketDataInfoHeaders(this);

    // Checking if data fits inside chunk.
    GW_ASSERT(payload_len <= (int32_t)sd->get_num_available_network_bytes());

    // Checking if message should be copied.
    memcpy(sd->get_data_blob_start(), payload, payload_len);

    // Setting proper payload offset.
    sd->set_user_data_offset_in_socket_data(static_cast<uint16_t>(sd->get_data_blob_start() - (uint8_t*) sd));

    // Adjusting the accumulative buffer.
    AddAccumulatedBytes(payload_len);

    // This socket becomes unattached.
    sd->reset_socket_representer_flag();

    *new_sd = sd;

    return 0;
}

// Copies IPC chunks to gateway chunk.
uint32_t SocketDataChunk::CopyIPCChunksToGatewayChunk(
    WorkerDbInterface* worker_db,
    SocketDataChunk* ipc_sd)
{
    // Copying first chunk data.
    CopyFromOneChunkIPCSocketData(ipc_sd, MixedCodeConstants::SOCKET_DATA_BLOB_SIZE_BYTES);

    int32_t bytes_left = ipc_sd->get_user_data_length_bytes() - MixedCodeConstants::SOCKET_DATA_BLOB_SIZE_BYTES,
        data_bytes_offset = MixedCodeConstants::SOCKET_DATA_BLOB_SIZE_BYTES;

    // Checking that number of left bytes in linked chunks is correct.
    GW_ASSERT(bytes_left > 0);
    GW_ASSERT(bytes_left <= MixedCodeConstants::MAX_BYTES_EXTRA_LINKED_IPC_CHUNKS);

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
        memcpy(get_data_blob_start() + data_bytes_offset, ipc_smc, cur_chunk_data_size);

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
        GetUserData(),
        get_user_data_length_bytes(),
        *db_chunk_index,
        MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA + SOCKET_DATA_OFFSET_BLOB,
        &actual_written_bytes,
        num_ipc_chunks);

    if (0 != err_code) {

        // Releasing management chunks.
        worker_db->ReturnLinkedChunksToPool(*db_chunk_index);

        return err_code;
    }

    GW_ASSERT(actual_written_bytes == get_user_data_length_bytes());

    // NOTE: Adjusting the user data offset because we copy directly to start of the blob.
    (*new_ipc_sd)->set_user_data_offset_in_socket_data(SOCKET_DATA_OFFSET_BLOB);

    return err_code;
}

} // namespace network
} // namespace starcounter
