using Codeplex.Data;
using Starcounter.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;

namespace Starcounter.Tools.Service.Task {
    internal class NetworkTask {


        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="appList"></param>
        public static void Execute(StarcounterWatcher service, out Dictionary<string, IList<int>> appList) {

            appList = new Dictionary<string, IList<int>>();

            string url = string.Format("http://{0}:{1}{2}", service.IPAddress, service.Port, "/gwstats");

            // Example JSON response
            // {
            //    "ports": [
            //        { 
            //            "port": 8181,
            //            "acceptSockets": 1,
            //            "activeSockets": 9,
            //            "registeredUris": [
            //                {
            //                    "uri": "GET /test",
            //                    "location": "DEFAULT",
            //                    "application": "dummyApp"
            //                }
            //            ]
            //        }
            //    ],
            //    "databases": [
            //        {
            //            "name": "DEFAULT",
            //            "index": 1,
            //            "overflowChunks": 0
            //        }
            //    ],
            //    "workers": [
            //        {
            //            "id": 0,
            //            "bytesReceived": 22129,
            //            "packetsReceived": 63,
            //            "bytesSent": 349921,
            //            "packetsSent": 74,
            //            "allocatedChunks": "2, 11, 1, 6"
            //        }
            //    ],
            //    "globalStatistics": {
            //        "allWorkersLastSecond": {
            //            "httpRequests": 1,
            //            "receivedTimes": 1,
            //            "receiveBandwidth": 0.0006, // mbit/s
            //            "sentTimes": 1,
            //            "sendBandwidth": 0.007768 // mbit/s 
            //        }
            //    }
            //}

            Response response = Http.GET(url, null, 10000);

            if (response.IsSuccessStatusCode) {


                try {

                    dynamic incomingJson = DynamicJson.Parse(response.Body);
                    
                    foreach (var port in incomingJson.ports) {

                        foreach (var registeredUris in port.registeredUris) {

                            string applicationName = registeredUris.application as string;
                            int portNumber = (int)port.port;

                            if (appList.ContainsKey(applicationName)) {
                                // App port
                                if (!appList[applicationName].Contains(portNumber)) {
                                    appList[applicationName].Add(portNumber);
                                }
                            }
                            else {
                                List<int> portList = new List<int>();
                                portList.Add(portNumber);
                                appList.Add(applicationName, portList);
                            }
                        }
                    }
             
                }
                catch (Exception e) {
                    throw new Exception(e.ToString());
                }

            }
            else {
                throw new TaskCanceledException();
            }
        }


    }
}
