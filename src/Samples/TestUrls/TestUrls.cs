using Starcounter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NodeTest {
    class NodeTest {

        static Int32 NumRequestsPerUrl = 100000;

        static String ServerIp = "127.0.0.1";

        static UInt16 ServerPort = 8181;

        static Request[] RequestsToTest = new Request[] {

            /*new Request {
                Uri =
                  "/sf2.gif?/api/tracker?url=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                HeadersDictionary = new Dictionary<String, String>
                {
                  {"Cookie", "X-Mapping-fjhppofk=41F28B624CBF9949D80949A214991F1C"},
                  {
                    "Referer",
                    "http://www.hemnet.se/bostad/fritidshus-3rum-ramshyttan-borlange-kommun-sangenvagen-353-6133672"
                  },
                  {"DNT", "1"}
                }
            }*/

            new Request { Uri = "/gwtest" },
            new Request { Uri = "/test" },
            new Request { Uri = "/index.html" },
            new Request { Uri = "/" },
            new Request { Uri = "/app/services/ServerService.js" },
            new Request { Uri = "/app/partials/databases.html" },
            new Request { Uri = "/api/server" },
            new Request { Uri = "/api/admin/applications" },
            new Request { Uri = "/app/js/app.js" },
            new Request { Uri = "/app/services/LogService.js" },
            new Request { Uri = "/app/partials/applications.html" }
        };

        static void ReadGETUrlsFromFile(String filePath) {

            String[] urls = File.ReadAllLines(filePath);

            RequestsToTest = new Request[urls.Length];

            for (Int32 i = 0; i < urls.Length; i++) {

                if ((urls[i].Length < 1) || (!urls[i].StartsWith("/"))) {
                    throw new ArgumentException("Wrong input URL string: " + urls[i]);
                }

                RequestsToTest[i] = new Request {
                    Uri = urls[i]
                };
            }
        }

        static Int32 RunTest(Boolean useAggregation) {

            Console.WriteLine(String.Format("Starting test on {0}:{1} with aggregation flag {2} and NumRequestsPerUrl {3}.",
                ServerIp, ServerPort, useAggregation, NumRequestsPerUrl));

            Int32 numOk = 0, numFailed = 0, numProcessed = 0, counter = 0;

            Stopwatch sw = new Stopwatch();

            Node node = new Node(ServerIp, ServerPort, 0, useAggregation);

            try {

                sw.Start();

                // Repeating needed amount of times.
                for (Int32 i = 0; i < NumRequestsPerUrl; i++) {

                    // Going through all requests.
                    for (Int32 n = 0; n < RequestsToTest.Length; n++) {

                        Int32 m = n;

                        // Doing asynchronous node call.
                        node.CustomRESTRequest(RequestsToTest[n], null, (Response resp, Object userObject) => {

                            lock (RequestsToTest) {

                                // Checking response status code.
                                if (resp.IsSuccessStatusCode) {
                                    numOk++;
                                } else {
                                    numFailed++;
                                }

                                numProcessed++;
                            }
                        });

                        // Printing some info.
                        counter++;
                        if (10000 == counter) {

                            // Sleeping some time so the server is able to process requests.
                            Thread.Sleep(1);

                            lock (RequestsToTest) {

                                Console.WriteLine(String.Format("Responses HTTP 2XX: {0}, NOT 2XX: {1}, RPS: {2}.",
                                    numOk, numFailed, (Int32)(numProcessed * 1000.0 / sw.ElapsedMilliseconds)));
                            }
                            counter = 0;
                        }
                    }
                }

                Boolean allProcessed = false;

                // Waiting for some time to receive all responses.
                for (Int32 i = 0; i < 100; i++) {

                    Thread.Sleep(1000);

                    lock (RequestsToTest) {

                        if (numProcessed >= NumRequestsPerUrl * RequestsToTest.Length) {
                            allProcessed = true;
                            break;
                        }
                    }
                }

                if (allProcessed) {

                    sw.Stop();

                    Console.WriteLine("Test finished!");

                    Console.WriteLine(String.Format("Responses HTTP 2XX: {0}, NOT 2XX: {1}, RPS: {2}.",
                        numOk, numFailed, (Int32)(numProcessed * 1000.0 / sw.ElapsedMilliseconds)));

                } else {

                    Console.WriteLine("Failed to wait for correct number of responses: received {0} out of {1}.",
                        numProcessed, NumRequestsPerUrl * RequestsToTest.Length);

                    return 1;
                }

            } catch (Exception exc) {

                Console.Error.WriteLine(exc.ToString());

                return 1;

            } finally {

                if (useAggregation) {
                    node.StopAggregation();
                }
            }

            return 0;
        }

        static Int32 Main(string[] args) {

            //Debugger.Launch();

            Console.WriteLine("Usage: TestUrls.exe --ServerIp=127.0.0.1 --ServerPort=8080 --UrlsFile=urls.txt --NumRequestsPerUrl=100000");
            Console.WriteLine();

            String urlsFileName = "urls.txt";

            // Parsing parameters.
            foreach (String arg in args) {
                if (arg.StartsWith("--ServerIp=")) {
                    ServerIp = arg.Substring("--ServerIp=".Length);
                } else if (arg.StartsWith("--ServerPort=")) {
                    ServerPort = UInt16.Parse(arg.Substring("--ServerPort=".Length));
                } else if (arg.StartsWith("--UrlsFile=")) {
                    urlsFileName = arg.Substring("--UrlsFile=".Length);
                } else if (arg.StartsWith("--NumRequestsPerUrl=")) {
                    NumRequestsPerUrl = Int32.Parse(arg.Substring("--NumRequestsPerUrl=".Length));
                }
            }

            // Checking if we have an input file.
            if (File.Exists(urlsFileName)) {
                Console.WriteLine("Using input URLs file: " + urlsFileName);
                ReadGETUrlsFromFile(urlsFileName);
            }

            Int32 errCode;
            
            // Running with async node.
            errCode = RunTest(false);
            if (0 != errCode)
                return errCode;

            // Running with aggregation.
            errCode = RunTest(true);
            if (0 != errCode)
                return errCode;

            return errCode;
        }
    }
}
