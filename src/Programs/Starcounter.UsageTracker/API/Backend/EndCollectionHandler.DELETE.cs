using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void End_DELETE(ushort port) {

            Handle.DELETE(port,"/admin/installerend/{?}", (string id, Request request) => {
                lock (LOCK) {
                    try {
                        InstallerEnd item = null;
                        Db.Transaction(() => {
                            item = Db.SlowSQL("SELECT o FROM InstallerEnd o where ObjectID=?", id).First;
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
