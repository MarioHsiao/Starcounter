#pragma once
#ifndef WS_PROTO_HPP
#define WS_PROTO_HPP

namespace starcounter {
namespace network {

// Message types.
enum WS_OPCODE
{
    WS_OPCODE_CONTINUATION = 0,
    WS_OPCODE_TEXT = 1,
    WS_OPCODE_BINARY = 2,
    WS_OPCODE_RESERVED1 = 3,
    WS_OPCODE_RESERVED2 = 4,
    WS_OPCODE_RESERVED3 = 5,
    WS_OPCODE_RESERVED4 = 6,
    WS_OPCODE_RESERVED5 = 7,
    WS_OPCODE_CLOSE = 8,
    WS_OPCODE_PING = 9,
    WS_OPCODE_PONG = 10,
    WS_OPCODE_RESERVED6 = 11,
    WS_OPCODE_RESERVED7 = 12,
    WS_OPCODE_RESERVED8 = 13,
    WS_OPCODE_RESERVED9 = 14,
    WS_OPCODE_RESERVED10 = 15
};

// Frame types.
enum WS_FRAGMENT_FLAG
{
    WS_FRAME_SINGLE,
    WS_FRAME_FIRST,
    WS_FRAME_CONT,
    WS_FRAME_LAST
};

// Frame info.
class WsProtoFrameInfo
{
    // Declaring a friend class to access private fields.
    friend class WsProto;
    friend class SocketDataChunk;

    // Payload.
    uint8_t* payload_;

    // Payload length in bytes.
    int64_t payload_len_;

    // Masking value.
    uint64_t mask_;

    // Is final frame.
    bool is_final_;

    // Opcode type.
    uint8_t opcode_;

    // Is frame masked?
    bool is_masked_;
};

class GatewayWorker;
class SocketDataChunk;

class WsProto
{
    // Frame information.
    WsProtoFrameInfo frame_info_;

    // WebSocket client handshake key.
    char *client_key_;
    int32_t client_key_len_;

    // WebSocket sub-protocol.
    char *sub_protocol_;
    int32_t sub_protocol_len_;

public:

    // WebSockets frame info.
    WsProtoFrameInfo* get_frame_info()
    {
        return &frame_info_;
    }

    // Sets the client key.
    void SetClientKey(char *newClientKey, int32_t newClientKeyLen)
    {
        client_key_ = newClientKey;
        client_key_len_ = newClientKeyLen;
    }

    // Sets the sub protocol.
    void SetSubProtocol(char *newSubProtocol, int32_t newSubProtocolLen)
    {
        sub_protocol_ = newSubProtocol;
        sub_protocol_len_ = newSubProtocolLen;
    }

    // Initializes WebSocket structure.
    void Init()
    {
        Reset();
    }

    // Resets the structure.
    void Reset()
    {
        client_key_ = NULL;
        client_key_len_ = 0;

        sub_protocol_ = NULL;
        sub_protocol_len_ = 0;
    }

    uint32_t ProcessWsDataToDb(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled);

    uint32_t ProcessWsDataFromDb(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled);

    uint32_t DoHandshake(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, bool* is_handled);

    void MaskUnMask(int32_t payloadLen, uint64_t mask, uint64_t *data);

    uint8_t *WriteData(GatewayWorker *gw, uint8_t opcode, bool masking, WS_FRAGMENT_FLAG frame_type, uint8_t *payload, uint64_t *ppayload_len);

    uint32_t ParseFrameInfo(uint8_t *data);
};

} // namespace network
} // namespace starcounter

#endif // WS_PROTO_HPP
