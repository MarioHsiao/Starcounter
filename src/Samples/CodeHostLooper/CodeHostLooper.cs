using Starcounter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeHostLooper {

    class Settings {

        public UInt16 ServerPort;
        public UInt16 ServerEchoPort;
        public String ServerIp;
        public Int32 NumberOfSchedulersToUse;

        public void Init(string[] args) {

            foreach (String arg in args) {

                if (arg.StartsWith("-ServerIp=")) {
                    
                    ServerIp = arg.Substring("-ServerIp=".Length);

                } else if (arg.StartsWith("-ServerPort=")) {

                    ServerPort = UInt16.Parse(arg.Substring("-ServerPort=".Length));

                } else if (arg.StartsWith("-ServerEchoPort=")) {

                    ServerEchoPort = UInt16.Parse(arg.Substring("-ServerEchoPort=".Length));

                } else if (arg.StartsWith("-NumberOfSchedulersToUse=")) {

                    NumberOfSchedulersToUse = Int32.Parse(arg.Substring("-NumberOfSchedulersToUse=".Length));
                }
            }
        }
    }

    class Program {
    
        static Int32 Main(string[] args) {
            
            try {

                Settings settings = new Settings() {
                    ServerPort = 12345,
                    ServerEchoPort = 8080,
                    ServerIp = "127.0.0.1",
                    NumberOfSchedulersToUse = Environment.ProcessorCount - 1
                };

                settings.Init(args);

                if (settings.NumberOfSchedulersToUse > Environment.ProcessorCount - 1)
                    throw new ArgumentException("Number of scheduler parameters should be less than number of virtual CPUs.");

                Node localNode = new Node(settings.ServerIp, settings.ServerPort);
                Response resp;

                for (Byte i = 1; i < settings.NumberOfSchedulersToUse; i++) {

                    Console.WriteLine("Starting looping scheduler number " + i + "...");

                    Dictionary<String, String> headers = new Dictionary<String, String> {
                        { "SchedulerId", i.ToString() },
                        { "LoopHost", "True" }
                    };

                    resp = localNode.GET("/loop/" + i, headers);

                    if (!resp.IsSuccessStatusCode)
                        throw new ArgumentOutOfRangeException("Loop creation response is not successful.");

                    for (Int32 t = 0; t < 5; t++) {
                        Console.Write(".");
                        Thread.Sleep(1000);
                    }

                    headers = new Dictionary<String, String> {
                        { "SchedulerId", "0" }
                    };

                    resp = localNode.GET("/loopstats/0", headers);

                    if (!resp.IsSuccessStatusCode)
                        throw new ArgumentOutOfRangeException("Loop stats fetch response is not successful.");

                    Console.WriteLine();
                    Console.WriteLine(resp.Body);
                    Console.WriteLine();
                }

                return 0;

            } catch (Exception exc) {

                Console.WriteLine(exc);

                return 1;
            }            
        }
    }
}
