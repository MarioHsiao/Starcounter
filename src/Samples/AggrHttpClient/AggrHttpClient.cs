#define FAST_LOOPBACK
#define FAKE_SERVER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AggrHttpClient {
    class Program {

        [StructLayout(LayoutKind.Sequential)]
        public struct AggregationStruct {
            public UInt64 unique_socket_id_;
            public Int32 size_bytes_;
            public UInt32 socket_info_index_;
            public Int32 unique_aggr_index_;
            public UInt16 port_number_;
        }

        const Int32 AggregationStructSizeBytes = 24;

        const UInt16 UserPort = 8080;
        const UInt16 AggregationPort = 9191;
        const String ServerIp = "127.0.0.1";
        const Int32 RecvBufSize = 1024 * 1024 * 16;
        const Int32 SendBufSize = 1024 * 1024 * 16;

        const Int32 NumRequestsInSingleSend = 5000;

        public enum AggregationMessageTypes {
            AGGR_CREATE_SOCKET,
            AGGR_DESTROY_SOCKET,
            AGGR_DATA
        };

        class WorkerSettings
        {
            public Int32 NumRequestsToSend;
            public Int32 NumBodyCharacters;
            public CountdownEvent CountdownEvent;
            public String PrintLock;
            public Int32[] WorkersRPS;
        };

        static unsafe void ServerMode() {

            Console.WriteLine("Starting server...");

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1 << 19);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1 << 19);

#if FAST_LOOPBACK
            const int SIO_LOOPBACK_FAST_PATH = (-1744830448);
            Byte[] OptionInValue = BitConverter.GetBytes(1);

            sock.IOControl(
                SIO_LOOPBACK_FAST_PATH,
                OptionInValue,
                null);
#endif

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), AggregationPort);
            sock.Bind(localEndPoint);
            sock.Listen(1);
            Socket acceptSock = sock.Accept();

            Byte[] recvBuf = new Byte[RecvBufSize];

            // AGGR_CREATE_SOCKET phase:

            Int32 numBytesReceived = acceptSock.Receive(recvBuf);
            if (numBytesReceived != AggregationStructSizeBytes + 1)
                throw new ArgumentOutOfRangeException("numBytesReceived != AggregationStructSizeBytes");

            AggregationMessageTypes a = (AggregationMessageTypes) recvBuf[0];

            if (a != AggregationMessageTypes.AGGR_CREATE_SOCKET)
                throw new ArgumentOutOfRangeException("a != AggregationMessageTypes.AGGR_CREATE_SOCKET");

            fixed (Byte* p = recvBuf) {
                AggregationStruct agsOrig = *(AggregationStruct*)(p + 1);
            }

            acceptSock.Send(recvBuf, 1, AggregationStructSizeBytes, SocketFlags.None);

            // Send/Receive phase:

            while (true) {
                numBytesReceived = acceptSock.Receive(recvBuf);
                if (0 == numBytesReceived) {
                    Console.WriteLine("Client disconnected. Done.");
                    return;
                }
                
                acceptSock.Send(recvBuf, numBytesReceived, SocketFlags.None);
            }
        }

        static unsafe void Main(string[] args) {

            Int32 numWorkers = 1;

            if (args.Length > 0) {

                if (args[0] == "s") {
                    ServerMode();
                    return;
                }

                numWorkers = Int32.Parse(args[0]);
            }

            WorkerSettings ws = new WorkerSettings() {
                NumRequestsToSend = 5000000,
                NumBodyCharacters = 8,
                CountdownEvent = new CountdownEvent(numWorkers),
                PrintLock = "Lock",
                WorkersRPS = new Int32[numWorkers]
            };

            if ((ws.NumRequestsToSend % NumRequestsInSingleSend) != 0)
                throw new ArgumentException("NumRequests should be divisible by 1000.");

            Stopwatch time = new Stopwatch();
            time.Start();

            for (Int32 i = 0; i < numWorkers; i++) {
                Int32 workerId = i;
                ThreadStart threadDelegate = new ThreadStart(() => Worker(workerId, ws));
                Thread newThread = new Thread(threadDelegate);
                newThread.Start();
            }
            
            ws.CountdownEvent.Wait();
            time.Stop();

            Int32 totalRPS = 0;
            for (Int32 i = 0; i < numWorkers; i++)
                totalRPS += ws.WorkersRPS[i];

            Console.WriteLine(String.Format("Summary: total RPS is {0}, total time {1} ms.", totalRPS, time.ElapsedMilliseconds));

            Console.ReadLine();
        }

        static unsafe void CheckResponses(
            Byte[] buf,
            Int32 numBytes,
            out Int32 restartOffset,
            out Int32 numProcessedResponses,
            out Int64 numProcessedBodyBytes,
            out Int64 outChecksum) {

            Int32 numUnprocessedBytes = numBytes, offset = 0;

            numProcessedResponses = 0;
            numProcessedBodyBytes = 0;
            restartOffset = 0;
            outChecksum = 0;

            fixed (Byte* p = buf)
            {
                while (numUnprocessedBytes > 0) {

                    if (numUnprocessedBytes < AggregationStructSizeBytes) {

                        Buffer.BlockCopy(buf, numBytes - numUnprocessedBytes, buf, 0, numUnprocessedBytes);
                        restartOffset = numUnprocessedBytes;
                        return;
                    }

                    AggregationStruct* ags = (AggregationStruct*) (p + offset);
                    if (ags->port_number_ != UserPort)
                        throw new ArgumentOutOfRangeException();

                    if (numUnprocessedBytes < (AggregationStructSizeBytes + ags->size_bytes_)) {

                        Buffer.BlockCopy(buf, numBytes - numUnprocessedBytes, buf, 0, numUnprocessedBytes);
                        restartOffset = numUnprocessedBytes;
                        return;
                    }

                    outChecksum += ags->unique_aggr_index_;
                    numProcessedBodyBytes += ags->size_bytes_;
                    numProcessedResponses++;

                    numUnprocessedBytes -= AggregationStructSizeBytes + ags->size_bytes_;

                    offset += AggregationStructSizeBytes + ags->size_bytes_;
                }
            }
        }

        static unsafe void Worker(Int32 workerId, WorkerSettings ws) {
            Socket aggrTcpClient_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

#if FAST_LOOPBACK
            const int SIO_LOOPBACK_FAST_PATH = (-1744830448);
            Byte[] OptionInValue = BitConverter.GetBytes(1);

            aggrTcpClient_.IOControl(
                SIO_LOOPBACK_FAST_PATH,
                OptionInValue,
                null);
#endif
            
            aggrTcpClient_.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1 << 19);
            aggrTcpClient_.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1 << 19);
            
            aggrTcpClient_.Connect(ServerIp, AggregationPort);

            AggregationStruct agsOrig = new AggregationStruct() {
                port_number_ = UserPort
            };

            Byte[] sendBuf = new Byte[SendBufSize],
                recvBuf = new Byte[RecvBufSize];

            fixed (Byte* p = sendBuf) {
                *p = (Byte)AggregationMessageTypes.AGGR_CREATE_SOCKET;
                *(AggregationStruct*)(p + 1) = agsOrig;
            }

            aggrTcpClient_.Send(sendBuf, AggregationStructSizeBytes + 1, SocketFlags.None);
            Int32 numRecvBytes = aggrTcpClient_.Receive(recvBuf);

            if (numRecvBytes != AggregationStructSizeBytes)
                throw new ArgumentOutOfRangeException();

            fixed (Byte* p = recvBuf) {
                agsOrig = *(AggregationStruct*)p;
            }

            String httpRequest = "POST /echotest HTTP/1.1\r\nContent-Length: " + ws.NumBodyCharacters + "\r\n\r\n";
            
            String body = "";
            for (Int32 i = 0; i < ws.NumBodyCharacters; i++)
                body += "A";

            httpRequest += body;

            Byte[] httpRequestBytes = Encoding.ASCII.GetBytes(httpRequest);

            Int64 origChecksum = 0;
            Int32 offset = 0;
            fixed (Byte* p = sendBuf) {

                for (Int32 i = 0; i < NumRequestsInSingleSend; i++) {

#if !FAKE_SERVER

                    *(p + offset) = (Byte)AggregationMessageTypes.AGGR_DATA;
                    offset++;

#endif

                    AggregationStruct a = agsOrig;
                    a.unique_aggr_index_ = i;
                    a.size_bytes_ = httpRequestBytes.Length;

                    origChecksum += a.unique_aggr_index_;

                    *(AggregationStruct*)(p + offset) = a;

                    Marshal.Copy(httpRequestBytes, 0, new IntPtr(p + offset + AggregationStructSizeBytes), httpRequestBytes.Length);
                    offset += AggregationStructSizeBytes + httpRequestBytes.Length;
                }
            }

            origChecksum *= (ws.NumRequestsToSend / NumRequestsInSingleSend);

            Int32 numBytesToSend = offset;
            Stopwatch time = new Stopwatch();
            time.Start();

            Int32 totalNumResponses = 0;
            Int64 totalNumBodyBytes = 0;
            Int64 totalChecksum = 0;
            Int32 restartOffset = 0;
            Int32 numSentRequests = 0;

            while (totalNumResponses < ws.NumRequestsToSend) {

                if (numSentRequests < ws.NumRequestsToSend) {
                    aggrTcpClient_.Send(sendBuf, numBytesToSend, SocketFlags.None);
                    numSentRequests += NumRequestsInSingleSend;
                }

                numRecvBytes = aggrTcpClient_.Receive(recvBuf, restartOffset, recvBuf.Length - restartOffset, SocketFlags.None);
                numRecvBytes += restartOffset;

                Int64 numBodyBytes = 0;
                Int32 numResponses = 0;
                Int64 checksum = 0;

                CheckResponses(recvBuf, numRecvBytes, out restartOffset, out numResponses, out numBodyBytes, out checksum);

                totalNumResponses += numResponses;
                totalNumBodyBytes += numBodyBytes;
                totalChecksum += checksum;
            }

            time.Stop();

            if (totalChecksum != origChecksum)
                throw new Exception("Wrong checksums!");

            aggrTcpClient_.Disconnect(false);

            Int32 workerRPS = (Int32) (ws.NumRequestsToSend / (time.ElapsedMilliseconds / 1000.0));
            ws.WorkersRPS[workerId] = workerRPS;

            lock (ws.PrintLock) {
                Console.WriteLine(String.Format("Worker {0}: Took time {1} ms for {2} requests (with {3} responses), meaning total RPS {4}.",
                    workerId,
                    time.ElapsedMilliseconds,
                    ws.NumRequestsToSend,
                    totalNumResponses,
                    workerRPS));
            }

            ws.CountdownEvent.Signal();
        }
    }
}
