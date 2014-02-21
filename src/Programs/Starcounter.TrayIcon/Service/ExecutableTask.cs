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
    internal class ExecutablesTask {


        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="executablesArgs"></param>
        public static void Execute(StarcounterWatcher service, out ExecutablesEventArgs executablesArgs) {

            string url = string.Format("{0}:{1}{2}", service.IPAddress, service.Port, "/api/admin/executables");

            Response response;
            // Example JSON response
            //{
            //  "Executables":[
            //      {
            //          "Uri": "http://example.com/api/executables/foo/foo.exe-123456789",
            //          "Path": "C:\\path\to\\the\\exe\\foo.exe",
            //          "ApplicationFilePath" : "C:\\optional\\path\to\\the\\input\\file.cs",
            //          "Name" : "Name of the application",
            //          "Description": "Implements the Foo module",
            //          "Arguments": [{"dummy":""}],
            //          "DefaultUserPort": 1,
            //          "ResourceDirectories": [{"dummy":""}],
            //          "WorkingDirectory": "C:\\path\\to\\default\\resource\\directory",
            //          "IsTool":false,
            //          "StartedBy": "Per Samuelsson, per@starcounter.com",
            //          "Engine": {
            //              "Uri": "http://example.com/api/executables/foo"
            //          },
            //          "RuntimeInfo": {
            //             "LoadPath": "\\relative\\path\\to\\weaved\\server\\foo.exe",
            //                "Started": "ISO-8601, e.g. 2013-04-25T06.24:32",
            //                "LastRestart": "ISO-8601, e.g. 2013-04-25T06.49:01"
            //          }            
            //      }
            //  ]
            //}
            X.GET(url, out response, null, 10000);

            if (response.IsSuccessStatusCode) {

                //List<Executable> executables = new List<Executable>();

                try {
                    //dynamic incomingJson = DynamicJson.Parse(response.Body);
                    //bool bValid = incomingJson.IsDefined("Executables");
                    //if (bValid) {

                        Executables executables = new Executables();
                        executables.PopulateFromJson(response.Body);
                        executablesArgs = new ExecutablesEventArgs() { Executables = executables };

                        //foreach (Engine.ExecutablesJson.ExecutingElementJson application in incomingJson.Executables) {
                        //}

                        //// TODO: Try to use JSON directly
                        //foreach (var item in incomingJson.Executables) {
                        //    Executable executable = new Executable();
                        //    executable.Name = item.Name;
                        //    executable.Path = item.Path;
                        //    executable.Uri = item.Uri;
                        //    executable.RuntimeInfo.Started = item.RuntimeInfo.Started;          //"ISO-8601, e.g. 2013-04-25T06.24:32"
                        //    executable.RuntimeInfo.LastRestart = item.RuntimeInfo.LastRestart;  //"ISO-8601, e.g. 2013-04-25T06.24:32"
                        //    executables.Add(executable);
                        //}
                    //}
                }
                catch (Exception e) {
                    throw new Exception(e.ToString());
                }

                //executablesArgs = new ExecutablesEventArgs() { Executables = executables };

            }
            else {
                throw new TaskCanceledException();
            }
        }


    }
}
