#include "gateway.hpp"
#include "session_data.hpp"

namespace starcounter {
namespace network {

// Create new session based on random salt, linear index, scheduler.
void SessionData::GenerateNewSession(GatewayWorker *gw, UINT32 sessionIndex, UINT32 schedulerId)
{
    random_salt_ = gw->Random->uint64();
    session_index_ = sessionIndex;
    scheduler_id_ = schedulerId;

    num_visits_ = 0;
    attached_socket_ = INVALID_SOCKET;
    socket_stamp_ = gw->Random->uint64();
}

} // namespace network
} // namespace starcounter