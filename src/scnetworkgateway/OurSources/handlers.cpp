#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "worker_db_interface.hpp"
#include "worker.hpp"

namespace starcounter {
namespace network {

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
            port_uris->RemoveEntry(db_index, method_space_uri_);

            // Collecting empty ports.
            g_gateway.CleanUpEmptyPorts();

            break;
        }

        case bmx::HANDLER_TYPE::WS_HANDLER:
        {
            // Unregister globally.
            PortWsGroups* w = g_gateway.FindServerPort(port_)->get_registered_ws_groups();
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
uint32_t UdpPortProcessData(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE user_handler_id)
{
    uint32_t err_code;

    // Checking if data goes to user code.
    if (sd->get_to_database_direction_flag())
    {
        // Its a raw socket protocol.
        sd->SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_UDP);

        // Resetting user data parameters.
        sd->SetUserData(sd->get_data_blob_start(), sd->get_accumulated_len_bytes());

        // Setting matched URI index.
        sd->SetDestDbIndex(hl->get_db_index());

        // Checking if we need to send back UDP datagram here.
        if (sd->GetPortNumber() == 55555) {

            // Prepare buffer to send outside.
            sd->PrepareForSend(sd->GetUserData(), sd->get_user_data_length_bytes());

            // Posting cloning receive since all data is accumulated.
            err_code = gw->Send(sd);
            if (err_code)
                return err_code;

        } else {

            // Reordering port bytes to host order.
            sd->UdpChangePortByteOrder();

            // Posting cloning receive since all data is accumulated.
            err_code = sd->CloneToReceive(gw);
            if (err_code)
                return err_code;

            // Push chunk to corresponding channel/scheduler.
            err_code = gw->PushSocketDataToDb(sd, user_handler_id, false);
            if (err_code)
                return err_code;
        }

        return 0;
    }
    // Checking if data goes from user code.
    else
    {
        // Reordering port bytes.
        sd->UdpChangePortByteOrder();

        // Reordering IPv4 bytes.
        sd->UdpChangeIPv4ByteOrder();

        // Prepare buffer to send outside.
        sd->PrepareForSend(sd->GetUserData(), sd->get_user_data_length_bytes());

        // Sending data.
        err_code = gw->Send(sd);
        if (err_code)
            return err_code;

        return 0;
    }

    GW_ASSERT(false);
}

// General sockets handler.
uint32_t TcpPortProcessData(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE user_handler_id)
{
    uint32_t err_code;

    // Checking if we just want to disconnect.
    if (sd->get_just_push_disconnect_flag()) {

        // Push chunk to corresponding channel/scheduler.
        err_code = gw->PushSocketDataToDb(sd, user_handler_id, true);

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
        sd->SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_TCP);

        // Resetting user data parameters.
        sd->SetUserData(sd->get_data_blob_start(), sd->get_accumulated_len_bytes());

        // Setting matched URI index.
        sd->SetDestDbIndex(hl->get_db_index());

        // Posting cloning receive since all data is accumulated.
        err_code = sd->CloneToReceive(gw);
        if (err_code)
            return err_code;

        // Push chunk to corresponding channel/scheduler.
        err_code = gw->PushSocketDataToDb(sd, user_handler_id, false);
        if (err_code)
            return err_code;

        return 0;
    }
    // Checking if data goes from user code.
    else
    {
        // Checking if we want to disconnect the socket.
        if (sd->get_disconnect_socket_flag())
            return SCERRGWDISCONNECTFLAG;

        // Prepare buffer to send outside.
        sd->PrepareForSend(sd->GetUserData(), sd->get_user_data_length_bytes());

        // Sending data.
        err_code = gw->Send(sd);
        if (err_code)
            return err_code;

        return 0;
    }

    GW_ASSERT(false);
}

} // namespace network
} // namespace starcounter
