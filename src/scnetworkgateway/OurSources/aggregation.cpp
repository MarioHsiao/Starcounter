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

// Aggregation on gateway.
uint32_t PortAggregator(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef aggr_sd,
    BMX_HANDLER_TYPE handler_info)
{
    uint32_t err_code;

    SocketDataChunk* new_sd = NULL;
    uint8_t* orig_data_ptr = aggr_sd->get_data_blob_start();
    int32_t num_accum_bytes = aggr_sd->get_accumulated_len_bytes(), num_processed_bytes = 0;
    AggregationStruct* ags;

    socket_index_type aggr_socket_info_index = aggr_sd->get_socket_info_index();

    while (num_processed_bytes < num_accum_bytes)
    {
        // Processing current frame.
        uint8_t* cur_data_ptr = orig_data_ptr + num_processed_bytes;

        // Checking if frame is complete.
        if (num_accum_bytes - num_processed_bytes < AggregationStructSizeBytes)
        {
            // Checking if we need to move current data up.
            cur_data_ptr = aggr_sd->MoveDataToTopAndContinueReceive(cur_data_ptr, num_accum_bytes - num_processed_bytes);

            // Returning socket to receiving state.
            return gw->Receive(aggr_sd);
        }

        // Getting the frame info.
        ags = (AggregationStruct*) cur_data_ptr;

        // Checking type of the message.
        MixedCodeConstants::AggregationMessageTypes msg_type = (MixedCodeConstants::AggregationMessageTypes) ags->msg_type_;

        // Shifting to data structure size.
        num_processed_bytes += AggregationStructSizeBytes;

        switch (msg_type)
        {
            case MixedCodeConstants::AGGR_CREATE_SOCKET:
            {
                uint16_t port_num = ags->port_number_;

                // Getting port handler.
                port_index_type port_index = g_gateway.FindServerPortIndex(port_num);

                // Checking if port exists.
                if ((0 != ags->size_bytes_) || (INVALID_PORT_INDEX == port_index)) {

                    // Nullifying the aggregation structure.
                    memset(ags, 0, sizeof(AggregationStruct));
                    ags->msg_type_ = (uint8_t) MixedCodeConstants::AGGR_CREATE_SOCKET;

                    // Sending data on aggregation socket.
                    err_code = gw->SendOnAggregationSocket(
                        aggr_sd->get_socket_info_index(),
                        aggr_sd->get_unique_socket_id(),
                        (const uint8_t*) ags,
                        AggregationStructSizeBytes);

                    // NOTE: If problem finding port - breaking the sequence.
                    break;
                }
                
                // Getting new socket index.
                ags->socket_info_index_ = gw->ObtainFreeSocketIndex(
                    INVALID_SOCKET,
                    port_index,
                    MixedCodeConstants::NetworkProtocolType::PROTOCOL_UNKNOWN,
                    false);

                // Checking if we can't obtain new socket index.
                if (INVALID_SOCKET_INDEX == (ags->socket_info_index_)) {

                    // NOTE: If problems obtaining free chunk index, breaking the whole aggregated receive.
                    break;
                }

                // Getting unique socket id.
                ags->unique_socket_id_ = gw->GetUniqueSocketId(ags->socket_info_index_);

                // Setting some socket options.
                gw->SetSocketAggregatedFlag(ags->socket_info_index_);

                // Sending data on aggregation socket.
                err_code = gw->SendOnAggregationSocket(
                    aggr_sd->get_socket_info_index(),
                    aggr_sd->get_unique_socket_id(),
                    (const uint8_t*) ags,
                    AggregationStructSizeBytes);

                if (err_code) {
                    // NOTE: If problems obtaining chunk, breaking the whole aggregated receive.
                    break;
                }

                break;
            }

            case MixedCodeConstants::AGGR_DESTROY_SOCKET:
            {
                // TODO: Research what to do on disconnect.

                // Checking if socket is legitimate.
                if (gw->CompareUniqueSocketId(ags->socket_info_index_, ags->unique_socket_id_)) {

                    gw->ReleaseSocketIndex(ags->socket_info_index_);
                    ags->socket_info_index_ = INVALID_SOCKET_INDEX;
                }

                break;
            }

            case MixedCodeConstants::AGGR_DATA:
            {
                // Checking if whole request is received.
                if (num_processed_bytes + ags->size_bytes_ > num_accum_bytes)
                {
                    // Checking if we need to move current data up.
                    aggr_sd->MoveDataToTopAndContinueReceive(cur_data_ptr, num_accum_bytes - num_processed_bytes + AggregationStructSizeBytes);

                    // Returning socket to receiving state.
                    return gw->Receive(aggr_sd);
                }

                // Checking if we have already created socket.
                if (true != gw->CompareUniqueSocketId(ags->socket_info_index_, ags->unique_socket_id_)) {

                    // Payload size has been checked, so we can add payload as processed.
                    num_processed_bytes += static_cast<uint32_t>(ags->size_bytes_);

                    // NOTE: If wrong unique index, breaking the whole aggregated receive.
                    break;
                }

                // Cloning chunk to push it to database.
                err_code = aggr_sd->CreateSocketDataFromBigBuffer(gw, ags->socket_info_index_, ags->size_bytes_, orig_data_ptr + num_processed_bytes, &new_sd);

                // Payload size has been checked, so we can add payload as processed.
                num_processed_bytes += static_cast<uint32_t>(ags->size_bytes_);

                if (err_code) {
                    // NOTE: If problems obtaining chunk, breaking the whole aggregated receive.
                    break;
                }

                // Applying special parameters to socket data.
                gw->ApplySocketInfoToSocketData(new_sd, ags->socket_info_index_, ags->unique_socket_id_);

                // Setting this aggregation socket.
                gw->SetAggregationSocketInfo(
                    ags->socket_info_index_,
                    aggr_sd->get_socket_info_index(),
                    aggr_sd->get_unique_socket_id());

                // Setting aggregation socket.
                new_sd->set_unique_aggr_index(ags->unique_aggr_index_);
                new_sd->set_aggregated_flag();

                // FIXME: Obtaining the aggregation socket client IP instead of real client-client IP.
                new_sd->set_client_ip_info(aggr_sd->get_client_ip_info());

                // Changing accumulative buffer accordingly.
                new_sd->SetAccumulation(ags->size_bytes_);

                // Checking if its a no IPC test.
                switch ((MixedCodeConstants::AggregationMessageFlags) ags->msg_flags_) {

                    case MixedCodeConstants::AggregationMessageFlags::AGGR_MSG_GATEWAY_NO_IPC:
                        new_sd->set_gateway_no_ipc_test_flag();
                        break;

                    case MixedCodeConstants::AggregationMessageFlags::AGGR_MSG_GATEWAY_AND_IPC:
                        new_sd->set_gateway_and_ipc_test_flag();
                        break;

                    case MixedCodeConstants::AggregationMessageFlags::AGGR_MSG_GATEWAY_NO_IPC_NO_CHUNKS:
                        new_sd->set_gateway_no_ipc_no_chunks_test_flag();
                        break;
                }

                g_gateway.num_aggregated_recv_messages_++;

                // Running handler.
                err_code = gw->RunReceiveHandlers(new_sd);

                if (err_code) {

                    // Releasing the cloned chunk.
                    if (NULL != new_sd)
                        gw->ReturnSocketDataChunksToPool(new_sd);

                    // NOTE: If problems obtaining chunk, breaking the whole aggregated receive.
                    break;
                }

                break;
            }

            default:
                break;
        }
    }

    // Returning socket to original receiving state.
    aggr_sd->ResetAccumBuffer();
    return gw->Receive(aggr_sd);
}

// Processes all aggregated chunks.
uint32_t GatewayWorker::SendAggregatedChunks()
{
    uint32_t err_code;

    for (int32_t i = 0; i < aggr_sds_to_send_.get_num_entries(); i++)
    {
        SocketDataChunk* aggr_sd = aggr_sds_to_send_[i];

        // Removing this aggregation socket data from list.
        aggr_sds_to_send_.RemoveByIndex(i);

        // Reverting accumulating buffer before send.
        aggr_sd->RevertBeforeSend();

        // Sending it.
        err_code = Send(aggr_sd);
        if (err_code)
            return err_code;

        GW_ASSERT(NULL == aggr_sd);
    }

    return 0;
}

// Tries to find current aggregation socket data from aggregation socket index.
SocketDataChunk* GatewayWorker::FindAggregationSd(
    socket_index_type aggr_socket_info_index,
    random_salt_type aggr_unique_socket_id)
{
    for (int32_t i = 0; i < aggr_sds_to_send_.get_num_entries(); i++)
    {
        if ((aggr_socket_info_index == aggr_sds_to_send_[i]->get_socket_info_index()) &&
            (aggr_unique_socket_id == aggr_sds_to_send_[i]->get_unique_socket_id())) {

            return aggr_sds_to_send_[i];
        }
    }

    return NULL;
}

// Performs a send of given socket data on aggregation socket.
uint32_t GatewayWorker::SendOnAggregationSocket(SocketDataChunkRef sd, MixedCodeConstants::AggregationMessageTypes msg_type)
{
    socket_index_type aggr_socket_info_index = sd->GetAggregationSocketIndex();
    random_salt_type aggr_unique_socket_id = sd->GetAggregationSocketUniqueId();

    // NOTE: Since we are sending, we need to get number of available bytes instead of user data length.
    int32_t data_len_bytes = sd->get_num_available_network_bytes();

    uint32_t total_num_bytes = data_len_bytes + AggregationStructSizeBytes;

    AggregationStruct* aggr_struct = (AggregationStruct*) (sd->GetUserData() - AggregationStructSizeBytes);
    aggr_struct->port_number_ = g_gateway.get_server_port(sd->GetPortIndex())->get_port_number();
    aggr_struct->size_bytes_ = data_len_bytes;
    aggr_struct->socket_info_index_ = sd->get_socket_info_index();
    aggr_struct->unique_socket_id_ = sd->get_unique_socket_id();
    aggr_struct->unique_aggr_index_ = static_cast<int32_t>(sd->get_unique_aggr_index());
    aggr_struct->msg_type_ = msg_type;
    aggr_struct->msg_flags_ = 0;

    uint32_t err_code = SendOnAggregationSocket(aggr_socket_info_index, aggr_unique_socket_id, (uint8_t*) aggr_struct, total_num_bytes);

    if (!err_code) {
        // Releasing the chunk.
        ReturnSocketDataChunksToPool(sd);
    }

    return err_code;
}

// Performs a send of given socket data on aggregation socket.
uint32_t GatewayWorker::SendOnAggregationSocket(
    const socket_index_type aggr_socket_info_index,
    const random_salt_type aggr_unique_socket_id,
    const uint8_t* data,
    const int32_t data_len)
{
    // Checking if socket is valid.
    if (!CompareUniqueSocketId(aggr_socket_info_index, aggr_unique_socket_id)) {
        return SCERRGWOPERATIONONWRONGSOCKET;
    }

    // Making sure we are sending on a correct aggregation socket.
    SocketDataChunk* aggr_sd = FindAggregationSd(aggr_socket_info_index, aggr_unique_socket_id);

    // Checking if socket is correct.
    if (aggr_sd && (!aggr_sd->CompareUniqueSocketId())) {

        // Removing this aggregation socket data from list.
        aggr_sds_to_send_.RemoveEntry(aggr_sd);

        // Disconnect aggregation socket data.
        DisconnectAndReleaseChunk(aggr_sd);

        return SCERRGWOPERATIONONWRONGSOCKET;
    }

    uint32_t err_code;

WRITE_TO_AGGR_SD:

    if (aggr_sd) {

        GW_ASSERT(aggr_sd->get_bound_worker_id() == worker_id_);
        GW_ASSERT(aggr_sd->GetBoundWorkerId() == worker_id_);

        // Checking if data fits in socket data.
        uint32_t total_num_bytes = data_len;

        // NOTE: Asserting that maximum data to send fits in big aggregation chunk.
        GW_ASSERT(total_num_bytes < aggr_sd->get_data_blob_size());

        // Checking if data fits in the current buffer space.
        if (aggr_sd->get_num_available_network_bytes() >= total_num_bytes)
        {
            // Writing given buffer to send.
            aggr_sd->WriteBytesToSend((void*)data, total_num_bytes);

            // Checking if aggregation buffer is filled.
            if (AggregationStructSizeBytes > aggr_sd->get_num_available_network_bytes())
            {
                // Removing this aggregation socket data from list.
                aggr_sds_to_send_.RemoveEntry(aggr_sd);

                // Reverting accumulating buffer before send.
                aggr_sd->RevertBeforeSend();

                // Sending it.
                err_code = Send(aggr_sd);
                if (err_code)
                    return err_code;

                GW_ASSERT(NULL == aggr_sd);
            }

            return 0;
        }
        else
        {
            // Removing this aggregation socket data from list.
            aggr_sds_to_send_.RemoveEntry(aggr_sd);

            // Reverting accumulating buffer before send.
            aggr_sd->RevertBeforeSend();

            // Sending it.
            err_code = Send(aggr_sd);
            if (err_code)
                return err_code;

            GW_ASSERT(NULL == aggr_sd);

            goto WRITE_TO_AGGR_SD;
        }
    }
    else
    {
        // Creating new socket data.
        err_code = CreateSocketData(aggr_socket_info_index, aggr_sd);
        if (err_code)
            return err_code;

        // Checking if socket is correct.
        GW_ASSERT(aggr_sd->CompareUniqueSocketId());

        // Adding new aggregation sd to list.
        aggr_sds_to_send_.Add(aggr_sd);

        goto WRITE_TO_AGGR_SD;
    }

    return 0;
}

} // namespace network
} // namespace starcounter
