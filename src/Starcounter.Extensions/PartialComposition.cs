using System;
using System.Text;

namespace Starcounter {

    [Database]
    public class PartialComposition {
        public string Key;
        public string Value;
        public string Version;

        public static void CreateIndex() {

            if (Db.SQL("SELECT i FROM MaterializedIndex i WHERE i.Name = ?", "PartialCompositionIndex").First == null) {
                Starcounter.Db.SQL("CREATE UNIQUE INDEX PartialCompositionIndex ON Starcounter.PartialComposition (Key ASC)");
            }
        }

        public static PartialComposition GetUsingKey(string key) {
            return Db.SQL<PartialComposition>("SELECT c FROM Starcounter.PartialComposition c WHERE c.Key = ?", key).First;
        }

        public static PartialComposition GetUsingKeyAndVersion(string key, string version) {
            if (String.IsNullOrEmpty(version)) {
                return Db.SQL<PartialComposition>("SELECT c FROM Starcounter.PartialComposition c WHERE c.Key = ?", key).First;
            } else {
                return Db.SQL<PartialComposition>("SELECT c FROM Starcounter.PartialComposition c WHERE c.Key = ? AND c.Version = ?", key, version).First;
            }
        }

        public static void Register() {

            PartialComposition.CreateIndex();

            Handle.POST("/sc/partialcomposition?key={?}&ver={?}", (Request request, string key, string version) => {
                Db.Transact(() => {
                    var setup = PartialComposition.GetUsingKeyAndVersion(key, version);
                    if (setup == null) {
                        setup = new PartialComposition() {
                            Key = key,
                            Version = version
                        };
                    }
                    setup.Value = request.Body;
                });
                return 204;
            }, new HandlerOptions() { SkipRequestFilters = true });

            Handle.GET("/sc/partialcomposition?key={?}&ver={?}", (string key, string version) => {
                var setup = PartialComposition.GetUsingKeyAndVersion(key, version);
                if (setup == null)
                    return 404;

                Response response = new Response();
                response.ContentType = "application/json";
                response.Body = setup.Value;
                response.StatusCode = 200;
                return response;
            }, new HandlerOptions() { SkipRequestFilters = true });

            Handle.DELETE("/sc/partialcomposition?key={?}&ver={?}", (string key, string version) => {
                Db.Transact(() => {
                    if (key == "all") {
                        Db.SlowSQL("DELETE FROM Starcounter.PartialComposition");
                    } else {
                        var setup = PartialComposition.GetUsingKeyAndVersion(key, version);
                        if (setup != null) {
                            setup.Delete();
                        }
                    }
                });
                return 204;
            }, new HandlerOptions() { SkipRequestFilters = true });

            Handle.GET("/sc/generate/partialcomposition/{?}", (string app) => {
                string sql = "SELECT i FROM Starcounter.PartialComposition i WHERE i.Key LIKE ?";
                StringBuilder sb = new StringBuilder();
                int index = 0;

                app = "/" + app + "/%";
                sb.Append("INSERT INTO Starcounter.PartialComposition(\"Key\",\"Value\") VALUES").Append(Environment.NewLine);

                foreach (PartialComposition item in Db.SQL<PartialComposition>(sql, app)) {
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