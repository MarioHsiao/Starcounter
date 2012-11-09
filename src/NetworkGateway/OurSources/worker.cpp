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
    /*worker_iocp_ = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 1);
    if (worker_iocp_ == NULL)
    {
        GW_PRINT_WORKER << "Failed to create worker IOCP." << std::endl;
        return PrintLastError();
    }*/

    // Getting global iocp.
    worker_iocp_ = g_gateway.get_iocp();

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

// Allocates a bunch of new connections.
uint32_t GatewayWorker::CreateNewConnections(int32_t how_many, int32_t port_index, int32_t db_index)
{
    uint32_t errCode;
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
            return PrintLastError();
        }

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
            return PrintLastError();
        }

        // Creating new socket data structure inside chunk.
        SocketDataChunk *sd = CreateSocketData(
            new_socket,
            port_index,
            db_index);

        // Marking socket as alive.
        g_gateway.MarkSocketAlive(new_socket);

        // Checking if its a master node.
        if (!g_gateway.setting_is_master())
        {
            // Performing connect.
            errCode = Connect(sd, g_gateway.get_server_addr());
            GW_ERR_CHECK(errCode);
        }
        else
        {
            // Performing accept.
            errCode = Accept(sd);
            GW_ERR_CHECK(errCode);
        }
    }

    // Increasing number of allocated sockets.
    int64_t created_sockets = g_gateway.get_server_port(port_index)->ChangeNumAllocatedSockets(db_index, how_many);
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "New sockets amount: " << created_sockets << std::endl;
#endif

    return 0;
}

// Running receive on socket data.
uint32_t GatewayWorker::Receive(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Receive: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // Start receiving on socket.
    //profiler_.Start("Receive()", 5);
    uint32_t numBytes, errCode;

    // Checking if we have one or multiple chunks to receive.
    //if (1 == sd->get_num_chunks())
    {
        errCode = sd->ReceiveSingleChunk(&numBytes);
    }
    /*else
    {
        errCode = sd->ReceiveMultipleChunks(active_dbs_[sd->get_db_index()]->get_shared_int(), &numBytes);
    }*/
    //profiler_.Stop(5);

    // Checking if operation completed immediately.
    if (0 != errCode)
    {
        int32_t wsaErrCode = WSAGetLastError();
        if (WSA_IO_PENDING != wsaErrCode)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed WSARecv: " << sd->sock() << " " <<
                sd->chunk_index() << " Disconnecting socket..." << std::endl;
#endif
            PrintLastError();

            // Disconnecting this socket.
            Disconnect(sd);

            return 1;
        }
    }
    else
    {
        // Checking if socket is closed by the other peer.
        if (0 == numBytes)
        {
            // Disconnecting this socket.
            Disconnect(sd);

            return 1;
        }
        else
        {
            // Finish receive operation.
            errCode = FinishReceive(sd, numBytes);
            GW_ERR_CHECK(errCode);
        }
    }

    return 0;
}

// Socket receive finished.
uint32_t GatewayWorker::FinishReceive(SocketDataChunk *sd, int32_t numBytesReceived)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishReceive: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // If we received 0 bytes, the remote side has close the connection.
    if (0 == numBytesReceived)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Zero-bytes receive on socket: " << sd->sock() << ". Remote side closed the connection." << std::endl;
#endif

        // Disconnecting this socket.
        Disconnect(sd);

        return 0;
    }

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Adding to accumulated bytes.
    accum_buf->AddAccumulatedBytes(numBytesReceived);

    // Incrementing statistics.
    worker_stats_bytes_received_ += numBytesReceived;

    // Increasing number of receives.
    worker_stats_recv_num_++;

    // Assigning last received bytes.
    if (!sd->get_accumulating_flag())
    {
        accum_buf->SetLastReceivedBytes(numBytesReceived);
    }
    else
    {
        // Adding last received bytes.
        accum_buf->AddLastReceivedBytes(numBytesReceived);

        // Indicates if all data was accumulated.
        bool is_accumulated;

        // Trying to continue accumulation.
        uint32_t err_code = sd->ContinueAccumulation(this, &is_accumulated);
        GW_ERR_CHECK(err_code);

        // Checking if we have not accumulated everything yet.
        if (!is_accumulated)
            return Receive(sd);
    }

    // Getting attached session if any.
    ScSessionStruct* session = sd->GetAttachedSession();
    if (session)
    {
        // Check that data received belongs to the correct session (not coming from abandoned connection).
        if (!session->CompareSalts(sd->get_session_salt()))
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Data from abandoned/different socket received." << std::endl;
#endif
            // Disconnecting this socket.
            Disconnect(sd);

            return 0;
        }
    }
    else
    {
        // If session was already created, just attaching to it.
        session_index_type session_index = g_gateway.GetSocketData(sd->get_socket())->get_session_index();
        ScSessionStruct* session = g_gateway.GetSessionData(session_index);
        if (session)
            sd->AttachToSession(session);
    }

    // Checking if we have received everything.
    /*
    if (accumLenBytesRef < g_msgLenBytes)
    {
        // Shifting receive buffer pointer for the next receive.
        (recvBuf->curBufPtr) += numBytesReceived;

        // Adding back to receive since not all data was received.
        PutSocketToRecv(sd);

        return true;
    }
    else
    {
        // Checking that we processed correct number of bytes.
        if (accumLenBytesRef != g_msgLenBytes)
        {
            GW_PRINT_WORKER << "Incorrect number of bytes received: " << accumLenBytesRef << " of " << g_msgLenBytes << "(correct)" << std::endl;
            return false;
        }
    }

    // Comparing data contents.
    if (g_isClientMode)
    {
        // Fetching and comparing last four bytes of the message.
        uint32_t contentSend = (*(uint32_t *)(sd->sendBuf->origBufPtr + g_msgLenBytes - 4));
        uint32_t contentRecv = (*(uint32_t *)(recvBuf->origBufPtr + g_msgLenBytes - 4));
        if (contentRecv != contentSend)
        {
            GW_PRINT_WORKER << "Incorrect contents of message received: " << contentRecv << " vs " << contentSend << "(correct)" << std::endl;
            return false;
        }
    }
    else
    {
        // Simply echoing receive buffer.
        memcpy(sd->sendBuf->origBufPtr, recvBuf->origBufPtr, g_msgLenBytes);
    }

    // Resetting socket data.
    sd->Reset();
    */

    if (!g_gateway.setting_is_master())
    {
        // Performing send.
        return Send(sd);
    }
    else
    {
        // Running the handler.
        return RunToDbHandlers(sd);
    }

    return 0;
}

// Running send on socket data.
uint32_t GatewayWorker::Send(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Send: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // Start sending on socket.
    //profiler_.Start("Send()", 6);
    uint32_t numBytes, errCode;

    // Checking if we have one or multiple chunks to send.
    if (1 == sd->get_num_chunks())
    {
        errCode = sd->SendSingleChunk(&numBytes);
    }
    else
    {
        errCode = sd->SendMultipleChunks(active_dbs_[sd->get_db_index()]->get_shared_int(), &numBytes);
    }
    //profiler_.Stop(6);

    // Checking if operation completed immediately.
    if (0 != errCode)
    {
        int32_t wsaErrCode = WSAGetLastError();
        if (WSA_IO_PENDING != wsaErrCode)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed WSASend. Disconnecting socket..." << std::endl;
#endif
            PrintLastError();
            Disconnect(sd);
            return 1;
        }
    }
    else
    {
        // Finish send operation.
        errCode = FinishSend(sd, numBytes);
        GW_ERR_CHECK(errCode);
    }

    return 0;
}

// Socket send finished.
uint32_t GatewayWorker::FinishSend(SocketDataChunk *sd, int32_t num_bytes_sent)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishSend: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    AccumBuffer* accum_buf = sd->get_accum_buf();

    // Checking that we processed correct number of bytes.
    if (num_bytes_sent != accum_buf->get_buf_len_bytes())
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Incorrect number of bytes sent: " << num_bytes_sent << " of " << accum_buf->get_buf_len_bytes() << "(correct)" << std::endl;
#endif
        return SCERRUNSPECIFIED;
    }

    // Incrementing statistics.
    worker_stats_bytes_sent_ += num_bytes_sent;

    // Increasing number of sends.
    worker_stats_sent_num_++;

    // Checking disconnect state.
    if (sd->get_disconnect_after_send_flag())
    {
        // Performing disconnect.
        Disconnect(sd);

        return 0;
    }

    // Resets data buffer offset.
    sd->ResetUserDataOffset();

    // Resetting buffer information.
    accum_buf->ResetBufferForNewOperation();

    // Checking if socket data is for receiving.
    if (sd->get_receiving_flag())
    {
        // Performing receive.
        Receive(sd);

        return 0;
    }

    // Returning chunk to private chunk pool.
    WorkerDbInterface *db = active_dbs_[sd->get_db_index()];
    if (NULL != db)
        return db->ReturnChunkToPool(this, sd);

    return SCERRUNSPECIFIED;
}

// Running disconnect on socket data.
uint32_t GatewayWorker::Disconnect(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Disconnect: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // Checking if its not receiving socket data.
    if (!sd->get_receiving_flag())
    {
        // Returning chunk to private chunk pool.
        WorkerDbInterface *db = active_dbs_[sd->get_db_index()];
        if (NULL != db)
            return db->ReturnChunkToPool(this, sd);

        return SCERRUNSPECIFIED;
    }

    // Start disconnecting socket.
    //profiler_.Start("Disconnect()", 4);
    uint32_t errCode = sd->Disconnect();
    //profiler_.Stop(4);

    // Checking if operation completed immediately. 
    if (TRUE != errCode)
    {
        int32_t wsaErrCode = WSAGetLastError();

        // Checking if socket was not connected.
        if (WSAENOTCONN == wsaErrCode)
        {
            // Finish disconnect operation.
            FinishDisconnect(sd);
            return SCERRUNSPECIFIED;
        }

        // Checking for other errors.
        if (WSA_IO_PENDING != wsaErrCode)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Failed DisconnectEx." << std::endl;
#endif
            PrintLastError();
            return 1;
        }
    }
    else
    {
        // Finish disconnect operation.
        errCode = FinishDisconnect(sd);
        GW_ERR_CHECK(errCode);
    }

    return 0;
}

// Vanishes existing socket.
uint32_t GatewayWorker::VanishSocketData(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Vanish Socket Data: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // Checking if we were accepting socket.
    bool was_accepting = sd->type_of_network_oper() == ACCEPT_OPER;

    // Resetting the socket data.
    sd->Reset();

    // Returning chunk to private chunk pool.
    WorkerDbInterface *db = active_dbs_[sd->get_db_index()];
    uint32_t err_code = db->ReturnChunkToPool(this, sd);
    GW_ERR_CHECK(err_code);

    // Stop tracking this socket.
    err_code = g_gateway.GetDatabase(sd->get_db_index())->UntrackSocket(sd->sock());
    GW_ERR_CHECK(err_code);

    // Decreasing number of connections.
    ServerPort* server_port = g_gateway.get_server_port(sd->get_port_index());
    server_port->ChangeNumAllocatedSockets(sd->get_db_index(), -1);

    // Checking if were performing some operation that is not Accept.
    if (!was_accepting)
        server_port->ChangeNumActiveConns(sd->get_db_index(), -1);

    return 0;
}

// Socket disconnect finished.
inline uint32_t GatewayWorker::FinishDisconnect(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishDisconnect: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // Removing tracked session.
    g_gateway.GetSocketData(sd->get_socket())->Reset();

    // Stop tracking this socket.
    g_gateway.GetDatabase(sd->get_db_index())->UntrackSocket(sd->sock());

    // Checking if database is still up.
    if (!GetDatabase(sd->get_db_index()))
        return 0;

    // Getting corresponding server port.
    ServerPort* server_port = g_gateway.get_server_port(sd->get_port_index());

    // Decreasing number of connections.
    server_port->ChangeNumActiveConns(sd->get_db_index(), -1);

    // Resetting the socket data.
    sd->Reset();

    if (!g_gateway.setting_is_master())
    {
        // Performing connect.
        Connect(sd, g_gateway.get_server_addr());
    }
    else
    {
        // Performing accept.
        Accept(sd);
    }

    return 0;
}

// Running connect on socket data.
uint32_t GatewayWorker::Connect(SocketDataChunk *sd, sockaddr_in *serverAddr)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Connect: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    while(TRUE)
    {
        // Start connecting socket.
        //profiler_.Start("Connect()", 3);
        uint32_t errCode = sd->Connect(serverAddr);
        //profiler_.Stop(3);

        // Checking if operation completed immediately.
        if (TRUE != errCode)
        {
            int32_t wsaErrCode = WSAGetLastError();
            if (WSA_IO_PENDING != wsaErrCode)
            {
                if (WAIT_TIMEOUT == wsaErrCode)
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
                return 1;
            }
        }
        else
        {
            // Should never happen for connect.
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "ConnectEx finished immediately, WTF!" << std::endl;
#endif
            return 1;
        }

        break;
    }

    // Setting SO_UPDATE_CONNECT_CONTEXT.
    //setsockopt(sd->s, SOL_SOCKET, SO_UPDATE_CONNECT_CONTEXT, NULL, 0);

    return 0;
}

// Socket connect finished.
uint32_t GatewayWorker::FinishConnect(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishConnect: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // Increasing number of connections.
    g_gateway.get_server_port(sd->get_port_index())->ChangeNumActiveConns(sd->get_db_index(), 1);

    // Performing send.
    Send(sd);

    return 0;
}

// Running accept on socket data.
uint32_t GatewayWorker::Accept(SocketDataChunk *sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Accept: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // This socket data is for receiving.
    sd->set_receiving_flag(true);

    // Start accepting on socket.
    //profiler_.Start("Accept()", 7);
    uint32_t errCode = sd->Accept(this);
    //profiler_.Stop(7);
 
    // Checking if operation completed immediately.
    if (TRUE != errCode)
    {
        int32_t wsaErrCode = WSAGetLastError();
        if (WSA_IO_PENDING != wsaErrCode)
        {
#ifdef GW_ERRORS_DIAG
            GW_PRINT_WORKER << "Error accepting socket." << std::endl;
#endif
            PrintLastError();
            return 1;
        }
    }
    else
    {
        // Should never happen for connect.
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "AcceptEx finished immediately, WTF!" << std::endl;
#endif
        return 1;
    }

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    //setsockopt(sd->GetSocket(), SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT, NULL, 0 );

    return 0;
}

// Socket accept finished.
uint32_t GatewayWorker::FinishAccept(SocketDataChunk *sd, int32_t numBytesReceived)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishAccept: socket " << sd->sock() << " chunk " << sd->chunk_index() << std::endl;
#endif

    // Checking the endpoint information (e.g. for black listing).
    sockaddr_in remoteAddr = *(sockaddr_in *)(sd->accept_data() + sizeof(sockaddr_in) + 16);

    // Getting server port index and database index.
    int32_t port_index = sd->get_port_index(), db_index = sd->get_db_index();

    // Getting server port reference.
    ServerPort *server_port = g_gateway.get_server_port(port_index);

    // Increasing number of active connections.
    server_port->ChangeNumActiveConns(db_index, 1);

    // Checking if we need to extend data.
    if ((server_port->get_num_allocated_sockets(db_index) - server_port->get_num_active_conns(db_index)) < ACCEPT_ROOF_STEP_SIZE)
    {
        // Creating new set of prepared connections.
        uint32_t errCode = CreateNewConnections(ACCEPT_ROOF_STEP_SIZE, port_index, db_index);
        GW_ERR_CHECK(errCode);
    }

    // Performing receive.
    Receive(sd);

    return 0;
}

uint32_t GatewayWorker::WorkerRoutine()
{
    BOOL complStatus = false;
    OVERLAPPED_ENTRY *removedOvls = new OVERLAPPED_ENTRY[MAX_FETCHED_OVLS];
    ULONG removedOvlsNum = 0;
    uint32_t errCode = 0;
    uint32_t numBytes = 0, flags = 0, oldTimeMs = timeGetTime(), newTimeMs;
    uint32_t waitForIocpMs = INFINITE;
    bool found_something = false;

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

                // Checking that socket is valid.
                if (!sd->CheckSocketIsValid(this))
                    continue;

                // Checking for IOCP operation result.
                if (TRUE != WSAGetOverlappedResult(sd->sock(), sd->get_ovl(), (LPDWORD)&numBytes, FALSE, (LPDWORD)&flags))
                {
                    errCode = WSAGetLastError();
                    if ((WSA_IO_PENDING != errCode) && (WSA_IO_INCOMPLETE != errCode))
                    {
#ifdef GW_ERRORS_DIAG
                        GW_PRINT_WORKER << "IOCP operation failed: " << GetOperTypeString(sd->type_of_network_oper()) <<
                            " " << sd->sock() << " " << sd->chunk_index() << ". Disconnecting socket..." << std::endl;
#endif
                        PrintLastError();

                        // Disconnecting socket.
                        if (Disconnect(sd) != 0)
                            FinishDisconnect(sd);

                        continue;
                    }

                    continue;
                }

                // Checking type of operation.
                switch (sd->type_of_network_oper())
                {
                    // ACCEPT finished.
                    case ACCEPT_OPER:
                    {
                        errCode = FinishAccept(sd, numBytes);
                        GW_ERR_CHECK(errCode);

                        break;
                    }

                    // CONNECT finished.
                    case CONNECT_OPER:
                    {
                        errCode = FinishConnect(sd);
                        GW_ERR_CHECK(errCode);

                        break;
                    }

                    // DISCONNECT finished.
                    case DISCONNECT_OPER:
                    {
                        errCode = FinishDisconnect(sd);
                        GW_ERR_CHECK(errCode);

                        break;
                    }

                    // SEND finished.
                    case SEND_OPER:
                    {
                        errCode = FinishSend(sd, numBytes);
                        GW_ERR_CHECK(errCode);

                        break;
                    }

                    // RECEIVE finished.
                    case RECEIVE_OPER:
                    {
                        errCode = FinishReceive(sd, numBytes);
                        GW_ERR_CHECK(errCode);

                        break;
                    }

                    // Unknown operation.
                    default:
                    {
#ifdef GW_ERRORS_DIAG
                        GW_PRINT_WORKER << "Unknown completed IOCP operation: " << sd->type_of_network_oper() << std::endl;
#endif
                        return 1;
                    }
                }
            }
        }
        else
        {
            errCode = WSAGetLastError();

            // Checking if it was an APC event.
            if (STATUS_USER_APC != errCode &&
                STATUS_TIMEOUT != errCode)
            {
                PrintLastError();
                return errCode;
            }
        }

        // Scanning all channels.
        found_something = false;
        errCode = ScanChannels(&found_something);
        GW_ERR_CHECK(errCode);

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

    return 1;
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

#ifdef GW_DATABASES_DIAG
                GW_PRINT_WORKER << "Deleting shared memory for db slot: " << GetDatabase(i)->db_slot_index() << std::endl;
#endif

                // Finally completely deleting database object and closing shared memory.
                DeleteInactiveDatabase(i);

                // Leaving global lock.
                LeaveGlobalLock();
            }
        }
    }

    return 0;
}

// Creates the socket data structure.
SocketDataChunk *GatewayWorker::CreateSocketData(
    SOCKET sock,
    int32_t port_index,
    int32_t db_index)
{
    // Getting active database.
    WorkerDbInterface *db = active_dbs_[db_index];
    if (NULL == db)
        return NULL;

    // Pop chunk index from private chunk pool.
    core::chunk_index chunk_index;
    shared_memory_chunk *smc;
    uint32_t err_code = db->GetOneChunkFromPrivatePool(&chunk_index, &smc);
    if (err_code)
        return NULL;

    // Allocating socket data inside chunk.
    SocketDataChunk *socket_data = (SocketDataChunk *)((uint8_t*)smc + bmx::BMX_HEADER_MAX_SIZE_BYTES);

    // Initializing socket data.
    socket_data->Init(sock, port_index, db_index, chunk_index);

    // Configuring data buffer.
    socket_data->get_accum_buf()->Init(SOCKET_DATA_BLOB_SIZE_BYTES, socket_data->data_blob(), true);

    // Returning created accumulative buffer.
    return socket_data;
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
    if (NULL != db)
        return db->PushSocketDataToDb(this, sd, handler_id);

    return 1;
}

// Deleting inactive database.
void GatewayWorker::DeleteInactiveDatabase(int32_t db_index)
{
    delete active_dbs_[db_index];
    active_dbs_[db_index] = NULL;
}

} // namespace network
} // namespace starcounter
