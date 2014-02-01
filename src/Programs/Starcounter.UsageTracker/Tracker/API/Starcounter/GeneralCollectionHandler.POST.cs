using System;
using Starcounter;
using Starcounter.Advanced;

namespace Starcounter.Applications.UsageTrackerApp.API.Starcounter {
    internal static class GeneralCollectionHandler {

        private static Object LOCK = new Object();

        public static void Setup_POST(ushort port) {

            Handle.POST(port, "/api/usage/general", (Request request) => {
                lock (LOCK) {

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotImplemented };

                }
            });

        }

    }
}
