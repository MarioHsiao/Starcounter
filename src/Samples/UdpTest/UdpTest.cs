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
            public UInt16 DatagramSize;
            public String ServerIp;
            public UInt16 ServerPort;
        };

        static void WorkerLoop(Int32 workerId, WorkerSettings settings) {

            try {

                // Constructing random data array.
                Random rand = new Random();

                Byte[] sendBytes = new Byte[settings.DatagramSize];
                for (Int32 i = 0; i < sendBytes.Length; i++) {
                    sendBytes[i] = (Byte) (48 + rand.Next(9));
                }

                //IPEndPoint object will allow us to read datagrams sent from any source.
                EndPoint returnedEndpoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                Byte[] recvData = new Byte[100000];
                IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(settings.ServerIp), settings.ServerPort);

                // This constructor arbitrarily assigns the local port number.
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                for (Int32 i = 0; i < settings.NumEchoes; i++) {

                    s.SendTo(sendBytes, sendBytes.Length, SocketFlags.None, serverEndpoint);

                    // Blocks until a message returns on this socket from a remote host.
                    Int32 numRecvBytes = s.ReceiveFrom(recvData, ref returnedEndpoint);
                    if (numRecvBytes != sendBytes.Length) {
                        throw new ArgumentOutOfRangeException("Wrong size of UDP echo received: " + numRecvBytes);
                    }

                    // Checking every byte.
                    for (Int32 k = 0; k < sendBytes.Length; k++) {

                        // Checking echo correctness.
                        if (sendBytes[k] != recvData[k]) {
                            throw new ArgumentOutOfRangeException("Wrong UDP echo data received.");
                        }
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
            UInt16 datagramSize = 1000;
            Int32 numEchoes = 100000;
            String serverIp = "127.0.0.1";
            UInt16 serverPort = 8787;

            foreach (String arg in args) {
                if (arg.StartsWith("--DatagramSize=")) {
                    datagramSize = UInt16.Parse(arg.Substring("--DatagramSize=".Length));
                } else if (arg.StartsWith("--NumWorkers=")) {
                    numWorkers = Int32.Parse(arg.Substring("--NumWorkers=".Length));
                } else if (arg.StartsWith("--NumEchoes=")) {
                    numEchoes = Int32.Parse(arg.Substring("--NumEchoes=".Length));
                } else if (arg.StartsWith("--ServerPort=")) {
                    serverPort = UInt16.Parse(arg.Substring("--ServerPort=".Length));
                } else if (arg.StartsWith("--ServerIp=")) {
                    serverIp = arg.Substring("--ServerIp=".Length);
                }
            }

            Console.WriteLine("Usage: UdpTest.exe --ServerIp={0} --ServerPort={1} --NumWorkers={2} --NumEchoes={3} --DatagramSize={4}",
                serverIp,
                serverPort,
                numWorkers,
                numEchoes,
                datagramSize);

            Console.WriteLine();

            WorkerSettings settings = new WorkerSettings() {
                WaitForAllWorkersEvent = new CountdownEvent(numWorkers),
                WorkersRPS = new Int32[numWorkers],
                WorkerExitCodes = new Int32[numWorkers],
                NumEchoes = numEchoes,
                DatagramSize = datagramSize,
                ServerIp = serverIp,
                ServerPort = serverPort
            };

            Console.WriteLine("Starting UDP echo test with {0} workers, {1} echoes of size {2}.",
                numWorkers, settings.NumEchoes, settings.DatagramSize);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            for (Int32 i = 0; i < numWorkers; i++) {
                Int32 workerId = i;
                ThreadStart threadDelegate = new ThreadStart(() => WorkerLoop(workerId, settings));
                Thread newThread = new Thread(threadDelegate);
                newThread.Start();
            }

            Int32 timeoutCounter = 100;

            // Looping until worker finish events are set.
            while (settings.WaitForAllWorkersEvent.CurrentCount > 0) {

                lock (settings) {
                    for (Int32 i = 0; i < numWorkers; i++) {
                        Console.WriteLine(String.Format("[{0}] count: {1}", i, settings.WorkersRPS[i]));
                    }
                }

                Thread.Sleep(1000);

                timeoutCounter--;
                if (0 == timeoutCounter) {
                    throw new Exception("Test timed out!");
                }
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
