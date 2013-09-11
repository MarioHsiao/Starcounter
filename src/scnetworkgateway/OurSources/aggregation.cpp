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

struct AggregationStruct
{
    uint16_t port_number_;
    uint32_t size_bytes_;
    session_index_type socket_index_;
    random_salt_type socket_unique_index_;
};

const int32_t AggregationStructSizeBytes = sizeof(AggregationStruct);

// Aggregation on gateway.
uint32_t PortAggregator(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled)
{
    uint32_t err_code;

    // Handled successfully.
    *is_handled = true;

    SocketDataChunk* sd_push_to_db = NULL;
    uint8_t* orig_data_ptr = sd->get_data_blob();
    uint32_t num_accum_bytes = sd->get_accum_buf()->get_accum_len_bytes();
    uint32_t num_processed_bytes = 0;

    while (true)
    {
        // Processing current frame.
        uint8_t* cur_data_ptr = orig_data_ptr + num_processed_bytes;

        // Checking if frame is complete.
        if (orig_data_ptr + num_accum_bytes - cur_data_ptr <= AggregationStructSizeBytes)
        {
            // Checking if we need to move current data up.
            cur_data_ptr = sd->get_accum_buf()->MoveDataToTopIfTooLittleSpace(cur_data_ptr, WS_MAX_FRAME_INFO_SIZE);

            // Continue receiving.
            sd->get_accum_buf()->ContinueReceive();

            // Returning socket to receiving state.
            return gw->Receive(sd);
        }

        // Getting the frame info.
        AggregationStruct as = *(AggregationStruct*) cur_data_ptr;

        num_processed_bytes += AggregationStructSizeBytes;

        // Continue accumulating data.
        if (num_processed_bytes + as.size_bytes_ > num_accum_bytes)
        {
            // Enabling accumulative state.
            sd->set_accumulating_flag(true);

            // Setting the desired number of bytes to accumulate.
            sd->get_accum_buf()->StartAccumulation(static_cast<ULONG>(AggregationStructSizeBytes + as.size_bytes_),
                AggregationStructSizeBytes + num_accum_bytes - num_processed_bytes);

            // Trying to continue accumulation.
            bool is_accumulated;
            uint32_t err_code = sd->ContinueAccumulation(gw, &is_accumulated);
            if (err_code)
                return err_code;

            // Checking if we have not accumulated everything yet.
            return gw->Receive(sd);
        }
        // Checking if it is not the last frame.
        else
        {
            // Checking if all received data processed.
            if (num_processed_bytes + as.size_bytes_ == num_accum_bytes)
            {
                sd_push_to_db = NULL;
            }
            else
            {
                // Cloning chunk to push it to database.
                err_code = sd->CloneToPush(gw, &sd_push_to_db);
                if (err_code)
                    return err_code;
            }
        }

        // Payload size has been checked, so we can add payload as processed.
        num_processed_bytes += static_cast<uint32_t>(as.size_bytes_);

        // Data is complete, no more frames, creating parallel receive clone.
        if (NULL == sd_push_to_db)
        {
            // Only when session is created we can receive non-stop.
            err_code = sd->CloneToReceive(gw);
            if (err_code)
                return err_code;

            // Checking if we have already created socket.
            if (!as.socket_index_)
            {
                // Getting port handler.
                int32_t port_index = g_gateway.FindServerPortIndex(as.port_number_);

                // Getting new socket index.
                session_index_type new_socket_index = g_gateway.ObtainFreeSocketIndex(0, port_index);

                // Attaching socket data to socket.
            }

            // Running handler.
            return gw->RunReceiveHandlers(sd);
        }
        else
        {
            // Getting port handler.
            int32_t port_index = g_gateway.FindServerPortIndex(as.port_number_);

            // Running handler.
            err_code = gw->RunReceiveHandlers(sd);
            if (err_code)
                return err_code;
        }
    }

    return 0;
}

} // namespace network
} // namespace starcounter
