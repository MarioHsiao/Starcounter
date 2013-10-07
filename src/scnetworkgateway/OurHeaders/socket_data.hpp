#pragma once
#ifndef SOCKET_DATA_HPP
#define SOCKET_DATA_HPP

namespace starcounter {
namespace network {

class GatewayWorker;
class HttpWsProto;

enum SOCKET_DATA_FLAGS
{
    SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION = 1,
    SOCKET_DATA_FLAGS_SOCKET_REPRESENTER = 2,
    SOCKET_DATA_FLAGS_ACCUMULATING_STATE = 8,
    SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND = MixedCodeConstants::SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND,
    SOCKET_DATA_FLAGS_ACTIVE_CONN = 32,
    SOCKET_DATA_FLAGS_JUST_SEND = MixedCodeConstants::SOCKET_DATA_FLAGS_JUST_SEND,
    SOCKET_DATA_FLAGS_JUST_DISCONNECT = MixedCodeConstants::SOCKET_DATA_FLAGS_DISCONNECT,
    SOCKET_DATA_FLAGS_TRIGGER_DISCONNECT = 256,
    HTTP_WS_FLAGS_COMPLETE_HEADER = 512,
    HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET = 1024,
    HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO = 2048,
    HTTP_WS_FLAGS_GRACEFULLY_CLOSE = MixedCodeConstants::HTTP_WS_FLAGS_GRACEFULLY_CLOSE,
    SOCKET_DATA_FLAGS_BIG_ACCUMULATION_CHUNK = 8192,
    SOCKET_DATA_FLAGS_AGGREGATION_SD = SOCKET_DATA_FLAGS_BIG_ACCUMULATION_CHUNK * 2
};

// Socket data chunk.
class WorkerDbInterface;
class SocketDataChunk
{
    // Overlapped structure used for WinSock
    // (must be 8-bytes aligned).
    OVERLAPPED ovl_;

    // Starcounter session information.
    ScSessionStruct session_;

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
    session_index_type socket_info_index_;

    // Offset in bytes from the beginning of the socket data to place
    // where user data should be written.
    uint32_t user_data_offset_in_socket_data_;

    // Maximum bytes that user code can write to this chunk.
    uint32_t max_user_data_bytes_;

    // Size in bytes of written user data.
    uint32_t user_data_written_bytes_;

    // Corresponding chunk index.
    core::chunk_index chunk_index_;

    // Indicates how many chunks are associated with this socket data (normally 1).
    uint32_t num_chunks_;

    // Socket data flags.
    uint32_t flags_;

    /////////////////////////
    // 1 bytes aligned data.
    /////////////////////////

    // Index into databases array.
    db_index_type db_index_;

    // Current type of network operation.
    uint8_t type_of_network_oper_;

    // Type of network protocol.
    uint8_t type_of_network_protocol_;

    // Target database index.
    db_index_type target_db_index_;

    /////////////////////////
    // Data structures.
    /////////////////////////

    // Accumulative buffer chunk.
    AccumBuffer accum_buf_;

    // HTTP protocol instance.
    HttpWsProto http_ws_proto_;

    // Pointer to gateway chunk if any.
    GatewayMemoryChunk* gw_chunk_;

    struct { int64_t unique_aggr_index_; uint64_t unused_; } align_16bytes;

    // Accept or parameters or temporary data.
    uint8_t accept_or_params_or_temp_data_[MixedCodeConstants::PARAMS_INFO_MAX_SIZE_BYTES];

    // Blob buffer itself.
    uint8_t data_blob_[SOCKET_DATA_BLOB_SIZE_BYTES];

public:

    // Has gateway chunk attached?
    bool HasGatewayChunk()
    {
        return NULL != gw_chunk_;
    }

    GatewayMemoryChunk* get_gw_chunk()
    {
        return gw_chunk_;
    }

    // Returns gateway chunk to gateway if any.
    void ReturnGatewayChunk();

    int64_t get_unique_aggr_index()
    {
        return align_16bytes.unique_aggr_index_;
    }

    void set_unique_aggr_index(int64_t unique_aggr_index)
    {
        align_16bytes.unique_aggr_index_ = unique_aggr_index;
    }

    // Gets socket data accumulated data offset.
    uint32_t GetAccumOrigBufferSocketDataOffset()
    {
        return static_cast<uint32_t>(accum_buf_.get_chunk_orig_buf_ptr() - (uint8_t*)this);
    }

    // Resets safe flags.
    void ResetSafeFlags()
    {
        reset_accumulating_flag();
        reset_to_database_direction_flag();
        reset_complete_header_flag();
    }

#ifdef GW_LOOPED_TEST_MODE

    // Pushing given sd to network emulation queue.
    void PushToMeasuredNetworkEmulationQueue(GatewayWorker* gw);
    void PushToPreparationNetworkEmulationQueue(GatewayWorker* gw);

#endif

    // Checks if socket data is in correct state.
    uint32_t AssertCorrectState()
    {
        uint8_t* sd = (uint8_t*) this;
        uint8_t* smc = (uint8_t*)get_smc();

        GW_ASSERT(8 == sizeof(SOCKET));
        GW_ASSERT(8 == sizeof(random_salt_type));
        GW_ASSERT(8 == sizeof(BMX_HANDLER_TYPE));
        GW_ASSERT(4 == sizeof(core::chunk_index));

        GW_ASSERT(MixedCodeConstants::SHM_CHUNK_SIZE == starcounter::core::chunk_size);
        GW_ASSERT(MixedCodeConstants::CHUNK_LINK_SIZE == shared_memory_chunk::link_size);

        GW_ASSERT((&session_.scheduler_id_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SESSION_SCHEDULER_ID);
        GW_ASSERT(((uint8_t*)&session_.linear_index_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SESSION_LINEAR_INDEX);
        GW_ASSERT(((uint8_t*)&session_.random_salt_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SESSION_RANDOM_SALT);
        GW_ASSERT(((uint8_t*)&session_.reserved_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SESSION_RESERVED_INDEX);

        GW_ASSERT((accept_or_params_or_temp_data_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_PARAMS_INFO);

        GW_ASSERT((data_blob_ - sd) == SOCKET_DATA_OFFSET_BLOB);

        GW_ASSERT(((uint8_t*)&num_chunks_ - smc) == MixedCodeConstants::CHUNK_OFFSET_NUM_CHUNKS);

        GW_ASSERT(((uint8_t*)&flags_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS);

        GW_ASSERT(((uint8_t*)&type_of_network_protocol_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE);

        GW_ASSERT(((uint8_t*)&client_ip_info_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_CLIENT_IP);

        GW_ASSERT(((uint8_t*)http_ws_proto_.get_http_request() - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_HTTP_REQUEST);

        GW_ASSERT(((uint8_t*)(&accum_buf_) - sd) == MixedCodeConstants::SOCKET_DATA_NUM_CLONE_BYTES);

        GW_ASSERT(((uint8_t*)&user_data_offset_in_socket_data_ - smc) == MixedCodeConstants::CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

        GW_ASSERT(((uint8_t*)&max_user_data_bytes_ - smc) == MixedCodeConstants::CHUNK_OFFSET_MAX_USER_DATA_BYTES);

        GW_ASSERT(((uint8_t*)&user_data_written_bytes_ - smc) == MixedCodeConstants::CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES);

        GW_ASSERT(((uint8_t*)&unique_socket_id_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID);

        GW_ASSERT(((uint8_t*)&socket_info_index_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER);

        GW_ASSERT((&http_ws_proto_.get_ws_proto()->get_frame_info()->opcode_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_WS_OPCODE);

        return 0;
    }

    // Sets unique socket id.
    void set_unique_socket_id(random_salt_type unique_socket_id)
    {
        unique_socket_id_ = unique_socket_id;
    }

    // Deletes global session and sends message to database to delete session there.
    uint32_t SendDeleteSession(GatewayWorker* gw);

    // Clone current socket data to another database.
    uint32_t CloneToAnotherDatabase(
        GatewayWorker* gw,
        int32_t new_db_index,
        SocketDataChunk** new_sd);

    // Returns all linked chunks except the main one.
    uint32_t ReturnExtraLinkedChunks(GatewayWorker* gw);

    // Getting type of network protocol.
    MixedCodeConstants::NetworkProtocolType get_type_of_network_protocol()
    {
        return (MixedCodeConstants::NetworkProtocolType) type_of_network_protocol_;
    }

    // Checking if its a WebSocket protocol.
    bool IsWebSocket()
    {
        return MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS == get_type_of_network_protocol();
    }

    // Gets last linked smc.
    shared_memory_chunk* GetLastLinkedSmc(GatewayWorker* gw);

    // Continues fill up if needed.
    uint32_t ContinueAccumulation(GatewayWorker* gw, bool* is_accumulated);

    // Chunk data offset.
    int32_t GetAccumOrigBufferChunkOffset()
    {
        return static_cast<int32_t> (accum_buf_.get_chunk_orig_buf_ptr() - (uint8_t*)get_smc());
    }

    // Getting to database direction flag.
    bool get_to_database_direction_flag()
    {
        return flags_ & SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
    }

    // Setting to database direction flag.
    void set_to_database_direction_flag()
    {
        flags_ |= SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
    }

    // ReSetting to database direction flag.
    void reset_to_database_direction_flag()
    {
        flags_ &= ~SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION;
    }

    // Getting socket just send flag.
    bool get_socket_just_send_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_JUST_SEND) != 0;
    }

    // Setting socket just send flag.
    void reset_socket_just_send_flag()
    {
        flags_ &= ~SOCKET_DATA_FLAGS_JUST_SEND;
    }

    // Getting socket representer flag.
    bool get_socket_representer_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_SOCKET_REPRESENTER) != 0;
    }

    // Setting socket representer flag.
    void set_socket_representer_flag()
    {
        GW_ASSERT(false == get_socket_trigger_disconnect_flag());

        flags_ |= SOCKET_DATA_FLAGS_SOCKET_REPRESENTER;
    }

    // Resetting socket representer flag.
    void reset_socket_representer_flag()
    {
        flags_ &= ~SOCKET_DATA_FLAGS_SOCKET_REPRESENTER;
    }

    // Getting socket trigger disconnect flag.
    bool get_socket_trigger_disconnect_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_TRIGGER_DISCONNECT) != 0;
    }

    // Setting socket trigger disconnect flag.
    void set_socket_trigger_disconnect_flag()
    {
        // Do nothing if already a socket representer.
        if (get_socket_representer_flag())
            return;

        flags_ |= SOCKET_DATA_FLAGS_TRIGGER_DISCONNECT;
    }

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Getting socket diagnostics active connection flag.
    bool get_socket_diag_active_conn_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_ACTIVE_CONN) != 0;
    }

    // Setting socket diagnostics active connection flag.
    void set_socket_diag_active_conn_flag()
    {
        flags_ |= SOCKET_DATA_FLAGS_ACTIVE_CONN;
    }

    // ReSetting socket diagnostics active connection flag.
    void reset_socket_diag_active_conn_flag()
    {
        flags_ &= ~SOCKET_DATA_FLAGS_ACTIVE_CONN;
    }

#endif

#ifdef GW_PROXY_MODE

    // Getting proxying flag.
    bool get_proxied_server_socket_flag()
    {
        return (flags_ & HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET) != 0;
    }

    // Setting proxying flag.
    void set_proxied_server_socket_flag()
    {
        flags_ |= HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET;
    }

    // ReSetting proxying flag.
    void reset_proxied_server_socket_flag()
    {
        flags_ &= ~HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET;
    }

    // Getting proxying unknown protocol flag.
    bool get_unknown_proxied_proto_flag()
    {
        return (flags_ & HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO) != 0;
    }

    // Setting proxying unknown protocol flag.
    void set_unknown_proxied_proto_flag()
    {
        flags_ |= HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO;
    }

#endif

    // Getting accumulating flag.
    bool get_accumulating_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_ACCUMULATING_STATE) != 0;
    }

    // Setting accumulating flag.
    void set_accumulating_flag()
    {
        // Saving original data pointer.
        accum_buf_.SaveFirstChunkOrigBufPtr();

        flags_ |= SOCKET_DATA_FLAGS_ACCUMULATING_STATE;
    }

    // ReSetting accumulating flag.
    void reset_accumulating_flag()
    {
        flags_ &= ~SOCKET_DATA_FLAGS_ACCUMULATING_STATE;
    }

    // Getting disconnect after send flag.
    bool get_disconnect_after_send_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND) != 0;
    }

    // Setting disconnect after send flag.
    void set_disconnect_after_send_flag()
    {
        flags_ |= SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND;
    }

    // ReSetting disconnect after send flag.
    void reset_disconnect_after_send_flag()
    {
        flags_ &= ~SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND;
    }

    // Getting gracefully close flag.
    bool get_gracefully_close_flag()
    {
        return (flags_ & HTTP_WS_FLAGS_GRACEFULLY_CLOSE) != 0;
    }

    // Setting gracefully close flag.
    void set_gracefully_close_flag()
    {
        flags_ |= HTTP_WS_FLAGS_GRACEFULLY_CLOSE;
    }

    // ReSetting gracefully close flag.
    void reset_gracefully_close_flag()
    {
        flags_ &= ~HTTP_WS_FLAGS_GRACEFULLY_CLOSE;
    }

    bool get_aggregation_sd_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_AGGREGATION_SD) != 0;
    }

    void set_aggregation_sd_flag()
    {
        flags_ |= SOCKET_DATA_FLAGS_AGGREGATION_SD;
    }

    void reset_aggregation_sd_flag()
    {
        flags_ &= ~SOCKET_DATA_FLAGS_AGGREGATION_SD;
    }

    // Getting big accumulation chunk.
    bool get_big_accumulation_chunk_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_BIG_ACCUMULATION_CHUNK) != 0;
    }

    // Setting big accumulation chunk.
    void set_big_accumulation_chunk_flag()
    {
        flags_ |= SOCKET_DATA_FLAGS_BIG_ACCUMULATION_CHUNK;
    }

    // ReSetting big accumulation chunk.
    void reset_big_accumulation_chunk_flag()
    {
        flags_ &= ~SOCKET_DATA_FLAGS_BIG_ACCUMULATION_CHUNK;
    }

    // Getting disconnect socket flag.
    bool get_disconnect_socket_flag()
    {
        return (flags_ & SOCKET_DATA_FLAGS_JUST_DISCONNECT) != 0;
    }

    // Getting complete header flag.
    bool get_complete_header_flag()
    {
        return (flags_ & HTTP_WS_FLAGS_COMPLETE_HEADER) != 0;
    }

    // Setting complete header flag.
    void set_complete_header_flag()
    {
        flags_ |= HTTP_WS_FLAGS_COMPLETE_HEADER;
    }

    // ReSetting complete header flag.
    void reset_complete_header_flag()
    {
        flags_ &= ~HTTP_WS_FLAGS_COMPLETE_HEADER;
    }

    // Getting scheduler id.
    scheduler_id_type get_scheduler_id()
    {
        return session_.scheduler_id_;
    }

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

    // Checking if unique socket number is correct.
    bool CompareUniqueSocketId()
    {
        return g_gateway.CompareUniqueSocketId(socket_info_index_, unique_socket_id_);
    }

    // Setting type of network protocol.
    void set_type_of_network_protocol(MixedCodeConstants::NetworkProtocolType proto_type)
    {
        type_of_network_protocol_ = proto_type;
    }

    // Setting type of network protocol.
    void SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType proto_type)
    {
        type_of_network_protocol_ = proto_type;

        g_gateway.SetTypeOfNetworkProtocol(socket_info_index_, proto_type);
    }

    // Setting aggregated flag on socket.
    void SetSocketAggregatedFlag()
    {
        g_gateway.SetSocketAggregatedFlag(socket_info_index_);
    }

    // Getting aggregated flag on socket.
    bool GetSocketAggregatedFlag()
    {
        return g_gateway.GetSocketAggregatedFlag(socket_info_index_);
    }

    // Getting saved user handler id.
    BMX_HANDLER_TYPE GetSavedUserHandlerId()
    {
        return g_gateway.GetSavedUserHandlerId(socket_info_index_);
    }

    // Setting user handler id.
    void SetSavedUserHandlerId(BMX_HANDLER_TYPE saved_user_handler_id)
    {
        g_gateway.SetSavedUserHandlerId(socket_info_index_, saved_user_handler_id);
    }

    // Setting new unique socket number.
    void GenerateUniqueSocketInfoIds(scheduler_id_type scheduler_id)
    {
        session_.scheduler_id_ = scheduler_id;
        unique_socket_id_ = g_gateway.GenerateUniqueSocketInfoIds(socket_info_index_, session_.scheduler_id_);
    }

    // Sets session if socket is correct.
    void SetGlobalSessionIfEmpty()
    {
        // Checking unique socket id and session.
        if (!g_gateway.IsGlobalSessionActive(socket_info_index_))
            g_gateway.SetGlobalSessionCopy(socket_info_index_, session_);
    }

    // Forcedly sets session if socket is correct.
    void ForceSetGlobalSessionIfEmpty()
    {
        // Checking unique socket id and session.
        if (session_.IsActive())
            g_gateway.SetGlobalSessionCopy(socket_info_index_, session_);
    }

    // Updates connection timestamp if socket is correct.
    void UpdateConnectionTimeStamp()
    {
        g_gateway.UpdateSocketTimeStamp(socket_info_index_);
    }

    // Sets session if socket is correct.
    void SetSdSessionIfEmpty()
    {
        if ((!session_.IsActive()) && (g_gateway.IsGlobalSessionActive(socket_info_index_)))
            session_ = g_gateway.GetGlobalSessionCopy(socket_info_index_);
    }

    // Releases socket info index.
    void ReleaseSocketIndex(GatewayWorker* gw)
    {
        g_gateway.ReleaseSocketIndex(gw, socket_info_index_);
    }

    // Deletes global session.
    void DeleteGlobalSessionOnDisconnect()
    {
        g_gateway.DeleteGlobalSession(socket_info_index_);
    }

    // Checks if global session data is active.
    bool CompareGlobalSessionSalt()
    {
        return g_gateway.CompareGlobalSessionSalt(socket_info_index_, session_.random_salt_);
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

    // Returns port index.
    int32_t GetPortIndex()
    {
        return g_gateway.GetPortIndex(socket_info_index_);
    }

    // Returns scheduler id.
    scheduler_id_type GetSchedulerId()
    {
        return g_gateway.GetSchedulerId(socket_info_index_);
    }

    // Returns socket.
    SOCKET GetSocket()
    {
        return g_gateway.GetSocket(socket_info_index_);
    }

    // Returns true if has proxy socket.
    bool HasProxySocket()
    {
        return g_gateway.HasProxySocket(socket_info_index_);
    }

    // Returns aggregation socket index.
    session_index_type GetAggregationSocketIndex()
    {
        return g_gateway.GetAggregationSocketIndex(socket_info_index_);
    }

    // Sets aggregation socket index.
    void SetAggregationSocketIndex(session_index_type aggr_socket_index)
    {
        g_gateway.SetAggregationSocketIndex(socket_info_index_, aggr_socket_index);
    }

    // Returns proxy socket index.
    session_index_type GetProxySocketIndex()
    {
        return g_gateway.GetProxySocketIndex(socket_info_index_);
    }

    // Sets proxy socket index.
    void SetProxySocketIndex(session_index_type proxy_socket_index)
    {
        g_gateway.SetProxySocketIndex(socket_info_index_, proxy_socket_index);
    }

    // Returns socket info index.
    session_index_type get_socket_info_index()
    {
        return socket_info_index_;
    }

    // Sets socket info index.
    void set_socket_info_index(session_index_type socket_info_index)
    {
        socket_info_index_ = socket_info_index;
    }

    // Returns SMC representing this chunk.
    shared_memory_chunk* get_smc()
    {
        return (shared_memory_chunk*)((uint8_t*)this - MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA);
    }

    // Set new chunk index.
    void set_chunk_index(core::chunk_index the_chunk_index)
    {
        chunk_index_ = the_chunk_index;
    }

    // Gets chunk index.
    core::chunk_index& get_chunk_index()
    {
        return chunk_index_;
    }

    // Gets extra chunk index.
    core::chunk_index GetNextLinkedChunkIndex()
    {
        return get_smc()->get_link();
    }

#ifdef GW_TESTING_MODE

    // Getting data blob pointer.
    uint8_t* get_data_blob()
    {
        return data_blob_;
    }

#endif

    // Gets number of data bytes left in chunk.
    int32_t GetNumRemainingDataBytesInChunk(uint8_t* payload)
    {
        return static_cast<int32_t> (MixedCodeConstants::CHUNK_MAX_DATA_BYTES - (payload - (uint8_t*)get_smc()));
    }

    // Returns number of used chunks.
    uint32_t get_num_chunks()
    {
        return num_chunks_;
    }

    // Getting and linking more receiving chunks.
    uint32_t GetReceivingChunks(GatewayWorker *gw, uint32_t num_bytes);

    // Create WSA buffers.
    uint32_t CreateWSABuffers(
        WorkerDbInterface* worker_db,
        shared_memory_chunk* head_smc,
        uint32_t head_chunk_offset_bytes,
        uint32_t head_chunk_num_bytes,
        uint32_t total_bytes);

    // Get Http protocol instance.
    HttpWsProto* get_http_ws_proto()
    {
        return &http_ws_proto_;
    }

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

    // Size in bytes of written user data.
    void set_user_data_written_bytes(uint32_t user_data_written_bytes)
    {
        user_data_written_bytes_ = user_data_written_bytes;
    }

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    void set_user_data_offset_in_socket_data(uint32_t user_data_offset_in_socket_data)
    {
        user_data_offset_in_socket_data_ = user_data_offset_in_socket_data;

        // Correcting max user data bytes accordingly.
        max_user_data_bytes_ = MixedCodeConstants::SOCKET_DATA_MAX_SIZE - user_data_offset_in_socket_data_;
    }

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    uint32_t get_user_data_offset_in_socket_data()
    {
        return user_data_offset_in_socket_data_;
    }

    // Size in bytes of written user data.
    uint32_t get_user_data_written_bytes()
    {
        return user_data_written_bytes_;
    }

    // Data buffer chunk.
    AccumBuffer* get_accum_buf()
    {
        return &accum_buf_;
    }

    // Initializes accumulated data buffer based on chunk data.
    void InitAccumBufferFromUserData()
    {
        accum_buf_.Init(user_data_written_bytes_, (uint8_t*)this + user_data_offset_in_socket_data_, true);
    }

    // Resets accumulating buffer to its default socket data values.
    void ResetAccumBuffer()
    {
        // Checking if gateway chunk is used.
        if (gw_chunk_)
            accum_buf_.Init(gw_chunk_->buffer_len_bytes_, gw_chunk_->buf_, true);
        else
            accum_buf_.Init(SOCKET_DATA_BLOB_SIZE_BYTES, data_blob_, true);
    }

    // Index into target databases array.
    db_index_type get_target_db_index()
    {
        return target_db_index_;
    }

    // Index into target databases array.
    void set_target_db_index(db_index_type db_index)
    {
        target_db_index_ = db_index;
    }

    // Index into databases array.
    db_index_type get_db_index()
    {
        return db_index_;
    }

    // Index into databases array.
    void set_db_index(db_index_type db_index)
    {
        db_index_ = db_index;
    }

    // Exchanges sockets during proxying.
    void ExchangeToProxySocket()
    {
        session_index_type proxy_socket_info_index = GetProxySocketIndex();

        // Getting corresponding proxy socket id.
        random_salt_type proxy_unique_socket_id = g_gateway.GetUniqueSocketId(proxy_socket_info_index);

#ifdef GW_SOCKET_DIAG
        GW_COUT << "Exchanging sockets: " << socket_ << "<->" << proxy_socket_ << " and ids " <<
            unique_socket_id_ << "<->" << proxy_unique_socket_id << GW_ENDL;
#endif

        socket_info_index_ = proxy_socket_info_index;

        // Setting unique socket id.
        unique_socket_id_ = proxy_unique_socket_id;
    }

    // Initializes socket data that comes from database.
    void PreInitSocketDataFromDb(db_index_type db_index, core::chunk_index the_chunk_index)
    {
        db_index_ = db_index;
        chunk_index_ = the_chunk_index;
        type_of_network_protocol_ = g_gateway.GetTypeOfNetworkProtocol(socket_info_index_);
    }

    // Attaching to certain database.
    void AttachToDatabase(db_index_type db_index)
    {
        db_index_ = db_index;
    }

    // Initialization.
    void Init(
        session_index_type socket_info_index,
        db_index_type db_index,
        core::chunk_index chunk_index);

    // Resetting socket.
    void Reset();

    // Returns pointer to the beginning of user data.
    uint8_t* UserDataBuffer()
    {
        return (uint8_t*)this + user_data_offset_in_socket_data_;
    }

    // Resets user data offset.
    void ResetUserDataOffset()
    {
        user_data_offset_in_socket_data_ = static_cast<uint32_t> (data_blob_ - (uint8_t*)this);

        max_user_data_bytes_ = MixedCodeConstants::SOCKET_DATA_MAX_SIZE - user_data_offset_in_socket_data_;
    }

    // Start receiving on socket.
    uint32_t ReceiveSingleChunk(GatewayWorker *gw, uint32_t *num_bytes)
    {
        set_type_of_network_oper(RECEIVE_SOCKET_OPER);

        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return WSA_IO_PENDING;
#endif

        DWORD flags = 0;
        return WSARecv(GetSocket(), (WSABUF *)&accum_buf_, 1, (LPDWORD)num_bytes, &flags, &ovl_, NULL);
    }

    // Start receiving on socket using multiple chunks.
    uint32_t ReceiveMultipleChunks(GatewayWorker *gw, core::shared_interface* shared_int, uint32_t* num_bytes)
    {
        set_type_of_network_oper(RECEIVE_SOCKET_OPER);

        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return WSA_IO_PENDING;
#endif

        // NOTE: Need to subtract two chunks from being included in receive.
        DWORD flags = 0;
        return WSARecv(GetSocket(), (WSABUF*)&(shared_int->chunk(GetNextLinkedChunkIndex())), num_chunks_ - 1, (LPDWORD)num_bytes, &flags, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t SendSingleChunk(GatewayWorker* gw, uint32_t *numBytes)
    {
        set_type_of_network_oper(SEND_SOCKET_OPER);

        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return WSA_IO_PENDING;
#endif

        return WSASend(GetSocket(), (WSABUF *)&accum_buf_, 1, (LPDWORD)numBytes, 0, &ovl_, NULL);
    }

    // Start sending on socket.
    uint32_t SendMultipleChunks(GatewayWorker* gw, uint32_t *num_sent_bytes);

    // Start accepting on socket.
    uint32_t Accept(GatewayWorker* gw)
    {
        set_type_of_network_oper(ACCEPT_SOCKET_OPER);

        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToPreparationNetworkEmulationQueue(gw);
        return FALSE;
#endif

        // Running Windows API AcceptEx function.
        return AcceptExFunc(
            g_gateway.get_server_port(GetPortIndex())->get_listening_sock(),
            GetSocket(),
            accept_or_params_or_temp_data_,
            0,
            SOCKADDR_SIZE_EXT,
            SOCKADDR_SIZE_EXT,
            NULL,
            &ovl_);
    }

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    uint32_t SetAcceptSocketOptions()
    {
        SOCKET listening_sock = g_gateway.get_server_port(GetPortIndex())->get_listening_sock();

#ifndef GW_LOOPED_TEST_MODE
        if (setsockopt(GetSocket(), SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT, (char *)&listening_sock, sizeof(listening_sock)))
            return SCERRGWACCEPTEXFAILED;
#endif

        return 0;
    }

    // Start connecting on socket.
    uint32_t Connect(GatewayWorker* gw, sockaddr_in *serverAddr)
    {
        set_type_of_network_oper(CONNECT_SOCKET_OPER);

        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToPreparationNetworkEmulationQueue(gw);
        return FALSE;
#endif

        return ConnectExFunc(GetSocket(), (SOCKADDR *) serverAddr, sizeof(sockaddr_in), NULL, 0, NULL, &ovl_);
    }

    // Start disconnecting socket.
    uint32_t Disconnect(GatewayWorker *gw)
    {
        set_type_of_network_oper(DISCONNECT_SOCKET_OPER);

        memset(&ovl_, 0, OVERLAPPED_SIZE);

#ifdef GW_LOOPED_TEST_MODE
        PushToMeasuredNetworkEmulationQueue(gw);
        return FALSE;
#endif

        return DisconnectExFunc(GetSocket(), &ovl_, TF_REUSE_SOCKET, 0);
    }

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

    // Clone current socket data to simply send it.
    uint32_t CreateSocketDataFromBigBuffer(
        GatewayWorker*gw,
        session_index_type socket_info_index,
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
        // Resetting session data.
        session_.Reset();
    }
};

} // namespace network
} // namespace starcounter

#endif // SOCKET_DATA_HPP