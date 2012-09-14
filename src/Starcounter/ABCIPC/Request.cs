using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.ABCIPC {

    /// <summary>
    /// Created by the framework. Given to the server handler.
    /// If an exception is raised, the request fails.
    /// </summary>
    public class Request {

        internal static class Protocol {
            public const int MessageWithoutParameters = 10;     // Send(string)
            public const int MessageWithString = 11;            // Send(string, "value"), value not null.
            public const int MessageWithStringNULL = 12;        // Send(string, null);
            public const int MessageWithStringArray = 13;       // Send(string, {"one", "two"}), value not null.
            public const int MessageWithStringArrayNULL = 14;   // Send(string, null);
            public const int MessageWithDictionary = 15;        // Send(string, new Dictionary<string, string>), value not null.
            public const int MessageWithDictionaryNULL = 16;    // Send(string, null);

            public static string MakeRequestStringWithoutParameters(string message) {
                // "10"[NN][message] where [NN] is the length of the message and message contains the message itself.
                return string.Format("{0}{1}{2}", MessageWithoutParameters, message.Length.ToString("D2"), message);
            }

            public static string MakeRequestStringWithStringParameter(string message, string parameter) {
                // "11"[NN][message][NN][parameter]
                return string.Format("{0}{1}{2}{3}{4}", MessageWithString, message.Length.ToString("D2"), message, parameter.Length.ToString("D2"), parameter);
            }

            public static string MakeRequestStringWithStringNULL(string message) {
                // "12"[NN][message]
                return string.Format("{0}{1}{2}", MessageWithStringNULL, message.Length.ToString("D2"), message);
            }

            public static Request Parse(Server server, string stringRequest) {
                Request request;
                int code;
                int msgLength;
                string message;
                int dataLength;
                string data;

                // Ugly, rather ineffective parsing. Improve.
                // TODO:

                code = int.Parse(stringRequest.Substring(0, 2));
                if (code == 10 || code == 11 || code == 12) {
                    msgLength = int.Parse(stringRequest.Substring(2, 2));
                    message = stringRequest.Substring(4, msgLength);
                    if (code == 11) {
                        dataLength = int.Parse(stringRequest.Substring(4 + msgLength, 2));
                        data = stringRequest.Substring(4 + msgLength + 2, dataLength);
                    } else {
                        data = null;
                    }

                    request = new Request(server, code, message, data);
                    return request;
                }

                // Handle other requests.
                // TODO:

                throw new NotImplementedException();
            }
        }

        readonly Server server;
        internal bool IsResponded { get; private set; }
        internal int messageType;

        public string Message {
            get;
            private set;
        }

        public bool IsShutdown {
            get;
            set;
        }

        object ParameterData {
            get;
            set;
        }

        internal Request(Server server, int msgType, string message, string data) {
            this.server = server;
            this.messageType = msgType;
            this.Message = message;
            this.IsResponded = false;
            this.ParameterData = data;
        }

        public T GetParameter<T>() {
            try {
                return (T)this.ParameterData;
            } catch (InvalidCastException) {
                // Check if it's a cast exception and provide a more
                // understandable exception
                // TODO:
                throw;
            }
        }

        //public string GetStringParameter() {
        //    Trace.Assert(messageType == Protocol.MessageWithString || messageType == Protocol.MessageWithStringNULL);
        //    return (string)ParameterData;
        //}

        //public string[] GetStringArrayParameter() {
        //    Trace.Assert(messageType == Protocol.MessageWithStringArray || messageType == Protocol.MessageWithStringArrayNULL);
        //    return (string[])ParameterData;
        //}

        //public Dictionary<string, string> GetDictionaryParameter() {
        //    Trace.Assert(messageType == Protocol.MessageWithDictionary || messageType == Protocol.MessageWithDictionaryNULL);
        //    return (Dictionary<string, string>)ParameterData;
        //}

        /* internal enum ReplyType {
            OK = 50,
            OKWithCarry = 51,
            Progress = 52,
            ProgressWithCarry = 53,
            Fail = 80,
            FailWithCarry = 81,
            UnknownMessage = 82,
            BadSignature = 83,
            HandlerException = 84
        }*/

        public void Respond(bool result) {
            // OK/Fail without carry.
            InternalRespond2(result ? "50" : "80");
        }

        public void Respond(string reply) {
            if (reply == null)
                throw new ArgumentNullException();

            // OK with carry
            InternalRespond2("51" + reply);
        }

        public void Respond(bool result, string reply) {
            if (reply == null)
                throw new ArgumentNullException();

            // OK/Fail with carry
            InternalRespond2(result ? "51" : "81" + reply);
        }

        void InternalRespond2(string replyString) {
            if (IsResponded)
                throw new InvalidOperationException("Request has already been responded to.");

            // Log it?
            // Mark it as responded!
            // Save the response!
            // Send reply back.
            // TODO:

            server.reply(replyString);
            this.IsResponded = true;
        }
    }

}
