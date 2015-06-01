using Starcounter;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Starcounter {

    public class TcpSocket {

        /// <summary>
        /// Socket structure.
        /// </summary>
        SocketStruct socketStruct_;

        /// <summary>
        /// Socket structure.
        /// </summary>
        internal SocketStruct InternalSocketStruct {
            get {
                return socketStruct_;
            }
        }

        /// <summary>
        /// Converts lower and upper part of TcpSocket into an object.
        /// </summary>
        /// <param name="lowerPart">Lower part of the socket info.</param>
        /// <param name="upperPart">Upper part of the socket info.</param>
        public TcpSocket(UInt64 lowerPart, UInt64 upperPart) {
            socketStruct_ = SocketStruct.FromLowerUpper(lowerPart, upperPart);
        }

        /// <summary>
        /// Converts TcpSocket to lower and upper parts.
        /// </summary>
        /// <param name="socketIdLower">Lower part of socket ID.</param>
        /// <param name="socketIdUpper">Upper part of socket ID.</param>
        public void ToLowerUpper(
            out UInt64 socketIdLower,
            out UInt64 socketIdUpper) {

            SocketStruct.ToLowerUpper(socketStruct_, out socketIdLower, out socketIdUpper);
        }

        /// <summary>
        /// Creates a new TcpSocket.
        /// </summary>
        internal TcpSocket(
            NetworkDataStream dataStream,
            SocketStruct socketStruct)
        {
            dataStream_ = dataStream;
            socketStruct_ = socketStruct;
        }

        /// <summary>
        /// Register TCP socket handler.
        /// </summary>
        internal delegate void RegisterTcpSocketHandlerDelegate(
            UInt16 port,
            String appName,
            Action<TcpSocket, Byte[]> tcpCallback,
            out UInt64 handlerInfo);

        /// <summary>
        /// Delegate to register TCP handler.
        /// </summary>
        static internal RegisterTcpSocketHandlerDelegate RegisterTcpSocketHandler_;

        /// <summary>
        /// Initializes TCP sockets.
        /// </summary>
        /// <param name="registerTcpHandlerNative"></param>
        internal static void InitTcpSockets(RegisterTcpSocketHandlerDelegate h) {
            RegisterTcpSocketHandler_ = h;
        }

        /// <summary>
        /// Current RawSocket object.
        /// </summary>
        [ThreadStatic]
        internal static TcpSocket Current_;

        /// <summary>
        /// Current RawSocket object.
        /// </summary>
        public static TcpSocket Current {
            get { return Current_; }
            set { Current_ = value; }
        }

        /// <summary>
        /// Network data stream.
        /// </summary>
        NetworkDataStream dataStream_;

        /// <summary>
        /// Network data stream.
        /// </summary>
        internal NetworkDataStream DataStream {
            get {
                return dataStream_;
            }
        }

        /// <summary>
        /// Checks if socket is dead.
        /// </summary>
        /// <returns></returns>
        public Boolean IsDead() {

            return socketStruct_.IsDead();
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

            socketStruct_.Kill();
        }

        /// <summary>
        /// Destroys the socket.
        /// </summary>
        internal void Destroy(Boolean isStarcounterThread) {

            if (dataStream_ != null) {
                dataStream_.Destroy(isStarcounterThread);
            }

            socketStruct_.Kill();
        }

        /// <summary>
        /// Send data over Tcp socket.
        /// </summary>
        /// <param name="data">Data to push.</param>
        public void Send(Byte[] data) {
            PushServerMessage(data, 0, data.Length);
        }

        /// <summary>
        /// Send data over Tcp socket.
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

            NetworkDataStream dataStream;
            UInt32 chunkIndex;
            Byte* chunkMem;

            NetworkDataStream existingDataStream = dataStream_;

            // Checking if we still have the data stream with original chunk available.
            if (existingDataStream == null || existingDataStream.IsDestroyed()) {

                UInt32 err_code = bmx.sc_bmx_obtain_new_chunk(&chunkIndex, &chunkMem);
                if (0 != err_code) {
                    throw ErrorCode.ToException(err_code, "Can't obtain new chunk for session push.");
                }

                dataStream = new NetworkDataStream();
                dataStream.Init(chunkMem, chunkIndex, socketStruct_.GatewayWorkerId);

            } else {

                dataStream = existingDataStream;
                chunkIndex = dataStream.ChunkIndex;
                chunkMem = dataStream.RawChunk;
            }

            Byte* socket_data_begin = chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            (*(ScSessionStruct*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte) MixedCodeConstants.NetworkProtocolType.PROTOCOL_RAW_PORT;

            (*(UInt32*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = socketStruct_.SocketIndexNum;
            (*(UInt64*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = socketStruct_.SocketUniqueId;
            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = socketStruct_.GatewayWorkerId;

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            dataStream.SendResponse(data, offset, dataLen, connFlags);
        }
    }
}