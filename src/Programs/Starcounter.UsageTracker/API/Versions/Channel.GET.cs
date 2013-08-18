using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;

namespace StarcounterApplicationWebSocket.API.Versions {
    internal class Channel {

        public static void BootStrap(ushort port) {

            Handle.GET(port, "/channels", (Request request) => {

                try {

                    dynamic response = new DynamicJson();

                    var result = Db.SlowSQL("SELECT o FROM VersionSource o WHERE o.BuildError=? GROUP BY o.Channel", false);
         
                    response.channels = new object[] { };
                    int i = 0;
                    foreach (VersionSource item in result) {
                        response.channels[i++] = new {
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
