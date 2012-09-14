using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal;
using System.Diagnostics;

namespace Starcounter.ABCIPC {

    /// <summary>
    /// Created by the framework. Given to the server handler.
    /// If an exception is raised, the request fails.
    /// </summary>
    public class Request {

        /// <summary>
        /// The (opaque) name of the shutdown request message. Servers that need
        /// custom handling for the shutdown request can install a handler with
        /// this name and customize - or even cancel - the shutdown.
        /// </summary>
        public const string ShutdownMessage = "!Shutdown!";

        internal static class Protocol {
            public const int MessageWithoutParameters = 10;     // Send(string)
            public const int MessageWithString = 11;            // Send(string, "value"), value not null.
            public const int MessageWithStringNULL = 12;        // Send(string, null);
            public const int MessageWithStringArray = 13;       // Send(string, {"one", "two"}), value not null.
            public const int MessageWithStringArrayNULL = 14;   // Send(string, null);
            public const int MessageWithDictionary = 15;        // Send(string, new Dictionary<string, string>), value not null.
            public const int MessageWithDictionaryNULL = 16;    // Send(string, null);

            public const string ShutdownRequest = "00";

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

            public static string MakeRequestStringWithStringArray(string message, string[] array) {
                var serializedArray = KeyValueBinary.FromArray(array).Value;
                // "13"[NN][message][NNNN][array] (the protocol limits the serialized array size to ~1K).
                return string.Format("{0}{1}{2}{3}{4}",
                    MessageWithStringArray, 
                    message.Length.ToString("D2"), 
                    message,
                    serializedArray.Length.ToString("D4"),
                    serializedArray
                    );
            }

            public static string MakeRequestStringWithStringArrayNULL(string message) {
                // "14"[NN][message]
                return string.Format("{0}{1}{2}", MessageWithStringArrayNULL, message.Length.ToString("D2"), message);
            }

            public static string MakeRequestStringWithDictionary(string message, Dictionary<string, string> dictionary) {
                var serialized = KeyValueBinary.FromDictionary(dictionary).Value;
                // "15"[NN][message][NNNN][dictionary] (the protocol limits the serialized dictionary size to ~1K).
                return string.Format("{0}{1}{2}{3}{4}",
                    MessageWithDictionary, 
                    message.Length.ToString("D2"), 
                    message,
                    serialized.Length.ToString("D4"),
                    serialized
                    );
            }

            public static string MakeRequestStringWithDictionaryNULL(string message) {
                // "16"[NN][message]
                return string.Format("{0}{1}{2}", MessageWithDictionaryNULL, message.Length.ToString("D2"), message);
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

                if (code == 13 || code == 14) {
                    // Message with string array.
                    msgLength = int.Parse(stringRequest.Substring(2, 2));
                    message = stringRequest.Substring(4, msgLength);
                    if (code == 14) {
                        data = null;
                    } else {
                        dataLength = int.Parse(stringRequest.Substring(4 + msgLength, 4));
                        data = stringRequest.Substring(4 + msgLength + 4, dataLength);
                    }

                    request = new Request(server, code, message, data);
                    return request;
                }

                if (code == 15 || code == 16) {
                    // Message with dictionary array.
                    msgLength = int.Parse(stringRequest.Substring(2, 2));
                    message = stringRequest.Substring(4, msgLength);
                    if (code == 16) {
                        data = null;
                    } else {
                        dataLength = int.Parse(stringRequest.Substring(4 + msgLength, 4));
                        data = stringRequest.Substring(4 + msgLength + 4, dataLength);
                    }

                    request = new Request(server, code, message, data);
                    return request;
                }

                if (code == 0) {
                    request = new Request(server, code, Request.ShutdownMessage, null);
                    request.IsShutdown = true;
                    return request;
                }

                // Should never happen
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

        public T GetParameter<T>() where T : class {
            if (this.messageType == Request.Protocol.MessageWithStringArray || this.messageType == Request.Protocol.MessageWithStringArrayNULL) {
                Trace.Assert(typeof(T) == typeof(string[]), string.Format("{0} is not a string[]", typeof(T).Name));
                if (this.messageType == Request.Protocol.MessageWithStringArrayNULL)
                    return (T) null;

                return KeyValueBinary.ToArray((string)this.ParameterData) as T;
            }

            if (this.messageType == Request.Protocol.MessageWithDictionary || this.messageType == Request.Protocol.MessageWithDictionaryNULL) {
                Trace.Assert(typeof(Dictionary<string, string>).IsAssignableFrom(typeof(T)), string.Format("{0} is not a Dictionary<string, string>", typeof(T).Name));
                if (this.messageType == Request.Protocol.MessageWithDictionaryNULL)
                    return (T)null;

                return KeyValueBinary.ToDictionary((string)this.ParameterData) as T;
            }

            try {
                return (T)this.ParameterData;
            } catch (InvalidCastException) {
                // Check if it's a cast exception and provide a more
                // understandable exception
                // TODO:
                throw;
            }
        }

        public void Respond(bool result) {
            InternalReply(
                Reply.Protocol.MakeString(Reply.TypeFromResult(result)));
        }

        public void Respond(string reply) {
            if (reply == null)
                throw new ArgumentNullException("reply");

            InternalReply(Reply.Protocol.MakeString(Reply.ReplyType.OKWithCarry, reply));
        }

        public void Respond(bool result, string reply) {
            if (reply == null)
                throw new ArgumentNullException("reply");

            InternalReply(Reply.Protocol.MakeString(Reply.TypeFromResult(result), reply));
        }

        public void ReplyStatus(string reply) {
            string protocol = reply == null ? 
                Reply.Protocol.MakeString(Reply.ReplyType.Progress) : 
                Reply.Protocol.MakeString(Reply.ReplyType.ProgressWithCarry, reply);
            InternalReply(protocol, true);
        }

        internal void RespondToUnknownMessage() {
            InternalReply(Reply.Protocol.MakeString(Reply.ReplyType.UnknownMessage, this.Message));
        }

        void InternalReply(string replyString) {
            InternalReply(replyString, false);
        }

        void InternalReply(string replyString, bool statusMessage) {
            if (IsResponded)
                throw new InvalidOperationException("Request has already been responded to.");

            server.reply(replyString);
            if (!statusMessage) {
                this.IsResponded = true;
            }
        }
    }

}
