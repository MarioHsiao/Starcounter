using Starcounter;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Starcounter {

    public class UdpSocket {

        /// <summary>
        /// Register UDP socket handler.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="appName"></param>
        /// <param name="rawCallback"></param>
        /// <param name="handlerInfo"></param>
        internal delegate void RegisterUdpSocketHandlerDelegate(
            UInt16 port,
            String appName,
            Action<IPAddress, UInt16, Byte[]> udpCallback,
            out UInt64 handlerInfo);

        /// <summary>
        /// Delegate to register UDP handler.
        /// </summary>
        static internal RegisterUdpSocketHandlerDelegate RegisterUdpSocketHandler_;

        /// <summary>
        /// Initializes UDP sockets.
        /// </summary>
        /// <param name="registerTcpHandlerNative"></param>
        internal static void InitUdpSockets(RegisterUdpSocketHandlerDelegate h) {
            RegisterUdpSocketHandler_ = h;
        }

        /// <summary>
        /// Maximum datagram size.
        /// </summary>
        public const Int32 MaxDatagramSize = 65000;

        /// <summary>
        /// Sending UDP datagram.
        /// </summary>
        /// <param name="ipTo">IP address of destination.</param>
        /// <param name="portTo">Destination port.</param>
        /// <param name="portFrom">Port from which datagram should be sent.</param>
        /// <param name="datagram">Datagram bytes.</param>
        public static void Send(IPAddress ipTo, UInt16 portTo, UInt16 portFrom, Byte[] datagram) {

            // Checking datagram size.
            if (datagram.Length > MaxDatagramSize)
                throw new ArgumentException("Given UDP datagram size is larger than maximum allowed: " + MaxDatagramSize);

            // Converting IP address to UInt32.
            UInt32 ipInt = BitConverter.ToUInt32(ipTo.GetAddressBytes(), 0);

            // Pushing chunk containing datagram to gateway.
            PushChunksToGateway(ipInt, portTo, portFrom, datagram, 0, datagram.Length);
        }

        /// <summary>
        /// Sending UDP datagram.
        /// </summary>
        /// <param name="ipTo">IP address of destination.</param>
        /// <param name="portTo">Destination port.</param>
        /// <param name="datagram">Datagram bytes.</param>
        public static void Send(IPAddress ipTo, UInt16 portTo, UInt16 portFrom, String datagram) {

            Byte[] datagramBytes = Encoding.UTF8.GetBytes(datagram);

            Send(ipTo, portTo, portFrom, datagramBytes);
        }

        /// <summary>
        /// Internal UDP datagram.
        /// </summary>
        /// <param name="ipToInt">IPv4 address of destination as integer.</param>
        /// <param name="portTo">Destination port.</param>
        /// <param name="datagram">Datagram bytes.</param>
        internal void Send(UInt32 ipToInt, UInt16 portTo, UInt16 portFrom, Byte[] datagram) {
            PushChunksToGateway(ipToInt, portTo, portFrom, datagram, 0, datagram.Length);
        }

        /// <summary>
        /// Global round robin gateway worker id.
        /// </summary>
        static Byte UdpRoundRobinGatewayWorkerId = 0;

        /// <summary>
        /// Send response buffer.
        /// </summary>
        /// <param name="chunk_index"></param>
        /// <param name="p"></param>
        /// <param name="offset"></param>
        /// <param name="length_bytes"></param>
        unsafe static void SendUdpData(Byte gwWorkerId, UInt32 chunkIndex, Byte* dataPtr, Int32 offset, Int32 lengthBytes) {

            // Checking if we are actually sending something.
            if (lengthBytes <= 0) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, "You are trying to send an empty data.");
            }

            // NOTE: We are ignoring error here because we can't do much about it.
            bmx.sc_bmx_send_buffer(
                gwWorkerId,
                dataPtr + offset,
                lengthBytes,
                &chunkIndex,
                (UInt32)Response.ConnectionFlags.NoSpecialFlags);
        }

        /// <summary>
        /// Pushes server message over socket.
        /// </summary>
        unsafe static void PushChunksToGateway(
            UInt32 ipTo,
            UInt16 portTo,
            UInt16 portFrom,
            Byte[] data,
            Int32 offset,
            Int32 lengthBytes) {

            UInt32 chunkIndex;
            Byte* chunkMem;

            // Getting IPC chunk.
            UInt32 errCode = bmx.sc_bmx_obtain_new_chunk(&chunkIndex, &chunkMem);

            if (0 != errCode) {

                if (Error.SCERRACQUIRELINKEDCHUNKS == errCode) {
                    // NOTE: If we can not obtain a chunk just returning because we can't do much.
                    return;
                } else {
                    throw ErrorCode.ToException(errCode);
                }
            }

            Byte* socketDataBegin = chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            (*(ScSessionStruct*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte)MixedCodeConstants.NetworkProtocolType.PROTOCOL_UDP;

            (*(UInt32*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = 0;
            (*(UInt64*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = 0;

            // Doing round robin for gateway worker id.
            Byte gwWorkerId = UdpRoundRobinGatewayWorkerId;
            UdpRoundRobinGatewayWorkerId++;
            if (gwWorkerId >= StarcounterEnvironment.Gateway.NumberOfWorkers) {
                UdpRoundRobinGatewayWorkerId = 0;
                gwWorkerId = 0;
            }

            (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = gwWorkerId;

            // Zeroing SOCKADDR structure.
            (*(UInt64*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_UDP_DESTINATION_SOCKADDR)) = 0;
            (*(UInt64*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_UDP_DESTINATION_SOCKADDR + 8)) = 0;
            
            // Copying destination IP address and port.
            (*(UInt32*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_UDP_DESTINATION_IP)) = ipTo;
            (*(UInt16*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_UDP_DESTINATION_PORT)) = portTo;
            (*(UInt16*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_UDP_SOURCE_PORT)) = portFrom;

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            // Running on current Starcounter thread.
            fixed (Byte* p = data) {
                SendUdpData(gwWorkerId, chunkIndex, p, offset, lengthBytes);
            }
        }
    }
}