// ***********************************************************************
// <copyright file="InternalConsoleServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Starcounter.ABCIPC.Internal {

    /// <summary>
    /// Encapsulates the functionality used when utilizing the console to
    /// control a <see cref="Server"/>.
    /// </summary>
    internal static class InternalConsoleServer {
        // All parameterized messages allows a certain syntax
        // for invoking the server with NULL.
        const string NULL_PARAMETER = "$0";

        /// <summary>
        /// Creates the server.
        /// </summary>
        /// <returns></returns>
        internal static Server Create() {
            Usage();
            return new Server(ReadTextRequestFromConsole, WriteTextResponseToConsole);
        }

        /// <summary>
        /// Writes a usage message.
        /// </summary>
        static void Usage() {
            Console.WriteLine("The server prompt support requests with and without parameters.");
            Console.WriteLine("Requests are case-insensitive.");
            Console.WriteLine("To send NULL to parameterized requests, use \"{0}\".", NULL_PARAMETER);
            Console.WriteLine("? shows this help and !cls clears the screen.");
            Console.WriteLine("Hit [ENTER] on a blank line to exit.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  ping                           (No parameter request)");
            Console.WriteLine("  echo hello                     (string parameter request)");
            Console.WriteLine("  arraycount [1,2,3,4]           (string[] parameter request)");
            Console.WriteLine("  dictcount <key=val,key2=val2>  (Dictionary<string,string> parameter request)");
            Console.WriteLine();
        }

        
        /// <summary>
        /// Can be installed in servers that want to offer a simple
        /// text based input/output alternative, using the console.
        /// </summary>
        /// <remarks>
        /// Install when creating new server, like new Server(ReadTextRequestFromConsole, WriteTextResponseToConsole);
        /// </remarks>
        /// <returns></returns>
        static string ReadTextRequestFromConsole() {
            while (true) {
                Console.Write("Request>");
                string read = Console.ReadLine();

                string protocol;

                // Never return anything until we have properly parsed it!
                // And remember, we can write to the console here to!
                // TODO:

                read = read.Trim();
                if (read.Equals(string.Empty)) {
                    protocol = Request.Protocol.ShutdownRequest;
                    return RequestWithProtocol(protocol);
                } else if (read.Equals("?")) {
                    Usage();
                    continue;
                } else if (read.StartsWith("!")) {
                    read = read.Substring(1);
                    if (read.Equals("cls", StringComparison.InvariantCultureIgnoreCase)) {
                        Console.Clear();
                        continue;
                    }

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

                if (parameters.StartsWith("[")) {
                    string[] array;
                    // Call with string array parameters
                    // Implements: client.Send(string, string[]);

                    parameters = parameters.Trim('[', ']');
                    if (parameters.Equals(NULL_PARAMETER)) {
                        protocol = Request.Protocol.MakeRequestStringWithStringArrayNULL(message);
                    } else {
                        array = parameters.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        protocol = Request.Protocol.MakeRequestStringWithStringArray(message, array);
                    }

                    return RequestWithProtocol(protocol);
                } else if (parameters.StartsWith("<")) {
                    // Call with dictionary parameters
                    // Implements: client.Send(string, Dictionary<string, string>);
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    parameters = parameters.Trim('<', '>');

                    if (parameters.Equals(NULL_PARAMETER)) {
                        protocol = Request.Protocol.MakeRequestStringWithDictionaryNULL(message);
                    } else {
                        string[] keyValues = parameters.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var item in keyValues) {
                            dict.Add(item.Substring(0, item.IndexOf('=')), item.Substring(item.IndexOf('=') + 1));
                        }
                        protocol = Request.Protocol.MakeRequestStringWithDictionary(message, dict);
                    }

                    return RequestWithProtocol(protocol);
                } else {
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

        static void WriteTextResponseToConsole(string response, bool endsRequest) {
            var r = Reply.Protocol.Parse(response);
            ToConsoleWithColor("(<-" + response + ")", ConsoleColor.DarkGray);
            ToConsoleWithColor("Response>" + r.ToString(), r.IsSuccess ? ConsoleColor.Green : ConsoleColor.Red);
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
