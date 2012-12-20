#pragma once
#ifndef TLS_PROTO_HPP
#define TLS_PROTO_HPP

namespace starcounter {
namespace network {

class TlsProtocol
{
    TlsProtocol()
    {
    }

    void Reset()
    {
    }

    uint32_t ProcessTlsData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled);
};

uint32_t HttpsProcessData(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id, bool* is_handled);

} // namespace network
} // namespace starcounter

#endif // TLS_PROTO_HPP
