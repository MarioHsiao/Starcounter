using System;
using System.IO;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Advanced;
using System.Net;

namespace Starcounter {

    [Database]
    public class Layout {
        public string Key;
        public string Value;

        public static void CreateIndex() {

            if (Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE i.Name = ?", "StarcounterLayoutIndex").First == null) {
                Starcounter.Db.SQL("CREATE UNIQUE INDEX StarcounterLayoutIndex ON Starcounter.Layout (Key ASC)");
            }
        }

        public static Layout GetSetup(string key) {
            return Db.SQL<Layout>("SELECT c FROM Starcounter.Layout c WHERE c.Key = ?", key).First;
        }

        public static void Register() {

            Layout.CreateIndex();

            Handle.POST("/sc/layout?{?}", (Request request, string key) => {
                Db.Transact(() => {
                    var setup = Layout.GetSetup(key);
                    if (setup == null)
                        setup = new Layout() { Key = key };
                    setup.Value = request.Body;
                });
                return 204;
            }, new HandlerOptions() { SkipRequestFilters = true });

            Handle.GET("/sc/layout?{?}", (string key) => {
                var setup = Layout.GetSetup(key);
                if (setup == null)
                    return 404;

                Response response = new Response();
                response.ContentType = "application/json";
                response.Body = setup.Value;
                response.StatusCode = 200;
                return response;
            }, new HandlerOptions() { SkipRequestFilters = true });

            Handle.DELETE("/sc/layout?{?}", (string key) => {
                Db.Transact(() => {
                    if (key == "all") {
                        Db.SlowSQL("DELETE FROM Starcounter.Layout");
                    } else {
                        var setup = Layout.GetSetup(key);
                        if (setup != null) {
                            setup.Delete();
                        }
                    }
                });
                return 204;
            }, new HandlerOptions() { SkipRequestFilters = true });

            Handle.GET("/sc/generatestyles/{?}", (string app) => {
                string sql = "SELECT i FROM Starcounter.Layout i WHERE i.Key LIKE ?";
                StringBuilder sb = new StringBuilder();
                int index = 0;

                app = "/" + app + "/%";
                sb.Append("INSERT INTO Starcounter.Layout(\"Key\",\"Value\") VALUES").Append(Environment.NewLine);

                foreach (Layout item in Db.SQL<Layout>(sql, app)) {
                    if (index > 0) {
                        sb.Append(",").Append(Environment.NewLine);
                    }

                    sb.Append("    ('").Append(item.Key).Append("', '").Append(item.Value).Append("')");
                    index++;
                }

                return sb.ToString();
            }, new HandlerOptions() { SkipRequestFilters = true });
        }
    }
}