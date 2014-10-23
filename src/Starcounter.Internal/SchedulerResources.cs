using System.Threading.Tasks;
using Starcounter.Rest;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Advanced;

namespace Starcounter.Internal
{
    class SchedulerResources {

        public const Int32 ResponseTempBufSize = 4096;

        Byte[] response_temp_buf_ = new Byte[ResponseTempBufSize];

        public Byte[] ResponseTempBuf { get { return response_temp_buf_; } }

        public Response AggregationStubResponse = new Response() { Body = "Xaxa!" };

        static SchedulerResources[] all_schedulers_resources_;

        public static void Init(Int32 numSchedulers) {
            all_schedulers_resources_ = new SchedulerResources[numSchedulers];

            for (Int32 i = 0; i < numSchedulers; i++) {
                all_schedulers_resources_[i] = new SchedulerResources();
                all_schedulers_resources_[i].AggregationStubResponse.ConstructFromFields();
            }
        }

        public static SchedulerResources Current {
            get { return all_schedulers_resources_[StarcounterEnvironment.CurrentSchedulerId]; }
        }

        internal class GlobalSockets {

            static SchedulerSockets[] socketsPerScheduler_ = null;

            internal SchedulerSockets GetSchedulerSockets(Byte schedId) {
                return socketsPerScheduler_[schedId];
            }

            internal SocketsPerSchedulerPerGatewayWorker GetSchedulerWorkerSockets(Byte schedId, Byte gwWorkerId) {
                return socketsPerScheduler_[schedId].GetSocketsPerGatewayWorker(gwWorkerId);
            }

            public GlobalSockets() {
                socketsPerScheduler_ = new SchedulerSockets[StarcounterEnvironment.SchedulerCount];

                for (Int32 i = 0; i < socketsPerScheduler_.Length; i++) {
                    socketsPerScheduler_[i] = new SchedulerSockets();
                }
            }
        }

        internal class SocketContainer {

            internal WebSocketInternal Ws {
                get; set;
            }

            public TcpSocket Rs {
                get; set;
            }

            /// <summary>
            /// Active list node.
            /// </summary>
            LinkedListNode<UInt32> activeListNode_;

            /// <summary>
            /// Active list node.
            /// </summary>
            internal LinkedListNode<UInt32> ActiveListNode {
                get { return activeListNode_; }
            }

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
            /// Scheduler to which socket belongs.
            /// </summary>
            Byte scheduler_id_;

            /// <summary>
            /// Scheduler id.
            /// </summary>
            internal Byte SchedulerId { get { return scheduler_id_; } }

            /// <summary>
            /// Gateway worker id.
            /// </summary>
            Byte gatewayWorkerId_;

            /// <summary>
            /// Gateway worker id.
            /// </summary>
            internal Byte GatewayWorkerId { get { return gatewayWorkerId_; } }

            /// <summary>
            /// User cargo id.
            /// </summary>
            UInt64 cargoId_;

            /// <summary>
            /// Cargo ID getter.
            /// </summary>
            internal UInt64 CargoId {
                get { return cargoId_; }
                set { cargoId_ = value; }
            }

            /// <summary>
            /// Network data stream.
            /// </summary>
            NetworkDataStream dataStream_;

            /// <summary>
            /// Network data stream.
            /// </summary>
            public NetworkDataStream DataStream {
                get { return dataStream_; }
                set { dataStream_ = value; }
            }

            public void Init(
                UInt32 socketIndexNum,
                UInt64 socketUniqueId,
                Byte gatewayWorkerId,
                LinkedListNode<UInt32> activeListNode) {

                socketIndexNum_ = socketIndexNum;
                socketUniqueId_ = socketUniqueId;
                gatewayWorkerId_ = gatewayWorkerId;
                activeListNode_ = activeListNode;
                scheduler_id_ = StarcounterEnvironment.CurrentSchedulerId;
            }

            public void DestroyDataStream() {

                if (null != dataStream_) {
                    dataStream_.Destroy(true);
                    dataStream_ = null;
                }
            }

            public void Destroy() {

                Rs = null;
                Ws = null;
                socketUniqueId_ = 0;
                activeListNode_ = null;

                DestroyDataStream();
            }

            public Boolean IsDead() {
                return (0 == socketUniqueId_);
            }
        }

        internal class SocketsPerSchedulerPerGatewayWorker {

            public const Int32 MaxSocketsPerSchedulerPerGatewayWorker = 30000;

            SocketContainer[] sockets_ = null;

            Byte gatewayWorkerId_;

            public SocketsPerSchedulerPerGatewayWorker(Byte gatewayWorkerId) {
                sockets_ = new SocketContainer[MaxSocketsPerSchedulerPerGatewayWorker];
                gatewayWorkerId_ = gatewayWorkerId;
            }

            LinkedList<UInt32> activeSocketIndexes_ = new LinkedList<UInt32>();
            internal LinkedList<UInt32> ActiveSocketIndexes { get { return activeSocketIndexes_; } }

            LinkedList<UInt32> freeLinkedListNodes_ = new LinkedList<UInt32>();

            public SocketContainer GetSocket(UInt32 socketIndexNum) {
                return sockets_[socketIndexNum];
            }

            public SocketContainer GetSocket(UInt32 socketIndexNum, UInt64 socketUniqueId) {

                // Checking that socket exists and legal.
                if ((sockets_[socketIndexNum] != null) &&
                    (sockets_[socketIndexNum].SocketUniqueId == socketUniqueId)) {

                    return sockets_[socketIndexNum];
                }

                return null;
            }

            public SocketContainer AddSocket(UInt32 socketIndexNum, UInt64 socketUniqueId, Byte gatewayWorkerId) {

                Debug.Assert(sockets_[socketIndexNum] == null);

                sockets_[socketIndexNum] = new SocketContainer();

                LinkedListNode<UInt32> lln = null;

                if (freeLinkedListNodes_.First != null) {
                    lln = freeLinkedListNodes_.First;
                    freeLinkedListNodes_.RemoveFirst();
                    lln.Value = socketIndexNum;
                } else {
                    lln = new LinkedListNode<UInt32>(socketIndexNum);
                }

                activeSocketIndexes_.AddLast(lln);

                sockets_[socketIndexNum].Init(socketIndexNum, socketUniqueId, gatewayWorkerId, lln);

                return sockets_[socketIndexNum];
            }

            public void RemoveActiveSocket(SocketContainer sc) {

                Debug.Assert(sc.SchedulerId == StarcounterEnvironment.CurrentSchedulerId);

                LinkedListNode<UInt32> activeListNode = sc.ActiveListNode;
                Debug.Assert(activeListNode != null);
                Debug.Assert(activeListNode.Value == sc.SocketIndexNum);

                Debug.Assert(sockets_[sc.SocketIndexNum] != null);
                sockets_[sc.SocketIndexNum] = null;
                activeSocketIndexes_.Remove(activeListNode);
                freeLinkedListNodes_.AddLast(activeListNode);

                sc.Destroy();
            }
        }

        internal class SchedulerSockets {

            SocketsPerSchedulerPerGatewayWorker[] socketsPerSchedulerPerGatewayWorker_ = null;

            internal SocketsPerSchedulerPerGatewayWorker GetSocketsPerGatewayWorker(Byte gwWorkerId) {
                return socketsPerSchedulerPerGatewayWorker_[gwWorkerId];
            }

            public SchedulerSockets() {
                socketsPerSchedulerPerGatewayWorker_ = new SocketsPerSchedulerPerGatewayWorker[StarcounterEnvironment.Gateway.NumberOfWorkers];

                for (Byte i = 0; i < socketsPerSchedulerPerGatewayWorker_.Length; i++) {
                    socketsPerSchedulerPerGatewayWorker_[i] = new SocketsPerSchedulerPerGatewayWorker(i);
                }
            }
        }

        static GlobalSockets allHostSockets_;

        internal static GlobalSockets AllHostSockets { get { return allHostSockets_; }}

        internal static void InitSockets() {
            allHostSockets_ = new GlobalSockets();
        }

        internal static void ReturnSocketContainer(SocketContainer sc) {

            SocketsPerSchedulerPerGatewayWorker s;

            s = AllHostSockets.GetSchedulerWorkerSockets(StarcounterEnvironment.CurrentSchedulerId, sc.GatewayWorkerId);
            s.RemoveActiveSocket(sc);
        }

        internal static SocketContainer ObtainSocketContainerForRawSocket(NetworkDataStream dataStream) {
            unsafe {

                // Obtaining socket index and unique id.
                UInt32 socketIndex = *(UInt32*)(dataStream.RawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER);
                UInt64 socketUniqueId = *(UInt64*)(dataStream.RawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID);

                Byte schedId = StarcounterEnvironment.CurrentSchedulerId;
                SchedulerResources.SocketsPerSchedulerPerGatewayWorker sws = SchedulerResources.AllHostSockets.GetSchedulerWorkerSockets(schedId, dataStream.GatewayWorkerId);

                SchedulerResources.SocketContainer sc = null;

                // Checking if socket container does not exist.
                if (null == sws.GetSocket(socketIndex)) {
                    sc = sws.AddSocket(socketIndex, socketUniqueId, dataStream.GatewayWorkerId);
                    sc.Rs = new TcpSocket(sc);
                } else {
                    sc = sws.GetSocket(socketIndex, socketUniqueId);
                }

                // Setting data stream.
                if (null != sc) {
                    sc.DataStream = dataStream;
                }

                return sc;
            }
        }

        internal static SocketContainer ObtainSocketContainerForWebSocket(NetworkDataStream dataStream) {
            unsafe {

                // Obtaining socket index and unique id.
                UInt32 socketIndex = *(UInt32*)(dataStream.RawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER);
                UInt64 socketUniqueId = *(UInt64*)(dataStream.RawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID);

                Byte schedId = StarcounterEnvironment.CurrentSchedulerId;
                SchedulerResources.SocketsPerSchedulerPerGatewayWorker sws = SchedulerResources.AllHostSockets.GetSchedulerWorkerSockets(schedId, dataStream.GatewayWorkerId);

                SchedulerResources.SocketContainer sc = null;

                // Checking if socket container exists.
                if (null != sws.GetSocket(socketIndex)) {
                    sc = sws.GetSocket(socketIndex, socketUniqueId);
                }

                // Setting data stream.
                if (null != sc) {
                    sc.DataStream = dataStream;
                }

                return sc;
            }
        }

        internal static WebSocketInternal CreateNewWebSocket(
            UInt32 socketIndex,
            UInt64 socketUniqueId,
            Byte gatewayWorkerId,
            UInt64 cargoId,
            UInt32 channelId) {
            unsafe {

                Byte schedId = StarcounterEnvironment.CurrentSchedulerId;

                SchedulerResources.SocketsPerSchedulerPerGatewayWorker sws = SchedulerResources.AllHostSockets.GetSchedulerWorkerSockets(schedId, gatewayWorkerId);

                SchedulerResources.SocketContainer sc = null;

                // Checking if socket container exists.
                sc = sws.GetSocket(socketIndex);
                if (null != sc) {
                    sws.RemoveActiveSocket(sc);
                }

                // Adding new socket.
                sc = sws.AddSocket(socketIndex, socketUniqueId, gatewayWorkerId);

                Debug.Assert(sc != null);
                sc.CargoId = cargoId;

                Debug.Assert(null == sc.Ws);

                sc.Ws = new WebSocketInternal(sc, channelId);

                return sc.Ws;
            }
        }
    }
}