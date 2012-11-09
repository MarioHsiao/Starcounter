#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
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
    "HTTP/1.1 101 Web Socket Protocol Handshake\r\n"
    "Upgrade: Websocket\r\n"
    "Connection: Upgrade\r\n"
    "Sec-WebSocket-Accept: @                           \r\n"
    "Server: Starcounter\r\n";
    //"Sec-WebSocket-Protocol: $                                             \r\n"

const int32_t kWsHsResponseLen = strlen(kWsHsResponse);
const int32_t kWsAcceptOffset = abs(kWsHsResponse - strstr(kWsHsResponse, "@"));

// Embedding session cookie.
const char *kWsCookie = "Set-Cookie: SessionId=%                                       ; HttpOnly\r\n";
const int32_t kWsCookieLen = strlen(kWsCookie);
const int32_t kWsSessionIdOffset = abs(kWsCookie - strstr(kWsCookie, "%"));

const char *kWsBadProto =
    "HTTP/1.1 400 Bad Request\r\n"
    "Sec-WebSocket-Version: 13\r\n"
    "Content-Length: 0\r\n"
    "Pragma: no-cache\r\n"
    "\r\n";

const int32_t kWsBadProtoLen = strlen(kWsBadProto) + 1;

uint32_t WsProto::ProcessWsDataToDb(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE user_handler_id)
{
    uint8_t *payload = GetFrameInfo(&frame_info_, sd->get_accum_buf()->get_orig_buf_ptr());

#ifdef GW_WEBSOCKET_DIAG
    GW_COUT << "[" << gw->get_worker_id() << "]: " << "WS_OPCODE: " << frame_info_.opcode_ << std::endl;
#endif

    // Determining operation type.
    switch(frame_info_.opcode_)
    {
        case WS_OPCODE_TEXT:
        {
            // Data is complete, posting parallel receive.
            gw->Receive(sd->CloneReceive(gw));

            // Unmasking data.
            MaskUnMask(frame_info_.payload_len_, frame_info_.mask_, (uint64_t *)payload);

            // Setting user data length and pointer.
            sd->set_user_data_written_bytes(frame_info_.payload_len_);
            sd->set_user_data_offset(payload - ((uint8_t *)sd));

            // Push chunk to corresponding channel/scheduler.
            gw->PushSocketDataToDb(sd, user_handler_id);

            break;
        }

        case WS_OPCODE_BINARY:
        {
            // Data is complete, posting parallel receive.
            gw->Receive(sd->CloneReceive(gw));

            // Unmasking data.
            MaskUnMask(frame_info_.payload_len_, frame_info_.mask_, (uint64_t *)payload);

            // Setting user data length and pointer.
            sd->set_user_data_written_bytes(frame_info_.payload_len_);
            sd->set_user_data_offset(payload - ((uint8_t *)sd));

            // Push chunk to corresponding channel/scheduler.
            gw->PushSocketDataToDb(sd, user_handler_id);

            break;
        }

        case WS_OPCODE_CLOSE:
        {
            // Peer wants to close the WebSocket.
            gw->Disconnect(sd);
            return 0;
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
            gw->Send(sd);

            break;
        }

        default:
        {
            // Peer wants to close the WebSocket.
            gw->Disconnect(sd);
            return 0;
        }
    }

    return 0;
}

uint32_t WsProto::ProcessWsDataFromDb(GatewayWorker *gw, SocketDataChunk *sd, BMX_HANDLER_TYPE handler_id)
{
    // Getting user data.
    uint8_t *payload = sd->UserDataBuffer();

    // Length of user data in bytes.
    uint64_t payloadLen = sd->get_user_data_written_bytes();

    // Place where masked data should be written.
    payload = WriteData(gw, frame_info_.opcode_, false, WS_FRAME_SINGLE, payload, &payloadLen);

    // Prepare buffer to send outside.
    sd->get_accum_buf()->PrepareForSend(payload, payloadLen);

    // Sending data.
    gw->Send(sd);

    return 0;
}

uint32_t WsProto::DoHandshake(GatewayWorker *gw, SocketDataChunk *sd)
{
    // Pointing to the beginning of the data.
    uint8_t *respDataBegin = sd->get_accum_buf()->ResponseDataStart();
    uint32_t respBufferSize = 0;

    // Copying template in response buffer.
    memcpy(respDataBegin, kWsHsResponse, kWsHsResponseLen);

    // Generating handshake response.
    char hsResponse[128];
    strncpy(hsResponse, /*"FixwdnCvmOSgGEhuI38LjA=="*/client_key_, /*24*/client_key_len_);
    strncpy(hsResponse + client_key_len_, kWsGuid, kWsGuidLen);

    char sha1[20]; // = {0xb3, 0x7a, 0x4f, 0x2c, 0xc0, 0x62, 0x4f, 0x16, 0x90, 0xf6, 0x46, 0x06, 0xcf, 0x38, 0x59, 0x45, 0xb2, 0xbe, 0xc4, 0xea };
    SHA1((uint8_t *)hsResponse, client_key_len_ + kWsGuidLen, (uint8_t *)sha1);

    // Converting SHA1 into Base64.
    char sha1Base64[64];
    base64_encodestate b64;
    base64_init_encodestate(&b64);
    int32_t sha1Base64Len = base64_encode_block(sha1, 20, sha1Base64, &b64);
    int32_t sha1Base64EndLen = base64_encode_blockend(sha1Base64 + sha1Base64Len, &b64);

    // Sec-WebSocket-Accept.
    memcpy(respDataBegin + kWsAcceptOffset, sha1Base64, sha1Base64Len + sha1Base64EndLen - 1);
    respBufferSize += kWsHsResponseLen;

    // Set-Cookie.
    /*if (sd->GetAttachedSession() == NULL)
    {
        // Copying cookie header.
        memcpy(respDataBegin + respBufferSize, kWsCookie, kWsCookieLen);

        // Generating and attaching new session.
        sd->AttachToSession(g_gateway.GenerateNewSession(gw));

        // Converting session to string.
        char temp[32];
        int32_t sessionStringLen = sd->GetAttachedSession()->ConvertToString(temp);
        memcpy(respDataBegin + respBufferSize + kWsSessionIdOffset, temp, sessionStringLen);
        respBufferSize += kWsCookieLen;
    }*/

    // Empty line.
    memcpy(respDataBegin + respBufferSize, "\r\n", 2);
    respBufferSize += 2;

    // Prepare buffer to send outside.
    sd->get_accum_buf()->PrepareForSend(respDataBegin, respBufferSize);

    // Setting WebSocket handshake flag.
    sd->set_web_sockets_upgrade_flag(true);

    // Sending data.
    gw->Send(sd);

    // Printing the outgoing packet.
#ifdef GW_WEBSOCKET_DIAG
    GW_COUT << respDataBegin << std::endl;
#endif

    return 0;
}

void WsProto::MaskUnMask(int32_t payloadLen, uint64_t mask, uint64_t *data)
{
    uint32_t len8Bytes = (payloadLen >> 3) + 1;
    uint64_t mask8Bytes = mask | (mask << 32);
    for (int32_t i = 0; i < len8Bytes; i++)
        data[i] = data[i] ^ mask8Bytes;
}

#define swap64(y) (((uint64_t)ntohl(y)) << 32 | ntohl(y >> 32))

uint8_t *WsProto::GetFrameInfo(WsProtoFrameInfo *pFrameInfo, uint8_t *data)
{
    WsProtoFrameInfo &frameInfo = *pFrameInfo;

    // Getting final fragment bit.
    frameInfo.is_final_ = (*data & 0x80);

    // Getting operation code.
    frameInfo.opcode_ = (WS_OPCODE)(*data & 0x0F);

    // Getting mask flag.
    data++;
    frameInfo.is_masked_ = (*data & 0x80);

    // Removing the mask flag.
    (*data) &= 0x7F;

    // Calculating the payload length.
    switch(*data)
    {
        // 16 bits.
        case 126:
        {
            frameInfo.payload_len_ = ntohs(*(uint16_t *)(data + 1));
            frameInfo.mask_ = *(uint32_t *)(data + 3);
            data += 7;
            break;
        }

        // 64 bits.
        case 127:
        {
            frameInfo.payload_len_ = swap64(*(uint64_t *)(data + 1));
            frameInfo.mask_ = *(uint32_t *)(data + 9);
            data += 13;
            break;
        }

        // 7 bits.
        default:
        {
            frameInfo.payload_len_ = *data;
            frameInfo.mask_ = *(uint32_t *)(data + 1);
            data += 5;
        }
    }

    return data;
}

uint8_t *WsProto::WriteData(
    GatewayWorker *gw,
    WS_OPCODE opcode,
    bool masking,
    WS_FRAGMENT_FLAG frameType,
    uint8_t *payload,
    uint64_t *pPayloadLen)
{
    uint64_t &payloadLen = *pPayloadLen;

    // Pointing to destination memory.
    uint8_t *destData = payload - 1;

    // Checking masking.
    if (masking)
        destData -= 4;

    // Checking payload length.
    if (payloadLen < 126)
        destData -= 1;
    else if (payloadLen <= 0xFFFF)
        destData -= 3;
    else
        destData -= 9;

    // Saving original data pointer.
    uint8_t *destDataOrig = destData;

    // Applying opcode depending on the frame type.
    switch (frameType)
    {
        case WS_FRAME_SINGLE:
        {
            *destData = 0x80 | opcode;
            break;
        }

        case WS_FRAME_FIRST:
        {
            *destData = opcode;
            break;
        }

        case WS_FRAME_CONT:
        {
            *destData = 0;
            break;
        }

        case WS_FRAME_LAST:
        {
            *destData = 0x80;
            break;
        }
    }

    // Shifting to payload length byte.
    destData++;

    // Writing payload length.
    if (payloadLen < 126)
    {
        *destData = payloadLen;
        destData++;
    }
    else if (payloadLen <= 0xFFFF)
    {
        *destData = 126;
        (*(uint16_t *)(destData + 1)) = htons(payloadLen);
        destData += 3;
    }
    else
    {
        *destData = 127;
        (*(uint64_t *)(destData + 1)) = swap64(payloadLen);
        destData += 9;
    }

    // Checking if we do masking.
    if (masking)
    {
        // Create some random mask for use.
        uint32_t mask = gw->get_random()->uint64();

        // Writing mask.
        *(uint32_t *)destData = mask;
        *(destDataOrig + 1) |= 0x80;

        // Shifting to the payload itself.
        destData += 4;

        // Do masking on all data.
        MaskUnMask(payloadLen, mask, (uint64_t *)destData);
    }

    // Returning total data length.
    payloadLen = (payload - destDataOrig) + payloadLen;

    // Returning new data pointer.
    return destDataOrig;
}

} // namespace network
} // namespace starcounter
