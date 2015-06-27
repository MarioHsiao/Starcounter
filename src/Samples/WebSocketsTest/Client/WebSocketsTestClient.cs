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

        static Int32 NumMessagesPerWebSocket = 1000;

        static Int32 MessageSizeBytes = 10;

        static Int32 NumSockets = 100;

        static Int32 GlobalErrorCode = 0;

        static void RunOneWebSocketTestNetFramework(
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

                    String wsIdString = "";

                    // Accumulating until all data is received.
                    do {

                        recvTask = ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                        recvTask.Wait();
                        result = recvTask.Result;
                        wsIdString += Encoding.UTF8.GetString(respBytes, 0, result.Count);

                    } while (!result.EndOfMessage);

                    // Checking that its a full message and its text.
                    webSocketId = UInt64.Parse(wsIdString);

                    Int32 sendRecvRatio = 0;
                                        
                    Thread sendThread = new Thread(() => {

                        try {

                            Int32 c = 0;

                            // Sending and receiving messages.
                            for (Int32 n = 0; n < numMessages; n++) {

                                while (sendRecvRatio > 10000) {
                                    Thread.Sleep(1);
                                }

                                ArraySegment<Byte> bytesToSend = new ArraySegment<Byte>(sendBytes);

                                // Sending data in preferred format: text or binary.
                                Task task = ws.SendAsync(
                                    bytesToSend,
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);

                                task.Wait();

                                Interlocked.Increment(ref sendRecvRatio);

                                if (c == 10000) {
                                    Console.WriteLine("Sent: " + n);
                                    c = 0;
                                }

                                c++;
                            }

                        } catch (Exception exc) {
                            GlobalErrorCode = 1;
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

                                String respMessage = "";

                                // Accumulating until all data is received.
                                do {

                                    recvTask = ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                                    recvTask.Wait();
                                    result = recvTask.Result;
                                    respMessage += Encoding.UTF8.GetString(respBytes, 0, result.Count);

                                } while (!result.EndOfMessage);

                                // Checking the length of received message.
                                if (respMessage.Length != messageSize) {
                                    GlobalErrorCode = 2;
                                    return;
                                }

                                // Checking the contents of received message.
                                for (Int32 i = 0; i < messageSize; i++) {

                                    if (respMessage[i] != sendBytes[i])  {

                                        GlobalErrorCode = 3;
                                        return;
                                    }
                                }

                                Interlocked.Decrement(ref sendRecvRatio);

                                if (c == 10000) {
                                    Console.WriteLine("Recv: " + n);
                                    c = 0;
                                }

                                c++;
                            }

                        } catch (Exception exc) {
                            GlobalErrorCode = 4;
                            Console.WriteLine(exc);
                        }

                    });
                    recvThread.Start();

                    // Waiting for threads to finish.
                    sendThread.Join();
                    recvThread.Join();

                    if (GlobalErrorCode != 0)
                        return;

                    t = ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Just closing.", CancellationToken.None);
                    t.Wait();

                    String correctStats = String.Format("{0} {1} {2} {3} {4} {5} True",
                        webSocketId,
                        numMessages,
                        messageSize,
                        numMessages,
                        numMessages,
                        messageLetter);

                    for (Int32 i = 0; i < 30; i++) {

                        Response resp = Http.GET("http://" + ServerIp + ":" + ServerPort + "/WsStats/" + webSocketId);

                        // If stats are not correct, maybe server have not received all the data yet.
                        if (resp.IsSuccessStatusCode && (correctStats.ToLower() == resp.Body.ToLower())) {

                            break;

                        } else if (i == 29) {

                            Console.WriteLine(String.Format("Wrong socket stats: \"{0}\" VS \"{1}\"", correctStats, resp.Body));
                            GlobalErrorCode = 5;
                            return;
                        }

                        Thread.Sleep(1000);
                        Console.WriteLine(".");
                    }
                    
                }

            } catch (Exception exc) {

                Console.Error.WriteLine(exc.ToString());
                GlobalErrorCode = 6;
                return;
            }
        }
        
        static Int32 Main(string[] args) {

            //Debugger.Launch();

            // Parsing parameters.
            foreach (String arg in args) {
                if (arg.StartsWith("--ServerIp=")) {
                    ServerIp = arg.Substring("--ServerIp=".Length);
                } else if (arg.StartsWith("--ServerPort=")) {
                    ServerPort = UInt16.Parse(arg.Substring("--ServerPort=".Length));
                } else if (arg.StartsWith("--NumMessagesPerWebSocket=")) {
                    NumMessagesPerWebSocket = Int32.Parse(arg.Substring("--NumMessagesPerWebSocket=".Length));
                } else if (arg.StartsWith("--MessageSizeBytes=")) {
                    MessageSizeBytes = Int32.Parse(arg.Substring("--MessageSizeBytes=".Length));
                } else if (arg.StartsWith("--NumSockets=")) {
                    NumSockets = Int32.Parse(arg.Substring("--NumSockets=".Length));
                }
            }

            Console.WriteLine("WebSocketsTestClient.exe --ServerIp={0} --ServerPort={1} --NumMessagesPerWebSocket={2} --MessageSizeBytes={3} --NumSockets={4}",
                ServerIp, ServerPort, NumMessagesPerWebSocket, MessageSizeBytes, NumSockets);
            Console.WriteLine();

            Console.WriteLine(String.Format("Starting WebSocket test on {0}:{1}.", ServerIp, ServerPort));

            Stopwatch sw = new Stopwatch();

            for (Int32 i = 0; i < NumSockets; i++) {

                sw.Restart();

                RunOneWebSocketTestNetFramework(NumMessagesPerWebSocket, MessageSizeBytes, 'A');

                sw.Stop();

                if (GlobalErrorCode != 0) {

                    Console.WriteLine("Error on socket {0}: error code {1}.", i, GlobalErrorCode);
                    break;

                } else {

                    Console.WriteLine("Done socket {0} with {1} messages took {2} ms.", i, NumMessagesPerWebSocket, sw.ElapsedMilliseconds);
                }
            }

            Response globalStats = Http.GET("http://" + ServerIp + ":" + ServerPort + "/WsGlobalStatus");

            if (200 != globalStats.StatusCode) {

                Console.WriteLine("Wrong global statistics response: " + globalStats.Body);
                GlobalErrorCode = 7;
            }

            if (GlobalErrorCode == 0) {
                Console.WriteLine("WebSocket test finished successfully!");
            }

            return GlobalErrorCode;
        }
    }
}
