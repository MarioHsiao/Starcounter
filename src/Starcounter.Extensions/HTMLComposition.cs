using System;
using System.Text;

namespace Starcounter {

    [Database]
    public class HTMLComposition {
        public string Key;
        public string Value;
        public string Version;

        public static void CreateIndex() {

            if (Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE i.Name = ?", "StarcounterHTMLCompositionIndex").First == null) {
                Starcounter.Db.SQL("CREATE UNIQUE INDEX StarcounterHTMLCompositionIndex ON Starcounter.HTMLComposition (Key ASC)");
            }
        }

        public static HTMLComposition GetUsingKey(string key) {
            return Db.SQL<HTMLComposition>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.Key = ?", key).First;
        }

        public static HTMLComposition GetUsingKeyAndVersion(string key, string version) {
            if (String.IsNullOrEmpty(version)) {
                return Db.SQL<HTMLComposition>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.Key = ?", key).First;
            } else {
                return Db.SQL<HTMLComposition>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.Key = ? AND c.Version = ?", key, version).First;
            }
        }

        public static HTMLComposition GetAll() {
            return Db.SQL<HTMLComposition>("SELECT c FROM Starcounter.HTMLComposition c");
        }

        public static HTMLComposition GetUsingAppOrPartial(string app) {
            return Db.SQL<HTMLComposition>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.Key LIKE ?", app).First;
        }

        public static HTMLComposition GetStandalonePartial(string partial) {
            return Db.SQL<HTMLComposition>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.KEY=?", partial).First;
        }

        public static void Register() {

            HTMLComposition.CreateIndex();

            // Posts a composition to the database
            Handle.POST("/sc/partial/composition?key={?}&ver={?}", (Request request, string key, string version) => {
                key = System.Uri.UnescapeDataString(key);
                Db.Transact(() => {
                    var setup = HTMLComposition.GetUsingKeyAndVersion(key, version);
                    if (setup == null) {
                        setup = new HTMLComposition() {
                            Key = key,
                            Version = version
                        };
                    }
                    setup.Value = request.Body;
                });
                return 204;
            }, new HandlerOptions() { SkipRequestFilters = true });

            // Returns the wanted composition by inserting a key and/or version
            Handle.GET("/sc/partial/composition?key={?}&ver={?}&returntype={?)", (string key, string version, string returnType) => {
                key = System.Uri.UnescapeDataString(key);
                var setup = HTMLComposition.GetUsingKeyAndVersion(key, version);
                if (setup == null)
                    return 404;

                Response response = new Response();
                response.ContentType = returnType == "html" ? "text/html" : "application/json";
                response.Body = setup.Value;
                response.StatusCode = 200;
                return response;
            }, new HandlerOptions() { SkipRequestFilters = true });

            // Deletes an html composition from the database based on the key and/or version
            Handle.DELETE("/sc/partial/composition?key={?}&ver={?}", (string key, string version) => {
                key = System.Uri.UnescapeDataString(key);
                Db.Transact(() => {
                    if (key == "all") {
                        Db.SlowSQL("DELETE FROM Starcounter.HTMLComposition");
                    } else {
                        var setup = HTMLComposition.GetUsingKeyAndVersion(key, version);
                        if (setup != null) {
                            setup.Delete();
                        }
                    }
                });
                return 204;
            }, new HandlerOptions() { SkipRequestFilters = true });

            // Takes as input the name of an app an responds with all compositions connected to that app.
            // Returns all if you set that as the app
            Handle.GET("/sc/partial/composition?app={?}&returntype={?}", (string app) =>
            {
                var setup = app == "all" ? GetAll() : GetUsingAppOrPartial("%/" + app + "/%");

                if (setup == null)
                {
                    return 404;
                }

                Response response = new Response();
                response.ContentType = returnType == "html" ? "text/html" : "application/json";
                response.Body = setup.Value;
                response.StatusCode = 200;
                return response;
            }, new HandlerOptions() { SkipRequestFilters = true });

            // returns compositions for given partials in any set of apps
            Handle.GET("/sc/partial/composition?partial={?}&partial={?}&returntype={?}", (string partialOne, string partialTwo, string returnType) =>
            {
                string partials = "%/" + partialOne + "&" + partialTwo + "/%";
                var setup = GetUsingAppOrPartial(partials);

                if (setup == null)
                {
                    return 404;
                }

                Response response = new Response();
                response.ContentType = returnType == "html" ? "text/html" : "application/json";
                response.Body = setup.Value;
                response.StatusCode = 200;
                return response;

            }, new HandlerOptions() { SkipRequestFilters = true });

            Handle.GET("/sc/partial/composition?standalonepartial={?}&returntype={?}", (string partial, string returnType) =>
            {
                var setup = GetStandalonePartial(partial);

                if (setup == null)
                {
                    return 404;
                }

                Response response = new Response();
                response.ContentType = returnType == "html" ? "text/html" : "application/json";
                response.Body = setup.Value;
                response.StatusCode = 200;
                return response;
            });

            //Handle.GET("/sc/generate/partial/composition/{?}", (string app) => {
            //    string sql = "SELECT i FROM Starcounter.HTMLComposition i WHERE i.Key LIKE ?";
            //    StringBuilder sb = new StringBuilder();
            //    int index = 0;

            //    app = "/" + app + "/%";
            //    sb.Append("INSERT INTO Starcounter.HTMLComposition(\"Key\",\"Value\") VALUES").Append(Environment.NewLine);

            //    foreach (HTMLComposition item in Db.SQL<HTMLComposition>(sql, app))
            //    {
            //        if (index > 0)
            //        {
            //            sb.Append(",").Append(Environment.NewLine);
            //        }

            //        sb.Append("    ('").Append(item.Key).Append("', '").Append(item.Value).Append("')");
            //        index++;
            //    }

            //    return sb.ToString();
            //}, new HandlerOptions() { SkipRequestFilters = true });
        }
    }
}