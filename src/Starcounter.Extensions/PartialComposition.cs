using System;
using System.Text;

namespace Starcounter {

    namespace MergedPartial {

        [Database]
        public class Composition {
            public string Key;
            public string Value;
            public string Version;

            public static void CreateIndex() {

                if (Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE i.Name = ?", "PartialCompositionIndex").First == null) {
                    Starcounter.Db.SQL("CREATE UNIQUE INDEX PartialCompositionIndex ON Starcounter.MergedPartial.Composition (Key ASC)");
                }
            }

            public static Composition GetUsingKey(string key) {
                return Db.SQL<Composition>("SELECT c FROM Starcounter.MergedPartial.Composition c WHERE c.Key = ?", key).First;
            }

            public static Composition GetUsingKeyAndVersion(string key, string version) {
                if (String.IsNullOrEmpty(version)) {
                    return Db.SQL<Composition>("SELECT c FROM Starcounter.MergedPartial.Composition c WHERE c.Key = ?", key).First;
                } else {
                    return Db.SQL<Composition>("SELECT c FROM Starcounter.MergedPartial.Composition c WHERE c.Key = ? AND c.Version = ?", key, version).First;
                }
            }

            public static void Register() {

                Composition.CreateIndex();

                Handle.POST("/sc/partial/composition?key={?}&ver={?}", (Request request, string key, string version) => {
                    Db.Transact(() => {
                        var setup = Composition.GetUsingKeyAndVersion(key, version);
                        if (setup == null) {
                            setup = new Composition() {
                                Key = key,
                                Version = version
                            };
                        }
                        setup.Value = request.Body;
                    });
                    return 204;
                }, new HandlerOptions() { SkipRequestFilters = true });

                Handle.GET("/sc/partial/composition?key={?}&ver={?}", (string key, string version) => {
                    var setup = Composition.GetUsingKeyAndVersion(key, version);
                    if (setup == null)
                        return 404;

                    Response response = new Response();
                    response.ContentType = "application/json";
                    response.Body = setup.Value;
                    response.StatusCode = 200;
                    return response;
                }, new HandlerOptions() { SkipRequestFilters = true });

                Handle.DELETE("/sc/partial/composition?key={?}&ver={?}", (string key, string version) => {
                    Db.Transact(() => {
                        if (key == "all") {
                            Db.SlowSQL("DELETE FROM Starcounter.MergedPartial.Composition");
                        } else {
                            var setup = Composition.GetUsingKeyAndVersion(key, version);
                            if (setup != null) {
                                setup.Delete();
                            }
                        }
                    });
                    return 204;
                }, new HandlerOptions() { SkipRequestFilters = true });

                Handle.GET("/sc/generate/partial/composition/{?}", (string app) => {
                    string sql = "SELECT i FROM Starcounter.MergedPartial.Composition i WHERE i.Key LIKE ?";
                    StringBuilder sb = new StringBuilder();
                    int index = 0;

                    app = "/" + app + "/%";
                    sb.Append("INSERT INTO Starcounter.MergedPartial.Composition(\"Key\",\"Value\") VALUES").Append(Environment.NewLine);

                    foreach (Composition item in Db.SQL<Composition>(sql, app)) {
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
}