using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FileTestClient {

    class Program {

        static Int32 Main(string[] args) {

            // Starting server side application.
            Int32 exitCode = -1;
            try {
                Console.WriteLine("Starting server side code...");
                exitCode = Diagnostics.StartProcessAndWaitForExit("star.exe", "..\\..\\..\\StreamBodyTestServer.cs", 60000);
            } catch (Exception exc) {
                Console.WriteLine(exc.ToString());
            }

            if (exitCode != 0) {
                Console.WriteLine("Can't properly start test server side code.");
                return 1;
            }

            Int32 numWorkers = 4;
            Int32 requestsEachWorker = 100000;
            if (args.Length > 0) {
                requestsEachWorker = Int32.Parse(args[0]);
            }

            Int32 finished = 0;

            Console.WriteLine("Starting client side: workers {0}, requests each worker {1}.", numWorkers, requestsEachWorker);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var tasks = new List<Task>();

            for (int i = 0; i < numWorkers; i++) {

                tasks.Add(Task.Run(async () => {

                    var random = new Random(Thread.CurrentThread.ManagedThreadId);
                    var client = new HttpClient();
                    client.Timeout = new TimeSpan(0, 0, 10);

                    for (int j = 0; j < requestsEachWorker; j++) {

                        Int32 streamId = random.Next(1, 5);

                        using (var response = await client.GetAsync("http://localhost:8080/streamtest/" + streamId)) {

                            if (response.IsSuccessStatusCode) {

                                using (var rs = await response.Content.ReadAsStreamAsync()) {

                                    using (var ms = new MemoryStream()) {
                                        await rs.CopyToAsync(ms);

                                        Int32 correctRespLen = 0;
                                        Byte correctChar = 0;

                                        switch (streamId) {
                                            case 1: {
                                                correctRespLen = 1000;
                                                correctChar = (Byte)'a';
                                                break;
                                            }

                                            case 2: {
                                                correctRespLen = 1000 * 10;
                                                correctChar = (Byte)'b';
                                                break;
                                            }

                                            case 3: {
                                                correctRespLen = 1000 * 100;
                                                correctChar = (Byte)'c';
                                                break;
                                            }

                                            case 4: {
                                                correctRespLen = 1000 * 1000;
                                                correctChar = (Byte)'d';
                                                break;
                                            }

                                            case 5: {
                                                correctRespLen = 1000 * 10000;
                                                correctChar = (Byte)'e';
                                                break;
                                            }

                                            default: {
                                                throw new ArgumentOutOfRangeException("Wrong stream Id number: " + streamId);
                                            }
                                        }

                                        // Checking that response has correct length.
                                        if (ms.Length != correctRespLen) {
                                            throw new ArgumentOutOfRangeException("Wrong response length: " + ms.Length + ". Should be: " + correctRespLen);
                                        }

                                        // Checking that every response byte is correct.
                                        ms.Seek(0, SeekOrigin.Begin);
                                        for (Int32 k = 0; k < ms.Length; k++) {

                                            Int32 s = ms.ReadByte();
                                            if (s != (Int32)correctChar) {
                                                throw new ArgumentOutOfRangeException("Wrong character in stream!");
                                            }
                                        }
                                    }
                                    finished++;

                                    if (finished % 1000 == 0) {
                                        Console.WriteLine("Finished: " + finished);
                                    }

                                }
                            } else {
                                throw new ArgumentOutOfRangeException("Insuccessful response status code: " + response.StatusCode);
                            }
                        }
                    }
                }));
            }

            try {
                Task.WaitAll(tasks.ToArray());
            } catch (AggregateException ae) {
                foreach (var e in ae.InnerExceptions) {
                    Console.WriteLine(e.Message);
                }
                return 1;
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return 1;
            }

            sw.Stop();
            Console.WriteLine("{0} reqs in {1} s ({2} rps)",
                numWorkers * requestsEachWorker,
                sw.ElapsedMilliseconds / 1000.0,
                (double)(numWorkers * requestsEachWorker) / sw.ElapsedMilliseconds * 1000.0);

            Console.WriteLine("Test finished successfully!");

            return 0;
        }
    }
}