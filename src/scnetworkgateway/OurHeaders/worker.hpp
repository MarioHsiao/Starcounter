#pragma once
#ifndef WORKER_HPP
#define WORKER_HPP

namespace starcounter {
namespace network {

class WorkerChunks
{
    // Chunk stores.
    LinearQueue<SocketDataChunk*, MAX_WORKER_CHUNKS> worker_chunks_[NumGatewayChunkSizes];

    // Number of allocated chunks for each chunks store.
    int32_t num_allocated_chunks_[NumGatewayChunkSizes];

public:

    WorkerChunks()
    {
        memset(num_allocated_chunks_, 0, sizeof(num_allocated_chunks_));
    }

    void PrintInfo(std::stringstream& stats_stream)
    {
        for (int32_t i = 0; i < NumGatewayChunkSizes; i++) 
        {
            stats_stream << num_allocated_chunks_[i];
            if ((i + 1) < NumGatewayChunkSizes)
                stats_stream << ", "; 
        }
    }

    int32_t GetNumberAllocatedChunks(chunk_store_type store_index)
    {
        return num_allocated_chunks_[store_index];
    }

    // Releasing existing chunk to the list.
    void ReleaseChunk(SocketDataChunkRef sd)
    {
        chunk_store_type store_index = sd->get_chunk_store_index();

        // Invalidating before returning.
        sd->InvalidateWhenReturning();

        // Checking if we should completely dispose the chunk.
        if (worker_chunks_[store_index].get_num_entries() > GatewayChunkStoresSizes[store_index])
        {
            GwDeleteAligned(sd);
            num_allocated_chunks_[store_index]--;
        }
        else
        {
            worker_chunks_[store_index].PushBack(sd);
        }

        sd = NULL;
    }

    // Getting free chunk from the list or creating a new one.
    SocketDataChunk* ObtainChunk(int32_t data_size = GatewayChunkDataSizes[DefaultGatewayChunkSizeType])
    {
        chunk_store_type chunk_store_index = ObtainGatewayChunkType(data_size);

        return ObtainChunkByStoreIndex(chunk_store_index);
    }

    // Getting free chunk from the list or creating a new one.
    SocketDataChunk* ObtainChunkByStoreIndex(chunk_store_type chunk_store_index)
    {
        SocketDataChunk* sd;

        // Checking if a free chunk is available.
        if (worker_chunks_[chunk_store_index].get_num_entries())
        {
            sd = worker_chunks_[chunk_store_index].PopBack();
            goto RETURN_SD;
        }

        // Checking if we have allocated too many chunks for this store.
        if (num_allocated_chunks_[chunk_store_index] > GatewayChunkStoresSizes[chunk_store_index]) {
            return NULL;
        }

        // Creating new chunk.
        sd = (SocketDataChunk*) GwNewAligned(GatewayChunkSizes[chunk_store_index]);
        GW_ASSERT(NULL != sd);
        num_allocated_chunks_[chunk_store_index]++;

RETURN_SD:

        sd->set_chunk_store_index(chunk_store_index);
        return sd;
    }
};

_declspec(align(MEMORY_ALLOCATION_ALIGNMENT)) class RebalancedSocketInfo {

    // NOTE: Lock-free SLIST_ENTRY should be the first field!
    SLIST_ENTRY lockfree_entry_;

    // Saved socket handle.
    SOCKET socket_;

    // Saved client IP address.
    ip_info_type client_ip_info_;

    // Saved port index.
    port_index_type port_index_;
    
public:

    void Init(port_index_type port_index, SOCKET socket, ip_info_type client_ip_info) {
        port_index_ = port_index;
        socket_ = socket;
        client_ip_info_ = client_ip_info;
    }

    port_index_type get_port_index() {
        return port_index_;
    }

    SOCKET get_socket() {
        return socket_;
    }

    ip_info_type get_client_ip_info() {
        return client_ip_info_;
    }
};

class Profiler;
class WorkerDbInterface;
class GatewayWorker
{
    // Worker ID.
    worker_id_type worker_id_;

    // Worker IOCP handle.
    HANDLE worker_iocp_;

    // Worker statistics.
    int64_t worker_stats_bytes_received_,
        worker_stats_bytes_sent_,
        worker_stats_sent_num_,
        worker_stats_recv_num_;

    // All actively connected databases.
    WorkerDbInterface* worker_dbs_[MAX_ACTIVE_DATABASES];

    // Some worker temporary data.
    char method_space_uri_space_worker_buf_[MixedCodeConstants::MAX_URI_STRING_LEN];

    // Random generator.
    random_generator* rand_gen_;

    // Clone made during last iteration.
    SocketDataChunk* sd_receive_clone_;

    // Number of created connections calculated for worker.
    int32_t num_created_conns_worker_;

    // Thread-safe list of rebalanced accept sockets.
    PSLIST_HEADER rebalance_accept_sockets_;

    // Aggregation sockets waiting for send.
    LinearList<SocketDataChunk*, 256> aggr_sds_to_send_;

    // Overflow socket data chunks.
    LinearQueue<SocketDataChunk*, MAX_WORKER_CHUNKS> overflow_sds_;

    // Worker sockets infos.
    ScSocketInfoStruct* sockets_infos_;

    // Indexes to free socket infos.
    LinearQueue<socket_index_type, MAX_WORKER_CHUNKS> free_sockets_infos_;

    // Aggregation timer.
    uint64_t aggr_timer_;

    // Worker chunks.
    WorkerChunks worker_chunks_;
    
    // Avoiding false sharing.
    uint8_t pad[CACHE_LINE_SIZE];

    // Pops rebalance socket info.
    RebalancedSocketInfo* PopRebalanceSocketInfo() {
        RebalancedSocketInfo* rsi = (RebalancedSocketInfo*) InterlockedPopEntrySList(rebalance_accept_sockets_);
        return rsi;
    }

    // Pushes rebalance socket info.
    void PushRebalanceSocketInfo(RebalancedSocketInfo*& rsi) {
        InterlockedPushEntrySList(rebalance_accept_sockets_, (PSLIST_ENTRY) rsi);
        rsi = NULL;
    }

    // Creating accepting sockets on all ports.
    void CheckAcceptingSocketsOnAllActivePorts();

public:

    // Processes socket info for aggregation loopback.
    void LoopbackForAggregation(SocketDataChunkRef sd);

    // Checks if there is anything in overflow buffer and pushes all chunks from there.
    void PushOverflowChunks(uint32_t* next_sleep_interval_ms);

    // Worker chunks.
    WorkerChunks* GetWorkerChunks()
    {
        return &worker_chunks_;
    }

    void PushToOverflowQueue(SocketDataChunkRef sd) {
        overflow_sds_.PushBack(sd);
        sd = NULL;
    }

    SocketDataChunk* PopFromOverlowQueue() {

        if (overflow_sds_.get_num_entries() > 0)
            return overflow_sds_.PopFront();

        return NULL;
    }

    int32_t NumOverflowChunks() {
        return overflow_sds_.get_num_entries();
    }

    bool IsOverflowed() {
        return (NumOverflowChunks() > 0);
    }
    
    // Performs a send of given socket data on aggregation socket.
    uint32_t SendOnAggregationSocket(SocketDataChunkRef sd, MixedCodeConstants::AggregationMessageTypes msg_type);

    uint32_t SendOnAggregationSocket(
        const socket_index_type aggr_socket_info_index,
        const random_salt_type aggr_unique_socket_id,
        const uint8_t* data,
        const int32_t data_len);

    // Tries to find current aggregation socket data from aggregation socket index.
    SocketDataChunk* FindAggregationSd(socket_index_type aggr_socket_info_index, random_salt_type aggr_unique_socket_id);

    // Returns given socket data chunk to private chunk pool.
    void ReturnSocketDataChunksToPool(SocketDataChunkRef sd);

    // Processes all aggregated chunks.
    uint32_t SendAggregatedChunks();

    // Starting accumulation.
    uint32_t StartAccumulation(SocketDataChunkRef sd, uint32_t total_desired_bytes, uint32_t num_already_accumulated)
    {
        // Enabling accumulative state.
        sd->set_accumulating_flag();

        // Checking if the host accumulation should be involved.
        if (total_desired_bytes > g_gateway.setting_maximum_receive_content_length())
        {
            // We need to accumulate on host.
            sd->set_on_host_accumulation_flag();

			wchar_t temp[MixedCodeConstants::MAX_URI_STRING_LEN];
			wsprintf(temp, L"Attempt to upload of more than %d bytes on the socket. Closing socket connection.", 
				g_gateway.setting_maximum_receive_content_length());
			g_gateway.LogWriteWarning(temp);

            return SCERRGWMAXDATASIZEREACHED;
        }

        // Checking if data that needs accumulation fits into chunk.
        if (sd->get_data_blob_size() < total_desired_bytes)
        {
            uint32_t err_code = SocketDataChunk::ChangeToBigger(this, sd, total_desired_bytes);
            if (err_code)
                return err_code;
        }

        GW_ASSERT(sd->get_data_blob_size() >= total_desired_bytes);

        // Calculating the remaining number of bytes to accumulate.
        sd->set_num_available_network_bytes(total_desired_bytes - num_already_accumulated);

        return 0;
    }

    // Clone made during last iteration.
    SocketDataChunkRef get_sd_receive_clone()
    {
        return sd_receive_clone_;
    }

    // Checks if cloning was performed and does operations.
    uint32_t ProcessReceiveClones(bool just_delete_clone);

    // Sets the clone for the next iteration.
    void SetReceiveClone(SocketDataChunkRef sd_clone)
    {
		// Setting flag clone to receive.
		sd_clone->get_socket_info()->set_cloned_to_receive_flag();

        // Only one clone at a time is possible.
        GW_ASSERT(sd_receive_clone_ == NULL);

        sd_receive_clone_ = sd_clone;
    }

    // Changes number of accepting sockets.
    int64_t ChangeNumAcceptingSockets(port_index_type port_index, int64_t change_value)
    {
#ifdef GW_DETAILED_STATISTICS
        GW_COUT << "ChangeNumAcceptingSockets: " << change_value << GW_ENDL;
#endif

        return g_gateway.get_server_port(port_index)->ChangeNumAcceptingSockets(change_value);
    }

    void AddToActiveSockets(port_index_type port_index)
    {
        g_gateway.get_server_port(port_index)->AddToActiveSockets(worker_id_);
    }

    void RemoveFromActiveSockets(port_index_type port_index)
    {
        g_gateway.get_server_port(port_index)->RemoveFromActiveSockets(worker_id_);
    }

    worker_id_type GetLeastBusyWorkerId(port_index_type port_index)
    {
        return g_gateway.get_server_port(port_index)->GetLeastBusyWorkerId();
    }

    // Getting random generator.
    random_generator* get_random()
    {
        return rand_gen_;
    }

    // Getting worker temporary URI.
    char* get_method_space_uri_space_worker_buf()
    {
        return method_space_uri_space_worker_buf_;
    }

    // Adds new active database.
    uint32_t AddNewDatabase(db_index_type db_index);

    // Gets global lock.
    void WorkerEnterGlobalLock()
    {
		// Signalling that worker is in wait state.
		if (!SetEvent(g_gateway.get_worker_suspend_handle(worker_id_))) {
			GW_ASSERT(!"Can't set worker suspend event.");
		}

        // Entering global lock.
        g_gateway.EnterGlobalLock();
    }

    // Releases global lock.
    void WorkerLeaveGlobalLock()
    {
		// This should work as a barrier.
		if (!ResetEvent(g_gateway.get_worker_suspend_handle(worker_id_))) {
			GW_ASSERT(!"Can't reset worker suspend event.");
		}

        // Leaving global lock.
        g_gateway.LeaveGlobalLock();
    }

    // Getting one of the active databases.
    WorkerDbInterface* GetWorkerDb(db_index_type db_index)
    {
        // Checking for correct database.
        GW_ASSERT_DEBUG(db_index < g_gateway.get_num_dbs_slots());

        return worker_dbs_[db_index];
    }

    // Deleting inactive database.
    void DeleteInactiveDatabase(db_index_type db_index);

    // Sends given predefined response.
    uint32_t SendPredefinedMessage(
        SocketDataChunkRef sd,
        const char* message,
        const int32_t message_len);

    uint32_t SendHttp200WithBody(
        SocketDataChunkRef sd,
        const char* body,
        const int32_t body_len);

    uint32_t SendHttp500WithBody(
        SocketDataChunkRef sd,
        const char* body,
        const int32_t body_len);

    // Getting the bytes received statistics.
    int64_t get_worker_stats_bytes_received()
    {
        return worker_stats_bytes_received_;
    }

    // Getting the bytes sent statistics.
    int64_t get_worker_stats_bytes_sent()
    {
        return worker_stats_bytes_sent_;
    }

    // Getting the number of sends statistics.
    int64_t get_worker_stats_sent_num()
    {
        return worker_stats_sent_num_;
    }

    // Getting the number of receives statistics.
    int64_t get_worker_stats_recv_num()
    {
        return worker_stats_recv_num_;
    }

    // Printing the worker information.
    void PrintInfo(std::stringstream& stats_stream)
    {
        stats_stream << "{\"id\":" << static_cast<int32_t>(worker_id_) << ",";
        stats_stream << "\"bytesReceived\":" << worker_stats_bytes_received_ << ",";
        stats_stream << "\"packetsReceived\":" << worker_stats_recv_num_ << ",";
        stats_stream << "\"bytesSent\":" << worker_stats_bytes_sent_ << ",";
        stats_stream << "\"packetsSent\":" << worker_stats_sent_num_ << ",";
        stats_stream << "\"allocatedChunks\":\"";
        worker_chunks_.PrintInfo(stats_stream);
        stats_stream << "\"}";
    }

    // Worker initialization function.
    int32_t Init(int32_t workerId);

    // Default constructor.
    GatewayWorker()    
    {
    }

    // Destructor.
    ~GatewayWorker()
    {
        // Deleting only necessary stuff.
        for (int32_t i = 0; i < MAX_ACTIVE_DATABASES; i++)
        {
            if (worker_dbs_[i])
            {
                GwDeleteSingle(worker_dbs_[i]);
                worker_dbs_[i] = NULL;
            }
        }
    }

    // Processes rebalanced sockets from worker 0.
    void ProcessRebalancedSockets();

    // Main worker function.
    uint32_t WorkerRoutine();

    // Getting worker ID.
    worker_id_type get_worker_id() { return worker_id_; }

    // Gets worker IOCP.
    HANDLE get_worker_iocp() { return worker_iocp_; }

    // Used to create new connections when reaching the limit.
    uint32_t CreateAcceptingSockets(port_index_type port_index);

    // Used to create new UDP sockets when reaching the limit.
    uint32_t CreateUdpSockets(port_index_type port_index);

    // Allocates a bunch of new connections.
    uint32_t CreateProxySocket(SocketDataChunkRef proxy_sd, MixedCodeConstants::NetworkProtocolType protocol_type);

    // Functions to process finished IOCP events.
    uint32_t FinishReceive(SocketDataChunkRef sd, int32_t numBytesReceived, bool& called_from_receive);
    uint32_t FinishSend(SocketDataChunkRef sd, int32_t numBytesSent);
    uint32_t FinishDisconnect(SocketDataChunkRef sd);
    uint32_t FinishConnect(SocketDataChunkRef sd);
    uint32_t FinishAccept(SocketDataChunkRef sd);

	// Do internal HTTP request.
	uint32_t DoInternalHttpRequest(SocketDataChunkRef sd, const char* http_request_data, const int32_t request_data_size);

    // Running connect on socket data.
    uint32_t Connect(SocketDataChunkRef sd, sockaddr_in *serverAddr);

    // Running disconnect on socket data.
    void DisconnectAndReleaseChunk(SocketDataChunkRef sd);

    // Pushes disconnect message to host if needed.
    void PushDisconnectToCodehost(SocketDataChunkRef sd);

    // Send disconnect to database.
    uint32_t SendTcpSocketDisconnectToDb(SocketDataChunk* sd);

    // Initiates receive on arbitrary socket.
    uint32_t ReceiveOnSocket(socket_index_type socket_index);

    // Running send on socket data.
    uint32_t Send(SocketDataChunkRef sd);

    // Running send on socket data.
    uint32_t SendOnUdp(SocketDataChunkRef sd);

    // Running receive on socket data.
    uint32_t Receive(SocketDataChunkRef sd);

    // Running accept on socket data.
    uint32_t Accept(SocketDataChunkRef sd);

    // Processes socket data to database.
    uint32_t RunReceiveHandlers(SocketDataChunkRef sd)
    {
        // Putting socket data to database.
        sd->PrepareToDb();

        bool is_handled = false;

        // Here we have to process socket data using handlers.
        uint32_t err_code = RunHandlers(this, sd, &is_handled);
        if (err_code)
        {
            // Ban the fucking IP.

            return err_code;
        }

        return 0;
    }

    // Processes socket data from database.
    uint32_t RunFromDbHandlers(SocketDataChunkRef sd)
    {
        // Putting socket data from database.
        sd->PrepareFromDb();

        bool is_handled = false;

        // Here we have to process socket data using handlers.
        uint32_t err_code = RunHandlers(this, sd, &is_handled);
        if (err_code)
        {
            // Ban the fucking IP.

            return err_code;
        }

        return 0;
    }

    // Does general data processing using port handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd, bool* is_handled)
    {
        port_index_type port_index = sd->GetPortIndex();
        if (INVALID_PORT_INDEX == port_index)
            return SCERRGWWRONGPORTINDEX;

        HandlersList* ph = g_gateway.get_server_port(port_index)->get_port_handlers();

        GW_ASSERT(NULL != ph);

        return ph->RunHandlers(gw, sd, is_handled);
    }

    // Push given chunk to database queue.
    uint32_t PushSocketDataToDb(SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool disable_check_for_clone);

    // Push given chunk to database queue.
    uint32_t PushSocketDataFromOverflowToDb(SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool* again_for_overflow);

    // Scans all channels for any incoming chunks.
    uint32_t ScanChannels(uint32_t* next_sleep_interval_ms);

    // Creates the socket data structure.
    uint32_t CreateSocketData(
        const socket_index_type socket_info_index,
        SocketDataChunkRef out_sd,
        const int32_t data_len = GatewayChunkDataSizes[DefaultGatewayChunkSizeType]);

    // Checks if port for this socket is aggregating.
    bool IsAggregatingPort(socket_index_type socket_index);

    // Checking if unique socket number is correct.
    bool CompareUniqueSocketId(socket_index_type socket_index, random_salt_type unique_socket_id)
    {
        GW_ASSERT_DEBUG(socket_index < g_gateway.setting_max_connections_per_worker());

        bool is_equal = (sockets_infos_[socket_index].unique_socket_id_ == unique_socket_id);

        return is_equal;
    }

    // Setting aggregation socket index.
    void SetAggregationSocketInfo(
        socket_index_type socket_index,
        socket_index_type aggr_socket_info_index,
        random_salt_type aggr_unique_socket_id)
    {
        GW_ASSERT_DEBUG(socket_index < g_gateway.setting_max_connections_per_worker());

        sockets_infos_[socket_index].aggr_socket_info_index_ = aggr_socket_info_index;
        sockets_infos_[socket_index].aggr_unique_socket_id_ = aggr_unique_socket_id;
    }

    // Getting reference to a particular socket info.
    ScSocketInfoStruct* GetSocketInfoReference(socket_index_type socket_index)
    {
        GW_ASSERT_DEBUG(socket_index < g_gateway.setting_max_connections_per_worker());

        return sockets_infos_ + socket_index;
    }

    // Setting aggregated socket flag.
    void SetSocketAggregatedFlag(socket_index_type socket_index)
    {
        GW_ASSERT_DEBUG(socket_index < g_gateway.setting_max_connections_per_worker());

        sockets_infos_[socket_index].set_socket_aggregated_flag();
    }

    // Disconnect proxy socket.
    void DisconnectProxySocket(SocketDataChunk* sd)
    {
        socket_index_type proxy_socket_index = sd->GetProxySocketIndex();

        GW_ASSERT_DEBUG(proxy_socket_index < g_gateway.setting_max_connections_per_worker());

        // Checking if socket info is not reseted yet.
        if (false == sockets_infos_[proxy_socket_index].IsReset()) {

            // Setting unique socket id.
            GenerateUniqueSocketInfoIds(proxy_socket_index);

            if (false == sockets_infos_[proxy_socket_index].IsInvalidSocket()) {

                // Disconnecting proxy socket.
                sockets_infos_[proxy_socket_index].DisconnectSocket();
            }
        }
    }

    // Applying session parameters to socket data.
    bool ApplySocketInfoToSocketData(SocketDataChunkRef sd, socket_index_type socket_index, random_salt_type unique_socket_id);

    // Creates new socket info.
    void CreateNewSocketInfo(socket_index_type socket_index, port_index_type port_index, worker_id_type worker_id)
    {
        GW_ASSERT((port_index >= 0) && (port_index < MAX_PORTS_NUM));

        sockets_infos_[socket_index].port_index_ = port_index;
        sockets_infos_[socket_index].session_.gw_worker_id_ = worker_id;
    }

    // Setting new unique socket number.
    random_salt_type GenerateUniqueSocketInfoIds(socket_index_type socket_index)
    {
        GW_ASSERT_DEBUG(socket_index < g_gateway.setting_max_connections_per_worker());

        random_salt_type unique_id = g_gateway.get_unique_socket_id();
        GW_ASSERT(unique_id != INVALID_SESSION_SALT);

        sockets_infos_[socket_index].unique_socket_id_ = unique_id;

#ifdef GW_SOCKET_DIAG
        GW_COUT << "New unique socket id " << socket_index << ":" << unique_id << GW_ENDL;
#endif

        return unique_id;
    }

    // Getting unique socket number.
    random_salt_type GetUniqueSocketId(socket_index_type socket_index)
    {
        GW_ASSERT_DEBUG(socket_index < g_gateway.setting_max_connections_per_worker());

        return sockets_infos_[socket_index].unique_socket_id_;
    }

    // Gets socket info data by index.
    ScSocketInfoStruct GetGlobalSocketInfoCopy(socket_index_type socket_index)
    {
        // Checking validity of linear session index other wise return a wrong copy.
        if (INVALID_SOCKET_INDEX == socket_index)
            return ScSocketInfoStruct();

        // Fetching the session by index.
        return sockets_infos_[socket_index];
    }

    // Collects outdated sockets if any.
    uint32_t CollectInactiveSockets();

	// Collects outdated sockets if any.
	uint32_t DisonnectCodehostSockets(db_index_type db_index);

    // Releases socket info index.
    void ReleaseSocketIndex(socket_index_type socket_index);

    // Gets free socket index.
    socket_index_type ObtainFreeSocketIndex(
        SOCKET s,
        port_index_type port_index,
        MixedCodeConstants::NetworkProtocolType protocol_type,
        bool proxy_connect_socket);
};

} // namespace network
} // namespace starcounter

#endif // WORKER_HPP
