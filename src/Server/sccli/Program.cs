
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sccli {
    class Program {
        const string pipeName = "referenceserver";
        
        static Dictionary<string, Action<Client, string[]>> supportedCommands;
        static Program() {
            supportedCommands = new Dictionary<string, Action<Client, string[]>>();
            supportedCommands.Add("help", (c, args) => {
                var s = "Supported commands:" + Environment.NewLine;
                foreach (var item in supportedCommands) {
                    s += "  *" + item.Key + Environment.NewLine;
                }
                s += Environment.NewLine;
                ToConsoleWithColor(s, ConsoleColor.Yellow);
            });
            supportedCommands.Add("ping", Program.Ping);
            supportedCommands.Add("getdatabase", Program.GetDatabase);
            supportedCommands.Add("getdatabases", Program.GetDatabases);
            supportedCommands.Add("getserver", Program.GetServerInfo);
            supportedCommands.Add("createdatabase", Program.CreateDatabase);
            supportedCommands.Add("startdatabase", Program.StartDatabase);
            supportedCommands.Add("stopdatabase", Program.StopDatabase);
            supportedCommands.Add("exec", Program.ExecApp);
        }

        static void Main(string[] args) {
            string command;
            Action<Client, string[]> action;

            var client = ClientServerFactory.CreateClientUsingNamedPipes(pipeName);
            
            command = args.Length == 0 ? string.Empty : args[0].ToLowerInvariant();
            if (command.StartsWith("@")) {
                command = command.Substring(1);
                var args2 = new string[args.Length + 1];
                Array.Copy(args, args2, args.Length);
                args2[args2.Length - 1] = "@@Synchronous";
                args = args2;
            }

            if (!supportedCommands.TryGetValue(command, out action)) {
                ToConsoleWithColor(string.Format("Unknown command: {0}", command), ConsoleColor.Red);
                action = supportedCommands["help"];
            }

            try {
                action(client, args);
            } catch (TimeoutException timeout) {
                if (timeout.TargetSite.Name.Equals("Connect")) {
                    ToConsoleWithColor(string.Format("Unable to connect to {0}. Have you started the server?", pipeName), ConsoleColor.Red);
                    return;
                }
                throw;
            }
        }

        static void Ping(Client client, string[] args) {
            client.Send("Ping", (Reply reply) => WriteReplyToConsole(reply));
        }

        static void GetServerInfo(Client client, string[] args) {
            client.Send("GetServerInfo", (Reply reply) => WriteReplyToConsole(reply));
        }

        static void CreateDatabase(Client client, string[] args) {
            var props = new Dictionary<string, string>();
            props["Name"] = args[1];
            if (args.Contains<string>("@@Synchronous")) {
                props["@@Synchronous"] = bool.TrueString;
            }
            client.Send("CreateDatabase", props, (Reply reply) => WriteReplyToConsole(reply));
        }

        static void StartDatabase(Client client, string[] args) {
            var props = new Dictionary<string, string>();
            props["Name"] = args[1];
            if (args.Contains<string>("@@Synchronous")) {
                props["@@Synchronous"] = bool.TrueString;
            }
            client.Send("StartDatabase", props, (Reply reply) => WriteReplyToConsole(reply));
        }

        static void StopDatabase(Client client, string[] args) {
            var props = new Dictionary<string, string>();
            props["Name"] = args[1];
            if (args.Contains<string>("stopdb")) {
                props["StopDb"] = bool.TrueString;
            }
            if (args.Contains<string>("@@Synchronous")) {
                props["@@Synchronous"] = bool.TrueString;
            }
            client.Send("StopDatabase", props, (Reply reply) => WriteReplyToConsole(reply));
        }

        static void ExecApp(Client client, string[] args) {
            var props = new Dictionary<string, string>();
            props["AssemblyPath"] = args[1];
            if (args.Contains<string>("@@Synchronous")) {
                props["@@Synchronous"] = bool.TrueString;
            }
            client.Send("ExecApp", props, (Reply reply) => WriteReplyToConsole(reply));
        }

        static void GetDatabase(Client client, string[] args) {
            client.Send("GetDatabase", args[1], (Reply reply) => WriteReplyToConsole(reply));
        }

        static void GetDatabases(Client client, string[] args) {
            client.Send("GetDatabases", (Reply reply) => WriteReplyToConsole(reply));
        }

        static void WriteReplyToConsole(Reply reply) {
            if (reply.IsResponse) {
                ToConsoleWithColor(reply.ToString(), reply.IsSuccess ? ConsoleColor.Green : ConsoleColor.Red);
            }
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
