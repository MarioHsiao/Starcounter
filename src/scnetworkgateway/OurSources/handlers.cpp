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

// Running all registered handlers.
uint32_t PortHandlers::RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd, bool* is_handled)
{
    uint32_t err_code;

    // Going through all handler list.
    for (int32_t i = 0; i < handler_lists_.get_num_entries(); ++i)
    {
        err_code = handler_lists_[i]->RunHandlers(gw, sd, is_handled);

        // Checking if information was handled and no errors occurred.
        if (*is_handled || err_code)
            return err_code;
    }

    return SCERRGWPORTNOTHANDLED;
}

// Should be called when whole handlers list should be unregistered.
uint32_t HandlersList::UnregisterGlobally(db_index_type db_index)
{
    // Checking the type of handler.
    switch(type_)
    {
        case bmx::HANDLER_TYPE::PORT_HANDLER:
        {
            break;
        }

        case bmx::HANDLER_TYPE::SUBPORT_HANDLER:
        {
            // Unregister globally.

            break;
        }

        case bmx::HANDLER_TYPE::URI_HANDLER:
        {
            // Unregister globally.
            RegisteredUris* port_uris = g_gateway.FindServerPort(port_)->get_registered_uris();
            port_uris->RemoveEntry(db_index, processed_uri_info_);

            // Collecting empty ports.
            g_gateway.CleanUpEmptyPorts();

            break;
        }

        case bmx::HANDLER_TYPE::WS_HANDLER:
        {
            // Unregister globally.
            PortWsChannels* w = g_gateway.FindServerPort(port_)->get_registered_ws_channels();
            w->RemoveEntry(db_index);

            // Collecting empty ports.
            g_gateway.CleanUpEmptyPorts();

            break;
        }

        default:
        {
            GW_ASSERT(false);
        }
    }

    return 0;
}

// General sockets handler.
uint32_t AppsPortProcessData(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE user_handler_id,
    bool* is_handled)
{
    uint32_t err_code;

    // Checking if we just want to disconnect.
    if (sd->get_just_push_disconnect_flag()) {

        // Setting handled flag.
        *is_handled = true;

        // Push chunk to corresponding channel/scheduler.
        err_code = gw->PushSocketDataToDb(sd, user_handler_id);

        if (err_code) {

            // Releasing the cloned chunk.
            gw->ReturnSocketDataChunksToPool(sd);

            return err_code;
        }

        return 0;
    }

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Its a raw socket protocol.
        sd->SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_RAW_PORT);

        // Resetting user data parameters.
        sd->get_accum_buf()->set_chunk_num_available_bytes(sd->get_accum_buf()->get_accum_len_bytes());
        sd->ResetUserDataOffset();

        // Setting matched URI index.
        sd->SetDestDbIndex(hl->get_db_index());

        // Posting cloning receive since all data is accumulated.
        err_code = sd->CloneToReceive(gw);
        if (err_code)
            return err_code;

        // Push chunk to corresponding channel/scheduler.
        err_code = gw->PushSocketDataToDb(sd, user_handler_id);
        if (err_code)
            return err_code;

        // Setting handled flag.
        *is_handled = true;

        return 0;
    }
    // Checking if data goes from user code.
    else
    {
        // Checking if we want to disconnect the socket.
        if (sd->get_disconnect_socket_flag())
            return SCERRGWDISCONNECTFLAG;

        // Prepare buffer to send outside.
        sd->PrepareForSend(sd->UserDataBuffer(), sd->get_user_data_length_bytes());

        // Sending data.
        err_code = gw->Send(sd);
        if (err_code)
            return err_code;

        // Setting handled flag.
        *is_handled = true;

        return 0;
    }

    GW_ASSERT(false);
}

} // namespace network
} // namespace starcounter
