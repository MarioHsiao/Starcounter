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

    // Overflow chunk pools.
    core::chunk_pool<channel_chunk> private_overflow_pool_;

    // Database index.
    int32_t db_index_;

    // Worker id to which this interface belongs.
    int32_t worker_id_;

    // Open socket handles.
    std::bitset<MAX_SOCKET_HANDLE> active_sockets_bitset_;

    // Number of used sockets.
    int64_t num_used_sockets_;

    // Number of used chunks.
    int64_t num_used_chunks_;

    // Number of active schedulers.
    uint32_t num_schedulers_;

    // Current scheduler id.
    uint32_t cur_scheduler_id_;

    // Round-robin scheduler number.
    uint32_t GetSchedulerId()
    {
        cur_scheduler_id_++;
        if (cur_scheduler_id_ >= num_schedulers_)
            cur_scheduler_id_ = 0;

        return cur_scheduler_id_;
    }

    // Acquires needed amount of chunks from shared pool.
    uint32_t AcquireChunksFromSharedPool(int32_t num_chunks)
    {
        core::chunk_index current_chunk_index;

        // Acquire chunks from the shared chunk pool to this worker private chunk pool.
        int32_t num_acquired_chunks = shared_int_.acquire_from_shared_to_private(
            private_chunk_pool_, num_chunks, &shared_int_.client_interface(), 1000);

        // Checking that number of acquired chunks is correct.
        if (num_acquired_chunks != num_chunks)
        {
            // Some problem acquiring enough chunks.
#ifdef GW_ERRORS_DIAG
            GW_COUT << "Problem acquiring chunks from shared chunk pool." << GW_ENDL;
#endif
            return SCERRACQUIRELINKEDCHUNKS;
        }

        // Changing number of database chunks.
        ChangeNumUsedChunks(num_chunks);

        return 0;
    }


    // Registers push channel.
    uint32_t RegisterPushChannel(int32_t sched_num);

    // Requesting previously registered handlers.
    uint32_t RequestRegisteredHandlers(int32_t sched_num);

public:

    // Increments or decrements the number of active chunks.
    void ChangeNumUsedChunks(int64_t change_value)
    {
        num_used_chunks_ += change_value;

#ifdef GW_CHUNKS_DIAG
        GW_COUT << "ChangeNumUsedChunks: " << change_value << " and " << num_used_chunks_ << GW_ENDL;
#endif
    }

    // Getting the number of used chunks.
    int64_t get_num_used_chunks()
    {
        return num_used_chunks_;
    }

    // Tracks certain socket.
    void TrackSocket(SOCKET s)
    {
        num_used_sockets_++;
        active_sockets_bitset_[s] = true;
    }

    // Untracks certain socket.
    void UntrackSocket(SOCKET s)
    {
        num_used_sockets_--;
        active_sockets_bitset_[s] = false;
    }

    // Getting number of used sockets.
    int64_t get_num_used_sockets()
    {
        return num_used_sockets_;
    }

    // Gets certain socket state.
    bool GetSocketState(SOCKET s)
    {
        return active_sockets_bitset_[s];
    }

    // Sends session destroyed message.
    uint32_t PushDeadSession(
        apps_unique_session_num_type apps_unique_session,
        session_salt_type apps_session_salt,
        uint32_t scheduler_id);

    // Getting shared interface.
    core::shared_interface* get_shared_int()
    {
        return &shared_int_;
    }

    // Registers all push channels.
    uint32_t RegisterAllPushChannels();

    // Request registered user handlers.
    uint32_t RequestRegisteredHandlers();

    // Resets the existing interface.
    void Reset()
    {
        db_index_ = INVALID_DB_INDEX;
        worker_id_ = INVALID_WORKER_INDEX;
        num_used_sockets_ = 0;
        num_used_chunks_ = 0;
        num_schedulers_ = 0;
        cur_scheduler_id_ = 0;

        if (channels_)
        {
            delete[] channels_;
            channels_ = NULL;
        }

        // Setting all sockets as inactive.
        for (int32_t i = 0; i < MAX_SOCKET_HANDLE; i++)
            active_sockets_bitset_[i] = false;
    }

    // Allocates different channels and pools.
    WorkerDbInterface(
        const int32_t new_slot_index,
        const core::shared_interface& workerSharedInt,
        const int32_t worker_id);

    // Deallocates active database.
    ~WorkerDbInterface()
    {
        // Freeing all occupied channels.
        for (std::size_t i = 0; i < num_schedulers_; i++)
        {
            core::channel_type& the_channel = shared_int_.channel(channels_[i]);
            the_channel.set_to_be_released();
        }

        // Deleting channels.
        delete[] channels_;
        channels_ = NULL;
    }

    // Checks if there is anything in overflow buffer and pushes all chunks from there.
    uint32_t PushOverflowChunksIfAny()
    {
        // Checking if anything is in overflow pool.
        if (private_overflow_pool_.empty())
            return 0;

        uint32_t chunk_index_and_scheduler; // This type must be uint32_t.
        std::size_t current_overflow_size = private_overflow_pool_.size();

        // Try to empty the overflow buffer, but only those elements
        // that are currently in the buffer. Those that fail to be
        // pushed are put back in the buffer and those are attempted to
        // be pushed the next time around.
        for (std::size_t i = 0; i < current_overflow_size; ++i)
        {
            private_overflow_pool_.pop_back(&chunk_index_and_scheduler);
            core::chunk_index chunk_index = chunk_index_and_scheduler & 0xFFFFFFUL;
            uint32_t scheduler_id = (chunk_index_and_scheduler >> 24) & 0xFFUL;

            // Just getting number of chunks to push.
            SocketDataChunk* sd = (SocketDataChunk*)((uint8_t*)(&shared_int_.chunk(chunk_index)) + bmx::BMX_HEADER_MAX_SIZE_BYTES);

            // Pushing chunk using standard procedure.
            PushLinkedChunksToDb(chunk_index, sd->get_num_chunks(), scheduler_id, false);
        }

        return 0;
    }

    // Push whatever chunks we have to channels.
    void PushLinkedChunksToDb(
        core::chunk_index chunk_index,
        int32_t num_chunks,
        int16_t scheduler_id,
        bool not_overflow_chunk);

    uint32_t PushSocketDataToDb(GatewayWorker* gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_id);

    // Releases chunks from private chunk pool to the shared chunk pool.
    uint32_t ReleaseToSharedChunkPool(int32_t num_chunks);

    // Scans all channels for any incoming chunks.
    uint32_t ScanChannels(GatewayWorker *gw, bool* found_something);

    // Obtains chunk from a private pool if its not empty
    // (otherwise fetches from shared chunk pool).
    uint32_t GetOneChunkFromPrivatePool(core::chunk_index* chunk_index, shared_memory_chunk** smc)
    {
        // Trying to fetch chunk from private pool.
        uint32_t err_code;
        while (!private_chunk_pool_.acquire_linked_chunks_counted(&shared_int_.chunk(0), *chunk_index, 1))
        {
            // Getting chunks from shared chunk pool.
            err_code = AcquireChunksFromSharedPool(MAX_CHUNKS_IN_PRIVATE_POOL);
            GW_ERR_CHECK(err_code);
        }

        // Getting data pointer.
        (*smc) = (shared_memory_chunk *)(&shared_int_.chunk(*chunk_index));

        // Removing possible link to another chunk.
        (*smc)->terminate_link();

#ifdef GW_CHUNKS_DIAG
        GW_COUT << "Getting new chunk: " << *chunk_index << GW_ENDL;
#endif

        return 0;
    }

    // Returns given socket data chunk to private chunk pool.
    void ReturnSocketDataChunksToPool(GatewayWorker *gw, SocketDataChunkRef sd);

    // Returns given linked chunks to private chunk pool (and if needed then to shared).
    void ReturnLinkedChunksToPool(int32_t num_linked_chunks, core::chunk_index& first_linked_chunk);

    // Returns all chunks from private pool to shared.
    void ReturnAllPrivateChunksToSharedPool();

    // Handles management chunks.
    uint32_t HandleManagementChunks(GatewayWorker *gw, shared_memory_chunk* smc);
};

} // namespace network
} // namespace starcounter

#endif // WORKER_DB_INTERFACE_HPP
