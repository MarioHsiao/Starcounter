﻿using Starcounter;
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

                // Waiting until host is available.
                Boolean hostIsReady = false;
                Console.Write("Waiting for the host to be ready");

                Response resp;

                for (Int32 i = 0; i < 10; i++) {

                    resp = X.POST("http://" + settings.ServerIp + ":" + settings.ServerEchoPort + "/echotest", "Test!", null, 5000);

                    if ((200 == resp.StatusCode) && ("Test!" == resp.Body)) {

                        hostIsReady = true;
                        break;
                    }

                    Thread.Sleep(3000);
                    Console.Write(".");
                }

                Console.WriteLine();

                if (!hostIsReady)
                    throw new Exception("Host is not ready by some reason!");

                Node localNode = new Node(settings.ServerIp, settings.ServerPort);

                for (Byte i = 1; i < settings.NumberOfSchedulersToUse; i++) {

                    Console.WriteLine("Starting looping scheduler number " + i + "...");

                    resp = localNode.GET("/loop/" + i, "SchedulerId: " + i + "\r\n" + "LoopHost: True\r\n");

                    if (!resp.IsSuccessStatusCode)
                        throw new ArgumentOutOfRangeException("Loop creation response is not successful.");

                    for (Int32 t = 0; t < 5; t++) {
                        Console.Write(".");
                        Thread.Sleep(1000);
                    }

                    resp = localNode.GET("/loopstats/0", "SchedulerId: 0\r\n");

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