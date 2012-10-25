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
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

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
                        err_code = registered_handlers_[i].AddUserHandler(port_handler);

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
                    return SCERRUNSPECIFIED; // SCERRWRONGHANDLERINSLOT
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(bmx::HANDLER_TYPE::PORT_HANDLER, handler_id, port_num, 0, NULL, 0, bmx::HTTP_METHODS::OTHER_METHOD);
    GW_ERR_CHECK(err_code);

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddUserHandler(port_handler);
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
        // Creating new connections if needed for this database.
        err_code = g_gateway.CreateNewConnectionsAllWorkers(ACCEPT_ROOF_STEP_SIZE, port_num, db_index);
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
    uint32_t subport,
    BMX_HANDLER_TYPE handler_id,
    GENERIC_HANDLER_CALLBACK subport_handler,
    int32_t db_index)
{
    // Checking number of handlers.
    if (max_num_entries_ >= bmx::MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

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
                            err_code = registered_handlers_[i].AddUserHandler(subport_handler);

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
                        return SCERRUNSPECIFIED; // SCERRWRONGHANDLERINSLOT
                    }
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(bmx::HANDLER_TYPE::SUBPORT_HANDLER, handler_id, port_num, 0, NULL, 0, bmx::HTTP_METHODS::OTHER_METHOD);
    GW_ERR_CHECK(err_code);

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddUserHandler(subport_handler);
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
        // Creating new connections if needed for this database.
        err_code = g_gateway.CreateNewConnectionsAllWorkers(ACCEPT_ROOF_STEP_SIZE, port_num, db_index);
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
    char* uri_string,
    uint32_t uri_str_chars,
    bmx::HTTP_METHODS http_method,
    BMX_HANDLER_TYPE handler_id,
    GENERIC_HANDLER_CALLBACK uri_handler,
    int32_t db_index)
{
    // Checking number of handlers.
    if (max_num_entries_ >= bmx::MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

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
                            err_code = registered_handlers_[i].AddUserHandler(uri_handler);

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
                        return SCERRUNSPECIFIED; // SCERRWRONGHANDLERINSLOT
                    }
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(bmx::HANDLER_TYPE::URI_HANDLER, handler_id, port_num, 0, uri_string, uri_str_chars, http_method);
    GW_ERR_CHECK(err_code);

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddUserHandler(uri_handler);
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
        // Creating new connections if needed for this database.
        err_code = g_gateway.CreateNewConnectionsAllWorkers(ACCEPT_ROOF_STEP_SIZE, port_num, db_index);
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
uint32_t HandlersTable::UnregisterHandler(BMX_HANDLER_TYPE handler_id, GENERIC_HANDLER_CALLBACK user_handler)
{
    // Checking all registered handlers.
    uint32_t err_code = SCERRUNSPECIFIED;
    for (BMX_HANDLER_TYPE i = 0; i < max_num_entries_; i++)
    {
        // Checking if the same handler.
        if (handler_id == registered_handlers_[i].get_handler_id())
        {
            // Unregistering certain handler.
            if (!user_handler)
                err_code = registered_handlers_[i].Unregister();
            else
                err_code = registered_handlers_[i].Unregister(user_handler);

            return err_code;
        }
    }

    // If not removed.
    return SCERRUNSPECIFIED; // SCERRHANDLERNOTFOUND 
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
    if (sd->data_to_user_flag())
    {
        // Getting the corresponding port number.
        uint16_t port_num = g_gateway.get_server_port(sd->get_port_index())->get_port_number();

        // Searching for the user code handler id.
        handler_index = handlers_table->FindPortUserHandlerIndex(port_num);

        // Checking if user handler was not found.
        if (!handler_index)
        {
            // Disconnecting this socket.
            gw->Disconnect(sd);

            // Returning error code.
            return 1;
        }
    }

    // Now running specific handler.
    return handlers_table->get_handler_list(handler_index)->RunHandlers(gw, sd);
}

// General sockets handler.
uint32_t PortProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    // Setting handled flag.
    *is_handled = false;

    // Checking if data goes to user code.
    if (sd->data_to_user_flag())
    {
        // Looking for attached session or generating a new one.
        /*if (sd->GetAttachedSession() == NULL)
        {
            // Generating and attaching new session.
            sd->AttachToSession(g_gateway.GenerateNewSession(gw));
        }*/

        // Push chunk to corresponding channel/scheduler.
        gw->PushSocketDataToDb(sd, user_handler_id);

        // Setting handled flag.
        *is_handled = true;

        return 0;
    }
    // Checking if data goes from user code.
    else
    {
        // Prepare buffer to send outside.
        sd->get_data_buf()->PrepareForSend(sd->UserDataBuffer(), sd->get_user_data_written_bytes());

        // Sending data.
        gw->Send(sd);

        // Setting handled flag.
        *is_handled = true;

        return 0;
    }

    return 1;
}

// Outer port handler.
uint32_t OuterSubportProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_index, bool* is_handled)
{
    return OuterPortProcessData(gw, sd, handler_index, is_handled);
}

// Subport handler.
uint32_t SubportProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    return PortProcessData(gw, sd, user_handler_id, is_handled);
}

} // namespace network
} // namespace starcounter
