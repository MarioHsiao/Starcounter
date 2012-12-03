#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
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

                        return err_code;
                    }
                    else
                    {
                        // Same handler already exists just returning.
                        return 0;
                    }
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

                            return err_code;
                        }
                        else
                        {
                            // Same handler already exists just returning.
                            return 0;
                        }
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

                            return err_code;
                        }
                        else
                        {
                            // Same handler already exists just returning.
                            return 0;
                        }
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
uint32_t OuterPortProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_index, bool* is_handled)
{
    // Setting handled flag.
    *is_handled = false;

    HandlersTable* handlers_table = g_gateway.GetDatabase(sd->get_db_index())->get_user_handlers();

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Getting the corresponding port number.
        uint16_t port_num = g_gateway.get_server_port(sd->get_port_index())->get_port_number();

        // Searching for the user code handler id.
        handler_index = handlers_table->FindPortHandlerIndex(port_num);

        // Checking if user handler was not found.
        if (!handler_index)
        {
            // Returning error code.
            return SCERRGWINCORRECTHANDLER;
        }
    }

    // Now running specific handler.
    return handlers_table->get_handler_list(handler_index)->RunHandlers(gw, sd);
}

// General sockets handler.
uint32_t AppsPortProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    uint32_t err_code;

    // Setting handled flag.
    *is_handled = false;

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Looking for attached session or generating a new one.
        /*if (sd->GetAttachedSession() == NULL)
        {
            // Generating and attaching new session.
            sd->AttachToSession(g_gateway.GenerateNewSession(gw));
        }*/

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

// General sockets handler.
uint32_t GatewayPortProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    uint32_t err_code;

    // Setting handled flag.
    *is_handled = false;

    // Prepare buffer to send outside.
    sd->get_accum_buf()->PrepareForSend(sd->UserDataBuffer(), sd->get_user_data_written_bytes());

    // Sending data.
    err_code = gw->Send(sd);
    GW_ERR_CHECK(err_code);

    // Setting handled flag.
    *is_handled = true;

    return 0;
}

#endif

// Outer port handler.
uint32_t OuterSubportProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_index, bool* is_handled)
{
    return OuterPortProcessData(gw, sd, handler_index, is_handled);
}

// Subport handler.
uint32_t AppsSubportProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    return AppsPortProcessData(gw, sd, user_handler_id, is_handled);
}

#ifdef GW_TESTING_MODE

// Subport handler.
uint32_t GatewaySubportProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    return GatewayPortProcessData(gw, sd, user_handler_id, is_handled);
}

#endif

} // namespace network
} // namespace starcounter
