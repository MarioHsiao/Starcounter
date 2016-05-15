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

    // Reference to socket info structure.
    ScSocketInfoStruct* socket_info_;

    // Starcounter session information.
    ScSessionStruct session_;

    /////////////////////////
    // Networking data.
    /////////////////////////

    // Available bytes number for networking operation (send/receive).
    uint32_t num_available_network_bytes_;

    // Current network buffer pointer in chunk.
    uint8_t* cur_network_buf_ptr_;

    /////////////////////////
    // 4 bytes aligned data.
    /////////////////////////

    // Socket identifier.
    socket_index_type socket_info_index_;

    // Socket data flags.
    uint32_t flags_;

    // Aggregation unique index.
    uint32_t unique_aggr_index_;

    // Offset in bytes from the beginning of the socket data to user data.
    uint32_t user_data_offset_in_socket_data_;

    // Length of user data in bytes.
    uint32_t user_data_length_bytes_;

    // Total number of bytes accumulated.
    uint32_t accumulated_len_bytes_;

    /////////////////////////
    // 1 bytes aligned data.
    /////////////////////////

    // Current type of network operation.
    uint8_t type_of_network_oper_;

    // Type of network protocol.
    uint8_t type_of_network_protocol_;

    // Gateway chunk store index.
    chunk_store_type chunk_store_index_;

    // WebSocket related data.
    WsProto ws_proto_;

    /////////////////////////
    // Data structures.
    /////////////////////////

    // HTTP protocol instance.
    HttpProto http_proto_;

    // Accept or parameters or temporary data.
    uint8_t accept_or_params_or_temp_data_[MixedCodeConstants::PARAMS_INFO_MAX_SIZE_BYTES];

    // Start of the data blob.
    uint8_t data_blob_start__;

public:

	uint8_t* get_cur_network_buf_ptr() {
		return cur_network_buf_ptr_;
	}
	
	ScSocketInfoStruct* get_socket_info() {
		return socket_info_;
	}

    void SetWebSocketUpgradeResponsePartLength(uint32_t len_bytes) {
        num_available_network_bytes_ = len_bytes;
    }

    WSABUF* GetWSABUF() {
        return (WSABUF *)&num_available_network_bytes_;
    }

    // Checking there are enough space for receive.
    void CheckSpaceLeftForReceive() {
        GW_ASSERT(num_available_network_bytes_ > 0);
        GW_ASSERT(cur_network_buf_ptr_ + num_available_network_bytes_ <= get_data_blob_start() + get_data_blob_size());
    }

    uint8_t* get_data_blob_start() {
        return (uint8_t*)this + SOCKET_DATA_OFFSET_BLOB;
    }

    // Moves data in accumulative buffer to top.
    uint8_t* MoveDataToTopAndContinueReceive(uint8_t* cur_data_ptr, int32_t num_copy_bytes)
    {
        uint8_t* data_orig_ptr = get_data_blob_start();

        // Checking if we have anything to move.
        if ((cur_data_ptr > data_orig_ptr) &&
            (cur_data_ptr + num_copy_bytes <= data_orig_ptr + get_data_blob_size()))
        {
            memmove(data_orig_ptr, cur_data_ptr, num_copy_bytes);

            cur_network_buf_ptr_ = data_orig_ptr + num_copy_bytes;
            num_available_network_bytes_ = get_data_blob_size() - num_copy_bytes;

            accumulated_len_bytes_ = num_copy_bytes;
        }

        GW_ASSERT(accumulated_len_bytes_ <= get_data_blob_size());

        return data_orig_ptr;
    }

	// Initializes accumulative buffer.
	void SetAccumulation(uint32_t buf_total_len_bytes)
	{
		cur_network_buf_ptr_ = get_data_blob_start();
		accumulated_len_bytes_ = buf_total_len_bytes;

		GW_ASSERT(accumulated_len_bytes_ <= get_data_blob_size());
	}

    uint32_t get_num_available_network_bytes()
    {
        return num_available_network_bytes_;
    }

    void set_num_available_network_bytes(uint32_t value)
    {
        num_available_network_bytes_ = value;
    }

    void RevertBeforeSend()
    {
        num_available_network_bytes_ = get_data_blob_size() - num_available_network_bytes_;
    }

    uint32_t get_data_blob_size()
    {
        return GatewayChunkDataSizes[chunk_store_index_];
    }

    void WriteBytesToSend(void* data, uint32_t data_len)
    {
        memcpy(get_data_blob_start() + get_data_blob_size() - num_available_network_bytes_, data, data_len);
        num_available_network_bytes_ -= data_len;
    }

    // Adds accumulated bytes.
    void AddAccumulatedBytes(int32_t num_bytes)
    {
        accumulated_len_bytes_ += num_bytes;
        cur_network_buf_ptr_ += num_bytes;
        num_available_network_bytes_ -= num_bytes;

        GW_ASSERT(accumulated_len_bytes_ <= get_data_blob_size());
    }

    // Prepare buffer to proxy outside.
    void PrepareToSendOnProxy()
    {
        cur_network_buf_ptr_ = get_data_blob_start();
        num_available_network_bytes_ = accumulated_len_bytes_;
        accumulated_len_bytes_ = 0;
    }

    // Returns the size in bytes of accumulated data.
    uint32_t get_accumulated_len_bytes()
    {
        return accumulated_len_bytes_;
    }

    // Checks if accumulating buffer is filled.
    bool IsNetworkBufferFull()
    {
        return 0 == num_available_network_bytes_;
    }

    // Converting UDP port byte order.
    void UdpChangePortByteOrder() {

        sockaddr_in* addr = (sockaddr_in*) accept_or_params_or_temp_data_;
        addr->sin_port = ntohs(addr->sin_port);
    }

    // Converting UDP IPv4 byte order.
    void UdpChangeIPv4ByteOrder() {

        sockaddr_in* addr = (sockaddr_in*) accept_or_params_or_temp_data_;
        addr->sin_family = AF_INET;
    }

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
        cur_network_buf_ptr_ = NULL;
    }

    chunk_store_type get_chunk_store_index()
    {
        return chunk_store_index_;
    }

    void set_chunk_store_index(chunk_store_type store_type)
    {
        chunk_store_index_ = store_type;
    }

    void set_socket_info_reference(GatewayWorker* gw);

    worker_id_type get_bound_worker_id()
    {
        return session_.gw_worker_id_;
    }

    void set_bound_worker_id(worker_id_type worker_id)
    {
        session_.gw_worker_id_ = worker_id;
    }

    void PlainCopySocketDataInfoHeaders(SocketDataChunk* sd_from)
    {
        // NOTE:
        // Saving new chunk store index, otherwise it would be overwritten.
        chunk_store_type saved_new_store_index = chunk_store_index_;
        memcpy(this, sd_from, sizeof(SocketDataChunk));
        chunk_store_index_ = saved_new_store_index;
    }

    void CopyFromAnotherSocketData(SocketDataChunk* sd_from)
    {
        // First copying socket data headers.
        PlainCopySocketDataInfoHeaders(sd_from);

        // Resetting the accumulative buffer because it was overwritten.
        ResetAccumBuffer();

        GW_ASSERT(static_cast<int32_t>(sd_from->get_accumulated_len_bytes()) <= GatewayChunkDataSizes[chunk_store_index_]);

        memcpy(get_data_blob_start(), sd_from->get_data_blob_start(), sd_from->get_accumulated_len_bytes());

        // Adjusting the accumulative buffer.
        AddAccumulatedBytes(sd_from->get_accumulated_len_bytes());
    }

    void CopyFromOneChunkIPCSocketData(SocketDataChunk* ipc_sd, int32_t num_bytes_to_copy)
    {
        // First copying socket data headers.
        PlainCopySocketDataInfoHeaders(ipc_sd);

        // Resetting the WSABUF data pointer.
        cur_network_buf_ptr_ = get_data_blob_start();

        memcpy(get_data_blob_start(), ipc_sd->get_data_blob_start(), num_bytes_to_copy);
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
        return static_cast<uint32_t>(get_data_blob_start() - (uint8_t*)this);
    }

    // Prepare buffer to send outside.
    void PrepareForSend(uint8_t *data, uint32_t num_bytes)
    {
        num_available_network_bytes_ = num_bytes;
        cur_network_buf_ptr_ = data;
        accumulated_len_bytes_ = 0;

        set_user_data_offset_in_socket_data(static_cast<uint32_t>(data - (uint8_t*)this));
    }

    // Resets safe flags.
    void ResetSafeFlags()
    {
        reset_accumulating_flag();
        reset_to_database_direction_flag();
        reset_complete_header_flag();
    }

    uint32_t GetTotalUserDataLengthFromDb() {
        return accumulated_len_bytes_;
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

        std::cout << "public const int SOCKET_DATA_OFFSET_SESSION = " << ((uint8_t*)&session_ - sd) << ";" << std::endl;
        std::cout << "public const int CHUNK_OFFSET_SESSION = " << ((uint8_t*)&session_ - smc) << ";" << std::endl;
        std::cout << "public const int CHUNK_OFFSET_SESSION_SCHEDULER_ID = "<< (&session_.scheduler_id_ - smc) << ";" << std::endl;
        std::cout << "public const int CHUNK_OFFSET_SESSION_LINEAR_INDEX = "<< ((uint8_t*)&session_.linear_index_ - smc) << ";" << std::endl;
        std::cout << "public const int CHUNK_OFFSET_SESSION_RANDOM_SALT = "<< ((uint8_t*)&session_.random_salt_ - smc) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_PARAMS_INFO = "<< (accept_or_params_or_temp_data_ - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_BLOB = "<< ((uint8_t*)&data_blob_start__ - sd) << ";" << std::endl;
        std::cout << "public const int CHUNK_OFFSET_NUM_IPC_CHUNKS = "<< ((uint8_t*)&ovl_ - smc) << ";" << std::endl;

        std::cout << "public const int CHUNK_OFFSET_SOCKET_FLAGS = "<< ((uint8_t*)&flags_ - smc) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE = "<< ((uint8_t*)&type_of_network_protocol_ - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_CLIENT_IP = "<< ((uint8_t*)&client_ip_info_ - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_HTTP_REQUEST = "<< ((uint8_t*)get_http_proto()->get_http_request() - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_NUM_CLONE_BYTES = "<< ((uint8_t*)&accept_or_params_or_temp_data_ - sd) << ";" << std::endl;
        std::cout << "public const int CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA = "<< ((uint8_t*)&user_data_offset_in_socket_data_ - smc) << ";" << std::endl;
        std::cout << "public const int CHUNK_OFFSET_USER_DATA_NUM_BYTES = "<< ((uint8_t*)&user_data_length_bytes_ - smc) << ";" << std::endl;

        std::cout << "public const int SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID = "<< ((uint8_t*)&unique_socket_id_ - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER = "<< ((uint8_t*)&socket_info_index_ - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_WS_OPCODE = "<< (get_ws_proto()->get_opcode_addr() - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_BOUND_WORKER_ID = "<< ((uint8_t*)&(session_.gw_worker_id_) - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_WS_CHANNEL_ID = "<< ((uint8_t*)&accept_or_params_or_temp_data_ - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_UDP_DESTINATION_SOCKADDR = "<< ((uint8_t*)&accept_or_params_or_temp_data_ - sd) << ";" << std::endl;

        sockaddr_in* sock_addr = (sockaddr_in*)accept_or_params_or_temp_data_;

        std::cout << "public const int SOCKET_DATA_OFFSET_UDP_DESTINATION_IP = "<< ((uint8_t*)&(sock_addr->sin_addr.s_addr) - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_UDP_DESTINATION_PORT = "<< ((uint8_t*)&(sock_addr->sin_port) - sd) << ";" << std::endl;
        std::cout << "public const int SOCKET_DATA_OFFSET_UDP_SOURCE_PORT = "<< ((uint8_t*)sock_addr + sizeof(sockaddr_in) - sd) << ";" << std::endl;

        std::cout << "public const int CHUNK_OFFSET_UPGRADE_PART_BYTES = "<< ((uint8_t*)&num_available_network_bytes_ - smc) << ";" << std::endl;
        std::cout << "public const int CHUNK_OFFSET_USER_DATA_TOTAL_LENGTH_FROM_DB = "<< ((uint8_t*)&accumulated_len_bytes_ - smc) << ";" << std::endl;

        GW_ASSERT(1 == sizeof(WsProto));
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

        GW_ASSERT(((uint8_t*)&data_blob_start__ - sd) == SOCKET_DATA_OFFSET_BLOB);

        GW_ASSERT(((uint8_t*)&ovl_ - smc) == MixedCodeConstants::CHUNK_OFFSET_NUM_IPC_CHUNKS);

        GW_ASSERT(((uint8_t*)&flags_ - smc) == MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS);

        GW_ASSERT(((uint8_t*)&type_of_network_protocol_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE);

        GW_ASSERT(((uint8_t*)&client_ip_info_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_CLIENT_IP);

        GW_ASSERT(((uint8_t*)get_http_proto()->get_http_request() - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_HTTP_REQUEST);

        GW_ASSERT(((uint8_t*)&accept_or_params_or_temp_data_ - sd) == MixedCodeConstants::SOCKET_DATA_NUM_CLONE_BYTES);

        GW_ASSERT(((uint8_t*)&user_data_offset_in_socket_data_ - smc) == MixedCodeConstants::CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

        GW_ASSERT(((uint8_t*)&user_data_length_bytes_ - smc) == MixedCodeConstants::CHUNK_OFFSET_USER_DATA_NUM_BYTES);

        GW_ASSERT(((uint8_t*)&unique_socket_id_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID);

        GW_ASSERT(((uint8_t*)&socket_info_index_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER);

        GW_ASSERT((get_ws_proto()->get_opcode_addr() - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_WS_OPCODE);

        GW_ASSERT(((uint8_t*)&(session_.gw_worker_id_) - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_BOUND_WORKER_ID);

        GW_ASSERT(((uint8_t*)&accept_or_params_or_temp_data_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_WS_CHANNEL_ID);

        GW_ASSERT(((uint8_t*)&accept_or_params_or_temp_data_ - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_UDP_DESTINATION_SOCKADDR);

        GW_ASSERT(((uint8_t*)&(sock_addr->sin_addr.s_addr) - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_UDP_DESTINATION_IP);

        GW_ASSERT(((uint8_t*)&(sock_addr->sin_port) - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_UDP_DESTINATION_PORT);

        GW_ASSERT(((uint8_t*)sock_addr + sizeof(sockaddr_in) - sd) == MixedCodeConstants::SOCKET_DATA_OFFSET_UDP_SOURCE_PORT);

        GW_ASSERT(((uint8_t*)&num_available_network_bytes_ - smc) == MixedCodeConstants::CHUNK_OFFSET_UPGRADE_PART_BYTES_TO_DB);

        GW_ASSERT(((uint8_t*)&accumulated_len_bytes_ - smc) == MixedCodeConstants::CHUNK_OFFSET_USER_DATA_TOTAL_LENGTH_FROM_DB);

        GW_ASSERT(sizeof(sockaddr_in) == 16);

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

    // Is it a UDP socket.
    bool IsUdp() {
        return (MixedCodeConstants::NetworkProtocolType::PROTOCOL_UDP == type_of_network_protocol_);
    }

    // Checking if its a WebSocket protocol.
    bool is_web_socket()
    {
        return MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS == GetTypeOfNetworkProtocol();
    }

    // Getting to database direction flag.
    bool get_to_database_direction_flag()
    {
		return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION) != 0;
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

	bool get_internal_request_flag()
	{
		return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_INTERNAL_REQUEST) != 0;
	}

	void set_internal_request_flag()
	{
		flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_INTERNAL_REQUEST;
	}

	void reset_internal_request_flag()
	{
		flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_INTERNAL_REQUEST;
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

    bool get_gateway_no_ipc_test_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_NO_IPC_TEST) != 0;
    }

    void set_gateway_no_ipc_test_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_NO_IPC_TEST;
    }

    void reset_gateway_no_ipc_test_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_NO_IPC_TEST;
    }

    bool get_gateway_no_ipc_no_chunks_test_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_NO_IPC_NO_CHUNKS_TEST) != 0;
    }

    void set_gateway_no_ipc_no_chunks_test_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_NO_IPC_NO_CHUNKS_TEST;
    }

    void reset_gateway_no_ipc_no_chunks_test_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_NO_IPC_NO_CHUNKS_TEST;
    }

	bool get_streaming_response_body_flag()
	{
		return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_STREAMING_RESPONSE_BODY) != 0;
	}

	void set_streaming_response_body_flag()
	{
		flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_STREAMING_RESPONSE_BODY;
		socket_info_->set_streaming_response_body_flag();
	}

	bool GetStreamingResponseBodyFlag()
	{
		return socket_info_->get_streaming_response_body_flag();
	}

	void ResetStreamingResponseBodyFlag()
	{
		socket_info_->reset_streaming_response_body_flag();
	}

    bool get_gateway_and_ipc_test_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_AND_IPC_TEST) != 0;
    }

    void set_gateway_and_ipc_test_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_AND_IPC_TEST;
    }

    void reset_gateway_and_ipc_test_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_GATEWAY_AND_IPC_TEST;
    }

    bool get_chunk_looping_host_flag()
    {
        return (flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_HOST_LOOPING_CHUNKS) != 0;
    }

    void set_chunk_looping_host_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_HOST_LOOPING_CHUNKS;
    }

    void reset_chunk_looping_host_flag()
    {
        flags_ &= ~MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_HOST_LOOPING_CHUNKS;
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
        return ((flags_ & MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND) != 0) ||
            (socket_info_->get_disconnect_after_send_flag());
    }

    // Setting disconnect after send flag.
    void set_disconnect_after_send_flag()
    {
        flags_ |= MixedCodeConstants::SOCKET_DATA_FLAGS::SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND;
        GW_ASSERT(NULL != socket_info_);
        socket_info_->set_disconnect_after_send_flag();
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
    void set_socket_info_index(GatewayWorker* gw, socket_index_type socket_info_index)
    {
        socket_info_index_ = socket_info_index;
        set_socket_info_reference(gw);
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
    void CopyIPCChunksToGatewayChunk(
        WorkerDbInterface* worker_db,
        SocketDataChunk* sd,
        int32_t user_data_len_bytes);

    // Copies gateway chunk to IPC chunks.
    uint32_t CopyGatewayChunkToIPCChunks(
        WorkerDbInterface* worker_db,
        SocketDataChunk** new_ipc_sd,
        core::chunk_index* db_chunk_index);

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
    void set_user_data_offset_in_socket_data(uint32_t user_data_offset_in_socket_data)
    {
        user_data_offset_in_socket_data_ = user_data_offset_in_socket_data;
    }

    // Offset in bytes from the beginning of the chunk to place
    // where user data should be written.
    uint32_t get_user_data_offset_in_socket_data()
    {
        return user_data_offset_in_socket_data_;
    }

    // Size in bytes of written user data.
    uint32_t get_user_data_length_bytes() {

        GW_ASSERT(user_data_length_bytes_ <= get_data_blob_size());

        return user_data_length_bytes_;
    }

    // Size in bytes of written user data.
    uint32_t get_user_data_length_bytes_icp_chunk() {

        return user_data_length_bytes_;
    }

    // Resets accumulating buffer to its default socket data values.
    void ResetAccumBuffer()
    {
        num_available_network_bytes_ = get_data_blob_size();
        cur_network_buf_ptr_ = get_data_blob_start();
        accumulated_len_bytes_ = 0;
    }

    // Exchanges sockets during proxying.
    void ExchangeToProxySocket(GatewayWorker* gw);

    // Initializes socket data that comes from database.
    void PreInitSocketDataFromDb(GatewayWorker* gw, const scheduler_id_type sched_id);

    // Initializes socket data that comes from database.
    uint32_t PreInitUdpSocket(GatewayWorker* gw);

    // Initialization.
    void Init(
        GatewayWorker* gw,
        socket_index_type socket_info_index);

    // Resetting socket.
    void ResetWhenDisconnectIsDone(GatewayWorker *gw);

    // Returns pointer to the beginning of user data.
    uint8_t* GetUserData()
    {
        GW_ASSERT(user_data_offset_in_socket_data_ > 0);
        GW_ASSERT(user_data_offset_in_socket_data_ < (SOCKET_DATA_OFFSET_BLOB + get_data_blob_size()));

        return (uint8_t*)this + user_data_offset_in_socket_data_;
    }

    // Resets user data.
    void SetUserData(uint8_t* data_ptr, uint32_t data_len)
    {
        user_data_offset_in_socket_data_ = static_cast<uint32_t> (data_ptr - (uint8_t*)this);
        user_data_length_bytes_ = data_len;
    }

    // Start receiving on socket.
    uint32_t ReceiveTcp(GatewayWorker *gw, uint32_t *num_bytes);

    // Start sending on TCP socket.
    uint32_t SendTcp(GatewayWorker* gw, uint32_t *numBytes);

    // Start sending on UDP socket.
    uint32_t SendUdp(GatewayWorker* gw, uint32_t *numBytes);

    // Start receiving on socket.
    uint32_t ReceiveUdp(GatewayWorker *gw, uint32_t *num_bytes);

    // Start accepting on socket.
    uint32_t Accept(GatewayWorker* gw);

    // Setting SO_UPDATE_ACCEPT_CONTEXT.
    uint32_t SetAcceptSocketOptions(GatewayWorker* gw);

    // Start connecting on socket.
    uint32_t Connect(GatewayWorker* gw, sockaddr_in *serverAddr);

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
    uint32_t CloneToPush(GatewayWorker*gw, int32_t data_size, SocketDataChunk** new_sd);

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

    // Extract socket data from a bigger WebSocket frame.
    uint32_t CreateWebSocketDataFromBigBuffer(
        GatewayWorker*gw,
        uint8_t* payload,
        int32_t payload_len,
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

    // Sets global session data from local.
    void SetGlobalSessionFromLocal()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        // Fetching the session by index.
        socket_info_->session_ = session_;
    }

    // Resets the global session.
    void ResetGlobalSession()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        socket_info_->session_.Reset();
    }

    // Gets session data by index.
    ScSessionStruct GetGlobalSessionCopy()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        // Checking validity of linear session index other wise return a wrong copy.
        if (INVALID_SOCKET_INDEX == socket_info_index_)
            return ScSessionStruct();

        // Fetching the session by index.
        return socket_info_->session_;
    }

    // Sets connection type on given socket.
    void SetTypeOfNetworkProtocol(MixedCodeConstants::NetworkProtocolType proto_type)
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        set_type_of_network_protocol(proto_type);

        socket_info_->type_of_network_protocol_ = proto_type;
    }

    // Updates current global timer value on given socket.
    void UpdateSocketTimeStamp()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        socket_info_->socket_timestamp_ = g_gateway.get_global_timer_unsafe();
    }

    // Disconnects and invalidates socket.
    void DisconnectSocket() {

        // Checking if its a UDP socket that shouldn't be disconnected.
        if (IsUdp())
            return;

        // Disconnecting socket handle.
        socket_info_->DisconnectSocket();
    }

    // Invalidating socket number.
    bool IsInvalidSocket() {

        GW_ASSERT_DEBUG(NULL != socket_info_);

        return (INVALID_SOCKET == socket_info_->socket_);
    }

    void DecrementAccumulatedBytesLeft(uint32_t decr_bytes)
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        socket_info_->accum_data_bytes_left_ -= decr_bytes;
    }

    void SetAccumulatedBytesLeft(uint32_t num_bytes)
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        socket_info_->accum_data_bytes_left_ = num_bytes;
    }

    socket_index_type GetAccumulatedBytesLeft()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->accum_data_bytes_left_;
    }

    void SetProxySocketIndex(socket_index_type proxy_socket_index)
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        socket_info_->proxy_socket_info_index_ = proxy_socket_index;
    }

    socket_index_type GetProxySocketIndex()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->proxy_socket_info_index_;
    }

    // Getting aggregated socket flag.
    bool GetSocketAggregatedFlag()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->get_socket_aggregated_flag();
    }

    // Getting aggregation socket index.
    socket_index_type GetAggregationSocketIndex()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->aggr_socket_info_index_;
    }

    random_salt_type GetAggregationSocketUniqueId()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->aggr_unique_socket_id_;
    }

    bool GetWsCloseAlreadySentFlag()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->get_ws_close_already_sent_flag();
    }

    // Indicating that we already have sent the WebSocket Close frame.
    void SetWsCloseAlreadySentFlag()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        socket_info_->set_ws_close_already_sent_flag();
    }

    void SetWebSocketGroupId(ws_group_id_type ws_group_id)
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        socket_info_->ws_group_id_ = ws_group_id;
    }

    void FetchWebSocketGroupIdFromSocket()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        *(ws_group_id_type*)accept_or_params_or_temp_data_ = socket_info_->ws_group_id_;
    }

    ws_group_id_type GetWebSocketGroupId()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->ws_group_id_;
    }

    // Checks for proxy connect socket flag.
    bool IsProxyConnectSocket()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->get_socket_proxy_connect_flag();
    }

    // Checks for proxy socket.
    bool HasProxySocket()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return (INVALID_SOCKET_INDEX != socket_info_->proxy_socket_info_index_);
    }

    // Getting socket index.
    SOCKET GetSocket()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);
        GW_ASSERT_DEBUG(socket_info_->socket_ != INVALID_SOCKET);

        return socket_info_->socket_;
    }

    // Set scheduler id.
    void SetSchedulerId(scheduler_id_type sched_id)
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        set_scheduler_id(sched_id);

        socket_info_->session_.scheduler_id_ = sched_id;
    }

    // Getting scheduler id.
    scheduler_id_type GetSchedulerId()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->session_.scheduler_id_;
    }

    // Getting worker id.
    worker_id_type GetBoundWorkerId()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->session_.gw_worker_id_;
    }

    // Getting socket id.
    port_index_type GetPortIndex()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        port_index_type port_index = socket_info_->port_index_;

        // Checking if port is outside the range.
        if (port_index >= g_gateway.get_num_server_ports_slots())
            return INVALID_PORT_INDEX;

        GW_ASSERT(port_index >= 0);

        return port_index;
    }

    // Returns port number.
    uint16_t GetPortNumber()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        port_index_type port_index = GetPortIndex();

        ServerPort* sp = g_gateway.get_server_port(port_index);

        if (!sp->IsEmpty()) {
            return sp->get_port_number();
        }

        return INVALID_PORT_NUMBER;
    }

    // Setting destination database index.
    void SetDestDbIndex(db_index_type db_index)
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        socket_info_->dest_db_index_ = db_index;
    }

    // Getting destination database index.
    db_index_type GetDestDbIndex()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return socket_info_->dest_db_index_;
    }

	// Getting host streaming flag.
	bool GetHostStreamingFlag()
	{
		GW_ASSERT_DEBUG(NULL != socket_info_);

		return socket_info_->get_streaming_response_body_flag();
	}

    // Get type of network protocol for this socket.
    MixedCodeConstants::NetworkProtocolType GetTypeOfNetworkProtocol()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        return (MixedCodeConstants::NetworkProtocolType) socket_info_->type_of_network_protocol_;
    }

    // Checking if unique socket number is correct.
    bool CompareUniqueSocketId()
    {
        GW_ASSERT_DEBUG(NULL != socket_info_);

        bool is_equal = (socket_info_->unique_socket_id_ == unique_socket_id_) &&
            (INVALID_SESSION_SALT != unique_socket_id_);

        return is_equal;
    }
};

} // namespace network
} // namespace starcounter

#endif // SOCKET_DATA_HPP