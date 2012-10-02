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

uint32_t TlsProtocol::ProcessTlsData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    // Handled.
    *is_handled = true;

    // Resetting parser data.
    Reset();

    // Adding sockets to data from database queue.
    gw->RunFromDbHandlers(sd);

    return 0;
}

uint32_t HttpsProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled)
{
    // Checking if data goes to private pool.
    if (sd->data_to_user_flag())
        return 0;

    return 1;
}

} // namespace network
} // namespace starcounter
