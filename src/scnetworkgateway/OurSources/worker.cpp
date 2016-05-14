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

    // Allocating data for sockets infos.
    sockets_infos_ = (ScSocketInfoStruct*) GwNewAligned(sizeof(ScSocketInfoStruct) * g_gateway.setting_max_connections_per_worker());

    // Cleaning all socket infos and setting indexes.
    for (socket_index_type i = g_gateway.setting_max_connections_per_worker() - 1; i >= 0; i--)
    {
        // Resetting all sockets infos.
        sockets_infos_[i].Reset();
        sockets_infos_[i].read_only_index_ = i;

        // Pushing to free indexes list.
        free_sockets_infos_.PushBack(i);
    }

    // Creating IO completion port.
    worker_iocp_ = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
    GW_ASSERT(worker_iocp_ != NULL);
    if (worker_iocp_ == NULL)
    {
        GW_PRINT_WORKER << "Failed to create worker IOCP." << GW_ENDL;
        return PrintLastError();
    }

    rebalance_accept_sockets_ = (PSLIST_HEADER) GwNewAligned(sizeof(SLIST_HEADER));
    GW_ASSERT(rebalance_accept_sockets_);
    InitializeSListHead(rebalance_accept_sockets_);

    worker_stats_bytes_received_ = 0;
    worker_stats_bytes_sent_ = 0;
    worker_stats_sent_num_ = 0;
    worker_stats_recv_num_ = 0;

    for (int32_t i = 0; i < MAX_ACTIVE_DATABASES; i++)
        worker_dbs_[i] = NULL;

    // Creating random generator with current time seed.
    rand_gen_ = GwNewConstructor1(random_generator, timeGetTime());

    aggr_timer_ = timeGetTime();

    return 0;
}

// Allocates a new socket based on existing.
uint32_t GatewayWorker::CreateProxySocket(SocketDataChunkRef sd, MixedCodeConstants::NetworkProtocolType protocol_type)
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

    // Trying to set a SIO_LOOPBACK_FAST_PATH on a TCP socket.
    SetLoopbackFastPathOnTcpSocket(new_connect_socket);

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

    // Associating new socket with current worker IOCP.
    HANDLE iocp_handler = CreateIoCompletionPort((HANDLE) new_connect_socket, worker_iocp_, 0, 0);
    if (iocp_handler != worker_iocp_) {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Can't do socket CreateIoCompletionPort." << GW_ENDL;
#endif
        closesocket(new_connect_socket);

        return PrintLastError();
    }

    // Getting new socket index.
    port_index_type port_index = sd->GetPortIndex();
    socket_index_type proxied_socket_info_index = ObtainFreeSocketIndex(new_connect_socket, port_index, protocol_type, true);

    // Checking if we can't obtain new socket index.
    if (INVALID_SOCKET_INDEX == proxied_socket_info_index) {

        closesocket(new_connect_socket);

        return SCERRGWCANTOBTAINFREESOCKETINDEX;
    }

    // Setting proxy sockets indexes.
    socket_index_type orig_socket_info_index = sd->get_socket_info_index();
    sd->SetProxySocketIndex(proxied_socket_info_index);
    sd->set_socket_info_index(this, proxied_socket_info_index);
    sd->SetProxySocketIndex(orig_socket_info_index);
    sd->set_unique_socket_id(GetUniqueSocketId(proxied_socket_info_index));

    return 0;
}

// Releases used socket index.
void GatewayWorker::ReleaseSocketIndex(socket_index_type socket_index)
{
    // Checking that this socket info wasn't returned before.
    GW_ASSERT(!sockets_infos_[socket_index].IsReset());
    //GW_ASSERT(sockets_infos_[socket_index].session_.gw_worker_id_ == worker_id_);

    sockets_infos_[socket_index].Reset();

    // Pushing to free indexes list.
    free_sockets_infos_.PushBack(socket_index);
}

// Gets free socket index.
socket_index_type GatewayWorker::ObtainFreeSocketIndex(
    SOCKET s,
    port_index_type port_index,
    MixedCodeConstants::NetworkProtocolType protocol_type,
    bool proxy_connect_socket)
{
    // Checking if free socket indexes exist.
    if (free_sockets_infos_.get_num_entries() <= 0)
        return INVALID_SOCKET_INDEX;

    socket_index_type free_socket_index = free_sockets_infos_.PopBack();

    ScSocketInfoStruct* si = sockets_infos_ + free_socket_index;

    GW_ASSERT(si->IsReset());

    // Marking socket as alive.
    si->socket_ = s;
    si->type_of_network_protocol_ = protocol_type;

    // Checking if this socket is used for connecting to remote machine.
    if (proxy_connect_socket)
        si->set_socket_proxy_connect_flag();

    // Creating new socket info.
    CreateNewSocketInfo(si->read_only_index_, port_index, get_worker_id());

    // Creating unique ids.
    GenerateUniqueSocketInfoIds(si->read_only_index_);

    return si->read_only_index_;
}

// Applying session parameters to socket data.
bool GatewayWorker::ApplySocketInfoToSocketData(
    SocketDataChunkRef sd,
    socket_index_type socket_index,
    random_salt_type unique_socket_id)
{
    GW_ASSERT(socket_index < g_gateway.setting_max_connections_per_worker());

    if (sd->CompareUniqueSocketId())
    {
        ScSocketInfoStruct si = sockets_infos_[socket_index];

        sd->AssignSession(si.session_);
        sd->set_unique_socket_id(si.unique_socket_id_);
        sd->set_socket_info_index(this, si.read_only_index_);
        sd->set_type_of_network_protocol((MixedCodeConstants::NetworkProtocolType)si.type_of_network_protocol_);

        // Resetting the session based on protocol.
        sd->ResetSessionBasedOnProtocol(this);

        return true;
    }

    return false;
}

// Collects outdated sockets if any.
uint32_t GatewayWorker::DisonnectCodehostSockets(db_index_type db_index)
{
	int32_t num_checked = 0;
	int64_t num_active_sockets = g_gateway.NumberOfActiveSocketsOnAllPortsForWorker(worker_id_);

	for (socket_index_type i = 0; i < g_gateway.setting_max_connections_per_worker(); i++)
	{
		ScSocketInfoStruct* si = sockets_infos_ + i;

		// Checking that socket is alive.
		if ((!si->IsReset()) && (INVALID_SOCKET != si->get_socket())) {

			// Checking if database is the same.
			if (db_index == si->get_dest_db_index()) {

				// Updating unique socket id.
				GenerateUniqueSocketInfoIds(i);

				// Disconnecting outdated socket.
				si->DisconnectSocket();
			}

			// Increasing number of checked sockets.
			num_checked++;
		}

		// Checking if we have checked all active sockets.
		if (num_checked >= num_active_sockets) {
			break;
		}
	}

	// Releasing database index.
	g_gateway.GetDatabase(db_index)->ReleaseHoldingWorker();

	return 0;
}

// Collects outdated sockets if any.
uint32_t GatewayWorker::CollectInactiveSockets()
{
    int32_t num_inactive = 0;

    // TODO: Optimize scanning range.
    for (socket_index_type i = 0; i < g_gateway.setting_max_connections_per_worker(); i++)
    {
        ScSocketInfoStruct* si = sockets_infos_ + i;

        if ((worker_id_ == si->session_.gw_worker_id_) &&
            (!si->IsReset()) &&
            (INVALID_SOCKET != si->get_socket())) {

                num_inactive++;
        } else {
            continue;
        }

        // Checking if socket touch time is older than inactive socket timeout.
        if ((si->socket_timestamp_) &&
            (g_gateway.get_global_timer_unsafe() - si->socket_timestamp_) >= g_gateway.setting_inactive_socket_timeout_seconds())
        {
            ServerPort* sp = g_gateway.get_server_port(si->port_index_);
            if (sp->IsEmpty() || (sp->get_port_number() == g_gateway.get_setting_internal_system_port())) {
                continue;
            }

            // Disconnecting socket.
            switch (si->type_of_network_protocol_)
            {
                case MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1: {

                    // Updating unique socket id.
                    GenerateUniqueSocketInfoIds(i);

                    // Disconnecting outdated socket.
                    si->DisconnectSocket();

                    break;
                }
            }
        }

        // Checking if we have checked all active sockets.
        if (num_inactive >= g_gateway.NumberOfActiveSocketsOnAllPortsForWorker(worker_id_))
            break;
    }

    return 0;
}

// Used to create new UDP sockets when reaching the limit.
uint32_t GatewayWorker::CreateUdpSockets(port_index_type port_index) {

    uint32_t err_code;
    SOCKET new_socket;

    // Creating new socket.
    new_socket = WSASocket(AF_INET, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, WSA_FLAG_OVERLAPPED);
    if (new_socket == INVALID_SOCKET)
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "WSASocket() failed." << GW_ENDL;
#endif
        return PrintLastError();
    }

    // Setting needed socket options.
    int32_t on_flag = TRUE;

    // Reusing socket address.
    if (setsockopt(new_socket, SOL_SOCKET, SO_REUSEADDR, (char *)&on_flag, sizeof(on_flag)))
    {
        GW_PRINT_WORKER << "Can't set SO_REUSEADDR on socket." << GW_ENDL;

        closesocket(new_socket);

        return PrintLastError();
    }

    // Disabling WSAECONNRESET.
    uint32_t ul = 0, temp;
    if (WSAIoctl(new_socket, SIO_UDP_CONNRESET, &ul, sizeof(ul), NULL, 0, (LPDWORD)&temp, NULL, NULL))
    {
        GW_PRINT_WORKER << "Can't set SIO_UDP_CONNRESET on socket." << GW_ENDL;

        closesocket(new_socket);

        return PrintLastError();
    }

    // Getting port number.
    ServerPort* sp = g_gateway.get_server_port(port_index);
    uint16_t port = sp->get_port_number();

    // The socket address to be passed to bind.
    sockaddr_in binding_addr;
    memset(&binding_addr, 0, sizeof(sockaddr_in));
    binding_addr.sin_family = AF_INET;
    binding_addr.sin_addr.s_addr = INADDR_ANY;
    binding_addr.sin_port = htons(port);

    // Binding socket to certain interface and port.
    if (bind(new_socket, (SOCKADDR *) &binding_addr, sizeof(binding_addr)))
    {
        GW_PRINT_WORKER << "Can't bind UDP socket." << GW_ENDL;

        closesocket(new_socket);

        return PrintLastError();
    }

    // Creating new socket data structure inside chunk.
    SocketDataChunk* new_sd = NULL;

    // Getting new socket index.
    socket_index_type new_socket_index = ObtainFreeSocketIndex(
        new_socket,
        port_index,
        MixedCodeConstants::NetworkProtocolType::PROTOCOL_UDP,
        false);

    // Checking if we can't obtain new socket index.
    if (INVALID_SOCKET_INDEX == new_socket_index) {

        closesocket(new_socket);

        return SCERRGWCANTOBTAINFREESOCKETINDEX;
    }

    // Creating new socket data.
    err_code = CreateSocketData(new_socket_index, new_sd, MAX_UDP_DATAGRAM_SIZE);

    if (err_code)
    {
        closesocket(new_socket);

        return err_code;
    }

    // Setting UDP network protocol.
    new_sd->SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_UDP);

    // Adding to ready UDP sockets.
    sp->PushToReadyUdpSockets(worker_id_, new_socket_index);

    // Associating new socket with least busy worker IOCP.
    HANDLE iocp_handler = CreateIoCompletionPort((HANDLE) new_socket, worker_iocp_, 0, 0);
    GW_ASSERT(iocp_handler == worker_iocp_);

    // Setting unique socket id.
    new_sd->GenerateUniqueSocketInfoIds(this);

    // This socket data is socket representation.
    new_sd->set_socket_representer_flag();

    // Immediately receiving on this UDP socket.
    err_code = Receive(new_sd);

    if (err_code) {
        closesocket(new_socket);

        return err_code;
    }

    return 0;
}

// Allocates a bunch of new connections.
uint32_t GatewayWorker::CreateAcceptingSockets(port_index_type port_index)
{
    GW_ASSERT(0 == worker_id_);

    int32_t how_many_sockets_to_accept = ACCEPT_ROOF_STEP_SIZE;
    ServerPort* sp = g_gateway.get_server_port(port_index);
    GW_ASSERT(NULL != sp);

    // Checking if this is an aggregation port, then one accepting socket is enough.
    if (sp->get_aggregating_flag())
        how_many_sockets_to_accept = 1;

    // Checking if we have not enough accepting sockets.
    if (sp->get_num_accepting_sockets() >= how_many_sockets_to_accept)
        return 0;

    uint32_t err_code;
    int32_t curIntNum = 0;

    for (int32_t i = 0; i < how_many_sockets_to_accept; i++)
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

        // Trying to set a SIO_LOOPBACK_FAST_PATH on a TCP socket.
        SetLoopbackFastPathOnTcpSocket(new_socket);
        
        // Setting needed socket options.
        int32_t on_flag = 1;

        // Disables the Nagle algorithm for send coalescing.
        if (setsockopt(new_socket, IPPROTO_TCP, TCP_NODELAY, (char *)&on_flag, sizeof(on_flag)))
        {
            GW_PRINT_WORKER << "Can't set TCP_NODELAY on socket." << GW_ENDL;

            closesocket(new_socket);

            return PrintLastError();
        }

#ifdef GW_IOCP_IMMEDIATE_COMPLETION
        // Skipping completion port if operation is already successful.
        SetFileCompletionNotificationModes((HANDLE)new_socket, FILE_SKIP_COMPLETION_PORT_ON_SUCCESS);
#endif
        // Putting socket into non-blocking mode.
        uint32_t ul = 1, temp;
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
        
        // Getting new socket index.
        socket_index_type new_socket_index = ObtainFreeSocketIndex(
            new_socket,
            port_index,
            MixedCodeConstants::NetworkProtocolType::PROTOCOL_UNKNOWN,
            false);

        // Checking if we can't obtain new socket index.
        if (INVALID_SOCKET_INDEX == new_socket_index) {

            closesocket(new_socket);

            return SCERRGWCANTOBTAINFREESOCKETINDEX;
        }

        // Creating new socket data.
        err_code = CreateSocketData(new_socket_index, new_sd);

        if (err_code)
        {
            closesocket(new_socket);

            return err_code;
        }

        // Performing accept.
        err_code = Accept(new_sd);

        // NOTE: If we have an error accepting on socket, we simply returning chunk to pool.
        if (err_code) {

            // Resetting socket representer flags.
            new_sd->reset_socket_representer_flag();

            // Returning chunks to pool.
            ReturnSocketDataChunksToPool(new_sd);
        }
    }

    return 0;
}

// Running receive on socket data.
uint32_t GatewayWorker::Receive(SocketDataChunkRef sd)
{
    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

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

    uint32_t numBytes, err_code;

    // Checking if its a UDP socket.
    if (sd->IsUdp()) {

        // Resetting buffer so we can receive again.
        sd->ResetAccumBuffer();

        err_code = sd->ReceiveUdp(this, &numBytes);

    } else {

        err_code = sd->ReceiveTcp(this, &numBytes);
    }

    // Checking if operation completed immediately.
    if (0 != err_code)
    {
        int32_t wsa_err_code = WSAGetLastError();

        // Checking if IOCP event was scheduled.
        if (WSA_IO_PENDING != wsa_err_code)
        {
#ifdef GW_WARNINGS_DIAG
            GW_PRINT_WORKER << "Failed receive: socket " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << ". Disconnecting socket..." << GW_ENDL;

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

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    // Checking that socket arrived on correct worker.
    GW_ASSERT(sd->get_bound_worker_id() == worker_id_);
    GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

    // If we received 0 bytes, the remote side has close the connection.
    if (0 == num_bytes_received)
    {
#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Zero-bytes receive on socket index: " << sd->get_socket_info_index() << ". Remote side closed the connection." << GW_ENDL;
#endif

        return SCERRGWSOCKETCLOSEDBYPEER;
    }

    // Updating connection timestamp.
    sd->UpdateSocketTimeStamp();

    // Adding to accumulated bytes.
    sd->AddAccumulatedBytes(num_bytes_received);

    // Incrementing statistics.
    worker_stats_bytes_received_ += num_bytes_received;

    // Increasing number of receives.
    worker_stats_recv_num_++;

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
        sd->ExchangeToProxySocket(this);

        // Setting number of bytes to send.
        sd->PrepareToSendOnProxy();

        // Sending data to user.
        return Send(sd);
    }

    // Assigning last received bytes.
    if (sd->get_accumulating_flag())
    {
        // Checking if we have not accumulated everything yet.
        if (!sd->IsNetworkBufferFull())
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

	} else {
	
		// Resetting the session based on protocol.
		sd->ResetSessionBasedOnProtocol(this);
	}

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

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    // Checking that socket arrived on correct worker.
    GW_ASSERT(sd->get_bound_worker_id() == worker_id_);
    GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

    // Checking if aggregation is involved.
    if (sd->GetSocketAggregatedFlag())
    {
        // Increasing number of sends.
        worker_stats_sent_num_++;
        g_gateway.num_aggregated_send_queued_messages_++;

        return SendOnAggregationSocket(sd, MixedCodeConstants::AggregationMessageTypes::AGGR_DATA);
    }

    // Start sending on socket.
    uint32_t num_sent_bytes, err_code;

    // Checking if its a UDP socket.
    if (sd->IsUdp()) {
        
        err_code = sd->SendUdp(this, &num_sent_bytes);

    } else {

        err_code = sd->SendTcp(this, &num_sent_bytes);
    }

    // Checking if operation completed immediately.
    if (0 != err_code)
    {
        int32_t wsa_err_code = WSAGetLastError();

        // Checking if IOCP event was scheduled.
        if (WSA_IO_PENDING != wsa_err_code)
        {
#ifdef GW_WARNINGS_DIAG
            GW_PRINT_WORKER << "Failed send: socket " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << ". Disconnecting socket..." << GW_ENDL;

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

// Do internal HTTP request.
uint32_t GatewayWorker::DoInternalHttpRequest(SocketDataChunkRef sd, const char* http_request_data, const int32_t request_data_size) {

	SocketDataChunk* sd_new = NULL;
	uint32_t err_code = sd->CloneToPush(this, request_data_size, &sd_new);
	if (err_code)
		return err_code;

	// Resetting all flags and setting internal request flag.
	sd_new->ResetAllFlags();
	sd_new->set_internal_request_flag();

	// Resetting the buffer for new data.
	sd_new->ResetAccumBuffer();
	sd_new->AddAccumulatedBytes(request_data_size);

	// Copying HTTP data.
	memcpy(sd_new->get_data_blob_start(), http_request_data, request_data_size);

	// Running the handler.
	err_code = RunReceiveHandlers(sd_new);

	if (err_code) {
		// Returning chunks to pool.
		ReturnSocketDataChunksToPool(sd_new);
	}

	return err_code;
}

// Socket send finished.
__forceinline uint32_t GatewayWorker::FinishSend(SocketDataChunkRef sd, int32_t num_bytes_sent)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishSend: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId())
        return SCERRGWOPERATIONONWRONGSOCKET;

    // Checking that socket data is valid.
    sd->CheckForValidity();

    // Checking that socket arrived on correct worker.
    GW_ASSERT(sd->get_bound_worker_id() == worker_id_);
    GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

    // Checking disconnect state.
    if (sd->get_disconnect_after_send_flag()) {

		// NOTE: We can't shutdown the socket because the last
		// message is not delivered even we do SHUTDOWN with blocking
		// either receives or sends. So instead we wait until client
		// closes the socket or inactive socket timeout is reached.

		//sd->get_socket_info()->ShutdownSend();
		//sd->get_socket_info()->ShutdownReceive(); 

		// Checking if its a WebSocket protocol.
		if (sd->is_web_socket())
			return SCERRGWDISCONNECTFLAG;
    }

    // If we received 0 bytes, the remote side has close the connection.
    if (0 == num_bytes_sent)
    {
#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Zero-bytes sent on socket index: " << sd->get_socket_info_index() << ". Remote side closed the connection." << GW_ENDL;
#endif

        return SCERRGWSOCKETCLOSEDBYPEER;
    }

    // Checking that we processed correct number of bytes.
    GW_ASSERT(num_bytes_sent == sd->get_num_available_network_bytes());

    // Updating connection timestamp.
    sd->UpdateSocketTimeStamp();

    // Incrementing statistics.
    worker_stats_bytes_sent_ += num_bytes_sent;

    // Increasing number of sends.
    worker_stats_sent_num_++;

	// Checking if we have streaming response.
	if (sd->GetStreamingResponseBodyFlag()) {

		uint16_t port_num = sd->GetPortNumber();
		std::string port_num_s = std::to_string(port_num);
		std::string s = kHttpGetFinishSend;
		s.replace(kHttpGetFinishSendPortOffset, 1, port_num_s);

		DoInternalHttpRequest(sd, s.c_str(), (int32_t) s.length());
	}

    // Checking if socket data is for receiving.
    if (sd->get_socket_representer_flag())
    {
        // Resets data buffer offset.
        sd->SetUserData(sd->get_data_blob_start(), sd->get_accumulated_len_bytes());

        // Resetting buffer information.
        sd->ResetAccumBuffer();

        // Resetting safe flags.
        sd->ResetSafeFlags();

        // Performing receive.
        return Receive(sd);
    }
    
    // Returning chunks to pool.
    ReturnSocketDataChunksToPool(sd);

    g_gateway.num_pending_sends_--;

    return 0;
}

// Returns given socket data chunk to private chunk pool.
void GatewayWorker::ReturnSocketDataChunksToPool(SocketDataChunkRef sd)
{
    GW_ASSERT(NULL != sd);

    GW_ASSERT(false == sd->get_socket_representer_flag());

#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Returning chunk to pool: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    worker_chunks_.ReleaseChunk(sd);

    // IMPORTANT: Preventing further usages of this socket data.
    sd = NULL;
}

// Initiates receive on arbitrary socket.
uint32_t GatewayWorker::ReceiveOnSocket(socket_index_type socket_index)
{
    // Getting existing socket info copy.
    ScSocketInfoStruct global_socket_info_copy = GetGlobalSocketInfoCopy(socket_index);

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

// Send disconnect to database.
uint32_t GatewayWorker::SendTcpSocketDisconnectToDb(SocketDataChunk* sd)
{
    SocketDataChunk* sd_push_to_db = NULL;
    uint32_t err_code = sd->CloneToPush(this, sd->get_accumulated_len_bytes(), &sd_push_to_db);
    if (err_code)
        return err_code;

    sd_push_to_db->ResetAllFlags();

    sd_push_to_db->set_just_push_disconnect_flag();

    // NOTE: There is no used data when disconnecting.
	sd_push_to_db->SetUserData(sd_push_to_db->get_data_blob_start(), 0);

    // Searching for server port and corresponding raw port handler.
    ServerPort* sp = g_gateway.get_server_port(sd->GetPortIndex());

    if ((NULL != sp) && (!sp->IsEmpty())) {
        
        bool is_handled = false;

        HandlersList* ph = sp->get_port_handlers();

        if ((ph != NULL) && (!ph->IsEmpty())) {

			// Push chunk to corresponding channel/scheduler.
			err_code = PushSocketDataToDb(sd_push_to_db, ph->get_handler_info());

			if (err_code) {

				// Releasing the cloned chunk.
				ReturnSocketDataChunksToPool(sd_push_to_db);

				return err_code;
			}
        }        
    }

    // Checking if we were not able to push.
    if (NULL != sd_push_to_db) {

        // Releasing the cloned chunk.
        ReturnSocketDataChunksToPool(sd_push_to_db);
    }

    return 0;
}

// Pushes disconnect message to host if needed.
void GatewayWorker::PushDisconnectToCodehost(SocketDataChunkRef sd) {

	// Checking if we already pushed the disconnect to codehost.
	if (sd->get_socket_info()->get_disconnect_pushed_to_codehost_flag())
		return;

	// Setting flag to send disconnect to codehost.
	sd->get_socket_info()->set_disconnect_pushed_to_codehost_flag();

	// Checking if we have streaming response.
	if (sd->GetStreamingResponseBodyFlag()) {

		uint16_t port_num = sd->GetPortNumber();
		std::string port_num_s = std::to_string(port_num);
		std::string s = kHttpDeleteStream;
		s.replace(kHttpDeleteStreamPortOffset, 1, port_num_s);

		DoInternalHttpRequest(sd, s.c_str(), (int32_t) s.length());
	}

    // Processing session according to protocol.
    switch (sd->GetTypeOfNetworkProtocol()) {

        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS: {
            // NOTE: Ignoring the error code.
            WsProto::SendWebSocketDisconnectToDb(this, sd);
            break;
        }

        case MixedCodeConstants::NetworkProtocolType::PROTOCOL_TCP: {
            SendTcpSocketDisconnectToDb(sd);
            break;
        }
    }
}

// Running disconnect on socket data.
// NOTE: Socket data chunk can not be used after this function is called!
void GatewayWorker::DisconnectAndReleaseChunk(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Disconnect: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    GW_ASSERT(NULL != sd);

    // Checking that socket data is valid.
    sd->CheckForValidity();

    // Checking if we have a UDP socket.
    if (sd->IsUdp()) {

        // Checking if its a socket representer.
        if (!sd->get_socket_representer_flag()) {

            goto RELEASE_CHUNK_TO_POOL;

        } else {

            // Continue receiving.
            uint32_t err_code = Receive(sd);
            GW_ASSERT(0 == err_code);

            return;
        }
    }

    // Checking if its a socket representer.
    if (!sd->get_socket_representer_flag()) {

        // Checking correct unique socket.
        if (!sd->CompareUniqueSocketId())
            goto RELEASE_CHUNK_TO_POOL;

        // Setting unique socket id.
        GenerateUniqueSocketInfoIds(sd->get_socket_info_index());

        // Aggregated sockets are treated specifically.
        if (sd->GetSocketAggregatedFlag())
            goto RELEASE_CHUNK_TO_POOL;

        // Disconnecting socket handle and invalidate it.
        sd->DisconnectSocket();

        goto RELEASE_CHUNK_TO_POOL;
    }

    uint32_t err_code = 0;

    // Checking correct unique socket.
    if (sd->CompareUniqueSocketId()) {

        // Setting unique socket id.
        GenerateUniqueSocketInfoIds(sd->get_socket_info_index());

        // Aggregated sockets are treated specifically.
        if (sd->GetSocketAggregatedFlag())
            goto RELEASE_CHUNK_TO_POOL;

        // Disconnecting socket handle and invalidate it.
        sd->DisconnectSocket();

        // NOTE: Finishing disconnect operation here since we don't schedule another IOCP operation.
        err_code = FinishDisconnect(sd);
        GW_ASSERT(0 == err_code);

    } else {

        // NOTE: Finishing disconnect operation here since we don't schedule another IOCP operation.
        err_code = FinishDisconnect(sd);
        GW_ASSERT(0 == err_code);
    }

    // Checking that socket data was released.
    GW_ASSERT(NULL == sd);

    // The disconnect operation is pending.
    return;

    // Returning the chunk to pool.
RELEASE_CHUNK_TO_POOL:

    // Resetting socket representer flags.
    sd->reset_socket_representer_flag();

    // Returning chunks to pool.
    ReturnSocketDataChunksToPool(sd);
}

// Socket disconnect finished.
__forceinline uint32_t GatewayWorker::FinishDisconnect(SocketDataChunkRef sd)
{
#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "FinishDisconnect: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // NOTE: Since we are here means that this socket data represents this socket.
    GW_ASSERT(sd->get_socket_representer_flag());
    GW_ASSERT(sd->get_type_of_network_oper() != UNKNOWN_SOCKET_OPER);

	// Pushing disconnect message to host if needed.
	PushDisconnectToCodehost(sd);

    // Deleting session.
    sd->ResetGlobalSession();

    // Checking if it was an accepting socket.
    if (ACCEPT_SOCKET_OPER == sd->get_type_of_network_oper()) {
        ChangeNumAcceptingSockets(sd->GetPortIndex(), -1);
    }

    // Releasing socket resources only there weren't yet released.
    if (!sd->IsInvalidSocket()) {

        // Making sure that socket data is unique.
        GW_ASSERT(true == sd->CompareUniqueSocketId());

        // Disconnecting socket handle and invalidate it.
        sd->DisconnectSocket();
    }

    // Removing from active sockets.
    RemoveFromActiveSockets(sd->GetPortIndex());
    
    // Resetting the socket data.
    sd->ResetWhenDisconnectIsDone(this);

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

    // This socket data is socket representation.
    sd->set_socket_representer_flag();

    // Adding to active sockets for this worker.
    AddToActiveSockets(sd->GetPortIndex());

    while(TRUE)
    {
        // Start connecting socket.

        // Setting unique socket id.
        sd->GenerateUniqueSocketInfoIds(this);

        // Calling ConnectEx.
        uint32_t err_code = sd->Connect(this, server_addr);

        // Checking if operation completed immediately.
        GW_ASSERT(TRUE != err_code);

        int32_t wsa_err_code = WSAGetLastError();

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

    // Setting SO_UPDATE_CONNECT_CONTEXT.
    if (setsockopt(sd->GetSocket(), SOL_SOCKET, SO_UPDATE_CONNECT_CONTEXT, NULL, 0))
    {
        GW_PRINT_WORKER << "Can't set SO_UPDATE_CONNECT_CONTEXT on socket." << GW_ENDL;
        return SCERRGWCONNECTEXFAILED;
    }

    // Since we are proxying this instance represents the socket.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    sd->set_type_of_network_oper(UNKNOWN_SOCKET_OPER);

    // Checking if we are in proxy mode.
    GW_ASSERT(sd->HasProxySocket() == true);

    // Resuming receive on initial socket.
    uint32_t err_code = ReceiveOnSocket(sd->GetProxySocketIndex());
    if (err_code)
        return err_code;

    // Sending to proxied server.
    return Send(sd);
}

// Running accept on socket data.
uint32_t GatewayWorker::Accept(SocketDataChunkRef sd)
{
    GW_ASSERT(0 == worker_id_);

#ifdef GW_SOCKET_DIAG
    GW_PRINT_WORKER << "Accept: socket index " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;
#endif

    // Setting unique socket id.
    sd->GenerateUniqueSocketInfoIds(this);

    // This socket data is socket representation.
    sd->set_socket_representer_flag();

    port_index_type port_index = sd->GetPortIndex();

    // Adding to active sockets for this worker.
    AddToActiveSockets(port_index);

    // Calling AcceptEx.
    uint32_t err_code = sd->Accept(this);

    // Checking if operation completed immediately.
    GW_ASSERT(TRUE != err_code);

    int32_t wsa_err_code = WSAGetLastError();

    // Checking if IOCP event was scheduled.
    if (WSA_IO_PENDING != wsa_err_code)
    {
#ifdef GW_WARNINGS_DIAG
        GW_PRINT_WORKER << "Failed AcceptEx: " << sd->get_socket_info_index() << ":" << sd->GetSocket() << ":" << sd->get_unique_socket_id() << ":" << (uint64_t)sd << GW_ENDL;

        PrintLastError();
#endif

        return SCERRGWFAILEDACCEPTEX;
    }

    // Updating number of accepting sockets.
    ChangeNumAcceptingSockets(port_index, 1);

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

    // Checking that its not a UDP protocol.
    GW_ASSERT(!sd->IsUdp());

    // Checking correct unique socket.
    GW_ASSERT(true == sd->CompareUniqueSocketId());

    // Checking if was rebalanced.
    if (0 == worker_id_) {

        // Checking client IP address information.
        sockaddr_in client_addr = *(sockaddr_in *)(sd->get_accept_or_params_data() + sizeof(sockaddr_in) + 16);
        sd->set_client_ip_info(client_addr.sin_addr.S_un.S_addr);

        // Checking if white list is on.
        if (!g_gateway.CheckIpForWhiteList(sd->get_client_ip_info()))
            return SCERRGWIPISNOTONWHITELIST;

        // Port index for the corresponding socket.
        port_index_type port_index = sd->GetPortIndex();

        // Getting least used worker.
        worker_id_type least_busy_worker_id = GetLeastBusyWorkerId(port_index);

        // Decreasing number of accepting sockets.
        int64_t cur_num_accept_sockets = ChangeNumAcceptingSockets(port_index, -1);

        // Creating new set of prepared connections.
        // NOTE: Ignoring error code on purpose.
        CreateAcceptingSockets(port_index);

        // NOTE: All handlers must be registered on worker 0.
        if (sd->GetPortNumber() == g_gateway.get_setting_internal_system_port())
            least_busy_worker_id = 0;

        // Checking if rebalanced worker is different.
        if (0 != least_busy_worker_id) {

            // Decreasing number of active sockets on worker 0.
            RemoveFromActiveSockets(port_index);

            // Adding to active sockets of the other worker.
            ServerPort* sp = g_gateway.get_server_port(port_index);
            sp->AddToActiveSockets(least_busy_worker_id);

            // Getting temporary rebalance container.
            RebalancedSocketInfo* rsi = PopRebalanceSocketInfo();
            if (NULL == rsi) {
                rsi = (RebalancedSocketInfo*) GwNewAligned(sizeof(RebalancedSocketInfo));
            }
            rsi->Init(port_index, sd->GetSocket(), sd->get_client_ip_info());

            // Returning all allocated resources except socket.
            sd->ResetAllFlags();
            ReleaseSocketIndex(sd->get_socket_info_index());
            ReturnSocketDataChunksToPool(sd);

            g_gateway.get_worker(least_busy_worker_id)->PushRebalanceSocketInfo(rsi);

            // Posting this finish Accept on a new worker IOCP.
            g_gateway.SendRebalanceAPC(least_busy_worker_id);

            return 0;

        } else {
            // Associating new socket with least busy worker IOCP.
            HANDLE iocp_handler = CreateIoCompletionPort((HANDLE) sd->GetSocket(), worker_iocp_, 0, 0);
            GW_ASSERT(iocp_handler == worker_iocp_);
        }
    }

    // Updating connection timestamp.
    sd->UpdateSocketTimeStamp();

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    uint32_t err_code = sd->SetAcceptSocketOptions(this);
    if (0 != err_code) {
        return SCERRGWCONNECTEXFAILED;
    }

    // This socket data is socket representation.
    GW_ASSERT(true == sd->get_socket_representer_flag());

    // Performing receive.
    return Receive(sd);
}

// Checks if cloning was performed and does operations.
uint32_t GatewayWorker::ProcessReceiveClones(bool just_delete_clone)
{
    // Checking if there was no clone.
    if (sd_receive_clone_ == NULL)
        return 0;

    uint32_t err_code = 0;
    while (sd_receive_clone_ != NULL)
    {
        // NOTE: Taking just a pointer without reference.
        SocketDataChunk* sd = sd_receive_clone_;

		// Resetting flag clone to receive.
		sd_receive_clone_->get_socket_info()->reset_cloned_to_receive_flag();

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

// Processes socket info for aggregation loopback.
void GatewayWorker::LoopbackForAggregation(SocketDataChunkRef sd)
{
    char body[1024];
    int32_t body_len = sd->get_http_proto()->get_http_request()->content_len_bytes_;
    GW_ASSERT(body_len <= 1024);

    memcpy(body, (char*)sd + sd->get_http_proto()->get_http_request()->content_offset_, body_len);

    GW_ASSERT(static_cast<uint32_t>(body_len + kHttp200HeaderLength) < sd->get_data_blob_size());

    // Getting user data pointer.
    uint8_t* dest_data = sd->get_data_blob_start();

    // Copying predefined header.
    memcpy(dest_data, kHttp200Header, kHttp200HeaderLength);

    // Making length a white space.
    *(uint64_t*)(dest_data + kHttp200HeaderInsertPoint) = 0x2020202020202020;

    // Converting content length to string.
    WriteUIntToString((char*)dest_data + kHttp200HeaderInsertPoint, body_len);

    // Copying body to response.
    memcpy(dest_data + kHttp200HeaderLength, body, body_len);

    // We don't need original chunk contents.
    sd->ResetAccumBuffer();
    sd->SetUserData(sd->get_data_blob_start(), kHttp200HeaderLength + body_len);

    // Prepare buffer to send outside.
    sd->AddAccumulatedBytes(kHttp200HeaderLength + body_len);

    // Running the handlers.
    uint32_t err_code = RunFromDbHandlers(sd);

    if (0 != err_code) {
        // Disconnecting this socket data.
        DisconnectAndReleaseChunk(sd);
    }
}

// Processes rebalanced sockets from worker 0.
void GatewayWorker::ProcessRebalancedSockets() {

    GW_ASSERT (0 != worker_id_);

    uint32_t err_code = 0;

    RebalancedSocketInfo* rsi = PopRebalanceSocketInfo();

    if (NULL != rsi) {

        // Getting all saved properties.
        port_index_type pi = rsi->get_port_index();
        SOCKET s = rsi->get_socket();
        ip_info_type client_ip_info = rsi->get_client_ip_info();

        // Returning socket info back to origin.
        g_gateway.get_worker(0)->PushRebalanceSocketInfo(rsi);

        // Associating new socket with least busy worker IOCP.
        HANDLE iocp_handler = CreateIoCompletionPort((HANDLE) s, worker_iocp_, 0, 0);
        GW_ASSERT(iocp_handler == worker_iocp_);

        // Getting new socket index.
        socket_index_type new_socket_index = ObtainFreeSocketIndex(
            s,
            pi,
            MixedCodeConstants::NetworkProtocolType::PROTOCOL_UNKNOWN,
            false);

        // Checking if we can't obtain new socket index.
        if (INVALID_SOCKET_INDEX == new_socket_index) {

            // Just closing the socket.
            closesocket(s);

            return;
        }

        // Creating new socket data structure inside chunk.
        SocketDataChunk* new_sd = NULL;

        // Creating new socket data.
        err_code = CreateSocketData(new_socket_index, new_sd);

        if (err_code) {

            // Releasing socket index.
            ReleaseSocketIndex(new_socket_index);
            new_socket_index = INVALID_SOCKET_INDEX;

            // Just closing the socket.
            closesocket(s);

            return;
        }

        // This socket data is socket representation.
        new_sd->set_socket_representer_flag();

        // Setting saved client IP address.
        new_sd->set_client_ip_info(client_ip_info);

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

// Main gateway worker routine.
uint32_t GatewayWorker::WorkerRoutine()
{
    BOOL compl_status = false;
    OVERLAPPED_ENTRY* fetched_ovls = GwNewArray(OVERLAPPED_ENTRY, MAX_FETCHED_OVLS);
    uint32_t num_fetched_ovls = 0;
    uint32_t err_code = 0;
    uint32_t oper_num_bytes = 0, flags = 0, oldTimeMs = timeGetTime();
    uint32_t next_sleep_interval_ms = 1000;

#ifdef WORKER_NO_SLEEP
    next_sleep_interval_ms = 0;
#endif

    sd_receive_clone_ = NULL;

    // Starting worker loop.
    while (TRUE)
    {
        // Checking if there no work.
        if (0 != next_sleep_interval_ms) {

            // Since we are going to sleep, we need to set notification flag.
            for (int32_t i = 0; i < g_gateway.get_num_dbs_slots(); i++) {

                WorkerDbInterface *db = GetWorkerDb(i);
                if (NULL != db) {
                    db->get_shared_int()->client_interface().set_notify_flag(true);
                }
            }

            // Checking again if we need to be notified.
            err_code = ScanChannels(&next_sleep_interval_ms);
            GW_ASSERT(0 == err_code);

            // If we have some chunks, we need to reset notification.
            if (0 == next_sleep_interval_ms) {

                for (int32_t i = 0; i < g_gateway.get_num_dbs_slots(); i++) {

                    WorkerDbInterface *db = GetWorkerDb(i);
                    if (NULL != db) {
                        db->get_shared_int()->client_interface().set_notify_flag(false);
                    }
                }
            }
        }

        // Getting IOCP status.
        compl_status = GetQueuedCompletionStatusEx(worker_iocp_, fetched_ovls, MAX_FETCHED_OVLS, (PULONG)&num_fetched_ovls, next_sleep_interval_ms, TRUE);

        // Checking if he have slept, then disabling notification.
        if (0 != next_sleep_interval_ms) {

            for (int32_t i = 0; i < g_gateway.get_num_dbs_slots(); i++) {

                WorkerDbInterface *db = GetWorkerDb(i);
                if (NULL != db) {
                    db->get_shared_int()->client_interface().set_notify_flag(false);
                }
            }
        }

        // Check if global lock is set.
		if (g_gateway.is_global_lock_set()) {
			g_gateway.SuspendWorker(this);
		}

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
                GW_ASSERT(sd->get_socket_info_index() < g_gateway.setting_max_connections_per_worker());

                // Checking that Accept can only be performed on worker 0.
                if (sd->get_type_of_network_oper() == ACCEPT_SOCKET_OPER)
                    GW_ASSERT(0 == worker_id_);

                // Checking that socket arrived on correct worker.
                GW_ASSERT(sd->get_bound_worker_id() == worker_id_);

                // Checking correct unique socket.
                if (sd->CompareUniqueSocketId())
                    GW_ASSERT(sd->GetBoundWorkerId() == worker_id_);

                // Checking error code (lower 32-bits of Internal).
                if (ERROR_SUCCESS != (uint32_t) fetched_ovls[i].lpOverlapped->Internal)
                {
                    // Checking correct unique socket.
                    if (sd->get_socket_representer_flag()) {

                        // Checking if its a UDP socket.
                        if (sd->IsUdp()) {

                            // Simply receiving again, if its a UDP socket.
                            err_code = Receive(sd);
                            GW_ASSERT(0 == err_code);
                            
                        } else {

                            // Disconnecting TCP socket.
                            err_code = FinishDisconnect(sd);
                            GW_ASSERT(0 == err_code);
                        }

                    } else {

                        // Returning chunks to pool.
                        ReturnSocketDataChunksToPool(sd);
                    }

                    continue;
                }

                // Checking if socket is still legal to be used.
                if (!sd->CompareUniqueSocketId()) {

                    // Checking that its not a UDP socket.
                    GW_ASSERT(false == sd->IsUdp());

                    // Checking if its a socket representer.
                    if (sd->get_socket_representer_flag()) {

                        err_code = FinishDisconnect(sd);
                        GW_ASSERT(0 == err_code);

                    } else {

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
                        GW_ASSERT(0 == err_code);

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
        else
        {
            err_code = WSAGetLastError();

            // Checking if it was an APC event.
            GW_ASSERT((STATUS_USER_APC == err_code) || (STATUS_TIMEOUT == err_code));
        }

        // Setting gateway to wait 1 second for network and other IOCP events.
        next_sleep_interval_ms = 1000;

        // Checking if we have aggregation.
        if (INVALID_PORT_NUMBER != g_gateway.setting_aggregation_port())
        {
            // Setting timeout on GetQueuedCompletionStatusEx.
            next_sleep_interval_ms = 3;

            // Checking if we have aggregated.
            if ((timeGetTime() - aggr_timer_) >= 3.0)
            {
                // Processing aggregated chunks.
                // NOTE: Do nothing about error codes.
                SendAggregatedChunks();

                // Resetting the timer.
                aggr_timer_ = timeGetTime();
            }
        }

        // Scanning all channels.
        err_code = ScanChannels(&next_sleep_interval_ms);
        GW_ASSERT(0 == err_code);

        // Pushing overflow chunks if any.
        PushOverflowChunks(&next_sleep_interval_ms);

        // Creating accepting sockets on all ports and for all databases.
        // NOTE: Ignoring error code on purpose.
        if (0 == worker_id_) {
            CheckAcceptingSocketsOnAllActivePorts();
        }

#ifdef WORKER_NO_SLEEP
        next_sleep_interval_ms = 0;
#endif

    }

    GW_ASSERT(false);
}

// Creating accepting sockets on all ports.
void GatewayWorker::CheckAcceptingSocketsOnAllActivePorts()
{
    for (port_index_type p = 0; p < g_gateway.get_num_server_ports_slots(); p++)
    {
        ServerPort* server_port = g_gateway.get_server_port(p);

        // Checking that port is not empty.
        if ((!server_port->IsEmpty()) && (!server_port->is_udp()))
        {
            // Creating new set of prepared connections.
            // NOTE: Ignoring error code on purpose.
            CreateAcceptingSockets(p);
        }
    }
}

// Scans all channels for any incoming chunks.
uint32_t GatewayWorker::ScanChannels(uint32_t* next_sleep_interval_ms)
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
					// Checking if its the main worker thread.
					if (0 == worker_id_) {

						// Entering global lock.
						WorkerEnterGlobalLock();

						// Deleting all ports that are empty from chunks, etc.
						g_gateway.CleanUpEmptyPorts();

						// Making sure the database is ready for clean up.
						GW_ASSERT(true == g_gateway.GetDatabase(i)->IsReadyForCleanup());

						// Leaving global lock.
						WorkerLeaveGlobalLock();
					}

                    // Finally completely deleting database object and closing shared memory.
                    DeleteInactiveDatabase(i);

#ifdef GW_DATABASES_DIAG
                    GW_PRINT_WORKER << "Deleted shared memory for db slot: " << i << GW_ENDL;
#endif
					// Releasing holding worker.
                    g_gateway.GetDatabase(i)->ReleaseHoldingWorker();

                }
                else
                {
                    // Gateway needs to loop for a while because of chunks being released.
                    if (*next_sleep_interval_ms > 100)
                        *next_sleep_interval_ms = 100;

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

// Checks if port for this socket is aggregating.
bool GatewayWorker::IsAggregatingPort(socket_index_type socket_index)
{
    return g_gateway.get_server_port(sockets_infos_[socket_index].port_index_)->get_aggregating_flag();
}

// Creates the socket data structure.
uint32_t GatewayWorker::CreateSocketData(
    const socket_index_type socket_info_index,
    SocketDataChunkRef out_sd,
    const int32_t data_len)
{
    // Obtaining chunk from gateway private memory.
    // Checking if its an aggregation socket.
    if (IsAggregatingPort(socket_info_index)) {
        out_sd = worker_chunks_.ObtainChunk(MAX_SOCKET_DATA_SIZE);
    } else {
        out_sd = worker_chunks_.ObtainChunk(data_len);
    }

    // Checking if couldn't obtain chunk.
    if (NULL == out_sd)
        return SCERRGWMAXCHUNKSNUMBERREACHED;

    // Initializing socket data.
    out_sd->Init(this, socket_info_index);
    
#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Creating socket data: socket index " << out_sd->get_socket_info_index() << ":" << out_sd->get_unique_socket_id() << ":" << (uint64_t)out_sd << GW_ENDL;
#endif

    return 0;
}

// Adds new active database.
uint32_t GatewayWorker::AddNewDatabase(db_index_type db_index)
{
    worker_dbs_[db_index] = GwNewConstructor2(WorkerDbInterface, db_index, worker_id_);

    return 0;
}

// Push given chunk to database queue.
uint32_t GatewayWorker::PushSocketDataToDb(SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id)
{
    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId()) {

        // Checking if its a disconnect push.
        if (!sd->get_just_push_disconnect_flag()) {
            return SCERRGWOPERATIONONWRONGSOCKETWHENPUSHING;
        }
    }

    // Getting database to which this chunk belongs.
    db_index_type db_index = sd->GetDestDbIndex();
    if (INVALID_DB_INDEX == db_index)
        return SCERRGWOPERATIONONWRONGSOCKETWHENPUSHING;

    WorkerDbInterface *db = GetWorkerDb(db_index);

    // Pushing chunk to that database.
    if (NULL != db) {

        // Always storing the handler id.
        sd->set_handler_id(handler_id);

        // Checking if there is a non-empty overflow queue, so putting in it.
        if (IsOverflowed()) {
            PushToOverflowQueue(sd);
            return 0;
        }

        uint32_t err_code = db->PushSocketDataToDb(this, sd, handler_id, false);

        // Checking if any issue occurred.
        if (err_code) {

            GW_ASSERT(NULL != sd);

            PushToOverflowQueue(sd);

            return 0;
        }

    } else {
        return SCERRGWNULLCODEHOST;
    }

    return 0;
}

// Push given chunk to database queue.
uint32_t GatewayWorker::PushSocketDataFromOverflowToDb(SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* again_for_overflow)
{
    // Assuming no tries.
    *again_for_overflow = false;

    // Checking correct unique socket.
    if (!sd->CompareUniqueSocketId()) {

        // Checking if its a disconnect push.
        if (!sd->get_just_push_disconnect_flag()) {
            return SCERRGWOPERATIONONWRONGSOCKETWHENPUSHING;
        }
    }

    // Getting database to which this chunk belongs.
    db_index_type db_index = sd->GetDestDbIndex();
    if (INVALID_DB_INDEX == db_index)
        return SCERRGWOPERATIONONWRONGSOCKETWHENPUSHING;

    WorkerDbInterface *db = GetWorkerDb(db_index);

    // Pushing chunk to that database.
    if (NULL != db) {

        uint32_t err_code = db->PushSocketDataToDb(this, sd, handler_id, true);

        // Checking if we need to put the socket back to overflow.
        if (err_code) {

            GW_ASSERT(NULL != sd);

            *again_for_overflow = true;

            return 0;
        }

    } else {
        return SCERRGWNULLCODEHOST;
    }

    return 0;
}

// Checks if there is anything in overflow buffer and pushes all chunks from there.
void GatewayWorker::PushOverflowChunks(uint32_t* next_sleep_interval_ms)
{
    uint32_t err_code;
    bool again_for_overflow;

    // We can't try infinitely.
    int32_t num_tries = 0;

    // Looping while we have anything in overflow queue.
    while (IsOverflowed()) {

        SocketDataChunk* sd = PopFromOverlowQueue();
        
        GW_ASSERT(sd != NULL);

        // Checking that socket data is valid.
        sd->CheckForValidity();

        num_tries++;

        err_code = PushSocketDataFromOverflowToDb(sd, sd->get_handler_id(), &again_for_overflow);

        if (0 == err_code) {

            // Checking if sd is again for overflow.
            if (again_for_overflow) {
                PushToOverflowQueue(sd);
            }

        } else {

            // Disconnecting this socket data.
            DisconnectAndReleaseChunk(sd);
        }

        GW_ASSERT(NULL == sd);

        // Checking if number of pushes exceeded.
        if (num_tries >= MAX_OVERFLOW_ATTEMPTS)
            break;
    }

    // Checking if overflowed.
    if (IsOverflowed()) {
        *next_sleep_interval_ms = 0;
    }
}

// Deleting inactive database.
void GatewayWorker::DeleteInactiveDatabase(db_index_type db_index)
{
    if (worker_dbs_[db_index] != NULL)
    {
        GwDeleteSingle(worker_dbs_[db_index]);
        worker_dbs_[db_index] = NULL;
    }
}

uint32_t GatewayWorker::SendHttp200WithBody(
    SocketDataChunkRef sd,
    const char* body,
    const int32_t body_len)
{
    GW_ASSERT(body_len < (TEMP_BIG_BUFFER_SIZE - 512));
    char temp_buf[TEMP_BIG_BUFFER_SIZE];

    // Copying predefined header.
    memcpy(temp_buf, kHttp200Header, kHttp200HeaderLength);

    // Making length a white space.
    *(uint64_t*)(temp_buf + kHttp200HeaderInsertPoint) = 0x2020202020202020;

    // Converting content length to string.
    WriteUIntToString(temp_buf + kHttp200HeaderInsertPoint, body_len);

    // Copying body to response.
    memcpy(temp_buf + kHttp200HeaderLength, body, body_len);

    // Sending predefined response.
    return SendPredefinedMessage(sd, temp_buf, kHttp200HeaderLength + body_len);
}

uint32_t GatewayWorker::SendHttp500WithBody(
    SocketDataChunkRef sd,
    const char* body,
    const int32_t body_len)
{
    GW_ASSERT(body_len < (TEMP_BIG_BUFFER_SIZE - 512));
    char temp_buf[TEMP_BIG_BUFFER_SIZE];

    // Copying predefined header.
    memcpy(temp_buf, kHttp500Header, kHttp500HeaderLength);

    // Making length a white space.
    *(uint64_t*)(temp_buf + kHttp500HeaderInsertPoint) = 0x2020202020202020;

    // Converting content length to string.
    WriteUIntToString(temp_buf + kHttp500HeaderInsertPoint, body_len);

    // Copying body to response.
    memcpy(temp_buf + kHttp500HeaderLength, body, body_len);

    // Sending predefined response.
    return SendPredefinedMessage(sd, temp_buf, kHttp500HeaderLength + body_len);
}

// Sends given predefined response.
uint32_t GatewayWorker::SendPredefinedMessage(
    SocketDataChunkRef sd,
    const char* message,
    const int32_t message_len)
{
    GW_ASSERT(NULL != message);

    uint32_t err_code;

    int32_t cur_message_offset = 0;
    int32_t cur_message_len = message_len;

    // We don't need original chunk contents.
    sd->ResetAccumBuffer();

    // Checking if we need to split the message into portions.
    while (cur_message_len > MAX_SOCKET_DATA_SIZE) {

        // Setting message length to maximum chunk size.
        cur_message_len = MAX_SOCKET_DATA_SIZE;

        SocketDataChunk* sd_send_clone = NULL;
        err_code = sd->CloneToPush(this, cur_message_len, &sd_send_clone);
        if (err_code)
            return err_code;

        GW_ASSERT(cur_message_len == sd_send_clone->get_data_blob_size());

        // Copying part of the message.
        memcpy(sd_send_clone->get_data_blob_start(), message + cur_message_offset, cur_message_len);

        // Prepare buffer to send outside.
        sd_send_clone->PrepareForSend(sd_send_clone->get_data_blob_start(), cur_message_len);

        // Sending data.
        err_code = Send(sd_send_clone);
        if (err_code) {
            // Releasing the cloned chunk.
            ReturnSocketDataChunksToPool(sd_send_clone);
            return err_code;
        }

        GW_ASSERT(NULL == sd_send_clone);

        // Shifting message.
        cur_message_offset += cur_message_len;
        cur_message_len = message_len - cur_message_offset;
    }

    // Checking if data fits inside chunk.
    if (cur_message_len > (int32_t) sd->get_num_available_network_bytes()) {

        err_code = SocketDataChunk::ChangeToBigger(this, sd, cur_message_len);
        if (err_code)
            return err_code;
    }

    // Copying data.
    memcpy(sd->get_data_blob_start(), message + cur_message_offset, cur_message_len);

    // Prepare buffer to send outside.
    sd->PrepareForSend(sd->get_data_blob_start(), cur_message_len);

    // Sending data.
    return Send(sd);
}

} // namespace network
} // namespace starcounter
