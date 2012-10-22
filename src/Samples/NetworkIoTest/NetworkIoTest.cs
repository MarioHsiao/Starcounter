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
            RegisterHandlers();
        }

        // Handlers registration.
        private static void RegisterHandlers()
        {
            UInt16 handlerId;

            GatewayHandlers.RegisterUriHandler(80, "GET /", OnHttpGetRoot, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            GatewayHandlers.RegisterUriHandler(80, "POST /", OnHttpPostRoot, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            GatewayHandlers.RegisterUriHandler(80, "/", OnHttpRoot, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            GatewayHandlers.RegisterUriHandler(80, "/users", OnHttpUsers, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            GatewayHandlers.RegisterUriHandler(80, "GET /image", OnHttpGetImage, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

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

            // Writing back to channel.
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

            // Writing back to channel.
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

            // Writing back to channel.
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
                "<h1>All cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "<h1>Method: " + p.HttpMethod + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Set-Cookie: " + p.SessionStruct.ConvertToSessionCookieFaster() + "; HttpOnly\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n" +
                "\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + responseBody);

            // Writing back to channel.
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
                "<h1>All cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "<h1>Method: " + p.HttpMethod + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Set-Cookie: " + p.SessionStruct.ConvertToSessionCookieFaster() + "; HttpOnly\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n" +
                "\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + responseBody);

            // Writing back to channel.
            p.WriteResponse(respBytes, 0, respBytes.Length);

            return true;
        }

        private static Boolean OnHttpGetRoot(HttpRequest p)
        {
            String responseBody =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>URI handler: OnHttpGetRoot </h1>\r\n" +
                p.ToString() +
                "<h1>All cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Set-Cookie: " + p.SessionStruct.ConvertToSessionCookieFaster() + "; HttpOnly\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n" +
                "\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + responseBody);

            // Writing back to channel.
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
                "<h1>All cookies: " + p["Cookie"] + "</h1>" +
                "<h1>Host: " + p["Host"] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            String responseHeader = 
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n" +
                "Set-Cookie: " + p.SessionStruct.ConvertToSessionCookieFaster() + "; HttpOnly\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n" +
                "\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(responseHeader + responseBody);

            // Writing back to channel.
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
                "Set-Cookie: " + p.SessionStruct.ConvertToSessionCookieFaster() + "; HttpOnly\r\n" +
                "Content-Length: " + bodyBytes.Length + "\r\n" +
                "\r\n";

            Byte[] headerBytes = Encoding.ASCII.GetBytes(headerString);

            // Combining two arrays together.
            Byte[] responseBuf = new Byte[headerBytes.Length + bodyBytes.Length];
            headerBytes.CopyTo(responseBuf, 0);
            bodyBytes.CopyTo(responseBuf, headerBytes.Length);

            // Writing back to channel.
            p.WriteResponse(responseBuf, 0, responseBuf.Length);

            return true;
        }
    }
}
