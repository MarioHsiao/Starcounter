using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpClientCs {

    class UdpTest {

        class WorkerSettings {
            public CountdownEvent WaitForAllWorkersEvent;
            public Int32[] WorkersRPS;
            public Int32[] WorkerExitCodes;
            public Int32 NumEchoes;
        };

        static void WorkerLoop(Int32 workerId, WorkerSettings settings) {

            try {

                String echoString = "Here is my loooooooong echo string!";

                // Sends a message to the host to which you have connected.
                Byte[] sendBytes = Encoding.ASCII.GetBytes(echoString);

                //IPEndPoint object will allow us to read datagrams sent from any source.
                EndPoint returnedEndpoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                Byte[] recvData = new Byte[2048];
                IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8787);

                // This constructor arbitrarily assigns the local port number.
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                for (Int32 i = 0; i < settings.NumEchoes; i++) {

                    s.SendTo(sendBytes, sendBytes.Length, SocketFlags.None, serverEndpoint);

                    // Blocks until a message returns on this socket from a remote host.
                    Int32 numRecvBytes = s.ReceiveFrom(recvData, ref returnedEndpoint);
                    String returnData = Encoding.ASCII.GetString(recvData, 0, numRecvBytes);

                    // Checking echo correctness.
                    if (returnData != echoString) {
                        throw new ArgumentOutOfRangeException("Wrong UDP echo received: " + returnData);
                    }

                    settings.WorkersRPS[workerId]++;
                }

                // Uses the IPEndPoint object to determine which of these two hosts responded.
                s.Close();

            } catch (Exception e) {

                lock (settings) {
                    Console.WriteLine(e.ToString());
                }

                settings.WorkerExitCodes[workerId] = 1;

            } finally {

                settings.WaitForAllWorkersEvent.Signal();
            }
        }

        static Int32 Main(string[] args) {

            Int32 numWorkers = 2;

            if (args.Length > 0) {
                numWorkers = Int32.Parse(args[0]);
            }

            Console.WriteLine("Starting UDP echo test with workers: " + numWorkers);

            WorkerSettings ws = new WorkerSettings() {
                WaitForAllWorkersEvent = new CountdownEvent(numWorkers),
                WorkersRPS = new Int32[numWorkers],
                WorkerExitCodes = new Int32[numWorkers],
                NumEchoes = 500000
            };

            Stopwatch timer = new Stopwatch();
            timer.Start();

            for (Int32 i = 0; i < numWorkers; i++) {
                Int32 workerId = i;
                ThreadStart threadDelegate = new ThreadStart(() => WorkerLoop(workerId, ws));
                Thread newThread = new Thread(threadDelegate);
                newThread.Start();
            }

            // Printing status info.
            while (ws.WaitForAllWorkersEvent.CurrentCount > 0) {

                lock (ws) {
                    for (Int32 i = 0; i < numWorkers; i++) {
                        Console.WriteLine(String.Format("[{0}] count: {1}", i, ws.WorkersRPS[i]));
                    }
                }

                Thread.Sleep(1000);
            }

            ws.WaitForAllWorkersEvent.Wait();
            timer.Stop();

            // Checking if every worker succeeded.
            for (Int32 i = 0; i < numWorkers; i++) {
                if (ws.WorkerExitCodes[i] != 0) {
                    return ws.WorkerExitCodes[i];
                }
            }

            Int32 totalRPS = 0;
            Int32 totalEchoes = 0;

            for (Int32 i = 0; i < numWorkers; i++) {
                totalRPS += ws.WorkersRPS[i];
                totalEchoes += ws.NumEchoes;
            }

            totalRPS = (Int32) ((totalRPS * 1000.0) / timer.ElapsedMilliseconds);

            Console.WriteLine(String.Format("Summary: total RPS is {0} (total number of echoes is {1}), total time {2} ms.",
                totalRPS, totalEchoes, timer.ElapsedMilliseconds));

            return 0;
        }
    }
}
