using Starcounter;
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
            public String ServerIp;
            public UInt16 ServerPort;
        };

        static void WorkerLoop(Int32 workerId, WorkerSettings settings) {

            try {

                String echoString = "Here is my loooooooong echo string!";

                // Sends a message to the host to which you have connected.
                Byte[] sendBytes = Encoding.ASCII.GetBytes(echoString);

                //IPEndPoint object will allow us to read datagrams sent from any source.
                EndPoint returnedEndpoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                Byte[] recvData = new Byte[2048];
                IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(settings.ServerIp), settings.ServerPort);

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

            Int32 numWorkers = 3;

            if (args.Length > 0) {
                numWorkers = Int32.Parse(args[0]);
            }

            WorkerSettings settings = new WorkerSettings() {
                WaitForAllWorkersEvent = new CountdownEvent(numWorkers),
                WorkersRPS = new Int32[numWorkers],
                WorkerExitCodes = new Int32[numWorkers],
                NumEchoes = 500000,
                ServerIp = "127.0.0.1",
                ServerPort = 8787
            };

            // Waiting until host is available.
            Boolean hostIsReady = false;
            Console.Write("Waiting for the host to be ready");

            Response resp;

            for (Int32 i = 0; i < 10; i++) {

                resp = X.POST("http://" + settings.ServerIp + ":8080/echotest", "Test!", null, 5000);

                if ((200 == resp.StatusCode) && ("Test!" == resp.Body)) {

                    hostIsReady = true;
                    break;
                }

                Thread.Sleep(3000);
                Console.Write(".");
            }

            Console.WriteLine();

            if (!hostIsReady)
                throw new Exception("Host is not ready by some reason!");

            Console.WriteLine("Starting UDP echo test with workers: " + numWorkers);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            for (Int32 i = 0; i < numWorkers; i++) {
                Int32 workerId = i;
                ThreadStart threadDelegate = new ThreadStart(() => WorkerLoop(workerId, settings));
                Thread newThread = new Thread(threadDelegate);
                newThread.Start();
            }

            // Printing status info.
            while (settings.WaitForAllWorkersEvent.CurrentCount > 0) {

                lock (settings) {
                    for (Int32 i = 0; i < numWorkers; i++) {
                        Console.WriteLine(String.Format("[{0}] count: {1}", i, settings.WorkersRPS[i]));
                    }
                }

                Thread.Sleep(1000);
            }

            settings.WaitForAllWorkersEvent.Wait();
            timer.Stop();

            // Checking if every worker succeeded.
            for (Int32 i = 0; i < numWorkers; i++) {
                if (settings.WorkerExitCodes[i] != 0) {
                    return settings.WorkerExitCodes[i];
                }
            }

            Int32 totalRPS = 0;
            Int32 totalEchoes = 0;

            for (Int32 i = 0; i < numWorkers; i++) {
                totalRPS += settings.WorkersRPS[i];
                totalEchoes += settings.NumEchoes;
            }

            totalRPS = (Int32) ((totalRPS * 1000.0) / timer.ElapsedMilliseconds);

            Console.WriteLine(String.Format("Summary: total RPS is {0} (total number of echoes is {1}), total time {2} ms.",
                totalRPS, totalEchoes, timer.ElapsedMilliseconds));

            return 0;
        }
    }
}
