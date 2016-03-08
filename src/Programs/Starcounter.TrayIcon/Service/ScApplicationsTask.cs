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
    internal class ScApplicationsTask {


        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="applications"></param>
        public static bool Execute(StarcounterWatcher service, out Executables applications) {

            string url = string.Format("http://{0}:{1}{2}", service.IPAddress, service.Port, "/api/admin/applications");

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
            //             "Started": "ISO-8601, e.g. 2013-04-25T06.24:32",
            //             "LastRestart": "ISO-8601, e.g. 2013-04-25T06.49:01"
            //          }            
            //      }
            //  ]
            //}

            Response response = Http.GET(url, null, 10);

            if (response.IsSuccessStatusCode) {
                applications = new Executables();
                applications.PopulateFromJson(response.Body);
                return true;
            }

            applications = null;

            return false;
        }


    }
}
