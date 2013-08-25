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

// When decoded, is 16 bytes in length.
const char *kSecWebSocketKey = "Sec-WebSocket-Key";
const int32_t kSecWebSocketKeyLen = strlen(kSecWebSocketKey);

// Server response security field.
const char *kSecWebSocketAccept = "Sec-WebSocket-Accept";
const int32_t kSecWebSocketAcceptLen = strlen(kSecWebSocketAccept);

// Should be 13.
const char *kSecWebSocketVersion = "Sec-WebSocket-Version";
const int32_t kSecWebSocketVersionLen = strlen(kSecWebSocketVersion);

// Which protocols the client would like to speak.
const char *kSecWebSocketProtocol = "Sec-WebSocket-Protocol";
const int32_t kSecWebSocketProtocolLen = strlen(kSecWebSocketProtocol);

const char *kWsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
const int32_t kWsGuidLen = strlen(kWsGuid);

// The server MUST close the connection upon receiving a frame that is not masked.
// A server MUST NOT mask any frames that it sends to the client. 
// A client MUST close a connection if it detects a masked frame.

const char *kWsHsResponse =
    "HTTP/1.1 101 Switching Protocols\r\n"
    "Upgrade: websocket\r\n"
    "Connection: Upgrade\r\n"
    "Sec-WebSocket-Accept: @                           \r\n"
    "Server: SC\r\n"
    //"Sec-WebSocket-Protocol: chat\r\n";
    //"Sec-WebSocket-Protocol: $                                             \r\n"
    "\r\n";

const int32_t kWsHsResponseLen = strlen(kWsHsResponse);
const int32_t kWsAcceptOffset = abs(kWsHsResponse - strstr(kWsHsResponse, "@"));

const char *kWsBadProto =
    "HTTP/1.1 400 Bad Request\r\n"
    "Sec-WebSocket-Version: 13\r\n"
    "Content-Length: 0\r\n"
    "Pragma: no-cache\r\n"
    "\r\n";

const int32_t kWsBadProtoLen = strlen(kWsBadProto);

// Unmasks frame and pushes it to database.
uint32_t WsProto::UnmaskFrameAndPush(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id)
{
    uint32_t err_code;

    uint8_t* payload = sd->get_data_blob() + frame_info_.payload_data_blob_offset_;

    switch (frame_info_.opcode_)
    {
        case WS_OPCODE_TEXT:
        case WS_OPCODE_BINARY:
        {
            // Unmasking data.
            UnMaskAllChunks(gw, sd, frame_info_.payload_len_, frame_info_.mask_, payload);

            // Setting user data length and pointer.
            sd->set_user_data_written_bytes(frame_info_.payload_len_);

            // Setting request offsets.
            HttpRequest* req = sd->get_http_ws_proto()->get_http_request();
            req->request_len_bytes_ = frame_info_.payload_len_;
            req->request_offset_ = payload - (uint8_t*)sd;
            req->content_len_bytes_ = req->request_len_bytes_;
            req->content_offset_ = req->request_offset_;

            // Determining user data offset.
            uint32_t user_data_offset = payload - (uint8_t *) sd;
            if ((payload - sd->get_data_blob()) < WS_NEEDED_USER_DATA_OFFSET)
                user_data_offset += WS_NEEDED_USER_DATA_OFFSET;

            //std::cout << "Pushing payload: " << req->content_len_bytes_ << std::endl;

            sd->set_user_data_offset_in_socket_data(user_data_offset);

            // Push chunk to corresponding channel/scheduler.
            return gw->PushSocketDataToDb(sd, user_handler_id);
        }

        case WS_OPCODE_CLOSE:
        {
            // Peer wants to close the WebSocket.
            return SCERRGWWEBSOCKETOPCODECLOSE;
        }

        case WS_OPCODE_PING:
        {
            // Send the request Ping.
            return SCERRGWWEBSOCKETPINGOPCODE;
        }

        case WS_OPCODE_PONG:
        {
            uint64_t payload_len = frame_info_.payload_len_;

            // Send the response Pong.
            UnMaskAllChunks(gw, sd, payload_len, frame_info_.mask_, payload);
            payload = WritePayload(gw, sd, WS_OPCODE_PONG, false, WS_FRAME_SINGLE, payload, payload_len);

            // Prepare buffer to send outside.
            sd->get_accum_buf()->PrepareForSend(payload, payload_len);

            // Sending data.
            return gw->Send(sd);
        }

        default:
        {
            // Peer wants to close the WebSocket.
            return SCERRGWWEBSOCKETUNKNOWNOPCODE;
        }
    }
}

// Processes incoming WebSocket frames.
uint32_t WsProto::ProcessWsDataToDb(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE user_handler_id,
    bool* is_handled)
{
    // Handled successfully.
    *is_handled = true;

    uint32_t err_code = 0;

    // Obtaining saved user handler id.
    user_handler_id = sd->get_saved_user_handler_id();
    GW_ASSERT_DEBUG(bmx::BMX_INVALID_HANDLER_INFO != user_handler_id);

    SocketDataChunk* sd_push_to_db = NULL;
    uint8_t* orig_data_ptr = sd->get_data_blob();
    uint32_t num_accum_bytes = sd->get_accum_buf()->get_accum_len_bytes();
    uint32_t num_processed_bytes = 0;

    // Resetting completeness flag to be sure that its not used later.
    if (sd->get_num_chunks() > 1)
        GW_ASSERT(true == frame_info_.is_complete_);
    else
        frame_info_.is_complete_ = false;

    // Checking if we have already parsed the frame.
    if (frame_info_.is_complete_)
        goto UNMASK_FRAME;

    // Since WebSocket frames can be grouped into one network packet
    // we have to processes all of them in a loop.
    while (true)
    {
        // Processing current frame.
        uint8_t* cur_data_ptr = orig_data_ptr + num_processed_bytes;

        // Obtaining frame info.
        ParseFrameInfo(sd, cur_data_ptr, orig_data_ptr + num_accum_bytes);

        // Checking frame information is complete.
        if (!frame_info_.is_complete_)
        {
            // Checking if we need to move current data up.
            cur_data_ptr = sd->get_accum_buf()->MoveDataToTopIfTooLittleSpace(cur_data_ptr, WS_MAX_FRAME_INFO_SIZE);

            // Continue receiving.
            sd->get_accum_buf()->ContinueReceive();

            // Returning socket to receiving state.
            return gw->Receive(sd);
        }

        uint8_t* payload = sd->get_data_blob() + frame_info_.payload_data_blob_offset_;

        // Calculating number of bytes processed.
        int32_t header_len = payload - cur_data_ptr;
        num_processed_bytes += header_len;

        // Continue accumulating data.
        if (num_processed_bytes + frame_info_.payload_len_ > num_accum_bytes)
        {
            // Enabling accumulative state.
            sd->set_accumulating_flag(true);

            // Setting the desired number of bytes to accumulate.
            sd->get_accum_buf()->StartAccumulation(header_len + frame_info_.payload_len_, header_len + num_accum_bytes - num_processed_bytes);

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
            if (num_processed_bytes + frame_info_.payload_len_ == num_accum_bytes)
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
        num_processed_bytes += frame_info_.payload_len_;

#ifdef GW_WEBSOCKET_DIAG
        GW_COUT << "[" << gw->get_worker_id() << "]: " << "WS_OPCODE: " << frame_info_.opcode_ << GW_ENDL;
#endif

UNMASK_FRAME:

        // Data is complete, no more frames, creating parallel receive clone.
        if (NULL == sd_push_to_db)
        {
            err_code = sd->CloneToReceive(gw);
            if (err_code)
                return err_code;

            // Unmasking frame and pushing to database.
            return UnmaskFrameAndPush(gw, sd, user_handler_id);
        }
        else
        {
            // Unmasking frame and pushing to database.
            err_code = UnmaskFrameAndPush(gw, sd_push_to_db, user_handler_id);
            if (err_code)
                return err_code;
        }
    }

    return 0;
}

// Processes payload data from database.
uint32_t WsProto::ProcessWsDataFromDb(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    // Handled successfully.
    *is_handled = true;

    // Checking if we want to disconnect.
    if (sd->get_disconnect_flag())
    {
        // NOTE: Socket must be closed.
        sd->set_socket_representer_flag(true);

        gw->DisconnectAndReleaseChunk(sd);

        return 0;
    }

    // Checking if this socket data is for send only.
    if (sd->get_socket_just_send_flag())
        goto JUST_SEND_SOCKET_DATA;

    // Getting user data.
    uint8_t* payload = sd->UserDataBuffer();

    // Length of user data in bytes.
    uint64_t payload_len = sd->get_user_data_written_bytes();

    // Place where masked data should be written.
    payload = WritePayload(gw, sd, frame_info_.opcode_, false, WS_FRAME_SINGLE, payload, payload_len);

    // Prepare buffer to send outside.
    sd->get_accum_buf()->PrepareForSend(payload, payload_len);

    // Checking if multiple chunks involved.
    if (sd->get_num_chunks() > 1)
    {
        // Adjusting first WSABuf structure.
        WSABUF* wsa_buf = (WSABUF*) gw->GetSmcFromChunkIndex(sd->get_db_index(), sd->get_extra_chunk_index());
        wsa_buf->len = sd->GetNumRemainingDataBytesInChunk(payload);
        wsa_buf->buf = (char *) payload;
    }
    
JUST_SEND_SOCKET_DATA:

    // Sending data.
    return gw->Send(sd);
}

// Performs the WebSocket handshake.
uint32_t WsProto::DoHandshake(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    // Handled successfully.
    *is_handled = true;

    uint32_t err_code;

    // Pointing to the beginning of the data.
    uint8_t *resp_data_begin = sd->get_accum_buf()->ResponseDataStart();

    // Copying template in response buffer.
    memcpy(resp_data_begin, kWsHsResponse, kWsHsResponseLen);

    // Generating handshake response.
    char handshake_resp_temp[128];
    strncpy(handshake_resp_temp, client_key_, client_key_len_);
    strncpy(handshake_resp_temp + client_key_len_, kWsGuid, kWsGuidLen);

    // Generating SHA1 into temp buffer.
    char* sha1_begin = handshake_resp_temp + client_key_len_ + kWsGuidLen;
    SHA1((uint8_t *)handshake_resp_temp, client_key_len_ + kWsGuidLen, (uint8_t *)sha1_begin);

    // Converting SHA1 into Base64.
    char* base64_begin = sha1_begin + 20;
    base64_encodestate b64;
    base64_init_encodestate(&b64);
    uint8_t base64_len = base64_encode_block(sha1_begin, 20, base64_begin, &b64);
    base64_len += base64_encode_blockend(base64_begin + base64_len, &b64) - 1;

    // Sec-WebSocket-Accept.
    GW_ASSERT_DEBUG(28 == base64_len);
    memcpy(resp_data_begin + kWsAcceptOffset, base64_begin, base64_len);

    // Setting WebSocket handshake flag.
    sd->set_type_of_network_protocol(MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS);

    // Setting fixed handler id.
    sd->set_saved_user_handler_id(user_handler_id);

    // TODO: Check the strategy about creating session.
#if 0

    // Setting user data information so its applied to accumulating buffer on the way back.
    sd->set_user_data_offset_in_socket_data(resp_data_begin - (uint8_t*)sd);
    sd->set_user_data_written_bytes(kWsHsResponseLen);

    // Just sending on way back.
    sd->set_socket_just_send_flag(true);

    // Push chunk to obtain new session.
    err_code = gw->GetWorkerDb(sd->get_db_index())->PushSessionCreate(sd);
    if (err_code)
        return err_code;

#else

    // Prepare buffer to send outside.
    sd->get_accum_buf()->PrepareForSend(resp_data_begin, kWsHsResponseLen);

    // Sending data.
    err_code = gw->Send(sd);
    if (err_code)
        return err_code;

#endif

    // Printing the outgoing packet.
#ifdef GW_WEBSOCKET_DIAG
    GW_COUT << resp_data_begin << GW_ENDL;
#endif

    return 0;
}

// Masks or unmasks payload.
void WsProto::MaskUnMask(
    uint8_t* payload,
    int32_t payload_len,
    uint64_t mask_8bytes,
    int8_t& num_remaining_bytes)
{
    GW_ASSERT_DEBUG(num_remaining_bytes < 8);

    int32_t processed_bytes = 0, bytes_left = payload_len;

    // Checking if we have any remaining bytes.
    if (num_remaining_bytes)
    {
        int32_t num_begin_bytes = 8 - num_remaining_bytes;

        for (int32_t i = 0; i < num_remaining_bytes; i++)
            payload[i] = payload[i] ^ (((uint8_t*)&mask_8bytes)[num_begin_bytes + i]);

        processed_bytes += num_remaining_bytes;
    }

    // Masking until all bytes are processed.
    while (processed_bytes < payload_len)
    {
        int32_t tail_bytes_num = bytes_left - processed_bytes;
        if (tail_bytes_num < 8)
        {
            // Processing last bytes.
            for (int32_t i = 0; i < tail_bytes_num; i++)
                payload[processed_bytes + i] = payload[processed_bytes + i] ^ (((uint8_t*)&mask_8bytes)[i]);

            num_remaining_bytes = 8 - tail_bytes_num;

            return;
        }
        else
        {
            *((uint64_t*)(payload + processed_bytes)) ^= mask_8bytes;
            processed_bytes += 8;
        }
    }
}

// Masks or unmasks payload.
void WsProto::UnMaskAllChunks(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    const int32_t payload_len_bytes,
    const uint64_t mask,
    uint8_t* payload)
{
    // Getting link to the first chunk in chain.
    shared_memory_chunk* smc = sd->get_smc();
    core::chunk_index cur_chunk_index;
    int32_t chunk_bytes_left = sd->GetNumRemainingDataBytesInChunk(payload);
    if (payload_len_bytes < chunk_bytes_left)
        chunk_bytes_left = payload_len_bytes;

    int8_t num_remaining_bytes = 0;
    int32_t total_processed_len_bytes = 0;
    uint64_t mask_8bytes = mask | (mask << 32);

    // Processing all linked chunks.
    while (true)
    {
        MaskUnMask(payload, chunk_bytes_left, mask_8bytes, num_remaining_bytes);
        total_processed_len_bytes += chunk_bytes_left;

        int32_t num_payload_bytes_left = payload_len_bytes - total_processed_len_bytes;

        if (0 == num_payload_bytes_left)
            break;
        
        // Getting next linked chunk.
        cur_chunk_index = smc->get_link();

        // Obtaining chunk memory.
        smc = gw->GetSmcFromChunkIndex(sd->get_db_index(), cur_chunk_index);

        payload = (uint8_t*) smc;

        chunk_bytes_left = MixedCodeConstants::CHUNK_MAX_DATA_BYTES;

        if (num_payload_bytes_left < chunk_bytes_left)
            chunk_bytes_left = num_payload_bytes_left;
    }
}

#define swap64(y) (((uint64_t)ntohl(y)) << 32 | ntohl(y >> 32))

// Parses WebSockets frame info.
void WsProto::ParseFrameInfo(SocketDataChunkRef sd, uint8_t *data, uint8_t* limit)
{
    // Getting final fragment bit.
    frame_info_.is_final_ = (*data & 0x80);

    // Getting operation code.
    frame_info_.opcode_ = (*data & 0x0F);

    // Getting mask flag.
    data++;
    frame_info_.is_masked_ = (*data & 0x80);

    // Removing the mask flag.
    (*data) &= 0x7F;

    // Calculating the payload length.
    switch(*data)
    {
        // 16 bits.
        case 126:
        {
            frame_info_.payload_len_ = ntohs(*(uint16_t *)(data + 1));
            frame_info_.mask_ = *(uint32_t *)(data + 3);
            data += 7;
            break;
        }

        // 64 bits.
        case 127:
        {
            frame_info_.payload_len_ = swap64(*(uint64_t *)(data + 1));
            frame_info_.mask_ = *(uint32_t *)(data + 9);
            data += 13;
            break;
        }

        // 7 bits.
        default:
        {
            frame_info_.payload_len_ = *data;
            frame_info_.mask_ = *(uint32_t *)(data + 1);
            data += 5;
        }
    }

    // Increasing limit by one if payload length is 0.
    if (0 == frame_info_.payload_len_)
        limit++;

    // Checking if frame was successfully parsed.
    if (data < limit)
        frame_info_.is_complete_ = true;
    
    frame_info_.payload_data_blob_offset_ = data - sd->get_data_blob();

    return;
}

// Write payload data.
uint8_t *WsProto::WritePayload(
    GatewayWorker* gw,
    SocketDataChunkRef sd,
    uint8_t opcode,
    bool masking,
    WS_FRAGMENT_FLAG frame_type,
    uint8_t* payload,
    uint64_t& payload_len)
{
    // Pointing to destination memory.
    uint8_t *p = payload - 1;

    // Checking masking.
    if (masking)
        p -= 4;

    // Checking payload length.
    if (payload_len < 126)
        p -= 1;
    else if (payload_len <= 0xFFFF)
        p -= 3;
    else
        p -= 9;

    // Saving frame pointer.
    uint8_t *frame_start = p;

    // Applying opcode depending on the frame type.
    switch (frame_type)
    {
        case WS_FRAME_SINGLE:
        {
            *p = 0x80 | opcode;
            break;
        }

        case WS_FRAME_FIRST:
        {
            *p = opcode;
            break;
        }

        case WS_FRAME_CONT:
        {
            *p = 0;
            break;
        }

        case WS_FRAME_LAST:
        {
            *p = 0x80;
            break;
        }
    }

    // Shifting to payload length byte.
    p++;

    // Writing payload length.
    if (payload_len < 126)
    {
        *p = payload_len;
        p++;
    }
    else if (payload_len <= 0xFFFF)
    {
        *p = 126;
        (*(uint16_t *)(p + 1)) = htons(payload_len);
        p += 3;
    }
    else
    {
        *p = 127;
        (*(uint64_t *)(p + 1)) = swap64(payload_len);
        p += 9;
    }

    // Checking if we do masking.
    if (masking)
    {
        // Create some random mask for use.
        uint32_t mask = gw->get_random()->uint64();

        // Writing mask.
        *(uint32_t *)p = mask;
        *(frame_start + 1) |= 0x80;

        // Shifting to the payload itself.
        p += 4;

        // Do masking on all data.
        UnMaskAllChunks(gw, sd, payload_len, mask, p);
    }

    // Returning total data length.
    payload_len = (payload - frame_start) + payload_len;

    // Returning frame start address.
    return frame_start;
}

} // namespace network
} // namespace starcounter
