using Starcounter;
using System;
using System.Threading;

namespace NodeTest
{
    class NodeTest
    {
        static String[] UrlsToTest = new String[] {
            "/index.html",
            "/",
            "/app/services/ServerService.js",
            "/app/partials/databases.html",
            "/api/server",
            "/api/admin/applications",
            "/app/js/app.js",
            "/app/services/LogService.js",
            "/app/partials/applications.html"
        };

        static Int32 NumRequests = 100000;

        static Int32 RunTest() {

            Int32 numProcessed = 0;

            try {

                Node node = new Node("127.0.0.1", 8181);

                Console.Write("Starting the test!");

                // Repeating needed amount of times.
                for (Int32 i = 0; i < NumRequests; i++) {

                    // Going through all URLs.
                    for (Int32 n = 0; n < UrlsToTest.Length; n++) {

                        Int32 m = n;

                        // Doing asynchronous node call.
                        node.GET(UrlsToTest[n], null, null, (Response resp, Object userObject) => {

                            // Checking response status code.
                            if (resp.IsSuccessStatusCode) {

                                lock (UrlsToTest) {
                                    numProcessed++;
                                }

                            } else {

                                Console.WriteLine("Wrong HTTP response: " + UrlsToTest[m] + ":" + resp.Body);

                                Environment.Exit(1);
                            }
                        });
                    }
                }

            } catch (Exception exc) {

                Console.Error.WriteLine(exc.ToString());

                Environment.Exit(1);
            }

            Boolean allProcessed = false;

            // Waiting for some time to receive all responses.
            for (Int32 i = 0; i < 10; i++ ) {
                
                Thread.Sleep(1000);

                lock (UrlsToTest) {

                    if (numProcessed >= NumRequests * UrlsToTest.Length) {
                        allProcessed = true;
                        break;
                    }
                }
            }

            if (allProcessed) {
                Console.WriteLine("TestUrls finished successfully! Processed: " + numProcessed);
            } else {
                Console.WriteLine("TestUrls failed with incorrect number of responses: " + numProcessed);
                return 1;
            }

            return 0;
        }

        static Int32 Main(string[] args) {

            //Debugger.Launch();

            return RunTest();
        }
    }
}
