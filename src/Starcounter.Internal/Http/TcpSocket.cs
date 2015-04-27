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
        /// Unique socket id on gateway.
        /// </summary>
        public UInt64 SocketUniqueId { get { return socketContainer_.SocketUniqueId; } }

        /// <summary>
        /// Scheduler ID to which this socket belongs.
        /// </summary>
        internal Byte schedulerId_ = StarcounterConstants.MaximumSchedulersNumber;

        /// <summary>
        /// Cargo ID getter.
        /// </summary>
        public UInt64 CargoId {
            get { return socketContainer_.CargoId; }
            set { socketContainer_.CargoId = value; }
        } 

        /// <summary>
        /// Socket container.
        /// </summary>
        SchedulerResources.SocketContainer socketContainer_;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal TcpSocket(SchedulerResources.SocketContainer outerSocketContainer) {

            socketContainer_ = outerSocketContainer;
            schedulerId_ = StarcounterEnvironment.CurrentSchedulerId;
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
        /// Destroys only data stream.
        /// </summary>
        /// <param name="isStarcounterThread"></param>
        internal void DestroyDataStream() {
            if (null != socketContainer_)
                socketContainer_.DestroyDataStream();
        }

        /// <summary>
        /// Resets the socket.
        /// </summary>
        internal void Destroy() {

            if (null != socketContainer_) {
                SchedulerResources.ReturnSocketContainer(socketContainer_);
                socketContainer_ = null;
            }
        }

        /// <summary>
        /// Checks if socket is dead.
        /// </summary>
        /// <returns></returns>
        public Boolean IsDead() {

            // Checking if WebSocket is used on correct scheduler.
            if (StarcounterEnvironment.CurrentSchedulerId != schedulerId_) {
                throw ErrorCode.ToException(Error.SCERRBADSCHEDIDSUPPLIED, 
                    "Trying to perform an operation on TcpSocket that belongs to a different scheduler.");
            }

            return ((null == socketContainer_) || (socketContainer_.IsDead()));
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

            Destroy();
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

            NetworkDataStream dataStream;
            UInt32 chunkIndex;
            Byte* chunkMem;

            NetworkDataStream existingDataStream = socketContainer_.DataStream;

            // Checking if we still have the data stream with original chunk available.
            if (existingDataStream == null || existingDataStream.IsDestroyed()) {

                UInt32 err_code = bmx.sc_bmx_obtain_new_chunk(&chunkIndex, &chunkMem);
                if (0 != err_code) {
                    throw ErrorCode.ToException(err_code, "Can't obtain new chunk for session push.");
                }

                dataStream = new NetworkDataStream();
                dataStream.Init(chunkMem, chunkIndex, socketContainer_.GatewayWorkerId);

            } else {

                dataStream = existingDataStream;
                chunkIndex = dataStream.ChunkIndex;
                chunkMem = dataStream.RawChunk;
            }

            Byte* socket_data_begin = chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            (*(ScSessionStruct*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte) MixedCodeConstants.NetworkProtocolType.PROTOCOL_RAW_PORT;

            (*(UInt32*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = socketContainer_.SocketIndexNum;
            (*(UInt64*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = socketContainer_.SocketUniqueId;
            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = socketContainer_.GatewayWorkerId;

            (*(UInt16*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            dataStream.SendResponse(data, offset, dataLen, connFlags);
        }

        /// <summary>
        /// Running the given action on raw sockets that meet given criteria.
        /// </summary>
        public static void ForEach(UInt64 cargoId, Action<TcpSocket> action, UInt16 port) {

            // Checking if port is not specified.
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port) {
                if (StarcounterEnvironment.IsAdministratorApp) {
                    port = StarcounterEnvironment.Default.SystemHttpPort;
                } else {
                    port = StarcounterEnvironment.Default.UserHttpPort;
                }
            }

            // For each scheduler.
            for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {

                Byte schedId = i;

                // Running asynchronous task.
                ScSessionClass.DbSession.RunAsync(() => {

                    // Saving current socket since we are going to set other.
                    TcpSocket origCurrentSocket = TcpSocket.Current;

                    try {
                        SchedulerResources.SchedulerSockets ss = SchedulerResources.AllHostSockets.GetSchedulerSockets(schedId);

                        // Going through each gateway worker.
                        for (Byte gwWorkerId = 0; gwWorkerId < StarcounterEnvironment.Gateway.NumberOfWorkers; gwWorkerId++) {

                            SchedulerResources.SocketsPerSchedulerPerGatewayWorker spspgw = ss.GetSocketsPerGatewayWorker(gwWorkerId);

                            // Going through each active socket.
                            foreach (UInt32 wsIndex in spspgw.ActiveSocketIndexes) {

                                // Getting socket container.
                                SchedulerResources.SocketContainer sc = spspgw.GetSocket(wsIndex);

                                // Checking if its a raw socket.
                                if ((sc != null) && (port == sc.Port) && (!sc.IsDead())) {

                                    TcpSocket rs = sc.Rs;

                                    // Checking if socket is alive.
                                    if (null != rs) {

                                        // Comparing given cargo ID if any.
                                        if ((cargoId == UInt64.MaxValue) || (cargoId == sc.CargoId)) {

                                            // Setting current socket.
                                            TcpSocket.Current = rs;

                                            // Running user delegate with socket as parameter.
                                            action(rs);
                                        }
                                    }
                                }
                            }
                        }
                    } finally {
                        // Restoring original current socket.
                        TcpSocket.Current = origCurrentSocket;
                    }

                }, schedId);
            }
        }

        /// <summary>
        /// Disconnecting Tcp Sockets that meet given criteria.
        /// </summary>
        public static void DisconnectEach(UInt64 cargoId = UInt64.MaxValue, UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort) {
            ForEach(cargoId, (TcpSocket s) => { s.Disconnect(); }, port);
        }
    }
}