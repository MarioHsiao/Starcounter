using Starcounter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace NodeTest {
    class NodeTest {
        static Int32 NumRequests = 100000;

        static Request[] RequestsToTest = new Request[] {
            /*new Request {
                Uri =
                  "/sf2.gif?/api/tracker?url=http://www.hemnet.se/bostad/fritidshus-3rum-ramshyttan-borlange-kommun-sangenvagen-353-6133672&time=0",
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

            //new Request { Uri = "/gwtest" },
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

        static Int32 RunTest(Boolean useAggregation) {

            Console.WriteLine("Starting test with aggregation flag: " + useAggregation);

            Int32 numProcessed = 0;
            Stopwatch sw = new Stopwatch();

            Node node = new Node("127.0.0.1", 8181, 0, useAggregation);

            try {

                Console.WriteLine("Starting the test!");

                sw.Start();

                // Repeating needed amount of times.
                for (Int32 i = 0; i < NumRequests; i++) {

                    // Going through all URLs.
                    for (Int32 n = 0; n < RequestsToTest.Length; n++) {

                        Int32 m = n;

                        // Doing asynchronous node call.
                        node.CustomRESTRequest(RequestsToTest[n], null, (Response resp, Object userObject) => {

                            // Checking response status code.
                            if (resp.IsSuccessStatusCode) {

                                lock (RequestsToTest) {
                                    numProcessed++;
                                }

                            } else {

                                Console.WriteLine("Wrong HTTP response: " + RequestsToTest[m].Uri + ":" + resp.Body);

                                Environment.Exit(1);
                            }
                        });
                    }
                }

                Boolean allProcessed = false;

                // Waiting for some time to receive all responses.
                for (Int32 i = 0; i < 100; i++) {

                    Thread.Sleep(1000);

                    lock (RequestsToTest) {
                        if (numProcessed >= NumRequests * RequestsToTest.Length) {
                            allProcessed = true;
                            break;
                        }
                    }
                }

                if (allProcessed) {
                    sw.Stop();
                    Console.WriteLine("TestUrls finished successfully! Processed: " + numProcessed + ". RPS: " +
                        numProcessed * 1000.0 / sw.ElapsedMilliseconds);
                } else {
                    Console.WriteLine("TestUrls failed with incorrect number of responses: " + numProcessed);
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

            Int32 errCode = RunTest(false);
            if (0 != errCode)
                return errCode;

            RunTest(true);
            if (0 != errCode)
                return errCode;

            return errCode;
        }
    }
}
