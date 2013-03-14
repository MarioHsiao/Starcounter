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

        // Skipping completion port if operation is already successful.
        SetFileCompletionNotificationModes((HANDLE)new_socket, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);

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

    // Marking socket as alive.
    g_gateway.MarkSocketAlive(new_socket);

#ifdef GW_COLLECT_SOCKET_STATISTICS

    if (!reused_socket)
    {
        // Changing number of created sockets.
        int64_t created_sockets = g_gateway.get_server_port(proxy_sd->get_port_index())->ChangeNumAllocatedConnectSockets(1);

#ifdef GW_SOCKET_DIAG
        GW_PRINT_WORKER << "New sockets amount: " << created_sockets << GW_ENDL;
#endif
    }

#endif

    // Setting receiving socket.
    proxy_sd->set_proxy_socket(proxy_sd->get_socket());

    // Setting new socket.
    proxy_sd->set_socket(new_socket);

    return 0;
}

#endif

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
        int32_t onFlag = 1;

        // Disables the Nagle algorithm for send coalescing.
        if (setsockopt(new_socket, IPPROTO_TCP, TCP_NODELAY, (char *)&onFlag, 4))
        {
            GW_PRINT_WORKER << "Can't set TCP_NODELAY on socket." << GW_ENDL;

            closesocket(new_socket);

            return PrintLastError();
        }

        // Does not block close waiting for unsent data to be sent.
        if (setsockopt(new_socket, SOL_SOCKET, SO_DONTLINGER, (char *)&onFlag, 4))
        {
            GW_PRINT_WORKER << "Can't set SO_DONTLINGER on socket." << GW_ENDL;

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
            GW_PRINT_WORKER << "Can't put socket into non-blocking mode." << GW_ENDL;
#endif
            closesocket(new_socket);

            return PrintLastError();
        }

        // Creating new socket data structure inside chunk.
        SocketDataChunk* new_sd = NULL;
        err_code = CreateSocketData(new_socket, port_index, db_index, new_sd);
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
    GW_PRINT_WORKER << "New sockets amount: " << created_sockets << GW_ENDL;
#endif

#endif

#ifdef GW_TESTING_MODE
    // Updating number of created connections for worker.
    num_created_conns_worker_ += how_many;
#endif

    return 0;
}

// Running receive on socket data.
uint32_t GatewayWorker::Receive(SocketDataChunkRef sd)
{
// This label is used to avoid recursiveness between Receive and FinishReceive.
START_RECEIVING_AGAIN:

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Receive: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

#ifdef GW_SOCKET_ID_CHECK
    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;
#endif

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
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed WSARecv: " << sd->get_socket() << " " <<
                sd->get_chunk_index() << " Disconnecting socket..." << GW_ENDL;
#endif
            PrintLastError();

            return SCERRGWFAILEDWSARECV;
        }

        // NOTE: Setting socket data to null, so other
        // manipulations on it are not possible.
        sd = NULL;
    }
    else
    {
        // Checking if socket is closed by the other peer.
        if (0 == numBytes)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Zero-bytes receive on socket: " << sd->get_socket() << " " <<
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

    return 0;
}

// Socket receive finished.
__forceinline uint32_t GatewayWorker::FinishReceive(
    SocketDataChunkRef sd,
    int32_t num_bytes_received,
    bool& called_from_receive)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishReceive: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

#ifdef GW_SOCKET_ID_CHECK
    // Checking correct unique socket.
    GW_ASSERT(true == sd->CompareUniqueSocketId());
#endif

    // If we received 0 bytes, the remote side has close the connection.
    if (0 == num_bytes_received)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Zero-bytes receive on socket: " << sd->get_socket() << " " <<
            sd->get_chunk_index() << ". Remote side closed the connection." << GW_ENDL;
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
            GW_PRINT_WORKER << "Data from abandoned/different socket received." << GW_ENDL;
#endif

            // Just resetting the session.
            sd->ResetSdSession();
        }
    }

    // Running the handler.
    return RunReceiveHandlers(sd);
}

// Running send on socket data.
uint32_t GatewayWorker::Send(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Send: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

#ifdef GW_SOCKET_ID_CHECK
    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;
#endif

    // Start sending on socket.
#ifdef GW_PROFILER_ON
    profiler_.Start("Send()", 2);
#endif

    uint32_t num_bytes, err_code;

    // Checking if we have one or multiple chunks to send.
    if (1 == sd->get_num_chunks())
    {
        err_code = sd->SendSingleChunk(this, &num_bytes);
    }
    else
    {
        err_code = sd->SendMultipleChunks(this, worker_dbs_[sd->get_db_index()]->get_shared_int(), &num_bytes);
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
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed WSASend on socket: " << sd->get_socket() << " "
                << sd->get_chunk_index() << ". Disconnecting socket..." << GW_ENDL;
#endif
            PrintLastError();

            return SCERRGWFAILEDWSASEND;
        }

        // NOTE: Setting socket data to null, so other
        // manipulations on it are not possible.
        sd = NULL;
    }
    else
    {
        // Finish send operation.
        err_code = FinishSend(sd, num_bytes);
        GW_ERR_CHECK(err_code);
    }

    return 0;
}

// Socket send finished.
__forceinline uint32_t GatewayWorker::FinishSend(SocketDataChunkRef sd, int32_t num_bytes_sent)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishSend: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

#ifdef GW_SOCKET_ID_CHECK
    // Checking correct unique socket.
    if (false == sd->CompareUniqueSocketId())
    {
        // Only non-representative socket data can have wrong socket id.
        GW_ASSERT(false == sd->get_socket_representer_flag());

        return SCERRGWOPERATIONONWRONGSOCKET;
    }
#endif

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Checking that we processed correct number of bytes.
    if (num_bytes_sent != accum_buf->get_buf_len_bytes())
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Incorrect number of bytes sent: " << num_bytes_sent << " of " << accum_buf->get_buf_len_bytes() << "(correct)" << GW_ENDL;
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
    GW_ASSERT(db != NULL);

    // Returning chunks to pool.
    db->ReturnSocketDataChunksToPool(this, sd);

    return 0;
}

// Running disconnect on socket data.
// NOTE: Socket data chunk can not be used after this function is called!
void GatewayWorker::DisconnectAndReleaseChunk(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Disconnect: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

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

#ifdef GW_SOCKET_ID_CHECK
    // Setting unique socket id.
    sd->SetUniqueSocketId();
#endif

    // Calling DisconnectEx.
    uint32_t err_code = sd->Disconnect(this);

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
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed DisconnectEx." << GW_ENDL;
#endif
            PrintLastError();

            // Finish disconnect operation.
            // (e.g. returning to pool or starting accept).
            if (FinishDisconnect(sd, true))
                goto RELEASE_CHUNK_TO_POOL;

            return;
        }

        // NOTE: Setting socket data to null, so other
        // manipulations on it are not possible.
        sd = NULL;

        // The disconnect operation is pending.
        return;
    }
    else
    {
        // Finish disconnect operation.
        // (e.g. returning to pool or starting accept).
        if (FinishDisconnect(sd, false))
            goto RELEASE_CHUNK_TO_POOL;

        return;
    }

    // Returning the chunk to pool.
RELEASE_CHUNK_TO_POOL:

    WorkerDbInterface *db = worker_dbs_[sd->get_db_index()];
    GW_ASSERT(db != NULL);

    // Returning chunks to pool.
    db->ReturnSocketDataChunksToPool(this, sd);
}

// Socket disconnect finished.
__forceinline uint32_t GatewayWorker::FinishDisconnect(SocketDataChunkRef sd, bool just_release)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishDisconnect: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

#ifdef GW_SOCKET_ID_CHECK
    // Checking correct unique socket.
    GW_ASSERT(true == sd->CompareUniqueSocketId());
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    GW_ASSERT(sd->get_type_of_network_oper() != UNKNOWN_SOCKET_OPER);
#endif

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

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
        SOCKET sock = sd->get_socket();
        reusable_connect_sockets_.PushBack(sock);

#ifdef GW_SOCKET_DIAG
        GW_COUT << "Adding socket for reuse: " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

        // Returning chunks to pool.
        db->ReturnSocketDataChunksToPool(this, sd);

        return 0;
    }

#endif

    // Checking if just releasing the socket data.
    if (just_release)
        return SCERRJUSTRELEASEDSOCKETDATA;

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
    GW_PRINT_WORKER << "Connect: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

    while(TRUE)
    {
        // Start connecting socket.

#ifdef GW_PROFILER_ON
        profiler_.Start("Connect()", 4);
#endif

        // Start tracking this socket.
        TrackSocket(sd->get_db_index(), sd->get_socket());

#ifdef GW_SOCKET_ID_CHECK
        // Setting unique socket id.
        sd->SetUniqueSocketId();
#endif

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

#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed ConnectEx: " << sd->get_socket() << " " <<
                sd->get_chunk_index() << " Disconnecting socket..." << GW_ENDL;
#endif
            PrintLastError();
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
    GW_PRINT_WORKER << "FinishConnect: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

#ifdef GW_SOCKET_ID_CHECK
    // Checking correct unique socket.
    GW_ASSERT(true == sd->CompareUniqueSocketId());
#endif

#ifndef GW_LOOPED_TEST_MODE
    // Setting SO_UPDATE_CONNECT_CONTEXT.
    if (setsockopt(sd->get_socket(), SOL_SOCKET, SO_UPDATE_CONNECT_CONTEXT, NULL, 0))
    {
        GW_PRINT_WORKER << "Can't set SO_UPDATE_CONNECT_CONTEXT on socket." << GW_ENDL;
        return SCERRGWCONNECTEXFAILED;
    }
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
    GW_PRINT_WORKER << "Accept: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Start accepting on socket.

#ifdef GW_PROFILER_ON
    profiler_.Start("Accept()", 5);
#endif

    // Tracking corresponding socket.
    TrackSocket(sd->get_db_index(), sd->get_socket());

    // Updating number of accepting sockets.
    ChangeNumAcceptingSockets(sd->get_port_index(), 1);

#ifdef GW_SOCKET_ID_CHECK
    // Setting unique socket id.
    sd->SetUniqueSocketId();
#endif

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
        ChangeNumAcceptingSockets(sd->get_port_index(), -1);

#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Failed AcceptEx: " << sd->get_socket() << ":" <<
            sd->get_chunk_index() << " Disconnecting socket..." << GW_ENDL;
#endif
        PrintLastError();

        return SCERRGWACCEPTEXFAILED;
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
    GW_PRINT_WORKER << "FinishAccept: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

#ifdef GW_SOCKET_ID_CHECK
    // Checking correct unique socket.
    GW_ASSERT(true == sd->CompareUniqueSocketId());
#endif

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

    // Decreasing number of accepting sockets.
    int64_t cur_num_accept_sockets = ChangeNumAcceptingSockets(sd->get_port_index(), -1);

#ifdef GW_LOOPED_TEST_MODE

    // Checking that we don't exceed number of active connections.
    if (num_created_conns_worker_ < g_gateway.setting_num_connections_to_master_per_worker())
    {
        // Creating new set of prepared connections.
        err_code = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, sd->get_port_index(), sd->get_db_index());
        GW_ERR_CHECK(err_code);
    }

#else

    // Checking if we need to extend number of accepting sockets.
    if (cur_num_accept_sockets < ACCEPT_ROOF_STEP_SIZE)
    {
        // Creating new set of prepared connections.
        err_code = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, sd->get_port_index(), sd->get_db_index());
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
    uint32_t oper_num_bytes = 0, flags = 0, oldTimeMs = timeGetTime(), newTimeMs;
    bool found_something = false;
    uint32_t sleep_interval_ms = INFINITE;

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
        compl_status = GetQueuedCompletionStatusEx(worker_iocp_, fetched_ovls, MAX_FETCHED_OVLS, &num_fetched_ovls, sleep_interval_ms, TRUE);
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
                GW_PRINT_WORKER << "GetQueuedCompletionStatusEx: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << ":" << (uint64_t)sd << GW_ENDL;
#endif

                // Checking for socket data correctness.
                GW_ASSERT((sd->get_db_index() >= 0) && (sd->get_db_index() < MAX_ACTIVE_DATABASES));
                GW_ASSERT(sd->get_socket() < g_gateway.setting_max_connections());
                GW_ASSERT(sd->get_chunk_index() != INVALID_CHUNK_INDEX);

                // Checking that socket is valid.
                if (!sd->ForceSocketDataValidity(this))
                {
                    // Releasing socket data chunks back to pool.
                    GetWorkerDb(sd->get_db_index())->ReturnSocketDataChunksToPool(this, sd);

                    continue;
                }

                // Checking if its not receiving socket data.
                if (!sd->get_socket_representer_flag())
                    GW_ASSERT(sd->get_type_of_network_oper() > DISCONNECT_SOCKET_OPER);

#ifdef GW_LOOPED_TEST_MODE
                oper_num_bytes = fetched_ovls[i].dwNumberOfBytesTransferred;
#else
                // Checking for IOCP operation result.
                if (TRUE != WSAGetOverlappedResult(sd->get_socket(), sd->get_ovl(), (LPDWORD)&oper_num_bytes, FALSE, (LPDWORD)&flags))
                {
                    err_code = WSAGetLastError();
                    if (WSA_IO_INCOMPLETE == err_code)
                        continue;

#ifdef GW_ERRORS_DIAG
                    GW_PRINT_WORKER << "IOCP operation failed: " << GetOperTypeString(sd->get_type_of_network_oper()) <<
                        " " << sd->get_socket() << " " << sd->get_chunk_index() << ". Disconnecting socket..." << GW_ENDL;
#endif
                    PrintLastError();

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
                        err_code = FinishDisconnect(sd, false);
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

        // Scanning all channels.
        found_something = false;
        err_code = ScanChannels(&found_something);
        GW_ERR_CHECK(err_code);

        // Checking if something was found.
        if (found_something)
        {
            // Making at least one more round.
            sleep_interval_ms = 0;
        }
        else
        {
            // Going to wait infinitely for network events.
            sleep_interval_ms = INFINITE;
        }

        // Checking inactive sessions cleanup (only first worker).
        if ((g_gateway.get_num_sessions_to_cleanup_unsafe()) && (worker_id_ == 0))
            g_gateway.CleanupInactiveSessions(this);

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

// Scans all channels for any incoming chunks.
void __stdcall EmptyApcFunction(ULONG_PTR arg);
uint32_t GatewayWorker::ScanChannels(bool* found_something)
{
    uint32_t err_code;

    for (int32_t i = 0; i < g_gateway.get_num_dbs_slots(); i++)
    {
        // Scan channels.
        WorkerDbInterface *db = GetWorkerDb(i);
        if (NULL != db)
        {
            // Scanning channels first.
            err_code = db->ScanChannels(this, found_something);
            GW_ERR_CHECK(err_code);

            // Checking if database deletion is started.
            if (g_gateway.GetDatabase(i)->IsDeletionStarted())
            {
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
                    GW_PRINT_WORKER << "Deleted shared memory for db slot: " << i << GW_ENDL;
#endif

                    // Leaving global lock.
                    LeaveGlobalLock();
                }
                else
                {
                    // Gateway needs to loop for a while because of chunks being released.
                    *found_something = true;

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
    SOCKET sock,
    int32_t port_index,
    int32_t db_index,
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
    out_sd = (SocketDataChunk*)((uint8_t*)smc + bmx::BMX_HEADER_MAX_SIZE_BYTES);

    // Initializing socket data.
    out_sd->Init(sock, port_index, db_index, chunk_index);

    // Configuring data buffer.
    out_sd->get_accum_buf()->Init(SOCKET_DATA_BLOB_SIZE_BYTES, out_sd->get_data_blob(), true);

    return 0;
}

// Adds new active database.
uint32_t GatewayWorker::AddNewDatabase(
    int32_t db_index,
    const core::shared_interface& worker_shared_int)
{
    worker_dbs_[db_index] = new WorkerDbInterface(db_index, worker_shared_int, worker_id_);
    return 0;
}

// Push given chunk to database queue.
uint32_t GatewayWorker::PushSocketDataToDb(SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id)
{
    // Getting database to which this chunk belongs.
    WorkerDbInterface *db = GetWorkerDb(sd->get_db_index());
    GW_ASSERT(NULL != db);
    
    // Pushing chunk to that database.
    return db->PushSocketDataToDb(this, sd, handler_id);
}

// Gets a new chunk for new database and copies the old one into it.
uint32_t GatewayWorker::CloneChunkForNewDatabase(SocketDataChunkRef old_sd, int32_t new_db_index, SocketDataChunk** new_sd)
{
    // TODO: Add support for linked chunks.
    GW_ASSERT(1 == old_sd->get_num_chunks());

    core::chunk_index new_chunk_index;
    shared_memory_chunk* new_smc;

    // Getting a chunk from new database.
    uint32_t err_code = worker_dbs_[new_db_index]->GetOneChunkFromPrivatePool(&new_chunk_index, &new_smc);
    if (err_code)
    {
        // New chunk can not be obtained.
        return err_code;
    }

    // Socket data inside chunk.
    (*new_sd) = (SocketDataChunk*)((uint8_t*)new_smc + bmx::BMX_HEADER_MAX_SIZE_BYTES);

    // Copying chunk data.
    memcpy(new_smc, old_sd->get_smc(),
        bmx::BMX_HEADER_MAX_SIZE_BYTES + SOCKET_DATA_BLOB_OFFSET_BYTES + old_sd->get_accum_buf()->get_accum_len_bytes());

    // Attaching to new database.
    (*new_sd)->AttachToDatabase(new_db_index);

    // Changing new chunk index.
    (*new_sd)->set_chunk_index(new_chunk_index);

    // Returning old chunk to its pool.
    // TODO: Or disconnect?
    old_sd->set_socket_diag_active_conn_flag(false);
    worker_dbs_[old_sd->get_db_index()]->ReturnSocketDataChunksToPool(this, old_sd);

    return 0;
}

// Deleting inactive database.
void GatewayWorker::DeleteInactiveDatabase(int32_t db_index)
{
    delete worker_dbs_[db_index];
    worker_dbs_[db_index] = NULL;
}

// Sends given predefined response.
uint32_t GatewayWorker::SendPredefinedMessage(
    SocketDataChunkRef sd,
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
    uint32_t max_fetched)
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
                    accum_buffer->get_buf_len_bytes(),
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
                fetched_ovls[num_processed].dwNumberOfBytesTransferred = accum_buffer->get_buf_len_bytes();

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
                // Returning this chunk to database.
                WorkerDbInterface *db = GetWorkerDb(sd->get_db_index());
                GW_ASSERT(db != NULL);

#ifdef GW_COLLECT_SOCKET_STATISTICS
                sd->set_socket_diag_active_conn_flag(false);
#endif

                // Returning chunks to pool.
                db->ReturnSocketDataChunksToPool(this, sd);

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
