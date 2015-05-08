using Starcounter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketsTestClient {

    class WebSocketsTestClient {

        static String ServerIp = "127.0.0.1";

        static UInt16 ServerPort = 8080;
         
        static Int32 NumMessagesPerWebSocket = 10000;

        static Int32 NumSockets = 100;

        static Int32 GlobalErrorCode = 0;

        static Int32 RunOneWebSocketTestNetFramework(
            Int32 numMessages, 
            Int32 messageSize,
            Char messageLetter) {

            try {

                using (ClientWebSocket ws = new ClientWebSocket()) {

                    // Establishing WebSockets connection.
                    Uri serverUri = new Uri("ws://" + ServerIp + ":" + ServerPort + "/wstest");
                    ws.Options.SetRequestHeader("NumMessagesToSend", numMessages.ToString());
                    ws.Options.SetRequestHeader("MessageSize", messageSize.ToString());
                    ws.Options.SetRequestHeader("MessageLetter", messageLetter.ToString());
                    Task t = ws.ConnectAsync(serverUri, CancellationToken.None);
                    t.Wait();

                    Byte[] sendBytes = new Byte[messageSize],
                        respBytes = new Byte[messageSize];

                    for (Int32 i = 0; i < messageSize; i++) {
                        sendBytes[i] = (Byte)messageLetter;
                    }

                    UInt64 webSocketId = UInt64.MaxValue;

                    Task<WebSocketReceiveResult> recvTask;
                    WebSocketReceiveResult result;

                    ArraySegment<Byte> bytesReceived = new ArraySegment<Byte>(respBytes, 0, respBytes.Length);
                    
                    recvTask = ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                    recvTask.Wait();
                    result = recvTask.Result;

                    // Checking that its a full message and its text.
                    if ((result.EndOfMessage) && (result.MessageType == WebSocketMessageType.Text)) {

                        String wsIdString = Encoding.UTF8.GetString(respBytes, 0, result.Count);
                        webSocketId = UInt64.Parse(wsIdString);

                    } else {

                        Console.Error.WriteLine("Wrong WebSocketId response.");
                        GlobalErrorCode = 1;
                        return GlobalErrorCode;
                    }
                                        
                    Int32 errCode = 0;
                    
                    Thread sendThread = new Thread(() => {

                        try {

                            // Sending and receiving messages.
                            for (Int32 n = 0; n < numMessages; n++) {

                                ArraySegment<Byte> bytesToSend = new ArraySegment<Byte>(sendBytes);

                                // Sending data in preferred format: text or binary.
                                Task task = ws.SendAsync(
                                    bytesToSend,
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);

                                task.Wait();
                            }

                        } catch (Exception exc) {
                            GlobalErrorCode = 2;
                            Console.WriteLine(exc);
                        }

                    });
                    sendThread.Start();

                    Thread recvThread = new Thread(() => {

                        try {

                            Int32 c = 0;

                            // Sending and receiving messages.
                            for (Int32 n = 0; n < numMessages; n++) {

                                bytesReceived = new ArraySegment<Byte>(respBytes, 0, respBytes.Length);

                                recvTask = ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                                recvTask.Wait();
                                result = recvTask.Result;

                                // Checking if we have a text message.
                                if (result.MessageType != WebSocketMessageType.Text) {
                                    errCode = 2;
                                    return;
                                }

                                // Checking if need to continue accumulation.
                                if (!result.EndOfMessage) {

                                    Int32 totalReceived = result.Count;

                                    // Accumulating until all data is received.
                                    while (totalReceived < sendBytes.Length) {

                                        bytesReceived = new ArraySegment<byte>(respBytes, totalReceived, respBytes.Length - totalReceived);

                                        recvTask = ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                                        recvTask.Wait();
                                        result = recvTask.Result;

                                        totalReceived += result.Count;
                                    }
                                }

                                if (c == 10000) {
                                    Console.WriteLine(n);
                                    c = 0;
                                }

                                c++;
                            }

                        } catch (Exception exc) {
                            GlobalErrorCode = 3;
                            Console.WriteLine(exc);
                        }

                    });
                    recvThread.Start();

                    // Waiting for threads to finish.
                    sendThread.Join();
                    recvThread.Join();
                    if (errCode != 0)
                        return errCode;

                    /*
                    // Sending and receiving messages.
                    for (Int32 n = 0; n < numMessages; n++) {

                        ArraySegment<Byte> bytesToSend = new ArraySegment<Byte>(sendBytes);

                        // Sending data in preferred format: text or binary.
                        Task task = ws.SendAsync(
                            bytesToSend,
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);

                        task.Wait();

                        bytesReceived = new ArraySegment<Byte>(respBytes, 0, respBytes.Length);

                        recvTask = ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                        recvTask.Wait();
                        result = recvTask.Result;

                        // Checking if we have a text message.
                        if (result.MessageType != WebSocketMessageType.Text) {
                            GlobalErrorCode = 1;
                            return GlobalErrorCode;
                        }

                        // Checking if need to continue accumulation.
                        if (!result.EndOfMessage) {

                            Int32 totalReceived = result.Count;

                            // Accumulating until all data is received.
                            while (totalReceived < sendBytes.Length) {

                                bytesReceived = new ArraySegment<byte>(respBytes, totalReceived, respBytes.Length - totalReceived);

                                recvTask = ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                                recvTask.Wait();
                                result = recvTask.Result;

                                totalReceived += result.Count;
                            }
                        }
                    }
                    */

                    // Trying to get correct socket statistics.
                    const Int32 NumStatsRetries = 10;
                    for (Int32 i = 0; i < NumStatsRetries; i++) {

                        String serverSocketStats = Http.GET<String>("http://localhost:8080/WsStats/" + webSocketId);

                        String correctStats = String.Format("{0} {1} {2} {3} {4} {5}",
                            webSocketId,
                            numMessages,
                            messageSize,
                            numMessages,
                            numMessages,
                            messageLetter);

                        // If stats are not correct, maybe server have not received all the data yet.
                        if (correctStats != serverSocketStats) {

                            // Checking if its a last try.
                            if ((NumStatsRetries - 1) == i) {

                                Console.Error.WriteLine("Wrong socket stats: " + serverSocketStats);
                                GlobalErrorCode = 4;
                                return GlobalErrorCode;
                            }

                        } else {

                            break;
                        }

                        Console.Write(".");
                        Thread.Sleep(100);
                    }

                    t = ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Just closing.", CancellationToken.None);
                    t.Wait();
                }

            } catch (Exception exc) {

                Console.Error.WriteLine(exc.ToString());
                GlobalErrorCode = 5;
                return GlobalErrorCode;
            }

            return 0;
        }

        static Int32 RunOneWebSocketTestWebSocket4Net(Int32 numMessages, Int32 messageSize, Char messageLetter) {

            try {

                UInt64 webSocketId = UInt64.MaxValue;

                List<KeyValuePair<String, String>> headers = new List<KeyValuePair<String, String>>();
                headers.Add(new KeyValuePair<string, string>("NumMessagesToSend", numMessages.ToString()));
                headers.Add(new KeyValuePair<string, string>("MessageSize", messageSize.ToString()));
                headers.Add(new KeyValuePair<string, string>("MessageLetter", messageLetter.ToString()));

                WebSocket4Net.WebSocket ws = new WebSocket4Net.WebSocket(
                    "ws://" + ServerIp + ":" + ServerPort + "/wstest",
                    null,
                    null,
                    headers);

                Byte[] bodyBytes = new Byte[messageSize];

                for (Int32 i = 0; i < messageSize; i++) {
                    bodyBytes[i] = (Byte) messageLetter;
                }

                Int32 messagesCount = numMessages;

                AutoResetEvent testFinishedEvent = new AutoResetEvent(false);

                ws.MessageReceived += (s, e) => {

                    // Trying to read the web socket id.
                    if (webSocketId == UInt64.MaxValue) {

                        webSocketId = UInt64.Parse(e.Message);
                        return;
                    }

                    // Checking that echo size is correct.
                    if (e.Message.Length != bodyBytes.Length) {

                        lock (ServerIp) {
                            Console.WriteLine("Incorrect WebSocket response size: " + e.Message.Length + ", should be " + bodyBytes.Length);
                            Console.WriteLine("Received echo body: " + e.Message);
                        }

                        GlobalErrorCode = 6;

                        testFinishedEvent.Set();

                        return;
                    }

                    // Checking the response and generating an error if a problem found.
                    for (Int32 i = 0; i < bodyBytes.Length; i++) {

                        if (bodyBytes[i] != e.Message[i]) {

                            lock (ServerIp) {
                                Console.WriteLine("Incorrect WebSocket response of length: " + e.Message.Length);
                            }

                            GlobalErrorCode = 7;

                            testFinishedEvent.Set();

                            return;
                        }
                    }

                    // Sending data again if number of messages is not exhausted.
                    messagesCount--;
                    if (messagesCount <= 0) {
                        testFinishedEvent.Set();
                    } else {
                        ws.Send(bodyBytes, 0, bodyBytes.Length);
                    }
                };

                ws.Opened += (s, e) => {

                    ws.Send(bodyBytes, 0, bodyBytes.Length);
                };

                ws.Error += (s, e) => {

                    lock (ServerIp) {
                        Console.WriteLine(e.Exception.ToString());
                    }

                    GlobalErrorCode = 8;

                    testFinishedEvent.Set();
                };

                Boolean closedGracefully = false;

                ws.Closed += (s, e) => {

                    if (!closedGracefully) {

                        lock (ServerIp) {
                            Console.WriteLine("WebSocket connection was closed unexpectedly!");
                        }

                        GlobalErrorCode = 9;

                        testFinishedEvent.Set();
                    }
                };

                // Starting the handshake.
                ws.Open();

                // Waiting for all tests to finish.
                if (!testFinishedEvent.WaitOne(1000000)) {

                    lock (ServerIp) {
                        Console.WriteLine("Failed to get WebSocket response in time!");
                    }

                    GlobalErrorCode = 10;
                }

                // If we are here that means the connection is closed properly.
                closedGracefully = true;

                ws.Close();

                // Trying to get correct socket statistics.
                const Int32 NumStatsRetries = 10;
                for (Int32 i = 0; i < NumStatsRetries; i++) {

                    String serverSocketStats = Http.GET<String>("http://localhost:8080/WsStats/" + webSocketId);

                    String correctStats = String.Format("{0} {1} {2} {3} {4} {5}",
                        webSocketId,
                        numMessages,
                        messageSize,
                        numMessages,
                        numMessages,
                        messageLetter);

                    // If stats are not correct, maybe server have not received all the data yet.
                    if (correctStats != serverSocketStats) {

                        // Checking if its a last try.
                        if ((NumStatsRetries - 1) == i) {

                            Console.Error.WriteLine("Wrong socket stats: " + serverSocketStats);

                            GlobalErrorCode = 11;
                            return GlobalErrorCode;
                        }

                    } else {

                        break;
                    }

                    Console.Write(".");
                    Thread.Sleep(1000);
                }

            } catch (Exception exc) {

                Console.Error.WriteLine(exc.ToString());

                GlobalErrorCode = 13;
                return GlobalErrorCode;
            }

            return GlobalErrorCode;
        }

        static Int32 Main(string[] args) {

            //Debugger.Launch();

            Console.WriteLine("Usage: WebSocketsTestClient.exe --ServerIp=127.0.0.1 --ServerPort=8080 --NumMessagesPerWebSocket=10000");
            Console.WriteLine();

            // Parsing parameters.
            foreach (String arg in args) {
                if (arg.StartsWith("--ServerIp=")) {
                    ServerIp = arg.Substring("--ServerIp=".Length);
                } else if (arg.StartsWith("--ServerPort=")) {
                    ServerPort = UInt16.Parse(arg.Substring("--ServerPort=".Length));
                } else if (arg.StartsWith("--NumMessagesPerWebSocket=")) {
                    NumMessagesPerWebSocket = Int32.Parse(arg.Substring("--NumMessagesPerWebSocket=".Length));
                }
            }

            Console.WriteLine(String.Format("Starting WebSocket test on {0}:{1}.", ServerIp, ServerPort));

            Stopwatch sw = new Stopwatch();

            for (Int32 i = 0; i < NumSockets; i++) {

                sw.Restart();

                //Int32 errCode = RunOneWebSocketTestWebSocket4Net(NumMessagesPerWebSocket, i + 100, 'A');

                Int32 errCode = RunOneWebSocketTestNetFramework(NumMessagesPerWebSocket, i + 100, 'A');

                sw.Stop();

                if (errCode != 0) {
                    GlobalErrorCode = errCode;
                    Console.WriteLine("Error socket {0}: {1} took {2} ms.", i, errCode, sw.ElapsedMilliseconds);
                    break;
                } else {
                    Console.WriteLine("Done socket {0} with {1} messages took {2} ms.", i, NumMessagesPerWebSocket, sw.ElapsedMilliseconds);
                }
            }

            Response globalStats = Http.GET("http://localhost:8080/WsGlobalStatus");

            if (200 == globalStats.StatusCode) {

                Int32 numDisconnects = Int32.Parse(globalStats.Body);

                if (numDisconnects == NumSockets) {

                    Console.WriteLine("WebSockets test finished successfully!");

                    return 0;

                } else {
                    Console.WriteLine("Wrong number of disconnects: " + numDisconnects);
                    GlobalErrorCode = 14;
                }
            } else {
                Console.WriteLine("Wrong global statistics response.");
                GlobalErrorCode = 15;
            }

            return GlobalErrorCode;
        }
    }
}
