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
    if (worker_iocp_ == NULL)
    {
        GW_PRINT_WORKER << "Failed to create worker IOCP." << GW_ENDL;
        return PrintLastError();
    }

    // Getting global IOCP.
    //worker_iocp_ = g_gateway.get_iocp();

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

    // Initializing profilers.
    profiler_.Init(64);

    aggr_timer_.Start();

    return 0;
}

#ifdef GW_PROXY_MODE

// Allocates a new socket based on existing.
uint32_t GatewayWorker::CreateProxySocket(SocketDataChunkRef proxy_sd)
{
    // Creating new socket.
    SOCKET new_socket;

    // Indicates if we used previously created socket.
    bool reused_socket = false;
    
    // Checking if we can reuse existing socket.
    if (reusable_connect_sockets_.get_num_entries())
    {
        new_socket = reusable_connect_sockets_.PopFront();

#ifdef GW_SOCKET_DIAG
        GW_PRINT_WORKER << "Reusing 'connect' socket: " << new_socket << GW_ENDL;
#endif

        reused_socket = true;
    }
    else
    {
        new_socket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
        if (new_socket == INVALID_SOCKET)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "WSASocket() failed." << GW_ENDL;
#endif
            return PrintLastError();
        }

        // Adding to IOCP.
        HANDLE tempHandle = CreateIoCompletionPort((HANDLE) new_socket, worker_iocp_, 0, 1);
        if (tempHandle != worker_iocp_)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Wrong IOCP returned when attaching socket to IOCP." << GW_ENDL;
#endif
            closesocket(new_socket);

            return PrintLastError();
        }

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

            binding_addr.sin_port = htons(g_gateway.get_last_bind_port_num());

            // Generating new port/interface.
            g_gateway.GenerateNewBindPortInterfaceNumbers();

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

        int32_t onFlag = 1;

        // Does not block close waiting for unsent data to be sent.
        if (setsockopt(new_socket, SOL_SOCKET, SO_DONTLINGER, (char *)&onFlag, 4))
        {
            GW_PRINT_WORKER << "Can't set SO_DONTLINGER on socket." << GW_ENDL;

            closesocket(new_socket);

            return PrintLastError();
        }

#ifdef GW_IOCP_IMMEDIATE_COMPLETION
        // Skipping completion port if operation is already successful.
        SetFileCompletionNotificationModes((HANDLE)new_socket, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);
#endif

        // Putting socket into non-blocking mode.
        ULONG ul = 1;
        uint32_t temp;
        if (WSAIoctl(new_socket, FIONBIO, &ul, sizeof(ul), NULL, 0, (LPDWORD)&temp, NULL, NULL))
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Can't put socket into non-blocking mode." << GW_ENDL;
#endif
            closesocket(new_socket);

            return PrintLastError();
        }
    }

    // Getting new socket index.
    int32_t port_index = proxy_sd->GetPortIndex();
    session_index_type new_socket_index = g_gateway.ObtainFreeSocketIndex(this, proxy_sd->get_db_index(), new_socket, port_index);

#ifdef GW_COLLECT_SOCKET_STATISTICS

    if (!reused_socket)
    {
        // Changing number of created sockets.
        int64_t created_sockets = g_gateway.get_server_port(port_index)->ChangeNumAllocatedConnectSockets(1);

#ifdef GW_SOCKET_DIAG
        GW_PRINT_WORKER << "New sockets amount: " << created_sockets << GW_ENDL;
#endif
    }

#endif

    // Setting receiving socket.
    proxy_sd->SetProxySocketIndex(proxy_sd->get_socket_info_index());

    // Setting new socket.
    proxy_sd->set_socket_info_index(new_socket_index);

    return 0;
}

#endif

// Allocates a bunch of new connections.
uint32_t GatewayWorker::CreateNewConnections(int32_t how_many, int32_t port_index, db_index_type db_index)
{
    uint32_t err_code;
    int32_t curIntNum = 0;

    for (int32_t i = 0; i < how_many; i++)
    {
        SOCKET new_socket;
        bool reused_socket = false;

        // Checking if we can reuse existing socket.
        if (reusable_accept_sockets_.get_num_entries())
        {
            new_socket = reusable_accept_sockets_.PopFront();

#ifdef GW_SOCKET_DIAG
            GW_PRINT_WORKER << "Reusing 'accept' socket: " << new_socket << GW_ENDL;
#endif

            reused_socket = true;
        }
        else
        {
            // Creating new socket.
            new_socket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
            if (new_socket == INVALID_SOCKET)
            {
#ifdef GW_ERRORS_DIAG
                GW_PRINT_WORKER << "WSASocket() failed." << GW_ENDL;
#endif
                return PrintLastError();
            }

            // Adding to IOCP.
            HANDLE tempHandle = CreateIoCompletionPort((HANDLE) new_socket, worker_iocp_, 0, 1);
            if (tempHandle != worker_iocp_)
            {
#ifdef GW_ERRORS_DIAG
                GW_PRINT_WORKER << "Wrong IOCP returned when attaching socket to IOCP." << GW_ENDL;
#endif
                closesocket(new_socket);

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

                    binding_addr.sin_port = htons(g_gateway.get_last_bind_port_num());

                    // Generating new port/interface.
                    g_gateway.GenerateNewBindPortInterfaceNumbers();

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

            // Putting socket into non-blocking mode.
            ULONG ul = 1;
            uint32_t temp;
            if (WSAIoctl(new_socket, FIONBIO, &ul, sizeof(ul), NULL, 0, (LPDWORD)&temp, NULL, NULL))
            {
#ifdef GW_ERRORS_DIAG
                GW_PRINT_WORKER << "Can't put socket into non-blocking mode." << GW_ENDL;
#endif
                closesocket(new_socket);

                return PrintLastError();
            }
        }

        // Creating new socket data structure inside chunk.
        SocketDataChunk* new_sd = NULL;
        
        // Getting new socket index.
        session_index_type new_socket_index = g_gateway.ObtainFreeSocketIndex(this, db_index, new_socket, port_index);

        // Creating new socket data.
        err_code = CreateSocketData(new_socket_index, db_index, new_sd);
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

        if (!reused_socket)
        {
            // Changing number of created sockets.
            int64_t created_sockets = g_gateway.get_server_port(port_index)->ChangeNumAllocatedAcceptSockets(how_many);

#ifdef GW_SOCKET_DIAG
            GW_PRINT_WORKER << "New sockets amount: " << created_sockets << GW_ENDL;
#endif
        }

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
    GW_ASSERT(0 == sd->get_db_index());

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    // Checking that not aggregated socket are trying to receive.
    GW_ASSERT(false == sd->GetSocketAggregatedFlag());

#ifdef GW_IOCP_IMMEDIATE_COMPLETION
// This label is used to avoid recursiveness between Receive and FinishReceive.
START_RECEIVING_AGAIN:
#endif

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Receive: socket index " << sd->get_socket_info_index() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

#ifdef GW_PROFILER_ON
    profiler_.Start("Receive()", 1);
#endif

    uint32_t numBytes, err_code;

    // Checking if we have one or multiple chunks to receive.
    //if (1 == sd->get_num_chunks())
    {
        err_code = sd->ReceiveSingleChunk(this, &numBytes);
    }
    /*else
    {
        errCode = sd->ReceiveMultipleChunks(active_dbs_[sd->get_db_index()]->get_shared_int(), &numBytes);
    }*/

#ifdef GW_PROFILER_ON
    profiler_.Stop(1);
#endif

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
            GW_PRINT_WORKER << "Failed WSARecv: socket index " << sd->get_socket_info_index() << " " <<
                sd->get_chunk_index() << " Disconnecting socket..." << GW_ENDL;

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
            GW_PRINT_WORKER << "Zero-bytes receive on socket index: " << sd->get_socket_info_index() << " " <<
                sd->get_chunk_index() << ". Remote side closed the connection." << GW_ENDL;
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
    GW_PRINT_WORKER << "FinishReceive: socket index " << sd->get_socket_info_index() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

    GW_ASSERT(0 == sd->get_db_index());

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    // If we received 0 bytes, the remote side has close the connection.
    if (0 == num_bytes_received)
    {
#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Zero-bytes receive on socket index: " << sd->get_socket_info_index() << " " <<
            sd->get_chunk_index() << ". Remote side closed the connection." << GW_ENDL;
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
    if (sd->get_proxied_server_socket_flag())
    {
        // Posting cloning receive since all data is accumulated.
        uint32_t err_code = sd->CloneToReceive(this);
        if (err_code)
            return err_code;

        // Setting proxy mode.
        sd_receive_clone_->set_proxied_server_socket_flag();

        // Since we are sending this socket data to client.
        sd->reset_proxied_server_socket_flag();

        // Finished receiving from proxied server,
        // now sending to the original user.
        sd->ExchangeToProxySocket();

        // Setting number of bytes to send.
        sd->get_accum_buf()->PrepareForSend();

        // Sending data to user.
        return Send(sd);
    }

#endif

    // Assigning last received bytes.
    if (sd->get_accumulating_flag())
    {
        // Indicates if all data was accumulated.
        bool is_accumulated;

        // Trying to continue accumulation.
        uint32_t err_code = sd->ContinueAccumulation(this, &is_accumulated);
        if (err_code)
        {
            sd->reset_accumulating_flag();

            return err_code;
        }

        // Checking if we have not accumulated everything yet.
        if (!is_accumulated)
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

    // Processing session according to protocol.
    switch (sd->get_type_of_network_protocol())
    {
        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1:
            sd->ResetSdSession();
        break;

        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS:
            sd->SetSdSessionIfEmpty();
        break;

        default:
            sd->ResetSdSession();
    }

    // Running the handler.
    return RunReceiveHandlers(sd);
}

// Running send on socket data.
uint32_t GatewayWorker::Send(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Send: socket index " << sd->get_socket_info_index() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

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
#ifdef GW_PROFILER_ON
    profiler_.Start("Send()", 2);
#endif

    uint32_t num_sent_bytes, err_code;

    // Checking if we have one or multiple chunks to send.
    if (1 == sd->get_num_chunks())
    {
        err_code = sd->SendSingleChunk(this, &num_sent_bytes);
    }
    else
    {
        // Creating special chunk for keeping WSA buffers information there.
        err_code = sd->CreateWSABuffers(
            worker_dbs_[sd->get_db_index()],
            sd->get_smc(),
            MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA + sd->get_user_data_offset_in_socket_data(),
            MixedCodeConstants::SOCKET_DATA_MAX_SIZE - sd->get_user_data_offset_in_socket_data(),
            sd->get_user_data_written_bytes());

        if (err_code)
            return err_code;

        err_code = sd->SendMultipleChunks(this, &num_sent_bytes);
    }

#ifdef GW_PROFILER_ON
    profiler_.Stop(2);
#endif

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
            GW_PRINT_WORKER << "Failed WSASend on socket index: " << sd->get_socket_info_index() << " "
                << sd->get_chunk_index() << ". Disconnecting socket..." << GW_ENDL;

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
    GW_PRINT_WORKER << "FinishSend: socket index " << sd->get_socket_info_index() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

    // Checking disconnect state.
    if (sd->get_disconnect_after_send_flag())
    {
        sd->set_socket_trigger_disconnect_flag();

        return SCERRGWDISCONNECTAFTERSENDFLAG;
    }

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
    {
        // Only non-representative socket data can have wrong socket id.
        GW_ASSERT(false == sd->get_socket_representer_flag());

        return SCERRGWOPERATIONONWRONGSOCKET;
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
        // We have to return attached chunks.
        if (1 != sd->get_num_chunks())
        {
            uint32_t err_code = sd->ReturnExtraLinkedChunks(this);
            if (err_code)
                return err_code;
        }

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
    GW_ASSERT(sd->get_socket_diag_active_conn_flag() == false);
#endif

    // Returning gateway chunk if any.
    sd->ReturnGatewayChunk();

    // Returning current chunks to pool.
    worker_dbs_[sd->get_db_index()]->ReturnLinkedChunksToPool(sd->get_num_chunks(), sd->get_chunk_index());

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
        DisconnectSocket(*it);
    }

    sockets_indexes_to_disconnect_.clear();
}

// Disconnects arbitrary socket.
void GatewayWorker::DisconnectSocket(session_index_type socket_index)
{
    // Getting existing socket info copy.
    ScSocketInfoStruct global_socket_info_copy = g_gateway.GetGlobalSocketInfoCopy(socket_index);

    // Creating new socket data and setting required parameters.
    SocketDataChunk* temp_sd;

    // NOTE: Fetching chunk from database 0.
    CreateSocketData(socket_index, 0, temp_sd);

    temp_sd->AssignSession(global_socket_info_copy.session_);
    temp_sd->set_socket_trigger_disconnect_flag();
    temp_sd->set_unique_socket_id(global_socket_info_copy.unique_socket_id_);

    DisconnectAndReleaseChunk(temp_sd);
}

// Running disconnect on socket data.
// NOTE: Socket data chunk can not be used after this function is called!
void GatewayWorker::DisconnectAndReleaseChunk(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Disconnect: socket index " << sd->get_socket_info_index() << ":" << sd->get_unique_socket_id() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    uint32_t err_code;

    // Checking if its just a trigger for disconnect.
    if (sd->get_socket_trigger_disconnect_flag())
        goto DISCONNECT_OPERATION;

    // Checking if its not receiving socket data.
    if (!sd->get_socket_representer_flag())
        goto RELEASE_CHUNK_TO_POOL;

    // We have to return attached chunks.
    if (1 != sd->get_num_chunks())
    {
        // NOTE: Skipping checking error code on purpose
        // since we are already in disconnect.
        sd->ReturnExtraLinkedChunks(this);
    }

#ifdef GW_PROFILER_ON
    profiler_.Start("Disconnect()", 3);
#endif

    // Sending dead session if its a WebSocket.
    if (MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS == sd->get_type_of_network_protocol())
    {
        // Verifying that session is correct and sending delete session to database.
        err_code = sd->SendDeleteSession(this);
        GW_ASSERT(0 == err_code);
    }

    // Setting unique socket id.
    sd->GenerateUniqueSocketInfoIds(GenerateSchedulerId(sd->get_db_index()));

DISCONNECT_OPERATION:

    // Calling DisconnectEx.
    err_code = sd->Disconnect(this);
    GW_ASSERT(!err_code);

#ifdef GW_PROFILER_ON
    profiler_.Stop(3);
#endif

    // Checking if operation completed immediately. 
    if (FALSE == err_code)
    {
        int32_t wsa_err_code = WSAGetLastError();

#ifdef GW_LOOPED_TEST_MODE
        wsa_err_code = WSA_IO_PENDING;
#endif

        // Checking if IOCP event was scheduled.
        if (WSA_IO_PENDING != wsa_err_code)
        {
#ifdef GW_WARNINGS_DIAG
            GW_PRINT_WORKER << "Failed DisconnectEx." << GW_ENDL;
            PrintLastError();
#endif

            // Finish disconnect operation.
            // (e.g. returning to pool or starting accept).
            if (FinishDisconnect(sd))
                goto RELEASE_CHUNK_TO_POOL;

            return;
        }

        // NOTE: Setting socket data to null, so other
        // manipulations on it are not possible.
        sd = NULL;

        // The disconnect operation is pending.
        return;
    }
    /*
    else
    {
        // Finish disconnect operation.
        // (e.g. returning to pool or starting accept).
        if (FinishDisconnect(sd))
            goto RELEASE_CHUNK_TO_POOL;

        return;
    }*/

    // Returning the chunk to pool.
RELEASE_CHUNK_TO_POOL:

    // Returning chunks to pool.
    ReturnSocketDataChunksToPool(sd);
}

// Socket disconnect finished.
__forceinline uint32_t GatewayWorker::FinishDisconnect(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishDisconnect: socket index " << sd->get_socket_info_index() << ":" << sd->get_unique_socket_id() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking if its just a trigger for disconnect.
    if (sd->get_socket_trigger_disconnect_flag())
    {
        // Returning chunks to pool.
        ReturnSocketDataChunksToPool(sd);

        return 0;
    }

#ifdef GW_COLLECT_SOCKET_STATISTICS
    GW_ASSERT(sd->get_type_of_network_oper() != UNKNOWN_SOCKET_OPER);
#endif

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    // Stop tracking this socket.
    UntrackSocket(sd->get_db_index(), sd->get_socket_info_index());

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

#ifdef GW_PROXY_MODE

    // Checking if we have reusable proxied server connect.
    if (sd->get_proxied_server_socket_flag())
    {
        // Returning socket for reuse.
        SOCKET sock = sd->GetSocket();
        reusable_connect_sockets_.PushBack(sock);

#ifdef GW_SOCKET_DIAG
        GW_COUT << "Adding socket for reuse: " << sock << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

        // Returning chunks to pool.
        ReturnSocketDataChunksToPool(sd);

        return 0;
    }

#endif

    // Checking if socket should just be reused.
    if (g_gateway.GetDatabase(sd->get_db_index())->IsDeletionStarted())
    {
        // Releasing socket index and reusing socket.
        sd->ReleaseSocketIndex(this);

        // Returning chunks to pool.
        ReturnSocketDataChunksToPool(sd);

        return 0;
    }

    // Resetting the socket data.
    sd->Reset();

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

    // Performing accept.
    return Accept(sd);
}

// Running connect on socket data.
uint32_t GatewayWorker::Connect(SocketDataChunkRef sd, sockaddr_in *server_addr)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Connect: socket index " << sd->get_socket_info_index() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

    while(TRUE)
    {
        // Start connecting socket.

#ifdef GW_PROFILER_ON
        profiler_.Start("Connect()", 4);
#endif

        // Start tracking this socket.
        TrackSocket(sd->get_db_index(), sd->get_socket_info_index());

        // Setting unique socket id.
        sd->GenerateUniqueSocketInfoIds(GenerateSchedulerId(sd->get_db_index()));

        // Calling ConnectEx.
        uint32_t err_code = sd->Connect(this, server_addr);

#ifdef GW_PROFILER_ON
        profiler_.Stop(4);
#endif

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
            GW_PRINT_WORKER << "Failed ConnectEx: socket index " << sd->get_socket_info_index() << " " <<
                sd->get_chunk_index() << " Disconnecting socket..." << GW_ENDL;

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
    GW_PRINT_WORKER << "FinishConnect: socket index " << sd->get_socket_info_index() << ":" << sd->get_chunk_index() << GW_ENDL;
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
    sd->set_socket_representer_flag();

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Changing active connections number.
    ChangeNumActiveConnections(sd->GetPortIndex(), 1);

    sd->set_socket_diag_active_conn_flag();

    sd->set_type_of_network_oper(UNKNOWN_SOCKET_OPER);

#endif

#ifdef GW_PROXY_MODE

    // Checking if we are in proxy mode.
    GW_ASSERT(sd->get_proxied_server_socket_flag() == true);

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
    GW_PRINT_WORKER << "Accept: socket index " << sd->get_socket_info_index() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Start accepting on socket.

#ifdef GW_PROFILER_ON
    profiler_.Start("Accept()", 5);
#endif

    // Tracking corresponding socket.
    TrackSocket(sd->get_db_index(), sd->get_socket_info_index());

    // Updating number of accepting sockets.
    ChangeNumAcceptingSockets(sd->GetPortIndex(), 1);

    // Setting unique socket id.
    sd->GenerateUniqueSocketInfoIds(GenerateSchedulerId(sd->get_db_index()));

    // Calling AcceptEx.
    uint32_t err_code = sd->Accept(this);

#ifdef GW_PROFILER_ON
    profiler_.Stop(5);
#endif

    // Checking if operation completed immediately.
    GW_ASSERT(TRUE != err_code);

    int32_t wsa_err_code = WSAGetLastError();

#ifdef GW_LOOPED_TEST_MODE
    wsa_err_code = WSA_IO_PENDING;
#endif

    // Checking if IOCP event was scheduled.
    if (WSA_IO_PENDING != wsa_err_code)
    {
        // Updating number of accepting sockets.
        ChangeNumAcceptingSockets(sd->GetPortIndex(), -1);

#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Failed AcceptEx: socket index " << sd->get_socket_info_index() << ":" <<
            sd->get_chunk_index() << " Disconnecting socket..." << GW_ENDL;

        PrintLastError();
#endif
        
        // Returning chunks to pool.
        ReturnSocketDataChunksToPool(sd);

        return 0;
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
    GW_PRINT_WORKER << "FinishAccept: socket index " << sd->get_socket_info_index() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking correct unique socket.
    GW_ASSERT(true == sd->CompareUniqueSocketId());

    // Updating connection timestamp.
    sd->UpdateConnectionTimeStamp();

    uint32_t err_code;

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    err_code = sd->SetAcceptSocketOptions();
    if (err_code)
    {
        GW_PRINT_WORKER << "Can't set SO_UPDATE_ACCEPT_CONTEXT on socket." << GW_ENDL;

        PrintLastError();

        return err_code;
    }

    // This socket data is socket representation.
    sd->set_socket_representer_flag();

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Changing active connections number.
    ChangeNumActiveConnections(sd->GetPortIndex(), 1);

    sd->set_socket_diag_active_conn_flag();

    sd->set_type_of_network_oper(UNKNOWN_SOCKET_OPER);

#endif

    // Checking client IP address information.
    sockaddr_in client_addr = *(sockaddr_in *)(sd->get_accept_or_params_data() + sizeof(sockaddr_in) + 16);
    sd->set_client_ip_info(client_addr.sin_addr.S_un.S_addr);

    // Checking if white list is on.
    if (!g_gateway.CheckIpForWhiteList(sd->get_client_ip_info()))
        return SCERRGWIPISNOTONWHITELIST;

    // Decreasing number of accepting sockets.
    int64_t cur_num_accept_sockets = ChangeNumAcceptingSockets(sd->GetPortIndex(), -1);

#ifdef GW_LOOPED_TEST_MODE

    // Checking that we don't exceed number of active connections.
    if (num_created_conns_worker_ < g_gateway.setting_num_connections_to_master_per_worker())
    {
        // Creating new set of prepared connections.
        err_code = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, sd->GetPortIndex(), sd->get_db_index());
        GW_ERR_CHECK(err_code);
    }

#else

    // Checking if we need to extend number of accepting sockets.
    if (cur_num_accept_sockets < ACCEPT_ROOF_STEP_SIZE)
    {
        // Creating new set of prepared connections.
        err_code = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, sd->GetPortIndex(), sd->get_db_index());
        GW_ERR_CHECK(err_code);
    }

#endif

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
        GW_PRINT_WORKER << "Processing clone: socket index " << sd->get_socket_info_index() << ":" << sd->get_unique_socket_id() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
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

// Main gateway worker routine.
uint32_t GatewayWorker::WorkerRoutine()
{
    BOOL compl_status = false;
    OVERLAPPED_ENTRY* fetched_ovls = new OVERLAPPED_ENTRY[MAX_FETCHED_OVLS];
    ULONG num_fetched_ovls = 0;
    uint32_t err_code = 0;
    uint32_t oper_num_bytes = 0, flags = 0, oldTimeMs = timeGetTime();
    uint32_t next_sleep_interval_ms = INFINITE;

#ifdef GW_PROFILER_ON
    uint32_t newTimeMs;
#endif

    sd_receive_clone_ = NULL;

    // Starting worker infinite loop.
    while (TRUE)
    {
#ifdef GW_PROFILER_ON
        profiler_.Start("GetQueuedCompletionStatusEx", 6);
#endif

        // Getting IOCP status.
#ifdef GW_LOOPED_TEST_MODE
        compl_status = ProcessEmulatedNetworkOperations(fetched_ovls, &num_fetched_ovls, MAX_FETCHED_OVLS);
#else
        compl_status = GetQueuedCompletionStatusEx(worker_iocp_, fetched_ovls, MAX_FETCHED_OVLS, &num_fetched_ovls, next_sleep_interval_ms, TRUE);
#endif

#ifdef GW_PROFILER_ON
        profiler_.Stop(6);
        profiler_.Start("ProcessingCycle", 7);
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
                GW_PRINT_WORKER << "GetQueuedCompletionStatusEx: socket index " << sd->get_socket_info_index() << ":" << sd->get_unique_socket_id() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

                // Checking for socket data correctness.
                GW_ASSERT((sd->get_db_index() >= 0) && (sd->get_db_index() < MAX_ACTIVE_DATABASES));
                GW_ASSERT(sd->get_socket_info_index() < g_gateway.setting_max_connections());
                GW_ASSERT(INVALID_CHUNK_INDEX != sd->get_chunk_index());

                // Checking if its not receiving socket data.
                if ((!sd->get_socket_representer_flag()) && (!sd->get_socket_trigger_disconnect_flag()))
                    GW_ASSERT(sd->get_type_of_network_oper() > DISCONNECT_SOCKET_OPER);

#ifdef GW_LOOPED_TEST_MODE
                oper_num_bytes = fetched_ovls[i].dwNumberOfBytesTransferred;
#else
                // Checking for IOCP operation result.
                if (TRUE != WSAGetOverlappedResult(sd->GetSocket(), sd->get_ovl(), (LPDWORD)&oper_num_bytes, FALSE, (LPDWORD)&flags))
                {
                    err_code = WSAGetLastError();
                    if (WSA_IO_INCOMPLETE == err_code)
                        continue;

#ifdef GW_WARNINGS_DIAG
                    GW_PRINT_WORKER << "IOCP operation failed: " << GetOperTypeString(sd->get_type_of_network_oper()) <<
                        " socket index " << sd->get_socket_info_index() << " " << sd->get_chunk_index() << ". Disconnecting socket..." << GW_ENDL;

                    PrintLastError();
#endif

                    // Disconnecting this socket data.
                    DisconnectAndReleaseChunk(sd);

                    continue;
                }
#endif

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

        // NOTE: Checking inactive sockets cleanup (only first worker).
        if ((g_gateway.get_num_sockets_to_cleanup_unsafe()) && (worker_id_ == 0))
            g_gateway.CleanupInactiveSocketsOnWorkerZero();

#ifdef GW_TESTING_MODE
        // Checking if its time to switch to measured test.
        BeginMeasuredTestIfReady();
#endif

#ifdef GW_PROFILER_ON

        profiler_.Stop(7);

        // Printing profiling results.
        newTimeMs = timeGetTime();
        if ((newTimeMs - oldTimeMs) >= 1000)
        {
            profiler_.DrawResults();
            oldTimeMs = timeGetTime();
        }
#endif
    }

    return SCERRGWWORKERROUTINEFAILED;
}

// Creating accepting sockets on all ports and for all databases.
uint32_t GatewayWorker::CheckAcceptingSocketsOnAllActivePortsAndDatabases()
{
    for (int32_t d = 0; d < g_gateway.get_num_dbs_slots(); d++)
    {
        // Checking if database is up.
        if (g_gateway.GetDatabase(d)->IsDeletionStarted() ||
            g_gateway.GetDatabase(d)->IsEmpty())
        {
            continue;
        }

        for (int32_t p = 0; p < g_gateway.get_num_server_ports_slots(); p++)
        {
            ServerPort* server_port = g_gateway.get_server_port(p);

            // Checking that port is not empty.
            if (!server_port->IsEmpty())
            {
                // Checking if we need to extend number of accepting sockets.
                if (server_port->get_num_accepting_sockets() < ACCEPT_ROOF_STEP_SIZE)
                {
                    // Creating new set of prepared connections.
                    uint32_t err_code = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, p, d);
                    GW_ERR_CHECK(err_code);
                }
            }
        }
    }

    return 0;
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
            // Scanning channels first.
            err_code = db->ScanChannels(this, next_sleep_interval_ms);
            if (err_code)
                return err_code;

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

#ifndef GW_LOOPED_TEST_MODE

                    // Creating accepting sockets on all ports and for all databases.
                    err_code = CheckAcceptingSocketsOnAllActivePortsAndDatabases();
                    GW_ERR_CHECK(err_code);

#endif
                }
                else
                {
                    // Gateway needs to loop for a while because of chunks being released.
                    next_sleep_interval_ms = 100;

                    // Releasing all private chunks to shared pool.
                    db->ReturnAllPrivateChunksToSharedPool();
                }
            }
        }
    }

    return 0;
}

// Creates the socket data structure.
uint32_t GatewayWorker::CreateSocketData(
    session_index_type socket_info_index,
    db_index_type db_index,
    SocketDataChunkRef out_sd)
{
    // Getting active database.
    WorkerDbInterface *db = worker_dbs_[db_index];
    if (NULL == db)
        return SCERRGWWRONGDATABASEINDEX;

    // Pop chunk index from private chunk pool.
    core::chunk_index chunk_index;
    shared_memory_chunk *smc;
    uint32_t err_code = db->GetOneChunkFromPrivatePool(&chunk_index, &smc);
    if (err_code)
    {
        // New chunk can not be obtained.

        return err_code;
    }

    // Allocating socket data inside chunk.
    out_sd = (SocketDataChunk*)((uint8_t*)smc + MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA);

    // Initializing socket data.
    out_sd->Init(socket_info_index, db_index, chunk_index);

#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Creating socket data: socket index " << out_sd->get_socket_info_index() << ":" << out_sd->get_unique_socket_id() << ":" << out_sd->get_chunk_index() << ":" << (uint64_t)out_sd << GW_ENDL;
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

    db_index_type target_db_index = sd->get_target_db_index();
    GW_ASSERT(INVALID_DB_INDEX != target_db_index);

    // Checking if we need to clone.
    if (target_db_index != 0)
    {
        // Getting new chunk and copy contents from old one.
        SocketDataChunk* new_sd = NULL;
        uint32_t err_code = CloneChunkForAnotherDatabase(sd, target_db_index, &new_sd);
        if (err_code)
            return err_code;

        // Setting new chunk reference.
        sd = new_sd;
    }

    // Getting database to which this chunk belongs.
    WorkerDbInterface *db = GetWorkerDb(target_db_index);
    GW_ASSERT(NULL != db);

    // Pushing chunk to that database.
    return db->PushSocketDataToDb(this, sd, handler_id);
}

// Gets a new chunk for new database and copies the old one into it.
uint32_t GatewayWorker::CloneChunkForAnotherDatabase(
    SocketDataChunkRef old_sd,
    int32_t new_db_index,
    SocketDataChunk** new_sd)
{
    // Cloning current socket data for another database.
    uint32_t err_code = old_sd->CloneToAnotherDatabase(this, new_db_index, new_sd);
    if (err_code)
        return err_code;

    // Checking if its not receiving socket data.
    // NOTE: Making sure that socket is always receiving.
    GW_ASSERT(!old_sd->get_socket_representer_flag());

    // Returning old chunk to its pool.
    old_sd->reset_socket_diag_active_conn_flag();
    ReturnSocketDataChunksToPool(old_sd);

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
    GW_ASSERT(body_len < 1800);
    char temp_resp[2048];

    // Copying predefined header.
    memcpy(temp_resp, kHttpStatsHeader, kHttpStatsHeaderLength);

    // Making length a white space.
    *(uint64_t*)(temp_resp + kHttpStatsHeaderInsertPoint) = 0x2020202020202020;

    // Converting content length to string.
    WriteUIntToString(temp_resp + kHttpStatsHeaderInsertPoint, body_len);

    // Copying body to response.
    memcpy(temp_resp + kHttpStatsHeaderLength, body, body_len);

    // Sending predefined response.
    return SendPredefinedMessage(sd, temp_resp, kHttpStatsHeaderLength + body_len);
}

// Sends given predefined response.
uint32_t GatewayWorker::SendPredefinedMessage(
    SocketDataChunkRef sd,
    const char* message,
    const int32_t message_len)
{
    // We don't need original chunk contents.
    sd->ResetAccumBuffer();

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Checking if data fits inside chunk.
    if (message_len < (int32_t)accum_buf->get_chunk_num_available_bytes())
    {
        // Checking if message should be copied.
        if (message)
            memcpy(accum_buf->get_chunk_orig_buf_ptr(), message, message_len);
    }
    else // Multiple chunks response.
    {
        GW_ASSERT(message != NULL);

        WorkerDbInterface* worker_db = GetWorkerDb(sd->get_db_index());
        starcounter::core::chunk_index src_chunk_index = sd->get_chunk_index();

        int32_t first_chunk_offset = sd->GetAccumOrigBufferChunkOffset();

        int32_t last_written_bytes;
        uint32_t err_code;

        int32_t total_processed_bytes = 0;
        bool just_sending_flag = false;

        // Copying user data to multiple chunks.
        err_code = worker_db->WriteBigDataToChunks(
            (uint8_t*)message + total_processed_bytes,
            message_len - total_processed_bytes,
            src_chunk_index,
            &last_written_bytes,
            first_chunk_offset,
            just_sending_flag
            );

        if (err_code)
            return err_code;

        // Increasing total sent bytes.
        total_processed_bytes += last_written_bytes;
    }

    // Prepare buffer to send outside.
    accum_buf->PrepareForSend(accum_buf->get_chunk_orig_buf_ptr(), message_len);
    sd->set_user_data_written_bytes(message_len);
    sd->set_user_data_offset_in_socket_data(sd->GetAccumOrigBufferSocketDataOffset());

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
    ULONG* num_fetched_ovls,
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

                // Executing selected echo response processor.
                err_code = g_gateway.get_looped_echo_response_processor()(
                    (char*)sd->get_data_blob(),
                    accum_buffer->get_chunk_num_available_bytes(),
                    &echo_id);

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
