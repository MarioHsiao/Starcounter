#pragma once
#ifndef WORKER_DB_INTERFACE_HPP
#define WORKER_DB_INTERFACE_HPP

namespace starcounter {
namespace network {
    
class GatewayWorker;
class WorkerDbInterface
{
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
            GW_COUT << "Problem acquiring chunks from shared chunk pool." << std::endl;
#endif
            return SCERRUNSPECIFIED;
        }

        return 0;
    }


    // Registers push channel.
    uint32_t RegisterPushChannel(int32_t sched_num);

    // Requesting previously registered handlers.
    uint32_t RequestRegisteredHandlers(int32_t sched_num);

public:

    // Getting shared interface.
    core::shared_interface* get_shared_int()
    {
        return &shared_int_;
    }

    // Index into databases array.
    uint16_t db_slot_index()
    {
        return db_index_;
    }

    // Registers all push channels.
    uint32_t RegisterAllPushChannels();

    // Request registered user handlers.
    uint32_t RequestRegisteredHandlers();

    // Initializes shared memory interface.
    uint32_t Init(
        const int32_t new_slot_index,
        const core::shared_interface& workerSharedInt,
        GatewayWorker *gw);

    // Resets the existing interface.
    void Reset()
    {
        db_index_ = -1;
        worker_id_ = -1;

        if (channels_)
        {
            delete[] channels_;
            channels_ = NULL;
        }
    }

    // Allocates different channels and pools.
    WorkerDbInterface()
    {
        channels_ = NULL;
        Reset();

        // Allocating channels.
        channels_ = new core::channel_number[g_gateway.get_num_schedulers()];

        // Setting private/overflow chunk pool capacity.
        private_chunk_pool_.set_capacity(core::chunks_total_number_max);
        private_overflow_pool_.set_capacity(core::chunks_total_number_max);
    }

    // Deallocates active database.
    ~WorkerDbInterface()
    {
        // Freeing all occupied channels.
        for (std::size_t i = 0; i < g_gateway.get_num_schedulers(); i++)
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

        uint32_t chunk_index_and_sched; // This type must be uint32_t.
        std::size_t current_overflow_size = private_overflow_pool_.size();

        // Try to empty the overflow buffer, but only those elements
        // that are currently in the buffer. Those that fail to be
        // pushed are put back in the buffer and those are attempted to
        // be pushed the next time around.
        for (std::size_t i = 0; i < current_overflow_size; ++i)
        {
            private_overflow_pool_.pop_back(&chunk_index_and_sched);
            core::chunk_index chunk_index = chunk_index_and_sched & 0xFFFFFFUL;
            uint32_t sched_num = (chunk_index_and_sched >> 24) & 0xFFUL;

            // Pushing chunk using standard procedure.
            uint32_t errCode = PushLinkedChunksToDb(chunk_index, 0, sched_num, false);
            GW_ERR_CHECK(errCode);
        }

        return 0;
    }

    // Push whatever chunks we have to channels.
    uint32_t PushLinkedChunksToDb(
        core::chunk_index chunk_index,
        int32_t num_chunks,
        int32_t sched_id,
        bool not_overflow_chunk);

    uint32_t PushSocketDataToDb(GatewayWorker* gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id);

    // Releases chunks from private chunk pool to the shared chunk pool.
    uint32_t ReleaseToSharedChunkPool(int32_t num_chunks);

    // Scans all channels for any incoming chunks.
    uint32_t ScanChannels(GatewayWorker *gw);

    // Obtains chunk from a private pool if its not empty
    // (otherwise fetches from shared chunk pool).
    core::chunk_index GetOneChunkFromPrivatePool(shared_memory_chunk **chunk_data)
    {
        // Pop chunk index from private chunk pool.
        core::chunk_index chunk_index;

        // Trying to fetch chunk from private pool.
        uint32_t err_code;
        while (!private_chunk_pool_.acquire_linked_chunks_counted(&shared_int_.chunk(0), chunk_index, 1))
        {
            // Getting chunks from shared chunk pool.
            err_code = AcquireChunksFromSharedPool(ACCEPT_ROOF_STEP_SIZE);
            GW_ERR_CHECK(err_code);
        }

        // Chunk has been acquired.
        g_gateway.GetDatabase(db_index_)->ChangeNumUsedChunks(1);

        // Getting data pointer.
        (*chunk_data) = (shared_memory_chunk *)(&shared_int_.chunk(chunk_index));

        // Removing possible link to another chunk.
        (*chunk_data)->terminate_link();

#ifdef GW_CHUNKS_DIAG
        GW_COUT << "Getting new chunk: " << chunk_index << std::endl;
#endif

        return chunk_index;
    }

    // Obtains needed linked chunks from a private pool if its not empty
    // (otherwise fetches them from shared chunk pool).
    core::chunk_index GetLinkedChunksFromPrivatePool(uint32_t num_bytes)
    {
        // Pop chunk index from private chunk pool.
        core::chunk_index chunk_index;

        // Determining number of chunks needed.
        uint32_t num_chunks_needed = num_bytes / starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK;

        // Trying to fetch chunk from private pool.
        uint32_t err_code;
        while (!private_chunk_pool_.acquire_linked_chunks(&shared_int_.chunk(0), chunk_index, num_bytes))
        {
            // Getting chunks from shared chunk pool.
            err_code = AcquireChunksFromSharedPool(num_chunks_needed);
            GW_ERR_CHECK(err_code);
        }

        // Chunks have been acquired.
        g_gateway.GetDatabase(db_index_)->ChangeNumUsedChunks(num_chunks_needed);

        // Getting data pointer.
        //(*chunk_data) = (shared_memory_chunk *)(&shared_int_.chunk(chunk_index));

#ifdef GW_CHUNKS_DIAG
        GW_COUT << "Getting new linked chunks: " << chunk_index << std::endl;
#endif

        return chunk_index;
    }

    // Returns given socket data chunk to private chunk pool.
    uint32_t ReturnChunkToPool(GatewayWorker *gw, SocketDataChunk *sd);

    // Returns given linked chunks to private chunk pool (and if needed then to shared).
    uint32_t ReturnLinkedChunksToPool(int32_t num_linked_chunks, core::chunk_index& chunk_index);
    uint32_t ReturnLinkedChunksToPool(shared_memory_chunk* smc, core::chunk_index& chunk_index);

    // Handles management chunks.
    uint32_t HandleManagementChunks(GatewayWorker *gw, shared_memory_chunk* smc);
};

} // namespace network
} // namespace starcounter

#endif // WORKER_DB_INTERFACE_HPP
