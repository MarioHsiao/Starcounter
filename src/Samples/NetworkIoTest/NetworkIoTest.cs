﻿using System;
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

namespace NetworkIoTestApp
{
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
                    r.DestroySession();
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
                    return "Session string: " + s.SessionIdString;

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
            MODE_WEBSOCKETS_PORT,
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
                return;

            String db_number_string = args[0].Replace("DbNumber=", ""),
                port_number_string = args[1].Replace("PortNumber=", ""),
                test_type_string = args[2].Replace("TestType=", "");

            Int32 db_number = 0;
            TestTypes test_type = TestTypes.MODE_STANDARD_BROWSER;
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

            // Reading the image file if any.
            if (File.Exists(ImagePath))
                ImageBodyBytes = File.ReadAllBytes(ImagePath);
            else
                ImageBodyBytes = new Byte[] { (Byte)'N', (Byte)'O', (Byte)'!' };

            // Running handlers registration.
            RegisterHandlers(db_number, port_number, test_type);

            // Starting performance statistics thread.
            //Thread perf_thread = new Thread(PrintPerformanceThread);
            //perf_thread.Start();

            //DbSession dbs = new DbSession();
            //dbs.RunAsync(() => PrintPerformanceThread(0), 0);
        }

        // Handlers registration.
        static void RegisterHandlers(Int32 db_number, UInt16 port_number, TestTypes test_type)
        {
            String db_postfix = "_db" + db_number;
            UInt64 handler_id;

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
                    GatewayHandlers.RegisterPortHandler(port_number, OnRawPortEcho, 5, out handler_id);
                    Console.WriteLine("Successfully registered new handler: " + handler_id);

                    break;
                }

                case TestTypes.MODE_WEBSOCKETS_PORT:
                {
                    GatewayHandlers.RegisterPortHandler(port_number, OnWebSocket, 5, out handler_id);
                    Console.WriteLine("Successfully registered new handler: " + handler_id);

                    break;
                }

                case TestTypes.MODE_GATEWAY_HTTP:
                case TestTypes.MODE_GATEWAY_RAW:
                {
                    // Do nothing since its purely a gateway test.
                    Console.WriteLine("Not registering anything, since gateway mode only!");

                    break;
                }

                case TestTypes.MODE_APPS_URIS:
                {
                    AppsBootstrapper.Bootstrap(
                        "c:\\pics", StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort);

                    AppsClass.InitAppHandlers();

                    break;
                }

                case TestTypes.MODE_APPS_URIS_SESSION:
                {
                    AppsClass.InitAppHandlersSession();

                    break;
                }

                case TestTypes.MODE_HTTP_REST_CLIENT:
                {
                    AppsBootstrapper.Bootstrap();
                    Handle.POST<Request>(port_number, "/testrest", OnTestRest);

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

                        r["MyHeader1"] = "Haha!";
                        r["MyHeader2"] = "Xaha!";

                        return r;
                    });


                    Handle.GET(8080, "/shutdown", (Request req) =>
                    {
                        return new Response()
                        {
                            Body = "Closing connection",
                            ConnFlags = Response.ConnectionFlags.DisconnectAfterSend
                        };
                    });

                    Handle.POST(8080, "/echotest", (Request req) =>
                    {
                        return new Response() { BodyBytes = req.BodyBytes };
                    });

                    Handle.GET(8080, "/echotestws", (Request req) =>
                    {
                        if (req.WebSocketUpgrade)
                        {
                            req.Upgrade("echotestws");

                            return HandlerStatus.Handled;
                        }

                        return 513;
                    });

                    Handle.Socket(8080, "echotestws", (String s, WebSocket ws) =>
                    {
                        ws.Send(s);
                    });

                    Handle.Socket(8080, "echotestws", (Byte[] bs, WebSocket ws) =>
                    {
                        ws.Send(bs);
                    });

                    Handle.SocketDisconnect(8080, "echotestws", (UInt64 cargoId, IAppsSession session) =>
                    {
                        // Do nothing!
                    });

                    Handle.CUSTOM(8080, "{?} /{?}", (Request req, String method, String p1) =>
                    {
                        return "CUSTOM method " + method + " with parameter " + p1;
                    });

                    Handle.GET("/exc1", (Request req) =>
                    {
                        Response resp;
                        X.GET("/exc2", out resp);

                        return resp;
                    });

                    Handle.GET("/exc2", (Request req) =>
                    {
                        try
                        {
                            Response resp;
                            X.GET("/exc3", out resp);
                            return resp;
                        }
                        catch (ResponseException exc)
                        {
                            exc.ResponseObject.StatusDescription = "Modified!";
                            exc.ResponseObject["MyHeader"] = "Super value!";
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
                        X.POST("/echotest", "Here we go!", null, null, (Response resp, Object userObject) =>
                        {
                            // Modifying the response object by injecting some data.
                            resp["MySuperHeader"] = "Here is my header value!";
                            resp["Set-Cookie"] = "MySuperCookie=CookieValue;" + resp["Set-Cookie"];
                            req.Response = resp;
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

                        req.SendResponse(resp);

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

                    Handle.PUT( "/upload/{?}", (Request req, String uploadId) =>
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
                        if (req["UploadSettings"] == "Final")
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
                    for (Byte i = 0; i < Db.Environment.SchedulerCount; i++)
                        WebSocketSessions[i] = new Dictionary<UInt64, WebSocket>();

                    Random rand = new Random();
                    DbSession dbSession = new DbSession();
                    Int32 interval = 5000;
                    WebSocketSessionsTimer = new Timer((state) =>
                    {
                        // Schedule a job to check once for inactive sessions on each scheduler.
                        for (Byte i = 0; i < Db.Environment.SchedulerCount; i++)
                        {
                            // NOTE: Very important to make a copy of looped variable here!
                            Byte k = i;

                            // Getting sessions for current scheduler.
                            dbSession.RunAsync(() =>
                            {
                                // NOTE: Very important to make a copy of looped variable here!
                                Byte sched = k;

                                foreach (KeyValuePair<UInt64, WebSocket> ws in WebSocketSessions[sched])
                                {
                                    String pushMsg = "Scheduler: " + sched + ", seconds: " + TimerSeconds + " and weight: " + new String('A', 1 + rand.Next(20000));
                                    ws.Value.Send(pushMsg); // Log has changed
                                }

                            }, i);
                        }
                        TimerSeconds += 1;

                    }, null, interval, interval);

                    // Registering WebSocket handler.
                    Handle.GET("/ws", (Request req) =>
                    {
                        if (req.WebSocketUpgrade)
                        {
                            Byte schedId = ThreadData.Current.Scheduler.Id;
                            UniqueWebSocketIdentifier[schedId]++;

                            WebSocket ws = req.Upgrade("test", UniqueWebSocketIdentifier[schedId]);
                            WebSocketSessions[schedId].Add(UniqueWebSocketIdentifier[schedId], ws);

                            return HandlerStatus.Handled;
                        }

                        return new Response()
                        {
                            StatusCode = 500,
                            StatusDescription = "WebSocket upgrade on " + req.Uri + " was not approved."
                        };
                    });

                    Handle.Socket("test", (String s, WebSocket ws) => {
                        ws.Send(s);
                    });

                    Handle.Socket("test", (Byte[] s, WebSocket ws) => {
                        ws.Send(s);
                    });

                    Handle.SocketDisconnect("test", (UInt64 cargoId, IAppsSession session) =>
                    {
                        Byte schedId = ThreadData.Current.Scheduler.Id;
                        if (WebSocketSessions[schedId].ContainsKey(cargoId))
                            WebSocketSessions[schedId].Remove(cargoId);
                    });

                    break;
                }
            }
        }

        static Dictionary<UInt64, WebSocket>[] WebSocketSessions = new Dictionary<UInt64, WebSocket>[Db.Environment.SchedulerCount];
        static UInt64[] UniqueWebSocketIdentifier = new UInt64[Db.Environment.SchedulerCount];

        static volatile Int32 TimerSeconds = 0;

        // NOTE: Timer should be static, otherwise its garbage collected.
        static Timer WebSocketSessionsTimer = null;

        private static Boolean OnRawPortEcho(PortHandlerParams p)
        {
            if (p.DataStream.PayloadSize != 8)
                throw new ArgumentOutOfRangeException();

            Byte[] buffer = new Byte[16];
            UInt64 echo = p.DataStream.ReadUInt64(0);
            unsafe
            {
                fixed (Byte* buf = buffer)
                {
                    *(UInt64*) buf = echo;
                    *(UInt64*) (buf + 8) = echo;
                }
            }

            // Writing back the response.
            p.DataStream.SendResponse(buffer, 0, 16, Response.ConnectionFlags.NoSpecialFlags);

            // Counting performance.
            perf_counter++;

            return true;
        }

        private static Boolean OnRawPort(PortHandlerParams p)
        {
            Byte[] buffer = new Byte[p.DataStream.PayloadSize];
            p.DataStream.Read(buffer, 0, buffer.Length);

            // Converting whole buffer to a string.
            String resUri = ASCIIEncoding.ASCII.GetString(buffer);

            // Generating response.
            String response = resUri + "_user_" + p.UserSessionId + "_raw_response" + DateTime.Now + ":" + DateTime.Now.Millisecond;

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            // Writing back the response.
            p.DataStream.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            return true;
        }

        private static Boolean OnWebSocket(PortHandlerParams p)
        {
            Byte[] buffer = new Byte[p.DataStream.PayloadSize];
            p.DataStream.Read(buffer, 0, buffer.Length);

            // Converting whole buffer to a string.
            String resUri = ASCIIEncoding.ASCII.GetString(buffer);

            // Generating response.
            String response = resUri + "_user_" + p.UserSessionId + "_ws_response_" + DateTime.Now + ":" + DateTime.Now.Millisecond;

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            // Writing back the response.
            p.DataStream.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            return true;
        }

        private static Boolean OnHttpPort(PortHandlerParams p)
        {
            // Creating response string.
            String response =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>Handler URI prefix: whole port </h1>\r\n" +
                p.ToString() +
                "</body>\r\n" +
                "</html>\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            // Writing back the response.
            p.DataStream.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);

            return true;
        }

        private static Boolean OnHttpRoot(Request p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpRoot </h1>\r\n" +
                p.ToString() +
                "<h1>Presented cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        private static Boolean OnHttpPostRoot(Request p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpPostRoot </h1>\r\n" +
                p.ToString() +
                "<h1>Presented cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        // Creates internal Request structure.
        private static Boolean OnInternalHttpRequest(Request p)
        {
            String[] request_strings =
            {
                "GET /pub/WWW/TheProject.html HTTP/1.1\r\n" +
                "Host: www.w3.org\r\n" +
                "\r\n",
                                         
                "GET /get_funky_content_length_body_hello HTTP/1.0\r\n" +
                "conTENT-Length: 5\r\n" +
                "\r\n" +
                "HELLO",

                "GET /vi/Q1Nnm4AZv4c/hqdefault.jpg HTTP/1.1\r\n" +
                "Host: i2.ytimg.com\r\n" +
                "Connection: keep-alive\r\n" +
                "User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.4 (KHTML, like Gecko) Chrome/22.0.1229.94 Safari/537.4\r\n" +
                "Accept: */*\r\n" +
                "Referer: http://www.youtube.com/\r\n" +
                "Accept-Encoding: gzip,deflate,sdch\r\n" +
                "Accept-Language: en-US,en;q=0.8\r\n" +
                "Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3\r\n" +
                "\r\n",

                "POST /post_identity_body_world?q=search#hey HTTP/1.1\r\n" +
                "Accept: */*\r\n" +
                "Transfer-Encoding: identity\r\n" +
                "Content-Length: 5\r\n" +
                "\r\n" +
                "World",

                "PATCH /file.txt HTTP/1.1\r\n" +
                "Host: www.example.com\r\n" +
                "Content-Type: application/example\r\n" +
                "If-Match: \"e0023aa4e\"\r\n" +
                "Content-Length: 10\r\n" +
                "\r\n" +
                "cccccccccc",

                "POST / HTTP/1.1\r\n" +
                "Host: www.example.com\r\n" +
                "Content-Type: application/x-www-form-urlencoded\r\n" +
                "Content-Length: 4\r\n" +
                "Connection: close\r\n" +
                "\r\n" +
                "q=42\r\n"
            };

            String responseBody = "";

            // Collecting all parsed HTTP requests.
            for (Int32 i = 0; i < request_strings.Length; i++)
            {
                Byte[] request_bytes = Encoding.ASCII.GetBytes(request_strings[i]);
                Request internal_request = new Request(request_bytes, request_bytes.Length);

                responseBody += "-------------------------------";
                responseBody += internal_request.ToString();

                internal_request.Destroy();
            }

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        // Upload any file to /upload/{file_name}
        private static Boolean OnHttpUpload(Request p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpUpload </h1>\r\n" +
                p.ToString() +
                "<h1>Uploaded file of length: " + p.ContentLength + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            // Obtaining uploaded file name.
            String file_postfix = "null";
            if (p.Uri.Length > 8)
                file_postfix = p.Uri.Substring(8/*/upload/*/);

            String file_name = "uploaded_" + file_postfix;
            File.WriteAllBytes(file_name, p.BodyBytes);
            Console.WriteLine("Uploaded file saved: " + file_name);

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Server: Starcounter\r\n" + 
                "Access-Control-Allow-Origin: *\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        // Download any file from /download/{file_name}
        private static Boolean OnHttpDownload(Request p)
        {
            // Obtaining uploaded file name.
            String file_postfix = "null";
            if (p.Uri.Length > 10)
                file_postfix = p.Uri.Substring(10/*/download/*/);

            // Obtaining uploaded file name.
            String file_name = "uploaded_" + file_postfix;

            // Trying to load file from disk.
            Byte[] bodyBytes = new Byte[0];
            if (File.Exists(file_name))
            {
                bodyBytes = File.ReadAllBytes(file_name);
                Console.WriteLine("Read uploaded file: " + file_name);
            }

            String headerString =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Length: " + bodyBytes.Length + "\r\n" +
                "\r\n";

            Byte[] headerBytes = Encoding.ASCII.GetBytes(headerString);

            // Combining two arrays together.
            Byte[] responseBytes = new Byte[headerBytes.Length + bodyBytes.Length];
            headerBytes.CopyTo(responseBytes, 0);
            bodyBytes.CopyTo(responseBytes, headerBytes.Length);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        private static Boolean OnHttpGetRoot(Request p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpGetRoot </h1>\r\n" +
                p.ToString() +
                "<h1>Presented cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        private static Boolean OnHttpKillSession(Request p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpKillSession </h1>\r\n" +
                p.ToString() +
                "<h1>Presented cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n";

            // Generating new session cookie if needed.
            if (p.CameWithCorrectSession)
            {
                // Generating and writing new session.
                p.DestroySession();
            }

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        private static Boolean OnHttpSession(Request p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpSession </h1>\r\n" +
                p.ToString() +
                "<h1>Presented cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n";

            // Generating new session cookie if needed.
            if (!p.CameWithCorrectSession)
            {
                try
                {
                    // Generating and writing new session.
                    UInt32 err_code = p.GenerateNewSession(SchedulerAppsSessionsPool.Pool.Allocate());
                    if (err_code != 0)
                        return false;
                }
                catch(Exception exc)
                {
                    Console.WriteLine(exc.ToString());
                }

                // Displaying new session unique number.
                Console.WriteLine("Generated new session with index: " + p.UniqueSessionIndex);
            }

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        private static Response OnHttpEcho(Request p)
        {
            if (p.Body.Length != 8)
                return 513;

            // Counting performance.
            perf_counter++;

            return p.Body;
        }

        static Node someNode = new Node("127.0.0.1");

        private static Response OnTestRest(Request p)
        {
            String jsonContent = "{\"FirstName\":\"Allan\",\"LastName\":\"Ballan\",\"Age\":19,\"PhoneNumbers\":[{\"Number\":\"123-555-7890\"}]}";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Content-Length: " + jsonContent.Length + "\r\n" +
                "\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + jsonContent);

            try
            {
                return new Response()
                {
                    BodyBytes = responseBytes
                };
            }
            catch
            {
                return new Response()
                {
                    BodyBytes = kHttpServiceUnavailable
                };
            }
        }

        private static Boolean OnHttpUsers(Request p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpUsers </h1>\r\n" +
                p.ToString() +
                "<h1>Presented cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader = 
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n" +
                "\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        private static Boolean OnHttpOptions(Request p)
        {
            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: PUT, POST, GET, OPTIONS\r\n" +
                "Access-Control-Allow-Headers: Origin, Content-Type, Accept\r\n" +
                "Content-Length: 0\r\n\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }

        // Loading image file from disk statically.
        static Byte[] ImageBodyBytes = null;

        private static Boolean OnHttpGetImage(Request p)
        {
            String headerString =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: image/png\r\n" +
                "Content-Length: " + ImageBodyBytes.Length + "\r\n" +
                "\r\n";

            Byte[] headerBytes = Encoding.ASCII.GetBytes(headerString);

            // Combining two arrays together.
            Byte[] responseBytes = new Byte[headerBytes.Length + ImageBodyBytes.Length];
            headerBytes.CopyTo(responseBytes, 0);
            ImageBodyBytes.CopyTo(responseBytes, headerBytes.Length);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length, Response.ConnectionFlags.NoSpecialFlags);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length, Response.ConnectionFlags.NoSpecialFlags);
            }

            return true;
        }
    }
}
