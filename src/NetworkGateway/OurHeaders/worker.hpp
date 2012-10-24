#pragma once
#ifndef WORKER_HPP
#define WORKER_HPP

namespace starcounter {
namespace network {

class Profiler;
class WorkerDbInterface;
class GatewayWorker
{
    // Performance profiler.
    Profiler profiler_;

    // Worker ID.
    int32_t worker_id_;

    // Worker IOCP handle.
    HANDLE worker_iocp_;

    // Worker statistics.
    int64_t worker_stats_bytes_received_,
        worker_stats_bytes_sent_,
        worker_stats_sent_num_,
        worker_stats_recv_num_,
        worker_stats_last_bound_num_;

    // All actively connected databases.
    WorkerDbInterface* active_dbs_[MAX_ACTIVE_DATABASES];

    // Worker suspend state.
    volatile bool worker_suspended_;

    // Some worker temporary data.
    char uri_lower_case_[bmx::MAX_URI_STRING_LEN];

public:

    // Getting worker temporary URI.
    char* get_uri_lower_case()
    {
        return uri_lower_case_;
    }

    // Adds new active database.
    uint32_t AddNewDatabase(
        int32_t db_index,
        const core::shared_interface& worker_shared_int);

    // Sets worker suspend state.
    void set_worker_suspended(bool value)
    {
        worker_suspended_ = value;
    }

    // Gets worker suspend state.
    bool worker_suspended()
    {
        return worker_suspended_;
    }

    // Gets global lock.
    void EnterGlobalLock()
    {
        worker_suspended_ = true;

        // Entering global lock.
        g_gateway.EnterGlobalLock();
    }

    // Releases global lock.
    void LeaveGlobalLock()
    {
        worker_suspended_ = false;

        // Leaving global lock.
        g_gateway.LeaveGlobalLock();
    }

    // Getting one of the active databases.
    WorkerDbInterface* GetDatabase(int32_t dbSlotIndex)
    {
        if (active_dbs_[dbSlotIndex] == NULL)
            return NULL;

        return active_dbs_[dbSlotIndex];
    }

    // Deleting inactive database.
    void DeleteInactiveDatabase(int32_t dbSlotIndex);

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

    // Getting the number of receives statistics.
    int64_t get_worker_stats_last_bound_num()
    {
        return worker_stats_last_bound_num_;
    }

    // Random generator.
    random_generator* Random;

    // Worker initialization function.
    int32_t Init(int32_t workerId);

    // Default constructor.
    GatewayWorker()    
    {
    }

    // Destructor.
    ~GatewayWorker()
    {
        // Closing IOCP handle.
        if (worker_iocp_ != g_gateway.get_iocp())
            CloseHandle(worker_iocp_);
    }

    // Main worker function.
    uint32_t WorkerRoutine();

    // Getting worker ID.
    int32_t get_worker_id() { return worker_id_; }

    // Gets worker IOCP.
    HANDLE get_worker_iocp() { return worker_iocp_; }

    // Used to create new connections when reaching the limit.
    uint32_t CreateNewConnections(int32_t how_many, int32_t port_index, int32_t db_index);

    // Functions to process finished IOCP events.
    uint32_t FinishReceive(SocketDataChunk *sd, int32_t numBytesReceived);
    uint32_t FinishSend(SocketDataChunk *sd, int32_t numBytesSent);
    uint32_t FinishDisconnect(SocketDataChunk *sd);
    uint32_t VanishSocketData(SocketDataChunk *sd);
    uint32_t FinishConnect(SocketDataChunk *sd);
    uint32_t FinishAccept(SocketDataChunk *sd, int32_t numBytesReceived);

    // Running connect on socket data.
    uint32_t Connect(SocketDataChunk *sd, sockaddr_in *serverAddr);

    // Running disconnect on socket data.
    uint32_t Disconnect(SocketDataChunk *sd);

    // Running send on socket data.
    uint32_t Send(SocketDataChunk *sd);

    // Running receive on socket data.
    uint32_t Receive(SocketDataChunk *sd);

    // Running accept on socket data.
    uint32_t Accept(SocketDataChunk *sd);

    // Processes socket data to database.
    uint32_t RunToDbHandlers(SocketDataChunk *sd)
    {
        // Putting socket data to database.
        sd->PrepareToDb();

        // Here we have to process socket data using handlers.
        uint32_t err_code = sd->RunHandlers(this);
        if (0 != err_code)
        {
            // Disconnecting socket.
            Disconnect(sd);

            // Ban the fucking IP.
        }

        return 0;
    }

    // Processes socket data from database.
    uint32_t RunFromDbHandlers(SocketDataChunk *sd)
    {
        // Putting socket data from database.
        sd->PrepareFromDb();

        // Here we have to process socket data using handlers.
        uint32_t err_code = sd->RunHandlers(this);
        if (0 != err_code)
        {
            // Disconnecting socket.
            Disconnect(sd);

            // Ban the fucking IP.
        }

        return 0;
    }

    // Push given chunk to database queue.
    uint32_t PushSocketDataToDb(SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id);

    // Scans all channels for any incoming chunks.
    uint32_t ScanChannels();

    // Creates the socket data structure.
    SocketDataChunk* CreateSocketData(
        SOCKET sock,
        int32_t port_index,
        int32_t db_index);
};

} // namespace network
} // namespace starcounter

#endif // WORKER_HPP
