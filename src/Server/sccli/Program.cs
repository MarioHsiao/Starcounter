using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.ABCIPC;
using System.IO.Pipes;

namespace sccli {
    class Program {
        const string pipeName = "referenceserver";
        static NamedPipeClientStream pipe;

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
        }

        static void Main(string[] args) {
            string command;
            Action<Client, string[]> action;

            var client = new Client(SendRequest, ReceiveReply);
            
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

        static void GetDatabase(Client client, string[] args) {
            client.Send("GetDatabase", args[1], (Reply reply) => WriteReplyToConsole(reply));
        }

        static void GetDatabases(Client client, string[] args) {
            client.Send("GetDatabases", (Reply reply) => WriteReplyToConsole(reply));
        }

        static void SendRequest(string request) {
            pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            pipe.Connect(3000);
            var bytes = Encoding.UTF8.GetBytes(request);
            pipe.Write(bytes, 0, bytes.Length);
        }

        static string ReceiveReply() {
            int length;

            // Replies are prefixed with size first when the
            // reference server operates using named pipes.
            
            length = pipe.ReadByte() * 256;
            length += pipe.ReadByte();
            
            var buffer = new byte[length];
            var count = pipe.Read(buffer, 0, length);
            pipe.Close();

            return Encoding.UTF8.GetString(buffer, 0, count);
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
