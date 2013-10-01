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
    uint8_t* orig_data_ptr = sd->get_accum_buf()->get_chunk_orig_buf_ptr();
    uint32_t num_accum_bytes = sd->get_accum_buf()->get_accum_len_bytes();
    uint32_t num_processed_bytes = 0;
    AggregationStruct ags;

    session_index_type aggr_socket_info_index = sd->get_socket_info_index();

    while (true)
    {
        // Processing current frame.
        uint8_t* cur_data_ptr = orig_data_ptr + num_processed_bytes;

        // Checking if frame is complete.
        if (num_accum_bytes - num_processed_bytes <= AggregationStructSizeBytes)
        {
            // Checking if we need to move current data up.
            cur_data_ptr = sd->get_accum_buf()->MoveDataToTopAndContinueReceive(cur_data_ptr, num_accum_bytes - num_processed_bytes);

            // Returning socket to receiving state.
            return gw->Receive(sd);
        }

        // Getting the frame info.
        ags = *(AggregationStruct*) cur_data_ptr;

        // Shifting to data structure size.
        num_processed_bytes += AggregationStructSizeBytes;

        // Continue accumulating data.
        if (num_processed_bytes + ags.size_bytes_ > num_accum_bytes)
        {
            // Checking if we need to move current data up.
            sd->get_accum_buf()->MoveDataToTopAndContinueReceive(cur_data_ptr, num_accum_bytes - num_processed_bytes + AggregationStructSizeBytes);

            // Enabling accumulative state.
            sd->set_accumulating_flag();

            // Setting the desired number of bytes to accumulate.
            sd->get_accum_buf()->StartAccumulation(
                static_cast<ULONG>(AggregationStructSizeBytes + ags.size_bytes_),
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
            if (num_processed_bytes + ags.size_bytes_ == num_accum_bytes)
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

        SocketDataChunk* cur_sd = sd_push_to_db;
        if (NULL == sd_push_to_db)
        {
            // Only when session is created we can receive non-stop.
            err_code = sd->CloneToReceive(gw);
            if (err_code)
                return err_code;

            cur_sd = sd;
        }

        // Getting port handler.
        int32_t port_index = g_gateway.FindServerPortIndex(ags.port_number_);

        // Checking if we have already created socket.
        if (!ags.socket_info_index_)
        {
            // Getting new socket index.
            ags.socket_info_index_ = g_gateway.ObtainFreeSocketIndex(gw, cur_sd->get_db_index(), INVALID_SOCKET, port_index);
            ags.unique_socket_id_ = g_gateway.GetUniqueSocketId(ags.socket_info_index_);
        }

        // Applying special parameters to socket data.
        g_gateway.ApplySocketInfoToSocketData(cur_sd, ags.socket_info_index_, ags.unique_socket_id_);

        // Setting aggregation socket.
        cur_sd->SetAggregationSocketIndex(aggr_socket_info_index);
        cur_sd->set_unique_aggr_index(ags.unique_aggr_index_);

        // Setting aggregation flag.
        cur_sd->SetSocketAggregatedFlag();

        // Changing accumulative buffer accordingly.
        cur_sd->get_accum_buf()->Init(ags.size_bytes_, num_processed_bytes);

        /*
        if (*(int32_t*)cur_sd->get_accum_buf()->get_orig_buf_ptr() != *(int32_t*)"POST")
            break;

        if (*(cur_sd->get_accum_buf()->get_orig_buf_ptr() + ags.size_bytes_ - 1) != (uint8_t)'1')
            break;
        */

        // Payload size has been checked, so we can add payload as processed.
        num_processed_bytes += static_cast<uint32_t>(ags.size_bytes_);

        g_gateway.num_aggregated_recv_messages_++;

        // Data is complete, no more frames, creating parallel receive clone.
        if (NULL == sd_push_to_db)
        {
            // Running handler.
            return gw->RunReceiveHandlers(sd);
        }
        else
        {
            // Running handler.
            err_code = gw->RunReceiveHandlers(sd_push_to_db);
            if (err_code)
                return err_code;
        }
    }

    return 0;
}

// Adds socket data chunk to aggregation queue.
void GatewayWorker::AddToAggregation(SocketDataChunkRef sd)
{
    if (INVALID_CHUNK_INDEX == first_aggregated_chunk_index_)
    {
        first_aggregated_chunk_index_ = sd->get_chunk_index();
        first_aggregated_chunk_db_index_ = sd->get_db_index();

        last_aggregated_chunk_ = sd->get_chunk_index();
        last_aggregated_chunk_db_index_ = sd->get_db_index();
    }
    else
    {
        SocketDataChunk* last_sd = worker_dbs_[last_aggregated_chunk_db_index_]->GetSocketDataFromChunkIndex(last_aggregated_chunk_);
        last_sd->set_next_chunk_db_index(sd->get_db_index());
        last_sd->get_smc()->set_next(sd->get_chunk_index());

        last_aggregated_chunk_ = sd->get_chunk_index();
        last_aggregated_chunk_db_index_ = sd->get_db_index();
    }
}

// Processes all aggregated chunks.
uint32_t GatewayWorker::SendAggregatedChunks()
{
    if (INVALID_CHUNK_INDEX == first_aggregated_chunk_index_)
        return 0;

    uint32_t err_code;

    core::chunk_index queue_chunk_index = first_aggregated_chunk_index_;

    db_index_type queue_chunk_db_index = first_aggregated_chunk_db_index_;

    int32_t num_wsa_bufs_done = 0, total_aggr_len_bytes = 0;

    SocketDataChunk* sd = NULL;

    session_index_type aggr_socket_index;

    SocketDataChunk* aggr_sd = NULL;

    core::chunk_index wsa_bufs_chunk_index;

    shared_memory_chunk* wsa_bufs_smc;

    WSABUF* wsa_buf;

    // Number of chunks for aggregated socket data.
    int32_t total_num_chunks;

SEND_AND_FETCH_NEXT:

    // Sending current aggregated chunks.
    if (NULL != aggr_sd)
    {
        aggr_sd->set_num_chunks(total_num_chunks);

        // Setting the aggregation buffer length.
        aggr_sd->get_accum_buf()->Init(total_aggr_len_bytes, 0);

        // Resetting counters.
        num_wsa_bufs_done = 0;
        total_aggr_len_bytes = 0;

        err_code = Send(aggr_sd);
        if (err_code)
        {
            // Disconnecting aggregation socket data.
            DisconnectAndReleaseChunk(aggr_sd);

            return err_code;
        }
    }

    // Fetching next chunk.
    queue_chunk_index = first_aggregated_chunk_index_;
    queue_chunk_db_index = first_aggregated_chunk_db_index_;

    // Checking if queue is finished.
    if (INVALID_CHUNK_INDEX == queue_chunk_index)
        return 0;

    // Getting socket data from chunk index.
    sd = worker_dbs_[queue_chunk_db_index]->GetSocketDataFromChunkIndex(queue_chunk_index);
    GW_ASSERT(sd->GetSocketAggregatedFlag());

    // Creating aggregated socket data.
    aggr_socket_index = sd->GetAggregationSocketIndex();
    err_code = CreateSocketData(aggr_socket_index, 0, aggr_sd);
    if (err_code)
        return err_code;

    GW_ASSERT(!aggr_sd->GetSocketAggregatedFlag());

    // Obtaining chunk for WSABUFs.
    err_code = worker_dbs_[0]->GetOneChunkFromPrivatePool(&wsa_bufs_chunk_index, &wsa_bufs_smc);
    if (err_code)
    {
        // Returning aggregation socket data to pool.
        worker_dbs_[0]->ReturnLinkedChunksToPool(1, aggr_sd->get_chunk_index());
        aggr_sd = NULL;

        return err_code;
    }

    // Shifting to next chunk information.
    first_aggregated_chunk_index_ = sd->get_smc()->get_next();
    first_aggregated_chunk_db_index_ = sd->get_next_chunk_db_index();

    // Linking the chunk from queue.
    aggr_sd->get_smc()->set_next(queue_chunk_index);
    aggr_sd->set_next_chunk_db_index(queue_chunk_db_index);

    // Aggregation chunk + WSA buffer chunk.
    total_num_chunks = 2;

    wsa_buf = (WSABUF*) wsa_bufs_smc;
    aggr_sd->set_extra_chunk_index(wsa_bufs_chunk_index);
    aggr_sd->get_smc()->set_link(wsa_bufs_chunk_index);

CREATE_AGGREGATION_STRUCT:

    // Sealing current sd.
    sd->get_smc()->set_next(INVALID_CHUNK_INDEX);
    sd->set_next_chunk_db_index(INVALID_DB_INDEX);

    AggregationStruct* aggr_struct = (AggregationStruct*) ((uint8_t*)sd + sd->get_user_data_offset_in_socket_data() - AggregationStructSizeBytes);
    aggr_struct->flags = sd->get_type_of_network_oper();
    aggr_struct->port_number_ = g_gateway.get_server_port(sd->GetPortIndex())->get_port_number();
    aggr_struct->size_bytes_ = sd->get_user_data_written_bytes();
    aggr_struct->socket_info_index_ = sd->get_socket_info_index();
    aggr_struct->unique_socket_id_ = sd->get_unique_socket_id();
    aggr_struct->unique_aggr_index_ = static_cast<int32_t>(sd->get_unique_aggr_index());
    
    // Checking if we have multiple chunks socket data.
    if (INVALID_CHUNK_INDEX != sd->get_extra_chunk_index())
    {
        core::chunk_index temp_wsa_bufs_chunk_index = sd->get_extra_chunk_index();
        shared_memory_chunk* temp_wsa_bufs_smc = worker_dbs_[sd->get_db_index()]->GetSharedMemoryChunkFromIndex(temp_wsa_bufs_chunk_index);

        // Checking if all chunks WSABUFs fit in current WSABUFs chunk.
        int32_t num_linked_data_chunks = sd->get_num_chunks() - 1;

        // Simply copying all WSABUFs.
        memcpy(wsa_buf, temp_wsa_bufs_smc, num_linked_data_chunks * sizeof(WSABUF));
        wsa_buf->len += AggregationStructSizeBytes;
        wsa_buf->buf = (char *)aggr_struct;

        // Adding to total number of chunks.
        total_num_chunks += num_linked_data_chunks;
        wsa_buf += num_linked_data_chunks;
        num_wsa_bufs_done += num_linked_data_chunks;

        // Removing old WSABUFs chunk.
        sd->set_extra_chunk_index(INVALID_CHUNK_INDEX);
        sd->get_smc()->set_link(temp_wsa_bufs_smc->get_link());
        temp_wsa_bufs_smc->set_link(INVALID_CHUNK_INDEX);

        // Returning old WSABUFs to pool.
        worker_dbs_[sd->get_db_index()]->ReturnLinkedChunksToPool(1, temp_wsa_bufs_chunk_index);
    }
    else
    {
        wsa_buf->buf = (char *)aggr_struct;
        wsa_buf->len = AggregationStructSizeBytes + aggr_struct->size_bytes_;

        // Adding another chunk.
        total_num_chunks++;
        wsa_buf++;
        num_wsa_bufs_done++;
    }

    g_gateway.num_aggregated_sent_messages_++;

    //std::cout << "Added to WSABUF: " << AggregationStructSizeBytes + aggr_struct->size_bytes_ << std::endl;

    // Adding to total aggregated length.
    total_aggr_len_bytes += AggregationStructSizeBytes + aggr_struct->size_bytes_;

    // Checking if we have not found a chunk for the same socket.
    if (num_wsa_bufs_done >= starcounter::bmx::MAX_EXTRA_LINKED_WSABUFS)
        goto SEND_AND_FETCH_NEXT;

    SocketDataChunk* temp_sd = sd;

    core::chunk_index prev_queue_chunk_index = queue_chunk_index;
    db_index_type prev_queue_chunk_db_index = queue_chunk_db_index;

    core::chunk_index cur_queue_chunk_index = first_aggregated_chunk_index_;
    db_index_type cur_queue_chunk_db_index = first_aggregated_chunk_db_index_;

    core::chunk_index next_queue_chunk_index = INVALID_CHUNK_INDEX;
    db_index_type next_queue_chunk_db_index = INVALID_DB_INDEX;

    bool first_in_queue = true;

    while (true)
    {
        if (INVALID_CHUNK_INDEX == cur_queue_chunk_index)
            goto SEND_AND_FETCH_NEXT;

        temp_sd = worker_dbs_[cur_queue_chunk_db_index]->GetSocketDataFromChunkIndex(cur_queue_chunk_index);
        session_index_type cur_aggr_socket_index = temp_sd->GetAggregationSocketIndex();

        // Getting next chunk index.
        next_queue_chunk_index = temp_sd->get_smc()->get_next();
        next_queue_chunk_db_index = temp_sd->get_next_chunk_db_index();

        // Checking if its the same aggregation socket.
        if (cur_aggr_socket_index == aggr_socket_index)
        {
            // NOTE: Immediately checking if new sd will fit into current aggregation message.

            // Checking if all chunks WSABUFs fit in current WSABUFs chunk.
            int32_t num_linked_data_chunks = temp_sd->get_num_chunks() - 1;

            // Checking if number of chunks fits in the remaining WSABUFs chunk.
            if (num_linked_data_chunks > static_cast<int32_t>(starcounter::bmx::MAX_EXTRA_LINKED_WSABUFS - num_wsa_bufs_done))
                goto SEND_AND_FETCH_NEXT;

            // Removing current chunk from queue.
            GW_ASSERT(INVALID_CHUNK_INDEX != prev_queue_chunk_db_index);
            SocketDataChunk* prev_sd = worker_dbs_[prev_queue_chunk_db_index]->GetSocketDataFromChunkIndex(prev_queue_chunk_index);

            if (!first_in_queue)
            {
                // Jumping over current chunk.
                prev_sd->get_smc()->set_next(next_queue_chunk_index);
                prev_sd->set_next_chunk_db_index(next_queue_chunk_db_index);
            }
            else
            {
                // Adjusting first aggregated chunk indexes.
                first_aggregated_chunk_index_ = next_queue_chunk_index;
                first_aggregated_chunk_db_index_ = next_queue_chunk_db_index;
            }

            // Linking to current socket data.
            sd->get_smc()->set_next(cur_queue_chunk_index);
            sd->set_next_chunk_db_index(cur_queue_chunk_db_index);

            // Switching current socket data.
            sd = temp_sd;

            // Setting current queue indexes.
            queue_chunk_index = cur_queue_chunk_index;
            queue_chunk_db_index = cur_queue_chunk_db_index;

            goto CREATE_AGGREGATION_STRUCT;
        }

        prev_queue_chunk_index = cur_queue_chunk_index;
        prev_queue_chunk_db_index = cur_queue_chunk_db_index;

        cur_queue_chunk_index = next_queue_chunk_index;
        cur_queue_chunk_db_index = next_queue_chunk_db_index;

        first_in_queue = false;
    }

    return 0;
}


} // namespace network
} // namespace starcounter
