
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Starcounter.ABCIPC.Internal {
    
    public static class Utils {

        public static class PromptHelper {

            /// <summary>
            /// Can be installed in servers that want to offer a simple
            /// text based input/output alternative, using the console.
            /// </summary>
            /// <remarks>
            /// Install when creating new server, like new Server(ReadTextRequestFromConsole, WriteTextResponseToConsole);
            /// </remarks>
            /// <returns></returns>
            public static string ReadTextRequestFromConsole() {
                while (true) {
                    Console.Write("Request>");
                    string read = Console.ReadLine();
                    string protocol;

                    // Never return anything until we have properly parsed it!
                    // And remember, we can write to the console here to!
                    // TODO:

                    read = read.Trim();
                    if (read.Equals(string.Empty)) {
                        // Make shutdown request.
                        // TODO;
                        Console.Beep();
                        continue;
                    }

                    int indexOfFirstSpace = read.IndexOf(" ");
                    if (indexOfFirstSpace == -1) {
                        // Call with no parameters.
                        // Implements: client.Send(string)
                        protocol = Request.Protocol.MakeRequestStringWithoutParameters(read);
                        return RequestWithProtocol(protocol);
                    }

                    // We've got a command and some additional stuff on the command line.
                    // Try interpret it.

                    string message;
                    string parameters;

                    message = read.Substring(0, indexOfFirstSpace);
                    parameters = read.Substring(indexOfFirstSpace + 1).TrimStart();

                    // All parameterized messages allows a certain syntax
                    // for invoking the server with NULL.
                    // TODO:

                    const string NULL_PARAMETER = "$0";

                    if (parameters.StartsWith("[")) {
                        // Call with string array parameters
                        // Implements: client.Send(string, string[]);
                        parameters = parameters.Trim('[', ']');
                        var arr = parameters.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (arr.Length == 0) {
                            arr = null;
                        }

                        Console.Beep();
                        continue;
                        //client.Send(message, arr, delegate(Reply reply) {
                        //    Console.WriteLine(message + "=" + reply);
                        //});
                    }
                    else if (parameters.StartsWith("<")) {
                        // Call with dictionary parameters
                        // Implements: client.Send(string, Dictionary<string, string>);
                        parameters = parameters.Trim('<', '>');
                        Console.Beep();
                        continue;
                    }
                    else {
                        // Call with single string parameter
                        // Implements: client.Send(string, string);

                        if (parameters.Equals(NULL_PARAMETER)) {
                            protocol = Request.Protocol.MakeRequestStringWithStringNULL(message);
                        } else {
                            protocol = Request.Protocol.MakeRequestStringWithStringParameter(message, parameters);
                        }

                        return RequestWithProtocol(protocol);
                    }
                }
            }

            public static void WriteTextResponseToConsole(string response) {
                var r = Reply.Protocol.Parse(response);
                ToConsoleWithColor("(<-" + response + ")", ConsoleColor.DarkGray);
                ToConsoleWithColor("Response>" + r.ToString(), ConsoleColor.Yellow);
            }

            static string RequestWithProtocol(string protocol) {
                ToConsoleWithColor("(->" + protocol + ")", ConsoleColor.DarkGray);
                return protocol;
            }

            static void ToConsoleWithColor(string text, ConsoleColor color) {
                try {
                    Console.ForegroundColor = color;
                    Console.WriteLine(text);
                } finally {
                    Console.ResetColor();
                }
            }
        }
	}
}
