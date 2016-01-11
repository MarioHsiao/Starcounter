using Starcounter;
using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        /// Converts socket struct to lower and upper parts.
        /// </summary>
        public UInt64 ToUInt64() {
            return socketStruct_.ToUInt64();
        }

        /// <summary>
        /// Creates a new TcpSocket.
        /// </summary>
        internal TcpSocket(NetworkDataStream dataStream)
        {
            SocketStruct socketStruct = new SocketStruct();
            socketStruct.Init(dataStream);

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
        public void Send(Byte[] data) {
            PushServerMessage(data, 0, data.Length);
        }

        /// <summary>
        /// Send data over Tcp socket.
        /// </summary>
        public void Send(Byte[] data, Int32 offset, Int32 dataLen) {
            PushServerMessage(data, offset, dataLen);
        }

        /// <summary>
        /// Send data over Tcp socket.
        /// </summary>
        public void Send(Byte[] data, Int32 offset, Int32 dataLen, Response.ConnectionFlags connFlags) {
            PushServerMessage(data, offset, dataLen, connFlags);
        }

        /// <summary>
        /// Streaming over TCP socket.
        /// NOTE: Function closes the stream once the end of stream is reached.
        /// </summary>
        internal async Task SendStreamOverSocket() {

            try {
                UInt64 socketId = ToUInt64();

                StreamingInfo streamInfo = Response.ResponseStreams_[socketId];

                Int32 numBytesRead = await streamInfo.StreamObject.ReadAsync(streamInfo.SendBuffer, 0, streamInfo.SendBuffer.Length);

                // Checking if its the end of the stream.
                Boolean hasReadEverything = streamInfo.HasReadEverything();

                if (hasReadEverything) {

                    StreamingInfo s;
                    Response.ResponseStreams_.TryRemove(socketId, out s);

                    // Now we are done with streaming object and can close it.
                    streamInfo.StreamObject.Close();
                    streamInfo.StreamObject = null;
                }

                // Task has finished.
                streamInfo.TaskObject = null;

                // We need to be on scheduler to send on socket.
                StarcounterBase._DB.RunAsync(() => {

                    // Sending on socket.
                    if (hasReadEverything) {
                        Send(streamInfo.SendBuffer, 0, numBytesRead);
                    } else {
                        Send(streamInfo.SendBuffer, 0, numBytesRead, Response.ConnectionFlags.StreamingResponseBody);
                    }
                });

            } catch (Exception exc) {

                // Just logging the exception.
                Diagnostics.LogHostException(exc);
            }
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

            // Checking if we still have the data stream with original chunk available.
            if (dataStream_ == null || dataStream_.IsDestroyed()) {

                UInt32 errCode = bmx.sc_bmx_obtain_new_chunk(&chunkIndex, &chunkMem);

                if (0 != errCode) {

                    if (Error.SCERRACQUIRELINKEDCHUNKS == errCode) {
                        // NOTE: If we can not obtain a chunk just returning because we can't do much.
                        return;
                    } else {
                        throw ErrorCode.ToException(errCode);
                    }
                }

                dataStream = new NetworkDataStream(chunkIndex, socketStruct_.GatewayWorkerId);

            } else {

                dataStream = dataStream_;
                chunkIndex = dataStream.ChunkIndex;
                chunkMem = dataStream.GetChunkMemory();
            }

            Byte* socket_data_begin = chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            (*(ScSessionStruct*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte) MixedCodeConstants.NetworkProtocolType.PROTOCOL_TCP;

            (*(UInt32*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = socketStruct_.SocketIndexNum;
            (*(UInt64*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = socketStruct_.SocketUniqueId;
            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = socketStruct_.GatewayWorkerId;

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            dataStream.SendResponse(data, offset, dataLen, connFlags);
        }
    }
}