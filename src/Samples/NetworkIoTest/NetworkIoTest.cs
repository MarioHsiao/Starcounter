using System;
using System.Text;
using System.Threading;
using Starcounter;

namespace NetworkIoTestApp
{
    public class NetworkIoTestApp : AppHandlers
    {
        internal static void Main(String[] args)
        {
            RegisterHandlers();
        }

        // Handlers registration.
        private static void RegisterHandlers()
        {
            UInt16 handlerId;

            RegisterUriHandler(80, "/", HTTP_METHODS.GET_METHOD, OnHttpMessageRoot, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            RegisterUriHandler(80, "/users", HTTP_METHODS.GET_METHOD, OnHttpMessageUsers, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            /*
            RegisterPortHandler(81, OnRawPortMessage, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            RegisterPortHandler(82, OnHttpMessagePort, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);

            RegisterPortHandler(83, OnWsMessage, out handlerId);
            Console.WriteLine("Successfully registered new handler: " + handlerId);
            */
        }

        // Raw port handler.
        private static Boolean OnRawPortMessage(PortHandlerParams p)
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

        // WebSockets handler with no specific URI.
        private static Boolean OnWsMessage(PortHandlerParams p)
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

        // HTTP handler with no specific URI.
        private static Boolean OnHttpMessagePort(PortHandlerParams p)
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

        // HTTP handler with no specific URI.
        private static Boolean OnHttpMessageRoot(HttpRequest p)
        {
            // Creating response string.
            String response =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>Handler URI prefix: root(/) </h1>\r\n" +
                p.ToString() +
                // TODO: Fix get header!
                //"<h1>All cookies: " + p["Cookie: "] + "</h1>" +
                //"<h1>Host: " + p["Host: "] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(response);

            // Writing back to channel.
            p.WriteResponse(respBytes, 0, respBytes.Length);

            return true;
        }

        // HTTP handler with no specific URI.
        private static Boolean OnHttpMessageUsers(HttpRequest p)
        {
            // Creating response string.
            String response =
                "<html>\r\n" +
                "<body>\r\n" +
                "<h1>Handler URI prefix: /users </h1>\r\n" +
                p.ToString() +
                // TODO: Fix get header!
                //"<h1>All cookies: " + p["Cookie: "] + "</h1>" +
                //"<h1>Host: " + p["Host: "] + "</h1>" +
                "</body>\r\n" +
                "</html>\r\n";

            // Converting string to byte array.
            Byte[] respBytes = Encoding.ASCII.GetBytes(response);

            // Writing back to channel.
            p.WriteResponse(respBytes, 0, respBytes.Length);

            return true;
        }
    }
}
