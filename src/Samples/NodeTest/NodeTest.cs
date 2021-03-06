﻿//#define FASTEST_POSSIBLE
#define FILL_RANDOMLY
#if FASTEST_POSSIBLE
#undef FILL_RANDOMLY
#endif

using Starcounter;
using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Starcounter.TestFramework;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.IO;

namespace NodeTest
{
    class Settings
    {
        public enum AsyncModes
        {
            ModeSync,
            ModeAsync,
            ModeRandom
        }

        public enum ProtocolTypes
        {
            ProtocolHttpV1,
            ProtocolWebSockets,
            ProtocolRawPort
        }

        public const String ServerNodeTestHttpRelativeUri = "/echotest";

        public const String ServerNodeTestWsRelativeUri = "/echotestws";

        public String ServerIp = "127.0.0.1";

        public UInt16 ServerPort = 8080;

        public UInt16 ServerRawPort = 8585;

        public static String CompleteHttpUri;

        public static String ResetCountersUri;

        public static String HttpCountersUri;

        public static String RawPortCountersUri;

        public static String CompleteWebSocketUri;

        public static String WebSocketCountersUri;

        public Int32 NumWorkers = 3;

        public Int32 MinEchoBytes = 1;

        public Int32 MaxEchoBytes = 10000;

        public Int32 NumEchoesPerWorker = 10000;

        public Int32 NumEchoesPerConnection = 10;

        public Int32 NumSecondsToWait = 5000;

        public AsyncModes AsyncMode = AsyncModes.ModeRandom;

        public ProtocolTypes ProtocolType = ProtocolTypes.ProtocolHttpV1;

        public Boolean UseAggregation = false;

        public Boolean ConsoleDiag = false;

        public Int64 numEchoesAllWorkers_ = 0;

        public Int64 NumEchoesAllWorkers {
            get { return numEchoesAllWorkers_; }
        }

        static Int64 totalBytesSent_ = 0;

        /// <summary>
        /// Adds to total number of bytes that are sent.
        /// </summary>
        /// <param name="numBytes"></param>
        public static void AddToTotalNumberOfTestBytes(Int32 numBytes) {
            Interlocked.Add(ref totalBytesSent_, numBytes);
        }

        public void Init(string[] args)
        {
            foreach (String arg in args)
            {
                if (arg.StartsWith("-ServerIp="))
                {
                    ServerIp = arg.Substring("-ServerIp=".Length);
                }
                else if (arg.StartsWith("-ServerPort="))
                {
                    ServerPort = UInt16.Parse(arg.Substring("-ServerPort=".Length));
                }
                else if (arg.StartsWith("-ServerRawPort="))
                {
                    ServerRawPort = UInt16.Parse(arg.Substring("-ServerRawPort=".Length));
                }
                else if (arg.StartsWith("-ProtocolType="))
                {
                    String protocolTypeParam = arg.Substring("-ProtocolType=".Length);

                    if (protocolTypeParam == "ProtocolHttpV1")
                        ProtocolType = ProtocolTypes.ProtocolHttpV1;
                    else if (protocolTypeParam == "ProtocolWebSockets")
                        ProtocolType = ProtocolTypes.ProtocolWebSockets;
                    else if (protocolTypeParam == "ProtocolRawPort")
                        ProtocolType = ProtocolTypes.ProtocolRawPort;
                }
                else if (arg.StartsWith("-NumWorkers="))
                {
                    NumWorkers = Int32.Parse(arg.Substring("-NumWorkers=".Length));
                }
                else if (arg.StartsWith("-MinEchoBytes="))
                {
                    MinEchoBytes = Int32.Parse(arg.Substring("-MinEchoBytes=".Length));
                }
                else if (arg.StartsWith("-MaxEchoBytes="))
                {
                    MaxEchoBytes = Int32.Parse(arg.Substring("-MaxEchoBytes=".Length));
                }
                else if (arg.StartsWith("-NumEchoesPerWorker="))
                {
                    NumEchoesPerWorker = Int32.Parse(arg.Substring("-NumEchoesPerWorker=".Length));
                }
                else if (arg.StartsWith("-NumEchoesPerConnection="))
                {
                    NumEchoesPerConnection = Int32.Parse(arg.Substring("-NumEchoesPerConnection=".Length));
                }
                else if (arg.StartsWith("-NumSecondsToWait="))
                {
                    NumSecondsToWait = Int32.Parse(arg.Substring("-NumSecondsToWait=".Length));
                }
                else if (arg.StartsWith("-AsyncMode="))
                {
                    String asyncParam = arg.Substring("-AsyncMode=".Length);

                    if (asyncParam == "ModeSync")
                        AsyncMode = AsyncModes.ModeSync;
                    else if (asyncParam == "ModeAsync")
                        AsyncMode = AsyncModes.ModeAsync;
                    else if (asyncParam == "ModeRandom")
                        AsyncMode = AsyncModes.ModeRandom;
                }
                else if (arg.StartsWith("-UseAggregation="))
                {
                    UseAggregation = Boolean.Parse(arg.Substring("-UseAggregation=".Length));
                }
                else if (arg.StartsWith("-ConsoleDiag="))
                {
                    ConsoleDiag = Boolean.Parse(arg.Substring("-ConsoleDiag=".Length));
                }
            }

            if (ProtocolTypes.ProtocolWebSockets == ProtocolType) {

                if ((NumEchoesPerWorker % NumEchoesPerConnection) != 0) {
                    throw new ArgumentException("NumEchoesPerWorker is not divisible by NumEchoesPerConnection!");
                }

                if ((AsyncModes.ModeSync != AsyncMode) || (true == UseAggregation)) {
                    throw new ArgumentException("WebSockets support only Sync mode (and no aggregation)!");
                }
            } else if (ProtocolTypes.ProtocolRawPort == ProtocolType) {

                if ((NumEchoesPerWorker % NumEchoesPerConnection) != 0) {
                    throw new ArgumentException("NumEchoesPerWorker is not divisible by NumEchoesPerConnection!");
                }

                if ((AsyncModes.ModeSync != AsyncMode) || (true == UseAggregation)) {
                    throw new ArgumentException("Raw ports socket supports only Sync mode (and no aggregation)!");
                }
            }

            numEchoesAllWorkers_ = NumEchoesPerWorker * NumWorkers;

            ResetCountersUri = "http://" + ServerIp + ":" + ServerPort + "/resetcounters";
            CompleteHttpUri = "http://" + ServerIp + ":" + ServerPort + ServerNodeTestHttpRelativeUri;
            CompleteWebSocketUri = "ws://" + ServerIp + ":" + ServerPort + ServerNodeTestWsRelativeUri;
            HttpCountersUri = "http://" + ServerIp + ":" + ServerPort + "/httpcounters";
            RawPortCountersUri = "http://" + ServerIp + ":" + ServerPort + "/rawportcounters";
            WebSocketCountersUri = "http://" + ServerIp + ":" + ServerPort + "/wscounters";
        }

        /// <summary>
        /// Checking for correct counters from server side.
        /// </summary>
        public void CheckServerCounters() {

            switch (ProtocolType) {

                case ProtocolTypes.ProtocolWebSockets: {

                    // NOTE: Need to sleep to receive correct statistics.
                    Thread.Sleep(1000);

                    String retrieved = Http.GET<String>(WebSocketCountersUri);

                    String expected = String.Format("WebSockets counters: handshakes={0}, echoes received={1}, disconnects={2}",
                        NumEchoesAllWorkers / NumEchoesPerConnection,
                        NumEchoesAllWorkers,
                        NumEchoesAllWorkers / NumEchoesPerConnection);

                    if (retrieved != expected)
                        throw new Exception(String.Format("Wrong expected counters data. Expected: {0}, Received: {1}", expected, retrieved));

                    break;
                }
                
                case ProtocolTypes.ProtocolHttpV1: {

                    String retrieved = Http.GET<String>(HttpCountersUri);

                    String expected = String.Format("Http counters: echoes received={0}.", NumEchoesAllWorkers);

                    if (retrieved != expected)
                        throw new Exception(String.Format("Wrong expected counters data. Expected: {0}, Received: {1}", expected, retrieved));

                    break;
                }

                case ProtocolTypes.ProtocolRawPort: {

                    String retrieved = Http.GET<String>(RawPortCountersUri);

                    String expected = String.Format("Raw port counters: bytes received={0}, disconnects={1}.", totalBytesSent_, NumEchoesAllWorkers / NumEchoesPerConnection);

                    if (retrieved != expected)
                        throw new Exception(String.Format("Wrong expected counters data. Expected: {0}, Received: {1}", expected, retrieved));

                    break;
                }
                

                default:
                    throw new ArgumentException();
            }
        }
    }

    class NodeTestInstance
    {
        UInt64 unique_id_;

        Int32 num_echo_bytes_;

        Boolean async_;

        Boolean useNodeX_;

#if !FASTEST_POSSIBLE
        Byte[] correct_hash_;
#endif

        Byte[] body_bytes_;

        Byte[] resp_bytes_;

        Settings settings_;

        Worker worker_;

        // Initializes new test instance.
        public void Init(
            Settings settings,
            Worker worker,
            UInt64 unique_id,
            Boolean async,
            Int32 num_echo_bytes)
        {
            settings_ = settings;
            worker_ = worker;
            unique_id_ = unique_id;
            async_ = async;

            if (settings.UseAggregation)
            {
                useNodeX_ = false;
                async_ = true;
            }
            else
            {
                useNodeX_ = ((num_echo_bytes_ % 2) == 0) ? true : false;
            }

            num_echo_bytes_ = num_echo_bytes;

            body_bytes_ = new Byte[num_echo_bytes_];
            resp_bytes_ = new Byte[num_echo_bytes_];

#if FILL_RANDOMLY
            for (Int32 i = 0; i < num_echo_bytes_; i++)
                body_bytes_[i] = (Byte)(worker_.Rand.Next(48, 57));
#else
            for (Int32 i = 0; i < num_echo_bytes_; i++)
                body_bytes_[i] = (Byte)('5');
#endif

#if !FASTEST_POSSIBLE
            // Calculating SHA1 hash.
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            correct_hash_ = sha1.ComputeHash(body_bytes_);
#endif
        }

        /// <summary>
        /// Performs a session of WebSocket echoes.
        /// </summary>
        /// <param name="bodyBytes"></param>
        /// <param name="respBytes"></param>
        /// <returns></returns>
        public async Task PerformSyncWebSocketEcho(Byte[] bodyBytes, Byte[] respBytes)
        {
            using (ClientWebSocket ws = new ClientWebSocket())
            {
                // Establishing WebSockets connection.
                Uri serverUri = new Uri(Settings.CompleteWebSocketUri);
                await ws.ConnectAsync(serverUri, CancellationToken.None);

                // Within one connection performing several echoes.
                Int32 numRuns = settings_.NumEchoesPerConnection;

                for (Int32 n = 0; n < numRuns; n++)
                {
                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(bodyBytes);

                    // Sending data in preferred format: text or binary.
                    await ws.SendAsync(
                        bytesToSend, WebSocketMessageType.Binary,
                        true, CancellationToken.None);

                    ArraySegment<byte> bytesReceived = new ArraySegment<byte>(respBytes, 0, respBytes.Length);

                    WebSocketReceiveResult result = await ws.ReceiveAsync(bytesReceived, CancellationToken.None);

                    // Checking if need to continue accumulation.
                    if (result.Count < bodyBytes.Length)
                    {
                        Int32 totalReceived = result.Count;

                        // Accumulating until all data is received.
                        while (totalReceived < bodyBytes.Length)
                        {
                            bytesReceived = new ArraySegment<byte>(respBytes, totalReceived, respBytes.Length - totalReceived);

                            result = await ws.ReceiveAsync(bytesReceived, CancellationToken.None);

                            totalReceived += result.Count;
                        }
                    }

                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Just closing.", CancellationToken.None);

                    // Creating response from received byte array.
                    Response resp = new Response { BodyBytes = resp_bytes_ };

                    // Checking the response and generating an error if a problem found.
                    if (!CheckResponse(resp))
                        throw new Exception("Incorrect WebSocket response of length: " + resp_bytes_.Length);

                    // Checking if all echoes are processed.
                    if (worker_.IsAllEchoesReceived())
                        return;
                }
            }
        }

        /// <summary>
        /// Performs a session of WebSocket4Net echoes.
        /// </summary>
        /// <param name="bodyBytes"></param>
        /// <param name="respBytes"></param>
        /// <returns></returns>
        public void PerformSyncWebSocket4NetEcho(Byte[] bodyBytes, Byte[] respBytes)
        {
            WebSocket4Net.WebSocket ws = new WebSocket4Net.WebSocket(Settings.CompleteWebSocketUri);

            Int32 numRuns = settings_.NumEchoesPerConnection;

            AutoResetEvent allEchoesReceivedEvent = new AutoResetEvent(false);

            ws.DataReceived += (s, e) => 
            {
                // Checking that echo size is correct.
                if (e.Data.Length != bodyBytes.Length)
                {
                    Console.WriteLine("Incorrect WebSocket response size: " + e.Data.Length + ", should be " + bodyBytes.Length);
                    Console.WriteLine("Received echo body: " + Encoding.UTF8.GetString(e.Data));

#if !FILL_RANDOMLY
                    for (Int32 i = 0; i < e.Data.Length; i++)
                    {
                        if (e.Data[i] != '1')
                        {
                            Console.WriteLine("Response contains illegal symbols: " + e.Data[i]);
                            break;
                        }
                    }
#endif

                    NodeTest.WorkersMonitor.FailTest();
                    return;
                }

                e.Data.CopyTo(respBytes, 0);

                // Creating response from received byte array.
                Response resp = new Response { BodyBytes = respBytes };

                // Checking the response and generating an error if a problem found.
                if (!CheckResponse(resp))
                {
                    Console.WriteLine("Incorrect WebSocket response of length: " + respBytes.Length);
                    NodeTest.WorkersMonitor.FailTest();
                    return;
                }

                // Checking if all echoes are processed.
                if (worker_.IsAllEchoesReceived()) {
                    allEchoesReceivedEvent.Set();
                    return;
                }

                // Sending data again if number of runs is not exhausted.
                numRuns--;
                if (numRuns <= 0)
                    allEchoesReceivedEvent.Set();
                else
                    ws.Send(bodyBytes, 0, bodyBytes.Length);
            };

            ws.Opened += (s, e) => 
            {
                ws.Send(bodyBytes, 0, bodyBytes.Length);
            };

            ws.Error += (s, e) =>
            {
                Console.WriteLine(e.Exception.ToString());
                NodeTest.WorkersMonitor.FailTest();
            };

            Boolean closedGracefully = false;

            ws.Closed += (s, e) =>
            {
                if (!closedGracefully)
                    throw new Exception("WebSocket connection was closed unexpectedly!");

                worker_.IncrementNumClosedConnections();
            };

            // Starting the handshake.
            ws.Open();

            // Waiting for all tests to finish.
            if (!allEchoesReceivedEvent.WaitOne(10000))
            {
                throw new Exception("Failed to get WebSocket response in time!");
            }

            // If we are here that means the connection is closed properly.
            closedGracefully = true;

            ws.Close();
        }

        /// <summary>
        /// Performs a session of raw port echoes.
        /// </summary>
        /// <param name="bodyBytes"></param>
        /// <param name="respBytes"></param>
        /// <returns></returns>
        public void PerformSyncRawPortEcho(Byte[] bodyBytes, Byte[] respBytes) {

            TcpClient tcpClientObj = new TcpClient(settings_.ServerIp, settings_.ServerRawPort);
            Socket socketObj = tcpClientObj.Client;

            try {

                Int32 numRuns = settings_.NumEchoesPerConnection;

                for (Int32 i = 0; i < numRuns; i++) {

                    Int32 bytesSent = socketObj.Send(bodyBytes, 0, bodyBytes.Length, SocketFlags.None);

                    Settings.AddToTotalNumberOfTestBytes(bodyBytes.Length);

                    Int32 totalRecievedBytes = 0;

                    // Looping until we get everything.
                    while (true) {

                        // Reading the response into predefined buffer.
                        Int32 curRecievedBytes = socketObj.Receive(respBytes, totalRecievedBytes, respBytes.Length - totalRecievedBytes, SocketFlags.None);

                        totalRecievedBytes += curRecievedBytes;

                        if (curRecievedBytes <= 0) {
                            throw new IOException("Remote host closed the connection.");
                        } else if (totalRecievedBytes == respBytes.Length) {
                            break;
                        }
                    }

                    // Creating response from received byte array.
                    Response resp = new Response { BodyBytes = respBytes };

                    // Checking the response and generating an error if a problem found.
                    if (!CheckResponse(resp)) {
                        Console.WriteLine("Incorrect raw socket response of length: " + respBytes.Length);
                        NodeTest.WorkersMonitor.FailTest();
                        return;
                    }

                    // Checking if all echoes are processed.
                    if (worker_.IsAllEchoesReceived()) {
                        break;
                    }
                }

            } finally {
                tcpClientObj.Close();
                socketObj.Close();
            }
        }

        // Sends data, gets the response, and checks its correctness.
        public Boolean PerformTest(Node node)
        {
            if (!async_)
            {
                switch (settings_.ProtocolType)
                {
                    case Settings.ProtocolTypes.ProtocolHttpV1:
                    {
                        if (useNodeX_)
                        {
                            Response resp = Http.POST(Settings.CompleteHttpUri, body_bytes_, null);
                            return CheckResponse(resp);
                        }
                        else
                        {
                            Response resp = node.POST(Settings.ServerNodeTestHttpRelativeUri, body_bytes_, null);
                            return CheckResponse(resp);
                        }
                    }

                    case Settings.ProtocolTypes.ProtocolWebSockets:
                    {
                        Boolean runNativeDotNetWebSockets = false;
                        if (runNativeDotNetWebSockets)
                        {
                            Task t = PerformSyncWebSocketEcho(body_bytes_, resp_bytes_);
                            t.Wait();
                        }
                        else
                        {
                            PerformSyncWebSocket4NetEcho(body_bytes_, resp_bytes_);
                        }

                        return true;
                    }

                    case Settings.ProtocolTypes.ProtocolRawPort:
                    {
                        PerformSyncRawPortEcho(body_bytes_, resp_bytes_);

                        return true;
                    }

                    default:
                        throw new ArgumentException("Unknown protocol type!");
                }
            }
            else
            {
                try
                {
                    if (useNodeX_) {
                        Http.POST(Settings.CompleteHttpUri, body_bytes_, null, (Response resp) => {
                            CheckResponse(resp);
                        });
                    } else {
                        node.POST(Settings.ServerNodeTestHttpRelativeUri, body_bytes_, null, (Response resp) => {
                            CheckResponse(resp);
                        });
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.ToString());
                    throw exc;
                }

                return true;
            }
        }

        // Checks response correctness.
        Boolean CheckResponse(Response resp)
        {
#if FASTEST_POSSIBLE

            if (NodeTest.WorkersMonitor.IncrementNumFinishedTests())
                allWorkerEchoesReceived = true;

            return true;
#else

            if (settings_.ConsoleDiag)
                Console.WriteLine(worker_.Id + ": echoed: " + num_echo_bytes_ + " bytes");

            Byte[] resp_body = resp.BodyBytes;

            // Checking first characters.
            for (Int32 i = 0; i < resp_body.Length; i++)
            {
                if (resp_body[i] != body_bytes_[i])
                {
                    Console.WriteLine("Completely wrong response (first 16 characters are different). Received length: " +
                        resp_body.Length + " where correct: " + body_bytes_.Length);

                    Console.WriteLine("Received echo body: " + Encoding.UTF8.GetString(resp_body));

                    NodeTest.WorkersMonitor.FailTest();
                    return false;
                }

                if (i >= 16)
                    break;
            }

            // Checking if response length is correct.
            if (resp_body.Length != num_echo_bytes_)
            {
                Console.WriteLine("Wrong echo size! Correct echo size: " + num_echo_bytes_ + ", wrong: " + resp_body.Length + " [Async=" + async_ + "]");
                Console.WriteLine("Received echo body: " + Encoding.UTF8.GetString(resp_body));

                NodeTest.WorkersMonitor.FailTest();
                return false;
            }

            try
            {
                // Calculating SHA1 hash.
                SHA1 sha1 = new SHA1CryptoServiceProvider();
                Byte[] received_hash_ = sha1.ComputeHash(resp_body);

                // Checking that hash is the same.
                for (Int32 i = 0; i < received_hash_.Length; i++)
                {
                    if (received_hash_[i] != correct_hash_[i])
                    {
                        for (Int32 k = 0; k < resp_body.Length; k++)
                        {
                            if (resp_body[k] != body_bytes_[k])
                            {
                                //Debugger.Launch();
                            }
                        }

                        Console.WriteLine("Received echo body: " + Encoding.UTF8.GetString(resp_body));
                        Console.WriteLine("Wrong echo contents! Correct echo size: " + num_echo_bytes_ + " [Async=" + async_ + "]");
                        NodeTest.WorkersMonitor.FailTest();
                        return false;
                    }
                }

                // Incrementing number of finished tests.
                worker_.IncrementNumFinishedEchoes();
                NodeTest.WorkersMonitor.IncrementNumFinishedTests();                    
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                throw exc;
            }

            return true;
#endif
        }
    }

    class Worker
    {
        public Int32 Id;

        public Random Rand;

        Settings settings_;

        Node worker_node_;

        Int32 numFinishedEchoes_;

        Int32 numClosedConnections_;

        public void IncrementNumFinishedEchoes() {
            numFinishedEchoes_++;
        }

        public void IncrementNumClosedConnections() {
            numClosedConnections_++;
        }

        public Boolean IsAllEchoesReceived() {

            if (numFinishedEchoes_ >= settings_.NumEchoesPerWorker)
                return true;

            return false;
        }

        public Node WorkerNode
        {
            get { return worker_node_; }
        }

        public void Init(Settings settings, Int32 id)
        {
            Id = id;
            Rand = new Random(id);
            settings_ = settings;

            worker_node_ = new Node(settings_.ServerIp, settings_.ServerPort);
        }

        /// <summary>
        /// Initializes specific new test for worker.
        /// </summary>
        /// <returns></returns>
        NodeTestInstance CreateNewTest()
        {
            UInt64 id = ((UInt64)Rand.Next() << 32);
            Int32 num_echo_bytes = Rand.Next(settings_.MinEchoBytes, settings_.MaxEchoBytes);

            NodeTestInstance test = new NodeTestInstance();

            Boolean async = true;
            switch (settings_.AsyncMode)
            {
                case Settings.AsyncModes.ModeSync:
                {
                    async = false;
                    break;
                }

                case Settings.AsyncModes.ModeAsync:
                {
                    async = true;
                    break;
                }

                case Settings.AsyncModes.ModeRandom:
                {
                    if (Rand.Next(0, 2) == 0)
                        async = true;
                    else
                        async = false;

                    break;
                }
            }

            test.Init(settings_, this, id, async, num_echo_bytes);

            return test;
        }

        /// <summary>
        /// Main worker test loop.
        /// </summary>
        public void WorkerLoop()
        {
            Console.WriteLine(Id + ": test started..");

            try
            {
                for (Int32 n = 0; n < settings_.NumEchoesPerWorker; n++)
                {
                    // Checking if all echoes has been received.
                    if (IsAllEchoesReceived())
                        return;

                    NodeTestInstance test = CreateNewTest();

                    if (!test.PerformTest(worker_node_))
                        return;

                    // Checking if tests has already failed.
                    if (NodeTest.WorkersMonitor.HasTestFailed)
                        return;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(Id + ": test crashed: " + exc.ToString());

                NodeTest.WorkersMonitor.FailTest();
            }
        }
    }

    class GlobalObserver
    {
        Settings settings_;

        Int64 num_finished_tests_;

        Worker[] workers_;

        public void Init(Settings settings, Worker[] workers)
        {
            settings_ = settings;
            workers_ = workers;
        }

        /// <summary>
        /// Increment global number of finished tests.
        /// </summary>
        public Boolean IncrementNumFinishedTests()
        {
            Interlocked.Increment(ref num_finished_tests_);

            if (num_finished_tests_ == settings_.NumEchoesAllWorkers)
                return true;

            return false;
        }

        volatile Boolean all_tests_succeeded_ = true;

        /// <summary>
        /// Returns True if tests failed.
        /// </summary>
        public Boolean HasTestFailed
        {
            get { return !all_tests_succeeded_; }
        }

        /// <summary>
        /// Indicate that some test failed.
        /// </summary>
        public void FailTest()
        {
            all_tests_succeeded_ = false;
        }

        /// <summary>
        /// Wait until all tests succeed, fail or timeout.
        /// </summary>
        public Boolean MonitorState()
        {
            Int64 num_ms_passed = 0, num_ms_max = settings_.NumSecondsToWait * 1000;

            Int32 delay_counter = 0;
            Int64 prev_num_finished_tests = 0;

            // Looping until either tests succeed, fail or timeout.
            while (
                (num_finished_tests_ < settings_.NumEchoesAllWorkers) &&
                (num_ms_passed < num_ms_max) &&
                (true == all_tests_succeeded_))
            {
                Thread.Sleep(10);
                num_ms_passed += 10;

                delay_counter++;
                if (delay_counter >= 100)
                {
                    Int64 num_finished_tests = num_finished_tests_;
                    Console.WriteLine("Finished echoes: " + num_finished_tests + " out of " + settings_.NumEchoesAllWorkers + ", last second: " + (num_finished_tests - prev_num_finished_tests));
                    
                    prev_num_finished_tests = num_finished_tests;
                    delay_counter = 0;
                }
            }

            if (!all_tests_succeeded_)
            {
                Console.Error.WriteLine("Test failed: incorrect echo received.");
                return false;
            }

            if (num_ms_passed >= num_ms_max)
            {
                Console.Error.WriteLine("Test failed: took too long time.");
                FailTest();
                return false;
            }

            return true;
        }
    }

    class NodeTest
    {
        public static GlobalObserver WorkersMonitor = new GlobalObserver();

        static Int32 Main(string[] args)
        {
            //Debugger.Launch();

            try {
                Settings settings = new Settings();
                settings.Init(args);

                Console.WriteLine("Node test settings!");
                Console.WriteLine("ServerIp: " + settings.ServerIp);
                Console.WriteLine("ServerPort: " + settings.ServerPort);
                Console.WriteLine("ProtocolType: " + settings.ProtocolType);
                Console.WriteLine("NumWorkers: " + settings.NumWorkers);
                Console.WriteLine("MinEchoBytes: " + settings.MinEchoBytes);
                Console.WriteLine("MaxEchoBytes: " + settings.MaxEchoBytes);
                Console.WriteLine("NumEchoesPerWorker: " + settings.NumEchoesPerWorker);
                Console.WriteLine("NumEchoesPerConnection: " + settings.NumEchoesPerConnection);
                Console.WriteLine("NumSecondsToWait: " + settings.NumSecondsToWait);
                Console.WriteLine("AsyncMode: " + settings.AsyncMode);
                Console.WriteLine("UseAggregation: " + settings.UseAggregation);

                // Resetting the counters.
                Response resp = Http.DELETE(Settings.ResetCountersUri, (String) null, null, 5000);
                if (200 != resp.StatusCode) {
                    throw new Exception("Can't reset counters properly!");
                }

                // Starting all workers.
                Worker[] workers = new Worker[settings.NumWorkers];
                Thread[] worker_threads = new Thread[settings.NumWorkers];

                WorkersMonitor.Init(settings, workers);

                for (Int32 w = 0; w < settings.NumWorkers; w++) {
                    Int32 id = w;
                    workers[w] = new Worker();
                    workers[w].Init(settings, w);

                    worker_threads[w] = new Thread(() => { workers[id].WorkerLoop(); });
                    worker_threads[w].Start();
                }

                Stopwatch timer = new Stopwatch();
                timer.Start();

                // Waiting for all workers to succeed or fail.
                if (!WorkersMonitor.MonitorState())
                    Environment.Exit(1);

                timer.Stop();

                // Checking server counters.
                settings.CheckServerCounters();

                Console.WriteLine("Test succeeded, took ms: " + timer.ElapsedMilliseconds);

                Double echoesPerSecond = ((settings.NumWorkers * settings.NumEchoesPerWorker) * 1000.0) / timer.ElapsedMilliseconds;
                TestLogger.ReportStatistics(
                    String.Format("nodetest_{0}_workers_{1}_echo_minbytes_{2}_maxbytes_{3}__echoes_per_second",
                        settings.ProtocolType,
                        settings.NumWorkers,
                        settings.MinEchoBytes,
                        settings.MaxEchoBytes),

                    echoesPerSecond);

                Console.WriteLine("Echoes/second: " + echoesPerSecond);

                // Forcing quiting.
                Environment.Exit(0);

                // Waiting for all worker threads to finish.
                for (Int32 w = 0; w < settings.NumWorkers; w++) {
                    worker_threads[w].Join();
                }

                return 0;

            } catch (Exception exc) {
                Console.Error.WriteLine(exc.ToString());
                Environment.Exit(1);
                return 1;
            }
        }
    }
}
