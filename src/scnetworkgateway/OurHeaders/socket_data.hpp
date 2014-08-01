#pragma once
#ifndef SOCKET_DATA_HPP
#define SOCKET_DATA_HPP

namespace starcounter {
namespace network {

class GatewayWorker;
class HttpProto;

// Socket data chunk.
class WorkerDbInterface;
class SocketDataChunk
{
    // Overlapped structure used for WinSock
    // (must be 8-bytes aligned).
    OVERLAPPED ovl_;

    /////////////////////////
    // 8 bytes aligned data.
    /////////////////////////

    // Unique number for socket.
    random_salt_type unique_socket_id_;

    // Client IP address information.
    ip_info_type client_ip_info_;

    /////////////////////////
    // 4 bytes aligned data.
    /////////////////////////

    // Socket identifier.
    socket_index_type socket_info_index_;

    // Socket data flags.
    uint32_t flags_;

    // Aggregation unique index.
    uint32_t unique_aggr_index_;

    /////////////////////////
    // 2 bytes aligned data.
    /////////////////////////

    // Offset in bytes from the beginning of the socket data to place
    // where user data should be written.
    uint16_t user_data_offset_in_socket_data_;

    /////////////////////////
    // 1 bytes aligned data.
    /////////////////////////

    // Current type of network operation.
    uint8_t type_of_network_oper_;

    // Type of network protocol.
    uint8_t type_of_network_protocol_;

    // Gateway chunk store index.
    chunk_store_type chunk_store_index_;

    /////////////////////////
    // Data structures.
    /////////////////////////

    // Accumulative buffer chunk.
    AccumBuffer accum_buf_;

    // HTTP protocol instance.
    HttpProto http_proto_;

    // WebSocket related data.
    WsProto ws_proto_;

    // Starcounter session information.
    ScSessionStruct session_;

    // Accept or parameters or temporary data.
    uint8_t accept_or_params_or_temp_data_[MixedCodeConstants::PARAMS_INFO_MAX_SIZE_BYTES];

    // Blob buffer itself.
    uint8_t data_blob_[1];

public:

    // Resets session depending on protocol.
    void ResetSessionBasedOnProtocol(GatewayWorker* gw);

    // Checking that gateway chunk is valid.
    void CheckForValidity()
    {
        GW_ASSERT((chunk_store_index_ >= 0) && (chunk_store_index_ < NumGatewayChunkSizes));
    }

    // Invalidating gateway chunk when returning to store.
    void InvalidateWhenReturning()
    {
        chunk_store_index_ = -1;
        accum_buf_.Invalidate();
    }

    chunk_store_type get_chunk_store_index()
    {
        return chunk_store_index_;
    }

    void set_chunk_store_index(chunk_store_type store_type)
    {
        chunk_store_index_ = store_type;
    }

    worker_id_type get_bound_worker_id()
    {
        return session_.gw_worker_id_;
    }

    void set_bound_worker_id(worker_id_type worker_id)
    {
        session_.gw_worker_id_ = worker_id;
    }

    void PlainCopySocketDataInfoHeaders(SocketDataChunk* sd)
    {
        // NOTE:
        // Saving new chunk store index, otherwise it would be overwritten.
        chunk_store_type saved_new_store_index = chunk_store_index_;
        memcpy(this, sd, sizeof(SocketDataChunk));
        chunk_store_index_ = saved_new_store_index;

        // Resetting the accumulative buffer because it was overwritten.
        ResetAccumBuffer();
    }

    void CopyFromAnotherSocketData(SocketDataChunk* sd)
    {
        // First copying socket data headers.
        PlainCopySocketDataInfoHeaders(sd);

        AccumBuffer* ab = sd->get_accum_buf();

        GW_ASSERT(static_cast<int32_t>(ab->get_accum_len_bytes()) <= GatewayChunkDataSizes[chunk_store_index_]);

        memcpy(data_blob_, ab->get_chunk_orig_buf_ptr(), ab->get_accum_len_bytes());

        // Adjusting the accumulative buffer.
        accum_buf_.AddAccumulatedBytes(ab->get_accum_len_bytes());
    }

    void CopyFromOneChunkIPCSocketData(SocketDataChunk* ipc_sd, int32_t num_bytes_to_copy)
    {
        // First copying socket data headers.
        PlainCopySocketDataInfoHeaders(ipc_sd);

        // Setting some specific accumulative buffer fields.
        accum_buf_.set_desired_accum_bytes(ipc_sd->get_accum_buf()->get_desired_accum_bytes());
        accum_buf_.set_chunk_num_available_bytes(ipc_sd->get_user_data_length_bytes());

        memcpy(data_blob_, (uint8_t*)ipc_sd + ipc_sd->get_user_data_offset_in_socket_data(), num_bytes_to_copy);

        set_user_data_offset_in_socket_data(static_cast<uint16_t>(data_blob_ - (uint8_t*)this));
    }

    // Get Http protocol instance.
    HttpProto* get_http_proto()
    {
        return &http_proto_;
    }

    // WebSockets protocol info.
    WsProto* get_ws_proto()
    {
        return &ws_proto_;
    }

    socket_index_type get_unique_aggr_index()
    {
        return unique_aggr_index_;
    }

    void set_unique_aggr_index(socket_index_type unique_aggr_index)
    {
        unique_aggr_index_ = unique_aggr_index;
    }

    // Gets socket data accumulated data offset.
    uint32_t GetAccumOrigBufferSocketDataOffset()
    {
        return static_cast<uint32_t>(accum_buf_.get_chunk_orig_buf_ptr() - (uint8_t*)this);
    }

    // Prepare buffer to send outside.
    void PrepareForSend(uint8_t *data, uint32_t num_bytes)
    {
        accum_buf_.PrepareForSend(data, num_bytes);
        set_user_data_offset_in_socket_data(static_cast<uint32_t>(data - (uint8_t*)this));
    }

    // Resets safe flags.
    void ResetSafeFlags()
    {
        reset_accumulating_flag();
        reset_to_database_direction_flag();
        reset_complete_header_flag();
    }

    void ResetAllFlags()
    {
        flags_ = 0;
    }

    // Checks if socket data is in correct state.
    uint32_t AssertCorrectState()
    {
        uint8_t* sd = (uint8_t*) this;
        uint8_t* smc = (uint8_t*) this - MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA;

        std::cout << "offset ovl_ = "<< ((uint8_t*)&ovl_ - sd) << std::endl;
        std::cout << "offset session_ = "<< ((uint8_t*)&session_ - sd) << std::endl;
        std::cout << "offset unique_socket_id_ = "<< ((uint8_t*)&unique_socket_id_ - sd) << std::endl;
        std::cout << "offset client_ip_info_ = "<< ((uint8_t*)&client_ip_info_ - sd) << std::endl;
        std::cout << "offset socket_info_index_ = "<< ((uint8_t*)&socket_info_index_ - sd) << std::endl;
        std::cout << "offset user_data_written_bytes_ = "<< ((uint8_t*)accum_buf_.get_chunk_num_available_bytes_addr() - sd) << std::endl;
        std::cout << "offset flags_ = "<< ((uint8_t*)&flags_ - sd) << std::endl;
        std::cout << "offset unique_aggr_index_ = "<< ((uint8_t*)&unique_aggr_index_ - sd) << std::endl;
        std::cout << "offset num_ipc_chunks_ = "<< ((uint8_t*)&user_data_offset_in_socket_data_ - sd) << std::endl;
        std::cout << "offset user_data_offset_in_socket_data_ = "<< ((uint8_t*)&user_data_offset_in_socket_data_ - sd) << std::endl;
        std::cout << "offset type_of_network_oper_ = "<< ((uint8_t*)&type_of_network_oper_ - sd) << std::endl;
        std::cout << "offset type_of_network_protocol_ = "<< ((uint8_t*)&type_of_network_protocol_ - sd) << std::endl;
        std::cout << "offset accum_buf_ = "<< ((uint8_t*)&accum_buf_ - sd) << std::endl;
        std::cout << "offset http_proto_ = "<< ((uint8_t*)&http_proto_ - sd) << std::endl;
        std::cout << "offset ws_proto_ = "<< ((uint8_t*)&ws_proto_ - sd) << std::endl;
        std::cout << "offset accept_or_params_or_temp_data_ = "<< ((uint8_t*)&accept_or_params_or_temp_data_ - sd) << std::endl;
        std::cout << "offset data_blob_ = "<< ((uint8_t*)&data_blob_ - sd) << std::endl;

        std::cout << std::endl;
        std::cout << std::endl;

        std::cout << "sizeof AccumBuffer = "<< sizeof(AccumBuffer) << std::endl;
        std::cout << "sizeof HttpProto = "<< sizeof(HttpProto) << std::endl;
        std::cout << "sizeof WsProto = "<< sizeof(WsProto) << std::endl;
        std::cout << "sizeof accept_or_params_or_temp_data_ = "<< sizeof(accept_or_params_or_temp_data_) << std::endl;
        std::cout << "sizeof ScSessionStruct = "<< sizeof(ScSessionStruct) << std::endl;
        std::cout << "sizeof HttpRequest = "<< sizeof(HttpRequest) << std::endl;
        std::cout << "sizeof http_parser = "<< sizeof(http_parser) << std::endl;
        std::cout << "sizeof WsProto = "<< sizeof(WsProto) << std::endl;
        std::cout << "sizeof OVERLAPPED = "<< sizeof(OVERLAPPED) << std::endl;

        std::cout << std::endl;
        std::cout << std::endl;

        std::cout << "SOCKET_DATA_OFFSET_SESSION = " << ((uint8_t*)&session_ - sd) << std::endl;
        std::cout << "CHUNK_OFFSET_SESSION = " << ((uint8_t*)&session_ - smc) << std::endl;
        std::cout << "CHUNK_OFFSET_SESSION_SCHEDULER_ID = "<< (&session_.scheduler_id_ - smc) << std::endl;
        std::cout << "CHUNK_OFFSET_SESSION_LINEAR_INDEX = "<< ((uint8_t*)&session_.linear_index_ - smc) << std::endl;
        std::cout << "CHUNK_OFFSET_SESSION_RANDOM_SALT = "<< ((uint8_t*)&session_.random_salt_ - smc) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_PARAMS_INFO = "<< (accept_or_params_or_temp_data_ - sd) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_BLOB = "<< (data_blob_ - sd) << std::endl;
        std::cout << "CHUNK_OFFSET_NUM_IPC_CHUNKS = "<< ((uint8_t*)&ovl_ - smc) << std::endl;

        std::cout << "CHUNK_OFFSET_SOCKET_FLAGS = "<< ((uint8_t*)&flags_ - smc) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE = "<< ((uint8_t*)&type_of_network_protocol_ - sd) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_CLIENT_IP = "<< ((uint8_t*)&client_ip_info_ - sd) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_HTTP_REQUEST = "<< ((uint8_t*)get_http_proto()->get_http_request() - sd) << std::endl;
        std::cout << "SOCKET_DATA_NUM_CLONE_BYTES = "<< ((uint8_t*)&accept_or_params_or_temp_data_ - sd) << std::endl;
        std::cout << "CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA = "<< ((uint8_t*)&user_data_offset_in_socket_data_ - smc) << std::endl;
        std::cout << "CHUNK_OFFSET_USER_DATA_TOTAL_LENGTH = "<< ((uint8_t*)accum_buf_.get_desired_accum_bytes_addr() - smc) << std::endl;

        std::cout << "CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES = "<< ((uint8_t*)accum_buf_.get_chunk_num_available_bytes_addr() - smc) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID = "<< ((uint8_t*)&unique_socket_id_ - sd) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER = "<< ((uint8_t*)&socket_info_index_ - sd) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_WS_OPCODE = "<< (&get_ws_proto()->get_frame_info()->opcode_ - sd) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_BOUND_WORKER_ID = "<< ((uint8_t*)&(session_.gw_worker_id_) - sd) << std::endl;
        std::cout << "CHUNK_OFFSET_WS_PAYLOAD_LEN = "<< ((uint8_t*)&(ws_proto_.get_frame_info()->payload_len_) - smc) << std::endl;
        std::cout << "CHUNK_OFFSET_WS_PAYLOAD_OFFSET_IN_SD = "<< ((uint8_t*)&(ws_proto_.get_frame_info()->payload_offset_) - smc) << std::endl;
        std::cout << "SOCKET_DATA_OFFSET_WS_CHANNEL_ID = "<< ((uint8_t*)&accept_or_params_or_temp_data_ - sd) << std::endl;

        GW_ASSERT(8 == sizeof(SOCKET));
        GW_ASSERT(8 == sizeof(random_salt_type));
        GW_ASSERT(8 == sizeof(BMX_HANDLER_TYPE));
        GW_ASSERT(4 == sizeof(core::chunk_index));
        GW_ASSERT(16 == sizeof(ScSessionStruct));
        GW_ASSERT(sizeof(ScSessionStruct) == MixedCodeConstants::SESSION_STRUCT_SIZE);

        GW_ASSERT(MixedCodeConstants::SHM_CHUNK_SIZE == starcounter::core::chunk_size);
        GW_ASSERT(MixedCodeConstants::CHUNK_LINK_SIZE == shared_memory_chunk::link_size);

        GW_ASSERT((sd - smc) == MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA);

        GW_ASSERT(((uint8_t*)&session_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SESSION);
        GW_ASSERT((&session_.scheduler_id_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SESSION_SCHEDULER_ID);
        GW_ASSERT(((uint8_t*)&session_.linear_index_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SESSION_LINEAR_INDEX);
        GW_ASSERT(((uint8_t*)&session_.random_salt_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SESSION_RANDOM_SALT);

        GW_ASSERT((accept_or_params_or_temp_data_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_PARAMS_INFO);

        GW_ASSERT((data_blob_ - sd) == SOCKET_DATA_OFFSET_BLOB);

        GW_ASSERT(((uint8_t*)&ovl_ - smc) == MixedCodeConstants::CHUNK_OFFSET_NUM_IPC_CHUNKS);

        GW_ASSERT(((uint8_t*)&flags_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS);

        GW_ASSERT(((uint8_t*)&type_of_network_protocol_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE);

        GW_ASSERT(((uint8_t*)&client_ip_info_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_CLIENT_IP);

        GW_ASSERT(((uint8_t*)get_http_proto()->get_http_request() - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_HTTP_REQUEST);

        GW_ASSERT(((uint8_t*)&accept_or_params_or_temp_data_ - sd) == MixedCodeConstants::SOCKET_DATA_NUM_CLONE_BYTES);

        GW_ASSERT(((uint8_t*)&user_data_offset_in_socket_data_ - smc) == MixedCodeConstants::CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

        GW_ASSERT(((uint8_t*)accum_buf_.get_desired_accum_bytes_addr() - smc) == MixedCodeConstants::CHUNK_OFFSET_USER_DATA_TOTAL_LENGTH);

        GW_ASSERT(((uint8_t*)accum_buf_.get_chunk_num_available_bytes_addr() - smc) == MixedCodeConstants::CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES);

        GW_ASSERT(((uint8_t*)&unique_socket_id_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID);

        GW_ASSERT(((uint8_t*)&socket_info_index_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER);

        GW_ASSERT((&get_ws_proto()->get_frame_info()->opcode_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_WS_OPCODE);

        GW_ASSERT(((uint8_t*)&(session_.gw_worker_id_) - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_BOUND_WORKER_ID);

        GW_ASSERT(((uint8_t*)&(ws_proto_.get_frame_info()->payload_len_) - smc) == MixedCodeConstants::CHUNK_OFFSET_WS_PAYLOAD_LEN);

        GW_ASSERT(((uint8_t*)&(ws_proto_.get_frame_info()->payload_offset_) - smc) == MixedCodeConstants::CHUNK_OFFSET_WS_PAYLOAD_OFFSET_IN_SD);

        GW_ASSERT(((uint8_t*)&accept_or_params_or_temp_data_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_WS_CHANNEL_ID);

        return 0;
    }

    // Sets unique socket id.
    void set_unique_socket_id(random_salt_type unique_socket_id)
    {
        unique_socket_id_ = unique_socket_id;
    }

    // Returns all linked chunks except the main one.
    uint32_t ReturnExtraLinkedChunks(GatewayWorker* gw);

    // Getting type of network protocol.
    MixedCodeConstants::NetworkProtocolType get_type_of_network_protocol()
    {
        return (MixedCodeConstants::NetworkProtocolType) type_of_network_protocol_;
    }

    // Checking if its a WebSocket protocol.
    bool is_web_socket()
    {
        return MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS == get_type_of_network_protocol();
    }

    // Getting to database direction flag.
    bool get_to_database_direction_flag()
    {
        return flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
    }

    // Setting to database direction flag.
    void set_to_database_direction_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
    }

    // ReSetting to database direction flag.
    void reset_to_database_direction_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
    }

    // Getting socket just send flag.
    bool get_socket_just_send_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_JUST_SEND) != 0;
    }

    // Setting socket just send flag.
    void reset_socket_just_send_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_JUST_SEND;
    }

    // Getting socket representer flag.
    bool get_socket_representer_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_SOCKET_REPRESENTER) != 0;
    }

    // Setting socket representer flag.
    void set_socket_representer_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_SOCKET_REPRESENTER;
    }

    // Resetting socket representer flag.
    void reset_socket_representer_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_SOCKET_REPRESENTER;
    }

    bool get_ws_upgrade_approved_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_UPGRADE_APPROVED) != 0;
    }

    void set_ws_upgrade_approved_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_UPGRADE_APPROVED;
    }

    void reset_ws_upgrade_approved_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_UPGRADE_APPROVED;
    }

    bool get_just_push_disconnect_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_JUST_PUSH_DISCONNECT) != 0;
    }

    void set_just_push_disconnect_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_JUST_PUSH_DISCONNECT;
    }

    void reset_just_push_disconnect_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_JUST_PUSH_DISCONNECT;
    }

    // Getting proxying unknown protocol flag.
    bool get_unknown_proxied_proto_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO) != 0;
    }

    // Setting proxying unknown protocol flag.
    void set_unknown_proxied_proto_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO;
    }

    // Setting accumulating flag.
    void set_accumulating_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_ACCUMULATING_STATE;
    }

    // Getting accumulating flag.
    bool get_accumulating_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_ACCUMULATING_STATE) != 0;
    }

    // ReSetting accumulating flag.
    void reset_accumulating_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_ACCUMULATING_STATE;
    }

    // Getting disconnect after send flag.
    bool get_disconnect_after_send_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND) != 0;
    }

    // Setting disconnect after send flag.
    void set_disconnect_after_send_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND;
    }

    // ReSetting disconnect after send flag.
    void reset_disconnect_after_send_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND;
    }

    // Getting gracefully close flag.
    bool get_gracefully_close_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_GRACEFULLY_CLOSE) != 0;
    }

    // Setting gracefully close flag.
    void set_gracefully_close_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_GRACEFULLY_CLOSE;
    }

    // ReSetting gracefully close flag.
    void reset_gracefully_close_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_GRACEFULLY_CLOSE;
    }

    bool get_ws_upgrade_request_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_UPGRADE_REQUEST) != 0;
    }

    void set_ws_upgrade_request_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_UPGRADE_REQUEST;
    }

    void reset_ws_upgrade_request_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_UPGRADE_REQUEST;
    }

    bool get_aggregated_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_AGGREGATED) != 0;
    }

    void set_aggregated_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_AGGREGATED;
    }

    void reset_aggregated_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_AGGREGATED;
    }

    bool get_on_host_accumulation_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_ON_HOST_ACCUMULATION) != 0;
    }

    void set_on_host_accumulation_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_ON_HOST_ACCUMULATION;
    }

    void reset_on_host_accumulation_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_ON_HOST_ACCUMULATION;
    }

    // Getting disconnect socket flag.
    bool get_disconnect_socket_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_JUST_DISCONNECT) != 0;
    }

    // Getting complete header flag.
    bool get_complete_header_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_COMPLETE_HEADER) != 0;
    }

    // Setting complete header flag.
    void set_complete_header_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_COMPLETE_HEADER;
    }

    // ReSetting complete header flag.
    void reset_complete_header_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::HTTP_WS_FLAGS_COMPLETE_HEADER;
    }

    // Getting scheduler id.
    scheduler_id_type get_scheduler_id()
    {
        return session_.scheduler_id_;
    }

    // Binds current socket to some scheduler.
    void BindSocketToScheduler(GatewayWorker* gw, WorkerDbInterface *db);

    // Setting scheduler id.
    void set_scheduler_id(scheduler_id_type sched_id)
    {
        session_.scheduler_id_ = sched_id;
    }

    // Getting session index.
    session_index_type get_session_index()
    {
        return session_.linear_index_;
    }

    // Getting session salt.
    random_salt_type get_session_salt()
    {
        return session_.random_salt_;
    }

    ScSessionStruct get_session_copy() {
        return session_;
    }

    // Determines if this sd has active session attached.
    bool HasActiveSession()
    {
        return INVALID_SESSION_INDEX != session_.linear_index_;
    }

    // Getting unique id.
    random_salt_type get_unique_socket_id()
    {
        return unique_socket_id_;
    }

    // Setting type of network protocol.
    void set_type_of_network_protocol(MixedCodeConstants::NetworkProtocolType proto_type)
    {
        type_of_network_protocol_ = proto_type;
    }

    // Client IP information.
    ip_info_type get_client_ip_info()
    {
        return client_ip_info_;
    }

    // Sets client IP information.
    void set_client_ip_info(ip_info_type client_ip_info)
    {
        client_ip_info_ = client_ip_info;
    }

    // Returns socket info index.
    socket_index_type get_socket_info_index()
    {
        return socket_info_index_;
    }

    // Sets socket info index.
    void set_socket_info_index(socket_index_type socket_info_index)
    {
        socket_info_index_ = socket_info_index;
    }

    // Sets number of IPC chunks.
    void SetNumberOfIPCChunks(uint16_t num_ip_chunks)
    {
        *(uint16_t*)(&ovl_) = num_ip_chunks;
    }

    void set_handler_id(BMX_HANDLER_TYPE handler_id) {
        *(BMX_HANDLER_TYPE*)((char*)(&ovl_) + 2) = handler_id;
    }

    BMX_HANDLER_TYPE get_handler_id() {
        return *(BMX_HANDLER_TYPE*)((char*)(&ovl_) + 2);
    }

    // Copies IPC chunks to gateway chunk.
    uint32_t CopyIPCChunksToGatewayChunk(
        WorkerDbInterface* worker_db,
        SocketDataChunk* sd);

    // Copies gateway chunk to IPC chunks.
    uint32_t CopyGatewayChunkToIPCChunks(
        WorkerDbInterface* worker_db,
        SocketDataChunk** new_ipc_sd,
        core::chunk_index* db_chunk_index,
        uint16_t* num_ipc_chunks);

    // Overlapped structure used for WinSock.
    OVERLAPPED* get_ovl()
    {
        return &ovl_;
    }

    // Current type of network operation.
    SocketOperType get_type_of_network_oper()
    {
        return (SocketOperType) type_of_network_oper_;
    }

    // Current type of network operation.
    void set_type_of_network_oper(SocketOperType type_of_network_oper)
    {
        type_of_network_oper_ = (uint8_t)type_of_network_oper;
    }

    // Accept data or parameters data.
    uint8_t* const get_accept_or_params_data()
    {
        return accept_or_params_or_temp_data_;
    }

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    void set_user_data_offset_in_socket_data(uint16_t user_data_offset_in_socket_data)
    {
        user_data_offset_in_socket_data_ = user_data_offset_in_socket_data;
    }

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    uint16_t get_user_data_offset_in_socket_data()
    {
        return user_data_offset_in_socket_data_;
    }

    // Size in bytes of written user data.
    uint32_t get_user_data_length_bytes()
    {
        return accum_buf_.get_chunk_num_available_bytes();
    }

    // Data buffer chunk.
    AccumBuffer* get_accum_buf()
    {
        return &accum_buf_;
    }

    // Resets accumulating buffer to its default socket data values.
    void ResetAccumBuffer()
    {
        accum_buf_.Init(GatewayChunkDataSizes[chunk_store_index_], data_blob_, true);
    }

    // Exchanges sockets during proxying.
    void ExchangeToProxySocket(GatewayWorker* gw);

    // Initializes socket data that comes from database.
    void PreInitSocketDataFromDb(GatewayWorker* gw);

    // Initialization.
    void Init(
        GatewayWorker* gw,
        socket_index_type socket_info_index);

    // Resetting socket.
    void ResetOnDisconnect(GatewayWorker *gw);

    // Returns pointer to the beginning of user data.
    uint8_t* UserDataBuffer()
    {
        return (uint8_t*)this + user_data_offset_in_socket_data_;
    }

    // Resets user data offset.
    void ResetUserDataOffset()
    {
        user_data_offset_in_socket_data_ = static_cast<uint32_t> (data_blob_ - (uint8_t*)this);
    }

    // Start receiving on socket.
    uint32_t Receive(GatewayWorker *gw, uint32_t *num_bytes);

    // Start sending on socket.
    uint32_t Send(GatewayWorker* gw, uint32_t *numBytes);

    // Start accepting on socket.
    uint32_t Accept(GatewayWorker* gw);

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    uint32_t SetAcceptSocketOptions(GatewayWorker* gw);

    // Start connecting on socket.
    uint32_t Connect(GatewayWorker* gw, sockaddr_in *serverAddr);

    // Start disconnecting socket.
    uint32_t Disconnect(GatewayWorker *gw);

    // Puts socket data to database.
    void PrepareToDb()
    {
        // Setting database data direction flag.
        set_to_database_direction_flag();
    }

    // Puts socket data from database.
    void PrepareFromDb()
    {
        // Removing database data direction flag.
        reset_to_database_direction_flag();
    }

    // Clones existing socket data chunk.
    uint32_t CloneToReceive(GatewayWorker *gw);

    // Clone current socket data to simply send it.
    uint32_t CloneToPush(GatewayWorker*gw, SocketDataChunk** new_sd);

    // Clone current socket data to a bigger one.
    static uint32_t ChangeToBigger(
        GatewayWorker*gw,
        SocketDataChunkRef sd,
        int32_t data_size = 0);

    // Clone current socket data to simply send it.
    uint32_t CreateSocketDataFromBigBuffer(
        GatewayWorker*gw,
        socket_index_type socket_info_index,
        int32_t data_len,
        uint8_t* data,
        SocketDataChunk** new_sd);

    // Attaches session to socket data.
    void AssignSession(ScSessionStruct& session)
    {
        // Setting session for this socket data.
        session_ = session;
    }

    // Getting session structure.
    ScSessionStruct* GetSessionStruct()
    {
        return &session_;
    }

    // Resets socket data session.
    void ResetSdSession()
    {
        worker_id_type saved_worker_id = get_bound_worker_id();

        session_.Reset();

        session_.gw_worker_id_ = saved_worker_id;
    }

    // Setting new unique socket number.
    void GenerateUniqueSocketInfoIds(GatewayWorker* gw);
};

} // namespace network
} // namespace starcounter

#endif // SOCKET_DATA_HPP