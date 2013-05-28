#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
#include "worker_db_interface.hpp"
#include "worker.hpp"
#include "common/macro_definitions.hpp"

namespace starcounter {
namespace network {

// Releases chunks from private chunk pool to the shared chunk pool.
uint32_t WorkerDbInterface::ReleaseToSharedChunkPool(int32_t num_chunks)
{
    // Release chunks from this worker threads private chunk pool to the shared chunk pool.
    int32_t released_chunks_num = shared_int_.release_from_private_to_shared(
        private_chunk_pool_, num_chunks, &shared_int_.client_interface(), 1000);

    // Checking that number of released chunks is correct.
    if (released_chunks_num != num_chunks)
    {
        // Some problem with releasing chunks.
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Problem releasing chunks to shared chunk pool." << GW_ENDL;
#endif
        return SCERRGWCANTRELEASETOSHAREDPOOL;
    }

    // Chunks have been released.
    ChangeNumUsedChunks(-num_chunks);

    return 0;
}

// Scans all channels for any incoming chunks.
uint32_t WorkerDbInterface::ScanChannels(GatewayWorker *gw, uint32_t& next_sleep_interval_ms)
{
    core::chunk_index cur_chunk_index;
    uint32_t err_code;

    // Push everything from overflow pool.
    PushOverflowChunksIfAny();

    // Number of popped chunks on all channels.
    int32_t num_popped_chunks = 0;

    // Indicates if chunk was popped on any channel.
    bool chunk_popped = true;

    // Looping until we have chunks to pop and we don't exceed the maximum number.
    while (chunk_popped && (num_popped_chunks < MAX_CHUNKS_TO_POP_AT_ONCE))
    {
        // Flag will be set if any chunk is popped.
        chunk_popped = false;

        // Running through all channels.
        for (int32_t i = 0; i < num_schedulers_; i++)
        {
            // Obtaining the channel.
            core::channel_type& the_channel = shared_int_.channel(channels_[i]);

            // Trying to pop the chunk from channel.
            if (false == the_channel.out.try_pop_back(&cur_chunk_index))
                continue;

            // Chunk was found.
            chunk_popped = true;
            num_popped_chunks++;

            // A message on channel ch was received. Notify the database
            // that the out queue in this channel is not full.
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
            the_channel.scheduler()->notify(shared_int_.scheduler_work_event
                (the_channel.get_scheduler_number()));
#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
            the_channel.scheduler()->notify();
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.

            // Get the chunk.
            shared_memory_chunk* smc = (shared_memory_chunk*) &(shared_int_.chunk(cur_chunk_index));

            // Check if its a BMX handlers management message.
            if (bmx::BMX_MANAGEMENT_HANDLER_INFO == smc->get_bmx_handler_info())
            {
                // Changing number of database chunks.
                ChangeNumUsedChunks(1);

                // Entering global lock.
                gw->EnterGlobalLock();

                // Handling management chunks.
                err_code = HandleManagementChunks(gw, smc);

                // Releasing management chunks.
                ReturnLinkedChunksToPool(1, cur_chunk_index);

                // Releasing global lock.
                gw->LeaveGlobalLock();

                if (err_code)
                    return err_code;

                continue;
            }

            // Process the chunk.
            SocketDataChunk* sd = (SocketDataChunk*)((uint8_t *)smc + MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA);

            // Setting the database index and sequence number.
            sd->AttachToDatabase(db_index_);

            // Checking for socket data correctness.
            GW_ASSERT(sd->get_socket() < g_gateway.setting_max_connections());

            // Setting chunk index because of possible cloned chunks.
            sd->set_chunk_index(cur_chunk_index);

            // Resetting number of chunks.
            sd->set_num_chunks(1);

            ActiveDatabase* current_db = g_gateway.GetDatabase(db_index_);

            // We need to check if its a multi-chunk response.
            if (!smc->is_terminated())
            {
                // Creating special chunk for keeping WSA buffers information there.
                sd->CreateWSABuffers(
                    this,
                    smc,
                    MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA + sd->get_user_data_offset_in_socket_data(),
                    MixedCodeConstants::SOCKET_DATA_MAX_SIZE - sd->get_user_data_offset_in_socket_data(),
                    sd->get_user_data_written_bytes());

                GW_ASSERT(sd->get_num_chunks() > 2);

                // NOTE: One chunk for WSA buffers will already be counted.
                ChangeNumUsedChunks(-1);
            }

            // Changing number of used chunks.
            ChangeNumUsedChunks(sd->get_num_chunks());

#ifdef GW_CHUNKS_DIAG
            GW_PRINT_WORKER << "Popping chunk: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

#ifndef GW_NEW_SESSIONS_APPROACH

            session_index_type gw_session_index = sd->get_session_index();
            session_salt_type gw_session_salt = sd->get_session_salt();
            apps_unique_session_num_type apps_session_num = sd->get_apps_unique_session_num();
            session_salt_type apps_session_salt = sd->get_apps_session_salt();

            // Checking if new session was generated.
            if (INVALID_SESSION_INDEX != gw_session_index)
            {
                // Getting copy of a global session.
                ScSessionStruct global_session_copy = g_gateway.GetGlobalSessionCopy(gw_session_index);

                // Checking if Apps unique number is valid.
                if (INVALID_APPS_UNIQUE_SESSION_NUMBER != sd->get_apps_unique_session_num())
                {
                    // Checking if session exists.
                    // Checking if session salt is correct.
                    if (!global_session_copy.CompareSalts(gw_session_salt))
                    {
#ifdef GW_SESSIONS_DIAG
                        GW_PRINT_WORKER << "Wrong session attached to socket: " << gw_session_salt << GW_ENDL;
#endif
                        // Killing session number for this Apps.
                        current_db->SetAppsSessionValue(gw_session_index, INVALID_APPS_UNIQUE_SESSION_NUMBER, INVALID_SESSION_SALT);

                        // The session was killed.
                        sd->ResetSdSession();
                    }
                    else
                    {
                        // Updating session time stamp.
                        g_gateway.SetSessionTimeStamp(gw_session_index);
                    }
                }
                else
                {
                    // Killing session number for this Apps session.
                    current_db->SetAppsSessionValue(gw_session_index, INVALID_APPS_UNIQUE_SESSION_NUMBER, INVALID_SESSION_SALT);

                    // Checking if session salt is correct.
                    if (!global_session_copy.CompareSalts(gw_session_salt))
                    {
#ifdef GW_SESSIONS_DIAG
                        GW_PRINT_WORKER << "Trying to kill a wrong session: " << gw_session_index << ":" << gw_session_salt << GW_ENDL;
#endif
                        // Resetting the socket data session.
                        sd->ResetSdSession();
                    }
                    else
                    {
                        // Killing global session.
                        bool session_was_killed;
                        sd->KillGlobalAndSdSession(&session_was_killed);
                    }
                }
            }
            else
            {
                // Checking if unique Apps number was supplied.
                if (INVALID_APPS_UNIQUE_SESSION_NUMBER != sd->get_apps_unique_session_num())
                {
                    // Session is newly created.
                    sd->set_new_session_flag(true);

                    // Creating new session with this salt and scheduler id.
                    ScSessionStruct new_session_copy = g_gateway.GenerateNewSessionAndReturnCopy(
                        gw->get_random()->uint64(),
                        sd->get_apps_unique_session_num(),
                        sd->get_apps_session_salt(),
                        i);

                    // Checking if session was generated successfully.
                    if (new_session_copy.IsValid())
                    {
                        // Attaching the session.
                        sd->AssignSession(new_session_copy);

                        // Updating session number for this Apps.
                        current_db->SetAppsSessionValue(
                            new_session_copy.gw_session_index_,
                            sd->get_apps_unique_session_num(),
                            sd->get_apps_session_salt());
                    }
                    else
                    {
#ifdef GW_SESSIONS_DIAG
                        GW_PRINT_WORKER << "Newly created session is invalid: " << new_session_copy.gw_session_index_ <<
                            ":" << new_session_copy.gw_session_salt_ << GW_ENDL;
#endif
                    }
                }
            }

#endif

            // Checking if we have session related socket.
            sd->SetGlobalSessionIfEmpty();

            // Resetting the accumulative buffer.
            sd->InitAccumBufferFromUserData();

            // Put the chunk into from database queue.
            err_code = gw->RunFromDbHandlers(sd);

            // Checking if any error occurred during socket operations.
            if (err_code)
            {
                // Disconnecting this socket data.
                gw->DisconnectAndReleaseChunk(sd);

                // Releasing the cloned chunk.
                gw->ProcessReceiveClones(true);
            }
            else
            {
                // Processing clones during last iteration.
                gw->ProcessReceiveClones(false);
            }
        }
    }

    // Checking if any chunks were popped.
    if (num_popped_chunks > 0)
        next_sleep_interval_ms = 0;

    return 0;
}

// Writes given big linear buffer into obtained linked chunks.
uint32_t WorkerDbInterface::WriteBigDataToChunks(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index cur_chunk_index,
    uint32_t* actual_written_bytes,
    uint32_t first_chunk_offset,
    bool just_sending_flag
    )
{
    // Maximum number of bytes that will be written in this call.
    uint32_t num_bytes_to_write = buf_len_bytes;
    uint32_t num_bytes_first_chunk = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES - first_chunk_offset;

    // Number of chunks to use.
    uint32_t num_extra_chunks_to_use = ((buf_len_bytes - num_bytes_first_chunk) / starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES) + 1;
    assert(num_extra_chunks_to_use > 0);

    // Checking if more than maximum chunks we can take at once.
    if (num_extra_chunks_to_use > starcounter::bmx::MAX_EXTRA_LINKED_WSABUFS)
    {
        num_extra_chunks_to_use = starcounter::bmx::MAX_EXTRA_LINKED_WSABUFS;
        num_bytes_to_write = starcounter::bmx::MAX_BYTES_EXTRA_LINKED_WSABUFS + num_bytes_first_chunk;
    }

    // Acquiring linked chunks.
    starcounter::core::chunk_index new_chunk_index;
    uint32_t err_code = GetMultipleChunksFromPrivatePool(
        &new_chunk_index,
        num_extra_chunks_to_use);

    if (err_code)
        return err_code;

    // Getting chunk memory address.
    uint8_t* cur_chunk_buf = (uint8_t *)(&shared_int_.chunk(cur_chunk_index));

    // Linking to the first chunk.
    ((shared_memory_chunk*)cur_chunk_buf)->set_link(new_chunk_index);

    // Checking if we should just send the chunks.
    if (just_sending_flag)
        (*(uint32_t*)(cur_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS)) |= starcounter::MixedCodeConstants::SOCKET_DATA_FLAGS_JUST_SEND;

    // Setting the number of written bytes.
    *(uint32_t*)(cur_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES) = num_bytes_to_write;

    // Going through each linked chunk and write data there.
    uint32_t left_bytes_to_write = num_bytes_to_write;
    uint32_t num_bytes_to_write_in_chunk = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;

    // Writing to first chunk.
    memcpy(cur_chunk_buf + first_chunk_offset, buf, num_bytes_first_chunk);
    left_bytes_to_write -= num_bytes_first_chunk;

    // Checking how many bytes to write next time.
    if (left_bytes_to_write < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
    {
        // Checking if we copied everything.
        if (left_bytes_to_write <= 0)
        {
            // Setting number of total written bytes.
            *actual_written_bytes = num_bytes_to_write;

            return 0;
        }

        num_bytes_to_write_in_chunk = left_bytes_to_write;
    }

    // Processing until have some bytes to write.
    while (true)
    {
        // Getting next chunk in chain.
        cur_chunk_index = ((shared_memory_chunk*)cur_chunk_buf)->get_link();

        // Getting chunk memory address.
        cur_chunk_buf = (uint8_t *)(&shared_int_.chunk(cur_chunk_index));

        // Copying memory.
        memcpy(cur_chunk_buf, buf + num_bytes_to_write - left_bytes_to_write, num_bytes_to_write_in_chunk);
        left_bytes_to_write -= num_bytes_to_write_in_chunk;

        // Checking how many bytes to write next time.
        if (left_bytes_to_write < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
        {
            // Checking if we copied everything.
            if (left_bytes_to_write <= 0)
                break;

            num_bytes_to_write_in_chunk = left_bytes_to_write;
        }
    }

    // Setting number of total written bytes.
    *actual_written_bytes = num_bytes_to_write;

    return 0;
}

// Push given chunk to database queue.
void WorkerDbInterface::PushLinkedChunksToDb(
    core::chunk_index the_chunk_index,
    int32_t stats_num_chunks,
    int16_t scheduler_id)
{
    // Assuring that session goes to correct scheduler.
    if (scheduler_id >= num_schedulers_)
    {
        // Returning linked multiple chunks.
        ReturnLinkedChunksToPool(stats_num_chunks, the_chunk_index);

        return;
    }

    // Obtaining the channel.
    core::channel_type& the_channel = shared_int_.channel(channels_[scheduler_id]);

    // Trying to push chunk if overflow is empty.
    if ((!the_channel.in_overflow().empty()) ||
        (!TryPushToChannel(the_channel, the_chunk_index, stats_num_chunks)))
    {
#ifdef GW_CHUNKS_DIAG
        GW_PRINT_WORKER << "Couldn't push chunk into channel. Putting to overflow pool." << GW_ENDL;
#endif

        // The in_overflow queue is not empty so the message is first pushed to
        // the in_overflow queue, to preserve the order of production.
        the_channel.in_overflow().push(the_chunk_index);
    }
}

// Returns given socket data chunk to private chunk pool.
void WorkerDbInterface::ReturnSocketDataChunksToPool(GatewayWorker* gw, SocketDataChunkRef sd)
{
#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Returning chunk: " << sd->get_socket() << ":" << sd->get_chunk_index() << GW_ENDL;
#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS
    GW_ASSERT(sd->get_socket_diag_active_conn_flag() == false);
#endif

    // Returning linked multiple chunks.
    ReturnLinkedChunksToPool(sd->get_num_chunks(), sd->get_chunk_index());

    // IMPORTANT: Preventing further usages of this socket data.
    sd = NULL;
}

// Returns given chunk to private chunk pool.
// NOTE: This function should always succeed.
void WorkerDbInterface::ReturnLinkedChunksToPool(int32_t num_linked_chunks, core::chunk_index& first_linked_chunk)
{
    // Releasing chunk to private pool.
    bool success = private_chunk_pool_.release_linked_chunks(&shared_int_.chunk(0), first_linked_chunk);
    GW_ASSERT(success == true);

    // Check if there are too many private chunks so
    // we need to release them to the shared chunk pool.
    if (private_chunk_pool_.size() > MAX_CHUNKS_IN_PRIVATE_POOL_DOUBLE)
    {
        uint32_t err_code = ReleaseToSharedChunkPool(private_chunk_pool_.size() - MAX_CHUNKS_IN_PRIVATE_POOL);

        // NOTE: The error can happen when for example the database is already dead
        // but for the future with centralized chunk pool this should never happen!

        // TODO: Uncomment when centralized chunks are ready!
        //GW_ASSERT(0 == err_code);
    }
}

// Releases all private chunks to shared chunk pool.
void WorkerDbInterface::ReturnAllPrivateChunksToSharedPool()
{
    // Checking if there are any chunks in private pool.
    if (private_chunk_pool_.size() > 0)
    {
        uint32_t err_code = ReleaseToSharedChunkPool(private_chunk_pool_.size());

        // NOTE: The error can happen when for example the database is already dead
        // but for the future with centralized chunk pool this should never happen!

        // TODO: Uncomment when centralized chunks are ready!
        //GW_ASSERT(0 == err_code);
    }
}

// Push given chunk to database queue.
uint32_t WorkerDbInterface::PushSocketDataToDb(
    GatewayWorker* gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE user_handler_id)
{
#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Pushing chunk: socket " << sd->get_socket() << ":" << sd->get_chunk_index() << " handler_id " << user_handler_id << GW_ENDL;
#endif

    // Checking if chunk belongs to this database.
    ActiveDatabase* current_db = g_gateway.GetDatabase(db_index_);
    if ((current_db->get_unique_num()) != sd->get_db_unique_seq_num())
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Socket data does not belong to this database." << GW_ENDL;
#endif
        return SCERRGWSOCKETDATAWRONGDATABASE;
    }

#ifndef GW_NEW_SESSIONS_APPROACH

    // Setting the Apps session number right before sending to that Apps (if session exists at all).
    if (INVALID_SESSION_INDEX != sd->get_session_index())
    {
        session_index_type session_index = sd->get_session_index();

        // Updating session time stamp.
        g_gateway.SetSessionTimeStamp(session_index);

        // Setting Apps specific information.
        sd->set_apps_unique_session_num(current_db->GetAppsUniqueSessionNumber(session_index));
        sd->set_apps_session_salt(current_db->GetAppsSessionSalt(session_index));
    }

    // Obtaining the current scheduler id.
    uint8_t sched_id = g_gateway.GetGlobalSessionSchedulerId(sd->get_session_index());

#endif

    // Obtaining the current scheduler id.
    scheduler_id_type sched_id = sd->get_scheduler_id();

    // Modifying chunk data to use correct handler.
    shared_memory_chunk *smc = (shared_memory_chunk*) &(shared_int_.chunk(sd->get_chunk_index()));
    smc->set_bmx_handler_info(user_handler_id); // User code handler id.
    smc->set_request_size(4);

    // Checking scheduler id validity.
    if (INVALID_SCHEDULER_ID == sched_id)
        sched_id = GetSchedulerId();

    // Setting scheduler id to session.
    sd->set_scheduler_id(sched_id);

    // Pushing socket data as a chunk.
    PushLinkedChunksToDb(sd->get_chunk_index(), sd->get_num_chunks(), sched_id);

    return 0;
}

// Registers push channel.
uint32_t WorkerDbInterface::RegisterPushChannel(int32_t sched_num)
{
    // Get a reference to the chunk.
    shared_memory_chunk *smc = NULL;

    // Getting a free chunk.
    core::chunk_index new_chunk;
    uint32_t err_code = GetOneChunkFromPrivatePool(&new_chunk, &smc);
    GW_ERR_CHECK(err_code);

    // Predefined BMX management handler.
    smc->set_bmx_handler_info(bmx::BMX_MANAGEMENT_HANDLER_INFO);

    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX message type.
    request->write(bmx::BMX_REGISTER_PUSH_CHANNEL);

    // Pushing the chunk.
    PushLinkedChunksToDb(new_chunk, 1, sched_num);

    return 0;
}

// Pushes session destroyed message.
uint32_t WorkerDbInterface::PushSessionDestroy(
    session_index_type linear_index,
    session_salt_type random_salt,
    uint8_t scheduler_id)
{
    // Get a reference to the chunk.
    shared_memory_chunk *smc = NULL;

    // Getting a free chunk.
    core::chunk_index new_chunk;
    uint32_t err_code = GetOneChunkFromPrivatePool(&new_chunk, &smc);
    GW_ERR_CHECK(err_code);

    // Predefined BMX management handler.
    smc->set_bmx_handler_info(bmx::BMX_MANAGEMENT_HANDLER_INFO);

    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX message type.
    request->write(bmx::BMX_SESSION_DESTROY);

    // Writing Apps unique session number.
    request->write(linear_index);

    // Writing Apps unique salt.
    request->write(random_salt);

    // Pushing the chunk.
    PushLinkedChunksToDb(new_chunk, 1, scheduler_id);

    return 0;
}

// Sends session create message.
uint32_t WorkerDbInterface::PushSessionCreate(SocketDataChunkRef sd)
{
    // Get a reference to the chunk.
    shared_memory_chunk *smc = sd->get_smc();

    // Predefined BMX management handler.
    smc->set_bmx_handler_info(bmx::BMX_MANAGEMENT_HANDLER_INFO);

    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX message type.
    request->write(bmx::BMX_SESSION_CREATE);

    // Obtaining the current scheduler id.
    scheduler_id_type sched_id = sd->get_scheduler_id();

    // Checking scheduler id validity.
    if (INVALID_SCHEDULER_ID == sched_id)
        sched_id = GetSchedulerId();

    // Pushing the chunk.
    PushLinkedChunksToDb(sd->get_chunk_index(), 1, sched_id);

    return 0;
}

// Requesting previously registered handlers.
uint32_t WorkerDbInterface::RequestRegisteredHandlers(int32_t sched_num)
{
    // Get a reference to the chunk.
    shared_memory_chunk *smc = NULL;

    // Getting a free chunk.
    core::chunk_index new_chunk; 
    uint32_t err_code = GetOneChunkFromPrivatePool(&new_chunk, &smc);
    GW_ERR_CHECK(err_code);

    // Filling the chunk as BMX management handler.
    smc->set_bmx_handler_info(bmx::BMX_MANAGEMENT_HANDLER_INFO);

    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX message type.
    request->write(bmx::BMX_SEND_ALL_HANDLERS);

    // Pushing the chunk.
    PushLinkedChunksToDb(new_chunk, 1, sched_num);

    return 0;
}

// Allocates different channels and pools.
WorkerDbInterface::WorkerDbInterface(
    const int32_t new_db_index,
    const core::shared_interface& shared_int,
    const int32_t worker_id)
{
	channels_ = 0;

    Reset();

    // Allocating channels.
    num_schedulers_ = shared_int.common_scheduler_interface().number_of_active_schedulers();
    channels_ = new core::channel_number[num_schedulers_];

    // Setting private/overflow chunk pool capacity.
    private_chunk_pool_.set_capacity(core::chunks_total_number_max);

    db_index_ = new_db_index;
    worker_id_ = worker_id;
    shared_int_ = shared_int;

    // Getting unique client interface for this worker.
    bool shared_int_acquired = shared_int_.acquire_client_number();
    GW_ASSERT(true == shared_int_acquired);

#ifdef GW_DATABASES_DIAG
    // Diagnostics.
    GW_PRINT_WORKER << "Database \"" << g_gateway.GetDatabase(db_index_)->get_db_name() <<
        "\" acquired client interface " << shared_int_.get_client_number() << " and " << num_schedulers_ << " channel(s): ";
#endif

    // Acquiring unique channel for each scheduler.
    for (std::size_t i = 0; i < num_schedulers_; ++i)
    {
        bool channel_acquired = shared_int_.acquire_channel(&channels_[i], i);
        GW_ASSERT(true == channel_acquired);

#ifdef GW_DATABASES_DIAG
        GW_COUT << channels_[i] << ", ";
#endif
    }

#ifdef GW_DATABASES_DIAG
    GW_COUT << GW_ENDL;
#endif

}

// Registers push channels on all schedulers.
uint32_t WorkerDbInterface::RegisterAllPushChannels()
{
    uint32_t err_code;

    // Sending push channel on each scheduler.
    for (int32_t i = 0; i < num_schedulers_; ++i)
    {
        // Pushing registration chunk.
        err_code = RegisterPushChannel(i);
        GW_ERR_CHECK(err_code);

#ifdef GW_DATABASES_DIAG
        GW_PRINT_WORKER << "Registered push channel on scheduler: " << i << GW_ENDL;
#endif
    }

    // Sending 100 pings.
    /*for (uint64_t i = 0; i < 100; i++)
    {
        shared_memory_chunk* smc;
        core::chunk_index new_chunk;
        err_code = GetOneChunkFromPrivatePool(&new_chunk, &smc);
        GW_ERR_CHECK(err_code);

        sc_bmx_construct_ping(i, smc);
        PushLinkedChunksToDb(new_chunk, 1, 0);
    }*/

    return 0;
}

// Requests registered user handlers.
uint32_t WorkerDbInterface::RequestRegisteredHandlers()
{
    uint32_t err_code;

    // Asking for existing handlers on scheduler 0.
    err_code = RequestRegisteredHandlers(0);
    GW_ERR_CHECK(err_code);

#ifdef GW_DATABASES_DIAG
    GW_PRINT_WORKER << "Requested registered handlers." << GW_ENDL;
#endif

    return 0;
}

// Handles management chunks.
uint32_t WorkerDbInterface::HandleManagementChunks(GatewayWorker *gw, shared_memory_chunk* smc)
{
    // Getting the response part of the chunk.
    response_chunk_part* resp_chunk = smc->get_response_chunk();
    uint32_t response_size = resp_chunk->get_offset();
    if (0 == response_size)
        return SCERRGWBMXCHUNKWRONGFORMAT;

    uint32_t err_code = 0;
    uint32_t offset = 0;

    resp_chunk->reset_offset();
    while (offset < response_size)
    {
        // Reading BMX message type.
        uint8_t bmx_type = resp_chunk->read_uint8();

        switch (bmx_type)
        {
            case bmx::BMX_PONG:
            {
                uint64_t ping_data = -1;
                
                // Jumping over 8 bytes because we reseted the offset.
                resp_chunk->skip(8);

                err_code = sc_bmx_parse_pong(smc, &ping_data);
                if (err_code)
                    return err_code;

                GW_PRINT_WORKER << "Pong with data: " << ping_data << GW_ENDL;

                return 0;
            }

            case bmx::BMX_REGISTER_PUSH_CHANNEL_RESPONSE:
            {
                // We have received a confirmation push channel chunk.
                g_gateway.GetDatabase(db_index_)->ReceivedPushChannelConfirmation();

                // Checking if we have all push channels confirmed from database.
                if (num_schedulers_ == g_gateway.GetDatabase(db_index_)->get_num_confirmed_push_channels())
                {
                    GW_PRINT_WORKER << "All push channels confirmed!" << GW_ENDL;

                    // Requesting all registered handlers.
                    //err_code = RequestRegisteredHandlers();
                    //if (err_code)
                    //    return err_code;
                }

                return 0;
            }

            case bmx::BMX_REGISTER_PORT:
            {
                // Reading handler info.
                BMX_HANDLER_TYPE handler_info = resp_chunk->read_handler_info();

                // Reading port number.
                uint16_t port = resp_chunk->read_uint16();

#ifdef GW_TESTING_MODE
                if ((g_gateway.setting_mode() != GatewayTestingMode::MODE_GATEWAY_SMC_RAW) &&
                    (g_gateway.setting_mode() != GatewayTestingMode::MODE_GATEWAY_SMC_APPS_RAW))
                {
                    GW_ASSERT(false);
                }
#endif

                GW_PRINT_WORKER << "New port " << port << " user handler registration with handler id: " << handler_info << GW_ENDL;

                // Registering handler on active database.
                HandlersTable* handlers_table = g_gateway.GetDatabase(db_index_)->get_user_handlers();

                // Registering handler on active database.
                err_code = g_gateway.AddPortHandler(
                    gw,
                    handlers_table,
                    port,
                    handler_info,
                    db_index_,
                    AppsPortProcessData);

                break;
            }
            
            case bmx::BMX_REGISTER_PORT_SUBPORT:
            {
                // Reading handler info.
                BMX_HANDLER_TYPE handler_info = resp_chunk->read_handler_info();

                // Reading port number.
                uint16_t port = resp_chunk->read_uint16();

                // Reading subport.
                bmx::BMX_SUBPORT_TYPE subport = resp_chunk->read_uint32();

                GW_PRINT_WORKER << "New subport " << subport << " port " << port << " user handler registration with handler id: " << handler_info << GW_ENDL;
                
                // Registering handler on active database.
                HandlersTable* handlers_table = g_gateway.GetDatabase(db_index_)->get_user_handlers();

                // Registering handler on active database.
                err_code = g_gateway.AddSubPortHandler(
                    gw,
                    handlers_table,
                    port,
                    subport,
                    handler_info,
                    db_index_,
                    AppsSubportProcessData);

                break;
            }

            case bmx::BMX_REGISTER_URI:
            {
                // Reading handler info.
                BMX_HANDLER_TYPE handler_info = resp_chunk->read_handler_info();

                // Reading port number.
                uint16_t port = resp_chunk->read_uint16();

                // Reading URIs.
                char original_uri_info[MixedCodeConstants::MAX_URI_STRING_LEN];
                uint32_t original_uri_info_len_chars = resp_chunk->read_uint32();
                resp_chunk->read_string(original_uri_info, original_uri_info_len_chars, MixedCodeConstants::MAX_URI_STRING_LEN);

                char processed_uri_info[MixedCodeConstants::MAX_URI_STRING_LEN];
                uint32_t processed_uri_info_len_chars = resp_chunk->read_uint32();
                resp_chunk->read_string(processed_uri_info, processed_uri_info_len_chars, MixedCodeConstants::MAX_URI_STRING_LEN);

                // Reading number of parameters.
                uint8_t num_params = resp_chunk->read_uint8();

                // Reading parameter types.
                uint8_t param_types[MixedCodeConstants::MAX_URI_CALLBACK_PARAMS];
                resp_chunk->copy_data_to_buffer(param_types, MixedCodeConstants::MAX_URI_CALLBACK_PARAMS);

                // Reading protocol type.
                MixedCodeConstants::NetworkProtocolType proto_type = (MixedCodeConstants::NetworkProtocolType)resp_chunk->read_uint8();

#ifdef GW_TESTING_MODE

                HttpTestInformation* http_test_info = g_gateway.GetHttpTestInformation();

                // Comparing URI with current test URI.
                if (!http_test_info || strcmp(original_uri_info, http_test_info->method_and_uri_info))
                    break;
#endif

                GW_PRINT_WORKER << "New URI handler \"" << processed_uri_info << "\" on port " << port << " registration with handler id: " << handler_info << GW_ENDL;

                // Registering handler on active database.
                HandlersTable* handlers_table = g_gateway.GetDatabase(db_index_)->get_user_handlers();

                // Registering determined URI Apps handler.
                err_code = g_gateway.AddUriHandler(
                    gw,
                    handlers_table,
                    port,
                    original_uri_info,
                    original_uri_info_len_chars,
                    processed_uri_info,
                    processed_uri_info_len_chars,
                    param_types,
                    num_params,
                    handler_info,
                    db_index_,
                    AppsUriProcessData);

                break;
            }

            case bmx::BMX_UNREGISTER:
            {
                // Reading handler id.
                BMX_HANDLER_TYPE handler_info = resp_chunk->read_handler_info();

                GW_PRINT_WORKER << "User handler unregistration for handler id: " << handler_info << GW_ENDL;

                // Getting handlers list.
                HandlersTable* handlers_table = g_gateway.GetDatabase(db_index_)->get_user_handlers();
                HandlersList* handlers_list = handlers_table->FindHandler(handler_info);
                GW_ASSERT(handlers_list != NULL);

                // Removing handler with certain id.
                err_code = handlers_table->UnregisterHandler(handler_info);

                // Removing from global data structures.
                ServerPort* server_port = g_gateway.FindServerPort(handlers_list->get_port());
                GW_ASSERT(server_port != NULL);

                // Removing this handler from all possible places.
                server_port->get_registered_uris()->RemoveEntry(handlers_list);
                server_port->get_registered_subports()->RemoveEntry(handlers_list);
                server_port->get_port_handlers()->RemoveEntry(handlers_list);

                // Checking if server port became empty.
                if (server_port->IsEmpty())
                    server_port->Erase();

                return 0;
            }

            case bmx::BMX_SESSION_DESTROY:
            {
                GW_ASSERT(false);
                break;
            }

            case bmx::BMX_SESSION_CREATE:
            {
                GW_ASSERT(false);
                break;
            }

            default:
            {
                return SCERRGWWRONGBMXCHUNKTYPE;
            }
        }

        // Checking for error code after registrations.
        if (err_code)
        {
            switch (err_code)
            {
            case SCERRGWFAILEDTOBINDPORT:
                // Ignore.
                break;

            default:
                return err_code;
            }
        }

        offset = resp_chunk->get_offset();
    }

    return 0;
}

} // namespace network
} // namespace starcounter