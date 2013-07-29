using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Starcounter.Administrator.FrontEndAPI {
    internal static partial class FrontEndAPI {

        public static void ServerLog_GET(ushort port) {


            #region Log (/adminapi/v1/server/log)

            Handle.GET("/api/admin/log?{?}", (string parameters, Request req) => {

                lock (LOCK) {
                    try {

                        NameValueCollection collection = System.Web.HttpUtility.ParseQueryString(parameters);

                        LogApp logApp = new LogApp();

                        #region Set Filter
                        Boolean filter_debug;
                        Boolean.TryParse(collection["debug"], out filter_debug);
                        logApp.FilterDebug = filter_debug;

                        Boolean filter_notice;
                        Boolean.TryParse(collection["notice"], out filter_notice);
                        logApp.FilterNotice = filter_notice;

                        Boolean filter_warning;
                        Boolean.TryParse(collection["warning"], out filter_warning);
                        logApp.FilterWarning = filter_warning;

                        Boolean filter_error;
                        Boolean.TryParse(collection["error"], out filter_error);
                        logApp.FilterError = filter_error;
                        #endregion

                        logApp.RefreshLogEntriesList();
                        return logApp;
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Returns the log
            Handle.GET("/api/admin/log", (Request req) => {
                lock (LOCK) {

                    try {

                        LogApp logApp = new LogApp() { FilterDebug = false, FilterNotice = false, FilterWarning = true, FilterError = true };
                        logApp.RefreshLogEntriesList();
                        return logApp;
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            #endregion







        }

    }
}
