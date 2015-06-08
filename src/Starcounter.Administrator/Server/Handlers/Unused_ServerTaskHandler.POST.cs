//using System;
//using Starcounter.Administrator.Server.Utilities;
//using Administrator.Server.Managers;
//using Administrator.Server.Model;

//namespace Starcounter.Administrator.Server.Handlers {
//    internal static partial class StarcounterAdminAPI {

//        /// <summary>
//        /// Register Application GET
//        /// </summary>
//        public static void ServerTaskHandler_POST(ushort port,  string appStoreHost, string imageResourceFolder) {

//            // Server task's
//            Handle.POST(port, "/api/admin/servers/{?}/task", (string name, Request request) => {

//                try {

//                    Representations.JSON.ApplicationTask task = new Representations.JSON.ApplicationTask();
//                    task.PopulateFromJson(request.Body);

//                    if (string.Equals("download", task.Type, StringComparison.InvariantCultureIgnoreCase)) {

//                        throw new NotImplementedException("Download");

//                        //// Download Application
//                        //Response response;
//                        //AppStoreApplication application = AppStoreManager.GetApplication(appStoreHost, task.ID, out response);
//                        //if (application != null) {
//                        //    DeployManager.Download(application, appsRootFolder, imageResourceFolder, out response);
//                        //}

//                        //return response;
//                    }
//                    else if (string.Equals("delete", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
//                        throw new NotImplementedException("Delete");

//                        //// Delete Application
//                        //Response response;
//                        //// TODO: Database?
//                        //DatabaseApplication application = ApplicationManager.GetApplication("TODO", task.ID);
//                        //if (application == null) {
//                        //    return 404; // TODO:
//                        //}

//                        //DeployManager.Delete(application, imageResourceFolder, out response);
//                        //return response;
//                    }
//                    else if (string.Equals("upgrade", task.Type, StringComparison.InvariantCultureIgnoreCase)) {

//                        // TODO:
//                        // Upgrade Application
//                        //Response response;
//                        //AppStoreApplication application = AppStoreManager.GetApplication(appStoreHost, task.ID, out response);

//                        //if (application != null) {
//                        //    DeployManager.Upgrade(application, appsRootFolder, imageResourceFolder, out response);
//                        //}

//                        //return response;
//                        throw new NotImplementedException("Upgrade");
//                    }

//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.BadRequest };
//                }
//                catch (InvalidOperationException e) {

//                    ErrorResponse errorResponse = new ErrorResponse();
//                    errorResponse.Text = e.Message;
//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
//                }
//                catch (Exception e) {

//                    return RestUtils.CreateErrorResponse(e);
//                }
//            });
//        }
//    }
//}
