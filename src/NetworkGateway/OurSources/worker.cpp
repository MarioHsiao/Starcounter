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
    cur_scheduler_id_ = 0;

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

    return 0;
}

// Allocates a new socket based on existing.
uint32_t GatewayWorker::CreateProxySocket(SocketDataChunk* proxy_sd)
{
    // Creating new socket.
    SOCKET new_socket;

    // Indicates if we used previously created socket.
    bool reused_socket = false;
    
    // Checking if we can reuse existing socket.
    if (reusable_connect_sockets_.size())
    {
        new_socket = reusable_connect_sockets_.back();
        reusable_connect_sockets_.pop_back();

#ifdef GW_SOCKET_DIAG
        GW_PRINT_WORKER << "Reusing 'connect' socket: " << new_socket << std::endl;
#endif

        reused_socket = true;
    }
    else
    {
        new_socket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
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
            binding_addr.sin_addr.s_addr = inet_addr(g_gateway.setting_local_interfaces().at(g_gateway.get_last_bind_interface_num()).c_str());
            binding_addr.sin_port = htons(g_gateway.get_last_bind_port_num());

            // Generating new port/interface.
            g_gateway.GenerateNewBindPortInterfaceNumbers();

            // Binding socket to certain interface and port.
            if (bind(new_socket, (SOCKADDR *) &binding_addr, sizeof(binding_addr)))
            {
#ifdef GW_ERRORS_DIAG
                GW_PRINT_WORKER << "Failed to bind port!" << std::endl;
#endif
                continue;
            }

            break;
        }

        int32_t onFlag = 1;

        // Does not block close waiting for unsent data to be sent.
        if (setsockopt(new_socket, SOL_SOCKET, SO_DONTLINGER, (char *)&onFlag, 4))
        {
            GW_PRINT_WORKER << "Can't set SO_DONTLINGER on socket." << std::endl;

            closesocket(new_socket);

            return PrintLastError();
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
    }

    // Marking socket as alive.
    g_gateway.MarkSocketAlive(new_socket);

#ifdef GW_COLLECT_SOCKET_STATISTICS

    if (!reused_socket)
    {
        // Changing number of created sockets.
        int64_t created_sockets = g_gateway.get_server_port(proxy_sd->get_port_index())->ChangeNumAllocatedConnectSockets(1);

#ifdef GW_SOCKET_DIAG
        GW_PRINT_WORKER << "New sockets amount: " << created_sockets << std::endl;
#endif
    }

#endif

    // Setting receiving socket.
    proxy_sd->set_proxy_socket(proxy_sd->get_socket());

    // Setting new socket.
    proxy_sd->set_socket(new_socket);

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
                bindAddr.sin_addr.s_addr = inet_addr(g_gateway.setting_local_interfaces().at(g_gateway.get_last_bind_interface_num()).c_str());
                bindAddr.sin_port = htons(g_gateway.get_last_bind_port_num());

                // Generating new port/interface.
                g_gateway.GenerateNewBindPortInterfaceNumbers();

                // Binding socket to certain interface and port.
                if (bind(new_socket, (SOCKADDR *) &bindAddr, sizeof(bindAddr)))
                {
#ifdef GW_ERRORS_DIAG
                    GW_PRINT_WORKER << "Failed to bind port!" << std::endl;
#endif
                    continue;
                }

                break;
            }
        }

#endif

        // Setting needed socket options.
        int32_t onFlag = 1;

        // Disables the Nagle algorithm for send coalescing.
        if (setsockopt(new_socket, IPPROTO_TCP, TCP_NODELAY, (char *)&onFlag, 4))
        {
            GW_PRINT_WORKER << "Can't set TCP_NODELAY on socket." << std::endl;

            closesocket(new_socket);

            return PrintLastError();
        }

        // Does not block close waiting for unsent data to be sent.
        if (setsockopt(new_socket, SOL_SOCKET, SO_DONTLINGER, (char *)&onFlag, 4))
        {
            GW_PRINT_WORKER << "Can't set SO_DONTLINGER on socket." << std::endl;

            closesocket(new_socket);

            return PrintLastError();
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

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Changing number of created sockets.
    int64_t created_sockets = g_gateway.get_server_port(port_index)->ChangeNumAllocatedAcceptSockets(how_many);

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "New sockets amount: " << created_sockets << std::endl;
#endif

#endif

    return 0;
}

// Running receive on socket data.
uint32_t GatewayWorker::Receive(SocketDataChunk*& sd)
{
// This label is used to avoid recursiveness between Receive and FinishReceive.
START_RECEIVING_AGAIN:

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Receive: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
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

            return SCERRGWFAILEDWSARECV;
        }
    }
    else
    {
        // Checking if socket is closed by the other peer.
        if (0 == numBytes)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Zero-bytes receive on socket: " << sd->get_socket() << " " <<
                sd->get_chunk_index() << ". Remote side closed the connection." << std::endl;
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
    SocketDataChunk*& sd,
    int32_t num_bytes_received,
    bool& called_from_receive)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishReceive: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
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

    // Checking if this is a proxied server socket.
    if (sd->get_proxied_server_socket_flag())
    {
        // Posting cloning receive since all data is accumulated.
        uint32_t err_code = sd->CloneToReceive(this);
        GW_ERR_CHECK(err_code);

        // Setting proxy mode.
        sd_receive_clone_->set_proxied_server_socket_flag(true);

        // Since we are sending this socket data to client.
        sd->set_proxied_server_socket_flag(false);

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

    // Running the handler.
    return RunReceiveHandlers(sd);
}

// Running send on socket data.
uint32_t GatewayWorker::Send(SocketDataChunk*& sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Send: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
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
        err_code = sd->SendMultipleChunks(worker_dbs_[sd->get_db_index()]->get_shared_int(), &numBytes);
    }
    //profiler_.Stop(6);

    // Checking if operation completed immediately.
    if (0 != err_code)
    {
        int32_t wsaErrCode = WSAGetLastError();
        if (WSA_IO_PENDING != wsaErrCode)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed WSASend on socket: " << sd->get_socket() << " "
                << sd->get_chunk_index() << ". Disconnecting socket..." << std::endl;
#endif
            PrintLastError();

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
__forceinline uint32_t GatewayWorker::FinishSend(SocketDataChunk*& sd, int32_t num_bytes_sent)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishSend: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
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
    if (sd->get_socket_representer_flag())
    {
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

    WorkerDbInterface *db = worker_dbs_[sd->get_db_index()];
    assert(db != NULL);

    // Returning chunks to pool.
    return db->ReturnSocketDataChunksToPool(this, sd);
}

// Running disconnect on socket data.
// NOTE: Socket data chunk can not be used after this function is called!
uint32_t GatewayWorker::DisconnectAndReleaseChunk(SocketDataChunk*& sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Disconnect: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
#endif

    uint32_t err_code;

    // Checking if its not receiving socket data.
    if (!sd->get_socket_representer_flag())
    {
        WorkerDbInterface *db = worker_dbs_[sd->get_db_index()];
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
        return FinishDisconnect(sd);
    }

    return 0;
}

// Socket disconnect finished.
__forceinline uint32_t GatewayWorker::FinishDisconnect(SocketDataChunk*& sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishDisconnect: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    assert(sd->get_type_of_network_oper() != UNKNOWN_SOCKET_OPER);
#endif

    // NOTE: Since we are here means that this socket data represents this socket.
    assert(sd->get_socket_representer_flag() == true);

    // Stop tracking this socket.
    UntrackSocket(sd->get_db_index(), sd->get_socket());

    // Updating the statistics.
#ifdef GW_COLLECT_SOCKET_STATISTICS
    if (sd->get_socket_diag_active_conn_flag())
    {
        ChangeNumActiveConnections(sd->get_port_index(), -1);
        sd->set_socket_diag_active_conn_flag(false);
    }
#endif

    // Checking if it was an accepting socket.
    if (ACCEPT_SOCKET_OPER == sd->get_type_of_network_oper())
        ChangeNumAcceptingSockets(sd->get_port_index(), -1);

#ifdef GW_PROXY_MODE

    // Checking if we have reusable proxied server connect.
    if (sd->get_proxied_server_socket_flag())
    {
        WorkerDbInterface *db = worker_dbs_[sd->get_db_index()];

        // Returning socket for reuse.
        reusable_connect_sockets_.push_back(sd->get_socket());

#ifdef GW_SOCKET_DIAG
        GW_COUT << "Adding socket for reuse: " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
#endif

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

#endif

    // Performing accept.
    return Accept(sd);
}

// Running connect on socket data.
uint32_t GatewayWorker::Connect(SocketDataChunk*& sd, sockaddr_in *serverAddr)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Connect: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
#endif

    while(TRUE)
    {
        // Start connecting socket.
        //profiler_.Start("Connect()", 3);
        uint32_t err_code = sd->Connect(this, serverAddr);
        TrackSocket(sd->get_db_index(), sd->get_socket());
        //profiler_.Stop(3);

        // Checking if operation completed immediately.
        assert(TRUE != err_code);

        int32_t wsa_err_code = WSAGetLastError();
        if (WSA_IO_PENDING != wsa_err_code)
        {
            if (WAIT_TIMEOUT == wsa_err_code)
            {
#ifdef GW_ERRORS_DIAG
                GW_PRINT_WORKER << "Timeout in ConnectEx. Retrying..." << std::endl;
#endif
                continue;
            }

#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed ConnectEx: " << sd->get_socket() << " " <<
                sd->get_chunk_index() << " Disconnecting socket..." << std::endl;
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
__forceinline uint32_t GatewayWorker::FinishConnect(SocketDataChunk*& sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishConnect: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
#endif

    // Since we are proxying this instance represents the socket.
    sd->set_socket_representer_flag(true);

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Changing active connections number.
    ChangeNumActiveConnections(sd->get_port_index(), 1);

    sd->set_socket_diag_active_conn_flag(true);

    sd->set_type_of_network_oper(UNKNOWN_SOCKET_OPER);

#endif

#ifdef GW_PROXY_MODE

    // Checking if we are in proxy mode.
    assert(sd->get_proxied_server_socket_flag() == true);

    // Sending to proxied server.
    return Send(sd);

#endif

#ifdef GW_TESTING_MODE

    // Connect is done only on a client.
    if (!g_gateway.setting_is_master())
    {
        switch (g_gateway.setting_mode())
        {
            case MODE_GATEWAY_PING:
            {
                // Checking that not all echoes are sent.
                if (!g_gateway.AllEchoesSent())
                {
                    // Sending echo request to server.
                    return SendRawEcho(sd, g_gateway.GetNextEchoNumber());
                }
                break;
            }
            
            case MODE_GATEWAY_HTTP:
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
uint32_t GatewayWorker::Accept(SocketDataChunk*& sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Accept: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
#endif

    // Start accepting on socket.
    //profiler_.Start("Accept()", 7);
    uint32_t errCode = sd->Accept(this);
    TrackSocket(sd->get_db_index(), sd->get_socket());
    //profiler_.Stop(7);

    // Checking if operation completed immediately.
    assert(TRUE != errCode);

    int32_t wsaErrCode = WSAGetLastError();
    if (WSA_IO_PENDING != wsaErrCode)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Failed AcceptEx: " << sd->get_socket() << ":" <<
            sd->get_chunk_index() << " Disconnecting socket..." << std::endl;
#endif
        PrintLastError();

        return SCERRGWACCEPTEXFAILED;
    }

    // Updating number of accepting sockets.
    ChangeNumAcceptingSockets(sd->get_port_index(), 1);

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    //setsockopt(sd->GetSocket(), SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT, NULL, 0 );

    return 0;
}

// Socket accept finished.
uint32_t GatewayWorker::FinishAccept(SocketDataChunk*& sd, int32_t numBytesReceived)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishAccept: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << std::endl;
#endif

    // This socket data is socket representation.
    sd->set_socket_representer_flag(true);

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Changing active connections number.
    ChangeNumActiveConnections(sd->get_port_index(), 1);

    sd->set_socket_diag_active_conn_flag(true);

    sd->set_type_of_network_oper(UNKNOWN_SOCKET_OPER);

#endif

    // Checking the endpoint information (e.g. for black listing).
    // TODO: Implement!
    //sockaddr_in remoteAddr = *(sockaddr_in *)(sd->accept_data() + sizeof(sockaddr_in) + 16);

    // Checking if we need to extend data.
    if (ChangeNumAcceptingSockets(sd->get_port_index(), -1) < ACCEPT_ROOF_STEP_SIZE)
    {
        // Creating new set of prepared connections.
        uint32_t errCode = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, sd->get_port_index(), sd->get_db_index());
        GW_ERR_CHECK(errCode);
    }

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
        SocketDataChunk* sd = sd_receive_clone_;
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
                {
                    // Releasing socket data chunks back to pool.
                    GetWorkerDb(sd->get_db_index())->ReturnSocketDataChunksToPool(this, sd);

                    continue;
                }

                // Checking if its not receiving socket data.
                if (!sd->get_socket_representer_flag())
                    assert(sd->get_type_of_network_oper() > DISCONNECT_SOCKET_OPER);

                // Checking for IOCP operation result.
                if (TRUE != WSAGetOverlappedResult(sd->get_socket(), sd->get_ovl(), (LPDWORD)&numBytes, FALSE, (LPDWORD)&flags))
                {
                    err_code = WSAGetLastError();
                    if (WSA_IO_INCOMPLETE == err_code)
                        continue;

#ifdef GW_ERRORS_DIAG
                    GW_PRINT_WORKER << "IOCP operation failed: " << GetOperTypeString(sd->get_type_of_network_oper()) <<
                        " " << sd->get_socket() << " " << sd->get_chunk_index() << ". Disconnecting socket..." << std::endl;
#endif
                    PrintLastError();

                    // Disconnecting this socket data.
                    DisconnectAndReleaseChunk(sd);

                    continue;
                }

                // Checking type of operation.
                // NOTE: Any failure on the following operations means that chunk is still in use!
                switch (sd->get_type_of_network_oper())
                {
                    // ACCEPT finished.
                    case ACCEPT_SOCKET_OPER:
                    {
                        err_code = FinishAccept(sd, numBytes);
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
                        err_code = FinishSend(sd, numBytes);
                        break;
                    }

                    // RECEIVE finished.
                    case RECEIVE_SOCKET_OPER:
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
        WorkerDbInterface *db = GetWorkerDb(i);
        if (NULL != db)
        {
            // Scanning channels first.
            errCode = db->ScanChannels(this, found_something);
            GW_ERR_CHECK(errCode);

            // Checking that database is ready for deletion (i.e. no pending sockets and chunks).
            if (g_gateway.GetDatabase(i)->IsEmpty())
            {
                // Entering global lock.
                EnterGlobalLock();

                // Deleting all associated info with this database from ports.
                g_gateway.DeletePortsForDb(i);

                // Finally completely deleting database object and closing shared memory.
                DeleteInactiveDatabase(i);

#ifdef GW_DATABASES_DIAG
                GW_PRINT_WORKER << "Deleted shared memory for db slot: " << i << std::endl;
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
    worker_dbs_[db_index] = new WorkerDbInterface();
    uint32_t errCode = worker_dbs_[db_index]->Init(db_index, worker_shared_int, this);
    GW_ERR_CHECK(errCode);

    return 0;
}

// Push given chunk to database queue.
uint32_t GatewayWorker::PushSocketDataToDb(SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id)
{
    WorkerDbInterface *db = GetWorkerDb(sd->get_db_index());
    assert(NULL != db);
    
    return db->PushSocketDataToDb(this, sd, handler_id);
}

// Deleting inactive database.
void GatewayWorker::DeleteInactiveDatabase(int32_t db_index)
{
    delete worker_dbs_[db_index];
    worker_dbs_[db_index] = NULL;
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

#endif

} // namespace network
} // namespace starcounter
