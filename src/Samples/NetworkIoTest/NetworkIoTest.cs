using System;
using System.IO;
using System.Text;
using System.Threading;
using HttpStructs;
using Starcounter;

namespace NetworkIoTestApp
{
    public class NetworkIoTestApp
    {
        internal static void Main(String[] args)
        {
            String db_number_string = Environment.GetEnvironmentVariable("DB_NUMBER");
            Int32 db_number = 0;
            
            if (!String.IsNullOrWhiteSpace(db_number_string))
                db_number = Int32.Parse(db_number_string);

            RegisterHandlers(db_number);
        }

        // Handlers registration.
        private static void RegisterHandlers(Int32 db_number)
        {
            String db_postfix = "_db" + db_number;
            UInt16 handler_id;

            /*
            GatewayHandlers.RegisterUriHandler(80, "GET /", OnHttpGetRoot, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            GatewayHandlers.RegisterUriHandler(80, "POST /", OnHttpPostRoot, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            GatewayHandlers.RegisterUriHandler(80, "/", OnHttpRoot, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);
            */

            String handler_uri = "/users" + db_postfix;
            GatewayHandlers.RegisterUriHandler(80, handler_uri, OnHttpUsers, out handler_id);
            Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

            handler_uri = "OPTIONS /" + db_postfix;
            GatewayHandlers.RegisterUriHandler(80, handler_uri, OnHttpOptions, out handler_id);
            Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

            handler_uri = "/session" + db_postfix;
            GatewayHandlers.RegisterUriHandler(80, handler_uri, OnHttpSession, out handler_id);
            Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

            handler_uri = "POST /upload";
            GatewayHandlers.RegisterUriHandler(80, handler_uri, OnHttpUpload, out handler_id);
            Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

            handler_uri = "GET /download";
            GatewayHandlers.RegisterUriHandler(80, handler_uri, OnHttpDownload, out handler_id);
            Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

            handler_uri = "/killsession" + db_postfix;
            GatewayHandlers.RegisterUriHandler(80, handler_uri, OnHttpKillSession, out handler_id);
            Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

            handler_uri = "GET /image" + db_postfix;
            GatewayHandlers.RegisterUriHandler(80, handler_uri, OnHttpGetImage, out handler_id);
            Console.WriteLine("Successfully registered new handler \"" + handler_uri + "\" with id: " + handler_id);

            /*
            RegisterPortHandler(81, OnRawPort, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            RegisterPortHandler(82, OnHttpPort, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            RegisterPortHandler(83, OnWebSocket, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);
            */
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
            Byte[] stringBytes = Encoding.ASCII.GetBytes(response);

            // Writing back the response.
            p.DataStream.Write(stringBytes, 0, stringBytes.Length);
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
            Byte[] stringBytes = Encoding.ASCII.GetBytes(response);

            // Writing back the response.
            p.DataStream.Write(stringBytes, 0, stringBytes.Length);
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
            Byte[] respBytes = Encoding.ASCII.GetBytes(response);

            // Writing back the response.
            p.DataStream.Write(respBytes, 0, respBytes.Length);

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
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            // Writing back the response.
            p.WriteResponse(respBytes, 0, respBytes.Length);

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
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            // Writing back the response.
            p.WriteResponse(respBytes, 0, respBytes.Length);

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
                "Content-Length: " + responseBody.Length + "\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            // Writing back the response.
            p.WriteResponse(respBytes, 0, respBytes.Length);

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
            Byte[] responseBuf = new Byte[headerBytes.Length + bodyBytes.Length];
            headerBytes.CopyTo(responseBuf, 0);
            bodyBytes.CopyTo(responseBuf, headerBytes.Length);

            // Writing back the response.
            p.WriteResponse(responseBuf, 0, responseBuf.Length);

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
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            // Writing back the response.
            p.WriteResponse(respBytes, 0, respBytes.Length);

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
                p.KillSession();
            }

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            // Writing back the response.
            p.WriteResponse(respBytes, 0, respBytes.Length);

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
                // Generating and writing new session.
                UInt64 uniqueSessionNumber = p.GenerateNewSession();

                // Displaying new session unique number.
                Console.WriteLine("Generated new session with number: " + p.UniqueSessionNumber);

                // Adding the session cookie stub.
                responseHeader += "Set-Cookie: " + p.SessionStruct.SessionCookieStubString + "; HttpOnly\r\n";
            }

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            // Writing back the response.
            p.WriteResponse(respBytes, 0, respBytes.Length);

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
                "Content-Length: " + responseBody.Length + "\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + "\r\n" + responseBody);

            // Writing back the response.
            p.WriteResponse(respBytes, 0, respBytes.Length);

            return true;
        }

        private static Boolean OnHttpOptions(HttpRequest p)
        {
            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: POST, GET, OPTIONS\r\n" +
                "Access-Control-Allow-Headers: Origin, Content-Type, Accept\r\n" +
                "Content-Length: 0\r\n\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader);

            // Writing back the response.
            p.WriteResponse(respBytes, 0, respBytes.Length);

            return true;
        }

        private static Boolean OnHttpGetImage(HttpRequest p)
        {
            // Loading image file from disk.
            Byte[] bodyBytes = File.ReadAllBytes(@"c:\github\Level1\src\Samples\NetworkIoTest\image.png");

            String headerString =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: image/png\r\n" +
                "Content-Length: " + bodyBytes.Length + "\r\n" +
                "\r\n";

            Byte[] headerBytes = Encoding.ASCII.GetBytes(headerString);

            // Combining two arrays together.
            Byte[] responseBuf = new Byte[headerBytes.Length + bodyBytes.Length];
            headerBytes.CopyTo(responseBuf, 0);
            bodyBytes.CopyTo(responseBuf, headerBytes.Length);

            // Writing back the response.
            p.WriteResponse(responseBuf, 0, responseBuf.Length);

            return true;
        }
    }
}
