using System;
using System.IO;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Internal.Web;
using Starcounter.Advanced;
using Codeplex.Data;
using System.Net;

namespace NetworkIoTestApp {

    /// <summary>
    /// Some Apps handlers.
    /// </summary>
    public class AppsClass
    {
        /// <summary>
        /// Initializes some Apps handlers.
        /// </summary>
        public static void InitAppHandlers()
        {
            String localString = "This is local string!";

            Handle.GET(80, "/local", () =>
            {
                return localString;
            });

            Handle.GET("/static/{?}/static", (String p1) =>
            {
                return String.Format("string {0}", p1);
            });

            Handle.GET("/{?}/{?}", (Int32 p1, Boolean p2) =>
            {
                return String.Format("int32 {0}, bool {1}", p1, p2);
            });

            Handle.GET("/{?}/{?}/{?}", (Int32 p1, Boolean p2, Double p3) =>
            {
                return String.Format("int32 {0}, bool {1}, double {2}", p1, p2, p3);
            });

            Handle.GET("/{?}", (Int32 p1) =>
            {
                return String.Format("int32 {0}", p1);
            });

            Handle.GET("/{?}/{?}", (Int32 p1, Decimal p2) =>
            {
                return String.Format("int32 {0}, decimal {1}", p1, p2);
            });
            
            Handle.GET("/{?}/{?}/{?}", (Int32 p1, string p2, string p3) =>
            {
                return String.Format("int32 {0}, string {1}, string {2}", p1, p2, p3);
            });

            Handle.GET("/{?}/{?}/{?}", (string p1, string p2, string p3) =>
            {
                return String.Format("string {0}, string {1}, string {2}", p1, p2, p3);
            });
             
            Handle.GET("/{?}/{?}/{?}", (string p1, Int32 p2, string p3) =>
            {
                return String.Format("string {0}, int32 {1}, string {2}", p1, p2, p3);
            });

            Handle.GET("/{?}/{?}", (Int64 p1, string p2) =>
            {
                return String.Format("int64 {0}, string {1}", p1, p2);
            });

            Handle.GET("/{?}/{?}", (string p1, string p2) =>
            {
                return String.Format("string {0}, string {1}", p1, p2);
            });

            Handle.GET("/{?}/{?}/{?}", (Int32 p1, string p2, Int32 p3) =>
            {
                return String.Format("int32 {0}, string {1}, int32 {2}", p1, p2, p3);
            });

            Handle.GET("/ab", () =>
            {
                return "ab";
            });

            Handle.GET("/{?}/{?}/{?}", (string p1, string p2, Int32 p3) =>
            {
                return String.Format("string {0}, string {1}, int32 {2}", p1, p2, p3);
            });
            
            Handle.GET("/{?}/{?}", (String p1, Int32 p2) =>
            {
                return String.Format("string {0}, int32 {1}", p1, p2);
            });

            Handle.GET("/", () =>
            {
                return "root";
            });

            Handle.GET("/s{?}", (String p1) =>
            {
                return "s" + p1;
            });

            Handle.GET("/{?}/static/{?}", (string str1, string str2) =>
            {
                return "str_concat_with_static=" + str1 + "static" + str2;
            });
        }

        /// <summary>
        /// Initializes some Apps handlers.
        /// </summary>
        public static void InitAppHandlersSession()
        {
            // http://127.0.0.1:8080/new-session
            Handle.GET("/new-session", (Request r) =>
            {
                if (!r.CameWithCorrectSession)
                {
                    if (Session.Current == null)
                        Session.Current = new Session();

                    UInt32 err = r.GenerateNewSession(Session.Current);
                    if (err != 0)
                        throw ErrorCode.ToException(err);

                    return "New session created: " + Session.Current.InternalSession.ToAsciiString();
                }
                else
                {
                    return "Session already exists!";
                }
                
            });

            // http://127.0.0.1:8080/del-session/70300000CAA03ED139EB1306FFFFFFFF
            Handle.GET("/del-session/{?}", (Session s, Request r) =>
            {
                if (r.CameWithCorrectSession)
                {
                    r.Session.Destroy();
                    return "Session deleted!";
                }
                else
                {
                    return "Session does not exist!";
                }
            });

            // http://127.0.0.1:8080/view-session/3030000008E25A422DB73D6FFFFFFFFF
            Handle.GET("/view-session/{?}", (Session s) =>
            {
                if (s != null)
                    return "Session string: " + s.SessionId;

                return "No session to view!";
            });
        }
    }

    public class NetworkIoTestApp
    {
        const String kHttpServiceUnavailableString =
            "HTTP/1.1 503 Service Unavailable\r\n" +
            "Content-Length: 0\r\n" +
            "\r\n";

        static readonly Byte[] kHttpServiceUnavailable = Encoding.ASCII.GetBytes(kHttpServiceUnavailableString);

        const String ImagePath = @"c:\github\Level1\src\Samples\NetworkIoTest\image.png";

        enum TestTypes
        {
            MODE_GATEWAY_HTTP,
            MODE_GATEWAY_RAW,
            MODE_GATEWAY_SMC_HTTP,
            MODE_GATEWAY_SMC_APPS_HTTP,
            MODE_GATEWAY_SMC_RAW,
            MODE_STANDARD_BROWSER,
            MODE_APPS_URIS,
            MODE_APPS_URIS_SESSION,
            MODE_HTTP_REST_CLIENT,
            MODE_WEBSOCKETS_URIS,
            MODE_NODE_TESTS,
            MODE_THROW_EXCEPTION
        }

        // Performance related counters.
        static volatile UInt32 perf_counter = 0;
        static void PrintPerformanceThread(Byte sched)
        {
            while (true)
            {
                Thread.Sleep(1000);

                Console.WriteLine("Database-side performance: " + perf_counter + " operations/s.");

                perf_counter = 0;
            }
        }

        static void Main(String[] args)
        {
            // Checking if length is correct.
            if (args.Length != 3)
                args = new String[] { "DbNumber=1", "PortNumber=8080", "TestType=MODE_NODE_TESTS" };

            String db_number_string = args[0].Replace("DbNumber=", ""),
                port_number_string = args[1].Replace("PortNumber=", ""),
                test_type_string = args[2].Replace("TestType=", "");

            Int32 db_number = 0;
            TestTypes test_type = TestTypes.MODE_NODE_TESTS;
            UInt16 port_number = 1235;

            Array test_type_values = Enum.GetValues(typeof(TestTypes));
            foreach (TestTypes t in test_type_values)
            {
                if (test_type_string == t.ToString())
                {
                    test_type = t;
                    break;
                }
            }

            // Checking if we need to throw an exception.
            if (test_type == TestTypes.MODE_THROW_EXCEPTION)
                throw new Exception("Oh no, I'm crashing the host!");
            
            if (!String.IsNullOrWhiteSpace(db_number_string))
                db_number = Int32.Parse(db_number_string);

            if (!String.IsNullOrWhiteSpace(port_number_string))
                port_number = UInt16.Parse(port_number_string);

            // Running handlers registration.
            RegisterHandlers(db_number, port_number, test_type);

            // Starting performance statistics thread.
            //Thread perf_thread = new Thread(PrintPerformanceThread);
            //perf_thread.Start();

            //Scheduling.ScheduleTask(() => PrintPerformanceThread(0), false, 0);
        }

        static Int32 WsEchoesCounter = 0;
        static Int32 WsDisconnectsCounter = 0;
        static Int32 WsHandshakesCounter = 0;
        static Int32 HttpEchoesCounter = 0;
        static Int32 RawPortBytesCounter = 0;
        static Int32 RawPortDisconnectsCounter = 0;

        // Handlers registration.
        static void RegisterHandlers(Int32 db_number, UInt16 port_number, TestTypes test_type)
        {
            String db_postfix = "_db" + db_number;

            switch(test_type)
            {
                case TestTypes.MODE_GATEWAY_SMC_HTTP:
                {
                    Handle.POST<Request>(port_number, "/smc-http-echo", OnHttpEcho);
                    break;
                }

                case TestTypes.MODE_GATEWAY_SMC_APPS_HTTP:
                {
                    Handle.POST<Request>(port_number, "/smc-http-echo", OnHttpEcho);
                    
                    break;
                }

                case TestTypes.MODE_GATEWAY_SMC_RAW:
                {

                    break;
                }

                case TestTypes.MODE_GATEWAY_HTTP:
                case TestTypes.MODE_GATEWAY_RAW:
                {
                    // Do nothing since its purely a gateway test.
                    Console.WriteLine("Not registering anything, since gateway mode only!");

                    break;
                }

                case TestTypes.MODE_APPS_URIS_SESSION:
                {
                    AppsClass.InitAppHandlersSession();

                    break;
                }

                case TestTypes.MODE_NODE_TESTS:
                {
                    Handle.GET(8080, "/headers", (Request req) =>
                    {
                        Response r = new Response()
                        {
                            Body = "Closing connection",
                            ConnFlags = Response.ConnectionFlags.DisconnectAfterSend
                        };

                        r.Headers["MyHeader1"] = "Haha!";
                        r.Headers["MyHeader2"] = "Xaha!";

                        return r;
                    });


                    Handle.GET(8080, "/shutdown", (Request req) =>
                    {
                        // Checking for client IP address.
                        String clientIp = req.ClientIpAddress.ToString();
                        if (clientIp != "127.0.0.1")
                            throw new Exception("Wrong client IP address: " + clientIp);

                        return new Response()
                        {
                            Body = "Closing connection",
                            ConnFlags = Response.ConnectionFlags.DisconnectAfterSend
                        };
                    });

                    Handle.GET(8080, "/echotestws", (Request req) =>
                    {
                        // Checking for client IP address.
                        String clientIp = req.ClientIpAddress.ToString();
                        if (clientIp != "127.0.0.1")
                            throw new Exception("Wrong client IP address: " + clientIp);

                        if (req.WebSocketUpgrade)
                        {
                            Interlocked.Increment(ref WsHandshakesCounter);

                            req.SendUpgrade("echotestws");

                            return HandlerStatus.Handled;
                        }

                        return 513;
                    });

                    Handle.DELETE(8080, "/resetcounters", (Request req) => {

                        // Checking for client IP address.
                        String clientIp = req.ClientIpAddress.ToString();
                        if (clientIp != "127.0.0.1")
                            throw new Exception("Wrong client IP address: " + clientIp);

                        WsEchoesCounter = 0;
                        WsDisconnectsCounter = 0;
                        WsHandshakesCounter = 0;
                        HttpEchoesCounter = 0;
                        RawPortBytesCounter = 0;
                        RawPortDisconnectsCounter = 0;

                        return 200;
                    });

                    Handle.GET(8080, "/wscounters", (Request req) => {

                        // Checking for client IP address.
                        String clientIp = req.ClientIpAddress.ToString();
                        if (clientIp != "127.0.0.1")
                            throw new Exception("Wrong client IP address: " + clientIp);

                        Int32 e = WsEchoesCounter,
                            d = WsDisconnectsCounter,
                            h = WsHandshakesCounter;

                        WsEchoesCounter = 0;
                        WsDisconnectsCounter = 0;
                        WsHandshakesCounter = 0;

                        return new Response() { Body = String.Format("WebSockets counters: handshakes={0}, echoes received={1}, disconnects={2}", h, e, d) };
                    });

                    Handle.WebSocket(8080, "echotestws", (String s, WebSocket ws) =>
                    {
                        Interlocked.Increment(ref WsEchoesCounter);

                        ws.Send(s);
                    });

                    Handle.WebSocket(8080, "echotestws", (Byte[] bs, WebSocket ws) =>
                    {
                        Interlocked.Increment(ref WsEchoesCounter);

                        ws.Send(bs);
                    });

                    Handle.WebSocketDisconnect(8080, "echotestws", (WebSocket ws) =>
                    {
                        Interlocked.Increment(ref WsDisconnectsCounter);

                        // Do nothing!
                    });

                    Handle.GET(8080, "/httpcounters", (Request req) => {

                        // Checking for client IP address.
                        String clientIp = req.ClientIpAddress.ToString();
                        if (clientIp != "127.0.0.1")
                            throw new Exception("Wrong client IP address: " + clientIp);

                        Int32 e = HttpEchoesCounter;

                        HttpEchoesCounter = 0;

                        return new Response() { Body = String.Format("Http counters: echoes received={0}.", e) };
                    });

                    Handle.POST(8080, "/echotest", (Request req) => {

                        // Checking for client IP address.
                        String clientIp = req.ClientIpAddress.ToString();
                        if (clientIp != "127.0.0.1")
                            throw new Exception("Wrong client IP address: " + clientIp);

                        Interlocked.Increment(ref HttpEchoesCounter);

                        return new Response() { BodyBytes = req.BodyBytes };
                    });

                    Int64[] LoopingHostNumRequests = new Int64 [StarcounterEnvironment.SchedulerCount];
                    Stopwatch[] LoopingHostStopwatches = new Stopwatch[StarcounterEnvironment.SchedulerCount];

                    Handle.GET(12345, "/loopstats/{?}", (Request req, Int32 schedId) => {

                        String stats = "Looping host schedulers status" + Environment.NewLine + "--------------------------";
                        double totalRps = 0;

                        for (Byte s = 0; s < StarcounterEnvironment.SchedulerCount; s++) {

                            if (null != LoopingHostStopwatches[s]) {

                                Int64 numRequests = LoopingHostNumRequests[s];
                                double rps = (numRequests * 1000.0) / LoopingHostStopwatches[s].ElapsedMilliseconds;
                                totalRps += rps;
                                stats += Environment.NewLine + "Scheduler " + s + ": processed requests " + numRequests + ", RPS " + rps;
                            }
                        }

                        stats += Environment.NewLine + "Total RPS: " + totalRps;
                        
                        return stats;
                    });

                    Handle.GET(12345, "/loop/{?}", (Request req, Int32 schedId) => {

                        Byte s = StarcounterEnvironment.CurrentSchedulerId;
                        Debug.Assert(schedId == s);

                        LoopingHostNumRequests[s]++;

                        // Checking if stopwatch is started.
                        if (LoopingHostStopwatches[s] == null) {
                            LoopingHostStopwatches[s] = new Stopwatch();
                            LoopingHostStopwatches[s].Start();
                        }

                        return new Response() { BodyBytes = req.BodyBytes };
                    });


                    Handle.CUSTOM(8080, "{?} /{?}", (Request req, String method, String p1) =>
                    {
                        return "CUSTOM method " + method + " with parameter " + p1;
                    });

                    Handle.GET(8080, "/rawportcounters", (Request req) => {

                        // Checking for client IP address.
                        String clientIp = req.ClientIpAddress.ToString();
                        if (clientIp != "127.0.0.1")
                            throw new Exception("Wrong client IP address: " + clientIp);

                        Int32 e = RawPortBytesCounter,
                            d = RawPortDisconnectsCounter;

                        RawPortBytesCounter = 0;
                        RawPortDisconnectsCounter = 0;

                        return new Response() { Body = String.Format("Raw port counters: bytes received={0}, disconnects={1}.", e, d) };
                    });

                    Handle.Tcp(8585, OnTcpSocket);

                    Handle.Udp(8787, (IPAddress clientIp, UInt16 clientPort, Byte[] datagram) => {

                        String msg = UTF8Encoding.UTF8.GetString(datagram);

                        //Console.WriteLine(msg);

                        UdpSocket.Send(clientIp, clientPort, 8787, msg);
                    });

                    Handle.GET("/exc1", (Request req) =>
                    {
                        Response resp = Self.GET("/exc2");

                        return resp;
                    });

                    Handle.GET("/exc2", (Request req) =>
                    {
                        try
                        {
                            Response resp = Self.GET("/exc3");
                            return resp;
                        }
                        catch (ResponseException exc)
                        {
                            exc.ResponseObject.StatusDescription = "Modified!";
                            exc.ResponseObject.Headers["MyHeader"] = "Super value!";
                            exc.UserObject = "My user object!";
                            throw exc;
                        }

                    });

                    Handle.GET("/exc3", (Request req) =>
                    {
                        Response resp = new Response()
                        {
                            StatusCode = 404,
                            StatusDescription = "Not found!"
                        };
                        throw new ResponseException(resp);
                    });

                    Handle.GET("/postponed", (Request req) =>
                    {
                        Http.POST("http://localhost:8080/echotest", "Here we go!", null, (Response resp) => {
                            // Modifying the response object by injecting some data.
                            resp.Headers["MySuperHeader"] = "Here is my header value!";
                            resp.Headers["Set-Cookie"] = "MySuperCookie=CookieValue;" + resp.Headers["Set-Cookie"];
                        }); // "resp" object will be automatically sent when delegate exits.

                        return HandlerStatus.Handled;
                    });

                    Handle.GET("/killme", (Request req) =>
                    {
                        Response resp = new Response()
                        {
                            StatusCode = 213,
                            StatusDescription = "I am gonna shutdown now.."
                        };

                        req.SendResponse(resp, null);

                        return HandlerStatus.Handled;
                    });

                    Dictionary<String, FileStream> uploadedFiles = new Dictionary<String, FileStream>();

                    Handle.POST("/upload", (Request req) =>
                    {
                        Random rand = new Random((int)DateTime.Now.Ticks);
                        String fileName = "upload-";
                        for (Int32 i = 0; i < 5; i++)
                            fileName += rand.Next();

                        // Create destination file.
                        FileStream fs = new FileStream(fileName, FileMode.Create);
                        uploadedFiles.Add(fileName, fs);

                        Response resp = new Response() { Body = fileName };

                        return resp;
                    });

                    Handle.PUT("/upload/{?}", (Request req, String uploadId) =>
                    {
                        // Checking that dictionary contains the upload.
                        if (!uploadedFiles.ContainsKey(uploadId))
                            return 404;

                        Byte[] bodyBytes = req.BodyBytes;
                        UInt64 checkSum = 0;
                        for (Int32 i = 0; i < bodyBytes.Length; i++)
                            checkSum += bodyBytes[i];

                        FileStream fs = uploadedFiles[uploadId];
                        fs.Write(bodyBytes, 0, bodyBytes.Length);
                        if (req.Headers["UploadSettings"] == "Final")
                        {
                            fs.Close();
                            uploadedFiles.Remove(uploadId);

                            try
                            {
                                
                            }
                            catch (Exception e)
                            {
                                return new Response()
                                {
                                    StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError,
                                    Body = "Failed to handle the package. " + e.ToString()
                                };
                            }
                        }

                        return new Response()
                        {
                            StatusCode = (ushort)System.Net.HttpStatusCode.NoContent,
                            Body = checkSum.ToString()
                        };
                    });

                    break;
                }

                case TestTypes.MODE_WEBSOCKETS_URIS:
                {
                    

                    break;
                }
            }
        }

        private static void OnTcpSocket(TcpSocket tcpSocket, Byte[] incomingData)
        {
            // Checking if we have socket disconnect here.
            if (null == incomingData) {

                Interlocked.Increment(ref RawPortDisconnectsCounter);

                // Utilize the rawSocket.SocketUniqueId to clean up user socket resources.

                return;
            }

            Interlocked.Add(ref RawPortBytesCounter, incomingData.Length);

            // Checking if data is big enough to be splited (JUST an example of several pushes at once).
            if (incomingData.Length < 10) {

                tcpSocket.Send(incomingData);

            } else {
                // Splitting data on several pushes.
                const Int32 NumPushes = 3;
                Int32 pushAtOnce = incomingData.Length / NumPushes,
                    offset = 0;

                // Simulating several pushes.
                for (Int32 i = 0; i < NumPushes - 1; i++) {

                    // Writing back the response.
                    tcpSocket.Send(incomingData, offset, pushAtOnce);

                    offset += pushAtOnce;
                }

                // Sending back the response.
                tcpSocket.Send(incomingData, offset, incomingData.Length - offset);
            }

            // Counting performance.
            perf_counter++;
        }

        private static Response OnHttpEcho(Request p)
        {
            if (p.Body.Length != 8)
                return 513;

            // Counting performance.
            perf_counter++;

            return p.Body;
        }
 
    }
}
