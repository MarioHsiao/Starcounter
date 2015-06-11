//using System;
//using Codeplex.Data;
//using Starcounter;
//using Starcounter.Advanced;
//using Starcounter.Server.PublicModel;
//using System.Net;
//using System.Diagnostics;
//using Starcounter.Internal;
//using Starcounter.Internal.Web;
//using Starcounter.Administrator.API.Utilities;
//using Starcounter.Administrator.API.Handlers;
//using Starcounter.Server.Rest.Representations.JSON;
//using Starcounter.Server.Rest;
//using Starcounter.CommandLine;
//using System.IO;
//using Starcounter.Rest.ExtensionMethods;
//using System.Collections.Generic;
//using Starcounter.Administrator.Server.Utilities;
//using System.Windows.Forms;
//using System.Text;
//using System.Reflection;
//using System.ComponentModel;
//using Administrator.Server.Managers;
//using Administrator.Server.Model;

//namespace Starcounter.Administrator.Server.Handlers {
//    internal static partial class StarcounterAdminAPI {

//        /// <summary>
//        /// Register Application GET
//        /// </summary>
//        public static void InstalledApplication_PUT(ushort port, string appsRootFolder, string appStoreHost, string imageResourceFolder) {

//            //
//            // Install Application zip package (from the body)
//            //
//            Handle.PUT(port, "/api/admin/installed/apps", (Request request) => {

//                try {
//                    using (MemoryStream packageZip = new MemoryStream(request.BodyBytes)) {

//                        string host = request["Host"];

//                        // TODO: Assure that the url is a full url. like file://mypackage.zip or something like that
//                        string url = host;
//                        DeployedConfigFile config;
//                        PackageManager.Unpack(packageZip, url, appsRootFolder, imageResourceFolder, out config);
//                    }

//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created };
//                }
//                catch (Exception e) {
//                    return RestUtils.CreateErrorResponse(e);
//                }
//            });
//        }
//    }
//}
