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

// Mandatory initialization function.
int32_t GatewayWorker::Init(int32_t newWorkerId)
{
    worker_id_ = newWorkerId;

    // Creating IO completion port.
    worker_iocp_ = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 1);
    if (worker_iocp_ == NULL)
    {
        GW_PRINT_WORKER << "Failed to create worker IOCP." << std::endl;
        return PrintLastError();
    }

    // Getting global IOCP.
    //worker_iocp_ = g_gateway.get_iocp();

    worker_stats_bytes_received_ = 0;
    worker_stats_bytes_sent_ = 0;
    worker_stats_sent_num_ = 0;
    worker_stats_recv_num_ = 0;
    worker_stats_last_bound_num_ = 1500;

    for (int32_t i = 0; i < MAX_ACTIVE_DATABASES; i++)
        active_dbs_[i] = NULL;

    // Creating random generator with current time seed.
    rand_gen_ = new random_generator(timeGetTime());

    // Initializing profilers.
    profiler_.Init(64);

    return 0;
}

// Allocates a new socket based on existing.
uint32_t GatewayWorker::CreateProxySocket(SocketDataChunk* proxy_sd, SocketDataChunk* client_sd)
{
    // Creating new socket.
    SOCKET new_socket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (new_socket == INVALID_SOCKET)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "WSASocket() failed." << std::endl;
#endif
        return PrintLastError();
    }

    // Adding to IOCP.
    HANDLE tempHandle = CreateIoCompletionPort((HANDLE) new_socket, worker_iocp_, 0, 1);
    if (tempHandle != worker_iocp_)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Wrong IOCP returned when attaching socket to IOCP." << std::endl;
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
        binding_addr.sin_addr.s_addr = inet_addr(g_gateway.setting_local_interfaces().at(0).c_str());
        binding_addr.sin_port = htons(worker_stats_last_bound_num_);

        // Going to next port.
        worker_stats_last_bound_num_++;

        // Binding socket to certain interface and port.
        if (bind(new_socket, (SOCKADDR *) &binding_addr, sizeof(binding_addr)))
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed to bind " << g_gateway.setting_local_interfaces().at(0) << " : " << (worker_stats_last_bound_num_ - 1) << std::endl;
#endif
            continue;
        }

        break;
    }

    // Skipping completion port if operation is already successful.
    SetFileCompletionNotificationModes((HANDLE)new_socket, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);

    // Putting socket into non-blocking mode.
    ULONG ul = 1;
    uint32_t temp;
    if (WSAIoctl(new_socket, FIONBIO, &ul, sizeof(ul), NULL, 0, (LPDWORD)&temp, NULL, NULL))
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Can't put socket into non-blocking mode." << std::endl;
#endif
        closesocket(new_socket);

        return PrintLastError();
    }

    // Marking socket as alive.
    g_gateway.MarkSocketAlive(new_socket);

    // Changing number of created sockets.
    int64_t created_sockets = g_gateway.get_server_port(proxy_sd->get_port_index())->ChangeNumAllocatedSockets(proxy_sd->get_db_index(), 1);

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "New sockets amount: " << created_sockets << std::endl;
#endif

    // Setting receiving socket.
    proxy_sd->set_proxy_socket(proxy_sd->get_socket());

    // Setting new socket.
    proxy_sd->set_socket(new_socket);

    // Setting client proxy socket.
    client_sd->set_proxy_socket(new_socket);

    return 0;
}

// Allocates a bunch of new connections.
uint32_t GatewayWorker::CreateNewConnections(int32_t how_many, int32_t port_index, int32_t db_index)
{
    uint32_t err_code;
    int32_t curIntNum = 0;

    for (int32_t i = 0; i < how_many; i++)
    {
        // Creating new socket.
        SOCKET new_socket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
        if (new_socket == INVALID_SOCKET)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "WSASocket() failed." << std::endl;
#endif
            return PrintLastError();
        }

        // Adding to IOCP.
        HANDLE tempHandle = CreateIoCompletionPort((HANDLE) new_socket, worker_iocp_, 0, 1);
        if (tempHandle != worker_iocp_)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Wrong IOCP returned when attaching socket to IOCP." << std::endl;
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
                sockaddr_in bindAddr;
                memset(&bindAddr, 0, sizeof(sockaddr_in));
                bindAddr.sin_family = AF_INET;
                bindAddr.sin_addr.s_addr = inet_addr(g_gateway.setting_local_interfaces().at(curIntNum).c_str());
                bindAddr.sin_port = htons(worker_stats_last_bound_num_);

                // Going to next port.
                worker_stats_last_bound_num_++;

                // Binding socket to certain interface and port.
                if (bind(new_socket, (SOCKADDR *) &bindAddr, sizeof(bindAddr)))
                {
#ifdef GW_ERRORS_DIAG
                    GW_PRINT_WORKER << "Failed to bind " << g_gateway.setting_local_interfaces().at(curIntNum) << " : " << (worker_stats_last_bound_num_ - 1) << std::endl;
#endif
                    continue;
                }

                break;
            }

            // Taking next interface.
            curIntNum++;
            if (curIntNum >= g_gateway.setting_local_interfaces().size())
                curIntNum = 0;
        }

#endif

        // Setting needed socket options.
        /*
        int32_t onFlag = 1;
        if (setsockopt(sockets[s], IPPROTO_TCP, TCP_NODELAY, (char *)&onFlag, 4))
        {
            GW_PRINT_WORKER << "Can't set TCP_NODELAY on socket." << std::endl;
            PrintLastError();
            goto CLEANUP;
        }
        */

        // Skipping completion port if operation is already successful.
        SetFileCompletionNotificationModes((HANDLE)new_socket, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);

        // Putting socket into non-blocking mode.
        ULONG ul = 1;
        uint32_t temp;
        if (WSAIoctl(new_socket, FIONBIO, &ul, sizeof(ul), NULL, 0, (LPDWORD)&temp, NULL, NULL))
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Can't put socket into non-blocking mode." << std::endl;
#endif
            closesocket(new_socket);

            return PrintLastError();
        }

        // Creating new socket data structure inside chunk.
        SocketDataChunk *new_sd;
        err_code = CreateSocketData(new_socket, port_index, db_index, &new_sd);
        if (err_code)
        {
            closesocket(new_socket);

            return err_code;
        }

        // Marking socket as alive.
        g_gateway.MarkSocketAlive(new_socket);

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
    }

    // Changing number of created sockets.
    int64_t created_sockets = g_gateway.get_server_port(port_index)->ChangeNumAllocatedSockets(db_index, how_many);

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "New sockets amount: " << created_sockets << std::endl;
#endif

    return 0;
}

// Running receive on socket data.
uint32_t GatewayWorker::Receive(SocketDataChunk *sd)
{
// This label is used to avoid recursiveness between Receive and FinishReceive.
START_RECEIVING_AGAIN:

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Receive: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

    // Start receiving on socket.
    //profiler_.Start("Receive()", 5);
    uint32_t numBytes, err_code;

    // Checking if we have one or multiple chunks to receive.
    //if (1 == sd->get_num_chunks())
    {
        err_code = sd->ReceiveSingleChunk(&numBytes);
    }
    /*else
    {
        errCode = sd->ReceiveMultipleChunks(active_dbs_[sd->get_db_index()]->get_shared_int(), &numBytes);
    }*/
    //profiler_.Stop(5);

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, 1);
#endif

    // Checking if operation completed immediately.
    if (0 != err_code)
    {
        int32_t wsaErrCode = WSAGetLastError();
        if (WSA_IO_PENDING != wsaErrCode)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed WSARecv: " << sd->get_socket() << " " <<
                sd->get_chunk_index() << " Disconnecting socket..." << std::endl;
#endif
            PrintLastError();

#ifdef GW_COLLECT_SOCKET_STATISTICS
            ChangeNumPendingNetworkOperations(sd, -1);
#endif

            return SCERRGWFAILEDWSARECV;
        }
    }
    else
    {
        // Checking if socket is closed by the other peer.
        if (0 == numBytes)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Zero-bytes receive on socket: " << sd->get_socket() << ". Remote side closed the connection." << std::endl;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
            ChangeNumPendingNetworkOperations(sd, -1);
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

    return 0;
}

// Socket receive finished.
__forceinline uint32_t GatewayWorker::FinishReceive(
    SocketDataChunk *sd,
    int32_t num_bytes_received,
    bool& called_from_receive)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishReceive: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, -1);
#endif

    // If we received 0 bytes, the remote side has close the connection.
    if (0 == num_bytes_received)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Zero-bytes receive on socket: " << sd->get_socket() << ". Remote side closed the connection." << std::endl;
#endif

        return SCERRGWSOCKETCLOSEDBYPEER;
    }

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Adding to accumulated bytes.
    accum_buf->AddAccumulatedBytes(num_bytes_received);

    // Incrementing statistics.
    worker_stats_bytes_received_ += num_bytes_received;

    // Increasing number of receives.
    worker_stats_recv_num_++;

#ifdef GW_PROXY_MODE

    // Checking if we are in proxy mode.
    if (sd->get_with_proxied_server_flag())
    {
        // Posting cloning receive since all data is accumulated.
        uint32_t err_code = sd->CloneToReceive(this);
        GW_ERR_CHECK(err_code);

        // Setting proxy mode.
        sd_receive_clone_->set_with_proxied_server_flag(true);

        // Finished receiving from proxied server,
        // now sending to the original user.
        sd->ExchangeToProxySocket();

        // Since we are sending this socket data to client.
        sd->set_with_proxied_server_flag(false);

        // Setting number of bytes to send.
        sd->get_accum_buf()->PrepareForSend();

        // Just sending to client.
        sd->set_receiving_flag(false);

        // Sending data to user.
        return Send(sd);
    }

#endif

    // Assigning last received bytes.
    if (!sd->get_accumulating_flag())
    {
        accum_buf->SetLastReceivedBytes(num_bytes_received);
    }
    else
    {
        // Adding last received bytes.
        accum_buf->AddLastReceivedBytes(num_bytes_received);

        // Indicates if all data was accumulated.
        bool is_accumulated;

        // Trying to continue accumulation.
        uint32_t err_code = sd->ContinueAccumulation(this, &is_accumulated);
        GW_ERR_CHECK(err_code);

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
    }

    // Getting attached session if any.
    if (INVALID_SESSION_INDEX != sd->get_session_index())
    {
        // Checking for session correctness.
        ScSessionStruct global_session_copy = g_gateway.GetGlobalSessionDataCopy(sd->get_session_index());

        // Check that data received belongs to the correct session (not coming from abandoned connection).
        if (!global_session_copy.CompareSalts(sd->get_session_salt()))
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Data from abandoned/different socket received." << std::endl;
#endif

            // Just resetting the session.
            sd->ResetSdSession();
        }
    }


#ifdef GW_TESTING_MODE

    // Checking if we are on gateway client.
    if (!g_gateway.setting_is_master())
    {
        // Sending response message to master node.
        return ProcessMasterMessage(sd, false);
    }

#endif

    // Running the handler.
    return RunReceiveHandlers(sd);
}

// Running send on socket data.
uint32_t GatewayWorker::Send(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Send: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

    // Start sending on socket.
    //profiler_.Start("Send()", 6);
    uint32_t numBytes, err_code;

    // Checking if we have one or multiple chunks to send.
    if (1 == sd->get_num_chunks())
    {
        err_code = sd->SendSingleChunk(&numBytes);
    }
    else
    {
        err_code = sd->SendMultipleChunks(active_dbs_[sd->get_db_index()]->get_shared_int(), &numBytes);
    }
    //profiler_.Stop(6);

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, 1);
#endif

    // Checking if operation completed immediately.
    if (0 != err_code)
    {
        int32_t wsaErrCode = WSAGetLastError();
        if (WSA_IO_PENDING != wsaErrCode)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed WSASend. Disconnecting socket..." << std::endl;
#endif
            PrintLastError();

#ifdef GW_COLLECT_SOCKET_STATISTICS
            ChangeNumPendingNetworkOperations(sd, -1);
#endif

            return SCERRGWFAILEDWSASEND;
        }
    }
    else
    {
        // Finish send operation.
        err_code = FinishSend(sd, numBytes);
        GW_ERR_CHECK(err_code);
    }

    return 0;
}

// Socket send finished.
__forceinline uint32_t GatewayWorker::FinishSend(SocketDataChunk *sd, int32_t num_bytes_sent)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishSend: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, -1);
#endif

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Checking that we processed correct number of bytes.
    if (num_bytes_sent != accum_buf->get_buf_len_bytes())
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Incorrect number of bytes sent: " << num_bytes_sent << " of " << accum_buf->get_buf_len_bytes() << "(correct)" << std::endl;
#endif
        return SCERRGWINCORRECTBYTESSEND;
    }

    // Incrementing statistics.
    worker_stats_bytes_sent_ += num_bytes_sent;

    // Increasing number of sends.
    worker_stats_sent_num_++;

    // Checking disconnect state.
    if (sd->get_disconnect_after_send_flag())
        return SCERRGWDISCONNECTAFTERSENDFLAG;

    // We have to return attached chunks.
    if (1 != sd->get_num_chunks())
    {
        uint32_t err_code = sd->ReturnExtraLinkedChunks(this);
        GW_ERR_CHECK(err_code);
    }

    // Resets data buffer offset.
    sd->ResetUserDataOffset();

    // Resetting buffer information.
    accum_buf->ResetBufferForNewOperation();

    // Checking if socket data is for receiving.
    if (sd->get_receiving_flag())
    {
        // Performing receive.
        return Receive(sd);
    }

#ifdef GW_TESTING_MODE

    // Checking if its gateway client.
    if (!g_gateway.setting_is_master())
    {
        // Posting cloning receive since all data is accumulated.
        SetReceiveClone(sd);

        return 0;
    }

#endif

    WorkerDbInterface *db = active_dbs_[sd->get_db_index()];
    assert(db != NULL);

    // Returning chunks to pool.
    return db->ReturnSocketDataChunksToPool(this, sd);
}

// Running disconnect on socket data.
// NOTE: Socket data chunk can not be used after this function is called!
uint32_t GatewayWorker::DisconnectAndReleaseChunk(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Disconnect: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

    uint32_t err_code;

    // Checking if its not receiving socket data.
    if (!sd->get_receiving_flag())
    {
        WorkerDbInterface *db = active_dbs_[sd->get_db_index()];
        assert(db != NULL);

        // Returning chunks to pool.
        return db->ReturnSocketDataChunksToPool(this, sd);
    }

    // We have to return attached chunks.
    if (1 != sd->get_num_chunks())
    {
        // NOTE: Skipping checking error code on purpose
        // since we are already in disconnect.
        sd->ReturnExtraLinkedChunks(this);
    }

    // Start disconnecting socket.
    //profiler_.Start("Disconnect()", 4);
    err_code = sd->Disconnect();
    //profiler_.Stop(4);

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, 1);
#endif

    // Checking if operation completed immediately. 
    if (TRUE != err_code)
    {
        int32_t wsaErrCode = WSAGetLastError();

        // Checking if socket was not connected.
        if (WSAENOTCONN == wsaErrCode)
        {
            // Finish disconnect operation.
            FinishDisconnect(sd);
            return SCERRGWSOCKETNOTCONNECTED;
        }

        // Checking for other errors.
        if (WSA_IO_PENDING != wsaErrCode)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed DisconnectEx." << std::endl;
#endif
            PrintLastError();
            FinishDisconnect(sd);
            return SCERRGWFAILEDDISCONNECTEX;
        }
    }
    else
    {
        // Finish disconnect operation.
        err_code = FinishDisconnect(sd);
        if (err_code)
            return err_code;
    }

    return 0;
}

// Socket disconnect finished.
__forceinline uint32_t GatewayWorker::FinishDisconnect(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishDisconnect: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, -1);
#endif

    // Stop tracking this socket.
    g_gateway.GetDatabase(sd->get_db_index())->UntrackSocket(sd->get_socket());
    g_gateway.UntrackSocket(sd->get_socket());

    // Changing active connections number.
    g_gateway.get_server_port(sd->get_port_index())->ChangeNumActiveConns(sd->get_db_index(), -1);

#ifdef GW_PROXY_MODE

    // Checking if socket was used in proxying.
    if (sd->get_with_proxied_server_flag())
    {
        WorkerDbInterface *db = active_dbs_[sd->get_db_index()];
        assert(db != NULL);

        // Returning chunks to pool.
        return db->ReturnSocketDataChunksToPool(this, sd);
    }

#endif

    // Resetting the socket data.
    sd->Reset();

#ifdef GW_TESTING_MODE

    if (!g_gateway.setting_is_master())
    {
        // Performing connect.
        return Connect(sd, g_gateway.get_server_addr());
    }
    else
    {
        // Performing accept.
        return Accept(sd);
    }

#else

    // Performing accept.
    return Accept(sd);

#endif

    return 0;
}

// Running connect on socket data.
uint32_t GatewayWorker::Connect(SocketDataChunk *sd, sockaddr_in *serverAddr)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Connect: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

    while(TRUE)
    {
        // Start connecting socket.
        //profiler_.Start("Connect()", 3);
        uint32_t err_code = sd->Connect(serverAddr);
        //profiler_.Stop(3);

        // Checking if operation completed immediately.
        assert(TRUE != err_code);

#ifdef GW_COLLECT_SOCKET_STATISTICS
        ChangeNumPendingNetworkOperations(sd, 1);
#endif

        int32_t wsa_err_code = WSAGetLastError();
        if (WSA_IO_PENDING != wsa_err_code)
        {
#ifdef GW_COLLECT_SOCKET_STATISTICS
            ChangeNumPendingNetworkOperations(sd, -1);
#endif

            if (WAIT_TIMEOUT == wsa_err_code)
            {
#ifdef GW_ERRORS_DIAG
                GW_PRINT_WORKER << "Timeout in ConnectEx. Retrying..." << std::endl;
#endif
                continue;
            }

#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed ConnectEx." << std::endl;
#endif
            PrintLastError();
            return SCERRGWCONNECTEXFAILED;
        }

        break;
    }

    // Setting SO_UPDATE_CONNECT_CONTEXT.
    //setsockopt(sd->s, SOL_SOCKET, SO_UPDATE_CONNECT_CONTEXT, NULL, 0);

    return 0;
}

// Socket connect finished.
__forceinline uint32_t GatewayWorker::FinishConnect(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishConnect: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, -1);
#endif

    // Changing active connections number.
    g_gateway.get_server_port(sd->get_port_index())->ChangeNumActiveConns(sd->get_db_index(), 1);

#ifdef GW_PROXY_MODE

    // Checking if we are in proxy mode.
    if (sd->get_with_proxied_server_flag())
    {
        // Sending to server.
        return Send(sd);
    }
#endif

#ifdef GW_TESTING_MODE
    // Sending determined message to master node.
    return ProcessMasterMessage(sd, true);
#endif

    return SCERRGWCONNECTEXFAILED;
}

// Running accept on socket data.
uint32_t GatewayWorker::Accept(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Accept: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

    // This socket data is for receiving.
    sd->set_receiving_flag(true);

    // Start accepting on socket.
    //profiler_.Start("Accept()", 7);
    uint32_t errCode = sd->Accept(this);
    //profiler_.Stop(7);

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, 1);
#endif
 
    // Checking if operation completed immediately.
    assert(TRUE != errCode);

    int32_t wsaErrCode = WSAGetLastError();
    if (WSA_IO_PENDING != wsaErrCode)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Error accepting socket." << std::endl;
#endif
        PrintLastError();

#ifdef GW_COLLECT_SOCKET_STATISTICS
        ChangeNumPendingNetworkOperations(sd, -1);
#endif

        return SCERRGWACCEPTEXFAILED;
    }

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    //setsockopt(sd->GetSocket(), SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT, NULL, 0 );

    return 0;
}

// Socket accept finished.
uint32_t GatewayWorker::FinishAccept(SocketDataChunk *sd, int32_t numBytesReceived)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishAccept: socket " << sd->get_socket() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    ChangeNumPendingNetworkOperations(sd, -1);
#endif

    // Checking the endpoint information (e.g. for black listing).
    sockaddr_in remoteAddr = *(sockaddr_in *)(sd->accept_data() + sizeof(sockaddr_in) + 16);

    // Getting server port index and database index.
    int32_t port_index = sd->get_port_index(), db_index = sd->get_db_index();

    // Getting server port reference.
    ServerPort *server_port = g_gateway.get_server_port(port_index);

    // Changing active connections number.
    server_port->ChangeNumActiveConns(db_index, 1);

    // Checking if we need to extend data.
    if ((server_port->get_num_allocated_sockets(db_index) - server_port->get_num_active_conns(db_index)) < ACCEPT_ROOF_STEP_SIZE)
    {
        // Creating new set of prepared connections.
        uint32_t errCode = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, port_index, db_index);
        GW_ERR_CHECK(errCode);
    }

    // Performing receive.
    return Receive(sd);
}

// Checks if cloning was performed and does operations.
__forceinline uint32_t GatewayWorker::ProcessReceiveClones(SocketDataChunk* sd, bool just_delete_clone)
{
    // Checking if there was no clone.
    if (sd_receive_clone_ == NULL)
        return 0;

    // Checking if we need just to delete the cloned socket data.
    if (just_delete_clone)
    {
        // Releasing cloned socket data chunks back to pool.
        GetDatabase(sd->get_db_index())->ReturnSocketDataChunksToPool(this, sd_receive_clone_);
        sd_receive_clone_ = NULL;

        return 0;
    }

    uint32_t err_code = 0;
    while (sd_receive_clone_ != NULL)
    {
        sd = sd_receive_clone_;
        sd_receive_clone_ = NULL;

        // Performing receive operation.
        err_code = Receive(sd);

        // Checking if any error occurred during socket operation.
        if (err_code)
        {
            // Disconnecting socket if needed and releasing chunk.
            DisconnectAndReleaseChunk(sd);

            // Checking if we still have a clone.
            if (sd_receive_clone_ != NULL)
            {
                // Releasing socket data chunks back to pool.
                GetDatabase(sd->get_db_index())->ReturnSocketDataChunksToPool(this, sd_receive_clone_);
                sd_receive_clone_ = NULL;
            }

            break;
        }
    }

    return err_code;
}

// Main gateway worker routine.
uint32_t GatewayWorker::WorkerRoutine()
{
    BOOL complStatus = false;
    OVERLAPPED_ENTRY *removedOvls = new OVERLAPPED_ENTRY[MAX_FETCHED_OVLS];
    ULONG removedOvlsNum = 0;
    uint32_t err_code = 0;
    uint32_t numBytes = 0, flags = 0, oldTimeMs = timeGetTime(), newTimeMs;
    uint32_t waitForIocpMs = INFINITE;
    bool found_something = false;
    sd_receive_clone_ = NULL;

    // Starting worker infinite loop.
    while (TRUE)
    {
        // Getting IOCP status.
        complStatus = GetQueuedCompletionStatusEx(worker_iocp_, removedOvls, MAX_FETCHED_OVLS, &removedOvlsNum, waitForIocpMs, TRUE);
        //profiler_.Start("ProcessingCycle", 1);

        // Check if global lock is set.
        if (g_gateway.global_lock())
            g_gateway.SuspendWorker(this);

        //profiler_.Stop(7);

        // Checking if operation successfully completed.
        if (TRUE == complStatus)
        {
            // Processing each retrieved overlapped.
            for (uint32_t i = 0; i < removedOvlsNum; i++)
            {
                // Obtaining socket data structure.
                SocketDataChunk *sd = (SocketDataChunk *)(removedOvls[i].lpOverlapped);

                // Checking for socket data correctness.
                assert((sd->get_db_index() >= 0) && (sd->get_db_index() <= 63));
                assert(sd->get_socket() <= MAX_SOCKET_HANDLE);

                // Checking that socket is valid.
                if (!sd->ForceSocketDataValidity(this))
                    continue;

                // Checking for IOCP operation result.
                if (TRUE != WSAGetOverlappedResult(sd->get_socket(), sd->get_ovl(), (LPDWORD)&numBytes, FALSE, (LPDWORD)&flags))
                {
                    err_code = WSAGetLastError();
                    if ((WSA_IO_PENDING != err_code) && (WSA_IO_INCOMPLETE != err_code))
                    {
#ifdef GW_ERRORS_DIAG
                        GW_PRINT_WORKER << "IOCP operation failed: " << GetOperTypeString(sd->type_of_network_oper()) <<
                            " " << sd->get_socket() << " " << sd->get_chunk_index() << ". Disconnecting socket..." << std::endl;
#endif
                        PrintLastError();

#ifdef GW_COLLECT_SOCKET_STATISTICS
                        ChangeNumPendingNetworkOperations(sd, -1);
#endif

                        // Disconnecting this socket data.
                        goto DISCONNECT_SOCKET_DATA_AND_RELEASE_CHUNK;
                    }

                    continue;
                }

                // Checking type of operation.
                switch (sd->type_of_network_oper())
                {
                    // ACCEPT finished.
                    case ACCEPT_OPER:
                    {
                        err_code = FinishAccept(sd, numBytes);
                        break;
                    }

                    // CONNECT finished.
                    case CONNECT_OPER:
                    {
                        err_code = FinishConnect(sd);
                        break;
                    }

                    // DISCONNECT finished.
                    case DISCONNECT_OPER:
                    {
                        err_code = FinishDisconnect(sd);
                        break;
                    }

                    // SEND finished.
                    case SEND_OPER:
                    {
                        err_code = FinishSend(sd, numBytes);
                        break;
                    }

                    // RECEIVE finished.
                    case RECEIVE_OPER:
                    {
                        bool called_from_receive = false;
                        err_code = FinishReceive(sd, numBytes, called_from_receive);
                        break;
                    }

                    // Unknown operation.
                    default:
                    {
                        assert(1 == 0);
                    }
                }

DISCONNECT_SOCKET_DATA_AND_RELEASE_CHUNK:

                // Checking if any error occurred during socket operations.
                if (err_code)
                {
                    // Disconnecting this socket data.
                    DisconnectAndReleaseChunk(sd);

                    // Releasing the cloned chunk.
                    ProcessReceiveClones(sd, true);
                }
                else
                {
                    // Processing clones during last iteration.
                    ProcessReceiveClones(sd, false);
                }
            }
        }
        else
        {
            err_code = WSAGetLastError();

            // Checking if it was an APC event.
            if (STATUS_USER_APC != err_code &&
                STATUS_TIMEOUT != err_code)
            {
                PrintLastError();
                return err_code;
            }
        }

        // Scanning all channels.
        found_something = false;
        err_code = ScanChannels(&found_something);
        GW_ERR_CHECK(err_code);

        // Checking if something was found.
        if (found_something)
        {
            // Making at least one more round.
            waitForIocpMs = 0;
        }
        else
        {
            // Going to wait infinitely for network events.
            waitForIocpMs = INFINITE;
        }

        // Checking inactive sessions cleanup (only first worker).
        if ((g_gateway.get_num_sessions_to_cleanup_unsafe()) && (worker_id_ == 0))
            g_gateway.CleanupInactiveSessions(this);

        //profiler_.Stop(1);
        //profiler_.DrawResults();

        // Printing profiling results.
        /*
        newTimeMs = timeGetTime();
        if ((newTimeMs - oldTimeMs) >= 1000)
        {
            profiler_.DrawResults();
            oldTimeMs = timeGetTime();
        }
        */
    }

    return SCERRGWWORKERROUTINEFAILED;
}

// Scans all channels for any incoming chunks.
void __stdcall EmptyApcFunction(ULONG_PTR arg);
uint32_t GatewayWorker::ScanChannels(bool* found_something)
{
    uint32_t errCode;

    for (int32_t i = 0; i < g_gateway.get_num_dbs_slots(); i++)
    {
        // Scan channels.
        WorkerDbInterface *db = GetDatabase(i);
        if (NULL != db)
        {
            // Scanning channels first.
            errCode = db->ScanChannels(this, found_something);
            GW_ERR_CHECK(errCode);

            // Checking that database is ready for deletion (i.e. no pending sockets).
            if (g_gateway.GetDatabase(i)->IsEmpty())
            {
                // Entering global lock.
                EnterGlobalLock();

                // Deleting all associated info with this database from ports.
                g_gateway.DeletePortsForDb(i);

                // Finally completely deleting database object and closing shared memory.
                DeleteInactiveDatabase(i);

#ifdef GW_DATABASES_DIAG
                GW_PRINT_WORKER << "Deleted shared memory for db slot: " << db->db_slot_index() << std::endl;
#endif

                // Leaving global lock.
                LeaveGlobalLock();
            }
        }
    }

    return 0;
}

// Creates the socket data structure.
uint32_t GatewayWorker::CreateSocketData(
    SOCKET sock,
    int32_t port_index,
    int32_t db_index,
    SocketDataChunk** out_sd)
{
    // Getting active database.
    WorkerDbInterface *db = active_dbs_[db_index];
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
    SocketDataChunk *new_sd = (SocketDataChunk *)((uint8_t*)smc + bmx::BMX_HEADER_MAX_SIZE_BYTES);

    // Initializing socket data.
    new_sd->Init(sock, port_index, db_index, chunk_index);

    // Configuring data buffer.
    new_sd->get_accum_buf()->Init(SOCKET_DATA_BLOB_SIZE_BYTES, new_sd->get_data_blob(), true);

    // Returning created accumulative buffer.
    *out_sd = new_sd;

    return 0;
}

// Adds new active database.
uint32_t GatewayWorker::AddNewDatabase(
    int32_t db_index,
    const core::shared_interface& worker_shared_int)
{
    active_dbs_[db_index] = new WorkerDbInterface();
    uint32_t errCode = active_dbs_[db_index]->Init(db_index, worker_shared_int, this);
    GW_ERR_CHECK(errCode);

    return 0;
}

// Push given chunk to database queue.
uint32_t GatewayWorker::PushSocketDataToDb(SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id)
{
    WorkerDbInterface *db = GetDatabase(sd->get_db_index());
    assert(NULL != db);
    
    return db->PushSocketDataToDb(this, sd, handler_id);
}

// Deleting inactive database.
void GatewayWorker::DeleteInactiveDatabase(int32_t db_index)
{
    delete active_dbs_[db_index];
    active_dbs_[db_index] = NULL;
}

// Sends given predefined response.
uint32_t GatewayWorker::SendPredefinedMessage(
    SocketDataChunk *sd,
    const char* message,
    const int32_t message_len)
{
    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Copying given response.
    if (message)
        memcpy(accum_buf->get_orig_buf_ptr(), message, message_len);

    // Prepare buffer to send outside.
    accum_buf->PrepareForSend(accum_buf->get_orig_buf_ptr(), message_len);

    // Sending data.
    return Send(sd);
}

#ifdef GW_TESTING_MODE

// Sends HTTP echo to master.
uint32_t GatewayWorker::SendHttpEcho(SocketDataChunk *sd, int64_t echo_id)
{
#ifdef GW_ECHO_STATISTICS
    GW_PRINT_WORKER << "Sending echo: " << echo_id << std::endl;
#endif

    // Copying HTTP response.
    memcpy(sd->get_data_blob(), kHttpEchoRequest, kHttpEchoRequestLength);

    // Inserting number into HTTP ping request.
    uint64_to_hex_string(echo_id, (char*)sd->get_data_blob() + kHttpEchoRequestInsertPoint, 8, false);

    // Sending Ping request to server.
    return SendPredefinedMessage(sd, NULL, kHttpEchoRequestLength);
}

// Sends raw echo to master.
uint32_t GatewayWorker::SendRawEcho(SocketDataChunk *sd, int64_t echo_id)
{
#ifdef GW_ECHO_STATISTICS
    GW_PRINT_WORKER << "Sending echo: " << echo_id << std::endl;
#endif

    // Inserting raw echo id.
    (*(int64_t*)sd->get_data_blob()) = echo_id;

    // Sending Ping request to server.
    return SendPredefinedMessage(sd, NULL, sizeof(echo_id));
}


// Sends certain message to master node.
uint32_t GatewayWorker::ProcessMasterMessage(SocketDataChunk *sd, bool just_connected)
{
    // Checking operational mode.
    uint32_t err_code;
    switch (g_gateway.setting_mode())
    {
        case GatewayTestingMode::MODE_GATEWAY_PING:
        {
            if (just_connected)
            {
                // Connect operation just finished.
                goto SEND_RAW_ECHO_TO_MASTER;
            }
            else
            {
                // Obtaining original echo number.
                int64_t echo_id = *(int32_t*)sd->get_data_blob();

#ifdef GW_ECHO_STATISTICS
                GW_PRINT_WORKER << "Received echo: " << echo_id << std::endl;
#endif

                // Confirming received echo.
                g_gateway.ConfirmEcho(echo_id);

                // Checking if all echo responses are returned.
                if (g_gateway.CheckConfirmedEchoResponses())
                {
                    GW_PRINT_WORKER << "All ECHO messages are confirmed on the client." << std::endl;

                    // Returning this chunk to database.
                    WorkerDbInterface *db = active_dbs_[sd->get_db_index()];
                    assert(db != NULL);

                    // Returning chunks to pool.
                    return db->ReturnSocketDataChunksToPool(this, sd);
                        
                    /*
                    EnterGlobalLock();
                    g_gateway.ResetEchoTests();
                    LeaveGlobalLock();
                    return 0;
                    */
                }
                else
                {
                    goto SEND_RAW_ECHO_TO_MASTER;
                }
            }

SEND_RAW_ECHO_TO_MASTER:

            // Checking that not all echoes are sent.
            if (!g_gateway.AllEchoesSent())
            {
                // Sending echo request to server.
                err_code = SendRawEcho(sd, g_gateway.GetNextEchoNumber());
                if (err_code)
                    return err_code;
            }

            break;
        }

        case GatewayTestingMode::MODE_GATEWAY_HTTP:
        case GatewayTestingMode::MODE_APPS_HTTP:
        {
            if (just_connected)
            {
                // Connect operation just finished.
                goto SEND_HTTP_ECHO_TO_MASTER;
            }
            else
            {
                // Obtaining original echo number.
                //int64_t echo_id = *(int32_t*)sd->get_data_blob();
                int64_t echo_id = hex_string_to_uint64((char*)sd->get_data_blob() + kHttpEchoResponseInsertPoint, kHttpEchoBodyLength);

#ifdef GW_ECHO_STATISTICS
                GW_PRINT_WORKER << "Received echo: " << echo_id << std::endl;
#endif

                // Confirming received echo.
                g_gateway.ConfirmEcho(echo_id);

                // Checking if all echo responses are returned.
                if (g_gateway.CheckConfirmedEchoResponses())
                {
                    GW_PRINT_WORKER << "All ECHO messages are confirmed on the client." << std::endl;

                    // Returning this chunk to database.
                    WorkerDbInterface *db = active_dbs_[sd->get_db_index()];
                    assert(db != NULL);

                    // Returning chunks to pool.
                    return db->ReturnSocketDataChunksToPool(this, sd);
                        
                    /*
                    EnterGlobalLock();
                    g_gateway.ResetEchoTests();
                    LeaveGlobalLock();
                    return 0;
                    */
                }
                else
                {
                    goto SEND_HTTP_ECHO_TO_MASTER;
                }
            }

SEND_HTTP_ECHO_TO_MASTER:

            // Checking that not all echoes are sent.
            if (!g_gateway.AllEchoesSent())
            {
                // Sending echo request to server.
                err_code = SendHttpEcho(sd, g_gateway.GetNextEchoNumber());
                if (err_code)
                    return err_code;
            }

            break;
        }

        case GatewayTestingMode::MODE_APPS_PING:
        {
            break;
        }
    }

    return 0;
}

#endif

} // namespace network
} // namespace starcounter
