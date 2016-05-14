#pragma once
#ifndef WORKER_DB_INTERFACE_HPP
#define WORKER_DB_INTERFACE_HPP

namespace starcounter {
namespace network {

class GatewayWorker;
class WorkerDbInterface
{
    enum {
        // The number of times to scan through all channels trying to push or
        // pop before waiting, in case did not push or pop for spin_count_reset
        // number of times. Need to experiment with this value.
        // NOTE: scan_counter_preset must be > 0.
        scan_counter_preset = 1 << 20,

        // The thread can give up waiting after wait_for_work_milli_seconds, but
        // if not, set it to INFINITE.
        wait_for_work_milli_seconds = 1000
    };

    // Each worker has a copy of the gateway's shared_interface. The pointers to
    // various objects in shared memory are copied, but each worker will
    // initialize its own client_number.
    core::shared_interface shared_int_;

    // An array of indexes to channels, unordered.
    core::channel_number* channels_;

    // Private chunk pool.
    core::chunk_pool<core::chunk_index> private_chunk_pool_;

    // Simulation shared memory queue for the shared memory.
    LinearQueue<core::chunk_index, 100000> simulated_shared_memory_queue_;

    // Simulation shared memory queue using the socket data.
    LinearQueue<SocketDataChunk*, 100000> simulated_shared_memory_queue_using_sd_;

    // Database index.
    db_index_type db_index_;

    // Number of IPC chunks available for gateway.
    std::size_t max_num_icp_chunks_for_gateway_;

    // Worker id to which this interface belongs.
    worker_id_type worker_id_;

    // Number of active schedulers.
    int32_t num_schedulers_;

#ifndef LEAST_USED_SCHEDULING
    // Current scheduler id.
    int32_t cur_scheduler_id_;
#endif // !LEAST_USED_SCHEDULING

    // Acquires needed amount of chunks from shared pool.
    uint32_t AcquireIPCChunksFromSharedPool(int32_t num_ipc_chunks)
    {
        // Checking that gateway is not taking more chunks than it should.
        if (shared_int_.size() < max_num_icp_chunks_for_gateway_)
            return SCERRACQUIRELINKEDCHUNKS;

        // Acquire chunks from the shared chunk pool to this worker private
        // chunk pool.
        //
        // Note that in case the server (database host process) failed while
        // holding a lock on the shared chunk pool the operation this is handled
        // by the operation timing out, no chunks will then be allocated (if no
        // time-out the thread, and gateway, might get stuck on the spin-lock
        // serializing access to the shared chunk pool).
        int32_t num_acquired_chunks = static_cast<int32_t> (shared_int_.acquire_from_shared_to_private(
            private_chunk_pool_, num_ipc_chunks, &shared_int_.client_interface(), 1000));

        //GW_ASSERT(num_acquired_chunks == num_ipc_chunks);

        // Checking that number of acquired chunks is correct.
        if (num_acquired_chunks != num_ipc_chunks)
        {
            // Some problem acquiring enough chunks.
#ifdef GW_ERRORS_DIAG
            GW_COUT << "Problem acquiring chunks from shared chunk pool." << GW_ENDL;
#endif
            return SCERRACQUIRELINKEDCHUNKS;
        }

        return 0;
    }

public:

    // Printing the database information.
    void PrintInfo(std::stringstream& stats_stream);

#ifndef LEAST_USED_SCHEDULING
    // Round-robin scheduler number.
    uint32_t GenerateSchedulerId()
    {
        cur_scheduler_id_++;
        if (cur_scheduler_id_ >= num_schedulers_)
            cur_scheduler_id_ = 0;

        return cur_scheduler_id_;
    }
#endif // !LEAST_USED_SCHEDULING

#ifdef LEAST_USED_SCHEDULING
    uint32_t GenerateSchedulerId()
    {
        // Selects the scheduler with the least number of tasks enqueued on the
        // channel reserved for the current worker.

        int32_t least_used_queue_length = INT32_MAX;
        int32_t sched_id = 0;
        for (int32_t s = 0; s < num_schedulers_; s++)
        {
            core::channel_type& the_channel = shared_int_.channel(channels_[s]);
            int32_t queue_length = the_channel.in.count();
            if (queue_length < least_used_queue_length)
            {
              least_used_queue_length = queue_length;
              sched_id = s;
            }
        }
        return (uint32_t)sched_id;
    }
#endif // LEAST_USED_SCHEDULING

    // Getting shared interface.
    core::shared_interface* get_shared_int()
    {
        return &shared_int_;
    }

    // Declares gateway ready for database pushes.
    uint32_t SetGatewayReadyForDbPushes();

    // Resets the existing interface.
    void Reset()
    {
        db_index_ = INVALID_DB_INDEX;
        worker_id_ = INVALID_WORKER_INDEX;

        num_schedulers_ = 0;
#ifndef LEAST_USED_SCHEDULING
        cur_scheduler_id_ = 0;
#endif // !LEAST_USED_SCHEDULING

        if (channels_)
        {
            GwDeleteArray(channels_);
            channels_ = NULL;
        }
    }

    // Allocates different channels and pools.
    WorkerDbInterface(
        const int32_t new_slot_index,
        const int32_t worker_id);

    // Deallocates active database.
    ~WorkerDbInterface()
    {
        // Deleting channels.
        GwDeleteArray(channels_);
        channels_ = NULL;
    }

    // Tries pushing to channel and returns try if it did.
    bool TryPushToChannel(
        core::channel_type& the_channel,
        core::chunk_index the_chunk_index)
    {
        // Trying to push chunk.
        if (the_channel.in.try_push_front(the_chunk_index))
        {
            // Successfully pushed the response message to the channel.

            // A message on channel ch was received. Notify the database
            // that the out queue in this channel is not full.
            the_channel.scheduler()->notify(shared_int_.scheduler_work_event
                (the_channel.get_scheduler_number()));

#ifdef GW_CHUNKS_DIAG
            GW_PRINT_WORKER_DB << "   successfully pushed: chunk " << the_chunk_index << GW_ENDL;
#endif

            return true;
        }

        return false;
    }

    // Push whatever chunks we have to channels.
    bool PushLinkedChunksToDb(
        core::chunk_index chunk_index,
        int16_t scheduler_id,
        bool is_gateway_no_ipc_test);

    uint32_t PushSocketDataToDb(GatewayWorker* gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id, bool is_from_overflow_pool);

    // Releases chunks from private chunk pool to the shared chunk pool.
    uint32_t ReleaseToSharedChunkPool(int32_t num_ipc_chunks);

    // Scans all channels for any incoming chunks.
    uint32_t ScanChannels(GatewayWorker *gw, uint32_t* next_sleep_interval_ms);

    // Getting shared memory chunk.
    shared_memory_chunk* GetSharedMemoryChunkFromIndex(core::chunk_index the_chunk_index)
    {
        return (shared_memory_chunk *)(&shared_int_.chunk(the_chunk_index));
    }

    // Obtains chunk from a private pool if its not empty
    // (otherwise fetches from shared chunk pool).
    uint32_t GetOneChunkFromPrivatePool(core::chunk_index* chunk_index, shared_memory_chunk** smc);

    // Obtains chunks from a private pool if its not empty
    // (otherwise fetches from shared chunk pool).
    uint32_t GetMultipleChunksFromPrivatePool(
        core::chunk_index* new_chunk_index,
        uint32_t num_ipc_chunks)
    {
        // Trying to fetch chunk from private pool.
        uint32_t err_code;
        while (!private_chunk_pool_.acquire_linked_chunks_counted(&shared_int_.chunk(0), *new_chunk_index, num_ipc_chunks))
        {
            // Getting chunks from shared chunk pool.
            err_code = AcquireIPCChunksFromSharedPool(MAX_CHUNKS_IN_PRIVATE_POOL);
            if (err_code)
                return err_code;
        }

#ifdef GW_CHUNKS_DIAG
        GW_COUT << "Acquired new " << num_ipc_chunks << " linked chunks: " << *new_chunk_index << GW_ENDL;
#endif

        return 0;
    }

    // Returns given linked chunks to private chunk pool (and if needed then to shared).
    void ReturnLinkedChunksToPool(core::chunk_index& first_linked_chunk);

    // Returns all chunks from private pool to shared.
    void ReturnAllPrivateChunksToSharedPool();
};

} // namespace network
} // namespace starcounter

#endif // WORKER_DB_INTERFACE_HPP
