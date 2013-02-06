﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using HttpStructs;
using Starcounter;
using System.Diagnostics;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Internal.Web;

namespace NetworkIoTestApp
{
    /// <summary>
    /// Some Apps handlers.
    /// </summary>
    public class AppsClass : App
    {
        /// <summary>
        /// Initializes some Apps handlers.
        /// </summary>
        public static void InitAppHandlers()
        {
            GET("/first", () =>
            {
                return "first";
            });

            GET("/{?}", (string str) =>
            {
                return "str_parameter=" + str;
            });

            /*
            GET("/{?}", (int num) =>
            {
                return "int_parameter=" + num;
            });

            GET("/second", () =>
            {
                return "second";
            });

            GET("/{?}/{?}", (string str1, string str2) =>
            {
                return "str_concat=" + str1 + str2;
            });

            GET("/{?}", (string str) =>
            {
                return "str_parameter=" + str;
            });
            */
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
            MODE_US_WEBSITE,
            MODE_APPS_URIS
        }

        // Performance related counters.
        static volatile UInt32 perf_counter = 0;
        static void PrintPerformanceThread()
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
            Thread perf_thread = new Thread(PrintPerformanceThread);
            perf_thread.Start();
        }

        // Handlers registration.
        static void RegisterHandlers(Int32 db_number, UInt16 port_number, TestTypes test_type)
        {
            String db_postfix = "_db" + db_number;
            UInt16 handler_id;
            String handler_uri;

            switch(test_type)
            {
                case TestTypes.MODE_STANDARD_BROWSER:
                {
                    handler_uri = "GET /";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpGetRoot, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "POST /";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpPostRoot, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "/";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpRoot, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "/users" + db_postfix;
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpUsers, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "OPTIONS /";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpOptions, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "/session" + db_postfix;
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpSession, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "POST /upload";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpUpload, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "/internal-http-request";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnInternalHttpRequest, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "GET /download";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpDownload, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "/killsession" + db_postfix;
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpKillSession, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    handler_uri = "GET /image" + db_postfix;
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpGetImage, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    break;
                }

                case TestTypes.MODE_GATEWAY_SMC_HTTP:
                {
                    handler_uri = "/smc-http-echo";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpEcho, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

                    break;
                }

                case TestTypes.MODE_GATEWAY_SMC_APPS_HTTP:
                {
                    handler_uri = "/smc-http-echo";
                    GatewayHandlers.RegisterUriHandler(port_number, handler_uri, OnHttpEcho, out handler_id);
                    Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);
                    
                    break;
                }

                case TestTypes.MODE_US_WEBSITE:
                {
                    AppsBootstrapper.Bootstrap(port_number, "c:\\ScOnScWeb\\sc\\www.starcounter.com");
                    RequestHandler.GET("/", null);

                    break;
                }

                case TestTypes.MODE_GATEWAY_SMC_RAW:
                {
                    GatewayHandlers.RegisterPortHandler(port_number, OnRawPortEcho, out handler_id);
                    Console.WriteLine("Successfully registered new handler: " + handler_id);

                    break;
                }

                case TestTypes.MODE_WEBSOCKETS_PORT:
                {
                    GatewayHandlers.RegisterPortHandler(port_number, OnWebSocket, out handler_id);
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
                    AppsBootstrapper.Bootstrap();
                    AppsClass.InitAppHandlers();

                    break;
                }
            }
        }

        private static Boolean OnRawPortEcho(PortHandlerParams p)
        {
            if (p.DataStream.PayloadSize != 8)
                throw new ArgumentOutOfRangeException();

            UInt64[] buffer = new UInt64[2];

            // Reading incoming echo message.
            buffer[0] = p.DataStream.ReadUInt64(0);
            buffer[1] = buffer[0];

            // Writing back the response.
            p.DataStream.SendResponse(buffer, 0, 16);

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
            p.DataStream.SendResponse(responseBytes, 0, responseBytes.Length);
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
            p.DataStream.SendResponse(responseBytes, 0, responseBytes.Length);
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
            p.DataStream.SendResponse(responseBytes, 0, responseBytes.Length);

            return true;
        }

        private static Boolean OnHttpRoot(HttpRequest p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpRoot </h1>\r\n" +
                p.ToString() +
                "<h1>Presented cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "<h1>Method: " + p.HttpMethod + "</h1>" +
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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        private static Boolean OnHttpPostRoot(HttpRequest p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpPostRoot </h1>\r\n" +
                p.ToString() +
                "<h1>Presented cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "<h1>Method: " + p.HttpMethod + "</h1>" +
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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        // Creates internal HttpRequest structure.
        private static Boolean OnInternalHttpRequest(HttpRequest p)
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
                HttpRequest internal_request = new HttpRequest(request_bytes);

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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        // Upload any file to /upload/{file_name}
        private static Boolean OnHttpUpload(HttpRequest p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpUpload </h1>\r\n" +
                p.ToString() +
                "<h1>Uploaded file of length: " + p.BodyLength + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            // Obtaining uploaded file name.
            String file_postfix = "null";
            if (p.Uri.Length > 8)
                file_postfix = p.Uri.Substring(8/*/upload/*/);

            String file_name = "uploaded_" + file_postfix;
            File.WriteAllBytes(file_name, p.GetBodyByteArray_Slow());
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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        // Download any file from /download/{file_name}
        private static Boolean OnHttpDownload(HttpRequest p)
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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        private static Boolean OnHttpGetRoot(HttpRequest p)
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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        private static Boolean OnHttpKillSession(HttpRequest p)
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
            if (p.HasSession)
            {
                // Generating and writing new session.
                p.DestroySession();
            }

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        private static Boolean OnHttpSession(HttpRequest p)
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
            if (!p.HasSession)
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

                // Adding the session cookie stub.
                responseHeader += "Set-Cookie: " + p.SessionStruct.SessionCookieStubString + "; HttpOnly\r\n";
            }

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        private static Boolean OnHttpEcho(HttpRequest p)
        {
            String responseBody = p.GetBodyStringUtf8_Slow();
            Debug.Assert(responseBody.Length == 8);

            //Console.WriteLine(responseBody);

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html\r\n" +
                "Content-Length: 8\r\n" +
                "\r\n";

            // Converting string to byte array.
            Byte[] responseBytes = Encoding.ASCII.GetBytes(responseHeader + responseBody);

            try
            {
                // Writing back the response.
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            // Counting performance.
            perf_counter++;

            return true;
        }

        private static Boolean OnHttpUsers(HttpRequest p)
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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        private static Boolean OnHttpOptions(HttpRequest p)
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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }

        // Loading image file from disk statically.
        static Byte[] ImageBodyBytes = null;

        private static Boolean OnHttpGetImage(HttpRequest p)
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
                p.SendResponse(responseBytes, 0, responseBytes.Length);
            }
            catch
            {
                // Writing back the error status.
                p.SendResponse(kHttpServiceUnavailable, 0, kHttpServiceUnavailable.Length);
            }

            return true;
        }
    }
}
