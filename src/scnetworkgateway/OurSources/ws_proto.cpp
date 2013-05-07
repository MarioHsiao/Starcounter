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

const int32_t kWsBadProtoLen = strlen(kWsBadProto) + 1;

uint32_t WsProto::ProcessWsDataToDb(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    // Handled successfully.
    *is_handled = true;

    uint32_t err_code = 0;

    // TODO: Make multi-chunks support.
    GW_ASSERT(1 == sd->get_num_chunks());

    // Obtaining saved user handler id.
    user_handler_id = sd->get_saved_user_handler_id();
    GW_ASSERT_DEBUG(bmx::BMX_INVALID_HANDLER_INFO != user_handler_id);

    // Data is complete, creating parallel receive clone.
    if (sd->HasActiveSession())
    {
        err_code = sd->CloneToReceive(gw);
        if (err_code)
            return err_code;
    }

    SocketDataChunk* new_sd = NULL;
    uint8_t* orig_data_ptr = sd->get_accum_buf()->get_orig_buf_ptr();
    uint32_t total_bytes = sd->get_accum_buf()->get_accum_len_bytes();
    uint32_t num_processed_bytes = 0;

    // Since WebSocket frames can be grouped into one network packet
    // we have to processes all of them in a loop.
    while (true)
    {
        // Processing current frame.
        uint8_t* cur_data_ptr = orig_data_ptr + num_processed_bytes;
        err_code = GetFrameInfo(cur_data_ptr);
        if (err_code)
            return err_code;

        uint8_t* payload = frame_info_.payload_;

        // Checking if header is at the very end of chunk.
        if (payload > (orig_data_ptr + total_bytes))
            return SCERRGWWEBSOCKETSPAYLOADTOOBIG;

        // Calculating number of bytes processed.
        num_processed_bytes += (frame_info_.payload_ - cur_data_ptr) + frame_info_.payload_len_;

        // Checking if payload length fits in maximum data.
        if (num_processed_bytes > total_bytes)
            return SCERRGWWEBSOCKETSPAYLOADTOOBIG;

        // TODO: Support messages that exceed chunk size.
        GW_ASSERT(num_processed_bytes < SOCKET_DATA_BLOB_SIZE_BYTES);

        // Checking if it is the last frame.
        if (num_processed_bytes < total_bytes)
        {
            // Cloning chunk to send it to database.
            err_code = sd->CloneToSend(gw, &new_sd);
            if (err_code)
                return err_code;

            // Updating data pointer in a new chunk.
            orig_data_ptr = new_sd->get_accum_buf()->get_orig_buf_ptr();
        }
        else
        {
            new_sd = NULL;
        }

#ifdef GW_WEBSOCKET_DIAG
        GW_COUT << "[" << gw->get_worker_id() << "]: " << "WS_OPCODE: " << frame_info_.opcode_ << GW_ENDL;
#endif

        uint32_t err_code;

        // Determining operation type.
        switch(frame_info_.opcode_)
        {
            case WS_OPCODE_TEXT:
            case WS_OPCODE_BINARY:
            {
                // Unmasking data.
                MaskUnMask(frame_info_.payload_len_, frame_info_.mask_, (uint64_t *)payload);

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
                if ((payload - cur_data_ptr) < WS_NEEDED_USER_DATA_OFFSET)
                    user_data_offset += WS_NEEDED_USER_DATA_OFFSET;

                sd->set_user_data_offset_in_socket_data(user_data_offset);

                // Push chunk to corresponding channel/scheduler.
                gw->PushSocketDataToDb(sd, user_handler_id);

                break;
            }

            case WS_OPCODE_CLOSE:
            {
                // Peer wants to close the WebSocket.
                return SCERRGWWEBSOCKETOPCODECLOSE;
            }

            case WS_OPCODE_PING:
            {
                // Send the request Ping.
                break;
            }

            case WS_OPCODE_PONG:
            {
                uint64_t payloadLen = frame_info_.payload_len_;

                // Send the response Pong.
                MaskUnMask(payloadLen, frame_info_.mask_, (uint64_t *)payload);
                payload = WriteData(gw, WS_OPCODE_PONG, false, WS_FRAME_SINGLE, payload, &payloadLen);

                // Prepare buffer to send outside.
                sd->get_accum_buf()->PrepareForSend(payload, payloadLen);

                // Sending data.
                err_code = gw->Send(sd);
                GW_ERR_CHECK(err_code);

                break;
            }

            default:
            {
                // Peer wants to close the WebSocket.
                return SCERRGWWEBSOCKETUNKNOWNOPCODE;
            }
        }

        // Checking if we have a new chunk.
        if (new_sd)
            sd = new_sd;
        else
            break;
    }

    return 0;
}

uint32_t WsProto::ProcessWsDataFromDb(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled)
{
    // Handled successfully.
    *is_handled = true;

    // Checking if this socket data is for send only.
    if (sd->get_socket_just_send_flag())
        goto JUST_SEND_SOCKET_DATA;

    // Getting user data.
    uint8_t *payload = sd->UserDataBuffer();

    // Length of user data in bytes.
    uint64_t payload_len = sd->get_user_data_written_bytes();

    // Place where masked data should be written.
    payload = WriteData(gw, frame_info_.opcode_, false, WS_FRAME_SINGLE, payload, &payload_len);

    // Checking that we are not running out of buffer.
    GW_ASSERT(sd->get_accum_buf()->get_orig_buf_ptr() < payload);

    // Prepare buffer to send outside.
    sd->get_accum_buf()->PrepareForSend(payload, payload_len);

JUST_SEND_SOCKET_DATA:

    // Sending data.
    uint32_t err_code = gw->Send(sd);
    if (err_code)
        return err_code;

    return 0;
}

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

    // Prepare buffer to send outside.
    sd->get_accum_buf()->PrepareForSend(resp_data_begin, kWsHsResponseLen);

    // TODO: Check the strategy about creating session.
    /*
    // Just sending on way back.
    sd->set_socket_just_send_flag(true);

    // Push chunk to obtain new session.
    err_code = gw->GetWorkerDb(sd->get_db_index())->PushSessionCreate(sd);
    if (err_code)
        return err_code;
    */

    // Sending data.
    err_code = gw->Send(sd);
    if (err_code)
        return err_code;

    // Printing the outgoing packet.
#ifdef GW_WEBSOCKET_DIAG
    GW_COUT << resp_data_begin << GW_ENDL;
#endif

    return 0;
}

void WsProto::MaskUnMask(int32_t payload_len, uint64_t mask, uint64_t *data)
{
    uint32_t len_8bytes = (payload_len >> 3) + 1;
    uint64_t mask_8bytes = mask | (mask << 32);
    for (int32_t i = 0; i < len_8bytes; i++)
        data[i] = data[i] ^ mask_8bytes;
}

#define swap64(y) (((uint64_t)ntohl(y)) << 32 | ntohl(y >> 32))

uint32_t WsProto::GetFrameInfo(uint8_t *data)
{
    // Getting final fragment bit.
    frame_info_.is_final_ = (*data & 0x80);

    // Getting operation code.
    frame_info_.opcode_ = (WS_OPCODE)(*data & 0x0F);

    // Getting mask flag.
    data++;
    frame_info_.is_masked_ = (*data & 0x80);

    // TODO: MUST BE masked from client.

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

    frame_info_.payload_ = data;

    return 0;
}

uint8_t *WsProto::WriteData(
    GatewayWorker *gw,
    WS_OPCODE opcode,
    bool masking,
    WS_FRAGMENT_FLAG frame_type,
    uint8_t *payload,
    uint64_t *ppayload_len)
{
    uint64_t &payload_len = *ppayload_len;

    // Pointing to destination memory.
    uint8_t *dest_data = payload - 1;

    // Checking masking.
    if (masking)
        dest_data -= 4;

    // Checking payload length.
    if (payload_len < 126)
        dest_data -= 1;
    else if (payload_len <= 0xFFFF)
        dest_data -= 3;
    else
        dest_data -= 9;

    // Saving original data pointer.
    uint8_t *dest_data_orig = dest_data;

    // Applying opcode depending on the frame type.
    switch (frame_type)
    {
        case WS_FRAME_SINGLE:
        {
            *dest_data = 0x80 | opcode;
            break;
        }

        case WS_FRAME_FIRST:
        {
            *dest_data = opcode;
            break;
        }

        case WS_FRAME_CONT:
        {
            *dest_data = 0;
            break;
        }

        case WS_FRAME_LAST:
        {
            *dest_data = 0x80;
            break;
        }
    }

    // Shifting to payload length byte.
    dest_data++;

    // Writing payload length.
    if (payload_len < 126)
    {
        *dest_data = payload_len;
        dest_data++;
    }
    else if (payload_len <= 0xFFFF)
    {
        *dest_data = 126;
        (*(uint16_t *)(dest_data + 1)) = htons(payload_len);
        dest_data += 3;
    }
    else
    {
        *dest_data = 127;
        (*(uint64_t *)(dest_data + 1)) = swap64(payload_len);
        dest_data += 9;
    }

    // Checking if we do masking.
    if (masking)
    {
        // Create some random mask for use.
        uint32_t mask = gw->get_random()->uint64();

        // Writing mask.
        *(uint32_t *)dest_data = mask;
        *(dest_data_orig + 1) |= 0x80;

        // Shifting to the payload itself.
        dest_data += 4;

        // Do masking on all data.
        MaskUnMask(payload_len, mask, (uint64_t *)dest_data);
    }

    // Returning total data length.
    payload_len = (payload - dest_data_orig) + payload_len;

    // Returning new data pointer.
    return dest_data_orig;
}

} // namespace network
} // namespace starcounter
