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

// Mandatory initialization function.
int32_t GatewayWorker::Init(int32_t new_worker_id)
{
    worker_id_ = new_worker_id;

    // Creating IO completion port.
    worker_iocp_ = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 1);
    GW_ASSERT(worker_iocp_ != NULL);
    if (worker_iocp_ == NULL)
    {
        GW_PRINT_WORKER << "Failed to create worker IOCP." << GW_ENDL;
        return PrintLastError();
    }

    // Getting global IOCP.
    //worker_iocp_ = g_gateway.get_iocp();

    rebalance_accept_sockets_ = (PSLIST_HEADER) _aligned_malloc(sizeof(SLIST_HEADER), MEMORY_ALLOCATION_ALIGNMENT);
    GW_ASSERT(rebalance_accept_sockets_);
    InitializeSListHead(rebalance_accept_sockets_);

    worker_suspended_unsafe_ = false;

    worker_stats_bytes_received_ = 0;
    worker_stats_bytes_sent_ = 0;
    worker_stats_sent_num_ = 0;
    worker_stats_recv_num_ = 0;

#ifdef GW_TESTING_MODE
    num_created_conns_worker_ = 0;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    for (int32_t i = 0; i < MAX_PORTS_NUM; i++)
        port_num_active_conns_[i] = 0;
#endif

    for (int32_t i = 0; i < MAX_ACTIVE_DATABASES; i++)
        worker_dbs_[i] = NULL;

    // Creating random generator with current time seed.
    rand_gen_ = new random_generator(timeGetTime());

    aggr_timer_.Start();

    return 0;
}

#ifdef GW_PROXY_MODE

// Allocates a new socket based on existing.
uint32_t GatewayWorker::CreateProxySocket(SocketDataChunkRef sd)
{
    // Creating new socket.
    SOCKET new_connect_socket;

    new_connect_socket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (new_connect_socket == INVALID_SOCKET)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "WSASocket() failed." << GW_ENDL;
#endif
        return PrintLastError();
    }

    // Trying to bind socket until this succeeds.
    while (true)
    {
        // The socket address to be passed to bind.
        sockaddr_in binding_addr;
        memset(&binding_addr, 0, sizeof(sockaddr_in));
        binding_addr.sin_family = AF_INET;

        // Checking if we have local interfaces to bind.
        if (g_gateway.setting_local_interfaces().size() > 0)
            binding_addr.sin_addr.s_addr = inet_addr(g_gateway.setting_local_interfaces().at(g_gateway.get_last_bind_interface_num()).c_str());
        else
            binding_addr.sin_addr.s_addr = INADDR_ANY;

        // Generating new port/interface.
        g_gateway.GenerateNewBindPortInterfaceNumbers();

        // Initially used when too many ports were needed (>64K).
        //binding_addr.sin_port = htons(g_gateway.get_last_bind_port_num());

        // Binding socket to certain interface and port.
        if (bind(new_connect_socket, (SOCKADDR *) &binding_addr, sizeof(binding_addr)))
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed to bind port!" << GW_ENDL;
#endif
            continue;
        }

        break;
    }

    int32_t onFlag = 1;

    // Does not block close waiting for unsent data to be sent.
    if (setsockopt(new_connect_socket, SOL_SOCKET, SO_DONTLINGER, (char *)&onFlag, 4))
    {
        GW_PRINT_WORKER << "Can't set SO_DONTLINGER on socket." << GW_ENDL;

        closesocket(new_connect_socket);

        return PrintLastError();
    }

#ifdef GW_IOCP_IMMEDIATE_COMPLETION
    // Skipping completion port if operation is already successful.
    SetFileCompletionNotificationModes((HANDLE)new_connect_socket, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);
#endif

    // Putting socket into non-blocking mode.
    uint32_t ul = 1, temp;
    if (WSAIoctl(new_connect_socket, FIONBIO, &ul, sizeof(ul), NULL, 0, (LPDWORD)&temp, NULL, NULL))
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Can't put socket into non-blocking mode." << GW_ENDL;
#endif
        closesocket(new_connect_socket);

        return PrintLastError();
    }

    // Getting new socket index.
    port_index_type port_index = sd->GetPortIndex();
    session_index_type proxied_socket_info_index = g_gateway.ObtainFreeSocketIndex(this, new_connect_socket, port_index, true);

    // Associating new socket with current worker IOCP.
    HANDLE iocp_handler = CreateIoCompletionPort((HANDLE) new_connect_socket, worker_iocp_, 0, 1);
    GW_ASSERT(iocp_handler == worker_iocp_);

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Changing number of created sockets.
    int64_t created_sockets = g_gateway.get_server_port(port_index)->ChangeNumAllocatedConnectSockets(1);

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "New sockets amount: " << created_sockets << GW_ENDL;
#endif

#endif

    // Setting proxy sockets indexes.
    session_index_type orig_socket_info_index = sd->get_socket_info_index();
    sd->SetProxySocketIndex(proxied_socket_info_index);
    sd->set_socket_info_index(proxied_socket_info_index);
    sd->SetProxySocketIndex(orig_socket_info_index);

    return 0;
}

#endif

// Allocates a bunch of new connections.
uint32_t GatewayWorker::CreateNewConnections(int32_t how_many, port_index_type port_index)
{
#ifndef GW_TESTING_MODE
    GW_ASSERT(0 == worker_id_);
#endif

    uint32_t err_code;
    int32_t curIntNum = 0;

    for (int32_t i = 0; i < how_many; i++)
    {
        SOCKET new_socket;

        // Creating new socket.
        new_socket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
        if (new_socket == INVALID_SOCKET)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "WSASocket() failed." << GW_ENDL;
#endif
            return PrintLastError();
        }

#ifdef GW_TESTING_MODE

        // Binding sockets if we are on client.
        if (!g_gateway.setting_is_master())
        {
            // Trying to bind socket until this succeeds.
            while(true)
            {
                // The socket address to be passed to bind.
                sockaddr_in binding_addr;
                memset(&binding_addr, 0, sizeof(sockaddr_in));
                binding_addr.sin_family = AF_INET;

                // Checking if we have local interfaces to bind.
                if (g_gateway.setting_local_interfaces().size() > 0)
                    binding_addr.sin_addr.s_addr = inet_addr(g_gateway.setting_local_interfaces().at(g_gateway.get_last_bind_interface_num()).c_str());
                else
                    binding_addr.sin_addr.s_addr = INADDR_ANY;


                // Generating new port/interface.
                g_gateway.GenerateNewBindPortInterfaceNumbers();

                // Initially used when too many ports were needed (>64K).
                //binding_addr.sin_port = htons(g_gateway.get_last_bind_port_num());

                // Binding socket to certain interface and port.
                if (bind(new_socket, (SOCKADDR *) &binding_addr, sizeof(binding_addr)))
                {
#ifdef GW_ERRORS_DIAG
                    GW_PRINT_WORKER << "Failed to bind port!" << GW_ENDL;
#endif
                    continue;
                }

                break;
            }
        }

#endif

        // Setting needed socket options.
        int32_t on_flag = 1;

        // Disables the Nagle algorithm for send coalescing.
        if (setsockopt(new_socket, IPPROTO_TCP, TCP_NODELAY, (char *)&on_flag, 4))
        {
            GW_PRINT_WORKER << "Can't set TCP_NODELAY on socket." << GW_ENDL;

            closesocket(new_socket);

            return PrintLastError();
        }

        // Does not block close waiting for unsent data to be sent.
        if (setsockopt(new_socket, SOL_SOCKET, SO_DONTLINGER, (char *)&on_flag, 4))
        {
            GW_PRINT_WORKER << "Can't set SO_DONTLINGER on socket." << GW_ENDL;

            closesocket(new_socket);

            return PrintLastError();
        }

#ifdef GW_IOCP_IMMEDIATE_COMPLETION
        // Skipping completion port if operation is already successful.
        SetFileCompletionNotificationModes((HANDLE)new_socket, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);
#endif
        /*
        // Putting socket into non-blocking mode.
        uint32_t ul = 1, temp;
        if (WSAIoctl(new_socket, FIONBIO, &ul, sizeof(ul), NULL, 0, (LPDWORD)&temp, NULL, NULL))
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Can't put socket into non-blocking mode." << GW_ENDL;
#endif
            closesocket(new_socket);

            return PrintLastError();
        }*/


        // Creating new socket data structure inside chunk.
        SocketDataChunk* new_sd = NULL;
        
        // Getting new socket index.
        session_index_type new_socket_index = g_gateway.ObtainFreeSocketIndex(this, new_socket, port_index, false);

        // Creating new socket data.
        err_code = CreateSocketData(new_socket_index, new_sd);
        if (err_code)
        {
            closesocket(new_socket);

            return err_code;
        }

#ifdef GW_TESTING_MODE

        // Checking if its a master node.
        if (!g_gateway.setting_is_master())
        {
            // Performing connect.
            err_code = Connect(new_sd, g_gateway.get_server_addr());
            GW_ERR_CHECK(err_code);
        }
        else
        {
            // Performing accept.
            err_code = Accept(new_sd);
            GW_ERR_CHECK(err_code);
        }
#else
        // Performing accept.
        err_code = Accept(new_sd);
        GW_ERR_CHECK(err_code);
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS

        // Changing number of created sockets.
        int64_t created_sockets = g_gateway.get_server_port(port_index)->ChangeNumAllocatedAcceptSockets(how_many);

#ifdef GW_SOCKET_DIAG
        GW_PRINT_WORKER << "New sockets amount: " << created_sockets << GW_ENDL;
#endif

#endif
    }

#ifdef GW_TESTING_MODE
    // Updating number of created connections for worker.
    num_created_conns_worker_ += how_many;
#endif

    return 0;
}

// Running receive on socket data.
uint32_t GatewayWorker::Receive(SocketDataChunkRef sd)
{
    // Checking that socket arrived on correct worker.
    GW_ASSERT(sd->get_bound_worker_id() == worker_id_);
    GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    // Checking that not aggregated socket are trying to receive.
    GW_ASSERT_DEBUG(false == sd->GetSocketAggregatedFlag());

#ifdef GW_IOCP_IMMEDIATE_COMPLETION
// This label is used to avoid recursiveness between Receive and FinishReceive.
START_RECEIVING_AGAIN:
#endif

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Receive: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    uint32_t numBytes, err_code;

    err_code = sd->Receive(this, &numBytes);

    // Checking if operation completed immediately.
    if (0 != err_code)
    {
        int32_t wsa_err_code = WSAGetLastError();

#ifdef GW_LOOPED_TEST_MODE
        wsa_err_code = WSA_IO_PENDING;
#endif

        // Checking if IOCP event was scheduled.
        if (WSA_IO_PENDING != wsa_err_code)
        {
#ifdef GW_WARNINGS_DIAG
            GW_PRINT_WORKER << "Failed WSARecv: socket " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << ". Disconnecting socket..." << GW_ENDL;

            PrintLastError();
#endif

            return SCERRGWFAILEDWSARECV;
        }
    }
#ifdef GW_IOCP_IMMEDIATE_COMPLETION
    else
    {
        // Checking if socket is closed by the other peer.
        if (0 == numBytes)
        {
#ifdef GW_WARNINGS_DIAG
            GW_PRINT_WORKER << "Zero-bytes receive on socket index: " << sd->get_socket_info_index() << ". Remote side closed the connection." << GW_ENDL;
#endif

            return SCERRGWSOCKETCLOSEDBYPEER;
        }
        else
        {
            // Finish receive operation.
            bool called_from_receive = true;
            err_code = FinishReceive(sd, numBytes, called_from_receive);
            GW_ERR_CHECK(err_code);

            // Checking if Receive was called internally again.
            if (!called_from_receive)
                goto START_RECEIVING_AGAIN;
        }
    }
#endif

    // NOTE: Setting socket data to null, so other
    // manipulations on it are not possible.
    sd = NULL;

    return 0;
}

// Socket receive finished.
__forceinline uint32_t GatewayWorker::FinishReceive(
    SocketDataChunkRef sd,
    int32_t num_bytes_received,
    bool& called_from_receive)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishReceive: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking that socket arrived on correct worker.
    GW_ASSERT(sd->get_bound_worker_id() == worker_id_);
    GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    // If we received 0 bytes, the remote side has close the connection.
    if (0 == num_bytes_received)
    {
#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Zero-bytes receive on socket index: " << sd->get_socket_info_index() << ". Remote side closed the connection." << GW_ENDL;
#endif

        return SCERRGWSOCKETCLOSEDBYPEER;
    }

    // Updating connection timestamp.
    sd->UpdateConnectionTimeStamp();

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Adding to accumulated bytes.
    accum_buf->AddAccumulatedBytes(num_bytes_received);

    // Incrementing statistics.
    worker_stats_bytes_received_ += num_bytes_received;

    // Increasing number of receives.
    worker_stats_recv_num_++;

#ifdef GW_PROXY_MODE

    // Checking if this is a proxied server socket.
    if (sd->HasProxySocket())
    {
        // Aggregation is done separately.
        if (!sd->GetSocketAggregatedFlag())
        {
            // Posting cloning receive since all data is accumulated.
            uint32_t err_code = sd->CloneToReceive(this);
            if (err_code)
                return err_code;
        }

        // Making sure that sd is just send.
        sd->reset_socket_representer_flag();

        // Finished receiving from proxied server,
        // now sending to the original user.
        sd->ExchangeToProxySocket();

        // Setting number of bytes to send.
        sd->get_accum_buf()->PrepareToSendOnProxy();

        // Sending data to user.
        return Send(sd);
    }

#endif

    // Assigning last received bytes.
    if (sd->get_accumulating_flag())
    {
        // Checking if we have not accumulated everything yet.
        if (!sd->get_accum_buf()->IsAccumulationComplete())
        {
            // Checking if we are called already from Receive to avoid recursiveness.
            if (!called_from_receive)
            {
                return Receive(sd);
            }
            else
            {
                // Just indicating this way that Receive should be called again.
                called_from_receive = false;

                return 0;
            }
        }
        else
        {
            sd->reset_accumulating_flag();
        }
    }

    // Resetting the session based on protocol.
    sd->ResetSessionBasedOnProtocol();

    ProfilerStart(worker_id_, utils::ProfilerEnums::Empty);
    ProfilerStop(worker_id_, utils::ProfilerEnums::Empty);

    // Running the handler.
    return RunReceiveHandlers(sd);
}

// Running send on socket data.
uint32_t GatewayWorker::Send(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Send: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking that socket data is valid.
    sd->CheckForValidity();

    // Checking that socket arrived on correct worker.
    GW_ASSERT(sd->get_bound_worker_id() == worker_id_);
    GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    // Checking if aggregation is involved.
    if (sd->GetSocketAggregatedFlag())
    {
        // Increasing number of sends.
        worker_stats_sent_num_++;
        g_gateway.num_aggregated_send_queued_messages_++;

        return SendOnAggregationSocket(sd);
    }

    // Start sending on socket.
    uint32_t num_sent_bytes, err_code;

    err_code = sd->Send(this, &num_sent_bytes);

    // Checking if operation completed immediately.
    if (0 != err_code)
    {
        int32_t wsa_err_code = WSAGetLastError();

#ifdef GW_LOOPED_TEST_MODE
        wsa_err_code = WSA_IO_PENDING;
#endif

        // Checking if IOCP event was scheduled.
        if (WSA_IO_PENDING != wsa_err_code)
        {
#ifdef GW_WARNINGS_DIAG
            GW_PRINT_WORKER << "Failed WSASend: socket " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << ". Disconnecting socket..." << GW_ENDL;

            PrintLastError();
#endif

            return SCERRGWFAILEDWSASEND;
        }
    }
#ifdef GW_IOCP_IMMEDIATE_COMPLETION
    else
    {
        // Finish send operation.
        err_code = FinishSend(sd, num_sent_bytes);
        if (err_code)
            return err_code;
    }
#endif

    // NOTE: Setting socket data to null, so other
    // manipulations on it are not possible.
    sd = NULL;

    g_gateway.num_pending_sends_++;

    return 0;
}

// Socket send finished.
__forceinline uint32_t GatewayWorker::FinishSend(SocketDataChunkRef sd, int32_t num_bytes_sent)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishSend: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking that socket data is valid.
    sd->CheckForValidity();

    // Checking that socket arrived on correct worker.
    GW_ASSERT(sd->get_bound_worker_id() == worker_id_);
    GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

    // Checking disconnect state.
    if (sd->get_disconnect_after_send_flag())
        return SCERRGWDISCONNECTAFTERSENDFLAG;

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    // If we received 0 bytes, the remote side has close the connection.
    if (0 == num_bytes_sent)
    {
#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Zero-bytes sent on socket index: " << sd->get_socket_info_index() << ". Remote side closed the connection." << GW_ENDL;
#endif

        return SCERRGWSOCKETCLOSEDBYPEER;
    }

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Checking that we processed correct number of bytes.
    GW_ASSERT(num_bytes_sent == accum_buf->get_chunk_num_available_bytes());

    // Updating connection timestamp.
    sd->UpdateConnectionTimeStamp();

    // Incrementing statistics.
    worker_stats_bytes_sent_ += num_bytes_sent;

    // Increasing number of sends.
    worker_stats_sent_num_++;

    // Checking if socket data is for receiving.
    if (sd->get_socket_representer_flag())
    {
        // Resets data buffer offset.
        sd->ResetUserDataOffset();

        // Resetting buffer information.
        sd->ResetAccumBuffer();

        // Resetting safe flags.
        sd->ResetSafeFlags();

        // Performing receive.
        return Receive(sd);
    }

#ifdef GW_TESTING_MODE

    // Checking if its gateway client.
    if (!g_gateway.setting_is_master())
    {
        // Always posting cloning receive when the test data is sent.
        SetReceiveClone(sd);

        return 0;
    }

#endif

    // Returning chunks to pool.
    ReturnSocketDataChunksToPool(sd);

    g_gateway.num_pending_sends_--;

    return 0;
}

// Returns given socket data chunk to private chunk pool.
void GatewayWorker::ReturnSocketDataChunksToPool(SocketDataChunkRef sd)
{
#ifdef GW_COLLECT_SOCKET_STATISTICS
#ifndef GW_TESTING_MODE
    GW_ASSERT(sd->get_socket_diag_active_conn_flag() == false);
#endif
#endif

#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Returning chunk to pool: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    worker_chunks_.ReleaseChunk(sd);

    // IMPORTANT: Preventing further usages of this socket data.
    sd = NULL;
}

// Processes sockets that should be disconnected.
void GatewayWorker::ProcessSocketDisconnectList()
{
    if (0 == sockets_indexes_to_disconnect_.size())
        return;

    for (std::list<session_index_type>::const_iterator it = sockets_indexes_to_disconnect_.begin();
        it != sockets_indexes_to_disconnect_.end();
        ++it)
    {
        uint32_t err_code = DisconnectSocket(*it);
        GW_ASSERT(0 == err_code);
    }

    sockets_indexes_to_disconnect_.clear();
}

// Disconnects arbitrary socket.
uint32_t GatewayWorker::DisconnectSocket(session_index_type socket_index)
{
    // Getting existing socket info copy.
    ScSocketInfoStruct global_socket_info_copy = g_gateway.GetGlobalSocketInfoCopy(socket_index);

    // Creating new socket data and setting required parameters.
    SocketDataChunk* temp_sd;

    // NOTE: Fetching chunk from database 0.
    uint32_t err_code = CreateSocketData(socket_index, temp_sd);
    if (err_code)
        return err_code;

    temp_sd->AssignSession(global_socket_info_copy.session_);

    DisconnectAndReleaseChunk(temp_sd);

    return 0;
}

// Initiates receive on arbitrary socket.
uint32_t GatewayWorker::ReceiveOnSocket(session_index_type socket_index)
{
    // Getting existing socket info copy.
    ScSocketInfoStruct global_socket_info_copy = g_gateway.GetGlobalSocketInfoCopy(socket_index);

    // Creating new socket data and setting required parameters.
    SocketDataChunk* temp_sd;

    // NOTE: Fetching chunk from database 0.
    uint32_t err_code = CreateSocketData(socket_index, temp_sd);
    if (err_code)
        return err_code;

    temp_sd->AssignSession(global_socket_info_copy.session_);
    temp_sd->set_socket_representer_flag();

    SetReceiveClone(temp_sd);

    return 0;
}

// Running disconnect on socket data.
// NOTE: Socket data chunk can not be used after this function is called!
void GatewayWorker::DisconnectAndReleaseChunk(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Disconnect: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking that socket data is valid.
    sd->CheckForValidity();

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        goto RELEASE_CHUNK_TO_POOL;

    // NOTE: The following is needed because the actual owner of the socket
    // will not pass the CompareUniqueSocketId check on the next IO operation.
    // Setting socket representer.
    sd->set_socket_representer_flag();
    sd->set_socket_diag_active_conn_flag();

    uint32_t err_code;

    // Sending dead session if its a WebSocket.
    if (MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS == sd->get_type_of_network_protocol())
    {
        // Verifying that session is correct and sending delete socket on database.
        if (!sd->get_destroy_sent_flag())
        {
            err_code = sd->SendDeleteSession(this);
            GW_ASSERT(0 == err_code);
            sd->set_destroy_sent_flag();
        }
        else
        {
            sd->reset_destroy_sent_flag();
        }
    }

    // Setting unique socket id.
    sd->GenerateUniqueSocketInfoIds(GenerateSchedulerId());

    // Calling DisconnectEx.
    err_code = sd->Disconnect(this);
    GW_ASSERT(!err_code);

    // Checking if operation completed immediately. 
    int32_t wsa_err_code = WSAGetLastError();

#ifdef GW_LOOPED_TEST_MODE
    wsa_err_code = WSA_IO_PENDING;
#endif

    // Checking if IOCP event was scheduled.
    if (WSA_IO_PENDING != wsa_err_code)
    {
#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Failed DisconnectEx: socket " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << ". Disconnecting socket..." << GW_ENDL;
        PrintLastError();
#endif
        GW_ASSERT(false);

        // Finish disconnect operation.
        // (e.g. returning to pool or starting accept).
        if (FinishDisconnect(sd))
        {
            // Closing socket resource.
            closesocket(sd->GetSocket());

            goto RELEASE_CHUNK_TO_POOL;
        }

        return;
    }

    // NOTE: Setting socket data to null, so other
    // manipulations on it are not possible.
    sd = NULL;

    // The disconnect operation is pending.
    return;

    // Returning the chunk to pool.
RELEASE_CHUNK_TO_POOL:

    // Resetting socket representer flags.
    sd->reset_socket_representer_flag();
    sd->reset_socket_diag_active_conn_flag();

    // Returning chunks to pool.
    ReturnSocketDataChunksToPool(sd);
}

// Socket disconnect finished.
__forceinline uint32_t GatewayWorker::FinishDisconnect(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishDisconnect: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    GW_ASSERT(sd->get_type_of_network_oper() != UNKNOWN_SOCKET_OPER);
#endif

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    // Deleting session.
    sd->DeleteGlobalSessionOnDisconnect();

    // Updating the statistics.
#ifdef GW_COLLECT_SOCKET_STATISTICS
    if (sd->get_socket_diag_active_conn_flag())
    {
        ChangeNumActiveConnections(sd->GetPortIndex(), -1);
        sd->reset_socket_diag_active_conn_flag();
    }
#endif

    // Checking if it was an accepting socket.
    if (ACCEPT_SOCKET_OPER == sd->get_type_of_network_oper())
        ChangeNumAcceptingSockets(sd->GetPortIndex(), -1);

    // Removing from active sockets.
    RemoveFromActiveSockets(sd->GetPortIndex());

    // Releasing socket resources.
    closesocket(sd->GetSocket());

#ifdef GW_TESTING_MODE

    if (!g_gateway.setting_is_master())
    {
        if (!g_gateway.AllEchoesSent())
        {
            // Performing connect.
            return Connect(sd, g_gateway.get_server_addr());
        }
        else
        {
            // Do nothing.
            return 0;
        }
    }

#endif

    // Resetting the socket data.
    sd->ResetOnDisconnect(this);

    // Returning chunks to pool.
    ReturnSocketDataChunksToPool(sd);

    return 0;
}

// Running connect on socket data.
uint32_t GatewayWorker::Connect(SocketDataChunkRef sd, sockaddr_in *server_addr)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Connect: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    while(TRUE)
    {
        // Start connecting socket.

        // Setting unique socket id.
        sd->GenerateUniqueSocketInfoIds(GenerateSchedulerId());

        // Calling ConnectEx.
        uint32_t err_code = sd->Connect(this, server_addr);

        // Checking if operation completed immediately.
        GW_ASSERT(TRUE != err_code);

        int32_t wsa_err_code = WSAGetLastError();

#ifdef GW_LOOPED_TEST_MODE
        wsa_err_code = WSA_IO_PENDING;
#endif

        // Checking if IOCP event was scheduled.
        if (WSA_IO_PENDING != wsa_err_code)
        {
            if (WAIT_TIMEOUT == wsa_err_code)
            {
#ifdef GW_ERRORS_DIAG
                GW_PRINT_WORKER << "Timeout in ConnectEx. Retrying..." << GW_ENDL;
#endif
                continue;
            }

#ifdef GW_WARNINGS_DIAG
            GW_PRINT_WORKER << "Failed ConnectEx: socket " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << ". Disconnecting socket..." << GW_ENDL;

            PrintLastError();
#endif
            
            return SCERRGWCONNECTEXFAILED;
        }

        // NOTE: Setting socket data to null, so other
        // manipulations on it are not possible.
        sd = NULL;

        break;
    }

    return 0;
}

// Socket connect finished.
__forceinline uint32_t GatewayWorker::FinishConnect(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishConnect: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking correct unique socket.
    GW_ASSERT(true == sd->CompareUniqueSocketId());

#ifndef GW_LOOPED_TEST_MODE
    // Setting SO_UPDATE_CONNECT_CONTEXT.
    if (setsockopt(sd->GetSocket(), SOL_SOCKET, SO_UPDATE_CONNECT_CONTEXT, NULL, 0))
    {
        GW_PRINT_WORKER << "Can't set SO_UPDATE_CONNECT_CONTEXT on socket." << GW_ENDL;
        return SCERRGWCONNECTEXFAILED;
    }
#endif

    // Since we are proxying this instance represents the socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Changing active connections number.
    ChangeNumActiveConnections(sd->GetPortIndex(), 1);

    sd->set_socket_diag_active_conn_flag();

    sd->set_type_of_network_oper(UNKNOWN_SOCKET_OPER);

#endif

#ifdef GW_PROXY_MODE

    // Checking if we are in proxy mode.
    GW_ASSERT(sd->HasProxySocket() == true);

    // Resuming receive on initial socket.
    uint32_t err_code = ReceiveOnSocket(sd->GetProxySocketIndex());
    if (err_code)
        return err_code;

    // Sending to proxied server.
    return Send(sd);

#endif

#ifdef GW_TESTING_MODE

    // Connect is done only on a client.
    if (!g_gateway.setting_is_master())
    {
        switch (g_gateway.setting_mode())
        {
            case GatewayTestingMode::MODE_GATEWAY_RAW:
            case GatewayTestingMode::MODE_GATEWAY_SMC_RAW:
            case GatewayTestingMode::MODE_GATEWAY_SMC_APPS_RAW:
            {
                // Checking that not all echoes are sent.
                if (!g_gateway.AllEchoesSent())
                {
                    // Sending echo request to server.
                    return SendRawEcho(sd, g_gateway.GetNextEchoNumber());
                }
                break;
            }
            
            case GatewayTestingMode::MODE_GATEWAY_HTTP:
            case GatewayTestingMode::MODE_GATEWAY_SMC_HTTP:
            case GatewayTestingMode::MODE_GATEWAY_SMC_APPS_HTTP:
            {
                // Checking that not all echoes are sent.
                if (!g_gateway.AllEchoesSent())
                {
                    // Sending echo request to server.
                    return SendHttpEcho(sd, g_gateway.GetNextEchoNumber());
                }
                break;
            }
        }
    }

#endif

    return SCERRGWCONNECTEXFAILED;
}

// Running accept on socket data.
uint32_t GatewayWorker::Accept(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Accept: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Start accepting on socket.

    // Updating number of accepting sockets.
    ChangeNumAcceptingSockets(sd->GetPortIndex(), 1);

    // Setting unique socket id.
    sd->GenerateUniqueSocketInfoIds(GenerateSchedulerId());

    // Calling AcceptEx.
    uint32_t err_code = sd->Accept(this);

    // Checking if operation completed immediately.
    GW_ASSERT(TRUE != err_code);

    int32_t wsa_err_code = WSAGetLastError();

#ifdef GW_LOOPED_TEST_MODE
    wsa_err_code = WSA_IO_PENDING;
#endif

    // Checking if IOCP event was scheduled.
    if (WSA_IO_PENDING != wsa_err_code)
    {
#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Failed AcceptEx: " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;

        PrintLastError();
#endif

        return SCERRGWFAILEDACCEPTEX;
    }

    // NOTE: Setting socket data to null, so other
    // manipulations on it are not possible.
    sd = NULL;

    return 0;
}

// Socket accept finished.
uint32_t GatewayWorker::FinishAccept(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishAccept: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

#ifdef GW_LOOPED_TEST_MODE

    // Checking that we don't exceed number of active connections.
    if (num_created_conns_worker_ < g_gateway.setting_num_connections_to_master_per_worker())
    {
        // Creating new set of prepared connections.
        uint32_t err_code = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, sd->GetPortIndex());
        GW_ERR_CHECK(err_code);
    }

#endif


#ifndef GW_TESTING_MODE

    // Checking if was rebalanced.
    if (0 == worker_id_) {

        // Decreasing number of accepting sockets.
        int64_t cur_num_accept_sockets = ChangeNumAcceptingSockets(sd->GetPortIndex(), -1);

        // Checking if we need to extend number of accepting sockets.
        if (cur_num_accept_sockets < ACCEPT_ROOF_STEP_SIZE)
        {
            // Creating new set of prepared connections.
            uint32_t err_code = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, sd->GetPortIndex());
            GW_ERR_CHECK(err_code);
        }

        worker_id_type least_busy_worker_id = GetLeastBusyWorkerId(sd->GetPortIndex());

        HANDLE worker_iocp_handler = g_gateway.get_worker(least_busy_worker_id)->get_worker_iocp();

        // Associating new socket with least busy worker IOCP.
        HANDLE iocp_handler = CreateIoCompletionPort((HANDLE) sd->GetSocket(), worker_iocp_handler, 0, 1);
        GW_ASSERT(iocp_handler == worker_iocp_handler);

        // Checking if rebalanced worker is different.
        if (0 != least_busy_worker_id) {

            // Getting temporary rebalance container.
            RebalancedSocketInfo* rsi = PopRebalanceSocketInfo();
            if (NULL == rsi) {
                rsi = (RebalancedSocketInfo*)_aligned_malloc(sizeof(RebalancedSocketInfo), MEMORY_ALLOCATION_ALIGNMENT);
            }
            rsi->Init(sd->GetPortIndex(), sd->GetSocket());

            // Returning all allocated resources except socket.
            sd->ResetAllFlags();
            sd->ReleaseSocketIndex(this);
            ReturnSocketDataChunksToPool(sd);

            g_gateway.get_worker(least_busy_worker_id)->PushRebalanceSocketInfo(rsi);

            // Posting this finish Accept on a new worker IOCP.
            WakeUpThreadUsingAPC(g_gateway.get_worker_thread_handle(least_busy_worker_id));

            return 0;
        }
    }

#endif

    // Checking correct unique socket.
    GW_ASSERT(true == sd->CompareUniqueSocketId());

    // Updating connection timestamp.
    sd->UpdateConnectionTimeStamp();

    uint32_t err_code;

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    err_code = sd->SetAcceptSocketOptions();
    GW_ASSERT (0 == err_code);

    // This socket data is socket representation.
    sd->set_socket_representer_flag();

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Changing active connections number.
    ChangeNumActiveConnections(sd->GetPortIndex(), 1);

    sd->set_socket_diag_active_conn_flag();

#endif

    // Adding to active sockets for this worker.
    AddToActiveSockets(sd->GetPortIndex());

    // Checking client IP address information.
    sockaddr_in client_addr = *(sockaddr_in *)(sd->get_accept_or_params_data() + sizeof(sockaddr_in) + 16);
    sd->set_client_ip_info(client_addr.sin_addr.S_un.S_addr);

    // Checking if white list is on.
    if (!g_gateway.CheckIpForWhiteList(sd->get_client_ip_info()))
        return SCERRGWIPISNOTONWHITELIST;

    // Performing receive.
    return Receive(sd);
}

// Checks if cloning was performed and does operations.
__forceinline uint32_t GatewayWorker::ProcessReceiveClones(bool just_delete_clone)
{
    // Checking if there was no clone.
    if (sd_receive_clone_ == NULL)
        return 0;

    uint32_t err_code = 0;
    while (sd_receive_clone_ != NULL)
    {
        // NOTE: Taking just a pointer without reference.
        SocketDataChunk* sd = sd_receive_clone_;

#ifdef GW_SOCKET_DIAG
        GW_PRINT_WORKER << "Processing clone: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

        // Invalidating clone for more reuse.
        sd_receive_clone_ = NULL;

        if (just_delete_clone)
        {
            // Disconnecting socket if needed and releasing chunk.
            DisconnectAndReleaseChunk(sd);
        }
        else
        {
            // Performing receive operation.
            err_code = Receive(sd);

            // Checking if any error occurred during socket operation.
            if (err_code)
            {
                // Disconnecting socket if needed and releasing chunk.
                DisconnectAndReleaseChunk(sd);
            }
        }
    }

    return err_code;
}

#if defined(GW_LOOPBACK_AGGREGATION) || defined(GW_SMC_LOOPBACK_AGGREGATION)

// Processes socket info for aggregation loopback.
void GatewayWorker::LoopbackForAggregation(SocketDataChunkRef sd)
{
    char body[1024];
    int32_t body_len = sd->get_http_proto()->get_http_request()->content_len_bytes_;
    GW_ASSERT (body_len <= 1024);

    memcpy(body, (char*)sd + sd->get_http_proto()->get_http_request()->content_offset_, body_len);

    GW_ASSERT(static_cast<uint32_t>(body_len + kHttpGenericHtmlHeaderLength) < sd->get_accum_buf()->get_chunk_num_available_bytes());

    uint8_t* dest_data = sd->get_accum_buf()->get_chunk_orig_buf_ptr();

    // Copying predefined header.
    memcpy(dest_data, kHttpGenericHtmlHeader, kHttpGenericHtmlHeaderLength);

    // Making length a white space.
    *(uint64_t*)(dest_data + kHttpGenericHtmlHeaderInsertPoint) = 0x2020202020202020;

    // Converting content length to string.
    WriteUIntToString((char*)dest_data + kHttpGenericHtmlHeaderInsertPoint, body_len);

    // Copying body to response.
    memcpy(dest_data + kHttpGenericHtmlHeaderLength, body, body_len);

    // We don't need original chunk contents.
    sd->ResetAccumBuffer();

    // Prepare buffer to send outside.
    sd->PrepareForSend(dest_data, kHttpGenericHtmlHeaderLength + body_len);

    uint32_t err_code = RunFromDbHandlers(sd);

    GW_ASSERT (0 == err_code);
}

#endif

// Main gateway worker routine.
uint32_t GatewayWorker::WorkerRoutine()
{
    BOOL compl_status = false;
    OVERLAPPED_ENTRY* fetched_ovls = new OVERLAPPED_ENTRY[MAX_FETCHED_OVLS];
    uint32_t num_fetched_ovls = 0;
    uint32_t err_code = 0;
    uint32_t oper_num_bytes = 0, flags = 0, oldTimeMs = timeGetTime();
    uint32_t next_sleep_interval_ms = INFINITE;

#ifdef WORKER_NO_SLEEP
    next_sleep_interval_ms = 0;
#endif

    sd_receive_clone_ = NULL;

    // Starting worker infinite loop.
    while (TRUE)
    {
        // Getting IOCP status.
#ifdef GW_LOOPED_TEST_MODE
        compl_status = ProcessEmulatedNetworkOperations(fetched_ovls, &num_fetched_ovls, MAX_FETCHED_OVLS);
#else
        compl_status = GetQueuedCompletionStatusEx(worker_iocp_, fetched_ovls, MAX_FETCHED_OVLS, (PULONG)&num_fetched_ovls, next_sleep_interval_ms, TRUE);
#endif

        // Check if global lock is set.
        if (g_gateway.global_lock())
            g_gateway.SuspendWorker(this);

        // Checking if operation successfully completed.
        if (TRUE == compl_status)
        {
            // Processing each retrieved overlapped.
            for (uint32_t i = 0; i < num_fetched_ovls; i++)
            {
                // Obtaining socket data structure.
                SocketDataChunk* sd = (SocketDataChunk*)(fetched_ovls[i].lpOverlapped);

#ifdef GW_SOCKET_DIAG
                GW_PRINT_WORKER << "GetQueuedCompletionStatusEx: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

                // Checking for socket data correctness.
                sd->CheckForValidity();
                GW_ASSERT(sd->get_socket_info_index() < g_gateway.setting_max_connections());

                // Checking that socket is bound to the correct worker.
                GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

                // Checking error code (lower 32-bits of Internal).
                if (ERROR_SUCCESS != (uint32_t) fetched_ovls[i].lpOverlapped->Internal)
                {
                    uint32_t flags;
                    BOOL success = WSAGetOverlappedResult(sd->GetSocket(), fetched_ovls[i].lpOverlapped, (LPDWORD)&oper_num_bytes, FALSE, (LPDWORD)&flags);
                    GW_ASSERT(FALSE == success);

                    // IOCP operation has completed but with error.
                    GW_ASSERT(success != WSAGetLastError());

                    // Checking correct unique socket.
                    if ((sd->CompareUniqueSocketId()) && (sd->get_socket_representer_flag())) {

                        err_code = FinishDisconnect(sd);
                        GW_ASSERT(0 == err_code);

                    } else {

                        // Resetting socket representer flags.
                        sd->reset_socket_representer_flag();
                        sd->reset_socket_diag_active_conn_flag();

                        // Returning chunks to pool.
                        ReturnSocketDataChunksToPool(sd);
                    }

                    continue;
                }

                // Getting number of bytes in operation.
                oper_num_bytes = fetched_ovls[i].dwNumberOfBytesTransferred;

                // Checking type of operation.
                // NOTE: Any failure on the following operations means that chunk is still in use!
                switch (sd->get_type_of_network_oper())
                {
                    // ACCEPT finished.
                    case ACCEPT_SOCKET_OPER:
                    {
                        err_code = FinishAccept(sd);
                        break;
                    }

                    // CONNECT finished.
                    case CONNECT_SOCKET_OPER:
                    {
                        err_code = FinishConnect(sd);
                        break;
                    }

                    // DISCONNECT finished.
                    case DISCONNECT_SOCKET_OPER:
                    {
                        err_code = FinishDisconnect(sd);
                        break;
                    }

                    // SEND finished.
                    case SEND_SOCKET_OPER:
                    {
                        err_code = FinishSend(sd, oper_num_bytes);
                        break;
                    }

                    // RECEIVE finished.
                    case RECEIVE_SOCKET_OPER:
                    {
                        bool called_from_receive = false;
                        err_code = FinishReceive(sd, oper_num_bytes, called_from_receive);
                        break;
                    }

                    // Unknown operation.
                    default:
                    {
                        GW_ASSERT(false);
                    }
                }

                // Checking if any error occurred during socket operations.
                if (err_code)
                {
                    // Disconnecting this socket data.
                    DisconnectAndReleaseChunk(sd);

                    // Releasing the cloned chunk.
                    ProcessReceiveClones(true);
                }
                else
                {
                    // Processing clones during last iteration.
                    ProcessReceiveClones(false);
                }
            }
        }
#ifndef GW_LOOPED_TEST_MODE
        else
        {
            err_code = WSAGetLastError();

            // Checking if it was an APC event.
            if ((STATUS_USER_APC != err_code) && (STATUS_TIMEOUT != err_code))
            {
                PrintLastError();
                return err_code;
            }
            else
            {
#ifndef GW_TESTING_MODE
                if (worker_id_ != 0) {

                    RebalancedSocketInfo* rsi = PopRebalanceSocketInfo();

                    if (NULL != rsi) {

                        port_index_type pi = rsi->get_port_index();
                        SOCKET s = rsi->get_socket();

                        // Returning socket info back to origin.
                        g_gateway.get_worker(0)->PushRebalanceSocketInfo(rsi);

                        // Creating new socket data structure inside chunk.
                        SocketDataChunk* new_sd = NULL;

                        // Getting new socket index.
                        session_index_type new_socket_index = g_gateway.ObtainFreeSocketIndex(this, s, pi, false);

                        // Creating new socket data.
                        err_code = CreateSocketData(new_socket_index, new_sd);

                        if (err_code) {
                            // Just closing the socket.
                            closesocket(s);
                            continue;
                        }

                        // Finishing accept.
                        new_sd->set_type_of_network_oper(ACCEPT_SOCKET_OPER);
                        err_code = FinishAccept(new_sd);

                        if (err_code) {
                            // Disconnecting this socket data.
                            DisconnectAndReleaseChunk(new_sd);

                            // Releasing the cloned chunk.
                            ProcessReceiveClones(true);
                        }
                    }
                }
#endif
            }
        }
#endif

        // Processing socket disconnect list.
        ProcessSocketDisconnectList();

        next_sleep_interval_ms = INFINITE;

        // Checking if we have aggregation.
        if (INVALID_PORT_NUMBER != g_gateway.setting_aggregation_port())
        {
            // Setting timeout on GetQueuedCompletionStatusEx.
            next_sleep_interval_ms = 3;

            // Stopping aggregation timer to check if we need to send.
            aggr_timer_.Stop();

            // Checking if we have aggregated.
            if (aggr_timer_.DurationMs() >= 3.0)
            {
                // Processing aggregated chunks.
                // NOTE: Do nothing about error codes.
                SendAggregatedChunks();

                // Resetting the aggregation timer.
                aggr_timer_.Reset();
                aggr_timer_.Start();
            }
        }

        // Scanning all channels.
        err_code = ScanChannels(next_sleep_interval_ms);
        if (err_code)
            return err_code;

#ifdef WORKER_NO_SLEEP
        next_sleep_interval_ms = 0;
#endif

        // NOTE: Checking inactive sockets cleanup (only first worker).
        if ((g_gateway.get_num_sockets_to_cleanup_unsafe()) && (worker_id_ == 0))
            g_gateway.CleanupInactiveSocketsOnWorkerZero();

#ifdef GW_TESTING_MODE
        // Checking if its time to switch to measured test.
        BeginMeasuredTestIfReady();
#endif

    }

    GW_ASSERT(false);
}

// Scans all channels for any incoming chunks.
uint32_t GatewayWorker::ScanChannels(uint32_t& next_sleep_interval_ms)
{
    uint32_t err_code;

    for (int32_t i = 0; i < g_gateway.get_num_dbs_slots(); i++)
    {
        // Scan channels.
        WorkerDbInterface *db = GetWorkerDb(i);
        if (NULL != db)
        {
            // Checking if database deletion is started.
            if (g_gateway.GetDatabase(i)->IsDeletionStarted())
            {
                // Checking that database is ready for deletion (i.e. no pending sockets and chunks).
                if (g_gateway.GetDatabase(i)->IsReadyForCleanup())
                {
                    // Entering global lock.
                    EnterGlobalLock();

                    // Deleting all ports that are empty from chunks, etc.
                    g_gateway.CleanUpEmptyPorts();

                    GW_ASSERT(true == g_gateway.GetDatabase(i)->IsReadyForCleanup());

                    // Finally completely deleting database object and closing shared memory.
                    DeleteInactiveDatabase(i);

#ifdef GW_DATABASES_DIAG
                    GW_PRINT_WORKER << "Deleted shared memory for db slot: " << i << GW_ENDL;
#endif
                    g_gateway.GetDatabase(i)->ReleaseHoldingWorker();

                    // Leaving global lock.
                    LeaveGlobalLock();

                }
                else
                {
                    // Gateway needs to loop for a while because of chunks being released.
                    next_sleep_interval_ms = 100;

                    // Releasing all private chunks to shared pool.
                    db->ReturnAllPrivateChunksToSharedPool();
                }
            }
            else
            {
                // Scanning channels first.
                err_code = db->ScanChannels(this, next_sleep_interval_ms);
                if (err_code)
                    return err_code;
            }
        }
    }

    return 0;
}

// Creates the socket data structure.
uint32_t GatewayWorker::CreateSocketData(
    const session_index_type socket_info_index,
    SocketDataChunkRef out_sd,
    const int32_t data_len)
{
    // Obtaining chunk from gateway private memory.
    // Checking if its an aggregation socket.
    if (g_gateway.IsAggregatingPort(socket_info_index))
        out_sd = worker_chunks_.ObtainChunk(GatewayChunkDataSizes[NumGatewayChunkSizes - 1]);
    else
        out_sd = worker_chunks_.ObtainChunk(data_len);

    // Initializing socket data.
    out_sd->Init(socket_info_index, worker_id_);
    
#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Creating socket data: socket index " << out_sd->get_socket_info_index() << ":" << out_sd->get_unique_socket_id() << ":" << (uint64_t)out_sd << GW_ENDL;
#endif

    return 0;
}

// Adds new active database.
uint32_t GatewayWorker::AddNewDatabase(db_index_type db_index)
{
    worker_dbs_[db_index] = new WorkerDbInterface(db_index, worker_id_);

    return 0;
}

// Push given chunk to database queue.
uint32_t GatewayWorker::PushSocketDataToDb(SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id)
{
    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKETWHENPUSHING;

    // Getting database to which this chunk belongs.
    WorkerDbInterface *db = GetWorkerDb(sd->GetDestDbIndex());

    // Pushing chunk to that database.
    if (NULL != db)
        db->PushSocketDataToDb(this, sd, handler_id);
    else
        ReturnSocketDataChunksToPool(sd);

    return 0;
}

// Deleting inactive database.
void GatewayWorker::DeleteInactiveDatabase(db_index_type db_index)
{
    if (worker_dbs_[db_index] != NULL)
    {
        delete worker_dbs_[db_index];
        worker_dbs_[db_index] = NULL;
    }
}

// Sends given body.
uint32_t GatewayWorker::SendHttpBody(
    SocketDataChunkRef sd,
    const char* body,
    const int32_t body_len)
{
    GW_ASSERT(body_len < 3800);
    char temp_resp[4096];

    // Copying predefined header.
    memcpy(temp_resp, kHttpGenericHtmlHeader, kHttpGenericHtmlHeaderLength);

    // Making length a white space.
    *(uint64_t*)(temp_resp + kHttpGenericHtmlHeaderInsertPoint) = 0x2020202020202020;

    // Converting content length to string.
    WriteUIntToString(temp_resp + kHttpGenericHtmlHeaderInsertPoint, body_len);

    // Copying body to response.
    memcpy(temp_resp + kHttpGenericHtmlHeaderLength, body, body_len);

    // Sending predefined response.
    return SendPredefinedMessage(sd, temp_resp, kHttpGenericHtmlHeaderLength + body_len);
}

// Sends given predefined response.
uint32_t GatewayWorker::SendPredefinedMessage(
    SocketDataChunkRef sd,
    const char* message,
    const int32_t message_len)
{
    // We don't need original chunk contents.
    sd->ResetAccumBuffer();

    // Checking if data fits inside chunk.
    if (message_len > (int32_t)sd->get_accum_buf()->get_chunk_num_available_bytes())
    {
        uint32_t err_code = SocketDataChunk::ChangeToBigger(this, sd, message_len);
        if (err_code)
            return err_code;
    }

    // Checking if message should be copied.
    if (message)
        memcpy(sd->get_accum_buf()->get_chunk_orig_buf_ptr(), message, message_len);

    // Prepare buffer to send outside.
    sd->PrepareForSend(sd->get_accum_buf()->get_chunk_orig_buf_ptr(), message_len);

    // Sending data.
    return Send(sd);
}

#ifdef GW_TESTING_MODE

// Checks if measured test should be started and begins it.
void GatewayWorker::BeginMeasuredTestIfReady()
{
    if ((!g_gateway.get_started_measured_test()) &&
        (0 == worker_id_) &&
#ifdef GW_LOOPED_TEST_MODE
        (0 == emulated_preparation_network_events_queue_.get_num_entries()) &&
        (0 == g_gateway.GetNumberOfPreparationNetworkEventsAllWorkers()) &&
#endif
        (g_gateway.GetNumberOfCreatedConnectionsAllWorkers() >= g_gateway.setting_num_connections_to_master()))
    {
        // Entering global lock.
        EnterGlobalLock();

        // Starting test measurements.
        g_gateway.StartMeasuredTest();

        // Leaving global lock.
        LeaveGlobalLock();
    }
}

// Sends HTTP echo to master.
uint32_t GatewayWorker::SendHttpEcho(SocketDataChunkRef sd, echo_id_type echo_id)
{
#ifdef GW_ECHO_STATISTICS
    GW_PRINT_WORKER << "Sending echo: " << echo_id << GW_ENDL;
#endif

    // Generating HTTP request.
    uint32_t http_request_len = g_gateway.GenerateHttpRequest((char*)sd->get_data_blob(), echo_id);

    // Sending Ping request to server.
    return SendPredefinedMessage(sd, NULL, http_request_len);
}

// Sends raw echo to master.
uint32_t GatewayWorker::SendRawEcho(SocketDataChunkRef sd, echo_id_type echo_id)
{
#ifdef GW_ECHO_STATISTICS
    GW_PRINT_WORKER << "Sending echo: " << echo_id << GW_ENDL;
#endif

    // Inserting raw echo id.
    (*(int64_t*)sd->get_data_blob()) = echo_id;

    // Sending Ping request to server.
    return SendPredefinedMessage(sd, NULL, sizeof(echo_id));
}

#endif

#ifdef GW_LOOPED_TEST_MODE

// Processes emulated network operations.
bool GatewayWorker::ProcessEmulatedNetworkOperations(
    OVERLAPPED_ENTRY* fetched_ovls,
    uint32_t* num_fetched_ovls,
    int32_t max_fetched)
{
    int32_t num_processed = 0, num_entries_left;
    uint32_t err_code;
    SocketDataChunk* sd;

    // Checking if its time to switch to measured test.
    BeginMeasuredTestIfReady();

    // Iterating over all network operations in queue.
    if (!g_gateway.get_started_measured_test())
    {
        num_entries_left = emulated_preparation_network_events_queue_.get_num_entries();
    }
    else
    {
        GW_ASSERT(0 == emulated_preparation_network_events_queue_.get_num_entries());

        num_entries_left = emulated_measured_network_events_queue_.get_num_entries();
    }

    // Looping until all entries are processed or we filled given buffer.
    while ((num_entries_left > 0) && (num_processed < max_fetched))
    {
        // Popping latest socket data.
        if (!g_gateway.get_started_measured_test())
        {
            sd = emulated_preparation_network_events_queue_.PopFront();
        }
        else
        {
            sd = emulated_measured_network_events_queue_.PopFront();
        }

        num_entries_left--;

        // Clearing overlapped entry.
        memset(fetched_ovls + num_processed, 0, sizeof(OVERLAPPED_ENTRY));

        AccumBuffer* accum_buffer = sd->get_accum_buf();

        switch (sd->get_type_of_network_oper())
        {
            // ACCEPT finished.
            case ACCEPT_SOCKET_OPER:
            {
                break;
            }

            // CONNECT finished.
            case CONNECT_SOCKET_OPER:
            {
                break;
            }

            // DISCONNECT finished.
            case DISCONNECT_SOCKET_OPER:
            {
                GW_ASSERT(false);
                break;
            }

            // SEND finished.
            case SEND_SOCKET_OPER: // Processing echo response here.
            {
                echo_id_type echo_id = -1;

#ifdef DONT_CHECK_ECHOES
                err_code = 0;
#else
                // Executing selected echo response processor.
                err_code = g_gateway.get_looped_echo_response_processor()(
                    (char*)sd->get_data_blob(),
                    accum_buffer->get_chunk_num_available_bytes(),
                    &echo_id);
#endif

                GW_ASSERT(0 == err_code);

#ifdef GW_ECHO_STATISTICS
                GW_PRINT_WORKER << "Received echo: " << echo_id << GW_ENDL;
#endif

#ifdef GW_LIMITED_ECHO_TEST
                // Confirming received echo.
                g_gateway.ConfirmEcho(echo_id);
#endif

                // Checking if all echo responses are returned.
                g_gateway.CheckConfirmedEchoResponses(this);

                // Setting number of bytes sent.
                fetched_ovls[num_processed].dwNumberOfBytesTransferred = accum_buffer->get_chunk_num_available_bytes();

                break;
            }

            // RECEIVE finished.
            case RECEIVE_SOCKET_OPER: // Sending echo request here.
            {
                // Checking that not all echoes are sent.
                if (!g_gateway.AllEchoesSent())
                {
                    // Generating echo number.
                    echo_id_type new_echo_num = 0;

#ifdef GW_LIMITED_ECHO_TEST
                    new_echo_num = g_gateway.GetNextEchoNumber();

                    // Checking if we are getting overflowed echo number.
                    if (new_echo_num >= g_gateway.setting_num_echoes_to_master())
                        goto RELEASE_CHUNK;
#endif

#ifdef GW_ECHO_STATISTICS
                    GW_COUT << "Sending echo: " << new_echo_num << GW_ENDL;
#endif

                    // Executing selected echo request creator.
                    uint32_t num_request_bytes;
                    err_code = g_gateway.get_looped_echo_request_creator()(
                        (char*)sd->get_data_blob(),
                        new_echo_num,
                        &num_request_bytes);

                    GW_ASSERT(0 == err_code);

                    // Assigning number of processed bytes.
                    fetched_ovls[num_processed].dwNumberOfBytesTransferred = num_request_bytes;

                    break;
                }

RELEASE_CHUNK:

#ifdef GW_COLLECT_SOCKET_STATISTICS
                sd->reset_socket_diag_active_conn_flag();
#endif

                // Returning chunks to pool.
                ReturnSocketDataChunksToPool(sd);

                // Just jumping to next processing.
                continue;
            }

            // Unknown operation.
            default:
            {
                GW_ASSERT(false);
            }
        }

        fetched_ovls[num_processed].lpOverlapped = (LPOVERLAPPED)sd;
        num_processed++;
    }

    // Assigning number of processed operations.
    *num_fetched_ovls = num_processed;

    if (num_processed)
        return true;

    return false;
}

#endif

} // namespace network
} // namespace starcounter
