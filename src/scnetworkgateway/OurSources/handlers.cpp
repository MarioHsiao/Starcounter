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

uint32_t HandlersTable::RegisterPortHandler(
    GatewayWorker *gw,
    uint16_t port_num,
    BMX_HANDLER_TYPE handler_id,
    GENERIC_HANDLER_CALLBACK port_handler,
    int32_t db_index)
{
    // Checking number of handlers.
    if (max_num_entries_ >= bmx::MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRGWMAXHANDLERSREACHED;

    uint32_t err_code = 0;

    BMX_HANDLER_TYPE i, empty_slot = max_num_entries_;

    // Running throw existing handlers.
    for (i = 0; i < max_num_entries_; i++)
    {
        // Checking if empty slot.
        if (registered_handlers_[i].IsEmpty())
        {
            empty_slot = i;
        }
        // Checking current handler type to be port handler.
        else if (bmx::PORT_HANDLER == registered_handlers_[i].get_type())
        {
            // Checking if port is the same.
            if (port_num == registered_handlers_[i].get_port())
            {
                // Checking same handler id.
                if (handler_id == registered_handlers_[i].get_handler_id())
                {
                    // Search if handler is already in the list.
                    if (!registered_handlers_[i].HandlerAlreadyExists(port_handler))
                    {
                        // Adding new handler to list.
                        err_code = registered_handlers_[i].AddHandler(port_handler);
                        GW_ERR_CHECK(err_code);
                    }

                    // Same handler already exists, checking server port.
                    goto PROCESS_SERVER_PORT;
                }
                else
                {
                    return SCERRGWWRONGHANDLERINSLOT;
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(bmx::HANDLER_TYPE::PORT_HANDLER, handler_id, port_num, 0, NULL, 0, bmx::HTTP_METHODS::OTHER_METHOD);
    GW_ERR_CHECK(err_code);

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddHandler(port_handler);
    GW_ERR_CHECK(err_code);

    // New handler added.
    if (empty_slot == max_num_entries_)
        max_num_entries_++;

PROCESS_SERVER_PORT:

    // Checking if there is a corresponding server port created.
    ServerPort* server_port = g_gateway.FindServerPort(port_num);

    // Checking if port exists.
    if (!server_port)
    {
        SOCKET listening_sock = INVALID_SOCKET;

        // Creating socket and binding to port for all workers.
        err_code = g_gateway.CreateListeningSocketAndBindToPort(gw, port_num, listening_sock);
        GW_ERR_CHECK(err_code);

        // Adding new server port.
        int32_t port_index = g_gateway.AddServerPort(port_num, listening_sock, RAW_BLOB_USER_DATA_OFFSET);
        server_port = g_gateway.get_server_port(port_index);
    }

    // Checking if port contains handlers from this database.
    if (server_port->get_port_handlers()->GetEntryIndex(db_index) < 0)
    {
        // Determining how many connections to create.
        int32_t how_many = ACCEPT_ROOF_STEP_SIZE;

#ifdef GW_TESTING_MODE

        // On the test client we immediately creating all needed connections.
        if (!g_gateway.setting_is_master())
            how_many = g_gateway.setting_num_connections_to_master();

#endif

        // Creating new connections if needed for this database.
        err_code = g_gateway.CreateNewConnectionsAllWorkers(how_many, port_num, db_index);
        if (err_code)
            return err_code;
    }

    // Adding handler if it does not exist yet.
    server_port->get_port_handlers()->Add(db_index, OuterPortProcessData);

    return 0;
}

// Registers sub-port handler.
uint32_t HandlersTable::RegisterSubPortHandler(
    GatewayWorker *gw,
    uint16_t port_num,
    bmx::BMX_SUBPORT_TYPE subport,
    BMX_HANDLER_TYPE handler_id,
    GENERIC_HANDLER_CALLBACK subport_handler,
    int32_t db_index)
{
    // Checking number of handlers.
    if (max_num_entries_ >= bmx::MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRGWMAXHANDLERSREACHED;

    uint32_t err_code = 0;

    BMX_HANDLER_TYPE i, empty_slot = max_num_entries_;

    // Running throw existing handlers.
    for (i = 0; i < max_num_entries_; i++)
    {
        // Checking if empty slot.
        if (registered_handlers_[i].IsEmpty())
        {
            empty_slot = i;
        }
        // Checking current handler type to be port handler.
        else if (bmx::HANDLER_TYPE::SUBPORT_HANDLER == registered_handlers_[i].get_type())
        {
            // Checking if port is the same.
            if (port_num == registered_handlers_[i].get_port())
            {
                if (subport == registered_handlers_[i].get_subport())
                {
                    // Checking same handler id.
                    if (handler_id == registered_handlers_[i].get_handler_id())
                    {
                        // Search if handler is already in the list.
                        if (!registered_handlers_[i].HandlerAlreadyExists(subport_handler))
                        {
                            // Adding new handler to list.
                            err_code = registered_handlers_[i].AddHandler(subport_handler);
                            GW_ERR_CHECK(err_code);
                        }

                        // Same handler already exists, checking server port.
                        goto PROCESS_SERVER_PORT;
                    }
                    else
                    {
                        return SCERRGWWRONGHANDLERINSLOT;
                    }
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(bmx::HANDLER_TYPE::SUBPORT_HANDLER, handler_id, port_num, 0, NULL, 0, bmx::HTTP_METHODS::OTHER_METHOD);
    GW_ERR_CHECK(err_code);

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddHandler(subport_handler);
    GW_ERR_CHECK(err_code);

    // New handler added.
    if (empty_slot == max_num_entries_)
        max_num_entries_++;

PROCESS_SERVER_PORT:

    // Checking if there is a corresponding server port created.
    ServerPort* server_port = g_gateway.FindServerPort(port_num);

    // Checking if port exists.
    if (!server_port)
    {
        SOCKET listening_sock = INVALID_SOCKET;

        // Creating socket and binding to port for all workers.
        err_code = g_gateway.CreateListeningSocketAndBindToPort(gw, port_num, listening_sock);
        GW_ERR_CHECK(err_code);

        // Adding new server port.
        int32_t port_index = g_gateway.AddServerPort(port_num, listening_sock, SUBPORT_BLOB_USER_DATA_OFFSET);
        server_port = g_gateway.get_server_port(port_index);
    }

    // Checking if port contains handlers from this database.
    if (server_port->get_port_handlers()->GetEntryIndex(db_index) < 0)
    {
        // Determining how many connections to create.
        int32_t how_many = ACCEPT_ROOF_STEP_SIZE;

#ifdef GW_TESTING_MODE

        // On the test client we immediately creating all needed connections.
        if (!g_gateway.setting_is_master())
            how_many = g_gateway.setting_num_connections_to_master();

#endif

        // Creating new connections if needed for this database.
        err_code = g_gateway.CreateNewConnectionsAllWorkers(how_many, port_num, db_index);
        if (err_code)
            return err_code;
    }

    // Adding handler if it does not exist yet.
    server_port->get_port_handlers()->Add(db_index, OuterSubportProcessData);

    return 0;
}

// Registers URI handler.
uint32_t HandlersTable::RegisterUriHandler(
    GatewayWorker *gw,
    uint16_t port_num,
    const char* uri_string,
    uint32_t uri_str_chars,
    bmx::HTTP_METHODS http_method,
    BMX_HANDLER_TYPE handler_id,
    GENERIC_HANDLER_CALLBACK uri_handler,
    int32_t db_index)
{
    // Checking number of handlers.
    if (max_num_entries_ >= bmx::MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRGWMAXHANDLERSREACHED;

    uint32_t err_code = 0;

    BMX_HANDLER_TYPE i, empty_slot = max_num_entries_;

    // Running throw existing handlers.
    for (i = 0; i < max_num_entries_; i++)
    {
        // Checking if empty slot.
        if (registered_handlers_[i].IsEmpty())
        {
            empty_slot = i;
        }
        // Checking current handler type to be port handler.
        else if (bmx::URI_HANDLER == registered_handlers_[i].get_type())
        {
            // Checking if port is the same.
            if (port_num == registered_handlers_[i].get_port())
            {
                // Checking the same URI.
                if (!strcmp(uri_string, registered_handlers_[i].get_uri()))
                {
                    // Checking same handler id.
                    if (handler_id == registered_handlers_[i].get_handler_id())
                    {
                        // Search if handler is already in the list.
                        if (!registered_handlers_[i].HandlerAlreadyExists(uri_handler))
                        {
                            // Adding new handler to list.
                            err_code = registered_handlers_[i].AddHandler(uri_handler);
                            GW_ERR_CHECK(err_code);
                        }

                        // Same handler already exists, checking server port.
                        goto PROCESS_SERVER_PORT;
                    }
                    else
                    {
                        return SCERRGWWRONGHANDLERINSLOT;
                    }
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(bmx::HANDLER_TYPE::URI_HANDLER, handler_id, port_num, 0, uri_string, uri_str_chars, http_method);
    GW_ERR_CHECK(err_code);

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddHandler(uri_handler);
    GW_ERR_CHECK(err_code);

    // New handler added.
    if (empty_slot == max_num_entries_)
        max_num_entries_++;

PROCESS_SERVER_PORT:

    // Checking if there is a corresponding server port created.
    ServerPort* server_port = g_gateway.FindServerPort(port_num);

    // Checking if port exists.
    if (!server_port)
    {
        SOCKET listening_sock = INVALID_SOCKET;

        // Creating socket and binding to port for all workers.
        err_code = g_gateway.CreateListeningSocketAndBindToPort(gw, port_num, listening_sock);
        GW_ERR_CHECK(err_code);

        // Adding new server port.
        int32_t port_index = g_gateway.AddServerPort(port_num, listening_sock, HTTP_BLOB_USER_DATA_OFFSET);
        server_port = g_gateway.get_server_port(port_index);
    }

    // Checking if port contains handlers from this database.
    if (server_port->get_port_handlers()->GetEntryIndex(db_index) < 0)
    {
        // Determining how many connections to create.
        int32_t how_many = ACCEPT_ROOF_STEP_SIZE;

#ifdef GW_TESTING_MODE

        // On the test client we immediately creating all needed connections.
        if (!g_gateway.setting_is_master())
            how_many = g_gateway.setting_num_connections_to_master();

#endif

        // Creating new connections if needed for this database.
        err_code = g_gateway.CreateNewConnectionsAllWorkers(how_many, port_num, db_index);
        if (err_code)
            return err_code;
    }

    // Adding handler if it does not exist yet.
    server_port->get_port_handlers()->Add(db_index, OuterUriProcessData);

    return 0;
}

// Finds certain handler.
HandlersList* HandlersTable::FindHandler(BMX_HANDLER_TYPE handler_id)
{
    // Checking all registered handlers.
    for (BMX_HANDLER_TYPE i = 0; i < max_num_entries_; i++)
    {
        // Checking if the same handler.
        if (handler_id == registered_handlers_[i].get_handler_id())
        {
            return registered_handlers_ + i;
        }
    }

    return NULL;
}

// Unregisters certain handler.
uint32_t HandlersTable::UnregisterHandler(BMX_HANDLER_TYPE handler_id, GENERIC_HANDLER_CALLBACK handler_callback)
{
    // Checking all registered handlers.
    uint32_t err_code = SCERRUNSPECIFIED;
    for (BMX_HANDLER_TYPE i = 0; i < max_num_entries_; i++)
    {
        // Checking if the same handler.
        if (handler_id == registered_handlers_[i].get_handler_id())
        {
            // Unregistering certain handler.
            if (!handler_callback)
                err_code = registered_handlers_[i].Unregister();
            else
                err_code = registered_handlers_[i].Unregister(handler_callback);

            return err_code;
        }
    }

    // If not removed.
    return SCERRGWHANDLERNOTFOUND;
}

// Unregisters certain handler.
uint32_t HandlersTable::UnregisterHandler(BMX_HANDLER_TYPE handler_id)
{
    return UnregisterHandler(handler_id, NULL);
}

// Outer port handler.
uint32_t OuterPortProcessData(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_index, bool* is_handled)
{
    // First searching in database handlers table.
    HandlersTable* handlers_table = g_gateway.GetDatabase(sd->get_db_index())->get_user_handlers();

    // Getting the corresponding port number.
    uint16_t port_num = g_gateway.get_server_port(sd->get_port_index())->get_port_number();

    // Searching for the user code handler id.
    handler_index = handlers_table->FindPortHandlerIndex(port_num);

    // Checking if handler was not found in database handlers.
    if (INVALID_HANDLER_INDEX == handler_index)
    {
        // Switching to gateway handlers.
        handlers_table = g_gateway.get_gw_handlers();

        // Searching for the user code handler id.
        handler_index = handlers_table->FindPortHandlerIndex(port_num);
    }

    // Making sure that handler index is obtained.
    GW_ASSERT(INVALID_HANDLER_INDEX != handler_index);

    // Now running specific handler.
    return handlers_table->get_handler_list(handler_index)->RunHandlers(gw, sd, is_handled);
}

// General sockets handler.
uint32_t AppsPortProcessData(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    uint32_t err_code;

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Resetting user data parameters.
        sd->set_user_data_written_bytes(sd->get_accum_buf()->get_accum_len_bytes());
        sd->ResetUserDataOffset();
        sd->ResetMaxUserDataBytes();

        // Push chunk to corresponding channel/scheduler.
        // TODO: Deal with situation when not able to push.
        gw->PushSocketDataToDb(sd, user_handler_id);

        // Setting handled flag.
        *is_handled = true;

        return 0;
    }
    // Checking if data goes from user code.
    else
    {
        // Prepare buffer to send outside.
        sd->get_accum_buf()->PrepareForSend(sd->UserDataBuffer(), sd->get_user_data_written_bytes());

        // Sending data.
        err_code = gw->Send(sd);
        GW_ERR_CHECK(err_code);

        // Setting handled flag.
        *is_handled = true;

        return 0;
    }

    return SCERRGWPORTPROCESSFAILED;
}

#ifdef GW_TESTING_MODE

// Port echo handler.
uint32_t GatewayPortProcessEcho(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    uint32_t err_code;

    // Setting handled flag.
    *is_handled = true;

    if (g_gateway.setting_is_master())
    {
        AccumBuffer* accum_buffer = sd->get_accum_buf();

        GW_ASSERT(accum_buffer->get_accum_len_bytes() == 8);

        // Copying echo message.
        int64_t orig_echo = *(int64_t*)accum_buffer->get_orig_buf_ptr();

        // Duplicating this echo.
        *(int64_t*)(accum_buffer->get_orig_buf_ptr() + 8) = orig_echo;

        // Prepare buffer to send outside.
        accum_buffer->PrepareForSend(accum_buffer->get_orig_buf_ptr(), 16);

        // Sending data.
        err_code = gw->Send(sd);
        GW_ERR_CHECK(err_code);
    }
    else
    {
        // Asserting correct number of bytes received.
        GW_ASSERT(sd->get_accum_buf()->get_accum_len_bytes() == 16);

        // Obtaining original echo number.
        echo_id_type echo_id = *(int32_t*)(sd->get_data_blob() + 8);

#ifdef GW_ECHO_STATISTICS
        GW_PRINT_WORKER << "Received echo: " << echo_id << GW_ENDL;
#endif

        // Confirming received echo.
        g_gateway.ConfirmEcho(echo_id);

        // Checking if all echo responses are returned.
        if (g_gateway.CheckConfirmedEchoResponses())
        {
            // Gracefully finishing the test.
            g_gateway.ShutdownTest(true);

            // Returning this chunk to database.
            WorkerDbInterface *db = gw->GetWorkerDb(sd->get_db_index());
            GW_ASSERT(db != NULL);

#ifdef GW_COLLECT_SOCKET_STATISTICS
            sd->set_socket_diag_active_conn_flag(false);
#endif

            // Returning chunks to pool.
            return db->ReturnSocketDataChunksToPool(gw, sd);
                        
            /*
            EnterGlobalLock();
            g_gateway.ResetEchoTests();
            LeaveGlobalLock();
            return 0;
            */
        }
        else
        {
            goto SEND_RAW_ECHO_TO_MASTER;
        }

SEND_RAW_ECHO_TO_MASTER:

        // Checking that not all echoes are sent.
        if (!g_gateway.AllEchoesSent())
        {
            // Sending echo request to server.
            err_code = gw->SendRawEcho(sd, g_gateway.GetNextEchoNumber());
            if (err_code)
                return err_code;
        }
    }

    return 0;
}

#endif

// Outer port handler.
uint32_t OuterSubportProcessData(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE handler_index, bool* is_handled)
{
    return OuterPortProcessData(gw, sd, handler_index, is_handled);
}

// Subport handler.
uint32_t AppsSubportProcessData(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    return AppsPortProcessData(gw, sd, user_handler_id, is_handled);
}

#ifdef GW_TESTING_MODE

// Subport echo handler.
uint32_t GatewaySubportProcessEcho(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    return GatewayPortProcessEcho(gw, sd, user_handler_id, is_handled);
}

#endif

} // namespace network
} // namespace starcounter
