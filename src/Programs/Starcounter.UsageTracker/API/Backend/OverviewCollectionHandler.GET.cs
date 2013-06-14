using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void Overview_GET(ushort port) {

            Handle.GET(port, "/admin/overview", (Request request) => {
                lock (LOCK) {
                    dynamic response = new DynamicJson();
                    try {
                        Int64 start_num = Db.SlowSQL("SELECT count(o) FROM InstallerStart o").First;
                        Int64 executing_num = Db.SlowSQL("SELECT count(o) FROM InstallerExecuting o").First;
                        Int64 finish_num = Db.SlowSQL("SELECT count(o) FROM InstallerFinish o").First;
                        Int64 end_num = Db.SlowSQL("SELECT count(o) FROM InstallerEnd o").First;
                        Int64 abort_num = Db.SlowSQL("SELECT count(o) FROM InstallerAbort o").First;

                        //                        response.overview = new object[] { };

                        response.overview = new {
                            start = start_num,
                            executing = executing_num,
                            finish = finish_num,
                            end = end_num,
                            abort = abort_num

                        };

                        return new Response() { Body = response.ToString(), StatusCode = (ushort)System.Net.HttpStatusCode.OK };

                    }
                    catch (Exception e) {
                        return Utils.CreateErrorResponse(e);
                    }
                }
            });

        }

    }
}
