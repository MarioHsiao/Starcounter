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
        GW_PRINT_WORKER << "Problem releasing chunks to shared chunk pool." << std::endl;
#endif
        return SCERRUNSPECIFIED;
    }

    return 0;
}

// Scans all channels for any incoming chunks.
uint32_t WorkerDbInterface::ScanChannels(GatewayWorker *gw, bool* found_something)
{
    core::chunk_index chunk_index;
    uint32_t errCode;

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
        for (int32_t i = 0; i < g_gateway.get_num_schedulers(); i++)
        {
            // Obtaining the channel.
            core::channel_type& the_channel = shared_int_.channel(channels_[i]);

            // Trying to pop the chunk from channel.
            if (false == the_channel.out.try_pop_back(&chunk_index))
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
            shared_memory_chunk* smc = (shared_memory_chunk*) &(shared_int_.chunk(chunk_index));

            // Check if its a BMX handlers management message.
            if (bmx::BMX_MANAGEMENT_HANDLER == smc->get_bmx_protocol())
            {
                // Changing number of owned chunks.
                g_gateway.GetDatabase(db_index_)->ChangeNumUsedChunks(1);

                // Entering global lock.
                gw->EnterGlobalLock();

                // Handling management chunks.
                errCode = HandleManagementChunks(gw, smc);
                GW_ERR_CHECK(errCode);

                // Releasing global lock.
                gw->LeaveGlobalLock();

                // Releasing management chunks.
                ReturnLinkedChunksToPool(smc, chunk_index);

                continue;
            }

            // Process the chunk.
            SocketDataChunk *sd = (SocketDataChunk *)((uint8_t *)smc + bmx::BMX_HEADER_MAX_SIZE_BYTES);

            // Setting chunk index because of possible cloned chunks.
            sd->set_chunk_index(chunk_index);

            // Resetting number of chunks.
            sd->set_num_chunks(1);

            // We need to check if its a multi-chunk response.
            if (!smc->is_terminated())
                sd->CreateWSABuffers(this, smc, 0, 0, sd->get_user_data_written_bytes());

            // Changing number of used chunks.
            ActiveDatabase* current_db = g_gateway.GetDatabase(db_index_);
            current_db->ChangeNumUsedChunks(sd->get_num_chunks());

            // Checking that corresponding database and handler are up.
            if (!sd->CheckSocketIsValid(gw))
                continue;

#ifdef GW_CHUNKS_DIAG
            GW_PRINT_WORKER << "Popping chunk: socket " << sd->sock() << " chunk " << sd->get_chunk_index() << std::endl;
#endif

            // Checking if new session was generated.
            session_index_type session_index = sd->get_session_index();
            if (INVALID_SESSION_INDEX != session_index)
            {
                // Checking if Apps unique number is valid.
                if (INVALID_APPS_UNIQUE_SESSION_NUMBER != sd->get_apps_unique_session_num())
                {
                    ScSessionStruct* session = g_gateway.GetSessionData(session_index);
                    if (!session->CompareSalts(sd->get_session_salt()))
                    {
#ifdef GW_SESSIONS_DIAG
                        GW_PRINT_WORKER << "Wrong session attached to socket: " << sd->get_session_salt() << std::endl;
#endif
                        // Killing session number for this Apps.
                        current_db->SetAppsSessionValue(session_index, INVALID_APPS_UNIQUE_SESSION_NUMBER);

                        // The session was killed.
                        sd->ResetSession();
                    }
                }
                else
                {
                    // Killing session number for this Apps session.
                    current_db->SetAppsSessionValue(session_index, INVALID_APPS_UNIQUE_SESSION_NUMBER);

                    // Killing session only if its the same.
                    ScSessionStruct* session = g_gateway.GetSessionData(session_index);
                    if (!session->CompareSalts(sd->get_session_salt()))
                    {
#ifdef GW_SESSIONS_DIAG
                        GW_PRINT_WORKER << "Trying to kill a wrong session: " << session_index << ":" << sd->get_session_salt() << std::endl;
#endif
                    }
                    else
                    {
                        // Killing global session.
                        sd->KillSession();

#ifdef GW_SESSIONS_DIAG
                        GW_PRINT_WORKER << "Session was killed: " << session_index << ":" << sd->get_session_salt() << std::endl;
#endif
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
                    ScSessionStruct* new_session = g_gateway.GenerateNewSession(gw->get_random()->uint64(), sd->get_apps_unique_session_num(), i);

                    // Attaching the session.
                    sd->AttachToSession(new_session);

                    // Updating session number for this Apps.
                    current_db->SetAppsSessionValue(new_session->get_session_index(), sd->get_apps_unique_session_num());
                }
            }

            // Resetting the data buffer.
            sd->get_accum_buf()->Init(SOCKET_DATA_BLOB_SIZE_BYTES, sd->data_blob(), true);

            // Setting the database index and sequence number.
            sd->AttachToDatabase(db_index_);

            // Put the chunk into from database queue.
            gw->RunFromDbHandlers(sd);
        }
    }

    // Checking if any chunks were popped.
    if (num_popped_chunks > 0)
        *found_something = true;

    return 0;
}

// Push given chunk to database queue.
uint32_t WorkerDbInterface::PushLinkedChunksToDb(
    core::chunk_index chunk_index,
    int32_t stats_num_chunks,
    int32_t sched_id,
    bool not_overflow_chunk = true)
{
    // Obtaining the channel.
    core::channel_type& the_channel = shared_int_.channel(channels_[sched_id]);

    // Is overflow pool empty?
    bool overflow_is_empty = private_overflow_pool_.empty();
    if (!not_overflow_chunk)
        overflow_is_empty = true;

    // Trying to push given chunk into channel.
    if (overflow_is_empty && (the_channel.in.try_push_front(chunk_index) == true))
    {
        // A message on channel ch was received. Notify the database
        // that the out queue in this channel is not full.
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
		the_channel.scheduler()->notify(shared_int_.scheduler_work_event
		(the_channel.get_scheduler_number()));
#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
        the_channel.scheduler()->notify();
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.

#ifdef GW_CHUNKS_DIAG
        GW_PRINT_WORKER << "   successfully pushed: chunk " << chunk_index << std::endl;
#endif
    }
    else
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Couldn't push chunk into channel. Putting to overflow pool." << std::endl;
#endif

        // Could not push the request to the channels in
        // queue - push it to the overflow_pool_ instead.
        private_overflow_pool_.push_front(sched_id << 24 | chunk_index);

        return 1;
    }

    // Chunk was pushed successfully either to channel or overflow pool.
    g_gateway.GetDatabase(db_index_)->ChangeNumUsedChunks(-stats_num_chunks);

    return 0;
}

// Returns given socket data chunk to private chunk pool.
uint32_t WorkerDbInterface::ReturnSocketDataChunksToPool(GatewayWorker *gw, SocketDataChunk *sd)
{
#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Returning chunk: " << sd->sock() << " " << sd->get_chunk_index() << std::endl;
#endif

    // Returning linked multiple chunks.
    return ReturnLinkedChunksToPool(sd->get_num_chunks(), sd->get_chunk_index());
}

// Returns given chunk to private chunk pool.
uint32_t WorkerDbInterface::ReturnLinkedChunksToPool(int32_t num_linked_chunks, core::chunk_index& first_linked_chunk)
{
    // Releasing chunk to private pool.
    private_chunk_pool_.release_linked_chunks(&shared_int_.chunk(0), first_linked_chunk);

    // Chunk has been released.
    g_gateway.GetDatabase(db_index_)->ChangeNumUsedChunks(-num_linked_chunks);

    // Check if there are too many private chunks so
    // we need to release them to the shared chunk pool.
    if (private_chunk_pool_.size() > MAX_CHUNKS_IN_PRIVATE_POOL)
    {
        uint32_t err_code = ReleaseToSharedChunkPool(private_chunk_pool_.size() - MAX_CHUNKS_IN_PRIVATE_POOL);
        GW_ERR_CHECK(err_code);
    }

    return 0;
}

// Returns given chunk to private chunk pool.
uint32_t WorkerDbInterface::ReturnLinkedChunksToPool(shared_memory_chunk* chunk_smc, core::chunk_index& first_linked_chunk)
{
    // Determine how many chunks are linked.
    int32_t num_linked_chunks = 1;
    while (shared_memory_chunk::LINK_TERMINATOR != chunk_smc->get_link())
    {
        chunk_smc = (shared_memory_chunk*) &(shared_int_.chunk(chunk_smc->get_link()));
        num_linked_chunks++;
    }

    // Returning certain number of chunks.
    return ReturnLinkedChunksToPool(num_linked_chunks, first_linked_chunk);
}

// Push given chunk to database queue.
uint32_t WorkerDbInterface::PushSocketDataToDb(GatewayWorker* gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id)
{
#ifdef GW_CHUNKS_DIAG
    GW_PRINT_WORKER << "Pushing chunk: socket " << sd->sock() << " chunk " << sd->get_chunk_index() << " handler_id " << user_handler_id << std::endl;
#endif

    // Checking if chunk belongs to this database.
    ActiveDatabase* current_db = g_gateway.GetDatabase(db_index_);
    if ((current_db->unique_num()) != sd->db_unique_seq_num())
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Socket data does not belong to this database." << std::endl;
#endif
        return 1;
    }

    // Setting the Apps session number right before sending to that Apps (if session exists at all).
    if (INVALID_SESSION_INDEX != sd->get_session_index())
        sd->set_apps_unique_session_num(current_db->GetAppsSessionValue(sd->get_session_index()));

    // Modifying chunk data to use correct handler.
    shared_memory_chunk *smc = (shared_memory_chunk*) &(shared_int_.chunk(sd->get_chunk_index()));
    smc->set_bmx_protocol(user_handler_id); // User code handler id.
    smc->set_request_size(4);

    // Obtaining the current scheduler id.
    ScSessionStruct* session = sd->GetAttachedSession();
    uint32_t sched_id;
    if (session)
        sched_id = session->get_scheduler_id();
    else
        sched_id = g_gateway.obtain_scheduler_id();

    // Pushing socket data as a chunk.
    return PushLinkedChunksToDb(sd->get_chunk_index(), sd->get_num_chunks(), sched_id);
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
    smc->set_bmx_protocol(bmx::BMX_MANAGEMENT_HANDLER);

    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX message type.
    request->write(bmx::BMX_REGISTER_PUSH_CHANNEL);

    // Pushing the chunk.
    return PushLinkedChunksToDb(new_chunk, 1, sched_num);
}

// Pushes session destroyed message.
uint32_t WorkerDbInterface::PushSessionDestroyed(ScSessionStruct* session, int32_t sched_num)
{
    // Get a reference to the chunk.
    shared_memory_chunk *smc = NULL;

    // Getting a free chunk.
    core::chunk_index new_chunk;
    uint32_t err_code = GetOneChunkFromPrivatePool(&new_chunk, &smc);
    GW_ERR_CHECK(err_code);

    // Predefined BMX management handler.
    smc->set_bmx_protocol(bmx::BMX_MANAGEMENT_HANDLER);

    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX message type.
    request->write(bmx::BMX_SESSION_DESTROYED);

    // Writing Apps unique session number.
    request->write(session->apps_unique_session_num_);

    // Pushing the chunk.
    return PushLinkedChunksToDb(new_chunk, 1, sched_num);
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
    smc->set_bmx_protocol(bmx::BMX_MANAGEMENT_HANDLER);

    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX message type.
    request->write(bmx::BMX_SEND_ALL_HANDLERS);

    // Pushing the chunk.
    return PushLinkedChunksToDb(new_chunk, 1, sched_num);
}

// Initializes shared memory interface.
uint32_t WorkerDbInterface::Init(
    const int32_t new_slot_index,
    const core::shared_interface& workerSharedInt,
    GatewayWorker *gw)
{
    db_index_ = new_slot_index;
    worker_id_ = gw->get_worker_id();
    shared_int_ = workerSharedInt;

    // Getting unique client interface for this worker.
    if (!shared_int_.acquire_client_number())
    {
#ifdef GW_ERRORS_DIAG
        GW_PRINT_WORKER << "Can't acquire client interface." << std::endl;
#endif
        return 1;
    }

#ifdef GW_DATABASES_DIAG
    // Diagnostics.
    GW_PRINT_WORKER << "Database \"" << g_gateway.GetDatabase(db_index_)->get_db_name() <<
        "\" acquired client interface " << shared_int_.get_client_number() << " and " << g_gateway.get_num_schedulers() << " channel(s): ";
#endif

    // Acquiring unique channel for each scheduler.
    for (std::size_t i = 0; i < g_gateway.get_num_schedulers(); ++i)
    {
        if (!shared_int_.acquire_channel(&channels_[i], i))
            return 1;

#ifdef GW_DATABASES_DIAG
        GW_COUT << channels_[i] << ", ";
#endif
    }

#ifdef GW_DATABASES_DIAG
    GW_COUT << std::endl;
#endif

    return 0;
}

// Registers push channels on all schedulers.
uint32_t WorkerDbInterface::RegisterAllPushChannels()
{
    uint32_t err_code;

    // Sending push channel on each scheduler.
    for (int32_t i = 0; i < g_gateway.get_num_schedulers(); ++i)
    {
        // Pushing registration chunk.
        err_code = RegisterPushChannel(i);
        GW_ERR_CHECK(err_code);

#ifdef GW_DATABASES_DIAG
        GW_PRINT_WORKER << "Registered push channel on scheduler: " << i << std::endl;
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
    GW_PRINT_WORKER << "Requested registered handlers." << std::endl;
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
        return SCERRUNSPECIFIED;

    uint32_t err_code;
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
                GW_ERR_CHECK(err_code);

                GW_PRINT_WORKER << "Pong with data: " << ping_data << std::endl;

                return 0;
            }

            case bmx::BMX_REGISTER_PUSH_CHANNEL_RESPONSE:
            {
                // We have received a confirmation push channel chunk.
                g_gateway.GetDatabase(db_index_)->ReceivedPushChannelConfirmation();

                // Checking if we have all push channels confirmed from database.
                if (g_gateway.GetDatabase(db_index_)->IsAllPushChannelsConfirmed())
                {
                    GW_PRINT_WORKER << "All push channels confirmed!" << std::endl;

                    // Requesting all registered handlers.
                    err_code = RequestRegisteredHandlers();

                    GW_ERR_CHECK(err_code);
                }

                return 0;
            }

            case bmx::BMX_REGISTER_PORT:
            {
                // Reading handler id.
                BMX_HANDLER_TYPE handler_id = resp_chunk->read_handler_id();

                // Reading port number.
                uint16_t port = resp_chunk->read_uint16();

                GW_PRINT_WORKER << "New port " << port << " user handler registration with handler id: " << handler_id << std::endl;

                // Registering handler on active database.
                err_code = g_gateway.GetDatabase(db_index_)->get_user_handlers()->RegisterPortHandler(
                    gw,
                    port,
                    handler_id,
                    PortProcessData,
                    db_index_);

                GW_ERR_CHECK(err_code);

                break;
            }
            
            case bmx::BMX_REGISTER_PORT_SUBPORT:
            {
                // Reading handler id.
                BMX_HANDLER_TYPE handler_id = resp_chunk->read_handler_id();

                // Reading port number.
                uint16_t port = resp_chunk->read_uint16();

                // Reading subport.
                uint32_t subport = resp_chunk->read_uint32();

                GW_PRINT_WORKER << "New subport " << subport << " port " << port << " user handler registration with handler id: " << handler_id << std::endl;
                
                // Registering handler on active database.
                err_code = g_gateway.GetDatabase(db_index_)->get_user_handlers()->RegisterSubPortHandler(
                    gw,
                    port,
                    subport,
                    handler_id,
                    SubportProcessData,
                    db_index_);

                GW_ERR_CHECK(err_code);

                break;
            }

            case bmx::BMX_REGISTER_URI:
            {
                // Reading handler id.
                BMX_HANDLER_TYPE handler_id = resp_chunk->read_handler_id();

                // Reading port number.
                uint16_t port = resp_chunk->read_uint16();

                // Reading URI.
                char uri[bmx::MAX_URI_STRING_LEN];
                uint32_t uri_len_chars = resp_chunk->read_uint32();

                resp_chunk->read_string(uri, uri_len_chars, bmx::MAX_URI_STRING_LEN);
                bmx::HTTP_METHODS http_method = (bmx::HTTP_METHODS)resp_chunk->read_uint8();

                GW_PRINT_WORKER << "New URI handler \"" << uri << "\" on port " << port << " registration with handler id: " << handler_id << std::endl;

                // Registering handler on active database.
                HandlersTable* handlers_table = g_gateway.GetDatabase(db_index_)->get_user_handlers();
                err_code = handlers_table->RegisterUriHandler(
                    gw,
                    port,
                    uri,
                    uri_len_chars,
                    http_method,
                    handler_id,
                    UriProcessData,
                    db_index_);

                GW_ERR_CHECK(err_code);

                // Search for handler index by URI string.
                BMX_HANDLER_TYPE handler_index = handlers_table->FindUriUserHandlerIndex(port, uri, uri_len_chars);

                // Getting the port structure.
                ServerPort* server_port = g_gateway.FindServerPort(port);

                // Registering URI on port.
                RegisteredUris* all_port_uris = server_port->get_registered_uris();
                int32_t index = all_port_uris->FindRegisteredUri(uri, uri_len_chars);

                // Checking if there is an entry.
                if (index < 0)
                {
                    // Creating totally new URI entry.
                    RegisteredUri new_entry(
                        uri,
                        uri_len_chars,
                        db_index_,
                        handlers_table->get_handler_list(handler_index));

                    // Adding entry to global list.
                    all_port_uris->AddEntry(new_entry);
                }
                else
                {
                    // Obtaining existing URI entry.
                    RegisteredUri reg_uri = all_port_uris->GetEntryByIndex(index);

                    // Checking if there is no database for this URI.
                    if (!reg_uri.ContainsDb(db_index_))
                    {
                        // Creating new unique handlers list for this database.
                        UniqueHandlerList uhl(db_index_, handlers_table->get_handler_list(handler_index));

                        // Adding new handler list for this database to the URI.
                        reg_uri.Add(uhl);
                    }
                }
                GW_ERR_CHECK(err_code);

                // Printing port information.
                server_port->Print();

                break;
            }

            case bmx::BMX_UNREGISTER:
            {
                // Reading handler id.
                BMX_HANDLER_TYPE handler_id = resp_chunk->read_handler_id();

                GW_PRINT_WORKER << "User handler unregistration for handler id: " << handler_id << std::endl;

                // Getting handlers list.
                HandlersTable* handlers_table = g_gateway.GetDatabase(db_index_)->get_user_handlers();
                HandlersList* handlers_list = handlers_table->FindHandler(handler_id);
                assert(handlers_list != NULL);

                // Removing handler with certain id.
                err_code = handlers_table->UnregisterHandler(handler_id);

                // Removing from global data structures.
                ServerPort* server_port = g_gateway.FindServerPort(handlers_list->get_port());
                assert(server_port != NULL);

                // Removing this handler from all possible places.
                server_port->get_registered_uris()->RemoveEntry(handlers_list);
                server_port->get_registered_subports()->RemoveEntry(handlers_list);
                if (bmx::HANDLER_TYPE::PORT_HANDLER == handlers_list->get_type())
                {
                    // Should always be only one user handler.
                    assert(1 == handlers_list->get_num_entries());

                    // Removing corresponding handler.
                    server_port->get_port_handlers()->RemoveEntry(db_index_, handlers_list->get_handlers()[0]);
                }

                // Checking if server port became empty.
                if (server_port->IsEmpty())
                    server_port->Erase();

                return 0;
            }

            case bmx::BMX_SESSION_DESTROYED:
            {
                // Reading Apps unique session number.
                uint64_t apps_unique_session_num = resp_chunk->read_uint64();

                // TODO: Handle destroyed session.

                GW_PRINT_WORKER << "Session " << apps_unique_session_num << " was destroyed." << std::endl;

                break;
            }

            default:
            {
                return SCERRUNSPECIFIED;
            }
        }

        offset = resp_chunk->get_offset();
    }

    return 0;
}


} // namespace network
} // namespace starcounter