using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void Installation_DELETE(ushort port) {

            Handle.DELETE(port,"/admin/installations/{?}", (string id, Request request) => {
                lock (LOCK) {
                    try {
                        Installation item = null;
                        Db.Transaction(() => {
                            item = Db.SlowSQL("SELECT o FROM installation o where ObjectID=?", id).First;
                            if (item != null) {
                                item.Delete();
                            }
                        });

                        if (item == null) {
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                        }

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK };
                    }
                    catch (Exception e) {
                        return Utils.CreateErrorResponse(e);
                    }

                }
            });


        }

    }
}
