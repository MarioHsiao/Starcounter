
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
                        // Implements: client.Send(string)
                        var r = Request.Protocol.MakeRequestStringWithoutParameters(read);
                        ToConsoleWithColor("(->" + r + ")", ConsoleColor.DarkGray);
                        return r;
                    }

                    // We've got a command and some additional stuff on the command line.
                    // Try interpret it.

                    Console.Beep();
                }
            }

            public static void WriteTextResponseToConsole(string response) {
                var r = Reply.Protocol.Parse(response);
                ToConsoleWithColor("(<-" + response + ")", ConsoleColor.DarkGray);
                ToConsoleWithColor("Response>" + r.ToString(), ConsoleColor.Yellow);
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
