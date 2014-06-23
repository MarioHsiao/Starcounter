using Starcounter;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Starcounter {

    public class RawSocket {

        /// <summary>
        /// Unique socket id on gateway.
        /// </summary>
        UInt64 socketUniqueId_;

        /// <summary>
        /// Unique socket id on gateway.
        /// </summary>
        public UInt64 SocketUniqueId { get { return socketUniqueId_; } }

        /// <summary>
        /// Socket index on gateway.
        /// </summary>
        UInt32 socketIndexNum_;

        /// <summary>
        /// Socket index on gateway.
        /// </summary>
        internal UInt32 SocketIndexNum { get { return socketIndexNum_; } }

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        Byte gatewayWorkerId_;

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        internal Byte GatewayWorkerId { get { return gatewayWorkerId_; } }

        /// <summary>
        /// Network data stream.
        /// </summary>
        NetworkDataStream dataStream_;

        /// <summary>
        /// Network data stream.
        /// </summary>
        public NetworkDataStream DataStream { get { return dataStream_; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="socketIndexNum"></param>
        /// <param name="socketUniqueId"></param>
        /// <param name="gatewayWorkerId"></param>
        /// <param name="dataStream"></param>
        internal RawSocket(
            UInt32 socketIndexNum,
            UInt64 socketUniqueId,
            Byte gatewayWorkerId,
            NetworkDataStream dataStream) {

            socketIndexNum_ = socketIndexNum;
            socketUniqueId_ = socketUniqueId;
            gatewayWorkerId_ = gatewayWorkerId;
            dataStream_ = dataStream;
        }

        internal void Reset() {
            gatewayWorkerId_ = 255;
        }

        /// <summary>
        /// Checks if socket is dead.
        /// </summary>
        /// <returns></returns>
        public Boolean IsDead() {
            return (gatewayWorkerId_ == 255);
        }

        /// <summary>
        /// TODO: Get rid of that!
        /// </summary>
        static Byte[] TempDisconnectBuffer = new Byte[1];

        /// <summary>
        /// Disconnects active socket.
        /// </summary>
        public void Disconnect() {

            PushServerMessage(TempDisconnectBuffer, 0, 1, Response.ConnectionFlags.DisconnectImmediately);

            Reset();
        }

        /// <summary>
        /// Send data over raw socket.
        /// </summary>
        /// <param name="data">Data to push.</param>
        public void Send(Byte[] data) {
            PushServerMessage(data, 0, data.Length);
        }

        /// <summary>
        /// Send data over raw socket.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="offset">Offset in data.</param>
        /// <param name="dataLen">Data length in bytes.</param>
        public void Send(Byte[] data, Int32 offset, Int32 dataLen) {
            PushServerMessage(data, offset, dataLen);
        }

        /// <summary>
        /// Pushes server message over socket.
        /// </summary>
        internal unsafe void PushServerMessage(
            Byte[] data,
            Int32 offset,
            Int32 dataLen,
            Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags) {

            // Checking if socket is already dead.
            if (IsDead())
                return;

            NetworkDataStream data_stream;
            UInt32 chunk_index;
            Byte* chunk_mem;

            // Checking if we still have the data stream with original chunk available.
            if (dataStream_ == null || dataStream_.IsDestroyed()) {

                UInt32 err_code = bmx.sc_bmx_obtain_new_chunk(&chunk_index, &chunk_mem);
                if (0 != err_code)
                    throw ErrorCode.ToException(err_code, "Can't obtain new chunk for session push.");

                data_stream = new NetworkDataStream(chunk_mem, chunk_index, gatewayWorkerId_);

            } else {

                data_stream = dataStream_;
                chunk_index = data_stream.ChunkIndex;
                chunk_mem = data_stream.RawChunk;
            }

            Byte* socket_data_begin = chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            (*(ScSessionStruct*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);

            (*(UInt32*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte) MixedCodeConstants.NetworkProtocolType.PROTOCOL_RAW_PORT;

            (*(UInt32*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = socketIndexNum_;
            (*(UInt64*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = socketUniqueId_;
            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = gatewayWorkerId_;

            (*(UInt16*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            data_stream.SendResponse(data, offset, dataLen, connFlags);
        }
    }
}