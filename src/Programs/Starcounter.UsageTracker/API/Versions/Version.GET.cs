using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;

namespace StarcounterApplicationWebSocket.API.Versions {
    internal class Version {

        public static void BootStrap(ushort port) {

            Handle.GET(port, "/versions", (Request request) => {

                try {

                    dynamic response = new DynamicJson();

                    var result = Db.SlowSQL("SELECT o FROM VersionSource o WHERE o.BuildError=?", false);

                    response.versions = new object[] { };
                    int i = 0;
                    foreach (VersionSource item in result) {
                        response.versions[i++] = new {
                            version = item.Version,
                            channel = item.Channel
                        };
                    }

                    return new Response() { Body = response.ToString(), StatusCode = (ushort)System.Net.HttpStatusCode.OK };

                }
                catch (Exception e) {
                    return new Response() { Body = e.ToString(), StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError };
                }

            });

        }
    }
}
