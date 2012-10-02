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

// Releases chunks from private chunk pool to the shared chunk pool.
int32_t WorkerDbInterface::ReleaseToSharedChunkPool(int32_t num_chunks)
{
    // Release chunks from this worker threads private chunk pool to the shared chunk pool.
    int32_t released_chunks_num = shared_int_.release_from_private_to_shared(
        private_chunk_pool_, num_chunks, &shared_int_.client_interface(), 1000);

    // Checking that number of released chunks is correct.
    if (released_chunks_num != num_chunks)
    {
        // Some problem with releasing chunks.
#ifdef GW_ERRORS_DIAG
        GW_COUT << "Problem releasing chunks to shared chunk pool." << std::endl;
#endif
    }

    // Returning released number of chunks.
    return released_chunks_num;
}

// Scans all channels for any incoming chunks.
uint32_t WorkerDbInterface::ScanChannels(GatewayWorker *gw)
{
    core::chunk_index chunk_index;
    uint32_t errCode;

    // Push everything from overflow pool.
    PushOverflowChunksIfAny();

    // Running through all channels.
    for (int32_t i = 0; i < g_gateway.active_sched_num_read_only(); i++)
    {
        // Obtaining the channel.
        core::channel_type& the_channel = shared_int_.channel(channels_[i]);

        // Check if there is a message and process it.
        if (the_channel.out.try_pop_back(&chunk_index) == true)
        {
            // A message on channel ch was received. Notify the database
            // that the out queue in this channel is not full.
            the_channel.scheduler()->notify();

            // Chunk was popped successfully.
            g_gateway.GetDatabase(db_index_)->ChangeNumUsedChunks(1);

            // Get the chunk.
            shared_memory_chunk* smc = (shared_memory_chunk*) &(shared_int_.chunk(chunk_index));

            // Check if its a BMX handlers management message.
            if (bmx::BMX_MANAGEMENT_HANDLER == smc->get_bmx_protocol())
            {
                // Entering global lock.
                gw->EnterGlobalLock();

                // Handling management chunks.
                errCode = HandleManagementChunks(gw, smc);
                GW_ERR_CHECK(errCode);

                // Releasing global lock.
                gw->LeaveGlobalLock();

                // Releasing chunk.
                ReturnChunkToPool(chunk_index);

                continue;
            }

            // Process the chunk.
            SocketDataChunk *sd = (SocketDataChunk *)((uint8_t *)smc + BMX_HEADER_MAX_SIZE_BYTES);

            // Checking that corresponding database and handler are up.
            if (!sd->CheckSocketIsValid(gw))
                continue;

#ifdef GW_CHUNKS_DIAG
            GW_COUT << "[" << gw->GetWorkerId() << "]: " << "Popping chunk: " << sd->sock() << " " << sd->chunk_index() << std::endl;
#endif

            // Resetting the data buffer.
            sd->get_data_buf()->Init(DATA_BLOB_SIZE_BYTES, sd->data_blob());

            // Setting the database index and sequence number.
            sd->AttachToDatabase(db_index_);

            /*
            // Getting attached session.
            SessionData *session = sd->GetAttachedSession();

            // Check that data received belongs to the correct session (not coming from abandoned connection).
            if ((NULL == session) || (!session->CompareSocketStamps(sd->GetSocketStamp())))
            {
#ifdef GW_ERRORS_DIAG
                GW_COUT << "[" << gw->GetWorkerId() << "]: " << "Data from db has wrong session: " <<
                    sd->GetSocket() << " " << sd->GetChunkIndex() << ", current session socket: " << session->get_socket() << std::endl;
#endif

                // Setting in session attribute (to return to pool).
                sd->set_in_session(true);

                // Silently disconnecting socket.
                gw->Disconnect(sd);

                continue;
            }*/

            // Put the chunk into from database queue.
            gw->RunFromDbHandlers(sd);
        }
    }

    return 0;
}

// Push given chunk to database queue.
uint32_t WorkerDbInterface::PushChunkToDb(
    core::chunk_index chunk_index,
    int32_t sched_num,
    bool not_overflow_chunk = true)
{
    // Obtaining the channel.
    core::channel_type& the_channel = shared_int_.channel(channels_[sched_num]);

    // Is overflow buffer empty?
    bool overflow_is_empty = private_overflow_pool_.empty();
    if (!not_overflow_chunk)
        overflow_is_empty = true;

    // Trying to push given chunk into channel.
    if (overflow_is_empty && (the_channel.in.try_push_front(chunk_index) == true))
    {
        // A message on channel ch was received. Notify the database
        // that the out queue in this channel is not full.
        the_channel.scheduler()->notify();

#ifdef GW_CHUNKS_DIAG
        GW_COUT << "   successfully pushed: " << chunk_index << std::endl;
#endif
    }
    else
    {
#ifdef GW_ERRORS_DIAG
        GW_COUT << "Couldn't push chunk into channel. Putting to overflow pool." << std::endl;
#endif

        // Could not push the request to the channels in
        // queue - push it to the overflow_pool_ instead.
        private_overflow_pool_.push_front(sched_num << 24 | chunk_index);

        return 1;
    }

    // Chunk was pushed successfully.
    g_gateway.GetDatabase(db_index_)->ChangeNumUsedChunks(-1);

    return 0;
}

// Returns given socket data chunk to private chunk pool.
uint32_t WorkerDbInterface::ReturnChunkToPool(GatewayWorker *gw, SocketDataChunk *sd)
{
#ifdef GW_CHUNKS_DIAG
    GW_COUT << "[" << gw->GetWorkerId() << "]: " << "Returning chunk: " << sd->sock() << " " << sd->chunk_index() << std::endl;
#endif

    return ReturnChunkToPool(sd->chunk_index());
}

// Returns given chunk to private chunk pool.
uint32_t WorkerDbInterface::ReturnChunkToPool(core::chunk_index& chunk_index)
{
    // Releasing chunk to private pool.
    private_chunk_pool_.release_linked_chunks(&shared_int_.chunk(0), chunk_index);

    // Chunk has been released.
    g_gateway.GetDatabase(db_index_)->ChangeNumUsedChunks(-1);

    // Check if there are too many private chunks so
    // we need to release them to the shared chunk pool.
    if (private_chunk_pool_.size() >= MAX_CHUNKS_IN_PRIVATE_POOL)
    {
        uint32_t errCode = ReleaseToSharedChunkPool(MAX_CHUNKS_IN_PRIVATE_POOL - NUM_CHUNKS_TO_LEAVE_IN_PRIVATE_POOL);
        GW_ERR_CHECK(errCode);
    }

    return 0;
}

// Push given chunk to database queue.
uint32_t WorkerDbInterface::PushSocketDataToDb(GatewayWorker* gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id)
{
#ifdef GW_CHUNKS_DIAG
    GW_COUT << "[" << gw->GetWorkerId() << "]: " << "Pushing chunk: " << sd->sock() << " " << sd->chunk_index() << " " << user_handler_id << std::endl;
#endif

    // Checking if chunk belongs to this database.
    if (g_gateway.GetDatabase(db_index_)->unique_num() != sd->db_unique_seq_num())
    {
#ifdef GW_ERRORS_DIAG
        GW_COUT << "Socket data does not belong to this database." << std::endl;
#endif
        return 1;
    }

    // Modifying chunk data to use correct handler.
    shared_memory_chunk *smc = (shared_memory_chunk*) &(shared_int_.chunk(sd->chunk_index()));
    smc->set_bmx_protocol(user_handler_id); // User code handler id.
    smc->terminate_link();
    smc->set_request_size(4);

    // Pushing socket data as a chunk.
    return PushChunkToDb(sd->chunk_index(), sd->GetAttachedSession()->get_scheduler_id());
}

// Registers push channel.
uint32_t WorkerDbInterface::RegisterPushChannel()
{
    // Get a reference to the chunk.
    shared_memory_chunk *smc = NULL;

    // Getting a free chunk.
    core::chunk_index new_chunk = GetChunkFromPrivatePool(&smc);

    // Predefined BMX management handler.
    smc->set_bmx_protocol(bmx::BMX_MANAGEMENT_HANDLER);
    smc->terminate_link();

    // Writing BMX message type.
    request_chunk_part* request = smc->get_request_chunk();
    request->write(bmx::BMX_REGISTER_PUSH_CHANNEL);

    // Pushing the chunk.
    return PushChunkToDb(new_chunk, 0);
}

// Requesting previously registered handlers.
uint32_t WorkerDbInterface::RequestRegisteredHandlers()
{
    // Get a reference to the chunk.
    shared_memory_chunk *smc = NULL;

    // Getting a free chunk.
    core::chunk_index new_chunk = GetChunkFromPrivatePool(&smc);

    // Filling the chunk as BMX management handler.
    smc->set_bmx_protocol(bmx::BMX_MANAGEMENT_HANDLER);
    smc->terminate_link();

    // Writing BMX message type.
    request_chunk_part* request = smc->get_request_chunk();
    request->write(bmx::BMX_SEND_ALL_HANDLERS);

    // Pushing the chunk.
    return PushChunkToDb(new_chunk, 0);
}

// Initializes shared memory interface.
uint32_t WorkerDbInterface::Init(
    const int32_t new_slot_index,
    const core::shared_interface& workerSharedInt,
    GatewayWorker *gw)
{
    db_index_ = new_slot_index;
    shared_int_ = workerSharedInt;

    // Getting unique client interface for this worker.
    if (!shared_int_.acquire_client_number())
    {
#ifdef GW_ERRORS_DIAG
        GW_COUT << "Can't acquire client interface." << std::endl;
#endif
        return 1;
    }

#ifdef GW_GENERAL_DIAG
    GW_COUT << "Acquired client interface number: " <<
        shared_int_.get_client_number() << std::endl;

    GW_COUT << "Acquired channel numbers: ";
#endif

    // Acquiring unique channel for each scheduler.
    for (std::size_t i = 0; i < g_gateway.active_sched_num_read_only(); ++i)
    {
        if (!shared_int_.acquire_channel(&channels_[i], i))
            return 1;

        GW_COUT << channels_[i] << " ";
    }
    GW_COUT << std::endl;

    uint32_t errCode;

    // Only worker zero does the push channel and handlers registration.
    if (gw->GetWorkerId() == 0)
    {
        // Pushing registration chunk.
        errCode = RegisterPushChannel();
        GW_ERR_CHECK(errCode);

        // Asking for existing handlers.
        errCode = RequestRegisteredHandlers();
        GW_ERR_CHECK(errCode);
    }

    return 0;
}

// Handles management chunks.
uint32_t WorkerDbInterface::HandleManagementChunks(GatewayWorker *gw, shared_memory_chunk* smc)
{
    // Getting the response part of the chunk.
    response_chunk_part* resp_chunk = smc->get_response_chunk();
    uint32_t response_size = resp_chunk->get_offset();
    if (0 == response_size)
        return 1;

    uint32_t err_code;
    uint32_t offset = 0;

    resp_chunk->reset_offset();
    while (offset < response_size)
    {
        // Reading BMX message type.
        uint8_t bmx_type = resp_chunk->read_uint8();

        switch (bmx_type)
        {
            case bmx::BMX_REGISTER_PORT:
            {
                // Reading handler id.
                BMX_HANDLER_TYPE handler_id = resp_chunk->read_handler_id();

                // Reading port number.
                uint16_t port = resp_chunk->read_uint16();

                GW_COUT << "New port " << port << " user handler registration with handler id: " << handler_id << std::endl;

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

                GW_COUT << "New subport " << subport << " port " << port << " user handler registration with handler id: " << handler_id << std::endl;
                
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

                GW_COUT << "New URI handler \"" << uri << "\" on port " << port << " registration with handler id: " << handler_id << std::endl;

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

                GW_COUT << "User handler unregistration for handler id: " << handler_id << std::endl;

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

                break;
            }

            default:
            {
                return 1;
            }
        }

        offset = resp_chunk->get_offset();
    }

    return 0;
}


} // namespace network
} // namespace starcounter