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
        public UInt64 SocketUniqueId { get { return socketContainer_.SocketUniqueId; } }

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
        internal RawSocket(SchedulerResources.SocketContainer outerSocketContainer) {

            socketContainer_ = outerSocketContainer;
        }

        /// <summary>
        /// Current RawSocket object.
        /// </summary>
        [ThreadStatic]
        internal static RawSocket Current_;

        /// <summary>
        /// Current RawSocket object.
        /// </summary>
        public static RawSocket Current {
            get { return Current_; }
            set { Current_ = value; }
        }

        /// <summary>
        /// Destroys only data stream.
        /// </summary>
        /// <param name="isStarcounterThread"></param>
        internal void DestroyDataStream() {
            if (null != socketContainer_)
                socketContainer_.DestroyDataStream(true);
        }

        /// <summary>
        /// Resets the socket.
        /// </summary>
        internal void Destroy(Boolean isStarcounterThread) {
            if (null != socketContainer_) {
                SchedulerResources.ReturnSocketContainer(socketContainer_, isStarcounterThread);
                socketContainer_ = null;
            }
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~RawSocket() {
            Destroy(false);
        }

        /// <summary>
        /// Checks if socket is dead.
        /// </summary>
        /// <returns></returns>
        public Boolean IsDead() {
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

            Destroy(true);
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

            NetworkDataStream dataStream = socketContainer_.DataStream;

            // Checking if we still have the data stream with original chunk available.
            if (dataStream == null || dataStream.IsDestroyed()) {

                UInt32 err_code = bmx.sc_bmx_obtain_new_chunk(&chunk_index, &chunk_mem);
                if (0 != err_code)
                    throw ErrorCode.ToException(err_code, "Can't obtain new chunk for session push.");

                data_stream = new NetworkDataStream(chunk_mem, chunk_index, socketContainer_.GatewayWorkerId);

            } else {

                data_stream = dataStream;
                chunk_index = data_stream.ChunkIndex;
                chunk_mem = data_stream.RawChunk;
            }

            Byte* socket_data_begin = chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            (*(ScSessionStruct*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);

            (*(UInt32*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte) MixedCodeConstants.NetworkProtocolType.PROTOCOL_RAW_PORT;

            (*(UInt32*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = socketContainer_.SocketIndexNum;
            (*(UInt64*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = socketContainer_.SocketUniqueId;
            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = socketContainer_.GatewayWorkerId;

            (*(UInt16*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            data_stream.SendResponse(data, offset, dataLen, connFlags);
        }

        /// <summary>
        /// Running the given action on raw sockets that meet given criteria.
        /// </summary>
        /// <param name="cargoId">Cargo ID filter (UInt64.MaxValue for all).</param>
        public static void ForEach(UInt64 cargoId, Action<RawSocket> action) {

            // For each scheduler.
            for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {

                Byte schedId = i;

                // Running asynchronous task.
                ScSessionClass.DbSession.RunAsync(() => {

                    // Saving current socket since we are going to set other.
                    RawSocket origCurrentSocket = RawSocket.Current;

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
                                if ((sc != null) && (!sc.IsDead())) {

                                    RawSocket rs = sc.Rs;

                                    // Checking if socket is alive.
                                    if (null != rs) {

                                        // Comparing given cargo ID if any.
                                        if ((cargoId == UInt64.MaxValue) || (cargoId == sc.CargoId)) {

                                            // Setting current socket.
                                            RawSocket.Current = rs;

                                            // Running user delegate with socket as parameter.
                                            action(rs);
                                        }
                                    }
                                }
                            }
                        }
                    } finally {
                        // Restoring original current socket.
                        RawSocket.Current = origCurrentSocket;
                    }

                }, schedId);
            }
        }

        /// <summary>
        /// Disconnecting Raw Sockets that meet given criteria.
        /// </summary>
        public static void DisconnectEach(UInt64 cargoId = UInt64.MaxValue) {
            ForEach(cargoId, (RawSocket s) => { s.Disconnect(); });
        }
    }
}