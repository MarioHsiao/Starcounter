using System;

namespace Starcounter
{

    [Database]
    public class HTMLComposition
    {
        public string Key;
        public string Value;
        public string Version;

        public static void CreateIndex()
        {

            if (Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE i.Name = ?", "StarcounterHTMLCompositionIndex").First == null)
            {
                Starcounter.Db.SQL("CREATE UNIQUE INDEX StarcounterHTMLCompositionIndex ON Starcounter.HTMLComposition (Key ASC)");
            }
        }

        public Response GetResponse(QueryResultRows setup, Request request, string version, string callSource)
        {
            List<string> body = new List<string>();

            if (setup == null)
            {
                return 404;
            }

            if (String.IsNullOrEmpty(version))
            {
                foreach (HTMLComposition composition in setup)
                {
                    body.Add(composition.Value + Environment.NewLine);
                }
            }
            else if (version == "latest")
            {
                body.Add(GetLatestVersion(setup).Value, callSource);
            }
            else
            {
                foreach (HTMLComposition composition in setup)
                {
                    if (composition.Version = version)
                    {
                        body.Add(composition.Value);
                        break;
                    }
                }
                if (body.Count == 0)
                {
                    return 404;
                }
            }
            Response response = new Response();
            response.ContentType = request.Headers["Accept"];
            response.Body = body;
            response.StatusCode = 200;
            return response;
        }

        private HTMLComposition GetLatestVersion(QueryResultRows setup, string callSource)
        {
            if (callSource == "partial")
            {
                return Db.SQL<QueryResultRows>("SELECT c.Key, max(c.Version) FROM Starcounter.HTMLComposition c WHERE c.Key LIKE '%Page.html%' GROUP BY c.Key");
            }

            int highestVersion = 0;
            HTMLComposition latestComposition;

            foreach (HTMLComposition composition in setup)
            {
                int currentVersion = int.Parse(composition.Version);
                HTMLComposition currentComposition = composition;
                if (currentVersion > highestVersion)
                {
                    highestVersion = currentVersion;
                    latestComposition = currentComposition;
                }
            }
            return latestComposition;
        }

        public static HTMLComposition GetUsingKey(string key)
        {
            return Db.SQL<QueryResultRows>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.Key = ?", key);
        }

        public static HTMLComposition GetUsingKeyAndVersion(string key, string version)
        {
            if (String.IsNullOrEmpty(version))
            {
                return Db.SQL<HTMLComposition>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.Key = ?", key).First;
            }
            else
            {
                return Db.SQL<HTMLComposition>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.Key = ? AND c.Version = ?", key, version).First;
            }
        }

        public static HTMLComposition GetAll()
        {
            return Db.SQL<QueryResultRows>("SELECT c FROM Starcounter.HTMLComposition c");
        }

        public static HTMLComposition GetUsingMergedPartials(string mergedPartials)
        {
            return Db.SQL<QueryResultRows>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.KEY=?", mergedPartials);
        }

        public static HTMLComposition GetUsingPartial(string partial)
        {
            return Db.SQL<QueryResultRows>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.KEY LIKE '%?%'", partial);
        }

        public static HTMLComposition GetUsingStandalonePartial(string partial)
        {
            return Db.SQL<QueryResultRows>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.KEY LIKE '?%'", partial);
        }

        public static HTMLComposition GetUsingApp(string app)
        {
            return Db.SQL<QueryResultRows>("SELECT c FROM Starcounter.HTMLComposition c WHERE c.KEY LIKE '%?=%'", app);
        }

        public static void Register()
        {

            HTMLComposition.CreateIndex();

            /// <summary>  
            ///  Posts a new HTMLComposition.  
            /// </summary> 
            Handle.POST("/sc/partial/composition?key={?}&ver={?}", (Request request, string key, string version) => {
                key = System.Uri.UnescapeDataString(key);
                Db.Transact(() => {
                    var setup = GetUsingKeyAndVersion(key, version);
                    if (setup == null)
                    {
                        setup = new HTMLComposition()
                        {
                            Key = key,
                            Version = version
                        };
                    }
                    setup.Value = request.Body;
                });
                return 201;
            }, new HandlerOptions() { SkipRequestFilters = true });

            /// <summary>  
            ///  Deletes a HTMLComposition from the input key and (optionally) version. The key "all" deletes all HTMLCompositions.  
            /// </summary> 
            Handle.DELETE("/sc/partial/composition?key={?}&ver={?}", (string key, string version) => {
                key = System.Uri.UnescapeDataString(key);
                Db.Transact(() => {
                    if (key == "all")
                    {
                        Db.SlowSQL("DELETE FROM Starcounter.HTMLComposition");
                    }
                    else
                    {
                        var setup = GetUsingKeyAndVersion(key, version);
                        if (setup != null)
                        {
                            setup.Delete();
                        }
                    }
                });
                return 204;
            }, new HandlerOptions() { SkipRequestFilters = true });

            /// <summary>  
            ///  Returns an HTMLComposition from the key and/or the version  
            /// </summary> 
            Handle.GET("/sc/partial/composition?key={?}&ver={?}", (Request request, string key, string version) => {
                key = System.Uri.UnescapeDataString(key);
                return GetResponse(GetUsingKey(key), request, version);
            }, new HandlerOptions() { SkipRequestFilters = true });

            /// <summary>  
            ///  Returns all the HTMLCompositions.  
            /// </summary> 
            Handle.GET("/sc/partial/composition/all", (Request request) =>
            {
                return GetResponse(GetAll(), request, null, "all");
            });

            /// <summary>  
            ///  Returns a compositions for merged partial in a set of apps.  
            /// </summary> 
            Handle.GET("/sc/partial/composition?mergedpartial={?}&ver={?}", (string mergedPartial, string version) =>
            {
                return GetResponse(GetUsingMergedPartials(mergedPartial), request, version, "mergedpartial");
            });

            /// <summary>  
            ///  Returns compositions for partial in any set of apps.  
            /// </summary> 
            Handle.GET("/sc/partial/composition?partial={?}&ver={?}", (string partial, string version) =>
            {
                return GetResponse(GetUsingPartial(partial), request, version, "partial");
            });

            /// <summary>  
            ///  Returns compositions for given app partial in standalone model.  
            /// </summary> 
            Handle.GET("/sc/partial/composition?standalonepartial={?}&ver={?}", (string standalonepartial, string version) =>
            {
                return GetResponse(GetUsingStandalonePartial(standalonepartial), request, version, "standalonepartial");
            });

            /// <summary>  
            ///  Returns compositions for given app partials in any set of apps.  
            /// </summary> 
            Handle.GET("/sc/partial/composition?app={?}&ver={?}", (string app, string version) =>
            {
                return GetResponse(GetUsingApp(app), request, version, "app");
            });
        }
    }
}